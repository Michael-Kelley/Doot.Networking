using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;



namespace Doot
{
    public class Server
    {
        readonly TcpListener listener;
        readonly CancellationToken cancellation;
        readonly List<Session> sessions;

        public Server(IPAddress bindAddress, int port)
        {
            listener = new TcpListener(bindAddress, port);
            cancellation = new CancellationToken();
            sessions = new List<Session>();
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

                var session = new Session(client);
                sessions.Add(session);

                _ = Task.Factory.StartNew(() => session.Receive(cancellation), CancellationToken.None);
            }
        }
    }
}
