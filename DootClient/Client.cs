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
        const int READ_BLOCK_SIZE = 4096;
        const int WRITE_BLOCK_SIZE = 4096;

        readonly TcpClient client;
        NetworkStream stream;
        readonly byte[] readBuf, writeBuf;
        readonly MessageSerialiser readSerialiser, writeSerialiser;
        readonly Dictionary<ulong, TaskCompletionSource<object>> rpcRequests;
        readonly CancellationToken cancellation;
        readonly Random rand;

        public Client()
        {
            client = new TcpClient();
            readBuf = new byte[READ_BLOCK_SIZE];
            readSerialiser = new MessageSerialiser(readBuf);
            writeBuf = new byte[WRITE_BLOCK_SIZE];
            writeSerialiser = new MessageSerialiser(writeBuf);
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

        public async Task<long> CallTestFunc()
        {
            return (long)await CallRemoteProcedure("test_func");
        }

        public async Task<double> CallAnotherTestFunc(long arg1, double arg2, string arg3)
        {
            return (double)await CallRemoteProcedure("another_test_func", arg1, arg2, arg3);
        }

        internal Task<object> CallRemoteProcedure(string name, params object[] args)
        {
            writeSerialiser.Rewind();
            writeSerialiser.Write(MessageType.RpcRequest);
            writeSerialiser.Skip(sizeof(int));  // Skip length for now
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

            writeSerialiser.Write(serial);
            writeSerialiser.Write(name);
            writeSerialiser.Write((byte)args.Length);

            for (int i = 0; i < args.Length; i++)
            {
                var obj = args[i];

                if (obj is ulong u64Obj)
                {
                    writeSerialiser.Write((byte)FieldType.UInt64);
                    writeSerialiser.Write(u64Obj);
                }
                else if (obj is long i64Obj)
                {
                    writeSerialiser.Write((byte)FieldType.Int64);
                    writeSerialiser.Write(i64Obj);
                }
                else if (obj is double f64Obj)
                {
                    writeSerialiser.Write((byte)FieldType.Double);
                    writeSerialiser.Write(f64Obj);
                }
                else if (obj is string sObj)
                {
                    writeSerialiser.Write((byte)FieldType.String);
                    writeSerialiser.Write(sObj);
                }
                else
                {
                    throw new ArgumentException($"Unsupported argument type: {obj.GetType()}", "args");
                }
            }

            // Write length
            var length = writeSerialiser.Position;
            writeSerialiser.Position = sizeof(MessageType);
            writeSerialiser.Write(length - (sizeof(MessageType) + sizeof(int)));

            var tcs = new TaskCompletionSource<object>();
            rpcRequests[serial] = tcs;

            _ = stream.WriteAsync(writeBuf, 0, length);

            return tcs.Task;
        }

        async void Receive()
        {
            var read = await stream.ReadAsync(readBuf, readSerialiser.Position, READ_BLOCK_SIZE - readSerialiser.Position, cancellation);

            if (read == 0)
                return;

            var available = readSerialiser.Position + read;

            if (available < 5)
            {
                readSerialiser.Position = available;
                _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
                return;
            }

            for (; ; )
            {
                readSerialiser.Read(out MessageType messageType);
                available -= sizeof(byte);
                readSerialiser.Read(out int length);
                available -= sizeof(int);

                if (available < length)
                {
                    _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
                    return;
                }

                switch (messageType)
                {
                    case MessageType.RpcResponse:
                        {
                            readSerialiser.Read(out ulong serial);
                            var payloadLength = length - sizeof(ulong);

                            if (!rpcRequests.ContainsKey(serial))
                            {
                                Logger.Log(LogCategory.Error, $"Received response with invalid serial! Skipping...");
                                readSerialiser.Skip(payloadLength);

                                goto End;
                            }

                            readSerialiser.Read(out FieldType resultType);
                            object result;

                            switch (resultType)
                            {
                                case FieldType.UInt64:
                                    {
                                        readSerialiser.Read(out ulong resultValue);
                                        result = resultValue;
                                        break;
                                    }
                                case FieldType.Int64:
                                    {
                                        readSerialiser.Read(out long resultValue);
                                        result = resultValue;
                                        break;
                                    }
                                case FieldType.Double:
                                    {
                                        readSerialiser.Read(out double resultValue);
                                        result = resultValue;
                                        break;
                                    }
                                case FieldType.Class:
                                    {
                                        //var fieldCount = reader.ReadByte();
                                        //var fields = new object[fieldCount];

                                        //for (int i = 0; i < fieldCount; i++)
                                        //{
                                        //    var fieldType = (FieldType)reader.ReadByte();

                                        //    switch (fieldType)
                                        //    {
                                        //        case FieldType.UInt64:
                                        //            {
                                        //                fields[i] = reader.ReadUInt64();
                                        //                break;
                                        //            }
                                        //        case FieldType.Int64:
                                        //            {
                                        //                fields[i] = reader.ReadInt64();
                                        //                break;
                                        //            }
                                        //        case FieldType.Double:
                                        //            {
                                        //                fields[i] = reader.ReadDouble();
                                        //                break;
                                        //            }
                                        //        case FieldType.String:
                                        //            {
                                        //                var fieldValueLength = reader.ReadInt32();
                                        //                var fieldValueBytes = reader.ReadBytes(fieldValueLength);
                                        //                fields[i] = Encoding.UTF8.GetString(fieldValueBytes);
                                        //                break;
                                        //            }
                                        //        default:
                                        //            break;
                                        //    }
                                        //}

                                        //result = fields;
                                        result = null;

                                        break;
                                    }
                                default:
                                    {
                                        Logger.Log(LogCategory.Error, "Unknown field type!");
                                        result = null;
                                        break;
                                    }
                            }

                            Logger.Log(LogCategory.Debug, $"RPC Response: {serial}");
                            var tcs = rpcRequests[serial];
                            rpcRequests.Remove(serial);
                            tcs.SetResult(result);

                            break;
                        }
                    default:
                        {
                            Logger.Log(LogCategory.Error, "Unknown message type! Skipping...");
                            readSerialiser.Skip(length);
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
            _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
        }
    }
}
