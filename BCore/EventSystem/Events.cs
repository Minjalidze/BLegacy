using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Timers;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.Users;
using RustProto;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BCore.EventSystem;

public class MOTDEvent : IDisposable
{
    public List<string> Announce;
    public bool Enabled;
    private readonly Timer Event;
    public List<string> Messages;
    public string Title;

    public MOTDEvent(string title, int interval = 3600)
    {
        Title = title;
        Messages = new List<string>();
        Announce = new List<string>();
        Event = new Timer();
        Event.Elapsed += (src, args) => DoMessages();
        Event.Elapsed += (src, args) => DoAnnounce();
        Event.AutoReset = true;
        Interval = interval;
    }

    public int Interval
    {
        get => (int)Event.Interval / 1000;
        set => Event.Interval = value * 1000;
    }

    public void Dispose()
    {
        Event.Stop();
        Event.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Start()
    {
        Event.Enabled = Enabled && Event.Interval >= 1.0d;
        if (!Event.Enabled)
        {
            DoMessages();
            DoAnnounce();
        }
    }

    public void Stop()
    {
        Event.Enabled = false;
    }

    protected void DoMessages()
    {
        if (Messages.Count == 0 || !Enabled) return;
        foreach (var S in Messages) Broadcast.MessageAll(Helper.ReplaceVariables(null, S));
    }

    protected void DoAnnounce()
    {
        if (Announce.Count == 0 || !Enabled) return;
        foreach (var S in Announce) Broadcast.NoticeAll("☢", Helper.ReplaceVariables(null, S));
    }
}

public class Events : MonoBehaviour
{
    // List of Timers of Server Events //
    public static List<EventTimer> Timer = new();

    // List of MOTD Events for Server //
    public static List<MOTDEvent> Motd = new();

    // Events: Event Do In Process //
    public static bool EventDoUsers;
    public static bool EventDoServer;

    // Threads: DateTime //
    public static DateTime EventTimeDoServer = DateTime.Now;
    public static DateTime EventTimeDoPlayers = DateTime.Now;

    // Airdrop Time Variables //
    public static long AirdropLastTime = -1;
    public static long AirdropNextTime = -1;
    public static long AirdropNextHour = -1;
    public static long AirdropNextDay = -1;

    private Events()
    {
        /* Initialize Class Instance */
    }

    public static Events Singleton { get; private set; }

    #region [private] Events.Awake

    private void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region [private] Events.DoPlayers

    public static void DoPlayers()
    {
        if (EventDoUsers) return;
        EventDoUsers = true;

        foreach (var userData in Data.All)
        {
            var netUser = NetUser.FindByUserID(userData.SteamID);
            if (netUser is { joinedGameWithCharacter: false }) netUser = null;
            var removeCountdown = new List<Countdown>();
            foreach (var cd in Data.CountdownList(userData.SteamID))
                if (cd.Expires)
                    if (cd.Expired)
                    {
                        removeCountdown.Add(cd);
                    }

            // Remove marked countdowns //
            foreach (var cd in removeCountdown) Data.CountdownRemove(userData.SteamID, cd);

            // Checking destroy ownership for user and disable when time expired //
            if (Config._Settings.OwnerDestroyAutoDisable > 0 && Config.DestoryOwnership.ContainsKey(userData.SteamID))
                if (Config.DestoryOwnership[userData.SteamID] < DateTime.Now)
                {
                    Config.DestoryOwnership.Remove(userData.SteamID);
                    if (netUser != null)
                        Broadcast.Notice(netUser, "☢",
                            Config.GetMessage(Messages.RuMessages.RuMessage.CommandDestroyDisabled));
                }

            // Update online user proparties //
            if (netUser != null && NetCull.connections.Contains(netUser.networkPlayer))
            {
                Character character;
                if (netUser.joinedGameWithCharacter && netUser.admin &&
                    Character.FindByUser(netUser.userID, out character))
                {
                    // Get character metabolism for administration users //
                    var metabolism = character.GetComponent<Metabolism>();
                    // Add calories anytime for administration users //
                    if (metabolism.GetCalorieLevel() < 3000)
                        metabolism.AddCalories(3000 - metabolism.GetCalorieLevel());
                    // Remove radiation anytime for administration users //
                    if (metabolism.GetRadLevel() > 0) metabolism.AddAntiRad(metabolism.GetRadLevel());
                }
            }
        }

        EventDoUsers = false;
    }

    #endregion

    #region [private] Events.DoServer

