using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net.WebSockets;
using Ninja.WebSockets;
namespace DiscordInjector
{
    class ElectronRemoteDebugger
    {
        private string host;
        private int port;

        public ElectronRemoteDebugger(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public List<Tuple<Uri, string>> windows()
        {
            string host = this.host;
            int port = this.port;
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;

            var ret = new List<Tuple<Uri, string>>();
            var resp = requests_get($"http://{host}:{port}/json/list?t={secondsSinceEpoch}");
            if (resp is JArray)
            {
                foreach (var W in resp.Values<JObject>())
                {
                    string url = W.ContainsKey("webSocketDebuggerUrl") ? W["webSocketDebuggerUrl"].ToString() : null;
                    string windowId = W.ContainsKey("id") ? W["id"].ToString() : null;
                    if (url == null || windowId == null)
                        continue;
                    var debugUri = new Uri(url);
                    var tupleObj = Tuple.Create(debugUri, windowId);
                    ret.Add(tupleObj);
                }
            }
            return ret;
        }

        private async Task<List<string>> Receive(WebSocket webSocket)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            var bufferList = new List<string>();

            while (true)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        break;
                    case WebSocketMessageType.Text:

                    case WebSocketMessageType.Binary:
                        string value = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        bufferList.Add(value);
                        break;
                }
                return bufferList;

            }
        }

            async Task Send(WebSocket webSocket, string expression)
            {
                var array = Encoding.UTF8.GetBytes(expression);
                var buffer = new ArraySegment<byte>(array);
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }


            async Task<string> sendrcv(Uri webSocketUri, string expression)
            {
                var factory = new WebSocketClientFactory();
                using (WebSocket webSocket = await factory.ConnectAsync(webSocketUri))
                {
                    try
                    {
                        // receive loop
                        var readTask = Receive(webSocket);

                        // send a message
                        await Send(webSocket, expression);

                        // initiate the close handshake
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

                        // wait for server to respond with a close frame
                        List<string> returnedData = await readTask;
                        return String.Join("", returnedData);
                    }
                    catch (WebSocketException)
                    {
                        return null;
                    }
                }
            }

            public async Task<JObject> eval(Uri webSocketUri, string expression)
            {
                var dataDict = new Dictionary<string, dynamic>
                {
                    { "id", 1 },
                    { "method", "Runtime.evaluate" },
                    { "params", new Dictionary<string, dynamic>
                        {
                            { "contextId",  1 },
                            { "doNotPauseOnExceptionsAndMuteConsole", false },
                            { "expression", expression },
                            { "gneratePreview", false },
                            { "includeCommandLineAPI", true },
                            { "objectGroup", "console" },
                            { "returnByValue", false },
                            { "userGesture", true }
                        }
                    }
                };

                var serializedData = JsonConvert.SerializeObject(dataDict);
                var receivedData = await sendrcv(webSocketUri, serializedData);

                if (receivedData == null)
                    return null;

                JObject deserializedObject = JObject.Parse(receivedData);
                if (!deserializedObject.ContainsKey("result"))
                    return deserializedObject;
                else if (deserializedObject["result"].ToObject<JObject>().ContainsKey("wasThrown"))
                    throw new Exception(deserializedObject["result"]["result"].ToString());
                else
                    return deserializedObject["result"].ToObject<JObject>();
            }

            JArray requests_get(string url, int tries = 5, int delay = 1)
            {
                Exception last_exception = null;
                var objectList = new List<JObject>();

                for (int i = 1; i < tries; i++)
                {
                    try
                    {
                        var client = new WebClient();
                        string response = client.DownloadString(url);
                        var jsonResp = JArray.Parse(response);
                        return jsonResp;
                    }
                    catch (Exception ex)
                    {
                        last_exception = ex;
                    }
                    Thread.Sleep(delay * 1000);
                }
                throw last_exception;
            }
        }
    }
