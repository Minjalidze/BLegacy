using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BCore.Configs;
using BCore.Users;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.ClanSystem;

[Flags]
public enum ClanMemberFlags
{
    Invite = 0x0001, // CAN INVITE MEMBERS  //
    Dismiss = 0x0002, // CAN DISMISS MEMBERS //
    Management = 0x0004, // CAN MANAGEMENT CLAN //
    ExpDetails = 0x0100 // EXPERIENCE DETAILS  //
}

[Flags]
public enum ClanFlags
{
    CanMotd = 0x0001, // CAN SET MESSAGE OF THE DAY //
    CanAbbr = 0x0002, // CAN HAVE ABBREVIATION      //
    CanFFire = 0x0004, // CAN TOGGLE FRIENDLY FIRE   //
    CanTax = 0x0008, // CAN HAVE A CLAN MONEY TAX  //
    CanWarp = 0x0010, // CAN USE WARP TO CLAN HOUSE //
    FriendlyFire = 0x0100, // FRIENDLYFIRE IS DISABLED   //
    NoDecayHouse = 0x0200 // CLAN HOUSE CANNOT BE DECAY //
}

public class Clans
{
    private static readonly string _saveFileName = "BClans.dat";
    public static int DefaultLevel = 0; // Default level of created clan
    public static uint CreateCost = 1000; // Amount in currency to create a clan

    public static float
        ExperienceMultiplier = 1.0f; // Multiplier of gain experience from gathering resources or murdering

    public static bool WarpOutdoorsOnly = false; // Players can use /clan warp command only in outdoors or own houses

    public static bool
        ClanWarDeathPay = true; // Clan is gain amount of currency when a member of clan kills a hostile clan member

    public static uint ClanWarDeathPercent = 10; // Gain percents for clan balance after member of hostile has died.

    public static bool
        ClanWarMurderFee = true; // Clan is lose amount of currency when member of clan died from hostile clan member

    public static uint ClanWarMurderPercent = 10; // Lose percents from clan balance after member of a clan has died.

    public static uint
        ClanWarDeclaredGainPercent =
            20; // Gain percents for clan(request) balance and experience when hostile clan declined from war.

    public static uint
        ClanWarDeclinedLostPercent =
            25; // Lost percents for clan(answer) balance and experience when this clan declined from war with other clan.

    public static string
        ClanWarDeclinedPenalty =
            "7d"; // Time of penalty for clan after decline war (s: seconds, m: mins, h: hours, d: days, y: years)

    public static string
        ClanWarAcceptedTime =
            "14d"; // Time of war between hostile clans (time abbr. s: seconds, m: mins, h: hours, d: days)

    public static string
        ClanWarEndedPenalty =
            "7d"; // Time of penalty for clans after war (s: seconds, m: mins, h: hours, d: days, y: years)


    // Dictionary(clan_id, ClanData) //
    public static Dictionary<uint, ClanData> Database;
    public static ClanConfig ClanCfg;
    public static string SaveFilePath { get; private set; }
    public static int Loaded { get; private set; }

    public static int Count => Database?.Count ?? 0;
    public static ClanData[] All => Database.Values.ToArray();

