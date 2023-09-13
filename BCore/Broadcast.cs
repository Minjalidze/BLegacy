using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BCore.ClanSystem;
using BCore.Commands;
using BCore.Configs;
using BCore.Users;
using UnityEngine;
using Config = BCore.Configs.Config;

namespace BCore;

public enum Color
{
    Null = -1,

    White = 0,
    Black = 1,

    Red = 2,
    Blue = 3,
    Green = 4,
    Purple = 5,
    Yellow = 6,

    Brown = 7,
    Gray = 8,
    Orange = 9,

    Magenta = 10,
    Lime = 11,
    Aqua = 12,
    Pink = 13
}

public static class ColorHelper
{
    public static string ToColor(this string str, Color color)
    {
        return $"{Broadcast.Colors[(int)color]}{str}";
    }
}

public static class Broadcast
{
    private static readonly Dictionary<uLink.NetworkPlayer, DateTime> TimeWait = new();

    public static readonly List<string> Colors = new()
    {
        "[COLOR#FFFFFF]",
        "[COLOR#000000]",

        "[COLOR#FF0000]",
        "[COLOR#0000FF]",
        "[COLOR#008000]",
        "[COLOR#800080]",
        "[COLOR#FFFF00]",

        "[COLOR#A52A2A]",
        "[COLOR#808080]",
        "[COLOR#FFA500]",

        "[COLOR#FF00FF]",
        "[COLOR#00FF00]",
        "[COLOR#00FFFF]",
        "[COLOR#FFC0CB]"
    };

    public static void SendMessage(NetUser player, string text, Color color = Color.Null, string sender = null)
    {
        try
        {
            if (text.IsEmpty()) return;
            if (player == null)
            {
                ConsoleSystem.Print(text);
                return;
            }

            if (color is not Color.Null) text = text.ToColor(color);
            Message(player.networkPlayer, text, sender);
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    public static void NoticeAll(string icon, string text, NetUser sender = null, float duration = 5f)
    {
        try
        {
            if (text.IsEmpty()) return;
            text = "notice.popup \"" + duration + "\" " + icon + "\" " + Helper.QuoteSafe(text);
            foreach (var PC in PlayerClient.All)
                if (PC.netUser != sender)
                    ConsoleNetworker.SendClientCommand(PC.netPlayer, text);
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }
    public static void ChatPm(NetUser sender, NetUser client, string text)
    {
        try
        {
            if (text.IsEmpty()) return;
            var clientPM = Helper.QuoteSafe(Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMTo) + " " +
                                            client.displayName);
            var senderPM = Helper.QuoteSafe(Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMFrom) + " " +
                                            sender.displayName);
            var textColor = Helper.GetChatTextColor(Config._Settings.ChatWhisperColor);
            text = Regex.Replace(text, @"(\[COLOR\s*\S*])|(\[/COLOR\s*\S*])", "", RegexOptions.IgnoreCase).Trim();
            Debug.Log("[PM] \"" + sender.displayName + "\" for \"" + client.displayName + "\" say " + text);
            var chatLines = Helper.WarpChatText(text, Config._Settings.ChatLineMaxLength);
            foreach (var t in chatLines)
            {
                ConsoleNetworker.SendClientCommand(sender.networkPlayer,
                    "chat.add " + clientPM + " " + Helper.QuoteSafe(textColor + t));
                ConsoleNetworker.SendClientCommand(client.networkPlayer,
                    "chat.add " + senderPM + " " + Helper.QuoteSafe(textColor + t));
            }
            if (!Config.History.ContainsKey(sender.userID))
            {
                Config.History.Add(sender.userID, new List<HistoryRecord>());
            }
            if (Config.History[sender.userID].Count > Config._Settings.ChatHistoryStored)
            {
                Config.History[sender.userID].RemoveAt(0);
            }
            Config.History[sender.userID].Add(default(HistoryRecord).Init(Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMTo, null, null) + " " + client.displayName, text));
            if (!Config.History.ContainsKey(client.userID))
            {
                Config.History.Add(client.userID, new List<HistoryRecord>());
            }
            if (Config.History[client.userID].Count > Config._Settings.ChatHistoryStored)
            {
                Config.History[client.userID].RemoveAt(0);
            }
            Config.History[client.userID].Add(default(HistoryRecord).Init(Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMFrom, null, null) + " " + sender.displayName, text));
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }

