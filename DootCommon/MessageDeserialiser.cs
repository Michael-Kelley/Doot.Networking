using System;
using System.Collections.Generic;
using System.Text;



namespace Doot
{
    public class MessageDeserialiser
    {
        public const int MAXIMUM_MESSAGE_SIZE = 4096;

        readonly static Dictionary<ulong, Func<MessageDeserialiser, ISerialisable>> classDeserialisers = new Dictionary<ulong, Func<MessageDeserialiser, ISerialisable>>();

        public byte[] Buffer { get; private set; }

        public int Position;

        public MessageDeserialiser()
        {
            Buffer = new byte[MAXIMUM_MESSAGE_SIZE];
            Position = 0;
        }

        public static void RegisterClass<T>() where T : ISerialisable, new()
        {
            classDeserialisers.Add(FNV1a.ComputeHash(typeof(T).FullName), (s) =>
            {
                var r = new T();
                r.Deserialise(s);

                return r;
            });
        }

        public MessageType GetNextMessageType()
        {
            return (MessageType)Buffer[Position];
        }

        public void SkipMessage(out int consumed)
        {
            Skip(sizeof(MessageType));
            consumed = sizeof(MessageType);
            Read(out int payloadLength);
            consumed += sizeof(int);
            Skip(payloadLength);
            consumed += payloadLength;
        }

        public void Rewind()
        {
            Position = 0;
        }

        public bool TryDeserialiseRPCRequest(int availableBytes, out ulong serial, out string funcName, out object[] arguments, out int consumed)
        {
            if (availableBytes < 5)
            {
                serial = 0;
                funcName = null;
                arguments = null;
                consumed = 0;
                return false;
            }

            if (GetNextMessageType() != MessageType.RpcRequest)
            {
                serial = 0;
                funcName = null;
                arguments = null;
                consumed = 0;
                return false;
            }

            var startPosition = Position;

            Skip(sizeof(MessageType));
            availableBytes -= sizeof(MessageType);
            consumed = sizeof(MessageType);
            Read(out int payloadLength);
            availableBytes -= sizeof(int);
            consumed += sizeof(int);

            if (availableBytes < payloadLength)
            {
                serial = 0;
                funcName = null;
                arguments = null;
                Position = startPosition;
                return false;
            }

            Read(out serial);
            Read(out funcName);
            Read(out byte argCount);
            arguments = new object[argCount];

            for (int i = 0; i < argCount; i++)
            {
                Read(out FieldType argType);

                switch (argType)
                {
                    case FieldType.Null:
                        {
                            arguments[i] = null;
                            break;
                        }
                    case FieldType.UInt64:
                        {
                            Read(out ulong fieldValue);
                            arguments[i] = fieldValue;
                            break;
                        }
                    case FieldType.Int64:
                        {
                            Read(out long fieldValue);
                            arguments[i] = fieldValue;
                            break;
                        }
                    case FieldType.Double:
                        {
                            Read(out double fieldValue);
                            arguments[i] = fieldValue;
                            break;
                        }
                    case FieldType.String:
                        {
                            Read(out string fieldValue);
                            arguments[i] = fieldValue;
                            break;
                        }
                    case FieldType.Class:
                        {
                            Read(out ISerialisable fieldValue);
                            arguments[i] = fieldValue;
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException($"Unsupported RPC argument '{argType}'!");
                        }
                }
            }

            consumed += payloadLength;

            return true;
        }

        public bool TryDeserialiseRPCResponse(int availableBytes, out ulong serial, out object returnValue, out int consumed)
        {
            if (availableBytes < 5)
            {
                serial = 0;
                returnValue = null;
                consumed = 0;
                return false;
            }

            if (GetNextMessageType() != MessageType.RpcResponse)
            {
                serial = 0;
                returnValue = null;
                consumed = 0;
                return false;
            }

            var startPosition = Position;

            Skip(sizeof(MessageType));
            availableBytes -= sizeof(MessageType);
            consumed = sizeof(MessageType);
            Read(out int payloadLength);
            availableBytes -= sizeof(int);
            consumed += sizeof(int);

            if (availableBytes < payloadLength)
            {
                serial = 0;
                returnValue = null;
                Position = startPosition;
                return false;
            }

            Read(out serial);
            Read(out FieldType returnType);

            switch (returnType)
            {
                case FieldType.Null:
                    {
                        returnValue = null;
                        break;
                    }
                case FieldType.UInt64:
                    {
                        Read(out ulong value);
                        returnValue = value;
                        break;
                    }
                case FieldType.Int64:
                    {
                        Read(out long value);
                        returnValue = value;
                        break;
                    }
                case FieldType.Double:
                    {
                        Read(out double value);
                        returnValue = value;
                        break;
                    }
                case FieldType.Class:
                    {
                        Read(out ISerialisable value);
                        returnValue = value;
                        break;
                    }
                default:
                    {
                        Logger.Log(LogCategory.Error, $"Unknown return type '{returnType}'!");
                        returnValue = null;
                        break;
                    }
            }

            consumed += payloadLength;

            return true;
        }

        void Skip(int count)
        {
            Position += count;
        }

        public void Read<T>(out T outValue) where T : unmanaged
        {
            unsafe
            {
                fixed (byte* ptr = &Buffer[Position])
                {
                    outValue = *(T*)ptr;
                    Position += sizeof(T);
                }
            }
        }

        public void Read(out string outValue)
        {
            Read(out int length);
            outValue = Encoding.UTF8.GetString(Buffer, Position, length);
            Position += length;
        }

        public void Read(out ISerialisable outValue)
        {
            Read(out ulong classId);

            if (!classDeserialisers.ContainsKey(classId))
                throw new ArgumentException($"Unregistered deserialisable class '{classId}'!");

            outValue = classDeserialisers[classId](this);
        }
    }
}
