using System;
using System.Collections.Generic;
using System.IO;
using BCore.ClanSystem;
using BCore.Users;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BCore.Configs;

public class Config
{
    public static string ProductName = "BCore";
    public static string Version = "5.13.2 <RELEASE>";
    public static Settings _Settings;

    public static Dictionary<ulong, List<HistoryRecord>> History = new();
    public static Dictionary<ulong, DateTime> DestoryOwnership = new();
    public static Dictionary<string, string> DestoryResources = new();
    public static Dictionary<ulong, UserQuery> ChatQuery = new();
    public static Dictionary<ulong, NetUser> Reply = new();

    public static void Initialize()
    {
        if (!Directory.Exists(@"serverdata\cfg")) Directory.CreateDirectory(@"serverdata\cfg");
        if (!Directory.Exists(@"serverdata\cfg\BCore")) Directory.CreateDirectory(@"serverdata\cfg\BCore");
        if (!Directory.Exists(@"serverdata\cfg\userdata")) Directory.CreateDirectory(@"serverdata\userdata");

        if (File.Exists(@"serverdata\cfg\BCore\coreconfig.cfg"))
        {
            LoadData();
        }
        else
        {
            _Settings = new Settings();
            SaveData();
        }
    }

    #region "Get death message string from config"

    public static string GetMessageDeath(string[] msg, NetUser Victim = null, string KillerName = null,
        string WeaponName = null)
    {
        var Message = msg;
        var Idx = Random.Range(0, Message.Length);
        var Result = Helper.ReplaceVariables(Victim, Message[Idx]);
        if (Victim != null && Result.Contains("%VICTIM%")) Result = Result.Replace("%VICTIM%", Victim.displayName);
        if (KillerName != null && Result.Contains("%KILLER%")) Result = Result.Replace("%KILLER%", KillerName);
        if (WeaponName != null && Result.Contains("%WEAPON%")) Result = Result.Replace("%WEAPON%", WeaponName);
        if (Result.Contains("%POSX%"))
            Result = Result.Replace("%POSX%", Victim.playerClient.lastKnownPosition.x.ToString());
        if (Result.Contains("%POSY%"))
            Result = Result.Replace("%POSY%", Victim.playerClient.lastKnownPosition.y.ToString());
        if (Result.Contains("%POSZ%"))
            Result = Result.Replace("%POSZ%", Victim.playerClient.lastKnownPosition.z.ToString());
        if (Result.Contains("%POS%"))
            Result = Result.Replace("%POS%", Victim.playerClient.lastKnownPosition.ToString());
        return Result;
    }

    #endregion

    #region "Get murder message string from config"

    public static string GetMessageMurder(string[] msg, NetUser Killer = null, string VictimName = null,
        string WeaponName = null)
    {
        var Message = msg;
        var Idx = Random.Range(0, Message.Length);
        var Result = Helper.ReplaceVariables(Killer, Message[Idx]);
        if (Killer != null && Result.Contains("%KILLER%")) Result = Result.Replace("%KILLER%", Killer.displayName);
        if (VictimName != null && Result.Contains("%VICTIM%")) Result = Result.Replace("%VICTIM%", VictimName);
        if (WeaponName != null && Result.Contains("%WEAPON%")) Result = Result.Replace("%WEAPON%", WeaponName);
        if (Result.Contains("%POSX%"))
            Result = Result.Replace("%POSX%", Killer.playerClient.lastKnownPosition.x.ToString());
        if (Result.Contains("%POSY%"))
            Result = Result.Replace("%POSY%", Killer.playerClient.lastKnownPosition.y.ToString());
        if (Result.Contains("%POSZ%"))
            Result = Result.Replace("%POSZ%", Killer.playerClient.lastKnownPosition.z.ToString());
        if (Result.Contains("%POS%"))
            Result = Result.Replace("%POS%", Killer.playerClient.lastKnownPosition.ToString());
        return Result;
    }

    #endregion

    public static string GetMessageCommand(string msg, string command = "", NetUser User = null)
    {
        var Result = msg;
        Result = Helper.ReplaceVariables(User, Result);
        if (Result.Contains("%COMMAND%")) Result = Result.Replace("%COMMAND%", command);
        return Result;
    }

