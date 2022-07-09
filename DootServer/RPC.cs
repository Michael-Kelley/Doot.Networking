using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot
{
    static class RPC
    {
        public static object LogIn(SessionBase session, object[] arguments)
        {
            /// arguments: [string email, string password]
            /// return: long account_id/account_status

            var (email, password) = arguments.ToValueTuple<string, string>();
            var server = ((Session)session).Server;

            var id = server.Database.LogIn(email, password);

            if (id > 0)
                session.State = SessionState.LoggedIn;

            return id;
        }

        public static object CreateAccount(SessionBase session, object[] arguments)
        {
            /// arguments: [string email, string password]
            /// return: long account_id

            var (email, password) = arguments.ToValueTuple<string, string>();
            var server = ((Session)session).Server;

            var id = server.Database.CreateAccount(email, password);

            if (id > 0)
                session.State = SessionState.LoggedIn;

            return id;
        }

        public static object JoinMatch(SessionBase session, object[] arguments)
        {
            /// arguments: [string matchId]
            /// return: bool

            var matchId = (string)arguments[0];
            var _session = (Session)session;
            var server = _session.Server;

            return server.AddSessionToMatch(matchId, _session);
        }

        public static object CreateMatch(SessionBase session, object[] arguments)
        {
            /// arguments: [string matchId]
            /// return: bool
            
            var matchId = (string)arguments[0];
            var _session = (Session)session;
            var server = _session.Server;

            return server.CreateMatch(matchId);
        }

        public static object JoinChatRoom(SessionBase session, object[] arguments)
        {
            /// arguments: [string roomId]
            /// return: bool

            var roomId = (string)arguments[0];
            var _session = (Session)session;
            var server = _session.Server;

            if (_session.JoinedChatRooms.ContainsKey(roomId))
                return false;

            return server.AddSessionToChatRoom(roomId, _session);
        }

        public static object SendChatMessage(SessionBase session, object[] arguments)
        {
            /// arguments: [string roomId, string message]
            /// return: bool

            var (roomId, message) = arguments.ToValueTuple<string, string>();
            var _session = (Session)session;

            if (!_session.JoinedChatRooms.ContainsKey(roomId))
                return false;

            _session.JoinedChatRooms[roomId].SendMessage(_session, message);

            return true;
        }
    }
}
