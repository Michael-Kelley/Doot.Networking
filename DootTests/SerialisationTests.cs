using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;



namespace Doot.Tests
{
    class MySimpleClass : ISerialisable, IEquatable<MySimpleClass>
    {
        public long ALong;
        public string AString;

        public void Serialise(MessageSerialiser serialiser)
        {
            serialiser.Write(ALong);
            serialiser.Write(AString);
        }

        public void Deserialise(MessageDeserialiser deserialiser)
        {
            deserialiser.Read(out ALong);
            deserialiser.Read(out AString);
        }

        public bool Equals(MySimpleClass other)
        {
            return ALong == other.ALong
                && AString == other.AString;
        }

        public override bool Equals(object obj) => Equals(obj as MySimpleClass);

        public override int GetHashCode()
        {
            return HashCode.Combine(ALong, AString);
        }
    }

    class MyStructClass : ISerialisable, IEquatable<MyStructClass>
    {
        public struct MyNestedStruct
        {
            public ulong AULong;
            public long ALong;
            public double ADouble;
        }

        public MyNestedStruct AStruct;

        public void Serialise(MessageSerialiser serialiser)
        {
            serialiser.Write(AStruct);
        }

        public void Deserialise(MessageDeserialiser deserialiser)
        {
            deserialiser.Read(out AStruct);
        }

        public bool Equals(MyStructClass other)
        {
            return AStruct.Equals(other.AStruct);
        }

        public override bool Equals(object obj) => Equals(obj as MyStructClass);

        public override int GetHashCode()
        {
            return AStruct.GetHashCode();
        }
    }

    class MyClassClass : ISerialisable, IEquatable<MyClassClass>
    {
        public class MyNestedClass : ISerialisable, IEquatable<MyNestedClass>
        {
            public ulong AULong;
            public long ALong;
            public double ADouble;

            public void Serialise(MessageSerialiser serialiser)
            {
                serialiser.Write(AULong);
                serialiser.Write(ALong);
                serialiser.Write(ADouble);
            }

            public void Deserialise(MessageDeserialiser deserialiser)
            {
                deserialiser.Read(out AULong);
                deserialiser.Read(out ALong);
                deserialiser.Read(out ADouble);
            }

            public bool Equals(MyNestedClass other)
            {
                return AULong == other.AULong
                    && ALong == other.ALong
                    && ADouble == other.ADouble;
            }

            public override bool Equals(object obj) => Equals(obj as MyNestedClass);

            public override int GetHashCode()
            {
                return HashCode.Combine(AULong, ALong, ADouble);
            }
        }

        public MyNestedClass AClass;

        public void Serialise(MessageSerialiser serialiser)
        {
            serialiser.Write(AClass);
        }

        public void Deserialise(MessageDeserialiser deserialiser)
        {
            deserialiser.Read(out ISerialisable _AClass);
            AClass = (MyNestedClass)_AClass;
        }

        public bool Equals(MyClassClass other)
        {
            return AClass.Equals(other.AClass);
        }

        public override bool Equals(object obj) => Equals(obj as MyClassClass);

        public override int GetHashCode()
        {
            return AClass.GetHashCode();
        }
    }



    [TestClass]
    public class SerialisationTests
    {
        [TestMethod]
        public void Write_ULong_ReadsSame()
        {
            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = 42UL;

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out ulong readValue);

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_Long_ReadsSame()
        {
            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = -42L;

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out long readValue);

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_Double_ReadsSame()
        {
            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = 42.0;

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out double readValue);

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_String_ReadsSame()
        {
            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = "forty two";

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out string readValue);

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_SimpleClass_ReadsSame()
        {
            MessageSerialiser.RegisterClass<MySimpleClass>();
            MessageDeserialiser.RegisterClass<MySimpleClass>();

            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = new MySimpleClass { ALong = 42L, AString = "forty two" };

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out ISerialisable _readValue);

            Assert.IsTrue(_readValue is MySimpleClass);

            var readValue = (MySimpleClass)_readValue;

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_StructClass_ReadsSame()
        {
            MessageSerialiser.RegisterClass<MyStructClass>();
            MessageDeserialiser.RegisterClass<MyStructClass>();

            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = new MyStructClass { AStruct = new MyStructClass.MyNestedStruct { AULong = 42UL, ALong = -42L, ADouble = 42.0 } };

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out ISerialisable _readValue);

            Assert.IsTrue(_readValue is MyStructClass);

            var readValue = (MyStructClass)_readValue;

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Write_ClassClass_ReadsSame()
        {
            MessageSerialiser.RegisterClass<MyClassClass>();
            MessageSerialiser.RegisterClass<MyClassClass.MyNestedClass>();
            MessageDeserialiser.RegisterClass<MyClassClass>();
            MessageDeserialiser.RegisterClass<MyClassClass.MyNestedClass>();

            var serialiser = new MessageSerialiser();
            var pSerialiser = new PrivateObject(serialiser);
            var deserialiser = new MessageDeserialiser();
            var value = new MyClassClass { AClass = new MyClassClass.MyNestedClass { AULong = 42UL, ALong = -42L, ADouble = 42.0 } };

            serialiser.Write(value);
            var writeBuffer = pSerialiser.GetField<byte[]>("writeBuffer");
            var writePosition = pSerialiser.GetField<int>("position");
            Buffer.BlockCopy(writeBuffer, 0, deserialiser.Buffer, 0, writePosition);

            deserialiser.Read(out ISerialisable _readValue);

            Assert.IsTrue(_readValue is MyClassClass);

            var readValue = (MyClassClass)_readValue;

            Assert.AreEqual(value, readValue);
        }

        [TestMethod]
        public void Serialise_Request_DeserialisesSame()
        {
            var serialiser = new MessageSerialiser();
            var deserialiser = new MessageDeserialiser();
            var serial = 123456789UL;
            var funcName = "not_a_real_function";
            var arg1 = 42L;
            var arg2 = "forty two";

            var (data, length) = serialiser.SerialiseRPCRequest(serial, funcName, new object[] { arg1, arg2 });
            Buffer.BlockCopy(data, 0, deserialiser.Buffer, 0, length);

            var res = deserialiser.TryDeserialiseRPCRequest(length, out var readSerial, out var readFuncName, out var readArgs, out _);

            Assert.IsTrue(res);
            Assert.AreEqual(serial, readSerial);
            Assert.AreEqual(funcName, readFuncName);
            Assert.AreEqual(readArgs.Length, 2);
            var (readArg1, readArg2) = readArgs.ToValueTuple<long, string>();
            Assert.AreEqual(arg1, readArg1);
            Assert.AreEqual(arg2, readArg2);
        }

        [TestMethod]
        public void Serialise_Response_DeserialisesSame()
        {
            var serialiser = new MessageSerialiser();
            var deserialiser = new MessageDeserialiser();
            var serial = 123456789UL;
            var returnValue = 42L;

            var (data, length) = serialiser.SerialiseRPCResponse(serial, returnValue);
            Buffer.BlockCopy(data, 0, deserialiser.Buffer, 0, length);

            var res = deserialiser.TryDeserialiseRPCResponse(length, out var readSerial, out var readReturnValue, out _);

            Assert.IsTrue(res);
            Assert.AreEqual(serial, readSerial);
            Assert.IsTrue(readReturnValue is long);
            Assert.AreEqual(returnValue, (long)readReturnValue);
        }
    }
}