    public static void Initialize()
    {
        SaveFilePath = Path.Combine(@"serverdata\", _saveFileName);
        Database = new Dictionary<uint, ClanData>();

        if (File.Exists(@"serverdata\cfg\BCore\clans.cfg"))
        {
            LoadData();
        }
        else
        {
            ClanCfg = new ClanConfig
            {
                Levels = new List<ClanLevel>
                {
                    new()
                    {
                        Id = 0,
                        RequireLevel = -1,
                        RequireCurrency = 1000,
                        RequireExperience = 2500,
                        MaxMembers = 2,
                        CurrencyTax = 5,
                        WarpTimeWait = 40,
                        WarpCountdown = 1800,
                        FlagMotd = true,
                        FlagAbbr = false,
                        FlagFFire = false,
                        FlagTax = false,
                        FlagHouse = false,
                        BonusCraftingSpeed = 0,
                        BonusGatheringWood = 5,
                        BonusGatheringRock = 5,
                        BonusGatheringAnimal = 5,
                        BonusMembersPayMurder = 0,
                        BonusMembersDefense = 0,
                        BonusMembersDamage = 0
                    }
                },
                CraftExperienceCategory = new Dictionary<string, int>
                {
                    { "Food", 5 },
                    { "Resource", 10 },
                    { "Parts", 17 },
                    { "Survival", 2 },
                    { "Armor", 100 },
                    { "Weapons", 200 }
                },
                CraftExperienceItems = new Dictionary<string, int>
                {
                    { "Camp Fire", 0 },

                    { "Arrow", 2 },
                    { "Handmade Shell", 7 },
                    { "Shotgun Shells", 12 },
                    { "9mm Ammo", 5 },
                    { "556 Ammo", 10 },

                    { "Hatchet", 100 },
                    { "Pick Axe", 200 },
                    { "Hunting Bow", 200 },

                    { "HandCannon", 75 },
                    { "Pipe Shotgun", 400 },
                    { "Revolver", 500 },
                    { "9mm Pistol", 500 },
                    { "P250", 750 },
                    { "Shotgun", 850 },
                    { "MP5A4", 1000 },
                    { "M4", 1500 },
                    { "Bolt Action Rifle", 2000 },

                    { "F1 Grenade", 500 },
                    { "Explosive Charge", 2000 },

                    { "Cloth Helmet", 40 },
                    { "Cloth Vest", 80 },
                    { "Cloth Pants", 70 },
                    { "Cloth Boots", 30 },

                    { "Leather Helmet", 80 },
                    { "Leather Vest", 140 },
                    { "Leather Pants", 120 },
                    { "Leather Boots", 60 },

                    { "Rad Suit Helmet", 140 },
                    { "Rad Suit Vest", 300 },
                    { "Rad Suit Pants", 260 },
                    { "Rad Suit Boots", 100 },

                    { "Kevlar Helmet", 180 },
                    { "Kevlar Vest", 340 },
                    { "Kevlar Pants", 320 },
                    { "Kevlar Boots", 160 }
                }
            };
            SaveData();
        }

        LoadAsTextFile();
    }

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\clans.cfg", JsonConvert.SerializeObject(ClanCfg, Formatting.Indented));
        Debug.Log("[BCore]: Clans Config Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\clans.cfg");
        ClanCfg = JsonConvert.DeserializeObject<ClanConfig>(file);
        Debug.Log("[BCore]: Clans Config Loaded!");
    }