    #region "Get object message string from config"

    public static string GetMessageObject(string msg, string VictimName = null, PlayerClient Killer = null,
        string WeaponName = null, User Owner = null)
    {
        var Result = msg;
        if (Result.Contains("%OWNERNAME%"))
            Result = Result.Replace("%OWNERNAME%", Owner != null ? Owner.UserName : "-");
        if (Result.Contains("%OWNER_ID%"))
            Result = Result.Replace("%OWNER_ID%", Owner != null ? Owner.SteamID.ToString() : "-");
        if (Killer != null && Result.Contains("%USERNAME%"))
            Result = Result.Replace("%USERNAME%", Killer.netUser.displayName);
        if (Killer != null && Result.Contains("%STEAM_ID%"))
            Result = Result.Replace("%STEAM_ID%", Killer.netUser.userID.ToString());
        if (VictimName != null && Result.Contains("%OBJECT%")) Result = Result.Replace("%OBJECT%", VictimName);
        if (WeaponName != null && Result.Contains("%WEAPON%")) Result = Result.Replace("%WEAPON%", WeaponName);
        return Result;
    }

    #endregion

    public static string[] GetMessages(string[] msg, NetUser User = null)
    {
        var Result = msg;
        for (var i = 0; i < Result.Length; i++) Result[i] = Helper.ReplaceVariables(User, Result[i]);
        return Result;
    }

    public static string GetMessage(string msg, NetUser User = null, string Username = null)
    {
        return Helper.ReplaceVariables(User, msg, Username == null ? null : "%USERNAME%", Username);
    }

