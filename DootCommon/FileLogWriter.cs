using System;
using System.IO;



namespace Doot
{
    public class FileLogWriter : ILogWriter
    {
        readonly StreamWriter logWriter;

        public FileLogWriter(string filename, bool autoFlush = false)
        {
            if (File.Exists(filename))
            {
                var creationTime = File.GetCreationTimeUtc(filename);
                File.Move(filename, $"{Path.GetFileNameWithoutExtension(filename)}_{creationTime:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(filename)}");
            }

            logWriter = File.CreateText(filename);
            logWriter.AutoFlush = autoFlush;
        }

        ~FileLogWriter()
        {
            logWriter.Close();
        }

        public void Write(LogCategory category, DateTime time, string message)
        {
            logWriter.WriteLine($"{time:G} | {category,-5} | {message}");
        }
    }
}
