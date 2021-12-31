using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;



namespace Doot.Tests
{
    static class ConnectionTests
    {
        [Test]
        public static async Task ClientConnectTest()
        {
            var server = new Server(IPAddress.Loopback, 0xD007);
            _ = server.Run();
            server.StartedEvent.WaitOne();

            var client = new Client();
            await client.Connect("127.0.0.1", 0xD007);

            Assert.Equal(client.State, SessionState.Connected);

            client.Disconnect();
            server.Stop();
        }

        //[Test]
        //public static async Task ConnectionTimeoutTest()
        //{
        //    var client = new Client();
        //    await client.Connect("127.0.0.1", 0xD007);
        //    Assert.Equal(client.State, SessionState.Connected);
        //    client.Disconnect();
        //}
    }
}
