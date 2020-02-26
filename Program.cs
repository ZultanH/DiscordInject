using Nito.AsyncEx;

namespace DiscordConnect
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            var ERD = new ElectronRemoteDebugger("localhost", 9222);
            await DiscordWebsocket.Inject(ERD, "console.log('AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA')");
        }
    }
}
