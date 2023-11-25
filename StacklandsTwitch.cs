using System;
using BepInEx;
using UnityEngine;
using System;
using System.Diagnostics;
using BepInEx.Configuration;
using System.Globalization;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using Random = UnityEngine.Random;

namespace Evaisa.StacklandsTwitch
{
    [BepInPlugin(GUID, ModName, Version)]
    public class StacklandsTwitch : BaseUnityPlugin
    {
        public const string GUID = "evaisa.stacklandstwitch";
        public const string ModName = "StacklandsTwitch";
        public const string Version = "1.0.0";


        public StacklandsTwitch instance;
        public static ConfigEntry<string> BoosterPack { get; set; }
        public static ConfigEntry<string> RandomCard { get; set; }
        public static ConfigEntry<string> SpawnEnemy { get; set; }
        public static ConfigEntry<string> Gluttony { get; set; }
        public static ConfigEntry<string> SpawnFood { get; set; }
        public static ConfigEntry<string> SpawnAnimal { get; set; }
        public static ConfigEntry<string> DamageEnemy { get; set; }
        public static ConfigEntry<string> Kleptomaniac { get; set; }
        public static ConfigEntry<string> StealResource { get; set; }

        public static bool DebugMode = true;

        void Awake()
        {
            instance = this;

            SetupConfigs();

            Evaisa.Twitch.TwitchManager.Initialize();

            On.WorldManager.Awake += WorldManager_Awake;

            SetupChannelPoints();
        }

        private void WorldManager_Awake(On.WorldManager.orig_Awake orig, WorldManager self)
        {
            orig(self);
            self.BoosterPackPrefabs.ForEach(p =>
            {
                Print($"{p.Name} ({p.BoosterId})");
            });
        }

        void Update()
        {
            Evaisa.Twitch.TwitchManager.Update();
        }

        public static void Print(string data)
        {
            UnityEngine.Debug.Log("[Evaisa.StacklandsTwitch]: " + data);
        }
        public static void Print(object data)
        {
            UnityEngine.Debug.Log("[Evaisa.StacklandsTwitch]: " + data.ToString());
        }

        public static void Print(List<object> data)
        {
            var printString = "[Evaisa.StacklandsTwitch]: ";
            foreach (object item in data)
            {
                printString += data.ToString();
            }

            UnityEngine.Debug.Log(printString);

        }

        void SetupConfigs()
        {
            BoosterPack = Config.Bind("Twitch Channel Point Rewards", "BoosterPack", "", "[Case Sensitive!] Title of the reward on twitch.");
            RandomCard = Config.Bind("Twitch Channel Point Rewards", "RandomCard", "", "[Case Sensitive!] Title of the reward on twitch.");
            SpawnEnemy = Config.Bind("Twitch Channel Point Rewards", "SpawnEnemy", "", "[Case Sensitive!] Title of the reward on twitch.");
            Gluttony = Config.Bind("Twitch Channel Point Rewards", "Gluttony", "", "[Case Sensitive!] Title of the reward on twitch.");
            SpawnFood = Config.Bind("Twitch Channel Point Rewards", "SpawnFood", "", "[Case Sensitive!] Title of the reward on twitch.");
            SpawnAnimal = Config.Bind("Twitch Channel Point Rewards", "SpawnAnimal", "", "[Case Sensitive!] Title of the reward on twitch.");
            DamageEnemy = Config.Bind("Twitch Channel Point Rewards", "DamageEnemy", "", "[Case Sensitive!] Title of the reward on twitch.");
            Kleptomaniac = Config.Bind("Twitch Channel Point Rewards", "Kleptomaniac", "", "[Case Sensitive!] Title of the reward on twitch.");
            StealResource = Config.Bind("Twitch Channel Point Rewards", "StealResource", "", "[Case Sensitive!] Title of the reward on twitch.");
        }

