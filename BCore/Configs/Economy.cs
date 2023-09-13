using System;
using System.Collections.Generic;
using System.IO;
using BCore.ClanSystem;
using BCore.Users;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

[Serializable]
public class UserEconomy
{
    public ulong steamID;
    public ulong balance;
    public int playersKilled;
    public int mutantsKilled;
    public int animalsKilled;
    public int deaths;

    public UserEconomy(ulong steamID, ulong balance)
    {
        this.steamID = steamID;
        this.balance = balance;
        playersKilled = 0;
        mutantsKilled = 0;
        animalsKilled = 0;
        deaths = 0;
    }

    public ulong Hash
    {
        get
        {
            var value = steamID;
            value += (ulong)balance.GetHashCode();
            value += (ulong)playersKilled.GetHashCode();
            value += (ulong)mutantsKilled.GetHashCode();
            value += (ulong)animalsKilled.GetHashCode();
            value += (ulong)deaths.GetHashCode();
            return value;
        }
    }
}

#region SHOP

[Serializable]
public class ShopGroup
{
    public int index;
    public string name;

    public ShopGroup(string groupName, int groupIndex)
    {
        name = groupName;
        index = groupIndex;
    }
}

[Serializable]
public class ShopItem
{
    public int index;
    public string name;
    public int sellPrice;
    public int buyPrice;
    public int quantity;
    public int slots;

    public ShopItem(int itemIndex, string itemName, int sellPrice, int buyPrice, int quantity = 1, int slots = -1)
    {
        index = itemIndex;
        name = itemName;
        this.sellPrice = sellPrice;
        this.buyPrice = buyPrice;
        this.quantity = quantity;
        this.slots = slots;
    }
}

#endregion

public class Economy
{
    public static EconomyData EData;

    public static void Balance(NetUser Sender, User userData, string Command, string[] Args)
    {
        var currencyBalance = "0" + EData.CurrencySign;
        if (Sender != null && !EData.Database.ContainsKey(userData.SteamID)) Add(userData.SteamID);
        if (Sender != null)
            currencyBalance = EData.Database[userData.SteamID].balance.ToString("N0") + EData.CurrencySign;

        if (Args != null && Args.Length > 0 && (Sender == null || Sender.admin))
        {
            userData = Data.FindUser(Args[0]);
            if (userData == null)
            {
                Broadcast.Notice(Sender, "✘",
                    Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null, Args[0]));
                return;
            }

            if (!EData.Database.ContainsKey(userData.SteamID))
            {
                Broadcast.Notice(Sender, "✘", "Player \"" + Args[0] + "\" not have balance");
                return;
            }

            var argValue = EData.Database[userData.SteamID].balance;
            var balanceAppend = Args.Length > 1 && Args[1].StartsWith("+");
            var balanceSubtract = Args.Length > 1 && Args[1].StartsWith("-");
            if (Args.Length > 1) Args[1] = Args[1].Replace("+", "").Replace("-", "").Trim();

            if (Args.Length > 1 && ulong.TryParse(Args[1], out argValue))
            {
                if (balanceSubtract) BalanceSub(userData.SteamID, argValue);
                else if (balanceAppend) BalanceAdd(userData.SteamID, argValue);
                else EData.Database[userData.SteamID].balance = argValue;

                currencyBalance = EData.Database[userData.SteamID].balance.ToString("N0") + EData.CurrencySign;
                Broadcast.Notice(Sender, EData.CurrencySign,
                    "Balance of \"" + userData.UserName + "\" now " + currencyBalance);
            }
            else
            {
                currencyBalance = EData.Database[userData.SteamID].balance.ToString("N0") + EData.CurrencySign;
                Broadcast.Notice(Sender, EData.CurrencySign,
                    "Balance of \"" + userData.UserName + "\" is " + currencyBalance);
            }

            return;
        }

