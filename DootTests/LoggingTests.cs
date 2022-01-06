using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;



namespace Doot.Tests
{
    [TestClass]
    public class LoggingTests
    {
        class TestLogWriter : ILogWriter
        {
            public Queue<(LogCategory Category, string Message)> Messages;

            public TestLogWriter()
            {
                Messages = new Queue<(LogCategory Category, string Message)>();
            }

            public void Write(LogCategory category, DateTime time, string message)
            {
                Messages.Enqueue((category, message));
            }
        }

        [TestMethod]
        public void LogCategory_Same()
        {
            var logCategory = LogCategory.Info;
            var logWriter = new TestLogWriter();
            var msgCategory = LogCategory.Info;
            var message = "test";

            Logger.SetLogCategory(logCategory);
            Logger.AddLogWriter(logWriter);
            Logger.Run();

            Logger.Log(msgCategory, message);
            Logger.Wait();

            Assert.AreEqual(logWriter.Messages.Count, 1);

            var (loggedCategory, loggedMessage) = logWriter.Messages.Dequeue();

            Assert.AreEqual(msgCategory, loggedCategory);
            Assert.AreEqual(message, loggedMessage);
        }

        [TestMethod]
        public void LogCategory_Lower()
        {
            var logCategory = LogCategory.Info;
            var logWriter = new TestLogWriter();
            var msgCategory = LogCategory.Warn;
            var message = "test";

            Logger.SetLogCategory(logCategory);
            Logger.AddLogWriter(logWriter);
            Logger.Run();

            Logger.Log(msgCategory, message);
            Logger.Wait();

            Assert.AreEqual(logWriter.Messages.Count, 1);

            var (loggedCategory, loggedMessage) = logWriter.Messages.Dequeue();

            Assert.AreEqual(msgCategory, loggedCategory);
            Assert.AreEqual(message, loggedMessage);
        }

        [TestMethod]
        public void LogCategory_Higher()
        {
            var logCategory = LogCategory.Info;
            var logWriter = new TestLogWriter();
            var msgCategory = LogCategory.Debug;
            var message = "test";

            Logger.SetLogCategory(logCategory);
            Logger.AddLogWriter(logWriter);
            Logger.Run();

            Logger.Log(msgCategory, message);
            Logger.Wait();

            Assert.AreEqual(logWriter.Messages.Count, 0);
        }
    }
}