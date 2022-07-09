using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Doot.Examples
{
    class ExampleApplication
    {
        public bool Finished { get; private set; } = false;

        public async Task Run()
        {
            await Task.Yield();

            var client = new Client();
            Console.WriteLine("Press any key to connect to server...");
            Console.ReadKey();
            await client.Connect("127.0.0.1", 0xD007);
            Logger.Log(LogCategory.Info, "Connected!");

            Logger.Log(LogCategory.Info, "Logging in...");
            string email = "notarealuser@email.com";
            string password = "notarealpassword";
            var userId = await client.LogIn(email, password);

            if (userId == 0)
            {
                Logger.Log(LogCategory.Info, "Account does not exist! Creating...");
                userId = await client.CreateAccount(email, password);
                Logger.Log(LogCategory.Info, $"Account created! ID: {userId}");
            }
            else if (userId == -1)
            {
                Logger.Log(LogCategory.Info, "Failed to log in. Incorrect password!");
            }
            else
            {
                Logger.Log(LogCategory.Info, $"Logged in! ID: {userId}");
            }

            client.ChatMessageReceived += (s, e) =>
            {
                Logger.Log(LogCategory.Info, $"[{e.RoomId}] <{e.UserId}> {e.Message}");
            };

            await client.JoinChatRoom("global");

            Console.WriteLine("Enter any message and hit enter to send a chat message to the global chat room. Type :q to exit.");

            string message;

            while ((message = await ReadLineAsync()) != ":q")
            {
                await client.SendChatMessage("global", message);
            }

            client.Disconnect();

            Finished = true;
        }

        async Task<string> ReadLineAsync()
        {
            var tcs = new TaskCompletionSource<string>();

            ThreadPool.QueueUserWorkItem((_tcs) =>
            {
                var line = Console.ReadLine();
                ((TaskCompletionSource<string>)_tcs).SetResult(line);
            }, tcs);

            return await tcs.Task;
        }
    }
}
