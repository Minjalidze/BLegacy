using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.EventSystem;
using BCore.Mods;
using BCore.Users;
using BCore.WorldManagement;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BCore.Commands;

internal static class CommandHelper
{
    public static string GetChatName()
    {
        return Configs.Config._Settings.ServerName;
    }

    public static void SendCommandDescription(ICommand command, NetUser user, User userData)
    {
        foreach (var desc in userData.Language == "Ru" ? command.RuDescription : command.EngDescription)
            CommandHook.Rust.SendChatMessage(user, GetChatName(), desc.ToColor(Color.Orange));
    }
}

public class About : ICommand
{
    public string CmdName => "about";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        Broadcast.Message(user, "The server is managed by:");
        Broadcast.Message(user, "BCore v5.13.2 <RELEASE>");
        Broadcast.Message(user, "Единственная фраза, которая мотивировала писать данное ядро - \"Monzi Pidor\".");
    }
}

public class Tele : ICommand
{
    public string CmdName => "tele";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser sender, string command, string[] args, User userData = null)
    {
        CommandHook.GetCommand("tp").Execute(sender, command, args, userData);
    }
}

public class Tp : ICommand
{
    public string CmdName => "tp";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser sender, string command, string[] args, User userData = null)
    {
        if (args == null || args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, sender, userData);
            return;
        }

        var client = sender.playerClient;
        PlayerClient target = null;
        User targetData = null;
        var toPosition = Vector3.zero;

        if (sender.admin || command.Equals("tele", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length > 0 && args[0].Contains(","))
                args = string.Join(" ", args).Replace(",", " ")
                    .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            switch (args.Length)
            {
                case 1 when (target = Helper.GetPlayerClient(args[0])) == null:
                    Broadcast.Notice(sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null, args[0]));
                    return;
                case 1 when client == target:
                    Broadcast.Notice(sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportOnSelf, client.netUser));
                    return;
                case 1:
                {
                    toPosition = target.controllable.character.transform.position + new Vector3(0f, 0.1f, 0f);
                    if (!target.netPlayer.isClient || !target.hasLastKnownPosition)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotCan,
                                target.netUser));
                        return;
                    }

                    break;
                }
                case 2 when (client = Helper.GetPlayerClient(args[0])) == null:
                    Broadcast.Notice(sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null, args[0]));
                    return;
                case 2 when (target = Helper.GetPlayerClient(args[1])) == null:
                    Broadcast.Notice(sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null, args[1]));
                    return;
                case 2 when client == target:
                    Broadcast.Notice(sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportToSelf, client.netUser));
                    return;
                case 2:
                {
                    toPosition = target.controllable.character.transform.position + new Vector3(0f, 0.1f, 0f);
                    if (!client.netPlayer.isClient || !client.hasLastKnownPosition)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotCan,
                                client.netUser));
                        return;
                    }

                    if (!target.netPlayer.isClient || !target.hasLastKnownPosition)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotCan,
                                target.netUser));
                        return;
                    }

                    break;
                }
                case 3:
                {
                    if (!float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY) ||
                        !float.TryParse(args[2], out var posZ))
                    {
                        CommandHelper.SendCommandDescription(this, sender, userData);
                        return;
                    }

                    toPosition = new Vector3(posX, posY, posZ);
                    break;
                }
            }

            Helper.TeleportTo(client.netUser, toPosition);
        }
        else
        {
            var playerPos = sender.playerClient.controllable.character.transform.position;
            if (Configs.Config._Settings.TeleportOutdoorsOnly)
                foreach (var collider in Physics.OverlapSphere(playerPos, 1f, GameConstant.Layer.kMask_ServerExplosion))
                {
                    var main = IDBase.GetMain(collider);
                    if (main == null) continue;

                    var structure = main.GetComponent<StructureMaster>();
                    if (structure == null || structure.ownerID == sender.userID) continue;

                    var ownerData = Data.FindUser(structure.ownerID);
                    if (ownerData != null && ownerData.HasShared(sender.userID)) continue;

                    Broadcast.Notice(sender, "☢",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotHere, sender));
                    return;
                }

            var countdown = Data.CountdownList(userData.SteamID).Find(f => f.Command == command);
            if (countdown != null)
            {
                if (!countdown.Expired)
                {
                    var time = TimeSpan.FromSeconds(countdown.TimeLeft);
                    if (time.TotalHours >= 1)
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportCountdown, sender)
                                .Replace("%TIME%",
                                    $"{time.TotalHours:F0}:{time.Minutes:D2}:{time.Seconds:D2}"));
                    else if (time.TotalMinutes >= 1)
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportCountdown, sender)
                                .Replace("%TIME%", $"{time.Minutes}:{time.Seconds:D2}"));
                    else
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportCountdown, sender)
                                .Replace("%TIME%", $"{time.Seconds}"));
                    return;
                }

                Data.CountdownRemove(userData.SteamID, countdown);
            }

            if ((target = Helper.GetPlayerClient(args[0])) == null)
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender, args[0]));
                return;
            }

            if (client == target)
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportOnSelf, sender));
                return;
            }

            var clientData = Data.FindUser(client.userID);
            targetData = Data.FindUser(target.userID);
            if (clientData == null)
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, client.netUser));
                return;
            }

            if (targetData == null)
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, target.netUser));
                return;
            }

            if (Configs.Config.ChatQuery.ContainsKey(target.userID))
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.PlayerChatQueryNotAnswer, target.netUser));
                return;
            }

            command = "tp";
            var tpEvent = Events.Timer.Find(e => (e.Sender == sender || e.Target == sender) && e.Command == command);
            if (tpEvent != null && tpEvent.TimeLeft > 0)
            {
                Broadcast.Notice(sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportAlready, client.netUser)
                        .Replace("%TIME%", tpEvent.TimeLeft.ToString()));
                return;
            }

            if (Configs.Config._Settings.TeleportOutdoorsOnly)
            {
                playerPos = target.controllable.character.transform.position;
                foreach (var collider in Physics.OverlapSphere(playerPos, 1f, GameConstant.Layer.kMask_ServerExplosion))
                {
                    var main = IDBase.GetMain(collider);
                    if (main == null) continue;
                    var structure = main.GetComponent<StructureMaster>();
                    if (structure == null || structure.ownerID == sender.userID) continue;
                    var ownerData = Data.FindUser(structure.ownerID);
                    if (ownerData != null && ownerData.HasShared(sender.userID)) continue;
                    Broadcast.Notice(sender, "☢",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNoTeleport, sender,
                            target.userName));
                    return;
                }
            }

            if (Configs.Config._Settings.TeleportPayment > 0)
            {
                var userEconomy = Economy.Get(userData.SteamID);
                var paymentPrice = Configs.Config._Settings.TeleportPayment.ToString("N0") + Economy.EData.CurrencySign;
                if (userEconomy.balance < Configs.Config._Settings.TeleportPayment)
                {
                    Broadcast.Notice(sender, "☢",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNoEnoughCurrency, sender)
                            .Replace("%PRICE%", paymentPrice));
                    return;
                }
            }

            var teleportQuery = new UserQuery(targetData,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportQuery, client.netUser));
            teleportQuery.answer.Add(new UserAnswer("CONFIRM",
                () => Events.TimeEvent_TeleportTo(client.netUser, target.netUser, command,
                    Configs.Config._Settings.TeleportTimeWait)));
            teleportQuery.answer.Add(new UserAnswer("ACCEPT",
                () => Events.TimeEvent_TeleportTo(client.netUser, target.netUser, command,
                    Configs.Config._Settings.TeleportTimeWait)));
            teleportQuery.answer.Add(new UserAnswer("accept",
                () => Events.TimeEvent_TeleportTo(client.netUser, target.netUser, command,
                    Configs.Config._Settings.TeleportTimeWait)));
            teleportQuery.answer.Add(new UserAnswer("*",
                () => Broadcast.Message(target.netPlayer,
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportRefuse, client.netUser),
                    "")));
            teleportQuery.answer.Add(new UserAnswer("*",
                () => Broadcast.Message(client.netPlayer,
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportRefused, target.netUser),
                    "")));
            Configs.Config.ChatQuery.Add(target.userID, teleportQuery);

            Broadcast.Notice(target.netPlayer, "?", teleportQuery.Query);
            Broadcast.Message(target.netPlayer, teleportQuery.Query);
            Broadcast.Message(target.netPlayer,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportQueryHelp, client.netUser));
        }
    }
}

public class Home : ICommand
{
    public string CmdName => "home";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData = null)
    {
        var GoTo = -1;
        List<Vector3> Spawns;
        Vector3 Position;
        // Administrators moving in to players home spawns (sleeping bag or bed) //
        if (Args != null && Args.Length > 0 && Sender != null && Sender.admin)
        {
            var targData = Data.FindUser(Args[0]);
            if (targData == null)
            {
                Broadcast.Notice(Sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, Args[0]));
                return;
            }

            // Get player position and list of player home spawns //
            Position = Sender.playerClient.lastKnownPosition;
            Spawns = Helper.GetPlayerSpawns(targData.SteamID, false);
            if (Spawns.Count == 0)
            {
                Broadcast.Notice(Sender, "✘", "Player \"" + targData.UserName + "\" not have a camp.");
                return;
            }

            // Get home spawn number for player from command arguments //
            if (Args.Length > 1 && int.TryParse(Args[1], out GoTo))
            {
                GoTo--;
                if (GoTo < 0) GoTo = 0;
                else if (GoTo >= Spawns.Count) GoTo = Spawns.Count - 1;
                // OR get near home spawn of player //
            }
            else
            {
                for (var i = 0; i < Spawns.Count; i++)
                    if (Vector3.Distance(Position, Spawns[i]) < 3.0f)
                        GoTo = ++i;
                if (GoTo < 0) GoTo = 0;
                else if (GoTo >= Spawns.Count) GoTo = 0;
            }

            Broadcast.Notice(Sender, "☢",
                "You moved on \"" + targData.UserName + "\" home spawn " + (GoTo + 1) + " of " + Spawns.Count);
            Helper.TeleportTo(Sender, Spawns[GoTo]);
            return;
        }

        // Get player position and list of player home spawns //
        Position = Sender.playerClient.lastKnownPosition;
        Spawns = Helper.GetPlayerSpawns(Sender.playerClient);
        if (Spawns.Count == 0)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandHomeNoCamp, "", Sender));
            return;
        }

        // Get near home spawn by player position and when near then gets next spawn //
        for (var i = 0; i < Spawns.Count; i++)
            if (Vector3.Distance(Position, Spawns[i]) < 3.0f)
                GoTo = i++;
        // Output list of user home spawns //
        if (Args != null && Args.Length > 0 && Args[0].Equals("LIST", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var S in Configs.Config.GetMessages(Messages.RuMessages.RuMessage.CommandHomeList, Sender))
                if (S.Contains("%HOME.NUM%") || S.Contains("%HOME.POSITION%"))
                    for (var i = 0; i < Spawns.Count; i++)
                        Broadcast.Message(Sender,
                            S.Replace("%HOME.NUM%", (i + 1).ToString())
                                .Replace("%HOME.POSITION%", Spawns[i].AsString()));
                else
                    Broadcast.Message(Sender,
                        Helper.ReplaceVariables(Sender, S).Replace("%HOME.COUNT%", Spawns.Count.ToString()));
            return;
        }

        if (Configs.Config._Settings.HomeOutdoorsOnly)
        {
            var playerPos = Sender.playerClient.controllable.character.transform.position;
            foreach (var collider in Physics.OverlapSphere(playerPos, 1f, GameConstant.Layer.kMask_ServerExplosion))
            {
                var Main = IDBase.GetMain(collider);
                if (Main == null) continue;
                var Structure = Main.GetComponent<StructureMaster>();
                if (Structure == null || Structure.ownerID == Sender.userID) continue;
                var OwnerData = Data.FindUser(Structure.ownerID);
                if (OwnerData != null && OwnerData.HasShared(Sender.userID)) continue;
                Broadcast.Notice(Sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeNotHere, Sender));
                return;
            }
        }

        // Try find countdown of user for home command
        var Countdown = Data.CountdownList(userData.SteamID).Find(F => F.Command == Command);
        // Check exists user countdown for home command
        if (Countdown != null)
        {
            if (!Countdown.Expired)
            {
                var Time = TimeSpan.FromSeconds(Countdown.TimeLeft);
                if (Time.TotalHours >= 1)
                    Broadcast.Notice(Sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeCountdown, Sender)
                            .Replace("%TIME%",
                                $"{Time.Hours:F0}:{Time.Minutes:D2}:{Time.Seconds:D2}"));
                else if (Time.TotalMinutes >= 1)
                    Broadcast.Notice(Sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeCountdown, Sender)
                            .Replace("%TIME%", $"{Time.Minutes}:{Time.Seconds:D2}"));
                else
                    Broadcast.Notice(Sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeCountdown, Sender)
                            .Replace("%TIME%", $"{Time.Seconds}"));
                return;
            }

            Data.CountdownRemove(userData.SteamID, Countdown);
        }

        // Try find event timer of user for home command //
        var HomeEvent = Events.Timer.Find(E => E.Sender == Sender && E.Command == Command);
        // Check exists event timer of player for home command //
        if (HomeEvent != null && HomeEvent.TimeLeft > 0)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeWait, Sender)
                    .Replace("%TIME%", HomeEvent.TimeLeft.ToString()));
            return;
        }

        // Remove experied event timer for player //
        if (HomeEvent != null)
        {
            HomeEvent.Dispose();
            Events.Timer.Remove(HomeEvent);
        }

        // Get home spawn number for player from command arguments //
        if (Args != null && Args.Length > 0 && int.TryParse(Args[0], out GoTo)) GoTo--;
        if (GoTo < 0) GoTo = 0;
        else if (GoTo >= Spawns.Count) GoTo = Spawns.Count - 1;
        // Payment for use command //
        if (Configs.Config._Settings.HomePayment > 0)
        {
            var userEconomy = Economy.Get(userData.SteamID);
            var PaymentPrice = Configs.Config._Settings.HomePayment.ToString("N0") + Economy.EData.CurrencySign;
            if (userEconomy.balance < Configs.Config._Settings.HomePayment)
            {
                Broadcast.Notice(Sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeNoEnoughCurrency, Sender)
                        .Replace("%PRICE%", PaymentPrice));
                return;
            }
        }

        // Teleport player to selected home spawn //
        HomeEvent = Events.TimeEvent_HomeWarp(Sender, Command, Configs.Config._Settings.HomeTimeWait, Spawns[GoTo]);
        if (HomeEvent != null && HomeEvent.TimeLeft > 0)
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandHomeStart, "", Sender)
                    .Replace("%TIME%", HomeEvent.TimeLeft.ToString()));
    }
}

