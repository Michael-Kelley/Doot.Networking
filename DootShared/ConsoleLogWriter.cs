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
                case LogCategory.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            Console.WriteLine($"[{time:u}] [{category}] {message}");
            Console.ForegroundColor = fg;
        }
    }
}
