﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;



namespace Doot
{
    public static class Logger
    {
        static LogCategory logCategory = LogCategory.Information;
        static readonly List<ILogWriter> logWriters = new List<ILogWriter>();
        static readonly ConcurrentQueue<(LogCategory Category, DateTime Time, string Message)> queuedMessages = new ConcurrentQueue<(LogCategory, DateTime, string)>();
        static readonly AutoResetEvent logEvent = new AutoResetEvent(false);

        public static void SetLogCategory(LogCategory value)
        {
            logCategory = value;
        }

        public static void AddLogWriter(ILogWriter writer)
        {
            logWriters.Add(writer);
        }

        public static void Log(LogCategory category, string message)
        {
            if (category > logCategory)
                return;

            queuedMessages.Enqueue((category, DateTime.Now, message));
            logEvent.Set();
        }

        public static void Run()
        {
            _ = Task.Factory.StartNew(() => WriteMessages(), TaskCreationOptions.LongRunning);
        }

        static void WriteMessages()
        {
            logEvent.WaitOne();

            while (queuedMessages.TryDequeue(out var message))
            {
                foreach (var w in logWriters)
                    w.Write(message.Category, message.Time, message.Message);
            }

            _ = Task.Factory.StartNew(() => WriteMessages(), TaskCreationOptions.LongRunning);
        }
    }
}