    public static string GetMessageClan(string msg, ClanData clan = null, NetUser netUser = null, User dataUser = null)
    {
        var Result = msg;
        ClanLevel nextLevel = null;
        if (clan != null) nextLevel = Clans.ClanCfg.Levels.Find(F => F.RequireLevel == clan.Level.Id);
        User leaderData = null;
        if (clan != null) leaderData = Data.FindUser(clan.LeaderID);
        Result = Helper.ReplaceVariables(netUser, Result);
        if (Result.Contains("%CREATE_COST%"))
            Result = Result.Replace("%CREATE_COST%", Clans.CreateCost.ToString("N0") + Economy.EData.CurrencySign);
        if (Result.Contains("%CLANS.COUNT%")) Result = Result.Replace("%CLANS.COUNT%", Clans.Database.Count.ToString());
        if (dataUser != null)
        {
            if (Result.Contains("%STEAM_ID%")) Result = Result.Replace("%STEAM_ID%", dataUser.SteamID.ToString());
            if (Result.Contains("%USERNAME%")) Result = Result.Replace("%USERNAME%", dataUser.UserName);
        }

        if (clan != null)
        {
            if (Result.Contains("%CLAN.ID%")) Result = Result.Replace("%CLAN.ID%", clan.ID.ToString());
            if (Result.Contains("%CLAN.NAME%")) Result = Result.Replace("%CLAN.NAME%", clan.Name);
            if (Result.Contains("%CLAN.ABBR%") && clan.Flags.Has(ClanFlags.CanAbbr))
                Result = Result.Replace("%CLAN.ABBR%", clan.Abbr);
            if (Result.Contains("%CLAN.MOTD%") && clan.Flags.Has(ClanFlags.CanMotd))
                Result = Result.Replace("%CLAN.MOTD%", clan.MOTD);
            if (Result.Contains("%CLAN.TAX%") && clan.Tax >= 0) Result = Result.Replace("%CLAN.TAX%", clan.Tax + "%");
            if (Result.Contains("%CLAN.EXPERIENCE%"))
                Result = Result.Replace("%CLAN.EXPERIENCE%", clan.Experience.ToString());
            if (Result.Contains("%CLAN.LOCATION%") && clan.Flags.Has(ClanFlags.CanWarp))
                Result = Result.Replace("%CLAN.LOCATION%", clan.Location.AsString());
            if (Result.Contains("%CLAN.ONLINE%")) Result = Result.Replace("%CLAN.ONLINE%", clan.Online.ToString());
            if (Result.Contains("%CLAN.MEMBERS.COUNT%"))
                Result = Result.Replace("%CLAN.MEMBERS.COUNT%", clan.Members.Count.ToString());
        }

        if (leaderData != null)
        {
            if (Result.Contains("%CLAN.LEADER.STEAM_ID%"))
                Result = Result.Replace("%CLAN.LEADER.STEAM_ID%", leaderData.SteamID.ToString());
            if (Result.Contains("%CLAN.LEADER.USERNAME%"))
                Result = Result.Replace("%CLAN.LEADER.USERNAME%", leaderData.UserName);
        }

        if (clan != null && clan.Level != null)
        {
            if (Result.Contains("%CLAN.LEVEL%")) Result = Result.Replace("%CLAN.LEVEL%", clan.Level.Id.ToString());
            if (Result.Contains("%CLAN.MEMBERS.MAX%"))
                Result = Result.Replace("%CLAN.MEMBERS.MAX%", clan.Level.MaxMembers.ToString());
            if (Result.Contains("%CLAN.WARP_TIMEOUT%"))
                Result = Result.Replace("%CLAN.WARP_TIMEOUT%", clan.Level.WarpTimeWait.ToString());
            if (Result.Contains("%CLAN.WARP_COUNTDOWN%"))
                Result = Result.Replace("%CLAN.WARP_COUNTDOWN%", clan.Level.WarpCountdown.ToString());
            if (Result.Contains("%CLAN.BONUS.CRAFTINGSPEED%") && clan.Level.BonusCraftingSpeed > 0)
                Result = Result.Replace("%CLAN.BONUS.CRAFTINGSPEED%", clan.Level.BonusCraftingSpeed + "%");
            if (Result.Contains("%CLAN.BONUS.GATHERINGWOOD%") && clan.Level.BonusGatheringWood > 0)
                Result = Result.Replace("%CLAN.BONUS.GATHERINGWOOD%", clan.Level.BonusGatheringWood + "%");
            if (Result.Contains("%CLAN.BONUS.GATHERINGROCK%") && clan.Level.BonusGatheringRock > 0)
                Result = Result.Replace("%CLAN.BONUS.GATHERINGROCK%", clan.Level.BonusGatheringRock + "%");
            if (Result.Contains("%CLAN.BONUS.GATHERINGANIMAL%") && clan.Level.BonusGatheringAnimal > 0)
                Result = Result.Replace("%CLAN.BONUS.GATHERINGANIMAL%", clan.Level.BonusGatheringAnimal + "%");
            if (Result.Contains("%CLAN.BONUS.MEMBERS_DEFENSE%") && clan.Level.BonusMembersDefense > 0)
                Result = Result.Replace("%CLAN.BONUS.MEMBERS_DEFENSE%", clan.Level.BonusMembersDefense + "%");
            if (Result.Contains("%CLAN.BONUS.MEMBERS_DAMAGE%") && clan.Level.BonusMembersDamage > 0)
                Result = Result.Replace("%CLAN.BONUS.MEMBERS_DAMAGE%", clan.Level.BonusMembersDamage + "%");
            if (Result.Contains("%CLAN.BONUS.MEMBERS_PAYMURDER%") && clan.Level.BonusMembersPayMurder > 0)
                Result = Result.Replace("%CLAN.BONUS.MEMBERS_PAYMURDER%", clan.Level.BonusMembersPayMurder + "%");
        }

        if (clan != null && nextLevel != null)
        {
            if (Result.Contains("%CLAN.NEXT_LEVEL%"))
                Result = Result.Replace("%CLAN.NEXT_LEVEL%", nextLevel.Id.ToString());
            if (Result.Contains("%CLAN.NEXT_CURRENCY%"))
                Result = Result.Replace("%CLAN.NEXT_CURRENCY%", nextLevel.RequireCurrency.ToString());
            if (Result.Contains("%CLAN.NEXT_EXPERIENCE%"))
                Result = Result.Replace("%CLAN.NEXT_EXPERIENCE%", nextLevel.RequireExperience.ToString());
            if (Result.Contains("%CLAN.NEXT_MAXMEMBERS%"))
                Result = Result.Replace("%CLAN.NEXT_MAXMEMBERS%", nextLevel.MaxMembers.ToString());
        }

        return Result;
    }

