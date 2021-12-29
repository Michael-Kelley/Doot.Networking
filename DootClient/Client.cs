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
        readonly Dictionary<string, Func<SessionBase, object[], object>> rpcFunctions;
        readonly CancellationToken cancellation;

        public Client() : base(new TcpClient())
        {
            rpcFunctions = new Dictionary<string, Func<SessionBase, object[], object>>();
            cancellation = new CancellationToken();

            SetRPCManager(this);
        }

        public void RegisterRPCFunction(string name, Func<SessionBase, object[], object> function)
        {
            rpcFunctions[name] = function;
        }

        public Func<SessionBase, object[], object> GetRPCFunction(string name)
        {
            return rpcFunctions[name];
        }

        public async Task Connect(string host, int port)
        {
            await client.ConnectAsync(host, port);
            stream = client.GetStream();

            _ = Task.Factory.StartNew(() => Receive(cancellation), CancellationToken.None);
        }

        public void Disconnect()
        {
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
