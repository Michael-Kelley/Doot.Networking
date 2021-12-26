using System.Net.Sockets;



namespace Doot
{
    class Session : SessionBase
    {
        public Session(TcpClient client) : base(client)
        {
        }
    }
}
