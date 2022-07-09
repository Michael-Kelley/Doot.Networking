using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Doot.Examples
{
    static class ClientExtensions
    {
        public static async Task<ulong> CallTestFunc(this Client client)
        {
            return (ulong)await client.CallRemoteProcedure("test_func");
        }

        public static async Task<double> CallAnotherTestFunc(this Client client, long arg1, double arg2, string arg3)
        {
            return (double)await client.CallRemoteProcedure("another_test_func", arg1, arg2, arg3);
        }
    }
}
