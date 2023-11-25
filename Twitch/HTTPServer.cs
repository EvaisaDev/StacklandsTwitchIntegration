using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Evaisa.Twitch
{
	public class HTTPServer
	{
		static  int Port = 21412;
		static HttpListener listener;

		public bool Terminate;
		System.Timers.Timer scheduledTermination;
		IAsyncResult result;
        BepInEx.Logging.ManualLogSource logger;
        public static bool TwitchAuthed = false;

        string twitchAuthPath = "";



		public static List<string> scopes = new List<string>
		{
			"user_read",
			"channel:read:subscriptions",
			"chat:read",
			"chat:edit",
			"bits:read",
			"channel:read:redemptions",
			"channel:manage:redemptions",
			"channel:read:predictions",
			"channel:manage:predictions",
		};

		public delegate void OnReceivedResultDelegate(string token, string scope, string tokentype);
		public static event OnReceivedResultDelegate OnReceivedResultEvent;

		public static bool IsSupported() => HttpListener.IsSupported;


        private const string HttpRedirect =
    @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <title>Twitch Token Auth Redirection</title>
            </head>
            <body>
                <h1 style=""color: #000000"">You can close this and go back to the game now!</h1>
                <noscript>
                    <h1>You must have javascript enabled for OAuth redirection to work!</h1>
                </noscript>
                <script lang=""javascript"">
                    let req = new XMLHttpRequest();
                    req.open('POST', '/', false);
                    req.setRequestHeader('Content-Type', 'text');
                    req.send(document.location.hash);
                    window.close();
                </script>
            </body>
            </html>
            ";

        public HTTPServer(string clientID)
		{

            if(logger == null)
            {
                logger = new ManualLogSource("HTTPServer");
            }

            var tokenScope = Authorize(clientID);

            if(tokenScope != null)
            {
                TwitchManager.Print("Token received: "+ tokenScope.token);
                OnReceivedResultEvent.Invoke(tokenScope.token, tokenScope.scope, "access_token");
            }

        }

        public class TokenScope
        {
            public string token;
            public string scope;
        }


        public static TokenScope Authorize(string clientID)
        {
            // Make sure we close before we try again
            listener?.Close();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");
            listener.Start();

            string scopeStr = string.Join(" ", scopes);
            Process.Start("https://id.twitch.tv/oauth2/authorize" +
                "?response_type=token" +
                "&client_id=" + clientID +
                "&redirect_uri=" + $"http://localhost:{Port}/auth/" +
                "&force_verify=true" +
                "&scope=" +
                string.Join(" ", HTTPServer.scopes));



            while (listener.IsListening)
            {
                var context = listener.GetContext();
                var request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    string text;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        text = reader.ReadToEnd();
                    }

                    listener.Close();

                    var keyValuePairs = WebUtility.UrlDecode(text)
                        .Split('&')
                        .Select(kv => kv.Split('='))
                        .ToDictionary(kv => kv[0].Replace("#", ""), kv => kv[1]);
                    if (keyValuePairs.TryGetValue("access_token", out var accessToken))
                    {
                        return new TokenScope { token = accessToken, scope = string.Join(" ", HTTPServer.scopes)};
                    }
                }
                else
                {
                    var response = context.Response;
                    byte[] buffer = Encoding.UTF8.GetBytes(HttpRedirect);
                    response.ContentLength64 = buffer.Length;
                    var output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }

            return null;
        }

        public static void CancelAuth() => listener?.Close();

        public static bool ValidateToken(string token)
        {
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
                var response = client.GetStringAsync("https://id.twitch.tv/oauth2/validate");
                response.Wait();

                if (JObject.Parse(response.Result).ContainsKey("client_id"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetUserID(string token, string ClientID)
        {
            try
            {
                var client = new HttpClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                client.DefaultRequestHeaders.Add("Client-Id", ClientID);
                var response = client.GetStringAsync("https://api.twitch.tv/helix/users");

                response.Wait();

                TwitchManager.Print(response.Result);

                dynamic myObject = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Result);

                var id = myObject["data"][0]["id"];

                TwitchManager.Print(id);

                return id;

            }
            catch
            {

            }
            return "";
        }


        public void CloseHttpListener()
		{
			if (listener != null && listener.IsListening)
				listener.Close();

			if (scheduledTermination != null)
			{
				scheduledTermination.Stop();
				scheduledTermination.Dispose();
			}
		}
	}

	public class FailedToGetProperResponseException : Exception
	{
		public FailedToGetProperResponseException() : base() { }

		public FailedToGetProperResponseException(string message) : base(message) { }
		public FailedToGetProperResponseException(string message, Exception innerException) : base(message, innerException) { }
	}
}
