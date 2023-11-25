using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Unity;
using UnityEngine;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using TwitchLib.Client.Models;
using System.Collections;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using Newtonsoft.Json;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;



namespace Evaisa.Twitch
{
    public class TwitchManager
    {
        public static string clientId;
        public static string authToken;
        public static bool tokenInitialized;
        public static string channelId;

        public static Api api;
        public static PubSub pubSub;
        public static Client client;

        static HTTPServer server;

        static ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "evaisa.twitch.cfg"), true);

        public static ConfigEntry<string> clientIDConfig { get; set; }
        public static ConfigEntry<string> userNameConfig { get; set; }
        public static ConfigEntry<bool> CreateRewardsOnStartupConfig { get; set; }
        public static ConfigEntry<string> authTokenConfig { get; set; }


        public static Action rewardValidCheck;

        public static void Initialize()
        {
            Utils.IlLine.init();

            userNameConfig = config.Bind("General",
            "Twitch Username",
            "evaisie",
            "Your twitch username.");

            CreateRewardsOnStartupConfig = config.Bind("General",
            "Create Rewards On Startup",
            true,
            "Create missing rewards on startup, note that the bot needs to be the creator of the rewards to be able to manage them.");


            clientIDConfig = config.Bind("General",
            "Client ID",
            "5xa3hc8z7ddclk3er42z0d8oginbrs",
            "Client ID of twitch application, you can change this if you want to use your own.");

            authTokenConfig = config.Bind("Data",
            "Auth Token",
            "",
            "Leave this empty, this will be generated once you start the game.");

            clientId = clientIDConfig.Value;

            rewardValidCheck += () => {};

        }

        public static void Connect()
        {
            api = new Api();
            api.Settings.ClientId = clientIDConfig.Value;
            api.Settings.AccessToken = authToken;


            channelId = HTTPServer.GetUserID(authToken, clientId);

            pubSub = new PubSub();

            pubSub.OnLog += (sender, e) => {
                Print($"[Twitch PubSub] {e.Data}");
            };

            pubSub.OnPubSubServiceError += (sender, e) =>
            {
                Print($"[Twitch PubSub] ERROR: {e.Exception}");
            };
            pubSub.OnPubSubServiceClosed += (sender, e) =>
            {
                Print($"[Twitch PubSub] Connection closed");
            };
            pubSub.OnListenResponse += (sender, e) =>
            {
                if (e.Successful)
                {
                    Print($"OnListenResponse (success): {e.Response.Nonce}");
                }
                else
                {
                    Print($"OnListenResponse (error): {e.Response.Error}");
                }
            };
            pubSub.OnChannelPointsRewardRedeemed += (sender, e) =>
            {
                Print($"Reward redeemed: {e.RewardRedeemed.Redemption.Reward.Title}");
            };

            pubSub.OnPubSubServiceConnected += (sender, e) =>
            {
                pubSub.ListenToChannelPoints(channelId);
                //pubSub.ListenToBitsEventsV2(userNameConfig.Value);
                pubSub.SendTopics(authToken);
            };

            pubSub.Connect();

            ConnectionCredentials credentials = new ConnectionCredentials(userNameConfig.Value, authToken);
            client = new Client();
            client.Initialize(credentials, userNameConfig.Value);

            client.OnConnected += OnConnected;
            client.OnJoinedChannel += OnJoinedChannel;
            client.OnMessageReceived += OnMessageReceived;

            client.Connect();
        }

        public static void Update()
        {
            if(tokenInitialized == false)
            {
                
                tokenInitialized = true;
            }
            
            rewardValidCheck.Invoke();
        }

        private static void OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Print($"The bot {e.BotUsername} succesfully connected to Twitch.");

            if (!string.IsNullOrWhiteSpace(e.AutoJoinChannel))
                Print($"The bot will now attempt to automatically join the channel provided when the Initialize method was called: {e.AutoJoinChannel}");
        }

        private static void OnJoinedChannel(object sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            Print($"The bot {e.BotUsername} just joined the channel: {e.Channel}");
        }

        private static void OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            Print($"Message received from {e.ChatMessage.Username}: {e.ChatMessage.Message}");
        }


        public static CustomReward[] GetChannelPointRewards()
        {
            if(channelId != null && channelId != "") {
                var task = api.Helix.ChannelPoints.GetCustomReward(channelId, onlyManageableRewards: true);
                task.Wait();
                return task.Result.Data;
            }
            return null;
        }

        public static bool HasRewardAccess(string reward_name)
        {
            var rewards = GetChannelPointRewards();
            if(rewards != null)
            {
                foreach(var reward in rewards)
                {
                    if(reward.Title == reward_name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static CustomReward GetChannelPointReward(string reward_name)
        {
            var rewards = GetChannelPointRewards();
            if (rewards != null)
            {
                foreach (var reward in rewards)
                {
                    if (reward.Title == reward_name)
                    {
                        return reward;
                    }
                }
            }
            return null;
        }


        public static CustomReward CreateChannelPointReward(string reward_name, bool require_message = false)
        {
            if (channelId != null && channelId != "")
            {
                var task = api.Helix.ChannelPoints.CreateCustomRewards(channelId, new CreateCustomRewardsRequest { Title = reward_name, IsEnabled = false, Cost = 10, IsUserInputRequired = require_message});
                task.Wait();
                
                return task.Result.Data[0];
            }
            return null;
        }

        public static void ToggleChannelPointReward(string id, bool enabled)
        {
            if (channelId != null && channelId != "" && id != null && id != "")
            {
                var task = Task.Factory.StartNew(() =>
                {
                    var task = api.Helix.ChannelPoints.UpdateCustomReward(channelId, id, new UpdateCustomRewardRequest { IsEnabled = enabled });
                    task.Wait();
                });
            }
        }

        public class Reward{
            public string title;
            public string id;
            public bool enabled;

            public Reward(string title, string id, bool enabled)
            {
                this.title = title;
                this.id = id;
                this.enabled = enabled;
            }
        }

        public static Dictionary<string, Reward> registeredRewards = new Dictionary<string, Reward>();

        public static void RegisterReward(string rewardID, string rewardTitle, bool requireMessage, Action<object, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs> action, Func<bool> enableCheck)
        {
            if (rewardTitle == "") return;


            if (HasRewardAccess(rewardTitle))
            {

                if (!registeredRewards.ContainsKey(rewardID))
                {
                    var reward = GetChannelPointReward(rewardTitle);
                    var newReward = new Reward(rewardTitle, reward.Id, false);

                    ToggleChannelPointReward(reward.Id, false);
                    Print($"Disabled reward: {rewardID} ({rewardTitle})");

                    registeredRewards.Add(rewardID, newReward);
                    Print($"Reward found: {rewardID} ({rewardTitle})");
                }
                
                
            }
            else
            {
                if (!registeredRewards.ContainsKey(rewardTitle))
                {
                    var reward = CreateChannelPointReward(rewardTitle, requireMessage);
                    var newReward = new Reward(rewardTitle, reward.Id, false);
                    registeredRewards.Add(rewardID, newReward);

                    Print($"Reward created: {rewardID} ({rewardTitle})");
                }
            }


 
            rewardValidCheck += () =>
            {
                if(enableCheck.Invoke())
                {
                    if (!registeredRewards[rewardID].enabled)
                    {
                        registeredRewards[rewardID].enabled = true;
                        ToggleChannelPointReward(registeredRewards[rewardID].id, true);
                        Print($"Enabled reward: {rewardID} ({rewardTitle})");
                    }
                }
                else{
                    if (registeredRewards[rewardID].enabled)
                    {
                        registeredRewards[rewardID].enabled = false;
                        ToggleChannelPointReward(registeredRewards[rewardID].id, false);
                        Print($"Disabled reward: {rewardID} ({rewardTitle})");
                    }
                }
            };


            pubSub.OnChannelPointsRewardRedeemed += (object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) =>
            {
                Print($"{e.RewardRedeemed.Redemption.Reward.Title} == {rewardTitle}");

                if (e.RewardRedeemed.Redemption.Reward.Title == rewardTitle)
                {
                    action(sender, e);
                }
            };
        }

        public static void Print(string data)
        {
            UnityEngine.Debug.Log("[Evaisa.Twitch]: " + data);
        }
        public static void Print(object data)
        {
            UnityEngine.Debug.Log("[Evaisa.Twitch]: "+data.ToString());
        }

        public static void Print(List<object> data)
        {
            var printString = "[Evaisa.Twitch]: ";
            foreach (object item in data)
            {
                printString += data.ToString();
            }

            UnityEngine.Debug.Log(printString);

        }

        private static void Server_OnReceivedResultEvent(string token, string scope, string tokentype)
        {
            if (token != "")
            {
                authToken = token;
                tokenInitialized = true;
                authTokenConfig.Value = authToken;

                config.Save();
                Print("TwitchLib initialized!");
            }
        }

        public static void RefreshAccessToken(string client_id)
        {
            if (authTokenConfig.Value == "") { 
                if (HTTPServer.IsSupported())
                {
                    if (server != null)
                    {
                        HTTPServer.OnReceivedResultEvent -= Server_OnReceivedResultEvent;
                        server.CloseHttpListener();
                    }
                    HTTPServer.OnReceivedResultEvent += Server_OnReceivedResultEvent;
                    server = new HTTPServer(client_id);

                }
            }else{
                var tokenValid = HTTPServer.ValidateToken(authTokenConfig.Value);

                if (tokenValid)
                {
                    Server_OnReceivedResultEvent(authTokenConfig.Value, string.Join(" ", HTTPServer.scopes), "access_token");
                }
                else
                {
                    if (HTTPServer.IsSupported())
                    {
                        if (server != null)
                        {
                            HTTPServer.OnReceivedResultEvent -= Server_OnReceivedResultEvent;
                            server.CloseHttpListener();
                        }
                        HTTPServer.OnReceivedResultEvent += Server_OnReceivedResultEvent;
                        server = new HTTPServer(client_id);

                    }
                }
            }
        }

    }
}
