using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Doot
{
    public class WebSocketClient
    {
        const int READ_BLOCK_SIZE = 4096;
        const int WRITE_BLOCK_SIZE = 4096;

        readonly ClientWebSocket client;
        readonly byte[] readBuf, writeBuf;
        int readIndex;
        readonly MemoryStream readStream, writeStream;
        readonly BinaryReader reader;
        readonly BinaryWriter writer;
        readonly Dictionary<ulong, TaskCompletionSource<object>> rpcRequests;
        readonly CancellationToken cancellation;
        readonly Random rand;

        public WebSocketClient()
        {
            client = new ClientWebSocket();
            readBuf = new byte[READ_BLOCK_SIZE];
            readIndex = 0;
            writeBuf = new byte[WRITE_BLOCK_SIZE];
            readStream = new MemoryStream(readBuf);
            writeStream = new MemoryStream(writeBuf);
            reader = new BinaryReader(readStream);
            writer = new BinaryWriter(writeStream);
            rpcRequests = new Dictionary<ulong, TaskCompletionSource<object>>();
            cancellation = new CancellationToken();
            rand = new Random(Environment.TickCount);
        }

        public async Task Connect(string host, int port)
        {
            await client.ConnectAsync(new Uri($"ws://{host}:{port}"), CancellationToken.None);

            _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
        }

        public async Task Disconnect()
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
            Console.WriteLine("-> 1");
            writeStream.Position = 0;
            writer.Write((byte)MessageType.RpcRequest);
            writeStream.Position += sizeof(int);
            ulong serial;
            Console.WriteLine("-> 2");

            do
            {
                // Generate random ulong
                serial = (ulong)rand.Next(ushort.MinValue, ushort.MaxValue);
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 16;
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 32;
                serial |= ((ulong)rand.Next(ushort.MinValue, ushort.MaxValue)) << 48;
            }
            while (rpcRequests.ContainsKey(serial));
            Console.WriteLine("-> 3");

            writer.Write(serial);
            writer.Write((byte)name.Length);
            writer.Write(Encoding.ASCII.GetBytes(name));
            writer.Write((byte)args.Length);
            Console.WriteLine("-> 4");

            for (int i = 0; i < args.Length; i++)
            {
                var obj = args[i];

                if (obj is ulong u64Obj)
                {
                    writer.Write((byte)FieldType.UInt64);
                    writer.Write(u64Obj);
                } else if (obj is long i64Obj)
                {
                    writer.Write((byte)FieldType.Int64);
                    writer.Write(i64Obj);
                }
                else if (obj is double f64Obj)
                {
                    writer.Write((byte)FieldType.Double);
                    writer.Write(f64Obj);
                } else if (obj is string sObj)
                {
                    writer.Write((byte)FieldType.String);
                    var bytes = Encoding.UTF8.GetBytes(sObj);
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }
                else
                {
                    throw new ArgumentException($"Unsupported argument type: {obj.GetType()}", "args");
                }
            }
            Console.WriteLine("-> 5");

            // Write length
            var length = writeStream.Position;
            writeStream.Position = sizeof(MessageType);
            writer.Write((int)length - (sizeof(MessageType) + sizeof(int)));
            Console.WriteLine("-> 6");

            var tcs = new TaskCompletionSource<object>();
            rpcRequests[serial] = tcs;
            Console.WriteLine("-> 7");

            _ = client.SendAsync(new ArraySegment<byte>(writeBuf, 0, (int)length), WebSocketMessageType.Binary, true, CancellationToken.None);
            Console.WriteLine("-> 8");

            return tcs.Task;
        }

        async void Receive()
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(readBuf, readIndex, READ_BLOCK_SIZE - readIndex), cancellation);

            if (result.Count == 0)
                return;

            var available = readIndex + result.Count;

            if (available < 5)
            {
                readIndex = available;
            }

            readStream.Position = 0;

            for (; ; )
            {
                var messageType = (MessageType)reader.ReadByte();
                available -= sizeof(byte);
                var length = reader.ReadInt32();
                available -= sizeof(int);

                if (available < length)
                {
                    readIndex = (int)readStream.Position;
                    _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
                    return;
                }

                switch (messageType)
                {
                    case MessageType.RpcResponse:
                        {
                            var serial = reader.ReadUInt64();
                            var payloadLength = length - sizeof(ulong);

                            if (!rpcRequests.ContainsKey(serial))
                            {
                                Console.Error.WriteLine($"Received response with invalid serial! Skipping...");
                                readStream.Position += payloadLength;

                                goto End;
                            }

                            var resultType = (FieldType)reader.ReadByte();
                            object rpcResult;

                            switch (resultType)
                            {
                                case FieldType.UInt64:
                                    {
                                        rpcResult = reader.ReadUInt64();
                                        break;
                                    }
                                case FieldType.Int64:
                                    {
                                        rpcResult = reader.ReadInt64();
                                        break;
                                    }
                                case FieldType.Double:
                                    {
                                        rpcResult = reader.ReadDouble();
                                        break;
                                    }
                                case FieldType.Class:
                                    {
                                        var fieldCount = reader.ReadByte();
                                        var fields = new object[fieldCount];

                                        for (int i = 0; i < fieldCount; i++)
                                        {
                                            var fieldType = (FieldType)reader.ReadByte();

                                            switch (fieldType)
                                            {
                                                case FieldType.UInt64:
                                                    {
                                                        fields[i] = reader.ReadUInt64();
                                                        break;
                                                    }
                                                case FieldType.Int64:
                                                    {
                                                        fields[i] = reader.ReadInt64();
                                                        break;
                                                    }
                                                case FieldType.Double:
                                                    {
                                                        fields[i] = reader.ReadDouble();
                                                        break;
                                                    }
                                                case FieldType.String:
                                                    {
                                                        var fieldValueLength = reader.ReadInt32();
                                                        var fieldValueBytes = reader.ReadBytes(fieldValueLength);
                                                        fields[i] = Encoding.UTF8.GetString(fieldValueBytes);
                                                        break;
                                                    }
                                                default:
                                                    break;
                                            }
                                        }

                                        rpcResult = fields;

                                        break;
                                    }
                                default:
                                    {
                                        Console.Error.WriteLine("Unknown field type!");
                                        result = null;
                                        break;
                                    }
                            }

                            Console.WriteLine($"RPC Response: {serial}");
                            var tcs = rpcRequests[serial];
                            rpcRequests.Remove(serial);
                            tcs.SetResult(result);

                            break;
                        }
                    default:
                        {
                            Console.Error.WriteLine("Unknown message type! Skipping...");
                            readStream.Position += length;
                            goto Next;
                        }
                }

                Next:

                available -= length;

                if (available == 0)
                    break;
            }

            End:

            readIndex = 0;
            _ = Task.Factory.StartNew(() => Receive(), CancellationToken.None);
        }
    }
}
