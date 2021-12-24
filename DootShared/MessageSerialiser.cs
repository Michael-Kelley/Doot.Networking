using System;
using System.Collections.Generic;
using System.Text;



namespace Doot
{
    public class MessageSerialiser
    {
        public int Position;

        readonly byte[] buffer;

        public MessageSerialiser(byte[] buffer)
        {
            this.buffer = buffer;
            Position = 0;
        }

        public void Rewind()
        {
            Position = 0;
        }

        public void Skip(int count)
        {
            Position += count;
        }

        public void Read<T>(out T outValue) where T : unmanaged
        {
            unsafe
            {
                fixed (byte* ptr = &buffer[Position])
                {
                    outValue = *(T*)ptr;
                    Position += sizeof(T);
                }
            }
        }

        public void Read(out string outValue)
        {
            Read(out int length);
            outValue = Encoding.UTF8.GetString(buffer, Position, length);
            Position += length;
        }

        public void Write<T>(T value) where T : unmanaged
        {
            unsafe
            {
                fixed(byte* ptr = &buffer[Position])
                {
                    *(T*)ptr = value;
                    Position += sizeof(T);
                }
            }
        }

        public void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Write(bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer, Position, bytes.Length);
            Position += bytes.Length;
        }
    }
}