        void SetupChannelPoints()
        {

            Twitch.TwitchManager.RegisterReward("BoosterPack", BoosterPack.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing) {
                    List<Boosterpack> unlockedPacks = WorldManager.instance.BoosterPackPrefabs.FindAll(x => x.IsUnlocked || x.BoosterId == "basic");

                    if(unlockedPacks.Count > 0)
                    {
                        var pack = unlockedPacks[GetRandomNumber(0, unlockedPacks.Count)];

                        
                        BuyBoosterBox buyBoosterBox = FindObjectsOfType<BuyBoosterBox>().FirstOrDefault(x => x.BoosterId == pack.BoosterId);

                        WorldManager.instance.CreateBoosterpack(buyBoosterBox.transform.position, pack.BoosterId).Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * 4.5f));

                        GameScreen.instance.AddNotification("Reward Redeemed", $"{e.RewardRedeemed.Redemption.User.DisplayName} gifted you a {buyBoosterBox.NameText.text}!");
                    }
                }
            }, delegate () { 
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.BoosterPackPrefabs.FindAll(x => x.IsUnlocked || x.BoosterId == "basic").Count > 0; 
            });

            Twitch.TwitchManager.RegisterReward("RandomCard", RandomCard.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    List<Boosterpack> unlockedPacks = WorldManager.instance.BoosterPackPrefabs.FindAll(x => x.IsUnlocked || x.BoosterId == "basic");

                    if (unlockedPacks.Count > 0)
                    {
                        var pack = unlockedPacks[GetRandomNumber(0, unlockedPacks.Count)];


                        BuyBoosterBox buyBoosterBox = FindObjectsOfType<BuyBoosterBox>().FirstOrDefault(x => x.BoosterId == pack.BoosterId);

                        var bags = WorldManager.instance.GetBoosterPrefab(pack.BoosterId).CardBags.FindAll(x => x.CardsInPack > 0);

                        if (bags.Count > 0) {
                            var bag = bags[Random.Range(0, bags.Count)];

                            var card = bag.GetCard(false);


                            var cardData = WorldManager.instance.CreateCard(buyBoosterBox.transform.position, card);
                            
                            cardData.MyGameCard.Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * 4.5f));
                            cardData.MyGameCard.RotWobble(1f);
                            AudioManager.me.PlaySound2D(AudioManager.me.OpenBooster, Random.Range(0.9f, 1.1f), 0.3f);
                            WorldManager.instance.GivenCards.Add(cardData.Id);
                            if (bag.CardBagType != CardBagType.SetPack && Random.value <= 0.01f)
                            {
                                cardData.SetFoil();
                            }

                            //WorldManager.instance.CreateBoosterpack(buyBoosterBox.transform.position, pack.BoosterId).Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * 4.5f));
                            GameScreen.instance.AddNotification("Reward Redeemed", $"{e.RewardRedeemed.Redemption.User.DisplayName} gifted you a {cardData.FullName}!");
                        }
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.BoosterPackPrefabs.FindAll(x => x.IsUnlocked || x.BoosterId == "basic").Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("SpawnEnemy", SpawnEnemy.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var cardChances = CardBag.GetChancesForSetCardBag(SetCardBag.BasicEnemy).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id));

                    if (CardBag.GetChancesForSetCardBag(SetCardBag.AdvancedEnemy).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id)).Count > 0 && Random.Range(0, 100) < 5)
                    {
                        cardChances = CardBag.GetChancesForSetCardBag(SetCardBag.AdvancedEnemy).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id));
                    }

                    if (cardChances.Count > 0) {
                        var card = WorldManager.instance.GetRandomCard(cardChances);
                        if (card != null)
                        {
                            BuyBoosterBox buyBoosterBox = FindObjectsOfType<BuyBoosterBox>()[Random.Range(0, FindObjectsOfType<BuyBoosterBox>().Count())];


                            var cardData = WorldManager.instance.CreateCard(buyBoosterBox.transform.position, card);

                            cardData.MyGameCard.Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * Random.Range(2.5f, 6.5f)));
                            cardData.MyGameCard.RotWobble(1f);
                            AudioManager.me.PlaySound2D(AudioManager.me.OpenBooster, Random.Range(0.9f, 1.1f), 0.3f);
                            WorldManager.instance.GivenCards.Add(cardData.Id);
                            if (Random.value <= 0.01f)
                            {
                                cardData.SetFoil();
                            }

                            GameScreen.instance.AddNotification("Reward Redeemed",$"{e.RewardRedeemed.Redemption.User.DisplayName} spawned a {cardData.MyGameCard.CardNameText.text}!");
                        }
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && CardBag.GetChancesForSetCardBag(SetCardBag.BasicEnemy).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id)).Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("Gluttony", Gluttony.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    Food food = WorldManager.instance.GetFoodToUseUp();
                    if (food == null)
                    {
                        return;
                    }
                    GameCard foodCard = food.MyGameCard;
                    foodCard.PushEnabled = false;
                    foodCard.SetY = false;
                    foodCard.Velocity = null;
                    List<GameCard> originalStack = foodCard.GetAllCardsInStack();
                    foodCard.RemoveFromStack();

                    GameScreen.instance.AddNotification("Reward Redeemed",$"{e.RewardRedeemed.Redemption.User.DisplayName} ate a {foodCard.CardNameText.text}!");

                    AudioManager.me.PlaySound2D(AudioManager.me.Eat, Random.Range(0.8f, 1.2f), 0.3f);
                    food.FoodValue--;

                    foodCard.SetHitEffect(null);
                    foodCard.transform.localScale *= 0.9f;

                    originalStack.Remove(foodCard);
                    WorldManager.instance.Restack(originalStack);
                    foodCard.DestroyCard(true, true);

                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.GetFoodToUseUp() != null;
            });

            Twitch.TwitchManager.RegisterReward("SpawnFood", SpawnFood.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var cardChances = CardBag.GetChancesForSetCardBag(SetCardBag.BasicFood).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id));


                    if (CardBag.GetChancesForSetCardBag(SetCardBag.AdvancedFood).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id)).Count > 0 && Random.Range(0, 100) < 5) {
                        cardChances = CardBag.GetChancesForSetCardBag(SetCardBag.AdvancedFood).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id));
                    }

                    if (cardChances.Count > 0)
                    {
                        var card = WorldManager.instance.GetRandomCard(cardChances);
                        if (card != null)
                        {
                            BuyBoosterBox buyBoosterBox = FindObjectsOfType<BuyBoosterBox>()[Random.Range(0, FindObjectsOfType<BuyBoosterBox>().Count())];


                            var cardData = WorldManager.instance.CreateCard(buyBoosterBox.transform.position, card);

                            cardData.MyGameCard.Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * Random.Range(2.5f, 6.5f)));
                            cardData.MyGameCard.RotWobble(1f);
                            AudioManager.me.PlaySound2D(AudioManager.me.OpenBooster, Random.Range(0.9f, 1.1f), 0.3f);
                            WorldManager.instance.GivenCards.Add(cardData.Id);
                            if (Random.value <= 0.01f)
                            {
                                cardData.SetFoil();
                            }

                            GameScreen.instance.AddNotification("Reward Redeemed",$"{e.RewardRedeemed.Redemption.User.DisplayName} gifted you a {cardData.FullName}!");
                        }
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && CardBag.GetChancesForSetCardBag(SetCardBag.BasicFood).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id)).Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("SpawnAnimal", SpawnAnimal.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var cardChances = CardBag.GetChancesForSetCardBag(SetCardBag.Animal).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id));

                    if (cardChances.Count > 0)
                    {
                        var card = WorldManager.instance.GetRandomCard(cardChances);
                        if (card != null)
                        {
                            BuyBoosterBox buyBoosterBox = FindObjectsOfType<BuyBoosterBox>()[Random.Range(0, FindObjectsOfType<BuyBoosterBox>().Count())];


                            var cardData = WorldManager.instance.CreateCard(buyBoosterBox.transform.position, card);

                            cardData.MyGameCard.Velocity = new Vector3?(new Vector3(0f, 8f, -buyBoosterBox.PushDir.Value.z * Random.Range(2.5f, 6.5f)));
                            cardData.MyGameCard.RotWobble(1f);
                            AudioManager.me.PlaySound2D(AudioManager.me.OpenBooster, Random.Range(0.9f, 1.1f), 0.3f);
                            WorldManager.instance.GivenCards.Add(cardData.Id);
                            if (Random.value <= 0.01f)
                            {
                                cardData.SetFoil();
                            }

                            GameScreen.instance.AddNotification("Reward Redeemed",$"{e.RewardRedeemed.Redemption.User.DisplayName} spawned a {cardData.FullName}!");
                        }
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && CardBag.GetChancesForSetCardBag(SetCardBag.Animal).FindAll(c => WorldManager.instance.CurrentSaveGame.FoundCardIds.Contains(WorldManager.instance.GetCardFromId(c.Id).Id)).Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("DamageEnemy", DamageEnemy.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var enemies = WorldManager.instance.AllCards.FindAll(card => card.CardData is Enemy);
                    if(enemies.Count > 0)
                    {
                        var enemy = enemies[Random.Range(0, enemies.Count)];
                        ((Enemy)enemy.CardData).Damage(1);

                        GameScreen.instance.AddNotification("Reward Redeemed", $"{e.RewardRedeemed.Redemption.User.DisplayName} damaged the {enemy.CardNameText.text}!");
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.AllCards.FindAll(card => card.CardData is Enemy).Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("StealResource", StealResource.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var cards = WorldManager.instance.AllCards.FindAll(card => card.CardData.MyCardType == CardType.Resources);
                    if (cards.Count > 0)
                    {
                        var card = cards[Random.Range(0, cards.Count)];

                        card.PushEnabled = false;
                        card.SetY = false;
                        card.Velocity = null;
                        List<GameCard> originalStack = card.GetAllCardsInStack();
                        card.RemoveFromStack();

                        GameScreen.instance.AddNotification("Reward Redeemed", $"{e.RewardRedeemed.Redemption.User.DisplayName} swiped a {card.CardNameText.text}!");

                        card.SetHitEffect(null);
                        card.transform.localScale *= 0.9f;

                        originalStack.Remove(card);
                        WorldManager.instance.Restack(originalStack);
                        card.DestroyCard(true, true);
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.AllCards.FindAll(card => card.CardData.MyCardType == CardType.Resources).Count > 0;
            });

            Twitch.TwitchManager.RegisterReward("Kleptomaniac", Kleptomaniac.Value, false, delegate (object manager, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e) {
                if (WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
                {
                    var cards = WorldManager.instance.AllCards.FindAll(card => card.CardData.MyCardType != CardType.Humans);
                    if (cards.Count > 0)
                    {
                        var card = cards[Random.Range(0, cards.Count)];

                        card.PushEnabled = false;
                        card.SetY = false;
                        card.Velocity = null;
                        List<GameCard> originalStack = card.GetAllCardsInStack();
                        card.RemoveFromStack();

                        GameScreen.instance.AddNotification("Reward Redeemed", $"{e.RewardRedeemed.Redemption.User.DisplayName} stole a {card.CardNameText.text}!");

                        card.SetHitEffect(null);
                        card.transform.localScale *= 0.9f;

                        originalStack.Remove(card);
                        WorldManager.instance.Restack(originalStack);
                        card.DestroyCard(true, true);
                    }
                }
            }, delegate () {
                return WorldManager.instance != null && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && WorldManager.instance.AllCards.FindAll(card => card.CardData.MyCardType != CardType.Humans).Count > 0;
            });
        }

        private int GetRandomNumber(int max, int min, double probabilityPower = 2)
        {
            var randomizer = new System.Random();
            var randomDouble = randomizer.NextDouble();

            var result = Math.Floor(min + (max + 1 - min) * (Math.Pow(randomDouble, probabilityPower)));
            return (int)result;
        }


        private List<int> ExtractIntegers(string input)
        {
            List<int> numbers = new List<int>();
            string integerString = "";
            foreach (Char c in input)
            {
                if (Char.IsDigit(c))
                {
                    integerString += c;
                }
                else 
                {
                    int x = -1;

                    Int32.TryParse(integerString, out x);
                    numbers.Add(x);

                    if (x != -1)
                    {
                        integerString = "";
                    }
                }
            }

            if(integerString != "")
            {
                int x = -1;

                Int32.TryParse(integerString, out x);

                if (x != -1)
                {
                    numbers.Add(x);
                }
            }

            return numbers;
        }

    }

    public static class EnumerableExtensions
    {

        /// <summary>
        /// ForEach but with a try catch in it.
        /// </summary>
        /// <param name="list">the enumerable object</param>
        /// <param name="action">the action to do on it</param>
        /// <param name="exceptions">the exception dictionary that will get filled, null by default if you simply want to silence the errors if any pop.</param>
        /// <typeparam name="T"></typeparam>
        public static void ForEachTry<T>(this IEnumerable<T>? list, Action<T>? action, IDictionary<T, Exception?>? exceptions = null)
        {
            list.ToList().ForEach(element => {
                try
                {
                    action(element);
                }
                catch (Exception exception)
                {
                    exceptions?.Add(element, exception);
                }
            });
        }
    }
}
