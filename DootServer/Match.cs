using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot
{
    class Match
    {
        public string ID;
        public List<Session> Sessions;

        public Match(string id)
        {
            ID = id;
            Sessions = new List<Session>();
        }
    }
}
