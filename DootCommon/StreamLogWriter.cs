using System;
using System.IO;



namespace Doot
{
    public class StreamLogWriter : ILogWriter
    {
        readonly Stream logStream;
        readonly StreamWriter logWriter;

        public StreamLogWriter(Stream outputStream, bool autoFlush = false)
        {
            logStream = outputStream;
            logWriter = new StreamWriter(outputStream)
            {
                AutoFlush = autoFlush
            };
        }

        public void Write(LogCategory category, DateTime time, string message)
        {
            logWriter.WriteLine($"{time:G} | {category,-5} | {message}");
        }
    }
}
