using System;



namespace Doot
{
    public interface ILogWriter
    {
        void Write(LogCategory category, DateTime time, string message);
    }
}
