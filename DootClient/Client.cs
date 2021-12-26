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
        readonly Dictionary<string, Func<object[], object>> rpcFunctions;
        readonly CancellationToken cancellation;

        public Client() : base(new TcpClient())
        {
            rpcFunctions = new Dictionary<string, Func<object[], object>>();
            cancellation = new CancellationToken();

            SetRPCManager(this);
        }

        public void RegisterRPCFunction(string name, Func<object[], object> function)
        {
            rpcFunctions[name] = function;
        }

        public Func<object[], object> GetRPCFunction(string name)
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

        public async Task<ulong> CallTestFunc()
        {
            return (ulong)await CallRemoteProcedure("test_func");
        }

        public async Task<double> CallAnotherTestFunc(long arg1, double arg2, string arg3)
        {
            return (double)await CallRemoteProcedure("another_test_func", arg1, arg2, arg3);
        }
    }
}