public class Destroy : ICommand
{
    public string CmdName => "destroy";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData = null)
    {
        if (Sender.admin)
        {
            var count = 0;
            User targData;
            if (Args is { Length: > 0 })
            {
                targData = Data.FindUser(Args[0]);
                if (targData == null)
                {
                    Broadcast.Notice(Sender, "✘",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, Args[0]));
                    return;
                }

                count += StructureMaster.AllStructures.Where(master => master.ownerID == targData.SteamID)
                    .Sum(Helper.DestroyStructure);
                Broadcast.Notice(Sender, "✔",
                    "You destroy " + count + " objects owned by \"" + targData.UserName + "\"");
                Debug.Log("User [" + userData.UserName + ":" + userData.SteamID + "] has destroy " + count +
                          " objects owned by [" + targData.UserName + ":" + targData.SteamID + "].");
            }
            else
            {
                IDBase meshBatch = null;
                var lookAt = Sender.playerClient.controllable.character.eyesRay;
                if (Physics.Raycast(lookAt, out var hit, 1000f, -1)) meshBatch = hit.collider.GetComponent<IDBase>();
                if (meshBatch == null)
                {
                    Broadcast.Notice(Sender, "✘", "You don't see anything for destroy.", 3f);
                    return;
                }

                var master = meshBatch.idMain as StructureMaster;
                if (master == null)
                {
                    Broadcast.Notice(Sender, "✘", "There are nothing for destroy.", 3f);
                }
                else
                {
                    targData = Data.FindUser(master.ownerID);
                    count = Helper.DestroyStructure(master);
                    Broadcast.Notice(Sender, "✔",
                        "You destroy " + count + " objects owned by \"" + (targData != null ? targData.UserName : "-") +
                        "\"");
                    Debug.Log("User [" + userData.UserName + ":" + userData.SteamID + "] has destroy " + count +
                              " objects at " + master.transform.position + " owned by [" +
                              (targData != null ? targData.UserName + ":" + targData.SteamID : "-:-") + "].");
                }
            }

            return;
        }

        if (Configs.Config.DestoryOwnership.ContainsKey(Sender.userID))
        {
            Configs.Config.DestoryOwnership.Remove(Sender.userID);
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandDestroyDisabled, Sender));
        }
        else
        {
            Configs.Config.DestoryOwnership.Add(Sender.userID,
                DateTime.Now.AddSeconds(Configs.Config._Settings.OwnerDestroyAutoDisable));
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandDestroyEnabled, Sender));
        }
    }
}

public class UnShare : ICommand
{
    public string CmdName => "unshare";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser sender, string command, string[] args, User userData = null)
    {
        NetUser target;
        if (args == null || args.Length == 0 || (sender == null && args.Length < 2))
        {
            if (sender == null) userData = Data.FindUser(args[0]);
            foreach (var userID in userData.SharedList)
            {
                target = NetUser.FindByUserID(userID);
                if (target != null)
                    Broadcast.Notice(target, "☢",
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareClient, sender));
            }

            userData.SharedList.Clear();
            if (sender == null) Broadcast.Notice(sender, "☢", userData.UserName + "'s ownership unshared for all.");
            else
                Broadcast.Notice(sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareClean, sender));
            return;
        }

        var targData = Data.FindUser(args[args.Length - 1]);
        if ((sender == null || sender.admin) && args.Length > 1) userData = Data.FindUser(args[0]);

        if (userData == null)
        {
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender, args[0]));
        }
        else if (targData == null)
        {
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender,
                    args[args.Length - 1]));
        }
        else if (targData.SteamID == userData.SteamID)
        {
            Broadcast.Notice(sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareSelf, sender));
        }
        else if (!userData.SharedList.Contains(targData.SteamID))
        {
            Broadcast.Notice(sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareAlready, sender, args[0]));
        }
        else if (sender != null && sender.userID == userData.SteamID)
        {
            userData.SharedList.Remove(targData.SteamID);
            target = NetUser.FindByUserID(targData.SteamID);
            if (target != null)
                Broadcast.Notice(target, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareClient, sender));
            Broadcast.Notice(sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareOwner, sender,
                    targData.UserName));
        }
        else
        {
            userData.SharedList.Remove(targData.SteamID);
            Broadcast.Notice(sender, "☢", userData.UserName + "'s ownership is unshared for " + targData.UserName);
            sender = NetUser.FindByUserID(userData.SteamID);
            target = NetUser.FindByUserID(targData.SteamID);
            if (sender != null)
                Broadcast.Notice(sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareOwner, sender));
            if (target != null)
                Broadcast.Notice(target, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandUnshareClient, sender));
        }
    }
}

public class Share : ICommand
{
    public string CmdName => "share";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData = null)
    {
        if (Args == null || Args.Length == 0 || (Sender == null && Args.Length < 2))
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        var targData = Data.FindUser(Args[Args.Length - 1]);
        NetUser target = null;
        if ((Sender == null || Sender.admin) && Args.Length > 1) userData = Data.FindUser(Args[0]);

        if (userData == null)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, Args[0]));
        }
        else if (targData == null)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender,
                    Args[Args.Length - 1]));
        }
        else if (targData.SteamID == userData.SteamID)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareSelf, Sender));
        }
        else if (userData.SharedList.Contains(targData.SteamID))
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareAlready, Sender, Args[0]));
        }
        else if (Sender != null && Sender.userID == userData.SteamID)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareOwner, Sender, targData.UserName));
            userData.SharedList.Add(targData.SteamID);
            target = NetUser.FindByUserID(targData.SteamID);
            if (target != null)
                Broadcast.Notice(target.networkPlayer, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareClient, Sender));
        }
        else
        {
            userData.SharedList.Add(targData.SteamID);
            Broadcast.Notice(Sender, "☢", userData.UserName + "'s ownership now is shared for " + targData.UserName);
            Sender = NetUser.FindByUserID(userData.SteamID);
            target = NetUser.FindByUserID(targData.SteamID);
            if (Sender != null)
                Broadcast.Notice(Sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareOwner, Sender));
            if (target != null)
                Broadcast.Notice(target, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandShareClient, Sender));
        }
    }
}

public class Reply : ICommand
{
    public string CmdName => "r";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData = null)
    {
        if (Args == null || Args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        if (!Configs.Config.Reply.ContainsKey(Sender.userID))
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandReplyNobody, Sender));
            return;
        }

        var Client = Configs.Config.Reply[Sender.userID];
        if (Client == null)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender,
                    Client.displayName));
            return;
        }

        if (Client == Sender)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMSelf, Sender));
            return;
        }

        var MuteCD = Data.CountdownGet(Sender.userID, "mute");
        if (MuteCD != null)
        {
            if (!MuteCD.Expired)
            {
                var Time = TimeSpan.FromSeconds(MuteCD.TimeLeft);
                var MessageMutedTime = MuteCD.Expires
                    ? $"{Time.Hours}:{Time.Minutes:D2}:{Time.Seconds:D2}"
                    : "-:-:-";
                Broadcast.Notice(Sender, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.PlayerMuted, Sender)
                        .Replace("%TIME%", MessageMutedTime));
                return;
            }

            Data.CountdownRemove(Sender.userID, MuteCD);
        }

        Broadcast.ChatPm(Sender, Client, string.Join(" ", Args));
        if (!Configs.Config.Reply.ContainsKey(Client.userID)) Configs.Config.Reply.Add(Client.userID, Sender);
        else Configs.Config.Reply[Client.userID] = Sender;
    }
}

