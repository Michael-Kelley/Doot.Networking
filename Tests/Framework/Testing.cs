using System.Diagnostics;
using System.Reflection;



namespace Doot.Tests
{
    static class Testing
    {
        public static void RunTests()
        {
            var tests = new List<(string Name, Func<Task> Function)>();

            var assembly = Assembly.GetExecutingAssembly();
            var classes = assembly.GetTypes().Where(t => t.IsClass && t.IsAbstract && t.IsSealed);

            foreach (var c in classes)
            {
                var methods = c.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var m in methods)
                {
                    if (m.GetCustomAttribute<TestAttribute>() != null)
                        tests.Add(($"{c.Name}.{m.Name}", m.CreateDelegate<Func<Task>>()));
                }
            }

            Logger.Log(LogCategory.Info, $"Running {tests.Count} test{(tests.Count > 1 ? "s" : "")}");
            Logger.Log(LogCategory.Info, "------------------------------------------------");

            var watch = new Stopwatch();
            var failCount = 0;
            var totalTime = TimeSpan.Zero;

            foreach (var (name, func) in tests)
            {
                Logger.Log(LogCategory.Info, $"Running {name}");
                bool passed = true;

                try
                {
                    watch.Restart();
                    func().Wait();
                    watch.Stop();
                }
                catch (AssertionException e)
                {
                    watch.Stop();
                    Logger.Log(LogCategory.Error, $"Failure in {name}: {(e.InnerException ?? e).Message}");
                    passed = false;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    Logger.Log(LogCategory.Error, $"Exception in {name}: {(e.InnerException ?? e).Message}");
                    passed = false;
                }

                if (!passed)
                    failCount++;

                totalTime += watch.Elapsed;

                Logger.Log((passed ? LogCategory.Info : LogCategory.Error), $"{name} {(passed ? "passed" : "failed")}! ({watch.Elapsed.Seconds}.{watch.Elapsed.Milliseconds:D3}s)");
                Logger.Log(LogCategory.Info, "------------------------------------------------");
            }

            Logger.Log(LogCategory.Info, $"{tests.Count} test{(tests.Count > 1 ? "s" : "")} ran, {tests.Count - failCount} passed, {failCount} failed. ({totalTime.Seconds}.{totalTime.Milliseconds:D3}s)");
        }
    }
}
