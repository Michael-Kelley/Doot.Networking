using System;
using System.Text;



namespace Doot
{
    class MessageDeserialiser
    {
        public const int MAXIMUM_MESSAGE_SIZE = 4096;

        public byte[] Buffer { get; private set; }

        public int Position;

        public MessageDeserialiser()
        {
            Buffer = new byte[MAXIMUM_MESSAGE_SIZE];
            Position = 0;
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
                    default:
                        {
                            throw new ArgumentException($"Unsupported RPC argument '{argType}'!");
                            break;
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
                        /*var fieldCount = reader.ReadByte();
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

                        result = fields;*/
                        returnValue = null;

                        break;
                    }
                default:
                    {
                        Logger.Log(LogCategory.Error, "Unknown field type!");
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

        void Read<T>(out T outValue) where T : unmanaged
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

        void Read(out string outValue)
        {
            Read(out int length);
            outValue = Encoding.UTF8.GetString(Buffer, Position, length);
            Position += length;
        }
    }
}