    public static void DoServer()
    {
        if (EventDoServer) return;
        EventDoServer = true;

        if (Config._Settings.CycleInstantCraft)
        {
            if (crafting.instant && (int)EnvironmentControlCenter.Singleton.GetTime() ==
                Config._Settings.CycleInstantCraftOff)
            {
                Broadcast.NoticeAll("☢", Config.GetMessage(Messages.RuMessages.RuMessage.CycleInstantCraftDisabled));
                crafting.instant = false;
            }
            else if (!crafting.instant && (int)EnvironmentControlCenter.Singleton.GetTime() ==
                     Config._Settings.CycleInstantCraftOn)
            {
                Broadcast.NoticeAll("☢", Config.GetMessage(Messages.RuMessages.RuMessage.CycleInstantCraftEnabled));
                crafting.instant = true;
            }
        }

        EventDoServer = false;
    }

    #endregion

    #region [private] Events.Airdrop

    private void DoAirdrop()
    {
        /*if (Config._Settings.AirDrop && Config._Settings.AirDropPlanes.Length > 0 && NetCull.connections.Length >= airdrop.min_players)
        {
            // Initialize variables //
            bool CallAirdrop = false; int TimeHour = (int)Math.Abs(EnvironmentControlCenter.Singleton.GetTime());
            // Check for call airdrop by time interval //
            if (Config._Settings.AirdropInterval && (uint)Environment.TickCount >= Events.AirdropNextTime)
            {
                if (Events.AirdropLastTime != -1) CallAirdrop = true;
                Events.AirdropLastTime = (uint)Environment.TickCount;
                Events.AirdropNextTime = Events.AirdropLastTime + (Config._Settings.AirdropIntervalTime * 1000);
                if (server.log > 1) Helper.Log("[Airdrop.Extended] A next call airdrop after " + Core.AirdropIntervalTime + " second(s).", true);
            }
            // Check for call airdrop by in-game hour //
            if (Core.AirdropDropTime)
            {
                if (Events.AirdropNextHour == -1 && Core.AirdropDropTimeHours.Length > 0)
                {
                    Events.AirdropNextDay = EnvironmentControlCenter.Singleton.sky.Cycle.Day + 1;
                    if (Core.AirdropDropTimeHours.Length > 2)
                    {
                        Events.AirdropNextHour = Core.AirdropDropTimeHours.Length.Random(0);
                    }
                    else if (Core.AirdropDropTimeHours.Length > 1)
                    {
                        Events.AirdropNextHour = UnityEngine.Random.Range(Core.AirdropDropTimeHours[0], Core.AirdropDropTimeHours[1]);
                    }
                    else
                    {
                        Events.AirdropNextHour = Core.AirdropDropTimeHours[0];
                    }
                    if (server.log > 1) Debug.Log("[Airdrop.Extended] A next call airdrop set on " + Events.AirdropNextHour + " h.", true);
                }
                else if (Events.AirdropNextHour == TimeHour && EnvironmentControlCenter.Singleton.sky.Cycle.Day >= AirdropNextDay)
                {
                    Events.AirdropNextHour = -1; CallAirdrop = true;
                }
            }
        }*/
    }

    #endregion

    // Initialize //

    #region [Public] Initialize

    public static void Initialize()
    {
        //Events.Singleton.InvokeRepeating("DoAirdrop", 0, 1f);
    }

    #endregion

    // Server Event: Shutdown //

    #region [Public] Event Function: Shutdown server

