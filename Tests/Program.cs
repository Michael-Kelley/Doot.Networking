using Doot;
using Doot.Tests;



Logger.SetLogCategory(LogCategory.Debug);
Logger.AddLogWriter(new ConsoleLogWriter());
Logger.Run();

Testing.RunTests();

Logger.Wait();