    public static bool LoadAsTextFile()
    {
        Loaded = 0;
        if (!File.Exists(SaveFilePath)) return false;
        var file = File.ReadAllText(SaveFilePath);
        if (string.IsNullOrEmpty(file)) return false;
        var data = file.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        string saveTitle = null;
        ClanData clanData = null;
        foreach (var str in data)
        {
            if (str.StartsWith("[") && str.EndsWith("]"))
            {
                clanData = null;
                if (saveTitle == null) continue;
                var clanID = str.Substring(1, str.Length - 2).ToUInt32();
                if (clanID == 0) continue;
                // Get existing user data or create new
                if (Database.ContainsKey(clanID))
                {
                    clanData = Database[clanID];
                }
                else
                {
                    clanID = Helper.NewSerial;
                    clanData = new ClanData(clanID);
                    Database.Add(clanID, clanData);
                    Loaded++;
                }

                continue;
            }

            // Get Title, Version, SaveTime of this Savefile //
            var var = str.Split('=');
            if (var.Length < 2) continue;
            if (clanData == null)
            {
                if (var[0].Equals("TITLE", StringComparison.OrdinalIgnoreCase)) saveTitle = var[1];

                continue;
            }

            switch (var[0].ToUpper())
            {
                case "NAME":
                    clanData.Name = var[1].Trim();
                    break;
                case "ABBREV":
                    clanData.Abbr = var[1].Trim();
                    break;
                case "LEADER":
                    clanData.LeaderID = ulong.Parse(var[1]);
                    break;
                case "FLAGS":
                    clanData.Flags = var[1].ToEnum<ClanFlags>();
                    break;
                case "BALANCE":
                    clanData.Balance = ulong.Parse(var[1]);
                    break;
                case "TAX":
                    clanData.Tax = uint.Parse(var[1]);
                    break;
                case "LEVEL":
                    clanData.SetLevel(ClanCfg.Levels.Find(f => f.Id == uint.Parse(var[1])));
                    break;
                case "EXPERIENCE":
                    clanData.Experience = ulong.Parse(var[1]);
                    break;
                case "LOCATION":
                    var = var[1].Split(',');
                    if (var.Length > 0) float.TryParse(var[0].Trim(), out clanData.Location.x);
                    if (var.Length > 1) float.TryParse(var[1].Trim(), out clanData.Location.y);
                    if (var.Length > 2) float.TryParse(var[2].Trim(), out clanData.Location.z);
                    break;
                case "MOTD":
                    clanData.MOTD = var[1].Trim();
                    break;
                case "HOSTILE":
                    var = var[1].Split(',');
                    if (var.Length < 2) continue;
                    clanData.Hostile.Add(var[0].ToUInt32(), DateTime.Parse(var[1]));
                    break;
                case "MEMBER":
                    var = var[1].Split(',');
                    var memberID = ulong.Parse(var[0]);
                    var memberUser = Data.FindUser(memberID);
                    if (memberUser == null) continue;
                    for (var i = 1; i < var.Length; i++) var[i - 1] = var[i];
                    Array.Resize(ref var, var.Length - 1);
                    var memberFlags = string.Join(",", var).ToEnum<ClanMemberFlags>();
                    memberUser.Clan = clanData.Name;
                    clanData.Members.Add(memberUser, memberFlags);
                    break;
            }
        }

        return true;
    }

    public static ClanData GetClan(string name) => Find(name);
    public static int SaveAsTextFile()
    {
        using (var file = File.CreateText(SaveFilePath + ".new"))
        {
            file.WriteLine("TITLE=" + "BCore");
            file.WriteLine();
            foreach (var idD in Database.Keys)
            {
                file.WriteLine("[" + idD.ToHex() + "]");
                file.WriteLine("NAME=" + Database[idD].Name);
                file.WriteLine("ABBREV=" + Database[idD].Abbr);
                file.WriteLine("LEADER=" + Database[idD].LeaderID);
                file.WriteLine("FLAGS=" + Database[idD].Flags.ToString().Replace(" ", ""));
                file.WriteLine("BALANCE=" + Database[idD].Balance);
                file.WriteLine("TAX=" + Database[idD].Tax);
                file.WriteLine("LEVEL=" + Database[idD].Level.Id);
                file.WriteLine("EXPERIENCE=" + Database[idD].Experience);
                file.WriteLine("LOCATION=" + Database[idD].Location.x + "," + Database[idD].Location.y + "," +
                               Database[idD].Location.z);
                file.WriteLine("MOTD=" + Database[idD].MOTD);
                if (Database[idD].Hostile.Count > 0)
                    foreach (var id in Database[idD].Hostile.Keys)
                        file.WriteLine("HOSTILE=" + id.ToHex() + "," +
                                       Database[idD].Hostile[id].ToString("MM/dd/yyyy HH:mm:ss"));
                foreach (var memberData in Database[idD].Members.Keys)
                    file.WriteLine("MEMBER=" + memberData.SteamID + "," +
                                   Database[idD].Members[memberData].ToString().Replace(" ", ""));
                file.WriteLine();
            }
        }

        Helper.CreateFileBackup(SaveFilePath);
        File.Move(SaveFilePath + ".new", SaveFilePath);
        return Database.Count;
    }

    #region [Public] Create clan

