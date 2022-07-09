using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot
{
    static class RPC
    {
        public static object OnChatMessage(SessionBase session, object[] arguments)
        {
            /// arguments: [string roomId, string userId, string message]
            /// return: void

            var (roomId, userId, message) = arguments.ToValueTuple<string, string, string>();
            var client = (Client)session;

            client.OnChatMessageReceived(userId, roomId, message);

            return null;
        }
    }
}
