using System.Net;



namespace Doot.Examples
{
    public class Program
    {
        public static async Task Main()
        {
            Logger.SetLogCategory(LogCategory.Debug);
            Logger.AddLogWriter(new ConsoleLogWriter());
            //Logger.AddLogWriter(new FileLogWriter("DootServer.log", true));
            Logger.Run();

            var server = new Server(IPAddress.Any, 0xD007);
            await server.Run();
        }
    }
}