        Broadcast.Message(Sender,
            Config.GetMessage(Messages.RuMessages.RuMessage.EconomyBalance, Sender)
                .Replace("%BALANCE%", currencyBalance));
    }

    public static void HurtKilled(DamageEvent damage)
    {
        ulong DeathFee = 0;
        var victimName = "";
        ulong DeathPay = 0;
        var killerName = "";
        var Victim = damage.victim.client;
        var Killer = damage.attacker.client;
        var VictimIsObject = !(damage.victim.idMain is Character);
        var KillerIsObject = !(damage.attacker.idMain is Character);
        var VictimData = Victim != null ? Data.FindUser(Victim.userID) : null;
        var KillerData = Killer != null ? Data.FindUser(Killer.userID) : null;
        var victimClan = VictimData != null ? Clans.Find(Data.FindUser(VictimData.SteamID).Clan) : null;
        var killerClan = KillerData != null ? Clans.Find(Data.FindUser(KillerData.SteamID).Clan) : null;
        // Killer is object and not Player //
        if (KillerIsObject && Killer == null) return;
        // Killer is Player
        if (Killer != null)
        {
            killerName = damage.attacker.client.netUser.displayName;
            // Killer is NPC
        }
        else
        {
            killerName = Helper.NiceName(damage.attacker.character.name);
            Debug.Log(killerName);
            killerName = Messages.RuMessages.Names[killerName];
        }

        // Victim is Object
        if (VictimIsObject)
        {
            if (damage.victim.idMain is SleepingAvatar && EData.FeeSleeper)
            {
                var SleeperAvatar = damage.victim.idMain as SleepingAvatar;
                if (SleeperAvatar == null) return;
                var victimData = Data.FindUser(SleeperAvatar.ownerID);
                if (victimData == null) return;
                victimClan = Clans.Find(victimData.Clan);
                var VictimBalance = GetBalance(victimData.SteamID);
                victimName = victimData.UserName;
                if (EData.FeeMurder) DeathFee = (ulong)Math.Abs(VictimBalance * EData.FeeMurderPercent / 100);
                if (EData.PayMurder) DeathPay = (ulong)Math.Abs(VictimBalance * EData.PayMurderPercent / 100);

                if (DeathPay > 0 && killerClan != null)
                {
                    if (killerClan.Level.BonusMembersPayMurder > 0)
                        DeathPay += DeathPay * killerClan.Level.BonusMembersPayMurder / 100;
                    if (killerClan.Tax > 0)
                    {
                        var PayTax = DeathPay * killerClan.Tax / 100;
                        killerClan.Balance += PayTax;
                        DeathPay -= PayTax;
                    }
                }

                if (DeathPay > 0) BalanceAdd(Killer.userID, DeathPay);
                if (DeathFee > 0) BalanceSub(victimData.SteamID, DeathFee);
            }
            else
            {
                return; // Return for other objects //
            }
        }
        // Victim is Player
        else if (Victim != null)
        {
            victimName = damage.victim.client.netUser.displayName;
            var VictimBalance = GetBalance(Victim.userID);
            // Killer is NPC
            if (Killer == null)
            {
                if (EData.FeeDeath) DeathFee = (ulong)Math.Abs(VictimBalance * EData.FeeDeathPercent / 100);
                // Killer is Victim (Suicide)
            }
            else if (Killer == Victim || KillerIsObject)
            {
                if (EData.FeeSuicide) DeathFee = (ulong)Math.Abs(VictimBalance * EData.FeeSuicidePercent / 100);
                // Killer is Player
            }
            else if (Killer != Victim && !KillerIsObject)
            {
                Get(Killer.userID).playersKilled++;
                if (EData.FeeMurder) DeathFee = (ulong)Math.Abs(VictimBalance * EData.FeeMurderPercent / 100);
                if (EData.PayMurder) DeathPay = (ulong)Math.Abs(VictimBalance * EData.PayMurderPercent / 100);
            }

            if (DeathPay > 0 && killerClan != null)
            {
                if (killerClan.Level.BonusMembersPayMurder > 0)
                    DeathPay += DeathPay * killerClan.Level.BonusMembersPayMurder / 100;
                if (killerClan.Tax > 0)
                {
                    var PayTax = DeathPay * killerClan.Tax / 100;
                    killerClan.Balance += PayTax;
                    DeathPay -= PayTax;
                }
            }

            if (DeathPay > 0) BalanceAdd(Killer.userID, DeathPay);
            if (DeathFee > 0) BalanceSub(Victim.userID, DeathFee);
            Get(Victim.userID).deaths++;
        }
        // Victim is NPC (killer is player owned)
        else if (Killer != null)
        {
            victimName = Helper.NiceName(damage.victim.character.name);

            if (victimName.Equals("Chicken", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostChicken != 0) DeathPay = EData.CostChicken;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Rabbit", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostRabbit != 0) DeathPay = EData.CostRabbit;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Boar", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostBoar != 0) DeathPay = EData.CostBoar;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Stag", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostStag != 0) DeathPay = EData.CostStag;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Wolf", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostWolf != 0) DeathPay = EData.CostWolf;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Bear", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostBear != 0) DeathPay = EData.CostBear;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Mutant Wolf", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostMutantWolf != 0) DeathPay = EData.CostMutantWolf;
                Get(Killer.userID).mutantsKilled++;
            }
            else if (victimName.Equals("Mutant Bear", StringComparison.OrdinalIgnoreCase))
            {
                if (EData.CostMutantBear != 0) DeathPay = EData.CostMutantBear;
                Get(Killer.userID).mutantsKilled++;
            }
            else
            {
                ConsoleSystem.LogWarning("[WARNING] Economy: Creature '" + victimName + "' not have cost of death.");
            }
            victimName = Messages.RuMessages.Names[victimName];
            if (DeathPay > 0 && killerClan != null)
            {
                if (killerClan.Level.BonusMembersPayMurder > 0)
                    DeathPay += DeathPay * killerClan.Level.BonusMembersPayMurder / 100;
                if (killerClan.Tax > 0)
                {
                    var PayTax = DeathPay * killerClan.Tax / 100;
                    killerClan.Balance += PayTax;
                    DeathPay -= PayTax;
                }
            }

            if (DeathPay > 0) BalanceAdd(Killer.userID, DeathPay);
        }

        // Killer is Player
        if (Killer != null && DeathPay > 0)
        {
            var MessageDeathPay =
                Config.GetMessage(Messages.RuMessages.RuMessage.EconomyPlayerDeathPay, Killer.netUser);
            if (VictimIsObject)
                MessageDeathPay =
                    Config.GetMessage(Messages.RuMessages.RuMessage.EconomySleeperDeathPay, Killer.netUser);
            MessageDeathPay = MessageDeathPay.Replace("%DEATHPAY%", DeathPay.ToString("N0") + EData.CurrencySign);
            MessageDeathPay = MessageDeathPay.Replace("%VICTIM%", victimName);
            Broadcast.Message(Killer.netPlayer, MessageDeathPay);
        }

        // Victim is Player
        if (Victim != null && DeathFee > 0)
        {
            string MessageDeathFee = MessageDeathFee =
                Config.GetMessage(Messages.RuMessages.RuMessage.EconomyPlayerDeathFee, Victim.netUser);
            if (Killer == Victim || KillerIsObject)
                MessageDeathFee =
                    Config.GetMessage(Messages.RuMessages.RuMessage.EconomyPlayerSuicideFee, Victim.netUser);
            MessageDeathFee = MessageDeathFee.Replace("%DEATHFEE%", DeathFee.ToString("N0") + EData.CurrencySign);
            MessageDeathFee = MessageDeathFee.Replace("%KILLER%", killerName);
            MessageDeathFee = MessageDeathFee.Replace("%VICTIM%", victimName);
            Broadcast.Message(Victim.netPlayer, MessageDeathFee);
        }
    }

    public static ulong GetBalance(ulong steam_id)
    {
        if (!EData.Database.ContainsKey(steam_id)) Add(steam_id);
        return EData.Database[steam_id].balance;
    }

    public static void BalanceAdd(ulong steam_id, ulong amount)
    {
        if (!EData.Database.ContainsKey(steam_id)) Add(steam_id);
        var balance = EData.Database[steam_id].balance;
        if (balance + amount < balance) balance = ulong.MaxValue;
        else balance += amount;
        EData.Database[steam_id].balance = balance;
    }

    public static void BalanceSub(ulong steam_id, ulong amount)
    {
        if (!EData.Database.ContainsKey(steam_id)) Add(steam_id);
        if (EData.Database[steam_id].balance <= amount) amount = 0;
        else amount = EData.Database[steam_id].balance - amount;
        EData.Database[steam_id].balance = amount;
    }

    public static UserEconomy Get(ulong steam_id)
    {
        if (!EData.Database.ContainsKey(steam_id)) Add(steam_id);
        return EData.Database[steam_id];
    }

    public static UserEconomy Add(ulong steam_id, int players_killed = 0, int mutants_killed = 0,
        int animals_killed = 0, int deaths = 0)
    {
        if (EData.Database.ContainsKey(steam_id)) return null;
        var userEconomy = new UserEconomy(steam_id, EData.StartBalance)
        {
            playersKilled = players_killed,
            mutantsKilled = mutants_killed,
            animalsKilled = animals_killed,
            deaths = deaths
        };
        EData.Database.Add(steam_id, userEconomy);
        return userEconomy;
    }

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\economy.cfg"))
        {
            LoadData();
        }
        else
        {
            EData = new EconomyData
            {
                Database = new Dictionary<ulong, UserEconomy>(),
                HashData = new Dictionary<ulong, ulong>()
            };
            SaveData();
        }

        Shop.Initialize();
    }

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\economy.cfg", JsonConvert.SerializeObject(EData, Formatting.Indented));
        Debug.Log("[BCore]: Economy Data Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\economy.cfg");
        EData = JsonConvert.DeserializeObject<EconomyData>(file);
        Debug.Log("[BCore]: Economy Data Loaded!");
    }

    public class EconomyData
    {
        public float CommandSendTax = 0.0f; // Tax percents of send currency from a sender to specified player
        public ulong CostBear = 20; // Cost in currency of killed a "Bear" for the a player
        public ulong CostBoar = 2; // Cost in currency of killed a "Boar" for the a player
        public ulong CostChicken = 1; // Cost in currency of killed a "Chicken" for the a player
        public ulong CostMutantBear = 50; // Cost in currency of killed a "Mutant Bear" for the a player
        public ulong CostMutantWolf = 25; // Cost in currency of killed a "Mutant Wolf" for the a player
        public ulong CostRabbit = 1; // Cost in currency of killed a "Rabbit" for the a player
        public ulong CostStag = 5; // Cost in currency of killed a "Deer" for the a player
        public ulong CostWolf = 10; // Cost in currency of killed a "Wolf" for the a player
        public string CurrencySign = "$"; // Sign of Currency

        public Dictionary<ulong, UserEconomy> Database;
        public bool Enabled = false; // Enable/Disable economy system on server for players.

        public bool FeeDeath = true; // Enable/Disable to lose currency (player was killed by NPC)

        public float
            FeeDeathPercent = 5.0f; // Percents of currency to lose after the player dies from suicide or NPC

        public bool FeeMurder = true; // Enable/Disable to lose currency (player was killed by a player)
        public float FeeMurderPercent = 10.0f; // Percents of currency to lose after the player dies from player
        public bool FeeSleeper = true; // Enable/Disable to lose/transfer currency (sleeper was killed by a player)
        public bool FeeSuicide = true; // Enable/Disable to lose currency (player was killed by NPC)

        public float
            FeeSuicidePercent = 1.0f; // Percents of currency to lose after the player dies from suicide or NPC

        public Dictionary<ulong, ulong> HashData;
        public bool PayMurder = true; // Enable/Disable to give currency for murder after kill player
        public float PayMurderPercent = 10.0f; // Percents of currency to give for murder after kill player
        public ulong StartBalance = 500; // Amount of starting balance for new players.
    }
}

