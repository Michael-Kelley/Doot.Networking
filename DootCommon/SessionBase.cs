using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Doot
{
    public abstract class SessionBase
    {
        public SessionState State;

        protected readonly TcpClient client;

        protected NetworkStream stream;
        
        readonly MessageDeserialiser deserialiser;
        readonly MessageSerialiser serialiser;
        readonly Dictionary<ulong, TaskCompletionSource<object>> rpcRequests;
        readonly Random rand;
        readonly SemaphoreSlim writeSemaphore;

        IRPCManager rpcManager;
        int readIndex;

        public SessionBase(TcpClient client)
        {
            this.client = client;

            if (client.Connected)
                stream = client.GetStream();

            deserialiser = new MessageDeserialiser();
            readIndex = 0;
            serialiser = new MessageSerialiser();
            rpcRequests = new Dictionary<ulong, TaskCompletionSource<object>>();
            rand = new Random();
            writeSemaphore = new SemaphoreSlim(1, 1);
            // TODO: Note to self: fix writing to same client from multiple threads in cabal emulator
        }

        protected void SetRPCManager(IRPCManager rpcManager)
        {
            this.rpcManager = rpcManager;
        }

        public void ReceiveLoop(CancellationToken cancellation)
        {
            _ = Task.Run(() => Receive(cancellation));
        }

        protected async void Receive(CancellationToken cancellation)
        {
            await Task.Yield();

            int read;

            try
            {
                read = await stream.ReadAsync(deserialiser.Buffer, readIndex, MessageDeserialiser.MAXIMUM_MESSAGE_SIZE - readIndex, cancellation);
            }
            catch (Exception)
            {
                return;
            }

            //var s = Encoding.UTF8.GetString(readBuf, 0, read);

            if (read == 0)
                return;

            readIndex += read;
            var available = readIndex;

            for (; ; )
            {
                var messageType = deserialiser.GetNextMessageType();

                switch (messageType)
                {
                    case MessageType.RpcRequest:
                        {
                            if (!deserialiser.TryDeserialiseRPCRequest(available, out var serial, out var funcName, out var arguments, out var consumed))
                            {
                                ReceiveLoop(cancellation);
                                return;
                            }

                            Logger.Log(LogCategory.Debug, $"RPC request: [{serial}] {funcName}({String.Join(", ", arguments)})");

                            var returnValue = rpcManager.GetRPCFunction(funcName)(this, arguments);
                            var response = serialiser.SerialiseRPCResponse(serial, returnValue);

                            await Write(response.Data, response.Length, cancellation);

                            available -= consumed;

                            break;
                        }
                    case MessageType.RpcResponse:
                        {
                            if (!deserialiser.TryDeserialiseRPCResponse(available, out var serial, out var returnValue, out var consumed))
                            {
                                ReceiveLoop(cancellation);
                                return;
                            }

                            if (!rpcRequests.ContainsKey(serial))
                            {
                                Logger.Log(LogCategory.Error, $"Received response with invalid serial! Skipping...");
                                available -= consumed;
                                goto Next;
                            }

                            Logger.Log(LogCategory.Debug, $"RPC response: {serial}");
                            var tcs = rpcRequests[serial];
                            rpcRequests.Remove(serial);
                            tcs.SetResult(returnValue);
                            available -= consumed;

                            break;
                        }
                    default:
                        {
                            Logger.Log(LogCategory.Error, $"Unknown message type '{messageType}'! Skipping...");
                            deserialiser.SkipMessage(out var consumed);
                            available -= consumed;
                            goto Next;
                        }
                }

                Next:

                if (available == 0)
                    break;
            }

            deserialiser.Rewind();
            readIndex = 0;
            ReceiveLoop(cancellation);
        }

        public async Task<object> CallRemoteProcedure(string name, params object[] arguments)
        {
            await Task.Yield();

            ulong serial;

            do
            {
                // Generate random ulong
                serial = (ulong)rand.Next(ushort.MinValue, ushort.MaxValue);
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 16;
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 32;
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 48;
            }
            while (rpcRequests.ContainsKey(serial));

            var request = serialiser.SerialiseRPCRequest(serial, name, arguments);

            var tcs = new TaskCompletionSource<object>();
            rpcRequests[serial] = tcs;

            await Write(request.Data, request.Length, CancellationToken.None);

            return await tcs.Task;
        }

        public async Task Write(byte[] data, int length, CancellationToken cancellation)
        {
            await writeSemaphore.WaitAsync();

            try {
                await stream.WriteAsync(data, 0, length);
            }
            finally {
                writeSemaphore.Release();
            }
        }
    }
}
