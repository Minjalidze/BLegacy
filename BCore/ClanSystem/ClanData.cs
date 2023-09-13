using System;
using System.Collections.Generic;
using System.Linq;
using BCore.Users;
using UnityEngine;

namespace BCore.ClanSystem;

public class ClanData
{
    public string Abbr;
    public ulong Balance;
    public ulong Experience;
    public ClanFlags Flags;
    public Dictionary<uint, DateTime> Hostile;
    public uint ID;
    public ulong LeaderID;
    public ClanLevel Level;
    public Vector3 Location;
    public Dictionary<User, ClanMemberFlags> Members;
    public string MOTD;
    public string Name;

    public uint Tax;
    //public void Message(string text) { Broadcast.MessageClan(this, text); }

    public ClanData(uint id, string name = null, string abbr = null, ulong leaderID = 0)
    {
        ID = id;
        Name = name;
        Abbr = abbr;
        LeaderID = leaderID;
        Balance = 0;
        Tax = 10;
        Level = new ClanLevel();
        Experience = 0;
        Location = Vector3.zero;
        MOTD = "";
        Hostile = new Dictionary<uint, DateTime>();
        Members = new Dictionary<User, ClanMemberFlags>();
    }

    public bool FriendlyFire
    {
        get => (Flags & ClanFlags.FriendlyFire) == ClanFlags.FriendlyFire;
        set => Flags = Flags.SetFlag(ClanFlags.FriendlyFire, value);
    }

    public int Online => Members.Keys.Count(u => NetUser.FindByUserID(u.SteamID) != null);

    public ulong Hash
    {
        get
        {
            var value = LeaderID;
            value += (ulong)(Name?.GetHashCode() ?? 0);
            value += (ulong)(Abbr?.GetHashCode() ?? 0);
            value += (ulong)Flags.GetHashCode();
            value += Balance;
            value += Tax;
            value += (ulong)Level.GetHashCode();
            value += Experience;
            value += (ulong)Location.GetHashCode();
            value += (ulong)(MOTD?.GetHashCode() ?? 0);
            value = Members.Keys.Aggregate(value, (current, member) => current + (ulong)Members[member]);
            return Hostile.Keys.Aggregate(value, (current, id) => current + id);
        }
    }

    public bool SetLevel(ClanLevel level)
    {
        if (level == null) return false;
        Level = level;
        Tax = level.CurrencyTax;
        Flags = Flags.SetFlag(ClanFlags.CanMotd, level.FlagMotd);
        Flags = Flags.SetFlag(ClanFlags.CanAbbr, level.FlagAbbr);
        Flags = Flags.SetFlag(ClanFlags.CanFFire, level.FlagFFire);
        Flags = Flags.SetFlag(ClanFlags.CanTax, level.FlagTax);
        Flags = Flags.SetFlag(ClanFlags.CanWarp, level.FlagHouse);
        return true;
    }
}