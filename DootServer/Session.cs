using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;



namespace Doot
{
    public class Session
    {
        readonly TcpClient client;
        readonly NetworkStream stream;
        readonly MessageDeserialiser deserialiser;
        int readIndex;
        readonly MessageSerialiser serialiser;

        public Session(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            deserialiser = new MessageDeserialiser();
            readIndex = 0;
            serialiser = new MessageSerialiser();
        }

        public async void Receive(CancellationToken cancellation)
        {
            var read = await stream.ReadAsync(deserialiser.Buffer, readIndex, MessageDeserialiser.MAXIMUM_MESSAGE_SIZE - readIndex, cancellation);

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
                                _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
                                return;
                            }

                            Logger.Log(LogCategory.Debug, $"RPC request: [{serial}] {funcName}({String.Join(',', arguments)})");

                            object returnValue = null;

                            // TODO: Change this so that it passes off the RPC arguments to a registered handler
                            if (funcName == "test_func")
                            {
                                returnValue = 42UL;
                            }
                            else if (funcName == "another_test_func")
                            {
                                returnValue = 123.456;
                            }

                            var response = serialiser.SerialiseRPCResponse(serial, returnValue);

                            await stream.WriteAsync(response.Data, 0, response.Length, cancellation);

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
            _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
        }
    }
}