public class Shop
{
    public static ShopData SData;
    public static List<ShopGroup> ShopGroups;

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\shop.cfg"))
        {
            LoadData();
        }
        else
        {
            ShopGroups = new List<ShopGroup>();

            SData = new ShopData();
            SData.EntryGroup = new ShopGroup("Ресурсы", 0);
            SData.ShopPages = new Dictionary<string, List<ShopItem>>
            {
                {
                    "Ресурсы", new List<ShopItem>
                    {
                        new(1, "Charcoal", 5, 1),
                        new(2, "Animal Fat", 12, 2),
                        new(3, "Cloth", 10, 2),
                        new(4, "Blood", 20, 5),
                        new(5, "Leather", 20, 2),
                        new(6, "Stones", 5, 1),
                        new(7, "Wood", 10, 1),
                        new(8, "Wood Planks", 100, 5),
                        new(9, "Low Grade Fuel", 22, 4),
                        new(10, "Sulfur Ore", 40, 3),
                        new(11, "Sulfur", 12, 2),
                        new(12, "Metal Ore", 40, 3),
                        new(13, "Gunpowder", 35, 4),
                        new(14, "Metal Fragments", 12, 3),
                        new(15, "Low Quality Metal", 180, 30)
                    }
                },
                {
                    "Ресурсы2", new List<ShopItem>
                    {
                        new(16, "Charcoal", 5, 1),
                        new(17, "Animal Fat", 12, 2),
                        new(18, "Cloth", 10, 2),
                        new(19, "Blood", 20, 5),
                        new(20, "Leather", 20, 2),
                        new(21, "Stones", 5, 1),
                        new(22, "Wood", 10, 1),
                        new(23, "Wood Planks", 100, 5),
                        new(24, "Low Grade Fuel", 22, 4),
                        new(25, "Sulfur Ore", 40, 3),
                        new(26, "Sulfur", 12, 2),
                        new(27, "Metal Ore", 40, 3),
                        new(28, "Gunpowder", 35, 4),
                        new(29, "Metal Fragments", 12, 3),
                        new(30, "Low Quality Metal", 180, 30)
                    }
                }
            };
            SaveData();
        }
    }

    #region [Public] Shop: Find shop items by group index

    public static List<ShopItem> GetItems(int group_index, out string group_name)
    {
        foreach (var group in ShopGroups)
        {
            if (group.name == null || group.index == 0) continue;
            if (group.index == group_index)
            {
                group_name = group.name;
                return SData.ShopPages[group.name];
            }
        }

        group_name = null;
        return null;
    }

    #endregion

    #region [Public] Shop: Find shop item by name

    public static ShopItem FindItem(string item_name)
    {
        ShopItem Result = null;
        var itemName = item_name.Trim('*').ToLower();
        foreach (var group in ShopGroups)
        {
            if (item_name.StartsWith("*"))
                Result = SData.ShopPages[group.name].Find(F => F.name.ToLower().EndsWith(itemName));
            if (item_name.EndsWith("*"))
                Result = SData.ShopPages[group.name].Find(F => F.name.ToLower().StartsWith(itemName));
            if (Result == null)
                Result = SData.ShopPages[group.name]
                    .Find(F => F.name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (Result != null) return Result;
        }

        return null;
    }

    #endregion

    #region [Public] Shop: Find item by index

    public static ShopItem FindItem(int item_index)
    {
        ShopItem Result = null;
        foreach (var group in ShopGroups)
        {
            Result = SData.ShopPages[group.name].Find(F => F.index == item_index);
            if (Result != null) return Result;
        }

        return null;
    }

    #endregion

    #region [Public] Shop: Find shop items by group name

    public static List<ShopItem> GetItems(string group_name, out int group_index)
    {
        var groupName = group_name.Trim('*').ToLower();
        foreach (var group in ShopGroups)
        {
            if (group.name == null || group.index == 0) continue;
            if (group_name.StartsWith("*") && group.name.ToLower().EndsWith(groupName))
            {
                group_index = group.index;
                return SData.ShopPages[group.name];
            }

            if (group_name.EndsWith("*") && group.name.ToLower().StartsWith(groupName))
            {
                group_index = group.index;
                return SData.ShopPages[group.name];
            }

            if (group.name.Equals(group_name, StringComparison.OrdinalIgnoreCase))
            {
                group_index = group.index;
                return SData.ShopPages[group.name];
            }
        }

        group_index = 0;
        return null;
    }

    #endregion

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\shop.cfg", JsonConvert.SerializeObject(SData, Formatting.Indented));
        Debug.Log("[BCore]: Shop Data Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\shop.cfg");
        SData = JsonConvert.DeserializeObject<ShopData>(file);
        ShopGroups = new List<ShopGroup>();
        var i = 0;
        foreach (var key in SData.ShopPages.Keys)
        {
            ShopGroups.Add(new ShopGroup(key, i));
            i++;
        }

        Debug.Log("[BCore]: Shop Data Loaded!");
    }

    public class ShopData
    {
        public bool CanBuy = false;
        public bool CanSell = false;
        public ShopGroup EntryGroup;

        public Dictionary<string, List<ShopItem>> ShopPages;

        public int GroupCount => 0;
        public int ItemCount => 0;
    }
}