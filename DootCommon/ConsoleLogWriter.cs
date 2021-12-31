using System;



namespace Doot
{
    public class ConsoleLogWriter : ILogWriter
    {
        public void Write(LogCategory category, DateTime time, string message)
        {
            var fg = Console.ForegroundColor;

            switch (category)
            {
                case LogCategory.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogCategory.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            Console.WriteLine($"{time:G} | {category,-5} | {message}");
            Console.ForegroundColor = fg;
        }
    }
}