    public static void EventServerShutdown(EventTimer sender, int shutdownTime, ref int timeleft)
    {
        if (timeleft == 0)
        {
        }
        else if (timeleft <= 5)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillShutdown)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 1);
        }
        else if (timeleft == 10)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillShutdown)
                    .Replace("%SECONDS%", timeleft.ToString()));
        }
        else if (timeleft == 30)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillShutdown)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }
        else if (timeleft == 60)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillShutdown)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }
        else if (timeleft == shutdownTime)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerShutdown)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }

        if (timeleft > 0)
        {
            timeleft--;
            return;
        }

        try
        {
            if (sender != null) sender.Stop();

            AvatarSaveProc.SaveAll();
            ServerSaveManager.AutoSave();

            Process.GetCurrentProcess().Kill();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    #endregion

    // Server Event: Restart //

    #region [Public] Event Function: Restart server

    public static void EventServerRestart(EventTimer sender, int shutdownTime, ref int timeleft)
    {
        if (timeleft == 0)
        {
        }
        else if (timeleft <= 5)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillRestart)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 1);
        }
        else if (timeleft == 10)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillRestart)
                    .Replace("%SECONDS%", timeleft.ToString()));
        }
        else if (timeleft == 30)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillRestart)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }
        else if (timeleft == 60)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerWillRestart)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }
        else if (timeleft == shutdownTime)
        {
            Broadcast.NoticeAll("☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.ServerRestart)
                    .Replace("%SECONDS%", timeleft.ToString()), null, 10);
        }

        if (timeleft > 0)
        {
            timeleft--;
            return;
        }

        try
        {
            if (sender != null) sender.Stop();

            AvatarSaveProc.SaveAll();
            ServerSaveManager.AutoSave();

            var executable = Environment.GetCommandLineArgs()[0];
            var arguments = string.Join(" ", Environment.GetCommandLineArgs()).Replace(executable, "").Trim();
            Process.Start(executable, arguments);

            Process.GetCurrentProcess().Kill();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

    #endregion

    // Event: Sleeping Avatar For Player //

    #region [Public] Event: Sleeping Avatar For Player

    public static void SleeperAway(ulong userID, int lifetime)
    {
        var newEvent = new EventTimer { Interval = lifetime, AutoReset = false };
        newEvent.Elapsed += (obj, args) => EventSleeperAway(obj, userID);
        newEvent.Start();
    }

    public static void EventSleeperAway(object obj, ulong userID)
    {
        if (obj is EventTimer timer) timer.Dispose();

        var username = Data.GetUsername(userID);
        var avatar = NetUser.LoadAvatar(userID);
        if (avatar is not { HasAwayEvent: true } ||
            avatar.AwayEvent.Type != AwayEvent.Types.AwayEventType.SLUMBER) return;
        var data = (SleepingAvatar.TransientData)typeof(SleepingAvatar)
            .GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] { userID })!;
        if (!data.exists) return;
        Debug.Log("User Sleeping [" + username + ":" + userID + "] is disappeared.");
        data.AdjustIncomingAvatar(ref avatar);
        NetUser.SaveAvatar(userID, ref avatar);
    }

    #endregion

    // Event: Player Home Warp To Home Camp //

    #region [Public] Event: Teleport Player To Camp

    public static EventTimer TimeEvent_HomeWarp(NetUser sender, string command, double time, Vector3 pos)
    {
        if (time <= 0)
        {
            Teleport_HomeWarp(null, sender, command, pos);
            return null;
        }

        var newEvent = new EventTimer { Interval = time * 1000, AutoReset = false };
        newEvent.Elapsed += (obj, args) => Teleport_HomeWarp(obj, sender, command, pos);
        newEvent.Sender = sender;
        newEvent.Command = command;
        newEvent.Start();

        return newEvent;
    }

    public static void Teleport_HomeWarp(object obj, NetUser sender, string command, Vector3 pos)
    {
        if (obj is EventTimer timer) timer.Dispose();
        if (Config._Settings.HomePayment > 0)
        {
            var userEconomy = Economy.Get(sender.userID);
            var paymentPrice = Config._Settings.HomePayment.ToString("N0") + Economy.EData.CurrencySign;
            if (userEconomy.balance < Config._Settings.HomePayment)
            {
                Broadcast.Notice(sender, "☢",
                    Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeNoEnoughCurrency, sender)
                        .Replace("%PRICE%", paymentPrice));
                return;
            }

            userEconomy.balance -= Config._Settings.HomePayment;
            var currencyBalance = userEconomy.balance.ToString("N0") + Economy.EData.CurrencySign;
            Broadcast.Message(sender,
                Config.GetMessage(Messages.RuMessages.RuMessage.EconomyBalance).Replace("%BALANCE%", currencyBalance));
        }

        if (Config._Settings.HomeCountdown > 0)
            Data.CountdownAdd(sender.userID, new Countdown(command, Config._Settings.HomeCountdown));
        Broadcast.Notice(sender, "☢", Config.GetMessage(Messages.RuMessages.RuMessage.CommandHomeReturn, sender));
        Helper.TeleportTo(sender, pos);
    }

    #endregion

    // Event: Player Clan Warp To Clan House //

    #region [Public] Event: Teleport Player To Clan House

    public static EventTimer TimeEvent_ClanWarp(NetUser netUser, string command, double time, ClanData clan)
    {
        if (time <= 0)
        {
            Teleport_ClanWarp(null, netUser, command, clan);
            return null;
        }

        var newEvent = new EventTimer { Interval = time * 1000, AutoReset = false };
        newEvent.Elapsed += (obj, args) => Teleport_ClanWarp(obj, netUser, command, clan);
        newEvent.Sender = netUser;
        newEvent.Command = command;
        newEvent.Start();
        return newEvent;
    }

    public static void Teleport_ClanWarp(object obj, NetUser netUser, string command, ClanData clan)
    {
        if (obj is EventTimer timer) timer.Dispose();
        Helper.TeleportTo(netUser, clan.Location);
        if (clan.Level.WarpCountdown > 0)
            Data.CountdownAdd(netUser.userID, new Countdown(command, clan.Level.WarpCountdown));
        Broadcast.Notice(netUser, "☢", Config.GetMessage(Messages.RuMessages.RuMessage.CommandClanWarpWarped, netUser));
    }

    #endregion

    // Event: Player Warp To Other Player //

    #region [Public] Event: Teleport Player To Player

    public static EventTimer TimeEvent_TeleportTo(NetUser sender, NetUser target, string command, double time)
    {
        var playerPos = target.playerClient.controllable.character.transform.position;

        if (target.playerClient.controllable.character.stateFlags.airborne)
        {
            Broadcast.Notice(sender, "☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNoTeleport, sender, target.displayName));
            Broadcast.Notice(target, "☢",
                Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotHere, target, sender.displayName));
            return null;
        }

        if (Config._Settings.TeleportOutdoorsOnly)
            foreach (var hit in Physics.SphereCastAll(playerPos, 1f, Vector3.down, 100f,
                         GameConstant.Layer.kMask_ServerExplosion))
            {
                var structure = IDBase.GetMain(hit.collider).GetComponent<StructureMaster>();
                if (structure == null) continue;
                var ownerData = Data.FindUser(structure.ownerID);
                if (ownerData != null)
                {
                    if (structure.ownerID == sender.userID || structure.ownerID == target.userID) continue;
                    if (ownerData.HasShared(sender.userID) || ownerData.HasShared(target.userID)) continue;
                }

                Broadcast.Notice(sender, "☢",
                    Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNoTeleport, sender,
                        target.displayName));
                Broadcast.Notice(target, "☢",
                    Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNotHere, target,
                        sender.displayName));
                return null;
            }

        Broadcast.Message(sender,
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportIsConfirm, sender)
                .Replace("%USERNAME%", target.displayName));
        Broadcast.Message(target,
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportConfirmed, target)
                .Replace("%USERNAME%", sender.displayName));

        Character character;
        if (!Character.FindByUser(target.userID, out character)) return null;
        if (time <= 0)
        {
            Teleport_PlayerTo(null, sender, target, command, character.transform.position);
            return null;
        }

        var newEvent = new EventTimer { Interval = time * 1000, AutoReset = false };
        newEvent.Elapsed += (obj, args) =>
            Teleport_PlayerTo(obj, sender, target, command, character.transform.position);
        newEvent.Sender = sender;
        newEvent.Target = target;
        newEvent.Command = command;

        Broadcast.Notice(sender, "☢",
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportTimewait, sender)
                .Replace("%TIME%", newEvent.TimeLeft.ToString()));
        Broadcast.Notice(target, "☢",
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportTimewait, target)
                .Replace("%TIME%", newEvent.TimeLeft.ToString()));

        newEvent.Start();

        return newEvent;
    }

    public static void Teleport_PlayerTo(object obj, NetUser sender, NetUser target, string command, Vector3 pos)
    {
        if (obj is EventTimer timer) timer.Dispose();

        if (Config._Settings.TeleportPayment > 0)
        {
            var userEconomy = Economy.Get(sender.userID);
            var paymentPrice = Config._Settings.TeleportPayment.ToString("N0") + Economy.EData.CurrencySign;
            if (userEconomy.balance < Config._Settings.TeleportPayment)
            {
                Broadcast.Notice(sender, "☢",
                    Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportNoEnoughCurrency, sender)
                        .Replace("%PRICE%", paymentPrice));
                return;
            }

            userEconomy.balance -= Config._Settings.TeleportPayment;
            var currencyBalance = userEconomy.balance.ToString("N0") + Economy.EData.CurrencySign;
            Broadcast.Message(sender,
                Config.GetMessage(Messages.RuMessages.RuMessage.EconomyBalance, sender)
                    .Replace("%BALANCE%", currencyBalance));
        }

        Broadcast.Notice(sender, "☢",
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportTeleportOnPlayer, sender)
                .Replace("%USERNAME%", target.displayName));
        Broadcast.Notice(target, "☢",
            Config.GetMessage(Messages.RuMessages.RuMessage.CommandTeleportTeleportedPlayer, target)
                .Replace("%USERNAME%", sender.displayName));
        if (Config._Settings.TeleportCountdown > 0)
            Data.CountdownAdd(sender.userID, new Countdown(command, Config._Settings.TeleportCountdown));
        Helper.TeleportTo(sender, pos);
    }

    #endregion
}