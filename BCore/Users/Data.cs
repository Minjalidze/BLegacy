using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;
using Avatar = RustProto.Avatar;

namespace BCore.Users;

[Serializable]
public class Countdown
{
    public Countdown(string command, double time = 0)
    {
        Command = command;
        Expires = time > 0;
        Stamp = Expires ? DateTime.Now.AddSeconds(time) : new DateTime();
    }

    public Countdown(string command, DateTime stamp = new())
    {
        Command = command;
        Expires = stamp.Ticks > 0;
        Stamp = stamp;
    }

    public DateTime Stamp { get; private set; }
    public string Command { get; private set; }
    public bool Expires { get; private set; }
    public bool Expired => Expires && DateTime.Now > Stamp;
    public double TimeLeft => Expires ? (Stamp - DateTime.Now).TotalSeconds : -1;
}

[StructLayout(LayoutKind.Sequential)]
public struct HistoryRecord
{
    public string Name;
    public string Text;

    public HistoryRecord Init(string name, string text)
    {
        Name = name;
        Text = text;
        return this;
    }
}

#region ChatQuery|Answer: Answer

[Serializable]
public class UserAnswer
{
    public UserAnswer(string text, Action action)
    {
        Text = text;
        Action = action;
    }

    public string Text { get; private set; }
    public Action Action { get; private set; }
}

#endregion

#region ChatQuery|Answer: Query

[Serializable]
public class UserQuery
{
    public List<UserAnswer> answer;

    public UserQuery(User userdata, string query, uint lifetime = 10)
    {
        Query = query;
        Userdata = userdata;
        answer = new List<UserAnswer>();
        Timeout = (uint)Environment.TickCount + lifetime * 1000;
    }

    public string Query { get; private set; }
    public User Userdata { get; private set; }
    public uint Timeout { get; private set; }

    public bool Answered(string text)
    {
        var result = false;
        foreach (var a in answer)
        {
            var replace = a.Text.Replace("*", "");
            if (replace == "") continue;
            if (a.Text.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                a.Action.Invoke();
                result = true;
            }
            else if (a.Text.StartsWith("*") && text.EndsWith(replace, StringComparison.OrdinalIgnoreCase))
            {
                a.Action.Invoke();
                result = true;
            }
            else if (a.Text.EndsWith("*") && text.StartsWith(replace, StringComparison.OrdinalIgnoreCase))
            {
                a.Action.Invoke();
                result = true;
            }
        }

        if (result) return true;
        {
            foreach (var a in answer.Where(a => a.Text.Replace("*", "") == ""))
            {
                a.Action.Invoke();
                result = true;
            }
        }

        return result;
    }
}

#endregion

public static class Data
{
    public enum UserFlags
    {
        Guest = 0x0000,
        Normal = 0x0001,
        Premium = 0x0002,
        Whitelisted = 0x0004,
        Banned = 0x0008,
        Admin = 0x0010,
        GodMode = 0x0020,
        Invisible = 0x0040,
        NoPvp = 0x0080,
        Online = 0x0100,
        SafeBoxes = 0x0200,
        Freeze = 0x0800,
        Details = 0x1000
    }

    public static bool UniqueNames = true;

    public static List<User> Users;
    public static Dictionary<ulong, List<Countdown>> Countdown;
    public static Dictionary<ulong, Avatar> Avatar;

    public static List<User> All => Users;

    public static User FindUser(string userName)
    {
        return Users.Find(f =>
            f.UserName.ToLower().Contains(userName.ToLower()) ||
            string.Equals(f.UserName, userName, StringComparison.CurrentCultureIgnoreCase));
    }

    public static User FindUser(ulong steamID)
    {
        return Users.Find(f => f.SteamID == steamID);
    }

    public static void LoadUsers()
    {
        Countdown = new Dictionary<ulong, List<Countdown>>();
        Avatar = new Dictionary<ulong, Avatar>();
        if (File.Exists(@"serverdata\BUsers.txt"))
        {
            var json = File.ReadAllText(@"serverdata\BUsers.txt");
            Users = JsonConvert.DeserializeObject<List<User>>(json);
        }
        else
        {
            Users = new List<User>();
            SaveUsers();
        }

        Debug.Log("[BCore]: Users Data Loaded.");
    }

    public static List<Countdown> CountdownList(ulong steamID)
    {
        if (!Countdown.ContainsKey(steamID)) Countdown.Add(steamID, new List<Countdown>());
        return Countdown[steamID];
    }

    #region [Public] Get countdown of user by steam id and command

    public static Countdown CountdownGet(ulong steam_id, string command)
    {
        if (!Countdown.ContainsKey(steam_id)) return null;
        return Countdown[steam_id].Find(F => F.Command == command);
    }

    #endregion

    public static string GetUsername(ulong steam_id)
    {
        return FindUser(steam_id) is null ? null : FindUser(steam_id).UserName;
    }

    #region [Public] Add countdown for user by steam id

    public static bool CountdownAdd(ulong steam_id, Countdown countdown)
    {
        if (!Countdown.ContainsKey(steam_id)) Countdown.Add(steam_id, new List<Countdown>());
        if (Countdown[steam_id].Exists(C => C.Command == countdown.Command)) return false;
        Countdown[steam_id].Add(countdown);
        return true;
    }

    #endregion

    #region [Public] Clear user countdowns by steam id

    public static void CountdownsClear(ulong steam_id)
    {
        if (!Countdown.ContainsKey(steam_id)) Countdown.Add(steam_id, new List<Countdown>());
        Countdown[steam_id].Clear();
    }

    #endregion

    public static void SaveUsers()
    {
        var json = JsonConvert.SerializeObject(Users, Formatting.Indented);
        File.WriteAllText(@"serverdata\BUsers.txt", json);
        Debug.Log($"[BCore]: Users Database Saved. \r\n[BCore]: Total Users Count: {Users.Count}.");
    }

    public static void SaveUser(User user)
    {
        Users.Add(user);
        var json = JsonConvert.SerializeObject(Users, Formatting.Indented);
        File.WriteAllText(@"serverdata\BUsers.txt", json);
    }

    #region [Public] Remove user countdown by steam id

    public static bool CountdownRemove(ulong steam_id, string command)
    {
        if (!Countdown.ContainsKey(steam_id)) Countdown.Add(steam_id, new List<Countdown>());
        return CountdownRemove(steam_id, Countdown[steam_id].Find(C => C.Command == command));
    }

    public static bool CountdownRemove(ulong steam_id, Countdown countdown)
    {
        if (!Countdown.ContainsKey(steam_id)) Countdown.Add(steam_id, new List<Countdown>());
        if (countdown == null || !Countdown[steam_id].Exists(C => C.Command == countdown.Command)) return false;
        return Countdown[steam_id].Remove(countdown);
    }

    #endregion
}