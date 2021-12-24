using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot.Examples
{
    class Program
    {
        static async Task Main()
        {
            Logger.SetLogCategory(LogCategory.Debug);
            Logger.AddLogWriter(new ConsoleLogWriter());
            //Logger.AddLogWriter(new FileLogWriter("DootClient.log", true));
            Logger.Run();

            var client = new Client();
            Console.WriteLine("Press any key to connect to server...");
            Console.ReadKey();
            await client.Connect("127.0.0.1", 0xD007);
            Logger.Log(LogCategory.Information, "Connected!");
            var result = await client.CallTestFunc();
            Logger.Log(LogCategory.Debug, $"Called remote procedure! Result = {result}");
            var result2 = await client.CallAnotherTestFunc(1, 2.3, "four");
            Logger.Log(LogCategory.Debug, $"Called remote procedure! Result = {result2}");
            client.Disconnect();
            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