public class Clan : ICommand
{
    public string CmdName => "clan";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser sender, string cmd, string[] args, User userData)
    {
        User memberData = null;
        User clanLeader = null;
        ClanData clanData = null;
        var nextLevel = Clans.Find(userData.Clan) != null
            ? Clans.ClanCfg.Levels.Find(f => f.RequireLevel == Clans.Find(userData.Clan).Level.Id)
            : null;

        if (args == null || args.Length == 0)
        {
            if (Clans.Find(userData.Clan) == null)
            {
                Broadcast.Notice(sender, "✘",
                    Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNotInClan, null, sender));
            }
            else
            {
                var clanInfo = Configs.Config.GetMessagesClan(Messages.RuMessages.RuMessage.CommandClanInfo,
                    Clans.Find(userData.Clan), sender);
                foreach (var info in clanInfo)
                    if (!info.Contains("%CLAN."))
                        Broadcast.SendMessage(sender, info);
            }

            return;
        }

        var Switch = args[0].ToUpper();
        if (sender == null || sender.admin)
        {
            if (Switch.Equals("LIST"))
            {
                var N = 0;
                Broadcast.Message(sender, "Total Clans: " + Clans.Count);
                foreach (var clan in Clans.Database.Values)
                    Broadcast.Message(sender,
                        ++N + ". " + clan.ID.ToHex() + ": " + clan.Name + " <" + clan.Abbr + "> - Lvl: " +
                        clan.Level.Id);
                return;
            }

            if (Switch.Equals("INFO"))
            {
                if (args.Length < 2)
                {
                    Broadcast.Notice(sender, "✘", "You must enter clan name for get information.");
                    return;
                }

                if ((clanData = Clans.Find(args[1])) == null)
                {
                    Broadcast.Notice(sender, "✔", "Clan with name \"" + args[1] + "\" not exists.");
                    return;
                }

                var infoClan = Configs.Config.GetMessagesClan(Messages.RuMessages.RuMessage.CommandClanInfo, clanData);
                foreach (var str in infoClan)
                    if (!str.Contains("%CLAN."))
                        Broadcast.MessageClan(sender, str);

                var infoAdmin =
                    Configs.Config.GetMessagesClan(Messages.RuMessages.RuMessage.CommandClanInfoAdmin, clanData);
                foreach (var str in infoAdmin)
                    if (str.Contains("%CLAN.MEMBERS_LIST%"))
                    {
                        var outMsg = str.Replace("%CLAN.MEMBERS_LIST%", "");
                        foreach (var member in clanData.Members.Keys)
                        {
                            outMsg += member.UserName + ", ";
                            if (outMsg.Length > 80)
                            {
                                Broadcast.MessageClan(sender, clanData, outMsg.Substring(0, outMsg.Length - 2));
                                outMsg = "";
                            }
                        }

                        if (outMsg.Length > 0)
                            Broadcast.MessageClan(sender, clanData, outMsg.Substring(0, outMsg.Length - 2));
                    }
                    else if (!str.Contains("%CLAN."))
                    {
                        Broadcast.MessageClan(sender, str);
                    }

                return;
            }

            if (Switch.Equals("EDIT"))
            {
                if (args.Length < 2)
                {
                    Broadcast.Notice(sender, "✘", "You must enter clan name or abbr to edit properties.");
                    return;
                }

                if ((clanData = Clans.Find(args[1])) == null)
                {
                    Broadcast.Notice(sender, "✔", "Clan with name \"" + args[1] + "\" not exists.");
                    return;
                }

                if (args.Length < 3)
                {
                    Broadcast.Notice(sender, "✘", "What properties do you want edit for this clan?");
                    return;
                }

                if (args.Length < 4)
                {
                    Broadcast.Notice(sender, "✘", "You must enter NEW value for this properties");
                    return;
                }

                var editCommand = args[2].ToUpper();
                switch (editCommand)
                {
                    case "NAME":
                        Broadcast.Notice(sender, "✔", "You change name for clan " + clanData.Name);
                        clanData.Name = args[3];
                        break;
                    case "ABBR":
                    case "ABBREVIATION":
                        clanData.Abbr = args[3];
                        Broadcast.Notice(sender, "✔", "You change abbreviation for clan " + clanData.Name);
                        break;
                    case "MOTD":
                    case "MESSAGEOFTHEDAY":
                        clanData.MOTD = args[3];
                        Broadcast.Notice(sender, "✔", "You change MOTD for clan " + clanData.Name);
                        break;
                    case "BALANCE":
                    case "MONEY":
                    {
                        ulong Money = 0;
                        if (!ulong.TryParse(args[3], out Money))
                        {
                            Broadcast.Notice(sender, "✘", "WRONG: Requires only digits!");
                            return;
                        }

                        clanData.Balance = Money;
                        Broadcast.Notice(sender, "✔",
                            "You change balance to " + Money.ToString("N0") + Economy.EData.CurrencySign +
                            " for clan " +
                            clanData.Name);
                        break;
                    }
                    case "EXP":
                    case "EXPERIENCE":
                    {
                        ulong Exp = 0;
                        if (!ulong.TryParse(args[3], out Exp))
                        {
                            Broadcast.Notice(sender, "✘", "WRONG: Requires only digits!");
                            return;
                        }

                        clanData.Experience = Exp;
                        Broadcast.Notice(sender, "✔",
                            "You change experience to " + Exp.ToString("N0") + " for clan " + clanData.Name);
                        break;
                    }
                    case "TAX":
                    {
                        uint Tax = 0;
                        if (!uint.TryParse(args[3], out Tax))
                        {
                            Broadcast.Notice(sender, "✘", "WRONG: Requires only digits!");
                            return;
                        }

                        clanData.Tax = Tax;
                        Broadcast.Notice(sender, "✔", "You change tax to " + Tax + "% for clan " + clanData.Name);
                        break;
                    }
                    case "LVL":
                    case "LEVEL":
                    {
                        var Lvl = 0;
                        if (!int.TryParse(args[3], out Lvl))
                        {
                            Broadcast.Notice(sender, "✘", "WRONG: Requires only digits!");
                            return;
                        }

                        var Level = Clans.ClanCfg.Levels.Find(F => F.Id == Lvl);
                        if (Level != null) clanData.SetLevel(Level);
                        Broadcast.Notice(sender, "✔", "You change level to " + Level.Id + " for clan " + clanData.Name);
                        break;
                    }
                    case "LEADER":
                    case "CLANLEADER":
                    {
                        if ((clanLeader = Data.FindUser(args[3])) == null)
                        {
                            Broadcast.Notice(sender, "✘",
                                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null,
                                    args[3]));
                            return;
                        }

                        clanData.LeaderID = clanLeader.SteamID;
                        clanData.Members[clanLeader] =
                            ClanMemberFlags.Invite | ClanMemberFlags.Dismiss | ClanMemberFlags.Management;
                        Broadcast.Notice(sender, "✔", "You change leader for clan " + clanData.Name);
                        break;
                    }
                }

                return;
            }

            if (Switch.Equals("REMOVE") || Switch.Equals("DELETE"))
            {
                if (args.Length < 2)
                {
                    Broadcast.Notice(sender, "✘", "You must enter clan name for remove.");
                    return;
                }

                if ((clanData = Clans.Find(args[1])) == null)
                {
                    Broadcast.Notice(sender, "✔", "Clan with name \"" + args[1] + "\" not exists.");
                    return;
                }

                foreach (var member in clanData.Members.Keys)
                {
                    member.Clan = null;
                    var netMember = NetUser.FindByUserID(member.SteamID);
                    if (netMember != null)
                        Broadcast.Notice(netMember, "☢",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDisbanded, clanData,
                                sender, member));
                }

                Broadcast.Notice(sender, "✘", "You remove \"" + clanData.Name + "\" a clan");
                Clans.Remove(clanData);
                return;
            }
        }

        if (Switch.Equals("CREATE") && Clans.Find(userData.Clan) != null)
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAlreadyInClan, null, sender));
        else if (!Switch.Equals("CREATE") && Clans.Find(userData.Clan) == null)
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNotInClan, null, sender));
        else
            switch (Switch)
            {
                case "CREATE":
                {
                    if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanCreateReqEnterName,
                                null, sender));
                    }
                    else if (!Regex.Match(args[1], @"([^()<>{}\[\]\*]+)", RegexOptions.IgnoreCase).Success)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanCreateForbiddenSyntax, null, sender));
                    }
                    else if (args[1].Length < 3)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanCreateTooShortLength,
                                null, sender));
                    }
                    else if (args[1].Length > 32)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanCreateTooLongLength,
                                null, sender));
                    }
                    else if (Clans.Find(args[1]) != null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanCreateNameAlredyInUse, null, sender));
                    }
                    else
                    {
                        if (Clans.CreateCost > 0)
                        {
                            if (Clans.CreateCost > Economy.Get(userData.SteamID).balance)
                            {
                                Broadcast.Notice(sender, "✘",
                                    Configs.Config.GetMessageClan(
                                        Messages.RuMessages.RuMessage.CommandClanCreateNotEnoughCurrency, null,
                                        sender));
                                return;
                            }

                            Economy.Get(userData.SteamID).balance -= Clans.CreateCost;
                            Economy.Balance(sender, userData, "balance", new string[0]);
                        }

                        if (sender != null) userData.Clan = Clans.Create(args[1], sender.userID).Name;
                        if (Clans.Find(userData.Clan) != null)
                        {
                            Clans.Find(userData.Clan).SetLevel(Clans.ClanCfg.Levels[Clans.DefaultLevel]);
                            Broadcast.Notice(sender, "✔",
                                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanCreateSuccess,
                                    Clans.Find(userData.Clan), sender, userData));
                            Clans.MemberJoin(Clans.Find(userData.Clan), userData);
                        }
                    }
                }
                    break;

                case "DISBAND":
                {
                    if (Clans.Find(userData.Clan) != null && Clans.Find(userData.Clan).LeaderID != userData.SteamID)
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else
                    {
                        if (Clans.Find(userData.Clan) != null)
                        {
                            foreach (var member in Clans.Find(userData.Clan).Members.Keys)
                            {
                                if (userData == member) continue;
                                var netMember = NetUser.FindByUserID(member.SteamID);
                                if (netMember != null)
                                    Broadcast.Notice(netMember, "☢",
                                        Configs.Config.GetMessageClan(
                                            Messages.RuMessages.RuMessage.CommandClanDisbanded, null, sender));
                                member.Clan = null;
                            }

                            Clans.Remove(Clans.Find(userData.Clan));
                        }

                        userData.Clan = null;
                        Broadcast.Notice(sender, "☢",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDisbanded, null,
                                sender));
                    }
                }
                    break;

                case "UP":
                case "RISE":
                case "GROW":
                case "LEVEL":
                {
                    if (Clans.Find(userData.Clan) != null && !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (nextLevel == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanLevelUpReachedMax,
                                null, sender));
                    }
                    else if (Clans.Find(userData.Clan).Balance < nextLevel.RequireCurrency)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanLevelUpNotEnoughCurrency, null, sender));
                    }
                    else if (Clans.Find(userData.Clan).Experience < nextLevel.RequireExperience)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanLevelUpNotEnoughExperience, null, sender));
                    }
                    else
                    {
                        Clans.Find(userData.Clan).SetLevel(nextLevel);
                        Clans.Find(userData.Clan).Balance -= nextLevel.RequireCurrency;
                        Clans.Find(userData.Clan).Experience -= nextLevel.RequireExperience;
                        var successMsg = Configs.Config.GetMessagesClan(
                            Messages.RuMessages.RuMessage.CommandClanLevelUpSuccess, Clans.Find(userData.Clan), sender);
                        foreach (var str in successMsg)
                            if (!str.Contains("%CLAN."))
                                Broadcast.MessageClan(Clans.Find(userData.Clan), str);
                    }
                }
                    break;

                case "DEPOSIT":
                {
                    var userEconomy = Economy.Get(userData.SteamID);
                    if (userEconomy == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyNotAvailable, sender));
                    }
                    else if (args.Length < 2 || !ulong.TryParse(args[1], out var Amount) || Amount == 0)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDepositNoAmount,
                                null, sender));
                    }
                    else
                    {
                        var depositAmount = Amount.ToString("N0") + Economy.EData.CurrencySign;
                        if (userEconomy.balance < Amount)
                        {
                            Broadcast.Notice(sender, "✘",
                                Configs.Config
                                    .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDepositNoEnoughAmount,
                                        Clans.Find(userData.Clan), sender).Replace("%DEPOSIT_AMOUNT%", depositAmount));
                        }
                        else
                        {
                            userEconomy.balance -= Amount;
                            if (Clans.Find(userData.Clan) != null)
                            {
                                Clans.Find(userData.Clan).Balance += Amount;
                                Broadcast.Notice(sender, "✘",
                                    Configs.Config
                                        .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDepositSuccess,
                                            Clans.Find(userData.Clan), sender).Replace("%DEPOSIT_AMOUNT%", depositAmount));
                            }

                            Economy.Balance(sender, userData, "balance", new string[0]);
                        }
                    }
                }
                    break;

                case "WITHDRAW":
                {
                    var userEconomy = Economy.Get(userData.SteamID);
                    if (Clans.Find(userData.Clan) != null && !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (userEconomy == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyNotAvailable));
                    }
                    else if (args.Length < 2 || !ulong.TryParse(args[1], out var amount) || amount == 0)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWithdrawNoAmount,
                                null, sender));
                    }
                    else
                    {
                        var withdrawAmount = amount.ToString("N0") + Economy.EData.CurrencySign;
                        if (Clans.Find(userData.Clan) != null && Clans.Find(userData.Clan).Balance < amount)
                        {
                            Broadcast.Notice(sender, "✘",
                                Configs.Config
                                    .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWithdrawNoEnoughAmount,
                                        Clans.Find(userData.Clan), sender).Replace("%WITHDRAW_AMOUNT%", withdrawAmount));
                        }
                        else
                        {
                            if (Clans.Find(userData.Clan) != null)
                            {
                                Clans.Find(userData.Clan).Balance -= amount;
                                userEconomy.balance += amount;
                                Broadcast.Notice(sender, "✘",
                                    Configs.Config
                                        .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWithdrawSuccess,
                                            Clans.Find(userData.Clan), sender).Replace("%WITHDRAW_AMOUNT%", withdrawAmount));
                            }

                            Economy.Balance(sender, userData, "balance", new string[0]);
                        }
                    }
                }
                    break;

                case "LEAVE":
                {
                    if (Clans.Find(userData.Clan) != null && Clans.Find(userData.Clan).LeaderID == userData.SteamID)
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanLeaveDisbandBefore,
                                null, sender));
                    }
                    else
                    {
                        Broadcast.Notice(sender, "☢",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanLeaveSuccess, null,
                                sender));
                        Broadcast.MessageClan(Clans.Find(userData.Clan),
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanLeaveMemberLeaved,
                                Clans.Find(userData.Clan), NetUser.FindByUserID(userData.SteamID)));
                        Clans.MemberLeave(Clans.Find(userData.Clan), userData);
                    }
                }
                    break;

                case "MEMBERS":
                {
                    var members = Configs.Config.GetMessagesClan(Messages.RuMessages.RuMessage.CommandClanMembers,
                        Clans.Find(userData.Clan), sender);
                    foreach (var str in members)
                        if (str.Contains("%CLAN.MEMBERS_LIST%"))
                        {
                            var outMsg = str.Replace("%CLAN.MEMBERS_LIST%", "");
                            if (Clans.Find(userData.Clan) != null)
                            {
                                foreach (var member in Clans.Find(userData.Clan).Members.Keys)
                                {
                                    outMsg += member.UserName + ", ";
                                    if (outMsg.Length > 80)
                                    {
                                        Broadcast.MessageClan(sender, Clans.Find(userData.Clan),
                                            outMsg.Substring(0, outMsg.Length - 2));
                                        outMsg = "";
                                    }
                                }

                                if (outMsg.Length > 0)
                                    Broadcast.MessageClan(sender, Clans.Find(userData.Clan),
                                        outMsg.Substring(0, outMsg.Length - 2));
                            }
                        }
                        else if (!str.Contains("%CLAN."))
                        {
                            Broadcast.MessageClan(sender, str);
                        }
                }
                    break;

                case "ONLINE":
                {
                    var online = Configs.Config.GetMessagesClan(Messages.RuMessages.RuMessage.CommandClanOnline,
                        Clans.Find(userData.Clan), sender);
                    foreach (var str in online)
                        if (str.Contains("%CLAN.ONLINE_LIST%"))
                        {
                            var outMsg = str.Replace("%CLAN.ONLINE_LIST%", "");
                            foreach (var Member in Clans.Find(userData.Clan).Members.Keys)
                            {
                                if (NetUser.FindByUserID(Member.SteamID) != null) outMsg += Member.UserName + ", ";
                                if (outMsg.Length > 80)
                                {
                                    Broadcast.MessageClan(sender, Clans.Find(userData.Clan),
                                        outMsg.Substring(0, outMsg.Length - 2));
                                    outMsg = "";
                                }
                            }

                            if (outMsg.Length > 0)
                                Broadcast.MessageClan(sender, Clans.Find(userData.Clan), outMsg.Substring(0, outMsg.Length - 2));
                        }
                        else if (!str.Contains("%CLAN."))
                        {
                            Broadcast.MessageClan(sender, str);
                        }
                }
                    break;

                case "INVITE":
                {
                    if (Clans.Find(userData.Clan) != null && !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Invite))
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanInviteNoValue, null,
                                sender));
                    }
                    else if (Clans.Find(userData.Clan).Members.Count >= Clans.Find(userData.Clan).Level.MaxMembers)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanInviteNoSlots, null,
                                sender));
                    }
                    else if ((memberData = Data.FindUser(args[1])) == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender,
                                args[1]));
                    }
                    else if (Clans.Find(memberData.Clan) != null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAlreadyInClan,
                                Clans.Find(userData.Clan), null, memberData));
                    }
                    else if (Configs.Config.ChatQuery.ContainsKey(memberData.SteamID))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanInviteAlreadyInvite,
                                Clans.Find(userData.Clan), null, memberData));
                    }
                    else
                    {
                        var targetUser = NetUser.FindByUserID(memberData.SteamID);
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanInviteInviteToJoin,
                                Clans.Find(userData.Clan), null, memberData));
                        var inviteQuery = new UserQuery(memberData,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanInviteJoinQuery,
                                Clans.Find(userData.Clan), sender));
                        inviteQuery.answer.Add(new UserAnswer("Y*", () => Clans.MemberJoin(Clans.Find(userData.Clan), memberData)));
                        inviteQuery.answer.Add(new UserAnswer("Y*",
                            () => Broadcast.MessageClan(Clans.Find(userData.Clan),
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanInviteJoinAnswerY, Clans.Find(userData.Clan), null,
                                    memberData))));
                        inviteQuery.answer.Add(new UserAnswer("*",
                            () => Broadcast.MessageClan(Clans.Find(userData.Clan),
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanInviteJoinAnswerN, Clans.Find(userData.Clan), null,
                                    memberData))));
                        Configs.Config.ChatQuery.Add(memberData.SteamID, inviteQuery);
                        if (targetUser != null) Broadcast.Notice(targetUser, "?", inviteQuery.Query);
                    }
                }
                    break;

                case "DISMISS":
                {
                    if (Clans.Find(userData.Clan) != null && !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Dismiss))
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDismissNoValue, null,
                                sender));
                    }
                    else if ((memberData = Data.FindUser(args[1])) == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender,
                                args[1]));
                    }
                    else if (Clans.Find(userData.Clan) != Clans.Find(memberData.Clan))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDismissNotInClan,
                                Clans.Find(userData.Clan), sender, memberData));
                    }
                    else if (Clans.Find(memberData.Clan).LeaderID == memberData.SteamID)
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDismissIsLeader,
                                Clans.Find(userData.Clan), sender, memberData));
                    }
                    else
                    {
                        var memberUser = NetUser.FindByUserID(memberData.SteamID);
                        if (memberUser != null)
                            Broadcast.Notice(memberUser, "☢",
                                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDismissIsLeader,
                                    Clans.Find(userData.Clan), memberUser));
                        Broadcast.Notice(sender, "☢",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDismissToDismiss,
                                Clans.Find(userData.Clan), sender, memberData));
                        Clans.MemberLeave(Clans.Find(userData.Clan), memberData);
                    }
                }
                    break;

                case "PRIV":
                {
                    if (args.Length < 2)
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config
                                .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanPrivileges, Clans.Find(userData.Clan),
                                    sender, memberData).Replace("%MEMBER_PRIV%",
                                    Clans.Find(userData.Clan).Members[userData].ToString()));
                    }
                    else
                    {
                        if ((memberData = Data.FindUser(args[1])) == null)
                        {
                            Broadcast.Notice(sender, "✘",
                                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender,
                                    args[1]));
                        }
                        else if (Clans.Find(userData.Clan) != Clans.Find(memberData.Clan))
                        {
                            Broadcast.Notice(sender, "✘",
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanPrivilegesNotInClan, Clans.Find(userData.Clan), sender,
                                    memberData));
                        }
                        else if (args.Length < 3)
                        {
                            Broadcast.MessageClan(sender,
                                Configs.Config
                                    .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanPrivilegesMember,
                                        Clans.Find(userData.Clan), sender, memberData).Replace("%MEMBER_PRIV%",
                                        Clans.Find(userData.Clan).Members[memberData].ToString()));
                        }
                        else if (Clans.Find(userData.Clan) != null &&
                                 !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                        {
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions,
                                    null, sender));
                        }
                        else if (Clans.Find(memberData.Clan).LeaderID == memberData.SteamID)
                        {
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanPrivilegesNoCanChange, null, sender));
                        }
                        else if (args.Length > 2)
                        {
                            args[2] = args[2].ToUpper();
                            switch (args[2])
                            {
                                case "NONE":
                                case "CLEAR":
                                    if (Clans.Find(userData.Clan) != null) Clans.Find(userData.Clan).Members[memberData] = 0;
                                    break;
                                case "FULL":
                                case "ALL":
                                    if (Clans.Find(userData.Clan) != null)
                                        Clans.Find(userData.Clan).Members[memberData] =
                                            ClanMemberFlags.Invite | ClanMemberFlags.Dismiss |
                                            ClanMemberFlags.Management;
                                    break;
                                case "INVITE":
                                    if (Clans.Find(userData.Clan) != null)
                                        Clans.Find(userData.Clan).Members[memberData] ^= ClanMemberFlags.Invite;
                                    break;
                                case "DISMISS":
                                    if (Clans.Find(userData.Clan) != null)
                                        Clans.Find(userData.Clan).Members[memberData] ^= ClanMemberFlags.Dismiss;
                                    break;
                                case "MANAGEMENT":
                                    if (Clans.Find(userData.Clan) != null)
                                        Clans.Find(userData.Clan).Members[memberData] ^= ClanMemberFlags.Management;
                                    break;
                                default:
                                    Broadcast.Notice(sender, "✘", "Unknown name of privilege.");
                                    return;
                            }

                            Broadcast.MessageClan(sender,
                                Configs.Config
                                    .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanPrivilegesMember,
                                        Clans.Find(userData.Clan), sender, memberData).Replace("%MEMBER_PRIV%",
                                        Clans.Find(userData.Clan).Members[memberData].ToString()));
                        }
                    }
                }
                    break;

                case "DETAILS":
                {
                    var stateDetails = Clans.Find(userData.Clan) != null &&
                                       Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.ExpDetails);
                    if (args.Length > 1)
                    {
                        stateDetails = args[1].ToBool();
                        if (stateDetails)
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDetailsSetOn,
                                    Clans.Find(userData.Clan), sender, userData));
                        else
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDetailsSetOff,
                                    Clans.Find(userData.Clan), sender, userData));

                        if (Clans.Find(userData.Clan) != null)
                            Clans.Find(userData.Clan).Members[userData] = Clans.Find(userData.Clan).Members[userData]
                                .SetFlag(ClanMemberFlags.ExpDetails, stateDetails);
                    }
                    else if (stateDetails)
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDetailsEnabled,
                                Clans.Find(userData.Clan), sender, userData));
                    }
                    else
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanDetailsDisabled,
                                Clans.Find(userData.Clan), sender, userData));
                    }
                }
                    break;

                case "ABBR":
                {
                    if (!Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanAbbr))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrNoAvailable,
                                null, sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrNoValue, null,
                                sender));
                    }
                    else if (args[1].Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrTooShortLength,
                                null, sender));
                    }
                    else if (args[1].Length > 8)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrTooLongLength,
                                null, sender));
                    }
                    else if (!Regex.Match(args[1], @"([^()<>{}\[\]\*]+)", RegexOptions.IgnoreCase).Success)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrForbiddenSyntax,
                                null, sender));
                    }
                    else
                    {
                        Clans.Find(userData.Clan).Abbr = args[1];
                        Broadcast.MessageClan(Clans.Find(userData.Clan),
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanAbbrSuccess,
                                Clans.Find(userData.Clan), sender));
                    }
                }
                    break;

                case "TAX":
                {
                    var Tax = Clans.Find(userData.Clan).Tax;
                    if (!Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanTax))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTaxNoAvailable, null,
                                sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTaxNoValue, null,
                                sender));
                    }
                    else if (!uint.TryParse(args[1], out Tax))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTaxNoNumeric, null,
                                sender));
                    }
                    else if (Tax > 90)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTaxVeryHigh, null,
                                sender));
                    }
                    else
                    {
                        Clans.Find(userData.Clan).Tax = Tax;
                        Broadcast.MessageClan(Clans.Find(userData.Clan),
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTaxSuccess,
                                Clans.Find(userData.Clan), sender));
                    }
                }
                    break;

                case "TRANSFER":
                {
                    if (Clans.Find(userData.Clan).LeaderID != userData.SteamID)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferNoValue,
                                null, sender));
                    }
                    else if ((memberData = Data.FindUser(args[1])) == null)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, sender,
                                args[1]));
                    }
                    else if (Clans.Find(memberData.Clan) == null || Clans.Find(memberData.Clan) != Clans.Find(userData.Clan))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferNotInClan,
                                Clans.Find(userData.Clan), sender, memberData));
                    }
                    else
                    {
                        var targetUser = NetUser.FindByUserID(memberData.SteamID);
                        Broadcast.MessageClan(sender, clanData,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferQuery,
                                clanData, sender, memberData));
                        var transferQuery = new UserQuery(memberData,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanTransferQueryMember,
                                Clans.Find(userData.Clan)));
                        transferQuery.answer.Add(new UserAnswer("ACCEPT",
                            () => Clans.TransferAccept(Clans.Find(userData.Clan), memberData)));
                        transferQuery.answer.Add(new UserAnswer("*",
                            () => Clans.TransferDecline(Clans.Find(userData.Clan), memberData)));
                        Configs.Config.ChatQuery.Add(memberData.SteamID, transferQuery);
                        if (targetUser != null) Broadcast.Notice(targetUser, "?", transferQuery.Query);
                    }
                }
                    break;

                case "MOTD":
                {
                    if (!Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanMotd))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanMotdNoAvailable,
                                null, sender));
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanMotdNoValue, null,
                                sender));
                    }
                    else
                    {
                        Clans.Find(userData.Clan).MOTD = args[1];
                        Broadcast.MessageClan(Clans.Find(userData.Clan),
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanMotdSuccess,
                                Clans.Find(userData.Clan), sender));
                    }
                }
                    break;

                case "FRIENDLYFIRE":
                case "FFIRE":
                case "FF":
                {
                    if (args.Length < 2)
                    {
                        if (Clans.Find(userData.Clan).FriendlyFire)
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanFriendlyFireEnabled, Clans.Find(userData.Clan),
                                    sender));
                        else
                            Broadcast.MessageClan(sender,
                                Configs.Config.GetMessageClan(
                                    Messages.RuMessages.RuMessage.CommandClanFriendlyFireDisabled, Clans.Find(userData.Clan),
                                    sender));
                        return;
                    }

                    if (!Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                        return;
                    }

                    if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanFFire))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanFriendlyFireNoAvailable, null, sender));
                        return;
                    }

                    if (args[1].Equals("YES", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = true;
                    }
                    else if (args[1].Equals("ON", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = true;
                    }
                    else if (args[1].Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = true;
                    }
                    else if (args[1].Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = true;
                    }
                    else if (args[1].Equals("NO", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = false;
                    }
                    else if (args[1].Equals("OFF", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = false;
                    }
                    else if (args[1].Equals("N", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = false;
                    }
                    else if (args[1].Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        Clans.Find(userData.Clan).FriendlyFire = false;
                    }
                    else
                    {
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanFriendlyFireHelp,
                                null, sender));
                        return;
                    }

                    if (Clans.Find(userData.Clan).FriendlyFire)
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanFriendlyFireToEnable,
                                Clans.Find(userData.Clan), sender));
                    else
                        Broadcast.MessageClan(sender,
                            Configs.Config.GetMessageClan(
                                Messages.RuMessages.RuMessage.CommandClanFriendlyFireToDisable, Clans.Find(userData.Clan), sender));
                }
                    break;

                case "HOUSE":
                {
                    if (Clans.Find(userData.Clan) != null && !Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.Management))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanNoPermissions, null,
                                sender));
                    }
                    else if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanWarp))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanHouseNoAvailable,
                                null, sender));
                    }
                    else
                    {
                        var Position = sender.playerClient.controllable.character.transform.position;
                        /* Leader of a clan can a set clan house point ONLY in self ownership */
                        foreach (var collider in Physics.OverlapSphere(Position, 1f))
                        {
                            var idBase = collider.gameObject.GetComponent<IDBase>();
                            if (idBase == null) continue;
                            if (idBase.idMain is StructureMaster master &&
                                master.ownerID == userData.SteamID)
                            {
                                Clans.Find(userData.Clan).Location = Position;
                                var houseLocated = Configs.Config.GetMessagesClan(
                                    Messages.RuMessages.RuMessage.CommandClanHouseSuccess, Clans.Find(userData.Clan), sender);
                                foreach (var str in houseLocated)
                                    if (!str.Contains("%CLAN."))
                                        Broadcast.MessageClan(Clans.Find(userData.Clan), str);
                                return;
                            }
                        }

                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanHouseOnlyLeaderHouse,
                                Clans.Find(userData.Clan), sender));
                    }
                }
                    break;

                case "WARP":
                {
                    if (!Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanWarp))
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWarpNoAvailable,
                                null, sender));
                    }
                    else if (Clans.Find(userData.Clan).Location == Vector3.zero)
                    {
                        Broadcast.Notice(sender, "✘",
                            Configs.Config.GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWarpNoClanHouse,
                                Clans.Find(userData.Clan), sender));
                    }
                    else
                    {
                        if (Clans.WarpOutdoorsOnly)
                        {
                            var playerPos = sender.playerClient.controllable.character.transform.position;
                            foreach (var collider in Physics.OverlapSphere(playerPos, 1f,
                                         GameConstant.Layer.kMask_ServerExplosion))
                            {
                                var Main = IDBase.GetMain(collider);
                                if (Main == null) continue;
                                var Structure = Main.GetComponent<StructureMaster>();
                                if (Structure == null || Structure.ownerID == sender.userID) continue;
                                var OwnerData = Data.FindUser(Structure.ownerID);
                                if (OwnerData != null && OwnerData.SharedList.Contains(sender.userID)) continue;
                                Broadcast.Notice(sender, "☢",
                                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandClanWarpNotHere,
                                        sender));
                                return;
                            }
                        }

                        var Countdown = Data.CountdownList(userData.SteamID).Find(F => F.Command == cmd);
                        if (Countdown != null)
                        {
                            if (!Countdown.Expired)
                            {
                                var Time = TimeSpan.FromSeconds(Countdown.TimeLeft);
                                Broadcast.Notice(sender, "✘",
                                    Configs.Config
                                        .GetMessage(Messages.RuMessages.RuMessage.CommandClanWarpCountdown, sender)
                                        .Replace("%TIME%", $"{Time.Minutes}:{Time.Seconds:D2}"));
                                return;
                            }

                            Data.CountdownRemove(userData.SteamID, Countdown);
                        }

                        var warpEvent = Events.Timer.Find(E => E.Sender == sender && E.Command == cmd);
                        if (warpEvent != null && warpEvent.TimeLeft > 0)
                        {
                            Broadcast.Notice(sender, "☢",
                                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandClanWarpTimewait, sender)
                                    .Replace("%SECONDS%", warpEvent.TimeLeft.ToString()));
                            return;
                        }

                        if (Clans.Find(userData.Clan).Level.WarpTimeWait <= 0)
                        {
                            warpEvent = null;
                            Events.Teleport_ClanWarp(null, sender, cmd, Clans.Find(userData.Clan));
                        }
                        else
                        {
                            warpEvent = Events.TimeEvent_ClanWarp(sender, cmd, Clans.Find(userData.Clan).Level.WarpTimeWait,
                                Clans.Find(userData.Clan));
                            if (warpEvent != null && warpEvent.TimeLeft > 0)
                                Broadcast.Notice(sender, "☢",
                                    Configs.Config
                                        .GetMessageClan(Messages.RuMessages.RuMessage.CommandClanWarpPrepare,
                                            Clans.Find(userData.Clan), sender).Replace("%SECONDS%", warpEvent.TimeLeft.ToString()));
                        }
                    }
                }
                    break;

                default:
                    CommandHelper.SendCommandDescription(this, sender, userData);
                    break;
            }
    }
}

