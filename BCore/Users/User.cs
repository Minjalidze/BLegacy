using System;
using System.Collections.Generic;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.WorldManagement;

namespace BCore.Users;

public class User
{
    public readonly Dictionary<string, DateTime> KitCoolDowns = new();
    public readonly List<ulong> SharedList = new();

    public readonly List<Data.UserFlags> UserFlags = new();

    public bool CanTeleportShot;

    public string FirstConnectIP;
    public bool HasUnlimitedAmmo;
    public string LastConnectIP;
    public int Rank = 0;

    public WorldZone Zone;

    public User(ulong steamID, string userName, string ip, string hardwareID)
    {
        SteamID = steamID;
        UserName = userName;
        FirstConnectIP = ip;
        LastConnectIP = ip;
        HardwareID = hardwareID;
    }

    public string UserName { get; set; }
    public ulong SteamID { get; set; }
    public string HardwareID { get; set; }
    public string Clan { get; set; }

    public string Language { get; set; } = "Ru";

    public bool Details => HasFlag(Data.UserFlags.Details);

    public void SetFlag(Data.UserFlags flag)
    {
        UserFlags.Add(flag);
    }

    public bool HasFlag(Data.UserFlags flag)
    {
        return UserFlags.Contains(flag);
    }

    public void RemoveFlag(Data.UserFlags flag)
    {
        UserFlags.Remove(flag);
    }

    public bool HasShared(ulong uID)
    {
        return SharedList.Contains(uID);
    }

    public bool HasCountdown(string command)
    {
        return Data.CountdownList(SteamID).Exists(f => f.Command == command);
    }
}