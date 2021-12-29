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
        internal readonly DatabaseManager Database;

        readonly TcpListener listener;
        readonly CancellationToken cancellation;
        readonly List<SessionBase> sessions;
        readonly Dictionary<string, Func<SessionBase, object[], object>> rpcFunctions;

        public Server(IPAddress bindAddress, int port)
        {
            listener = new TcpListener(bindAddress, port);
            cancellation = new CancellationToken();
            sessions = new List<SessionBase>();
            rpcFunctions = new Dictionary<string, Func<SessionBase, object[], object>>();
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
            Logger.Log(LogCategory.Information, "Server started");

            for (; ; )
            {
                var client = await listener.AcceptTcpClientAsync(cancellation);

                if (client == null)
                    break;

                Logger.Log(LogCategory.Debug, "Client connected");

                var session = new Session(client, this);
                sessions.Add(session);

                _ = Task.Factory.StartNew(() => session.Receive(cancellation), CancellationToken.None);
            }
        }
    }
}
