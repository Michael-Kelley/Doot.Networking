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
        readonly Dictionary<string, Match> matches;
        readonly Dictionary<string, ChatRoom> chatRooms;

        public Server(IPAddress bindAddress, int port)
        {
            listener = new TcpListener(bindAddress, port);
            cancellation = new CancellationTokenSource();
            sessions = new List<SessionBase>();
            rpcFunctions = new Dictionary<string, Func<SessionBase, object[], object>>();
            StartedEvent = new ManualResetEvent(false);
            stoppedEvent = new ManualResetEvent(false);
            matches = new Dictionary<string, Match>();
            chatRooms = new Dictionary<string, ChatRoom>();
            Database = new DatabaseManager();

            RegisterRPCFunction("log_in", RPC.LogIn);
            RegisterRPCFunction("create_account", RPC.CreateAccount);
            RegisterRPCFunction("join_match", RPC.JoinMatch);
            RegisterRPCFunction("create_match", RPC.CreateMatch);
            RegisterRPCFunction("join_chatroom", RPC.JoinChatRoom);
            RegisterRPCFunction("send_chat_message", RPC.SendChatMessage);

            CreateMatch("map_TestMap");

            CreateChatRoom("global");
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

                var session = new Session(client, Guid.NewGuid().ToString(), this);
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

        public bool CreateMatch(string id)
        {
            if (matches.ContainsKey(id))
                return false;

            matches[id] = new Match(id);

            return true;
        }

        public bool AddSessionToMatch(string matchId, Session session)
        {
            if (!matches.ContainsKey(matchId))
                return false;

            matches[matchId].Sessions.Add(session);

            return true;
        }

        public void RemoveSessionFromMatch(string matchId, Session session)
        {
            matches[matchId].Sessions.Remove(session);
        }

        public bool CreateChatRoom(string id)
        {
            if (chatRooms.ContainsKey(id))
                return false;

            chatRooms[id] = new ChatRoom(id);

            return true;
        }

        public bool AddSessionToChatRoom(string roomId, Session session)
        {
            if (!chatRooms.ContainsKey(roomId))
                return false;

            chatRooms[roomId].Sessions.Add(session);
            session.JoinedChatRooms[roomId] = chatRooms[roomId];

            return true;
        }

        public void RemoveSessionFromChatRoom(string roomId, Session session)
        {
            chatRooms[roomId].Sessions.Remove(session);
            session.JoinedChatRooms.Remove(roomId);
        }
    }
}