public class Config : ICommand
{
    public string CmdName => "config";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData)
    {
        if (args.Length > 0)
        {
            if (args[0] == "reload")
            {
                Configs.Commands.LoadData();
                Configs.Config.LoadData();
                Configs.Shop.LoadData();
                Configs.Kits.LoadData();
                Configs.Ranks.LoadData();
                Configs.Override.LoadData();
                Configs.Destroy.LoadData();
                
                Economy.LoadData();
                Clans.LoadData();
                Messages.LoadData();
                LoadOut.LoadData();
                Boot.LoadMods();
            }
            else
            {
                CommandHelper.SendCommandDescription(this, user, userData);
            }
        }
    }
}

public class Give : ICommand
{
    public string CmdName => "give";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        if (Args == null || Args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        var Target = Sender;
        var Slots = -1;
        var Quantity = 1;
        var itemName = Args[0];
        var ItemData = DatablockDictionary.GetByName(itemName);
        if (Args.Length >= 3 && !int.TryParse(Args[2], out Slots)) Slots = -1;
        if (Args.Length >= 2 && !int.TryParse(Args[1], out Quantity)) Quantity = 1;
        if (ItemData == null)
        {
            Target = Helper.GetNetUser(Args[0]);
            if (Target == null)
            {
                Broadcast.Notice(Sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, itemName));
                return;
            }

            if (Args.Length < 2)
            {
                CommandHelper.SendCommandDescription(this, Sender, userData);
                return;
            }

            itemName = Args[1];
            ItemData = DatablockDictionary.GetByName(itemName);
            if (Args.Length >= 4 && !int.TryParse(Args[3], out Slots)) Slots = -1;
            if (Args.Length >= 3 && !int.TryParse(Args[2], out Quantity)) Quantity = 1;
        }

