using System.Net;



namespace Doot.Examples
{
    public class Program
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

        public static async Task Main()
        {
            Logger.SetLogCategory(LogCategory.Debug);
            Logger.AddLogWriter(new ConsoleLogWriter());
            //Logger.AddLogWriter(new FileLogWriter("DootServer.log", true));
            Logger.Run();

            var server = new Server(IPAddress.Any, 0xD007);
            server.RegisterRPCFunction("test_func", TestFunc);
            server.RegisterRPCFunction("another_test_func", AnotherTestFunc);
            await server.Run();
        }
    }
}