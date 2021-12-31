using System;
using System.Collections.Generic;
using System.Text;



namespace Doot
{
    public class MessageSerialiser
    {
        const int MAXIMUM_MESSAGE_SIZE = 4096;

        readonly static Dictionary<Type, ulong> classIds = new Dictionary<Type, ulong>();

        readonly byte[] writeBuffer;
        int position;

        public MessageSerialiser()
        {
            writeBuffer = new byte[MAXIMUM_MESSAGE_SIZE];
            position = 0;
        }

        public static void RegisterClass<T>()
        {
            classIds.Add(typeof(T), FNV1a.ComputeHash(typeof(T).FullName));
        }

        internal static void ClearRegisteredClasses()
        {
            classIds.Clear();
        }

        public (byte[] Data, int Length) SerialiseRPCRequest(ulong serial, string funcName, object[] arguments)
        {
            position = 0;
            Write(MessageType.RpcRequest);
            Skip(sizeof(int));  // Skip length for now
            Write(serial);
            Write(funcName);
            Write((byte)arguments.Length);

            for (int i = 0; i < arguments.Length; i++)
            {
                var obj = arguments[i];

                if (obj == null)
                {
                    Write(FieldType.Null);
                }
                if (obj is ulong u64Obj)
                {
                    Write(FieldType.UInt64);
                    Write(u64Obj);
                }
                else if (obj is long i64Obj)
                {
                    Write(FieldType.Int64);
                    Write(i64Obj);
                }
                else if (obj is double f64Obj)
                {
                    Write(FieldType.Double);
                    Write(f64Obj);
                }
                else if (obj is string sObj)
                {
                    Write(FieldType.String);
                    Write(sObj);
                }
                else if (obj is ISerialisable zObj)
                {
                    Write(FieldType.Class);
                    Write(zObj);
                }
                else
                {
                    throw new ArgumentException($"Unsupported argument type: {obj.GetType()}", "args");
                }
            }

            // Write length
            var length = position;
            position = sizeof(MessageType);
            Write(length - (sizeof(MessageType) + sizeof(int)));

            return (writeBuffer, length);
        }

        public (byte[] Data, int Length) SerialiseRPCResponse(ulong serial, object returnValue)
        {
            position = 0;
            Write(MessageType.RpcResponse);
            Skip(sizeof(int));  // Skip length for now
            Write(serial);

            if (returnValue == null)
            {
                Write(FieldType.Null);
            }
            else if (returnValue is ulong u64ReturnValue)
            {
                Write(FieldType.UInt64);
                Write(u64ReturnValue);
            }
            else if (returnValue is long i64ReturnValue)
            {
                Write(FieldType.Int64);
                Write(i64ReturnValue);
            }
            else if (returnValue is double f64ReturnValue)
            {
                Write(FieldType.Double);
                Write(f64ReturnValue);
            }
            else if (returnValue is string sReturnValue)
            {
                Write(FieldType.String);
                Write(sReturnValue);
            }
            else if (returnValue is ISerialisable zReturnValue)
            {
                Write(FieldType.Class);
                Write(zReturnValue);
            }
            else
            {
                throw new ArgumentException("Unsupported return type!", nameof(returnValue));
            }

            var length = position;
            position = sizeof(MessageType);
            Write(length - (sizeof(MessageType) + sizeof(int)));    // Length

            return (writeBuffer, length);
        }

        void Skip(int count)
        {
            position += count;
        }

        public void Write<T>(T value) where T : unmanaged
        {
            unsafe
            {
                fixed (byte* ptr = &writeBuffer[position])
                {
                    *(T*)ptr = value;
                    position += sizeof(T);
                }
            }
        }

        public void Write(byte[] value)
        {
            Buffer.BlockCopy(value, 0, writeBuffer, position, value.Length);
            position += value.Length;
        }

        public void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Write(bytes.Length);
            Write(bytes);
        }

        public void Write(ISerialisable value)
        {
            if (!classIds.ContainsKey(value.GetType()))
                throw new Exception($"Unregistered serialisable class '{value.GetType().FullName}'!");

            Write(classIds[value.GetType()]);
            value.Serialise(this);
        }
    }
}
