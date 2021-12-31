using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;



namespace Doot.Tests
{
    static class RPCTests
    {
        [Test]
        public static async Task ClientRegisterTest()
        {
            var client = new Client();
            var rpcCount = client.RPCFunctions.Count;
            client.RegisterRPCFunction("some_func", (session, args) => null);

            Assert.Equal(client.RPCFunctions.Count, rpcCount + 1);
        }

        [Test]
        public static async Task CallVoidReturnNoParametersTest()
        {
            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("voidret_nopar", (session, args) =>
            {
                Assert.Equal(args.Length, 0);

                return null;
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task Func()
            {
                var r = await client.CallRemoteProcedure("voidret_nopar");

                Assert.Equal(r, null);
            }

            await Func();

            client.Disconnect();
            server.Stop();
        }

        [Test]
        public static async Task CallLongReturnNoParametersTest()
        {
            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("longret_nopar", (session, args) =>
            {
                Assert.Equal(args.Length, 0);

                return 1988L;
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task<long> Func()
            {
                var r = await client.CallRemoteProcedure("longret_nopar");

                Assert.True(r is long);

                return (long)r;
            }

            var res = await Func();

            Assert.Equal(res, 1988L);

            client.Disconnect();
            server.Stop();
        }

        [Test]
        public static async Task CallVoidReturnLongParameterTest()
        {
            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("voidret_longpar", (session, args) =>
            {
                Assert.Equal(args.Length, 1);
                Assert.True(args[0] is long);
                Assert.Equal((long)args[0], 42L);

                return null;
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task Func(long arg)
            {
                var r = await client.CallRemoteProcedure("voidret_longpar", arg);

                Assert.Equal(r, null);
            }

            await Func(42L);

            client.Disconnect();
            server.Stop();
        }

        [Test]
        public static async Task CallDoubleReturnDoubleParameterTest()
        {
            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("doubleret_doublepar", (session, args) =>
            {
                Assert.Equal(args.Length, 1);
                Assert.True(args[0] is double);
                Assert.Equal((double)args[0], 3.14);

                return 123.456;
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task<double> Func(double arg)
            {
                var r = await client.CallRemoteProcedure("doubleret_doublepar", arg);

                Assert.True(r is double);

                return (double)r;
            }

            var res = await Func(3.14);

            Assert.Equal(res, 123.456);

            client.Disconnect();
            server.Stop();
        }

        class MySerialisableClass : ISerialisable
        {
            public long A;
            public double B;
            public string C;

            public void Deserialise(MessageDeserialiser deserialiser)
            {
                deserialiser.Read(out A);
                deserialiser.Read(out B);
                deserialiser.Read(out C);
            }

            public void Serialise(MessageSerialiser serialiser)
            {
                serialiser.Write(A);
                serialiser.Write(B);
                serialiser.Write(C);
            }
        }

        [Test]
        public static async Task CallVoidReturnClassParameterTest()
        {
            MessageSerialiser.RegisterClass<MySerialisableClass>();
            MessageDeserialiser.RegisterClass<MySerialisableClass>();

            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("voidret_classpar", (session, args) =>
            {
                Assert.Equal(args.Length, 1);
                Assert.True(args[0] is ISerialisable);
                Assert.True(args[0] is MySerialisableClass);

                var arg = (MySerialisableClass)args[0];
                Assert.Equal(arg.A, 0xC0DE);
                Assert.Equal(arg.B, 999.99);
                Assert.Equal(arg.C, "arewecoolyet?");

                return null;
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task Func(MySerialisableClass arg)
            {
                var r = await client.CallRemoteProcedure("voidret_classpar", arg);

                Assert.True(r is null);
            }

            await Func(new MySerialisableClass { A = 0xC0DE, B = 999.99, C = "arewecoolyet?" });

            client.Disconnect();
            server.Stop();

            MessageDeserialiser.ClearRegisteredClasses();
            MessageSerialiser.ClearRegisteredClasses();
        }

        [Test]
        public static async Task CallClassReturnNoParametersTest()
        {
            MessageSerialiser.RegisterClass<MySerialisableClass>();
            MessageDeserialiser.RegisterClass<MySerialisableClass>();

            var server = new Server(IPAddress.Loopback, 0xD007);
            server.RegisterRPCFunction("classret_nopar", (session, args) =>
            {
                return new MySerialisableClass { A = 123, B = 45.678, C = "nine" };
            });
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            async Task<MySerialisableClass> Func()
            {
                var r = await client.CallRemoteProcedure("classret_nopar");

                Assert.True(r is MySerialisableClass);

                return (MySerialisableClass)r;
            }

            var res = await Func();

            Assert.Equal(res.A, 123);
            Assert.Equal(res.B, 45.678);
            Assert.Equal(res.C, "nine");

            client.Disconnect();
            server.Stop();

            MessageDeserialiser.ClearRegisteredClasses();
            MessageSerialiser.ClearRegisteredClasses();
        }
    }
}