        if (Target == null)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, itemName));
            return;
        }

        if (ItemData == null)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandItemNoFound, Sender, itemName));
            return;
        }

        Quantity = Helper.GiveItem(Target.playerClient, ItemData, Quantity, Slots);
        var ReceivedItem = "\"" + ItemData.name + "\"";
        if (Quantity > 1) ReceivedItem = Quantity + " " + ReceivedItem;
        if (Quantity == 0)
        {
            Broadcast.Notice(Sender, "✘", "Failed to give " + ReceivedItem + ", inventory is full.");
            return;
        }

        if (Sender != null && Sender != Target)
            Broadcast.Notice(Sender, "✔", "You give " + ReceivedItem + " into " + Target.displayName + " inventory.");
        Helper.Log(userData.UserName + " give " + ReceivedItem + " into " + Target.displayName + " inventory.");
        Broadcast.Notice(Target, "✔", "You received " + ReceivedItem + " into your inventory.");
    }
}

public class Admin : ICommand
{
    public string CmdName => "admin";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        Sender.admin = !Sender.admin;
        Broadcast.Notice(Sender.networkPlayer, "✔",
            "You have " + (Sender.admin ? "enabled" : "disabled") + " administrator rights.");
    }
}

public class Transfer : ICommand
{
    public string CmdName => "transfer";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        if (Args == null || Args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        var targData = Data.FindUser(Args[0]);
        if (targData == null || (targData.HasFlag(Data.UserFlags.Admin) && !userData.HasFlag(Data.UserFlags.Admin)))
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, Sender, Args[0]));
            return;
        }

        if (!Sender.admin && targData.SteamID == Sender.userID)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTransferSelf, Sender));
            return;
        }

        RaycastHit Hit;
        IDBase MeshBatch = null;
        string objectName = null;
        ulong objOwnerID = 0;
        var LookAt = Sender.playerClient.controllable.character.eyesRay;
        var Distance = Sender.admin ? 1000f : 10f;
        if (Physics.Raycast(LookAt, out Hit, Distance, -1)) MeshBatch = Hit.collider.GetComponent<IDBase>();
        if (MeshBatch == null)
        {
            Broadcast.Message(Sender,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTransferAway, Sender));
            return;
        }

        var Deploy = MeshBatch.idMain as DeployableObject;
        var Master = MeshBatch.idMain as StructureMaster;
        if (Deploy == null && Master == null)
        {
            Broadcast.Message(Sender,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTransferSeeNothing, Sender));
            return;
        }

        if (Deploy != null)
        {
            objOwnerID = Deploy.ownerID;
            objectName = Helper.NiceName(Deploy.name);
        }

        if (Master != null)
        {
            objOwnerID = Master.ownerID;
            objectName = Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandTransferBuilding, Sender);
        }

        if (objOwnerID == targData.SteamID)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config
                    .GetMessage(Messages.RuMessages.RuMessage.CommandTransferAlreadyOwned, Sender, targData.UserName)
                    .Replace("%OBJECT%", objectName));
            return;
        }

        if (!Sender.admin && objOwnerID != userData.SteamID)
        {
            Broadcast.Notice(Sender, "☢",
                Configs.Config
                    .GetMessage(Messages.RuMessages.RuMessage.CommandTransferNotYourOwned, Sender, targData.UserName)
                    .Replace("%OBJECT%", objectName));
            return;
        }

        if (Deploy != null)
        {
            if (Configs.Config._Settings.ForbiddenTransfer.Contains(objectName,
                    StringComparer.CurrentCultureIgnoreCase))
            {
                Broadcast.Notice(Sender, "☢",
                    Configs.Config
                        .GetMessage(Messages.RuMessages.RuMessage.CommandTransferForbidden, Sender, targData.UserName)
                        .Replace("%OBJECT%", objectName));
                return;
            }

            Deploy.creatorID = Deploy.ownerID = targData.SteamID;
            Deploy.CacheCreator();
        }

        if (Master != null)
        {
            if (Configs.Config._Settings.ForbiddenTransfer.Contains("Structure",
                    StringComparer.CurrentCultureIgnoreCase))
            {
                Broadcast.Notice(Sender, "☢",
                    Configs.Config
                        .GetMessage(Messages.RuMessages.RuMessage.CommandTransferForbidden, Sender, targData.UserName)
                        .Replace("%OBJECT%", objectName));
                return;
            }

            Master.creatorID = Master.ownerID = targData.SteamID;
            Master.CacheCreator();
        }

        Broadcast.Message(Sender, "You transfer " + objectName + " for \"" + targData.UserName + "\".");
    }
}

public class History : ICommand
{
    public string CmdName => "history";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        if (!Configs.Config.History.ContainsKey(Sender.userID)) return;
        var History = Configs.Config.History[Sender.userID];
        var count = 0;
        if (Args.Length > 0) int.TryParse(Args[0], out count);
        if (count < 1) count = Configs.Config._Settings.ChatHistoryDisplay;
        if (count > History.Count) count = History.Count;
        for (var i = count; i > 0; i--)
            Broadcast.Message(Sender, History[History.Count - i].Name + ": " + History[History.Count - i].Text,
                "HISTORY");
    }
}

public class Shop : ICommand
{
    public string CmdName => "shop";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        var groupIndex = 0;
        string groupName = null;
        List<ShopItem> ShopItems = null;
        // Try find specified category name or index
        if (Args != null && Args.Length > 0)
        {
            if (int.TryParse(Args[0], out groupIndex)) ShopItems = Configs.Shop.GetItems(groupIndex, out groupName);
            if (ShopItems == null) ShopItems = Configs.Shop.GetItems(Args[0], out groupIndex);
        }

        // Get items from entry category if list is null
        if (ShopItems == null) ShopItems = Configs.Shop.SData.ShopPages[Configs.Shop.SData.EntryGroup.name];
        // Output category items list
        foreach (var item in ShopItems)
        {
            var PriceBuy = item.buyPrice > 0 ? item.buyPrice + Economy.EData.CurrencySign : "None";
            var PriceSell = item.sellPrice > 0 ? item.sellPrice + Economy.EData.CurrencySign : "None";
            var ShoplistItem =
                Configs.Config.GetMessage(Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopListItem,
                    Sender));
            ShoplistItem = ShoplistItem.Replace("%INDEX%", item.index.ToString());
            ShoplistItem = ShoplistItem.Replace("%ITEMNAME%", item.name);
            ShoplistItem = ShoplistItem.Replace("%SELLPRICE%", PriceSell);
            ShoplistItem = ShoplistItem.Replace("%BUYPRICE%", PriceBuy);
            ShoplistItem = ShoplistItem.Replace("%QUANTITY%", item.quantity.ToString());
            Broadcast.Message(Sender, ShoplistItem);
        }

        if (groupIndex == 0)
            foreach (var group in Configs.Shop.ShopGroups)
                if (group.name != null && group.index != 0)
                {
                    var ShoplistGroup = Configs.Config.GetMessage(
                        Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopListGroup, Sender));
                    ShoplistGroup = ShoplistGroup.Replace("%INDEX%", group.index.ToString());
                    ShoplistGroup = ShoplistGroup.Replace("%GROUPNAME%", group.name);
                    Broadcast.Message(Sender, ShoplistGroup);
                }

        Broadcast.Message(Sender, Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopHelp, Sender));
    }
}