    public static ClanData Create(string name, ulong leaderID)
    {
        var clanData = new ClanData(Helper.NewSerial, name, "", leaderID);
        Database.Add(clanData.ID, clanData);
        return clanData;
    }

    #endregion

    #region [Public] Get clan by id

    public static ClanData Get(uint id)
    {
        return Database.ContainsKey(id) ? Database[id] : null;
    }

    #endregion

    #region [Public] Member Join to Clan

    public static bool MemberJoin(ClanData clanData, User userData)
    {
        if (clanData == null || userData == null || clanData.Members.ContainsKey(userData)) return false;
        ClanMemberFlags userFlags = 0;
        if (clanData.LeaderID == userData.SteamID)
            userFlags |= ClanMemberFlags.Invite | ClanMemberFlags.Dismiss | ClanMemberFlags.Management;
        clanData.Members.Add(userData, userFlags);
        userData.Clan = clanData.Name;
        var netUser = NetUser.FindByUserID(userData.SteamID);
        if (netUser != null)
            Broadcast.Message(netUser,
                Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanPlayerJoined, clanData, null, userData));
        return true;
    }

    #endregion

    #region [Public] Member Leave from Clan

    public static bool MemberLeave(ClanData clanData, User userData)
    {
        if (clanData == null || userData == null || userData.Clan != clanData.Name) return false;
        if (!clanData.Members.ContainsKey(userData)) return false;
        clanData.Members.Remove(userData);
        userData.Clan = null;
        var
            netUser = NetUser.FindByUserID(userData
                .SteamID);
        if (netUser != null)
            Broadcast.Message(netUser,
                Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanPlayerLeaved, clanData, null, userData));
        return true;
    }

    #endregion

    #region [Public] Accepts leadership of clan

    public static void TransferAccept(ClanData clanData, User userData)
    {
        var netLeader = NetUser.FindByUserID(clanData.LeaderID);
        if (netLeader != null)
            Broadcast.MessageClan(netLeader, clanData,
                Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferQueryAnswerY, clanData, null,
                    userData));
        clanData.LeaderID = userData.SteamID;
        clanData.Members[userData] = ClanMemberFlags.Invite | ClanMemberFlags.Dismiss | ClanMemberFlags.Management;
        Broadcast.MessageClan(clanData,
            Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferSuccess, clanData, null, userData));
    }

    #endregion

    #region [Public] Decline leadership of clan

    public static void TransferDecline(ClanData clanData, User userData)
    {
        var netLeader = NetUser.FindByUserID(clanData.LeaderID);
        if (netLeader == null) return;
        Broadcast.MessageClan(netLeader, clanData,
            Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferQueryAnswerN, clanData, null,
                userData));
    }

    #endregion

    public class ClanConfig
    {
        public Dictionary<string, int> CraftExperienceCategory;
        public Dictionary<string, int> CraftExperienceItems;
        public List<ClanLevel> Levels;
    }

    #region [Public] Remove clan

    public static void Remove(ClanData clanData)
    {
        if (clanData != null) Remove(clanData.ID);
    }

    public static void Remove(string name)
    {
        var result = Find(name);
        if (result != null) Remove(result);
    }

    public static void Remove(uint id)
    {
        if (!Database.ContainsKey(id)) return;
        Database[id].Hostile.Clear();
        Database[id].Members.Clear();
        Database.Remove(id);
    }

    #endregion

    #region [Public] Find clan by name or leader id

    public static ClanData Find(string name)
    {
        if (!string.IsNullOrEmpty(name))
            foreach (var id in Database.Keys)
            {
                if (Database[id].Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return Database[id];
                if (Database[id].Abbr.Equals(name, StringComparison.OrdinalIgnoreCase)) return Database[id];
            }

        return null;
    }

    public static ClanData Find(ulong leaderID)
    {
        return (from id in Database.Keys where Database[id].LeaderID == leaderID select Database[id]).FirstOrDefault();
    }

    #endregion
}