    public static void MessageClan(ClanData clan, string text)
    {
        try
        {
            if (text.IsEmpty()) return;
            if (clan == null) return;
            var sender = Config._Settings.ChatClanIcon;
            if (sender == "") sender = "<" + (clan.Abbr.IsEmpty() ? clan.Name : clan.Abbr) + ">";
            text = "chat.add " + Helper.QuoteSafe(sender) + " " +
                   Helper.QuoteSafe(Helper.GetChatTextColor(Config._Settings.ChatClanColor) + text.Trim('"'));
            PlayerClient PC;
            foreach (var UD in clan.Members.Keys)
                if (PlayerClient.FindByUserID(UD.SteamID, out PC))
                    ConsoleNetworker.SendClientCommand(PC.netPlayer, text);
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }

    public static void MessageClan(ClanData clan, string text, NetUser AsUser = null)
    {
        try
        {
            if (text.IsEmpty()) return;
            if (clan == null) return;
            var sender = Config._Settings.ChatClanIcon;
            if (sender == "") sender = "<" + (clan.Abbr.IsEmpty() ? clan.Name : clan.Abbr) + ">";
            if (AsUser != null) sender = AsUser.displayName + Config._Settings.ChatDivider + sender;
            text = "chat.add " + Helper.QuoteSafe(sender) + " " +
                   Helper.QuoteSafe(Helper.GetChatTextColor(Config._Settings.ChatClanColor) + text.Trim('"'));
            PlayerClient PC;
            foreach (var UD in clan.Members.Keys)
                if (PlayerClient.FindByUserID(UD.SteamID, out PC))
                    ConsoleNetworker.SendClientCommand(PC.netPlayer, text);
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }

    public static void MessageClan(NetUser user, string text)
    {
        try
        {
            if (text.IsEmpty()) return;
            var sender = "<Undefined>";
            if (user == null)
            {
                ConsoleSystem.Print(text);
                return;
            }

            var clan = Clans.Find(Data.FindUser(user.userID).Clan);
            if (clan == null) return;
            sender = "<" + (clan.Abbr.IsEmpty() ? clan.Name : clan.Abbr) + ">";
            text = "chat.add " + Helper.QuoteSafe(sender) + " " +
                   Helper.QuoteSafe(Helper.GetChatTextColor(Config._Settings.ChatClanColor) + text.Trim('"'));
            ConsoleNetworker.SendClientCommand(user.networkPlayer, text);
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }

    public static void MessageClan(NetUser user, ClanData clan, string text)
    {
        try
        {
            if (text.IsEmpty()) return;
            var sender = "<Undefined>";
            if (user == null)
            {
                if (clan != null) sender = "<" + (clan.Abbr.IsEmpty() ? clan.Name : clan.Abbr) + ">";
                ConsoleSystem.Print(sender + ": " + text);
                return;
            }

            if (clan == null) clan = Clans.Find(Data.FindUser(user.userID).Clan);
            if (clan == null) return;
            sender = "<" + (clan.Abbr.IsEmpty() ? clan.Name : clan.Abbr) + ">";
            text = "chat.add " + Helper.QuoteSafe(sender) + " " +
                   Helper.QuoteSafe(Helper.GetChatTextColor(Config._Settings.ChatClanColor) + text.Trim('"'));
            ConsoleNetworker.SendClientCommand(user.networkPlayer, text);
        }
        catch (Exception E)
        {
            Debug.Log("ERROR: " + E.Message);
        }
    }

    public static void Message(NetUser player, string text, string sender = null, float timeWait = 0)
    {
        try
        {
            if (text.IsEmpty()) return;
            if (player == null)
            {
                ConsoleSystem.Print(text);
                return;
            }

            Message(player.networkPlayer, text, sender, timeWait);
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    public static void Message(uLink.NetworkPlayer player, string text, string sender = null, float timewait = 0)
    {
        if (text.IsEmpty()) return;
        if (TimeWait.ContainsKey(player) && TimeWait[player] > DateTime.Now) return;
        if (timewait > 0)
        {
            if (!TimeWait.ContainsKey(player))
                TimeWait.Add(player, DateTime.Now.AddMilliseconds(1000 * timewait));
            else
                TimeWait[player] = DateTime.Now.AddMilliseconds(1000 * timewait);
        }

        if (player == null)
        {
            ConsoleSystem.Print(text);
            return;
        }

        if (string.IsNullOrEmpty(sender)) sender = CommandHelper.GetChatName();
        text = Helper.GetChatTextColor(Config._Settings.SystemColor) + text.Trim('"');
        ConsoleNetworker.SendClientCommand(player,
            "chat.add " + Helper.QuoteSafe(sender) + " " + Helper.QuoteSafe(text));
    }

    public static void Notice(NetUser player, string icon, string text, float duration = 5f)
    {
        try
        {
            if (text.IsEmpty()) return;
            if (player == null)
            {
                ConsoleSystem.Print(text);
                return;
            }

            Notice(player.networkPlayer, icon, text, duration);
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    public static void Notice(uLink.NetworkPlayer player, string icon, string text, float duration = 5f)
    {
        try
        {
            if (text.IsEmpty()) return;

            ConsoleNetworker.SendClientCommand(player,
                "notice.popup \"" + duration + "\" \"" + icon + "\" " + Helper.QuoteSafe(text));
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    public static void MessageAll(string text, NetUser exclude)
    {
        try
        {
            if (text.IsEmpty()) return;
            text = "chat.add " + Helper.QuoteSafe(Config._Settings.ServerName) + " " +
                   Helper.QuoteSafe(Helper.GetChatTextColor(Config._Settings.SystemColor) + text.Trim('"'));
            foreach (var pc in PlayerClient.All.Where(pc => pc.netUser != exclude))
                ConsoleNetworker.SendClientCommand(pc.netPlayer, text);
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }

    public static void MessageAll(string text)
    {
        try
        {
            if (text.IsEmpty()) return;
            text = Helper.GetChatTextColor(Config._Settings.SystemColor) + text.Trim('"');
            ConsoleNetworker.Broadcast("chat.add " + Helper.QuoteSafe(Config._Settings.ServerName) + " " +
                                       Helper.QuoteSafe(text));
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }
    public static void MessageAll(string name, string text)
    {
        try
        {
            if (text.IsEmpty() || name.IsEmpty()) return;
            ConsoleNetworker.Broadcast("chat.add " + Helper.QuoteSafe(name) + " " + Helper.QuoteSafe(text));
        }
        catch (Exception e)
        {
            Debug.Log("ERROR: " + e.Message);
        }
    }
}