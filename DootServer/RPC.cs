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
    }
}
