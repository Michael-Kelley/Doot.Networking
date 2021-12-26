using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Doot
{
    public class Client
    {
        readonly TcpClient client;
        NetworkStream stream;
        readonly MessageDeserialiser deserialiser;
        int readIndex;
        readonly MessageSerialiser serialiser;
        readonly Dictionary<ulong, TaskCompletionSource<object>> rpcRequests;
        readonly CancellationToken cancellation;
        readonly Random rand;

        public Client()
        {
            client = new TcpClient();
            deserialiser = new MessageDeserialiser();
            readIndex = 0;
            serialiser = new MessageSerialiser();
            rpcRequests = new Dictionary<ulong, TaskCompletionSource<object>>();
            cancellation = new CancellationToken();
            rand = new Random(Environment.TickCount);
        }

        public async Task Connect(string host, int port)
        {
            await client.ConnectAsync(host, port);
            stream = client.GetStream();

            _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
        }

        public void Disconnect()
        {
            client.Close();
        }

        public async Task<ulong> CallTestFunc()
        {
            return (ulong)await CallRemoteProcedure("test_func");
        }

        public async Task<double> CallAnotherTestFunc(long arg1, double arg2, string arg3)
        {
            return (double)await CallRemoteProcedure("another_test_func", arg1, arg2, arg3);
        }

        internal Task<object> CallRemoteProcedure(string name, params object[] arguments)
        {
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

            _ = stream.WriteAsync(request.Data, 0, request.Length);

            return tcs.Task;
        }

        async void Receive()
        {
            var read = await stream.ReadAsync(deserialiser.Buffer, readIndex, MessageDeserialiser.MAXIMUM_MESSAGE_SIZE - readIndex, cancellation);

            if (read == 0)
                return;

            readIndex += read;
            var available = readIndex;

            for (; ; )
            {
                var messageType = deserialiser.GetNextMessageType();

                switch (messageType)
                {
                    case MessageType.RpcResponse:
                        {
                            if (!deserialiser.TryDeserialiseRPCResponse(available, out var serial, out var returnValue, out var consumed))
                            {
                                _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
                                return;
                            }

                            if (!rpcRequests.ContainsKey(serial))
                            {
                                Logger.Log(LogCategory.Error, $"Received response with invalid serial! Skipping...");
                                available -= consumed;
                                goto Next;
                            }

                            Logger.Log(LogCategory.Debug, $"RPC Response: {serial}");
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
            _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
        }
    }
}