public class Buy : ICommand
{
    public string CmdName => "buy";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        if (Args == null || Args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        // Initialize item variables
        ShopItem Item = null;
        var ItemIndex = 0;
        // Try find specified item by index or name
        if (int.TryParse(Args[0], out ItemIndex)) Item = Configs.Shop.FindItem(ItemIndex);
        else Item = Configs.Shop.FindItem(Args[0]);
        // Specified item not exists or not available for sell
        if (Item == null || Item.sellPrice == -1)
        {
            var ArgItemName = Item != null ? Item.name : Args[0];
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopBuyItemNotAvailable, Sender)
                    .Replace("%ITEMNAME%", ArgItemName));
            return;
        }

        // Get player inventory
        var inventory = Sender.playerClient.controllable.GetComponent<Inventory>();
        // Check inventory free slots for purchase
        if (inventory == null || inventory.noVacantSlots)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.PlayerInventoryIsFull, Sender));
            return;
        }

        // Get piece price of purchase item
        var PiecePrice = Item.sellPrice / Item.quantity;
        // Get quantity from specified amount or set default quantity of item
        var Quantity = Item.quantity;
        if (Args.Length > 1 && !int.TryParse(Args[1], out Quantity)) Quantity = Item.quantity;
        if (Quantity < 1) Quantity = Item.quantity;
        // Get total price of purchase item by quantity
        var TotalPrice = (ulong)(PiecePrice * Quantity);
        // Get quantity of item and total price for purchase
        if (TotalPrice > Economy.GetBalance(Sender.userID))
        {
            var NotEnoughBalance =
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopBuyNotEnoughBalance, Sender);
            NotEnoughBalance =
                NotEnoughBalance.Replace("%TOTALPRICE%", TotalPrice.ToString("N0") + Economy.EData.CurrencySign);
            NotEnoughBalance = NotEnoughBalance.Replace("%ITEMNAME%", Item.name);
            Broadcast.Notice(Sender, Economy.EData.CurrencySign, NotEnoughBalance);
            return;
        }

        // Try to purchase item by specified quantity
        Quantity = Helper.GiveItem(Sender.playerClient, DatablockDictionary.GetByName(Item.name), Quantity, Item.slots);
        if (Quantity == 0)
        {
            Broadcast.Notice(Sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.PlayerInventoryIsFull, Sender));
            return;
        }

        // Get result message of purchased
        var TradeResult = "\"" + DatablockDictionary.GetByName(Item.name).name + "\"";
        if (Quantity > 1) TradeResult = Quantity + " " + TradeResult;
        // Subtract amount of currency from purchased
        TotalPrice = (ulong)(Quantity * PiecePrice);
        Economy.BalanceSub(Sender.userID, TotalPrice);
        // Send message to player about purchased item
        var ItemPurchased =
            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopBuyItemPurchased, Sender);
        ItemPurchased = ItemPurchased.Replace("%TOTALPRICE%", TotalPrice.ToString("N0") + Economy.EData.CurrencySign);
        ItemPurchased = ItemPurchased.Replace("%ITEMNAME%", TradeResult);
        Broadcast.Notice(Sender, Economy.EData.CurrencySign, ItemPurchased);
        // Call command "balance" after purchase item
        Economy.Balance(Sender, userData, "balance", null);
    }
}

public class Sell : ICommand
{
    public string CmdName => "sell";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        if (Args == null || Args.Length == 0)
        {
            CommandHelper.SendCommandDescription(this, Sender, userData);
            return;
        }

        // Initialize item variables
        ShopItem Item = null;
        var ItemIndex = 0;
        var SoldAmount = 0;
        // Get player inventory
        var inventory = Sender.playerClient.controllable.GetComponent<Inventory>();

        if (Args[0].Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            ulong TotalSold = 0;
            var Result = new List<IInventoryItem>();
            var Iterator = inventory.occupiedIterator;
            while (Iterator.Next())
            {
                Item = Configs.Shop.FindItem(Iterator.item.datablock.name);
                if (Item == null || Item.buyPrice == -1) continue;
                var itemCount = Iterator.item.datablock._splittable ? Iterator.item.uses : 1;
                var sellPrice = (ulong)(Item.buyPrice / Item.quantity) * (ulong)itemCount;
                TotalSold += sellPrice;
                Result.Add(Iterator.item);
            }

            if (Result.Count > 0)
            {
                foreach (var item in Result) inventory.RemoveItem(item);
                if (TotalSold > 0) Economy.BalanceAdd(Sender.userID, TotalSold);
                var AllSold = Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopSellAllSold, Sender);
                AllSold = AllSold.Replace("%TOTALPRICE%", TotalSold.ToString("N0") + Economy.EData.CurrencySign);
                AllSold = AllSold.Replace("%TOTALAMOUNT%", Result.Count.ToString());
                Broadcast.Notice(Sender, Economy.EData.CurrencySign, AllSold);
            }
            else
            {
                Broadcast.Notice(Sender, Economy.EData.CurrencySign,
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopSellNoNothing, Sender));
            }

            return;
        }

        // Try find specified item by index or name
        if (int.TryParse(Args[0], out ItemIndex)) Item = Configs.Shop.FindItem(ItemIndex);
        else Item = Configs.Shop.FindItem(Args[0]);
        // Specified item not exists or not available for buy
        if (Item == null || Item.buyPrice == -1)
        {
            var ArgItemName = Item != null ? Item.name : Args[0];
            Broadcast.Notice(Sender, Economy.EData.CurrencySign,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopSellItemNotAvailable, Sender)
                    .Replace("%ITEMNAME%", ArgItemName));
            return;
        }

        // Get piece price of sell a item
        var PiecePrice = Item.buyPrice / Item.quantity;
        // Get quantity from specified amount or set default quantity of item
        var Quantity = Item.quantity;
        if (Args.Length > 1 && !int.TryParse(Args[1], out Quantity)) Quantity = Item.quantity;
        if (Quantity < 1) Quantity = Item.quantity;
        // Get amount of specified item from player inventory
        SoldAmount = Helper.InventoryItemCount(inventory, DatablockDictionary.GetByName(Item.name));
        if (SoldAmount == 0)
        {
            Broadcast.Notice(Sender, Economy.EData.CurrencySign,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopSellNotEnoughItem, Sender)
                    .Replace("%ITEMNAME%", Item.name));
            return;
        }

        if (Quantity > SoldAmount) Quantity = SoldAmount;
        // Consume amount of specified item from player inventory
        SoldAmount = Helper.InventoryItemRemove(inventory, DatablockDictionary.GetByName(Item.name), Quantity);
        // Get result message for a sold item
        var TradeResult = "\"" + Item.name + "\"";
        if (SoldAmount > 1) TradeResult = SoldAmount + " " + TradeResult;
        // Add amount of currency from item sold
        var TotalPrice = (ulong)(SoldAmount * PiecePrice);
        Economy.BalanceAdd(Sender.userID, TotalPrice);
        // Send message to player about purchased item
        var ItemSold = Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyShopSellItemSold, Sender);
        ItemSold = ItemSold.Replace("%TOTALPRICE%", TotalPrice.ToString("N0") + Economy.EData.CurrencySign);
        ItemSold = ItemSold.Replace("%ITEMNAME%", TradeResult);
        Broadcast.Notice(Sender, Economy.EData.CurrencySign, ItemSold);
        // Call command "balance" after item sold
        Economy.Balance(Sender, userData, "balance", null);
    }
}

public class Balance : ICommand
{
    public string CmdName => "balance";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        var CurrencyBalance = "0" + Economy.EData.CurrencySign;
        if (Sender != null && !Economy.EData.Database.ContainsKey(userData.SteamID)) Economy.Add(userData.SteamID);
        if (Sender != null)
            CurrencyBalance = Economy.EData.Database[userData.SteamID].balance.ToString("N0") +
                              Economy.EData.CurrencySign;

        if (Args != null && Args.Length > 0 && (Sender == null || Sender.admin))
        {
            userData = Data.FindUser(Args[0]);
            if (userData == null)
            {
                Broadcast.Notice(Sender, "✘",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, null, Args[0]));
                return;
            }

            if (!Economy.EData.Database.ContainsKey(userData.SteamID))
            {
                Broadcast.Notice(Sender, "✘", "Player \"" + Args[0] + "\" not have balance");
                return;
            }

            var argValue = Economy.EData.Database[userData.SteamID].balance;
            var balanceAppend = Args.Length > 1 && Args[1].StartsWith("+");
            var balanceSubtract = Args.Length > 1 && Args[1].StartsWith("-");
            if (Args.Length > 1) Args[1] = Args[1].Replace("+", "").Replace("-", "").Trim();

            if (Args.Length > 1 && ulong.TryParse(Args[1], out argValue))
            {
                if (balanceSubtract) Economy.BalanceSub(userData.SteamID, argValue);
                else if (balanceAppend) Economy.BalanceAdd(userData.SteamID, argValue);
                else Economy.EData.Database[userData.SteamID].balance = argValue;

                CurrencyBalance = Economy.EData.Database[userData.SteamID].balance.ToString("N0") +
                                  Economy.EData.CurrencySign;
                Broadcast.Notice(Sender, Economy.EData.CurrencySign,
                    "Balance of \"" + userData.UserName + "\" now " + CurrencyBalance);
            }
            else
            {
                CurrencyBalance = Economy.EData.Database[userData.SteamID].balance.ToString("N0") +
                                  Economy.EData.CurrencySign;
                Broadcast.Notice(Sender, Economy.EData.CurrencySign,
                    "Balance of \"" + userData.UserName + "\" is " + CurrencyBalance);
            }

            return;
        }

        Broadcast.Message(Sender,
            Configs.Config.GetMessage(Messages.RuMessages.RuMessage.EconomyBalance, Sender)
                .Replace("%BALANCE%", CurrencyBalance));
    }
}

public class Money : ICommand
{
    public string CmdName => "money";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser Sender, string Command, string[] Args, User userData)
    {
        CommandHook.GetCommand("balance").Execute(Sender, Command, Args, userData);
    }
}

public class Help : ICommand
{
    private const int MaxLengthKits = 100;

    private const string CAv = "[COLOR#F2FBEF]";
    private const string CList = "[COLOR#00FFFF]";
    public string CmdName => "help";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData)
    {
        if (args.Length > 0)
        {
            if (!Helper.GetAvailableCommands(user).Contains(args[0])) return;

            var command = CommandHook.CommandList.Find(f => f.CmdName == args[0]);
            if (command is null) return;

            CommandHelper.SendCommandDescription(command, user, userData);

            return;
        }

        var haveCommands = false;
        var availableCommands = $"{CAv}Доступные команды: {CList}";

        foreach (var command in Helper.GetAvailableCommands(user))
        {
            if ((availableCommands + command + ", ").Length >= MaxLengthKits)
            {
                CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), $"{CList}{availableCommands}");
                availableCommands = string.Empty;
            }

            availableCommands += command + ", ";
            haveCommands = true;
        }

        if (haveCommands)
        {
            if (availableCommands.Length >= 2)
                availableCommands = availableCommands.Substring(0, availableCommands.Length - 2);
            CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), $"{CList}{availableCommands}");
        }
        else
        {
            CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), "У вас нет доступных команд.");
        }
    }
}

