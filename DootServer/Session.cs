using System.Net.Sockets;



namespace Doot
{
    public class Session : SessionBase
    {
        public string Id;
        public Server Server;
        public Dictionary<string, ChatRoom> JoinedChatRooms;

        public Session(TcpClient client, string id, Server server) : base(client)
        {
            Id = id;
            Server = server;
            JoinedChatRooms = new Dictionary<string, ChatRoom>();
            SetRPCManager(server);
        }

        public async Task OnChatMessage(string roomId, string userId, string message)
        {
            await CallRemoteProcedure("on_chat_message", roomId, userId, message);
        }
    }
}
