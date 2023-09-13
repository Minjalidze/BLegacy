using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace BCore.Users;

public static class Bans
{
    public static readonly List<BannedUser> BanList = new();

    public static void InitBans()
    {
        BanList.Clear();

        var webClient = new WebClient();
        var response = webClient.DownloadString("http://blessrust.site/API/BanAPI.php?action=getBans");

        if (string.IsNullOrEmpty(response))
        {
            Debug.Log($"[BCore]: Ban System Synchronized. \r\nTotal Banned Count: {BanList.Count}.");
            return;
        }

        var users = response.Split('|');
        if (users.Length <= 1) return;

        foreach (var user in users)
        {
            var data = user.Split('&');
            if (data.Length <= 1) continue;

            var userName = data[0];
            var steamID = ulong.Parse(data[1]);
            var hardwareID = data[2];
            var ip = data[3];
            var reason = data[4];

            BanList.Add(new BannedUser(userName, steamID, hardwareID, ip, reason));
        }

        Debug.Log($"[BCore]: Ban System Synchronized. \r\nTotal Banned Count: {BanList.Count}.");
    }

    public static void Unban(string nick)
    {
        var webClient = new WebClient();
        webClient.DownloadString($"http://blessrust.site/API/BanAPI.php?action=unbannick&nickname={nick}");
    }

    public static void Unban(ulong steamID)
    {
        var webClient = new WebClient();
        webClient.DownloadString($"http://blessrust.site/API/BanAPI.php?action=unbansteamid&steamid={steamID}");
    }

    public static void DoBan(ulong steamID, string reason)
    {
        var user = Data.Users.Find(f => f.SteamID == steamID);
        var webClient = new WebClient();
        var response = webClient.DownloadString("http://blessrust.site/API/BanAPI.php?action=ban" +
                                                $"&nickname={user.UserName}" +
                                                $"&steamid={user.SteamID}" +
                                                $"&ip={user.LastConnectIP}" +
                                                "&hwid=samplehwid" +
                                                $"&reason={reason}");
        BanList.Add(new BannedUser(user.UserName, steamID, user.HardwareID, user.LastConnectIP, reason));
    }

    public static string GetBanReason(string nickname)
    {
        var webClient = new WebClient();
        return webClient.DownloadString(
            $"http://blessrust.site/API/BanAPI.php?action=getbanreason&nickname={nickname}");
    }

    public static BannedUser GetBannedUser(string userName)
    {
        return BanList.Find(f => f.UserName == userName);
    }

    public static BannedUser GetBannedUser(ulong steamID)
    {
        return BanList.Find(f => f.SteamID == steamID);
    }

    public class BannedUser
    {
        public BannedUser(string userName, ulong steamID, string hardwareID, string ip, string reason)
        {
            UserName = userName;
            SteamID = steamID;
            HardwareID = hardwareID;
            IP = ip;
            Reason = reason;
        }

        public string UserName { get; set; }
        public ulong SteamID { get; set; }
        public string HardwareID { get; set; }
        public string IP { get; set; }
        public string Reason { get; set; }
    }
}