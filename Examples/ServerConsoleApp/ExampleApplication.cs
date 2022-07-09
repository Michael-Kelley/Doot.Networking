using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;



namespace Doot.Examples
{
    class ExampleApplication
    {
        static object TestFunc(SessionBase session, object[] args)
        {
            /// args: []
            /// return: ulong

            return 42UL;
        }

        static object AnotherTestFunc(SessionBase session, object[] args)
        {
            /// args: [long a, double b, string c]
            /// return: double

            return 123.456;
        }

        public bool Finished { get; private set; } = false;

        public async Task Run()
        {
            await Task.Yield();

            var server = new Server(IPAddress.Any, 0xD007);
            server.RegisterRPCFunction("test_func", TestFunc);
            server.RegisterRPCFunction("another_test_func", AnotherTestFunc);
            await server.Run();

            Finished = true;
        }
    }
}