public class Kit : ICommand
{
    public string CmdName => "kit";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        if (args.Length > 0)
        {
            var kit = args[0].ToLower().Trim();
            if (Data.FindUser(args[0]) != null)
            {
                if (args.Length > 1 && user.admin)
                {
                    var target = Helper.GetPlayerClient(Data.FindUser(args[0]).UserName);
                    if (target is not null)
                    {
                        kit = args[1].ToLower().Trim();
                        var kitData = Configs.Kits.KitData.Find(f =>
                            string.Equals(f.KitName, kit, StringComparison.CurrentCultureIgnoreCase));
                        if (kitData is null)
                        {
                            CommandHelper.SendCommandDescription(this, user, userData);
                            return;
                        }

                        foreach (var item in kitData.Items) Helper.GiveItem(target, item.Key, item.Value);
                        Broadcast.SendMessage(target.netUser, $"Вам был выдан кит \"{kitData.KitName}\"!", Color.Lime);
                        Broadcast.SendMessage(user,
                            $"Вы выдали кит \"{kitData.KitName}\" игроку \"{target.userName}\"!", Color.Lime);
                    }
                    else
                    {
                        CommandHelper.SendCommandDescription(this, user, userData);
                    }
                }
                else
                {
                    CommandHelper.SendCommandDescription(this, user, userData);
                }

                return;
            }

            var kitD = Configs.Kits.KitData.Find(f =>
                string.Equals(f.KitName, kit, StringComparison.CurrentCultureIgnoreCase));
            if (userData != null && (kitD is null || !kitD.Ranks.Contains(userData.Rank)))
            {
                CommandHelper.SendCommandDescription(this, user, userData);
                return;
            }

            if (userData != null && userData.KitCoolDowns.ContainsKey(kitD.KitName))
            {
                if (DateTime.Now < userData.KitCoolDowns[kitD.KitName])
                {
                    var time =
                        $"{(userData.KitCoolDowns[kitD.KitName] - DateTime.Now).Days}:{(userData.KitCoolDowns[kitD.KitName] - DateTime.Now).Minutes}:{(userData.KitCoolDowns[kitD.KitName] - DateTime.Now).Seconds}";

                    var split = time.Split(':');

                    if (split[0].Length < 2) split[0] = $"0{split[0]}";
                    if (split[1].Length < 2) split[1] = $"0{split[1]}";
                    if (split[2].Length < 2) split[2] = $"0{split[2]}";

                    CommandHook.Rust.Notice(user,
                        $"Вы не можете использовать кит \"{kitD.KitName}\" ещё \"{split[0]}:{split[1]}:{split[2]}\"!");
                    return;
                }

                userData.KitCoolDowns.Remove(kitD.KitName);
            }

            foreach (var item in kitD.Items) Helper.GiveItem(user.playerClient, item.Key, item.Value);
            Broadcast.SendMessage(user, $"Вы получили кит \"{kitD.KitName}\"!", Color.Lime);
            if (kitD.Cooldown == -1)
            {
                userData?.KitCoolDowns.Add(kitD.KitName, new DateTime(9, 9, 9, 9, 9, 9));
                return;
            }

            var dTime = DateTime.Now;
            dTime = dTime.AddSeconds(kitD.Cooldown);
            userData?.KitCoolDowns.Add(kitD.KitName, dTime);
        }
        else
        {
            CommandHelper.SendCommandDescription(this, user, userData);
        }
    }
}

public class Kits : ICommand
{
    private const int MaxLengthKits = 100;

    private const string CAv = "[COLOR#F2FBEF]";
    private const string CList = "[COLOR#00FFFF]";
    public string CmdName => "kits";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        var haveKits = false;
        var availableKits = $"{CAv}Доступные киты: {CList}";

        foreach (var command in Helper.GetAvailableKits(user))
        {
            if ((availableKits + command + ", ").Length >= MaxLengthKits)
            {
                CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), $"{CList}{availableKits}");
                availableKits = string.Empty;
            }

            availableKits += command + ", ";
            haveKits = true;
        }

        if (haveKits)
        {
            if (availableKits.Length >= 2) availableKits = availableKits.Substring(0, availableKits.Length - 2);
            CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), $"{CList}{availableKits}");
        }
        else
        {
            foreach (var desc in userData is { Language: "Ru" } ? RuDescription : EngDescription)
                CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(), desc);
        }
    }
}

public class Lang : ICommand
{
    public string CmdName => "lang";

    public string[] RuDescription { get; set; }
    public string[] EngDescription { get; set; }

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        if (args.Length > 0)
        {
            if (args[0] is "ru" or "eng")
            {
                if (args[0] == "ru")
                {
                    CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(),
                        "Вы успешно сменили свой язык на: Русский.");
                    if (userData != null) userData.Language = "Ru";
                }
                else
                {
                    CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(),
                        "You have successfully changed your language to: English.");
                    if (userData != null) userData.Language = "Eng";
                }
            }
            else
            {
                CommandHelper.SendCommandDescription(this, user, userData);
            }
        }
        else
        {
            CommandHelper.SendCommandDescription(this, user, userData);
        }
    }
}

public class Who : ICommand
{
    public string CmdName => "who";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser sender, string command, string[] args, User userData)
    {
        var distance = sender.admin ? 1000f : 10f;
        var objectGo = Helper.GetLookObject(Helper.GetLookRay(sender), distance);
        if (objectGo == null)
        {
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandWhoNotSeeAnything, sender), 3f);
            return;
        }

        var objectName = Helper.NiceName(objectGo.name);
        User objectOwner = null;
        var component = objectGo.GetComponent<StructureComponent>();
        var deployable = objectGo.GetComponent<DeployableObject>();
        var takeDamage = objectGo.GetComponent<TakeDamage>();

        if (component != null)
        {
            objectOwner = Data.FindUser(component._master.ownerID);
        }
        else if (deployable != null)
        {
            objectOwner = Data.FindUser(deployable.ownerID);
        }
        else
        {
            Broadcast.Notice(sender, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandWhoCannotOwned, sender), 3f);
            return;
        }

        var objectHealth = Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandWhoCondition, sender);
        if (takeDamage == null)
        {
            objectHealth = "";
        }
        else
        {
            objectHealth = objectHealth.Replace("%OBJECT.HEALTH%", takeDamage.health.ToString());
            objectHealth = objectHealth.Replace("%OBJECT.MAXHEALTH%", takeDamage.maxHealth.ToString());
        }

        if (objectOwner != null)
        {
            var objectDetails = Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandWho, sender)
                .Replace("%OBJECT.CONDITION%", objectHealth);
            objectDetails = objectDetails.Replace("%OBJECT.NAME%", objectName)
                .Replace("%OBJECT.OWNERNAME%", objectOwner.UserName);
            Broadcast.Message(sender, objectDetails);
            if (sender.admin)
            {
                Broadcast.Message(sender, "Steam ID: " + objectOwner.SteamID, "OBJECT OWNER");
                if (!string.IsNullOrEmpty(objectOwner.Clan))
                    Broadcast.Message(sender,
                        "Member of clan: " + Clans.Find(objectOwner.Clan).Name + " <" + Clans.Find(objectOwner.Clan).Abbr + ">",
                        "OBJECT OWNER");
            }
        }
        else
        {
            Broadcast.Message(sender,
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandWhoNotOwned, sender)
                    .Replace("%OBJECT.NAME%", objectName).Replace("%OBJECT.CONDITION%", objectHealth));
        }
    }
}

public class Online : ICommand
{
    public string CmdName => "online";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        Broadcast.Message(user,
            Configs.Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandOnline, "", user));
    }
}

public class Players : ICommand
{
    public string CmdName => "players";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        var Out = "";
        Broadcast.Message(user, Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayers, user));
        foreach (var client in PlayerClient.All.Where(_ =>
                     userData != null && !userData.HasFlag(Data.UserFlags.Invisible)))
        {
            Out += client.netUser.displayName + ", ";
            if (Out.Length <= 70) continue;
            Broadcast.Message(user, Out.Substring(0, Out.Length - 2));
            Out = "";
        }

        if (Out.Length != 0) Broadcast.Message(user, Out.Substring(0, Out.Length - 2));
    }
}

public class Suicide : ICommand
{
    public string CmdName => "suicide";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData)
    {
        if (user.playerClient.controllable.character.alive)
        {
            TakeDamage.KillSelf(user.playerClient.controllable.character);
            Broadcast.SendMessage(user, "You suicided!", Color.Red);
        }
    }
}

public class PM : ICommand
{
    public string CmdName => "pm";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData)
    {
        if (args == null || args.Length < 2)
        {
            CommandHelper.SendCommandDescription(this, user, userData);
            return;
        }

        var client = Helper.GetPlayerClient(args[0]);
        if (client == null)
        {
            Broadcast.Notice(user, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, user, args[0]));
            return;
        }

        if (client.netUser == user)
        {
            Broadcast.Notice(user, "✘", Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPMSelf, user));
            return;
        }

        if (userData.HasFlag(Data.UserFlags.Invisible))
        {
            Broadcast.Notice(user, "✘",
                Configs.Config.GetMessage(Messages.RuMessages.RuMessage.CommandPlayerNoFound, user, args[0]));
            return;
        }

        var muteCd = Data.CountdownGet(user.userID, "mute");
        if (muteCd != null)
        {
            if (!muteCd.Expired)
            {
                var time = TimeSpan.FromSeconds(muteCd.TimeLeft);
                var messageMutedTime = muteCd.Expires ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}" : "-:-:-";
                Broadcast.Notice(user, "☢",
                    Configs.Config.GetMessage(Messages.RuMessages.RuMessage.PlayerMuted, user)
                        .Replace("%TIME%", messageMutedTime));
                return;
            }

            Data.CountdownRemove(user.userID, muteCd);
        }

        var msgArgs = args;
        Array.Copy(args, 1, msgArgs, 0, args.Length - 1);
        Array.Resize(ref msgArgs, msgArgs.Length - 1);
        Broadcast.ChatPm(user, client.netUser, string.Join(" ", msgArgs));
        if (!Configs.Config.Reply.ContainsKey(client.userID)) Configs.Config.Reply.Add(client.userID, user);
        else Configs.Config.Reply[client.userID] = user;
    }
}

public class Users : ICommand
{
    public string CmdName => "users";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "rank":
                {
                    if (!string.IsNullOrEmpty(args[1]) && !string.IsNullOrEmpty(args[2]) &&
                        int.TryParse(args[2], out var result))
                    {
                        var victimUser = Data.FindUser(args[1]);
                        if (victimUser is null)
                        {
                            Broadcast.SendMessage(user, $"Игрок \"{args[1]}\" не найден.", Color.Aqua,
                                CommandHelper.GetChatName());
                            return;
                        }

                        victimUser.Rank = result;
                        Broadcast.SendMessage(user, $"Вы выдали игроку \"{args[1]}\" ранг \"{args[2]}\".", Color.Aqua,
                            CommandHelper.GetChatName());
                        Broadcast.Notice(NetUser.FindByUserID(victimUser.SteamID), "!",
                            $"Вы получили ранг \"{Configs.Ranks.RankList.Find(f => f.Number == int.Parse(args[2])).Name}\"!");
                    }

                    break;
                }
                case "remove":
                {
                    if (!string.IsNullOrEmpty(args[1]))
                    {
                        var victimUser = Data.FindUser(args[1]);
                        if (victimUser is null)
                        {
                            CommandHook.Rust.SendChatMessage(user, CommandHelper.GetChatName(),
                                $"Игрок \"{args[1]}\" не найден.");
                            return;
                        }

                        Data.Users.Remove(victimUser);
                        Broadcast.SendMessage(user, $"Вы удалили аккаунт игроку \"{args[1]}\".", Color.Aqua,
                            CommandHelper.GetChatName());
                    }

                    break;
                }
            }

            return;
        }

        CommandHelper.SendCommandDescription(this, user, userData);
    }
}