    public static string[] GetMessagesClan(string[] msg, ClanData clan = null, NetUser netuser = null,
        User dataUser = null)
    {
        var Result = msg;
        ClanLevel nextLevel = null;
        if (clan != null) nextLevel = Clans.ClanCfg.Levels.Find(F => F.RequireLevel == clan.Level.Id);
        User leaderData = null;
        if (clan != null) leaderData = Data.FindUser(clan.LeaderID);
        for (var i = 0; i < Result.Length; i++)
        {
            Result[i] = Helper.ReplaceVariables(netuser, Result[i]);
            if (Result[i].Contains("%CREATE_COST%"))
                Result[i] = Result[i].Replace("%CREATE_COST%", Clans.CreateCost + Economy.EData.CurrencySign);
            if (Result[i].Contains("%CLANS.COUNT%"))
                Result[i] = Result[i].Replace("%CLANS.COUNT%", Clans.Database.Count.ToString());
            if (dataUser != null)
            {
                if (Result[i].Contains("%USERNAME%")) Result[i] = Result[i].Replace("%USERNAME%", dataUser.UserName);
                if (Result[i].Contains("%STEAM_ID%"))
                    Result[i] = Result[i].Replace("%STEAM_ID%", dataUser.SteamID.ToString());
            }

            if (clan != null)
            {
                if (Result[i].Contains("%CLAN.ID%")) Result[i] = Result[i].Replace("%CLAN.ID%", clan.ID.ToString());
                if (Result[i].Contains("%CLAN.NAME%")) Result[i] = Result[i].Replace("%CLAN.NAME%", clan.Name);
                if (Result[i].Contains("%CLAN.ABBR%") && clan.Flags.Has(ClanFlags.CanAbbr))
                    Result[i] = Result[i].Replace("%CLAN.ABBR%", clan.Abbr);
                if (Result[i].Contains("%CLAN.MOTD%") && clan.Flags.Has(ClanFlags.CanMotd))
                    Result[i] = Result[i].Replace("%CLAN.MOTD%", clan.MOTD);
                if (Result[i].Contains("%CLAN.BALANCE%"))
                    Result[i] = Result[i].Replace("%CLAN.BALANCE%", clan.Balance + Economy.EData.CurrencySign);
                if (Result[i].Contains("%CLAN.TAX%") && clan.Tax >= 0)
                    Result[i] = Result[i].Replace("%CLAN.TAX%", clan.Tax + "%");
                if (Result[i].Contains("%CLAN.EXPERIENCE%"))
                    Result[i] = Result[i].Replace("%CLAN.EXPERIENCE%", clan.Experience.ToString());
                if (Result[i].Contains("%CLAN.LOCATION%") && clan.Flags.Has(ClanFlags.CanWarp))
                    Result[i] = Result[i].Replace("%CLAN.LOCATION%", clan.Location.AsString());
                if (Result[i].Contains("%CLAN.ONLINE%"))
                    Result[i] = Result[i].Replace("%CLAN.ONLINE%", clan.Online.ToString());
                if (Result[i].Contains("%CLAN.MEMBERS.COUNT%"))
                    Result[i] = Result[i].Replace("%CLAN.MEMBERS.COUNT%", clan.Members.Count.ToString());
            }

            if (leaderData != null)
            {
                if (Result[i].Contains("%CLAN.LEADER.STEAM_ID%"))
                    Result[i] = Result[i].Replace("%CLAN.LEADER.STEAM_ID%", leaderData.SteamID.ToString());
                if (Result[i].Contains("%CLAN.LEADER.USERNAME%"))
                    Result[i] = Result[i].Replace("%CLAN.LEADER.USERNAME%", leaderData.UserName);
            }

            if (clan != null && clan.Level != null)
            {
                if (Result[i].Contains("%CLAN.LEVEL%"))
                    Result[i] = Result[i].Replace("%CLAN.LEVEL%", clan.Level.Id.ToString());
                if (Result[i].Contains("%CLAN.MEMBERS.MAX%"))
                    Result[i] = Result[i].Replace("%CLAN.MEMBERS.MAX%", clan.Level.MaxMembers.ToString());
                if (Result[i].Contains("%CLAN.WARP_TIMEOUT%"))
                    Result[i] = Result[i].Replace("%CLAN.WARP_TIMEOUT%", clan.Level.WarpTimeWait.ToString());
                if (Result[i].Contains("%CLAN.WARP_COUNTDOWN%"))
                    Result[i] = Result[i].Replace("%CLAN.WARP_COUNTDOWN%", clan.Level.WarpCountdown.ToString());
                if (Result[i].Contains("%CLAN.BONUS.CRAFTINGSPEED%") && clan.Level.BonusCraftingSpeed > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.CRAFTINGSPEED%", clan.Level.BonusCraftingSpeed + "%");
                if (Result[i].Contains("%CLAN.BONUS.GATHERINGWOOD%") && clan.Level.BonusGatheringWood > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.GATHERINGWOOD%", clan.Level.BonusGatheringWood + "%");
                if (Result[i].Contains("%CLAN.BONUS.GATHERINGROCK%") && clan.Level.BonusGatheringRock > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.GATHERINGROCK%", clan.Level.BonusGatheringRock + "%");
                if (Result[i].Contains("%CLAN.BONUS.GATHERINGANIMAL%") && clan.Level.BonusGatheringAnimal > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.GATHERINGANIMAL%",
                        clan.Level.BonusGatheringAnimal + "%");
                if (Result[i].Contains("%CLAN.BONUS.MEMBERS_PAYMURDER%") && clan.Level.BonusMembersPayMurder > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.MEMBERS_PAYMURDER%",
                        clan.Level.BonusMembersPayMurder + "%");
                if (Result[i].Contains("%CLAN.BONUS.MEMBERS_DEFENSE%") && clan.Level.BonusMembersDefense > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.MEMBERS_DEFENSE%", clan.Level.BonusMembersDefense + "%");
                if (Result[i].Contains("%CLAN.BONUS.MEMBERS_DAMAGE%") && clan.Level.BonusMembersDamage > 0)
                    Result[i] = Result[i].Replace("%CLAN.BONUS.MEMBERS_DAMAGE%", clan.Level.BonusMembersDamage + "%");
            }

            if (clan != null && nextLevel != null)
            {
                if (Result[i].Contains("%CLAN.NEXT_LEVEL%"))
                    Result[i] = Result[i].Replace("%CLAN.NEXT_LEVEL%", nextLevel.Id.ToString());
                if (Result[i].Contains("%CLAN.NEXT_CURRENCY%"))
                    Result[i] = Result[i].Replace("%CLAN.NEXT_CURRENCY%", nextLevel.RequireCurrency.ToString());
                if (Result[i].Contains("%CLAN.NEXT_EXPERIENCE%"))
                    Result[i] = Result[i].Replace("%CLAN.NEXT_EXPERIENCE%", nextLevel.RequireExperience.ToString());
                if (Result[i].Contains("%CLAN.NEXT_MAXMEMBERS%"))
                    Result[i] = Result[i].Replace("%CLAN.NEXT_MAXMEMBERS%", nextLevel.MaxMembers.ToString());
            }
        }

        return Result;
    }

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\coreconfig.cfg",
            JsonConvert.SerializeObject(_Settings, Formatting.Indented));
        Debug.Log("[BCore]: Server Config Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\coreconfig.cfg");
        _Settings = JsonConvert.DeserializeObject<Settings>(file);
        Debug.Log("[BCore]: Server Config Loaded!");
    }

    public class Settings
    {
        public bool AdminInstantDestroy = true;
        public bool AdminJoin = false;

        public bool Decay = false;
        
        // AIRDROP
        public bool AirDrop = true;
        public float AirdropAirplaneSpeed = 250f;
        public bool AirdropNoAirplane = false;
        public float AirdropHeight = 300f;
        public bool AirDropAnnounce = false;
        public int[] AirDropPlanes = { 1, 2 };
        public float AmountFlayMultiplier = 1.0f;
        public float AmountRockMultiplier = 1.0f;

        // RESOURCES
        public float AmountWoodMultiplier = 1.0f;
        public bool AttackedAnnounce = true;
        public int AutoAdminRank = 5;

        // PLAYERS
        public float AvatarAutoSaveInterval = 1750.0f;
        public bool BindingNames = true;
        public int BuildMaxComponents = 0;
        public string ChatClanColor = "#7FFF7F";
        public string ChatClanIcon = "CLAN";
        public string ChatClanKey = ".";
        public bool ChatDisplayRank = true;
        public string ChatDivider = " | ";
        public int ChatHistoryDisplay = 10; // Default count of lines for display from history

        public int ChatHistoryStored = 100; // Maximum count of stored lines for chat history
        public int ChatLineMaxLength = 0;
        public string ChatWhisperColor = "#FF7FFF";

        public bool CycleInstantCraft = false; // Enable/Disable toggle for instant craft on server by game time
        public int CycleInstantCraftOff = 0; // Time in game hour for disable instant craft on server
        public int CycleInstantCraftOn = 6; // Time in game hour for enable instant craft on server
        public bool DeathMurder = true;
        public string DeathName = "DEATH";
        public bool DeathNpc = true;
        public bool DeathSelf = true;
        public bool DecayObjects = true;
        public int DefaultRank = 0;
        public bool DisplayClan = true;

        // COMMANDS
        public List<string> ForbiddenTransfer = new() { "Sleeping Bag", "Bed" };
        public float GatherFlayMultiplier = 1.0f;
        public float GatherRockMultiplier = 1.0f;
        public float GatherWoodMultiplier = 1.0f;

        // ADMIN
        public bool GodMode = true;
        public int HistoryLength = 25;
        public int HomeCountdown = 300;
        public bool HomeOutdoorsOnly = true;
        public ulong HomePayment = 0;
        public int HomeTimeWait = 20;
        public bool InstantDestroy = true;
        public bool InvisibleJoin = false;
        public bool KillNotice = true;
        public List<string> Languages = new() { "RUS", "ENG" };

        // CHAT
        public int LineMaxLength = 64;

        // OBJECTS
        public float LootLifeTime = 1800.0f;
        public int MaxHeight = 7;
        public int MaxLength = 7;
        public int MaxWidth = 7;
        public float MuteDefaultTime = 1800.0f;
        public int NoPvpCountdown = 3000;
        public int NoPvpDuration = 600;
        public int NoPvpTimeWait = 10;
        public bool OverrideDamage = false;
        public bool OverrideItems = true;

        // OVERRIDE
        public bool OverrideLoots = true;
        public bool OverrideSpawns = true;
        public float OwnerDestroyAutoDisable = 60.0f;
        public int OwnerMaxComponents = 0;
        public bool OwnershipDestroyNoCarryWeight = false;

        // OWNERSHIP
        public bool OwnershipDestroyReceiveResources = false;

        /*public bool OwnershipNotOwnerDenyBuild = false;*/
        public bool OwnershipNotOwnerDenyBuild = false;
        public string[] OwnershipNotOwnerDenyDeploy = new string[0];

        // ANNOUNCES
        public bool PlayerJoin = true;
        public bool PlayerLeave = true;

        public Dictionary<int, string> RanksColor = new() { { 3, "#FFFF8F" } };
        public float RestartTime = 120.0f;

        public int SaveBackupCount = 5;

        // SERVER
        public string ServerName = "Bless Rust";
        public float ShutdownTime = 120.0f;
        public int SleepersLifeTime = 300;
        public string SystemColor = "#7FFFFF";
        public int TeleportCountdown = 300;
        public bool TeleportOutdoorsOnly = true;
        public ulong TeleportPayment = 0;
        public int TeleportTimeWait = 20;
        public bool UniqueNames = true;
        public bool UserDisplayRank = true;
        public bool VerifyChars = true;

        // USERS
        public bool VerifyNames = true;

        // WHITELIST
        public bool WhiteList = false;
    }
}