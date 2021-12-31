using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;



namespace Doot
{
    public class Server : IRPCManager
    {
        public readonly ManualResetEvent StartedEvent;

        internal readonly DatabaseManager Database;

        readonly TcpListener listener;
        readonly CancellationTokenSource cancellation;
        readonly List<SessionBase> sessions;
        readonly Dictionary<string, Func<SessionBase, object[], object>> rpcFunctions;
        readonly ManualResetEvent stoppedEvent;

        public Server(IPAddress bindAddress, int port)
        {
            listener = new TcpListener(bindAddress, port);
            cancellation = new CancellationTokenSource();
            sessions = new List<SessionBase>();
            rpcFunctions = new Dictionary<string, Func<SessionBase, object[], object>>();
            StartedEvent = new ManualResetEvent(false);
            stoppedEvent = new ManualResetEvent(false);
            Database = new DatabaseManager();

            RegisterRPCFunction("log_in", RPC.LogIn);
            RegisterRPCFunction("create_account", RPC.CreateAccount);
        }

        public void RegisterRPCFunction(string name, Func<SessionBase, object[], object> function)
        {
            rpcFunctions[name] = function;
        }

        public Func<SessionBase, object[], object> GetRPCFunction(string name)
        {
            return rpcFunctions[name];
        }

        public async Task Run()
        {
            listener.Start();
            Logger.Log(LogCategory.Info, "Server started");
            StartedEvent.Set();

            for (; ; )
            {
                TcpClient client;

                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellation.Token);
                }
                catch (Exception)
                {
                    break;
                }

                if (client == null)
                    break;

                Logger.Log(LogCategory.Debug, "Client connected");

                var session = new Session(client, this);
                sessions.Add(session);

                session.ReceiveLoop(cancellation.Token);
            }

            listener.Stop();
            stoppedEvent.Set();
        }

        public void Stop()
        {
            cancellation.Cancel();
            stoppedEvent.WaitOne();
        }
    }
}