public class Zone : ICommand
{
    public string CmdName => "zone";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser sender, string cmd, string[] args, User userData = null)
    {
        var currentZone = Zones.Get(sender.playerClient);
        if (args is { Length: > 0 })
            switch (args[0].ToUpper())
            {
                case "HIDE":
                    Broadcast.Message(sender, "Markers of all zones has been removed.");
                    Zones.HidePoints();
                    return;
                case "SHOW":
                {
                    Zones.HidePoints();
                    foreach (var zone in Zones.All.Values) Zones.ShowPoints(zone);
                    Broadcast.Message(sender, "Markers of all zones has been created.");
                    return;
                }
                case "LIST":
                {
                    Broadcast.Message(sender, "List of zones:");
                    foreach (var defname in Zones.All.Keys)
                        Broadcast.Message(sender, Zones.All[defname].Name + " (" + defname + ")");
                    return;
                }
                case "SAVE":
                    Zones.SaveAsFile();
                    Broadcast.Message(sender, "All zones saved.");
                    return;
                case "LOAD":
                    Zones.LoadAsFile();
                    Broadcast.Message(sender, "All zones reloaded.");
                    return;
                default:
                    currentZone = Zones.Find(args[0]);
                    break;
            }

        if (args is { Length: > 1 })
        {
            var @switch = args[1].ToUpper().Trim();
            switch (@switch)
            {
                case "NEW":
                    if (!Zones.IsBuild)
                    {
                        if (Zones.BuildNew(args[0]))
                        {
                            Broadcast.Notice(sender, "✎", "You starting to create " + Zones.LastZone.Name + " zone");
                            Broadcast.Message(sender,
                                "Use \"/zone \"" + Zones.LastZone.Name + "\" mark\" to adding point for zone.");
                        }
                        else
                        {
                            Broadcast.Message(sender, "Zone with name \"" + args[0] + "\" already exists.");
                        }
                    }
                    else
                    {
                        Broadcast.Message(sender,
                            "You cannot create new zone because have not completed previous zone.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name +
                            "\" save\" to save a previous not completed zone.");
                    }

                    break;
                case "POINT":
                case "MARK":
                    if (Zones.IsBuild)
                    {
                        Zones.BuildMark(sender.playerClient.lastKnownPosition);
                        Broadcast.Notice(sender, "✎", "Point was added for \"" + Zones.LastZone.Name + "\" zone");
                    }
                    else
                    {
                        Broadcast.Message(sender, "You cannot mark point because you not in creating zone.");
                        Broadcast.Message(sender, "Use \"/zone <name> new\" for start creating new zone.");
                    }

                    break;
                case "SAVE":
                    if (Zones.IsBuild)
                    {
                        var buildZoneName = Zones.LastZone.Name;
                        if (Zones.BuildSave())
                            Broadcast.Notice(sender, "✎", "Zone \"" + buildZoneName + "\" a successfully created.");
                        else
                            Broadcast.Notice(sender, "✎",
                                "Error of creation zone \"" + buildZoneName + "\", no points.");
                    }
                    else
                    {
                        Broadcast.Message(sender, "You cannot save zone because you not in creating zone.");
                        Broadcast.Message(sender, "Use \"/zone <name> new\" for start creating new zone.");
                    }

                    break;
                case "SHOW":
                    if (!Zones.IsBuild && currentZone != null) Zones.ShowPoints(currentZone);
                    break;
                case "GO":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "You cannot teleport to zone because have not completed new zone.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else if (currentZone.Spawns.Count == 0)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not have spawn points for teleport.");
                    }
                    else
                    {
                        var spawnIndex = Random.Range(0, currentZone.Spawns.Count);
                        Helper.TeleportTo(sender, currentZone.Spawns[spawnIndex]);
                    }

                    break;
                case "DELETE":
                case "REMOVE":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "You cannot delete zone because have not completed new zone.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else
                    {
                        Broadcast.Notice(sender, "✎", "Zone \"" + currentZone.Name + "\" has been removed.");
                        Zones.Delete(currentZone);
                    }

                    break;
                case "SPAWN":
                case "SPAWNS":
                case "RAD":
                case "RADIATION":
                case "SAFE":
                case "PVP":
                case "DECAY":
                case "BUILD":
                case "TRADE":
                case "EVENT":
                case "CRAFT":
                case "NOENTER":
                case "NOLEAVE":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "Please complete new zone before manage other zones.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else
                    {
                        switch (@switch)
                        {
                            case "SPAWN":
                            {
                                var spawn = sender.playerClient.controllable.character.transform.position;
                                currentZone.Spawns.Add(spawn);
                                Broadcast.Notice(sender, "✎",
                                    "Added new spawn for zone \"" + currentZone.Name + "\" at " + spawn.AsString());
                                break;
                            }
                            case "SPAWNS":
                            {
                                Broadcast.Message(sender,
                                    "Zone \"" + currentZone.Name + "\" have " + currentZone.Spawns.Count +
                                    " spawn(s).");
                                for (var i = 0; i < currentZone.Spawns.Count; i++)
                                    Broadcast.Message(sender, "Spawn #" + i + ": " + currentZone.Spawns[i].AsString());
                                break;
                            }
                            case "RAD":
                            case "RADIATION":
                            {
                                if (currentZone.Radiation) currentZone.Flags ^= ZoneFlags.Radiation;
                                else currentZone.Flags |= ZoneFlags.Radiation;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " +
                                    (currentZone.Radiation ? "with" : "without") + " radiation.");
                                break;
                            }
                            case "SAFE":
                            {
                                if (currentZone.Safe) currentZone.Flags ^= ZoneFlags.Safe;
                                else currentZone.Flags |= ZoneFlags.Safe;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " + (currentZone.Safe ? "with" : "without") +
                                    " safe.");
                                break;
                            }
                            case "PVP":
                            {
                                if (currentZone.NoPvP) currentZone.Flags ^= ZoneFlags.NoPvp;
                                else currentZone.Flags |= ZoneFlags.NoPvp;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " +
                                    (currentZone.NoPvP ? "without" : "with") +
                                    " PvP.");
                                break;
                            }
                            case "DECAY":
                            {
                                if (currentZone.NoDecay) currentZone.Flags ^= ZoneFlags.NoDecay;
                                else currentZone.Flags |= ZoneFlags.NoDecay;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " +
                                    (currentZone.NoDecay ? "without" : "with") +
                                    " decay.");
                                break;
                            }
                            case "BUILD":
                            {
                                if (currentZone.NoBuild) currentZone.Flags ^= ZoneFlags.NoBuild;
                                else currentZone.Flags |= ZoneFlags.NoBuild;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " +
                                    (currentZone.NoBuild ? "without" : "with") +
                                    " build.");
                                break;
                            }
                            case "CRAFT":
                            {
                                if (currentZone.NoCraft) currentZone.Flags ^= ZoneFlags.NoCraft;
                                else currentZone.Flags |= ZoneFlags.NoCraft;
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.Name + "\" now " +
                                    (currentZone.NoCraft ? "with" : "without") +
                                    " craft.");
                                break;
                            }
                            case "NOENTER":
                            {
                                if (currentZone.NoEnter) currentZone.Flags ^= ZoneFlags.NoEnter;
                                else currentZone.Flags |= ZoneFlags.NoEnter;
                                Broadcast.Notice(sender, "✎",
                                    "Players now " + (currentZone.NoEnter ? "cannot" : "can") + " enter into \"" +
                                    currentZone.Name + "\" zone.");
                                break;
                            }
                            case "NOLEAVE":
                            {
                                if (currentZone.NoLeave) currentZone.Flags ^= ZoneFlags.NoLeave;
                                else currentZone.Flags |= ZoneFlags.NoLeave;
                                Broadcast.Notice(sender, "✎",
                                    "Players now " + (currentZone.NoLeave ? "cannot" : "can") + " leave from \"" +
                                    currentZone.Name + "\" zone.");
                                break;
                            }
                        }

                        Zones.SaveAsFile();
                    }

                    break;
                case "COMMAND":
                case "CMD":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone with name \"" + args[0] + "\" not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "Please complete new zone before manage other zones.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Message(sender,
                            "You must enter command name for enable/disable to use in this zone.");
                    }
                    else
                    {
                        if (currentZone.ForbiddenCommand.Contains(args[2]))
                        {
                            currentZone.ForbiddenCommand = currentZone.ForbiddenCommand.Remove(args[2]);
                            Broadcast.Notice(sender, "✎",
                                "Now command \"" + args[2] + "\" CAN be used in a zone \"" + currentZone.Name + "\"");
                        }
                        else
                        {
                            currentZone.ForbiddenCommand = currentZone.ForbiddenCommand.Add(args[2]);
                            Broadcast.Notice(sender, "✎",
                                "Now command \"" + args[2] + "\" FORBIDDEN to use in a zone \"" + currentZone.Name +
                                "\"");
                        }
                    }

                    break;
                case "NAME":
                    if (currentZone == null && !Zones.IsBuild)
                    {
                        Broadcast.Notice(sender, "✘", "Zone with name \"" + args[0] + "\" not exists");
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Message(sender, "You must enter new name of zone for change.");
                    }
                    else
                    {
                        if (Zones.IsBuild)
                        {
                            Zones.LastZone.Name = args[2];
                            Broadcast.Notice(sender, "✎",
                                "Current building zone now named \"" + Zones.LastZone.Name + "\".");
                        }
                        else
                        {
                            if (currentZone != null)
                            {
                                currentZone.Name = args[2];
                                Broadcast.Notice(sender, "✎",
                                    "Zone \"" + currentZone.DefName + "\" now named of \"" + currentZone.Name + "\".");
                            }
                        }
                    }

                    break;
                case "WARP":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "Please complete new zone before manage other zones.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Message(sender, "You must enter defName of other zone for create warp.");
                    }
                    else
                    {
                        WorldZone targetZone = null;
                        if ((targetZone = Zones.Find(args[2])) == null)
                        {
                            Broadcast.Notice(sender, "✘", "Warp zone " + args[2] + " not exists.");
                        }
                        else
                        {
                            targetZone.WarpZone = currentZone;
                            currentZone.WarpZone = targetZone;
                            Broadcast.Notice(sender, "✎",
                                "Zones \"" + targetZone.Name + "\" and \"" + currentZone.Name +
                                "\" now linked for warp.");
                        }
                    }

                    break;
                case "UNWARP":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "Please complete new zone before manage other zones.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else if (currentZone.WarpZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Warp zone " + currentZone.DefName + " not have warp.");
                    }
                    else
                    {
                        Broadcast.Notice(sender, "✎",
                            "Zones \"" + currentZone.WarpZone.Name + "\" and \"" + currentZone.Name +
                            "\" has been unlinked.");
                        currentZone.WarpZone.WarpZone = null;
                        currentZone.WarpZone = null;
                    }

                    break;
                case "WARPTIME":
                    if (currentZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not exists");
                    }
                    else if (Zones.IsBuild)
                    {
                        Broadcast.Message(sender, "Please complete new zone before manage other zones.");
                        Broadcast.Message(sender,
                            "Use \"/zone \"" + Zones.LastZone.Name + "\" save\" to save not completed zone.");
                    }
                    else if (currentZone.WarpZone == null)
                    {
                        Broadcast.Notice(sender, "✘", "Zone " + args[0] + " not have warp.");
                    }
                    else if (args.Length < 2)
                    {
                        Broadcast.Message(sender, "You must enter number of seconds to warp.");
                    }
                    else
                    {
                        long.TryParse(args[2], out currentZone.WarpTime);
                        if (currentZone.WarpTime > 0)
                            Broadcast.Notice(sender, "✎",
                                "You set " + currentZone.WarpTime + " seconds to warp for \"" + currentZone.Name +
                                "\" zone.");
                        else
                            Broadcast.Notice(sender, "✎", "Zone \"" + currentZone.Name + "\" now without warp time.");
                    }

                    break;
                default:
                    CommandHelper.SendCommandDescription(this, sender, userData);
                    break;
            }

            return;
        }

        if (currentZone == null)
        {
            Broadcast.Message(sender, "Zone: Not defined");
        }
        else
        {
            Broadcast.Message(sender, "Zone: " + currentZone.Name + " (" + currentZone.DefName + ")");
            Broadcast.Message(sender, "Flags: " + currentZone.Flags.ToString().Replace(" ", ""));
            Broadcast.Message(sender, "Center: " + currentZone.Center);
            Broadcast.Message(sender, "Points: " + currentZone.Points.Count + ", Spawns: " + currentZone.Spawns.Count);
            if (currentZone.WarpZone == null) return;
            Broadcast.Message(sender, "Warp Zone: " + currentZone.WarpZone.DefName);
            Broadcast.Message(sender, "Warp Time: " + currentZone.WarpTime);
        }
    }
}

public class Set : ICommand
{
    public string CmdName => "set";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.DefaultRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        if (args is { Length: > 0 })
            switch (args[0].Trim().ToUpper())
            {
                case "FPS":
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.ssaa false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.ssao false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.bloom false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.grain false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.shafts false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.tonemap false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.on false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.forceredraw false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.displacement false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.shadowcast false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.shadowreceive false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "render.level 0");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "render.vsync false");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "water.level -1");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "water.reflection false");
                    Broadcast.Notice(user, "✔", "Your graphics have been adjusted on performance.");
                    return;
                case "QUALITY":
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.ssaa true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.ssao true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.bloom true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.grain true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.shafts true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "gfx.tonemap true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.on true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.forceredraw true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.displacement true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.shadowcast true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "grass.shadowreceive true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "render.level 1");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "render.vsync true");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "water.level 1");
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "water.reflection true");
                    Broadcast.Notice(user, "✔", "Your graphics have been adjusted on quality.");
                    return;
                case "NUDE":
                case "NUDITY":
                case "CENSOR":
                    ConsoleNetworker.SendClientCommand(user.networkPlayer, "censor.nudity false");
                    Broadcast.Notice(user, "✔", "Censorship of nudity has is disabled.");
                    return;
            }
        else
            CommandHelper.SendCommandDescription(this, user, userData);
    }
}

public class Ping : ICommand
{
    public string CmdName => "ping";

    public string[] RuDescription { get; set; } = { "" };
    public string[] EngDescription { get; set; } = { "" };

    public int[] Ranks { get; set; } = { Configs.Config._Settings.AutoAdminRank };

    public void Execute(NetUser user, string cmd, string[] args, User userData = null)
    {
        if (user.admin && args.Length > 0)
        {
            var target = Helper.GetPlayerClient(args[0]);
            if (target is null)
            {
                Broadcast.Notice(user, "✘", $"Игрок с ником \"{args[0]}\" не найден!");
                return;
            }

            Broadcast.SendMessage(user, $"Пинг игрока \"{target.userName}\": {target.netPlayer.averagePing} ms.");
        }
        else
        {
            Broadcast.SendMessage(user, $"Ваш пинг: {user.networkPlayer.averagePing} ms.");
        }
    }
}