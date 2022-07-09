using System;
using System.Threading;



namespace Doot.Examples
{
    class Program
    {
        static void Main()
        {
            Logger.SetLogCategory(LogCategory.Debug);
            Logger.AddLogWriter(new ConsoleLogWriter());
            //Logger.AddLogWriter(new FileLogWriter("DootClient.log", true));
            Logger.Run();

            var app = new ExampleApplication();
            _ = app.Run();

            while (!app.Finished)
                Thread.Sleep(100);

            Logger.Wait();

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
