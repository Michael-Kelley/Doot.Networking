using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Doot
{
    public class Client : SessionBase, IRPCManager
    {
        internal readonly Dictionary<string, Func<SessionBase, object[], object>> RPCFunctions;

        readonly CancellationTokenSource cancellation;

        public Client() : base(new TcpClient())
        {
            RPCFunctions = new Dictionary<string, Func<SessionBase, object[], object>>();
            cancellation = new CancellationTokenSource();

            SetRPCManager(this);
        }

        public void RegisterRPCFunction(string name, Func<SessionBase, object[], object> function)
        {
            RPCFunctions[name] = function;
        }

        public Func<SessionBase, object[], object> GetRPCFunction(string name)
        {
            return RPCFunctions[name];
        }

        public async Task Connect(string host, int port)
        {
            await client.ConnectAsync(host, port);

            if (client.Connected)
                State = SessionState.Connected;

            stream = client.GetStream();

            _ = Task.Factory.StartNew(() => Receive(cancellation.Token), CancellationToken.None);
        }

        public void Disconnect()
        {
            cancellation.Cancel();
            client.Close();
        }

        public async Task<long> LogIn(string email, string password)
        {
            return (long)await CallRemoteProcedure("log_in", email, password);
        }

        public async Task<long> CreateAccount(string email, string password)
        {
            return (long)await CallRemoteProcedure("create_account", email, password);
        }
    }
}
