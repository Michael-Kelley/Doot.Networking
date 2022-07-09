using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot
{
    public class ChatRoom
    {
        public string ID;
        public List<Session> Sessions;

        public ChatRoom(string id)
        {
            ID = id;
            Sessions = new List<Session>();
        }

        public void SendMessage(Session session, string message)
        {
            foreach (var s in Sessions)
            {
                s.OnChatMessage(ID, session.Id, message);
            }
        }
    }
}
