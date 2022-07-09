using System.Net;



namespace Doot.Examples
{
    class Program
    {
        static void Main()
        {
            Logger.SetLogCategory(LogCategory.Debug);
            Logger.AddLogWriter(new ConsoleLogWriter());
            //Logger.AddLogWriter(new FileLogWriter("DootServer.log", true));
            Logger.Run();

            var app = new ExampleApplication();
            _ = app.Run();

            while (!app.Finished)
                Thread.Sleep(100);

            Logger.Wait();
        }
    }
}