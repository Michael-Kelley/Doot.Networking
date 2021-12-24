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
        const int READ_BLOCK_SIZE = 4096;
        const int WRITE_BLOCK_SIZE = 4096;

        readonly TcpClient client;
        readonly NetworkStream stream;
        readonly byte[] readBuf, writeBuf;
        readonly MessageSerialiser readSerialiser, writeSerialiser;

        public Session(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            readBuf = new byte[READ_BLOCK_SIZE];
            writeBuf = new byte[WRITE_BLOCK_SIZE];
            readSerialiser = new MessageSerialiser(readBuf);
            writeSerialiser = new MessageSerialiser(writeBuf);
        }

        public async void Receive(CancellationToken cancellation)
        {
            var read = await stream.ReadAsync(readBuf, readSerialiser.Position, READ_BLOCK_SIZE - readSerialiser.Position, cancellation);

            //var s = Encoding.UTF8.GetString(readBuf, 0, read);

            if (read == 0)
                return;

            var available = readSerialiser.Position + read;

            if (available < 5)
            {
                readSerialiser.Position = available;
                _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
                return;
            }

            for (; ; )
            {
                readSerialiser.Read(out MessageType messageType);
                available -= sizeof(MessageType);
                readSerialiser.Read(out int length);
                available -= sizeof(int);

                if (available < length)
                {
                    _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
                    return;
                }

                switch (messageType)
                {
                    case MessageType.RpcRequest:
                        {
                            readSerialiser.Read(out ulong serial);
                            readSerialiser.Read(out string funcName);
                            readSerialiser.Read(out byte argCount);
                            var args = new object[argCount];

                            for (int i = 0; i < argCount; i++)
                            {
                                readSerialiser.Read(out FieldType argType);

                                switch (argType)
                                {
                                    case FieldType.UInt64:
                                        {
                                            readSerialiser.Read(out ulong fieldValue);
                                            args[i] = fieldValue;
                                            break;
                                        }
                                    case FieldType.Int64:
                                        {
                                            readSerialiser.Read(out long fieldValue);
                                            args[i] = fieldValue;
                                            break;
                                        }
                                    case FieldType.Double:
                                        {
                                            readSerialiser.Read(out double fieldValue);
                                            args[i] = fieldValue;
                                            break;
                                        }
                                    case FieldType.String:
                                        {
                                            readSerialiser.Read(out string fieldValue);
                                            args[i] = fieldValue;
                                            break;
                                        }
                                }
                            }

                            Logger.Log(LogCategory.Debug, $"RPC request: [{serial}] {funcName}({String.Join(',', args)})");

                            writeSerialiser.Rewind();
                            writeSerialiser.Write(MessageType.RpcResponse);
                            writeSerialiser.Skip(sizeof(int));  // Skip length for now
                            writeSerialiser.Write(serial);

                            if (funcName == "test_func")
                            {
                                writeSerialiser.Write(FieldType.Int64);
                                writeSerialiser.Write(42UL);
                            }
                            else if(funcName == "another_test_func")
                            {
                                writeSerialiser.Write(FieldType.Double);
                                writeSerialiser.Write(123.456);
                            } else
                            {
                                // TODO: Change this so that it passes off the RPC arguments to a registered handler
                            }

                            var responseLength = writeSerialiser.Position;
                            writeSerialiser.Position = sizeof(MessageType);
                            writeSerialiser.Write(responseLength - (sizeof(MessageType) + sizeof(int)));    // Length

                            await stream.WriteAsync(writeBuf, 0, responseLength, cancellation);

                            break;
                        }
                    default:
                        {
                            readSerialiser.Skip(length);
                            Logger.Log(LogCategory.Error, "Unknown message type! Skipping...");
                            goto Next;
                        }
                }

                Next:

                available -= length;

                if (available == 0)
                    break;
            }

            End:

            readSerialiser.Rewind();
            _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
        }
    }
}
