using System.Net.Sockets;



namespace Doot
{
    class Session : SessionBase
    {
        public Server Server;

        public Session(TcpClient client, Server server) : base(client)
        {
            Server = server;
            SetRPCManager(server);
        }
    }
}
