using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.EventSystem;
using BCore.Mods;
using BCore.Users;
using BCore.WorldManagement;
using Facepunch;
using Facepunch.Clocks.Counters;
using Google.ProtocolBuffers.Serialization;
using Oxide.Core;
using Rust;
using Rust.Steam;
using RustProto;
using uLink;
using UnityEngine;
using BitStream = uLink.BitStream;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using String = Facepunch.Utility.String;
using User = BCore.Users.User;

namespace BCore.Hooks;

public class HookListener
{
    [HookMethod(typeof(TakeDamage), "ProcessDamageEvent")]
    private static void ProcessDamageEvent(TakeDamage hook, ref DamageEvent damage)
    {
        var obj = Interface.CallHook("ModifyDamage", hook, damage);
        if (obj is DamageEvent @event) damage = @event;
        if (hook.takenodamage) return;
        var status = damage.status;
        if (status != LifeStatus.IsAlive)
        {
            if (status == LifeStatus.WasKilled)
            {
                hook.health = 0f;
                Interface.CallHook("OnKilled", hook, damage);
            }
        }
        else
        {
            hook.health -= damage.amount;
            Interface.CallHook("OnHurt", hook, damage);
        }
    }

    [HookMethod(typeof(SupplyDropZone), "CallAirDropAt")]
    private static void CallAirDropAt(Vector3 pos)
    {
        Interface.CallHook("OnAirdrop", pos);
        if (Config._Settings.AirdropNoAirplane)
        {
            int min = Config._Settings.AirDrop ? Config._Settings.AirDropPlanes[0] : 1;
            int num = Config._Settings.AirDrop ? Config._Settings.AirDropPlanes[1] : 3;
            int num2 = UnityEngine.Random.Range(min, num + 1);
            for (int i = 0; i < num2; i++)
            {
                Vector3 position = pos + new Vector3(UnityEngine.Random.Range(-10f, 10f), Config._Settings.AirdropHeight, UnityEngine.Random.Range(-10f, 10f));
                NetCull.InstantiateClassic("SupplyCrate", position, UnityEngine.Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f)), 0).rigidbody.centerOfMass = new Vector3(0f, -1.5f, 0f);
            }
            return;
        }
        SupplyDropPlane component = NetCull.LoadPrefab("C130").GetComponent<SupplyDropPlane>();
        component.maxSpeed = Config._Settings.AirdropAirplaneSpeed;
        float d = 20f * component.maxSpeed;
        Vector3 vector = pos + new Vector3(0f, Config._Settings.AirdropHeight, 0f);
        Vector3 vector2 = vector + SupplyDropZone.RandomDirectionXZ() * d;
        UnityEngine.Quaternion rotation = UnityEngine.Quaternion.LookRotation((vector - vector2).normalized);
        int group = 0;
        NetCull.InstantiateClassic("C130", vector2, rotation, group).GetComponent<SupplyDropPlane>().SetDropTarget(vector);
    }
    
    [HookMethod(typeof(TakeDamage), "OnHurtShared")]
    private static LifeStatus OnHurtShared(IDBase attacker, IDBase victim, TakeDamage.Quantity damageQuantity, out DamageEvent damage, object extraData = null)
    {
        if (victim)
        {
            IDMain idMain = victim.idMain;
            if (idMain)
            {
                TakeDamage takeDamage;
                if (idMain is Character)
                {
                    takeDamage = ((Character)idMain).takeDamage;
                }
                else
                {
                    takeDamage = idMain.GetLocal<TakeDamage>();
                }
                if (takeDamage && !takeDamage.takenodamage)
                {
                    takeDamage.MarkDamageTime();
                    damage.victim.id = victim;
                    damage.attacker.id = attacker;
                    damage.amount = damageQuantity.value;
                    damage.sender = takeDamage;
                    damage.status = ((!takeDamage.dead) ? LifeStatus.IsAlive : LifeStatus.IsDead);
                    damage.damageTypes = 0;
                    damage.extraData = extraData;
                    if (damageQuantity.Unit == TakeDamage.Unit.List)
                    {
                        takeDamage.ApplyDamageTypeList(ref damage, damageQuantity.list);
                    }
                    if (Helper.HurtShared(takeDamage, ref damage, ref damageQuantity))
                    {
                        takeDamage.Hurt(ref damage);
                    }
                    return damage.status;
                }
            }
        }
        damage.victim.id = null;
        damage.attacker.id = null;
        damage.amount = 0f;
        damage.sender = null;
        damage.damageTypes = 0;
        damage.status = LifeStatus.Failed;
        damage.extraData = extraData;
        return LifeStatus.Failed;
    }

    public static bool HurtShared(IDBase victim, ref DamageEvent damage)
    {
        Debug.Log("Hurt.");
        return true;
    }
    
    public static bool IsFreeze(Character idMain, Vector3 origin, int encoded, ushort stateFlags,
        uLink.NetworkMessageInfo info)
    {
        var userData = Data.FindUser(idMain.netUser.userID);
        var timeAfter = (float)(NetCull.time - info.timestamp);
        var position = idMain.transform.position;
        if (userData.HasFlag(Data.UserFlags.Freeze))
        {
            if (Math.Abs(position.x - origin.x) > 0 || Math.Abs(position.y - origin.y) > 0 ||
                Math.Abs(position.z - origin.z) > 0)
            {
                Broadcast.Message(idMain.netUser,
                    Config.GetMessage(Messages.RuMessages.RuMessage.PlayerParalyzed, idMain.netUser));
                object[] args = { idMain.transform.position, idMain.eyesAngles.encoded, stateFlags, timeAfter };
                idMain.networkView.RPC("ReadClientMove", uLink.RPCMode.Others, args);
            }

            return true;
        }

        return false;
    }

    public static bool IsEvent(Character idMain, Vector3 origin)
    {
        var position = idMain.transform.position;
        if (Math.Abs(position.x - origin.x) > 0 || Math.Abs(position.y - origin.y) > 0 ||
            Math.Abs(position.z - origin.z) > 0)
        {
            // Interrupt Event: Home (player returning to camp)
            var homeWarp = Events.Timer.Find(e => e.Sender == idMain.netUser && e.Command == "home");
            if (homeWarp != null)
            {
                homeWarp.Dispose();
                Broadcast.Notice(idMain.netUser.networkPlayer, "☢",
                    Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandHomeInterrupt, "", idMain.netUser));
                return true;
            }

            // Interrupt Event: Clan Warp (player returning to clan house)
            var clanWarp = Events.Timer.Find(e => e.Sender == idMain.netUser && e.Command == "clan");
            if (clanWarp != null)
            {
                clanWarp.Dispose();
                Broadcast.Notice(idMain.netUser.networkPlayer, "☢",
                    Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandClanWarpInterrupt, "",
                        idMain.netUser));
                return true;
            }

            // Interrupt Event: Teleport (player teleportation to player)
            var teleport = Events.Timer.Find(e =>
                (e.Sender == idMain.netUser || e.Target == idMain.netUser) && e.Command == "tp");
            if (teleport != null)
            {
                if (teleport.Sender != null)
                    Broadcast.Notice(teleport.Sender, "☢",
                        Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandTeleportInterrupt, "",
                            teleport.Sender));
                if (teleport.Target != null)
                    Broadcast.Notice(teleport.Target, "☢",
                        Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandTeleportInterrupt, "",
                            teleport.Target));
                teleport.Dispose();
                return true;
            }
        }

        return false;
    }

    [HookMethod(typeof(ResourceTarget), "DoGather")]
    private static bool DoGather(ResourceTarget hook, Inventory reciever, float efficiency)
    {
        if (hook.resourcesAvailable.Count == 0) return false;

        float num = 1f;
        switch (hook.type)
        {
            case ResourceTarget.ResourceTargetType.Animal:
                num = Config._Settings.GatherFlayMultiplier;
                break;
            case ResourceTarget.ResourceTargetType.WoodPile:
                num = Config._Settings.GatherWoodMultiplier;
                break;
            case ResourceTarget.ResourceTargetType.Rock1:
                num = Config._Settings.GatherRockMultiplier;
                break;
            case ResourceTarget.ResourceTargetType.Rock2:
                num = Config._Settings.GatherRockMultiplier;
                break;
            case ResourceTarget.ResourceTargetType.Rock3:
                num = Config._Settings.GatherRockMultiplier;
                break;
        }

        if (num == 0f)
        {
            num = 0.01f;
        }

        ResourceGivePair resourceGivePair =
            hook.resourcesAvailable[Random.Range(0, hook.resourcesAvailable.Count)];
        hook.gatherProgress += efficiency * hook.gatherEfficiencyMultiplier * num;
        int num2 = (int)Mathf.Abs(hook.gatherProgress);
        hook.gatherProgress = Mathf.Clamp(hook.gatherProgress, 0f, num2);
        num2 = Mathf.Min(num2, resourceGivePair.AmountLeft());

        var obj = Interface.CallHook("OnGather", reciever, hook, resourceGivePair, num2);

        if (obj is int) num2 = (int)obj;
        if (num2 > 0)
        {
            User userData = null;
            NetUser netUser = NetUser.Find(reciever.networkView.owner);
            int num3 = reciever.AddItemAmount(resourceGivePair.ResourceItemDataBlock, num2);
            if (num3 < num2)
            {
                if (netUser != null)
                {
                    userData = Data.FindUser(netUser.userID);
                }

                int num4 = 0;
                if (((userData != null) ? userData.Clan : null) != null)
                {
                    if (hook.type == ResourceTarget.ResourceTargetType.WoodPile)
                    {
                        num4 = (int)(num2 * Clans.Find(userData.Clan).Level.BonusGatheringWood / 100L);
                    }
                    else if (hook.type == ResourceTarget.ResourceTargetType.Rock1)
                    {
                        num4 = (int)(num2 * Clans.Find(userData.Clan).Level.BonusGatheringRock / 100L);
                    }
                    else if (hook.type == ResourceTarget.ResourceTargetType.Rock2)
                    {
                        num4 = (int)(num2 * Clans.Find(userData.Clan).Level.BonusGatheringRock / 100L);
                    }
                    else if (hook.type == ResourceTarget.ResourceTargetType.Rock3)
                    {
                        num4 = (int)(num2 * Clans.Find(userData.Clan).Level.BonusGatheringRock / 100L);
                    }
                    else if (hook.type == ResourceTarget.ResourceTargetType.Animal)
                    {
                        num4 = (int)(num2 * Clans.Find(userData.Clan).Level.BonusGatheringAnimal / 100L);
                    }
                }

                if (num4 > 0)
                {
                    num4 -= reciever.AddItemAmount(resourceGivePair.ResourceItemDataBlock, num4);
                }

                int num5 = num2 - num3;
                resourceGivePair.Subtract(num5);
                hook.gatherProgress -= num5;
                Notice.Inventory(reciever.networkView.owner,
                    (num5 + num4).ToString() + " x " + resourceGivePair.ResourceItemName);
                hook.SendMessage("ResourcesGathered", SendMessageOptions.DontRequireReceiver);
                if (((userData != null) ? userData.Clan : null) != null)
                {
                    float num6 = 0f;
                    if (resourceGivePair.ResourceItemName.Equals("Raw Chicken Breast",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 * 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Animal Fat", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Blood", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 * 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Cloth", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Leather", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 * 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Wood", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 / 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Stones", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 / 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Metal Ore", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 * 2;
                    }
                    else if (resourceGivePair.ResourceItemName.Equals("Sulfur Ore", StringComparison.OrdinalIgnoreCase))
                    {
                        num6 = num5 * 2;
                    }

                    if (num6 is >= 0f and >= 1f)
                    {
                        num6 = Math.Abs(num6 * Clans.ExperienceMultiplier);
                        Clans.Find(userData.Clan).Experience += (ulong)num6;
                        if (Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.ExpDetails))
                        {
                            Broadcast.Message(reciever.networkView.owner,
                                Messages.RuMessages.RuMessage.ClanExperienceGather
                                    .Replace("%EXPERIENCE%", num6.ToString("N0")).Replace("%RESOURCE_NAME%",
                                        resourceGivePair.ResourceItemName), null, 0f);
                            
                        }
                    }
                }
            }
            else
            {
                hook.gatherProgress = 0f;
                Notice.Popup(reciever.networkView.owner, "", "Inventory full. You can't gather.", 4f);
            }
        }

        if (!resourceGivePair.AnyLeft())
        {
            hook.resourcesAvailable.Remove(resourceGivePair);
        }

        if (hook.resourcesAvailable.Count == 0)
        {
            hook.SendMessage("ResourcesDepletedMsg", SendMessageOptions.DontRequireReceiver);
        }

        return true;
    }

    [HookMethod(typeof(ConnectionAcceptor), "uLink_OnPlayerConnected")]
    private static void uLink_OnPlayerConnected(ConnectionAcceptor hook, uLink.NetworkPlayer player)
    {
        if (player.localData is not ClientConnection clientConnection)
        {
            NetCull.CloseConnection(player, true);
            return;
        }

        var netUser = new NetUser(player);
        netUser.DoSetup();
        netUser.connection = clientConnection;
        netUser.playerClient = ServerManagement.Get().CreatePlayerClientForUser(netUser);
        ServerManagement.Get().OnUserConnected(netUser);
        ConsoleSystem.Print(
            $"[BCore]: New Connection: [{clientConnection.UserName}:{clientConnection.UserID}]. Connections: " +
            NetCull.connections.Length + " / " + NetCull.maxConnections);
        Server.OnPlayerCountChanged();
        var user = Data.FindUser(clientConnection.UserID);
        user.LastConnectIP = player.externalIP;
        Interface.CallHook("OnPlayerConnected", netUser);
        
        Bootstrapper.BootObject.GetComponent<Synchronization>().StartCoroutine(Synchronization.GetPlayerRPCs(netUser));
    }

    [HookMethod(typeof(ConnectionAcceptor), "uLink_OnPlayerApproval")]
    private static void uLink_OnPlayerApproval(ConnectionAcceptor hook, NetworkPlayerApproval approval)
    {
        if (hook.m_Connections.Count >= server.maxplayers)
        {
            approval.Deny(uLink.NetworkConnectionError.TooManyConnectedPlayers);
            return;
        }

        var clientConnection = new ClientConnection();
        if (!clientConnection.ReadConnectionData(approval.loginData))
        {
            approval.Deny(uLink.NetworkConnectionError.IncorrectParameters);
            return;
        }

        if (Bans.GetBannedUser(clientConnection.UserName) != null
            || Bans.GetBannedUser(clientConnection.UserID) != null
            || Bans.BanList.Find(f => f.IP == approval.ipAddress) != null)
        {
            var user = Bans.GetBannedUser(clientConnection.UserName) ??
                       (Bans.GetBannedUser(clientConnection.UserID) != null
                           ? Bans.GetBannedUser(clientConnection.UserID)
                           : Bans.BanList.Find(f => f.IP == approval.ipAddress));
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: {user.Reason}.");
            approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
            return;
        }

        if (clientConnection.Protocol !=
            int.Parse(new WebClient().DownloadString("http://blessrust.site/appid.inf")))
        {
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: Old client version ({approval.ipAddress}).");
            if (Data.Users.Find(f => f.FirstConnectIP == approval.ipAddress || f.LastConnectIP == approval.ipAddress) is
                not null)
            {
                var user = Data.Users.Find(f =>
                    f.FirstConnectIP == approval.ipAddress || f.LastConnectIP == approval.ipAddress);
                if (user.SteamID != clientConnection.UserID && user.UserName != clientConnection.UserName)
                {
                    Bans.DoBan(user.SteamID, "MultiConnect. (Possible Bots)");
                    if (PlayerClient.FindByUserID(user.SteamID, out var client))
                    {
                        CommandHook.Rust.Notice(client.netUser, "MultiConnect. (Possible Bots)");
                        Debug.Log(
                            $"[BAC] User \"{clientConnection.UserName}\" has been banned. Reason: MultiConnect. (Possible Bots)");
                        client.netUser.Kick(NetError.ConnectionBanned, true);
                    }

                    approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                    return;
                }
            }

            approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
            return;
        }

        if (BanList.Contains(clientConnection.UserID))
        {
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: Banned by SERVER.");
            approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
            return;
        }


        if (Interface.CallHook("IOnUserApprove", clientConnection, approval, hook) != null) return;

        if (hook.IsConnected(clientConnection.UserID))
        {
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: they're already connected.");
            approval.Deny(uLink.NetworkConnectionError.AlreadyConnectedToAnotherServer);
            return;
        }

        if (!Regex.IsMatch(clientConnection.UserName, "^[" + "0-9a-zA-Z. _-".Trim('\"') + "]+$"))
        {
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: Forbidden username syntax.");
            approval.Deny(uLink.NetworkConnectionError.ConnectionFailed);
            return;
        }

        if (Data.FindUser(clientConnection.UserID) is not null)
        {
            var user = Data.FindUser(clientConnection.UserName);
            if (user == null)
            {
                Debug.Log(
                    $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: invalid UserName for current SteamID.");
                approval.Deny(uLink.NetworkConnectionError.DetectedDuplicatePlayerID);
                return;
            }
        }

        if (Data.FindUser(clientConnection.UserName) is not null)
        {
            var user = Data.FindUser(clientConnection.UserName);
            if (user.SteamID != clientConnection.UserID)
            {
                Debug.Log(
                    $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] access denied. Reason: invalid SteamID for current UserName.");
                approval.Deny(uLink.NetworkConnectionError.DetectedDuplicatePlayerID);
                return;
            }
        }

        if (Data.FindUser(clientConnection.UserID) is null)
        {
            if (Data.Users == null) Data.LoadUsers();
            var user = new User(clientConnection.UserID, clientConnection.UserName,
                approval.endpoint.Address.ToString(), "");
            Data.SaveUser(user);
            Debug.Log(
                $"[BCore] User [{clientConnection.UserName}:{clientConnection.UserID}] has been added in Users Database.");
        }

        hook.m_Connections.Add(clientConnection);
        var bs = new BitStream(false);
        bs.WriteString(Globals.currentLevel);
        bs.WriteSingle(NetCull.sendRate);
        bs.WriteString(server.hostname);
        bs.WriteBoolean(Server.Modded);
        bs.WriteBoolean(Server.Official);
        bs.WriteUInt64(Server.SteamID);
        bs.WriteUInt32(Server.IPAddress);
        bs.WriteInt32(server.port);
        approval.localData = clientConnection;
        Boot.SendMods(ref bs);
        approval.Approve(bs.GetDataByteArray());
    }

    [HookMethod(typeof(ConnectionAcceptor), "uLink_OnPlayerDisconnected")]
    private static void uLink_OnPlayerDisconnected(ConnectionAcceptor hook, uLink.NetworkPlayer player)
    {
        Interface.CallHook("OnPlayerDisconnected", player);
        var localData = player.GetLocalData();
        switch (localData)
        {
            case NetUser netUser:
            {
                var playerClient = netUser.playerClient;
                var userData = Data.FindUser(netUser.userID);
                if (sleepers.on)
                {
                    int num = Config._Settings.SleepersLifeTime * 1000;
                    if (userData.HasFlag(Data.UserFlags.Admin))
                    {
                        num = 100;
                    }
                    if (userData.Zone is { NoSleepers: true })
                    {
                        num = 100;
                    }
                    if (num > 0)
                    {
                        Events.SleeperAway(netUser.userID, num);
                    }
                }
                if (userData.UserFlags.Contains(Data.UserFlags.Admin) || netUser.admin)
                {
                    if (Config._Settings.AdminJoin)
                    	Broadcast.MessageAll(
                    		Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerLeave, "%USERNAME%",
                    			Helper.NiceName(playerClient.userName)), netUser);
                }
                else if (userData.UserFlags.Contains(Data.UserFlags.Invisible))
                {
                    if (Config._Settings.InvisibleJoin)
                    	Broadcast.MessageAll(
                    		Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerLeave, "%USERNAME%",
                    			Helper.NiceName(playerClient.userName)), netUser);
                } 
                // Normal Player //

                if (Config._Settings.PlayerLeave)
                	Broadcast.MessageAll(
                		Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerLeave, "%USERNAME%",
                			Helper.NiceName(playerClient.userName)), netUser);
                
                Debug.Log("[BCore] User Disconnected [" + userData.UserName + ":" + userData.SteamID + ":" +
                          userData.LastConnectIP +
                          "]: Connections: " + NetCull.connections.Length + " / " + NetCull.maxConnections);

                netUser.connection.netUser = null;
                hook.m_Connections.Remove(netUser.connection);

                try
                {
                    if (playerClient != null)
                        ServerManagement.Get().EraseCharactersForClient(playerClient, true, netUser);
                    NetCull.DestroyPlayerObjects(player);
                    CullGrid.ClearPlayerCulling(netUser);
                    NetCull.RemoveRPCs(player);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, hook);
                }

                Server.OnUserLeave(netUser.connection.UserID);

                try
                {
                    netUser.Dispose();
                }
                catch (Exception exception2)
                {
                    Debug.LogException(exception2, hook);
                }

                break;
            }
            case ClientConnection local:
                hook.m_Connections.Remove(local);
                break;
        }

        player.SetLocalData(null);
        Server.OnPlayerCountChanged();
    }

    public static void OnPlayerInitialized(NetUser netUser)
    {
        var userData = Data.FindUser(netUser.userID);
        if (userData.UserFlags.Contains(Data.UserFlags.Admin) || netUser.admin)
        {
            if (Config._Settings.AdminJoin)
                Broadcast.MessageAll(
                    Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerJoin, "%USERNAME%",
                        Helper.NiceName(netUser.displayName)), netUser);
        }
        else if (userData.UserFlags.Contains(Data.UserFlags.Invisible))
        {
            if (Config._Settings.InvisibleJoin)
                Broadcast.MessageAll(
                    Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerJoin, "%USERNAME%",
                        Helper.NiceName(netUser.displayName)), netUser);
        } 
        // Normal Player //

        if (Config._Settings.PlayerJoin)
            Broadcast.MessageAll(
                Helper.ReplaceVariables(netUser, Messages.RuMessages.RuMessage.PlayerJoin, "%USERNAME%",
                    Helper.NiceName(netUser.displayName)), netUser);
    }
    
    public static bool OnPlayerChat(NetUser user, string text)
    {
        return false;
    }

    public static void OnChatSay(ConsoleSystem.Arg arg)
    {
        if (!chat.enabled)
        {
            return;
        }
        if (!arg.argUser.CanChat())
        {
            return;
        }
        string text = arg.GetString(0, "text");
        if (text.Length > 128)
        {
            text = text.Substring(0, 128);
        }
        string str = Facepunch.Utility.String.QuoteSafe(arg.argUser.user.Displayname);
        string str2 = Facepunch.Utility.String.QuoteSafe(text);
        
        Debug.Log("[CHAT] " + str + ":" + str2);

        var userData = Data.FindUser(arg.argUser.userID);
        var rank = !string.IsNullOrEmpty(Ranks.RankList.Find(f => f.Number == userData.Rank).Name) && !int.TryParse(Ranks.RankList.Find(f => f.Number == userData.Rank).Name, out var res)
            ? $"[{Ranks.RankList.Find(f => f.Number == userData.Rank).Name}] {str}"
            : $"{str}";

        var color = Config._Settings.RanksColor.ContainsKey(userData.Rank) ? $"[COLOR{Config._Settings.RanksColor[userData.Rank]}]{text}" : $"{text}";
        
        foreach (var player in PlayerClient.All)
        {
            if (player is null) continue;
                    
            if (!Config.History.ContainsKey(player.userID))
                Config.History.Add(player.userID, new List<HistoryRecord>());
            if (Config.History[player.userID].Count > Config._Settings.ChatHistoryStored)
                Config.History[player.userID].RemoveAt(0);
            Config.History[player.userID].Add(new HistoryRecord().Init($"{arg.argUser.user.Displayname}", arg.GetString(0).Trim()));
        }
        
        Broadcast.MessageAll(rank, color);
        arg.argUser.NoteChatted();
    }
    
    [HookMethod(typeof(ConsoleSystem), "RunCommand")]
    private static bool RunCommand(ref ConsoleSystem.Arg arg, bool bWantReply = true)
    {
        var cmdClass = arg.Class.ToLower();
        var cmdFunction = arg.Function.ToLower();

        var isHandled = false;

        switch (cmdClass)
        {
            case "bcore":
            {
                if (arg.argUser != null) return false;
                if (cmdFunction == "unban")
                    if (Bans.GetBannedUser(arg.Args[0]) is not null)
                    {
                        var user = Bans.GetBannedUser(arg.Args[0]);
                        Bans.BanList.Remove(user);
                        Bans.Unban(user.UserName);
                        Debug.Log(
                            $"[BCore] User [{user.UserName}:{user.SteamID}] successfully unbanned. Ban reason: {user.Reason}");
                    }

                break;
            }
            case "serv":
            {
                if (arg.argUser != null) return false;
                if (cmdFunction == "giverank")
                {
                    var user = Data.FindUser(arg.Args[0]);
                    user.Rank = int.Parse(arg.Args[1]);
                    var rank = Ranks.RankList.Find(f => f.Number == user.Rank);
                    CommandHook.Rust.Notice(NetUser.FindByUserID(user.SteamID), $"Вам был выдан ранг: \"{rank.Name}\"!");
                    Debug.Log("Success!");
                    isHandled = true;
                }
                else if (cmdFunction == "givekit")
                {
                    var user = Data.FindUser(arg.Args[0]);
                    var kit = Kits.GetKit(arg.Args[1].ToLower());
                    PlayerClient.FindByUserID(user.SteamID, out var pc);
                    foreach (var item in kit.Items) Helper.GiveItem(pc, item.Key, item.Value);
                    CommandHook.Rust.Notice(NetUser.FindByUserID(user.SteamID), $"Вам был выдан кит: \"{kit.KitName.ToLower()}\"!");
                    Debug.Log("Success!");
                    isHandled = true;
                }
                else
                {
                    var cmd = CommandHook.GetCommand(cmdFunction);
                    cmd?.Execute(arg.argUser, cmd.CmdName, arg.Args);
                    isHandled = true;
                }
                break;
            }
            case "oxide":
            {
                if (arg.argUser != null) return false;
                var callResult = Interface.CallHook("OnRunCommand", arg, bWantReply);
                if (callResult is bool result) return result;
                isHandled = true;
                break;
            }
        }

        if (cmdFunction is "ver" or "version")
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            if (processModule != null)
                ConsoleSystem.Print(" - Rust Server v" + processModule.FileVersionInfo.ProductVersion);
            ConsoleSystem.Print(" - Unity Engine v" + Application.unityVersion);
            ConsoleSystem.Print(" - Oxide2 Core v" + OxideMod.Version);
            ConsoleSystem.Print(" - Bless Core v" + "5.3.1");
            isHandled = true;
        }

        if (arg.argUser != null && cmdClass == "chat" && cmdFunction == "say")
        {
            var sender = arg.argUser;
            var chatText = arg.GetString(0);
            if (Config.ChatQuery.ContainsKey(sender.userID) && !chatText.StartsWith("."))
            {
                var userQuery = Config.ChatQuery[sender.userID];
                if (userQuery.Answered(chatText)) Config.ChatQuery.Remove(sender.userID);
                else Broadcast.Notice(sender, "?", userQuery.Query);
                return false;
            }

            if (chatText.StartsWith("."))
            {
                var user = Data.FindUser(sender.userID);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(user.Clan))
                    {
                        Broadcast.Notice(sender, "?", Messages.RuMessages.RuMessage.CommandClanNotInClan);
                    }
                    else
                    {
                        Broadcast.MessageClan(Clans.Find(user.Clan), chatText.Replace(".", ""), sender);
                    }
                }
                isHandled = true;
            }
            
            if (chatText.StartsWith("/")) isHandled = CommandHook.OnCommand(arg);
            if (isHandled) return false;

            var obj = Interface.CallHook("OnRunCommand", arg, bWantReply);
            if (obj is bool b) return b;
        }

        if (isHandled) return false;

        var array = ConsoleSystem.FindTypes(arg.Class);
        if (array.Length == 0)
        {
            if (bWantReply) arg.ReplyWith("Console class not found: " + arg.Class);

            return false;
        }

        if (bWantReply) arg.ReplyWith(string.Concat("command ", arg.Class, ".", arg.Function, " was executed"));

        var i = 0;
        while (i < array.Length)
        {
            var type = array[i];
            var method = type.GetMethod(arg.Function);
            if (method is { IsStatic: true })
            {
                if (!arg.CheckPermissions(method.GetCustomAttributes(true)))
                {
                    if (bWantReply) arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);

                    return false;
                }

                object[] array3 =
                {
                    arg
                };
                try
                {
                    method.Invoke(null, array3);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(string.Concat("Error: ", arg.Class, ".", arg.Function, " - ", ex.Message));
                    arg.ReplyWith(string.Concat("Error: ", arg.Class, ".", arg.Function, " - ", ex.Message));
                    return false;
                }

                arg = array3[0] as ConsoleSystem.Arg;
                return true;
            }

            var field = type.GetField(arg.Function);
            if (field is { IsStatic: true })
            {
                if (!arg.CheckPermissions(field.GetCustomAttributes(true)))
                {
                    if (bWantReply) arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);

                    return false;
                }

                var fieldType = field.FieldType;
                if (arg.HasArgs())
                    try
                    {
                        var str = field.GetValue(null).ToString();
                        if (fieldType == typeof(float)) field.SetValue(null, float.Parse(arg.Args[0]));

                        if (fieldType == typeof(int)) field.SetValue(null, int.Parse(arg.Args[0]));

                        if (fieldType == typeof(string)) field.SetValue(null, arg.Args[0]);

                        if (fieldType == typeof(bool)) field.SetValue(null, bool.Parse(arg.Args[0]));

                        if (bWantReply)
                            arg.ReplyWith(string.Concat(arg.Class, ".", arg.Function, ": changed ",
                                String.QuoteSafe(str), " to ", String.QuoteSafe(field.GetValue(null).ToString()),
                                " (", fieldType.Name, ")"));
                    }
                    catch (Exception)
                    {
                        if (bWantReply) arg.ReplyWith("error setting value: " + arg.Class + "." + arg.Function);
                    }
                else if (bWantReply)
                    arg.ReplyWith(string.Concat(arg.Class, ".", arg.Function, ": ",
                        String.QuoteSafe(field.GetValue(null).ToString()), " (", fieldType.Name, ")"));

                return true;
            }

            var property = type.GetProperty(arg.Function);
            if (property != null && property.GetGetMethod().IsStatic &&
                property.GetSetMethod().IsStatic)
            {
                if (!arg.CheckPermissions(property.GetCustomAttributes(true)))
                {
                    if (bWantReply) arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);

                    return false;
                }

                var propertyType = property.PropertyType;
                if (arg.HasArgs())
                    try
                    {
                        var str2 = property.GetValue(null, null).ToString();
                        if (propertyType == typeof(float)) property.SetValue(null, float.Parse(arg.Args[0]), null);

                        if (propertyType == typeof(int)) property.SetValue(null, int.Parse(arg.Args[0]), null);

                        if (propertyType == typeof(string)) property.SetValue(null, arg.Args[0], null);

                        if (propertyType == typeof(bool)) property.SetValue(null, bool.Parse(arg.Args[0]), null);

                        if (bWantReply)
                            arg.ReplyWith(string.Concat(arg.Class, ".", arg.Function, ": changed ",
                                String.QuoteSafe(str2), " to ", String.QuoteSafe(property.GetValue(null, null)
                                    .ToString()), " (", propertyType.Name, ")"));
                    }
                    catch (Exception)
                    {
                        if (bWantReply) arg.ReplyWith("error setting value: " + arg.Class + "." + arg.Function);
                    }
                else if (bWantReply)
                    arg.ReplyWith(string.Concat(arg.Class, ".", arg.Function, ": ",
                        String.QuoteSafe(property.GetValue(null, null).ToString()), " (", propertyType.Name, ")"));

                return true;
            }

            i++;
        }

        if (bWantReply) arg.ReplyWith("Command not found: " + arg.Class + "." + arg.Function);

        return false;
    }

    [HookMethod(typeof(DatablockDictionary), "Initialize")]
    private static void Initialize(DatablockDictionary hook)
    {
        DatablockDictionary._dataBlocks = new Dictionary<string, int>();
        DatablockDictionary._dataBlocksByUniqueID = new Dictionary<int, int>();
        DatablockDictionary._lootSpawnLists = new Dictionary<string, LootSpawnList>();

        var list = new List<ItemDataBlock>();
        var set = new HashSet<ItemDataBlock>();
        foreach (var block in Bundling.LoadAll<ItemDataBlock>())
        {
            if (!set.Add(block)) continue;

            var count = list.Count;
            DatablockDictionary._dataBlocks.Add(block.name, count);
            DatablockDictionary._dataBlocksByUniqueID.Add(block.uniqueID, count);
            list.Add(block);
        }

        DatablockDictionary._all = list.ToArray();
        foreach (var list2 in Bundling.LoadAll<LootSpawnList>())
            DatablockDictionary._lootSpawnLists.Add(list2.name, list2);
        DatablockDictionary.initializedAtLeastOnce = true;

        WorldManagement.Override.Initialize();
        if (Interface.Oxide != null) Interface.CallHook("OnDatablocksInitialized", null);
    }

    [HookMethod(typeof(CraftingInventory), "OnCraftStarted")]
    private static void OnCraftStarted(CraftingInventory hook, BlueprintDataBlock blueprint, int amount, ulong startTime)
    {
        if (Interface.CallHook("OnItemCraft", hook, blueprint, amount, startTime) != null)
        {
            return;
        }

        var playerInventory = hook as PlayerInventory;
        if (playerInventory != null && !playerInventory.KnowsBP(blueprint))
        {
            Broadcast.Notice(playerInventory.networkView.owner, "✘",
                Config.GetMessage(Messages.RuMessages.RuMessage.PlayerCraftingBlueprintNotKnown), 2.5f);
            blueprint = null;
        }

        var netUser2 = NetUser.Find(hook.networkView.owner);
        if (netUser2 == null)
        {
            return;
        }
        
        var userData = Data.FindUser(netUser2.userID);
        if (userData == null)
        {
            return;
        }

        if (userData.Zone is { NoCraft: true } && !netUser2.admin)
        {
            if (playerInventory != null)
            {
                Broadcast.Notice(playerInventory.networkView.owner, "✘",
                    Config.GetMessage(Messages.RuMessages.RuMessage.PlayerCraftingNotAvailable), 2.5f);
            }

            blueprint = null;
        }

        var loadOutEntry = LoadOut.DataList.Find(F => F.Ranks.Contains(userData.Rank));
        if (userData != null && loadOutEntry != null && loadOutEntry.NoCrafting.Contains(blueprint.name))
        {
            if (playerInventory != null)
            {
                Broadcast.Notice(playerInventory.networkView.owner, "✘",
                    Config.GetMessage(Messages.RuMessages.RuMessage.PlayerCraftingBlueprintNotAvailable, null, null),
                    2.5f);
            }

            blueprint = null;
        }

        if (playerInventory != null && playerInventory.crafting.Restart(hook, amount, blueprint, startTime))
        {
            playerInventory._lastThinkTime = NetCull.time;
            if (Math.Abs(crafting.timescale - 1f) > 0)
            {
                playerInventory.crafting.duration =
                    Math.Max(0.1f, playerInventory.crafting.duration * crafting.timescale);
            }

            if (userData.Clan != null && Clans.Find(userData.Clan).Level.BonusCraftingSpeed > 0U)
            {
                var num = playerInventory.crafting.duration * Clans.Find(userData.Clan).Level.BonusCraftingSpeed / 100f;
                if (num > 0f)
                {
                    playerInventory.crafting.duration = Math.Max(0.1f, playerInventory.crafting.duration - num);
                }
            }

            if (playerInventory.IsInstant())
            {
                playerInventory.crafting.duration = 0.1f;
            }

            playerInventory.UpdateCraftingDataToOwner();
            playerInventory.BeginCrafting();
        }
    }

    public static void CompleteWork(ItemDataBlock hook, int amount, Inventory workbenchInv)
    {
        User userData = null;
        NetUser netUser = NetUser.Find(workbenchInv.networkView.owner);
        if (netUser != null)
        {
            userData = Data.FindUser(netUser.userID);
        }
        if (userData?.Clan != null)
        {
            float num3 = 0f;
            var temp = false;
            foreach (KeyValuePair<string, int> item2 in Clans.ClanCfg.CraftExperienceItems)
            {
                if (item2.Key == hook.name)
                {
                    num3 = item2.Value;
                    temp = true;
                    break;
                }
            }

            if (!temp)
            {
                foreach (KeyValuePair<string, int> item2 in Clans.ClanCfg.CraftExperienceCategory)
                {
                    if (item2.Key == hook.category.ToString())
                    {
                        num3 = item2.Value;
                        break;
                    }
                }
            }
            num3 *= amount;
            if (num3 < 0f)
            {
                num3 = 0f;
            }
            else if (num3 >= 1f)
            {
                num3 = Math.Abs(num3 * Clans.ExperienceMultiplier);
                Clans.Find(userData.Clan).Experience += (ulong)num3;
                if (Clans.Find(userData.Clan).Members[userData].Has(ClanMemberFlags.ExpDetails))
                {
                    Broadcast.Message(workbenchInv.networkView.owner, Config.GetMessage(Messages.RuMessages.RuMessage.ClanExperienceCrafted).Replace("%EXPERIENCE%", num3.ToString("N0")).Replace("%ITEM_NAME%", hook.name));
                }
            }
        }
        Notice.Inventory(workbenchInv.networkView.owner, amount + " x " + hook.name);
    }

    [HookMethod(typeof(NetUser), "InitializeClientToServer")]
    private static void InitializeClientToServer(NetUser netUser)
    {
        var avatar = netUser.LoadAvatar();
        ServerManagement.Get().UpdateConnectingUserAvatar(netUser, ref netUser.avatar);
        if (netUser.avatar != avatar)
        {
            netUser.SaveAvatar();
        }

        if (ServerManagement.Get().SpawnPlayer(netUser.playerClient, false, netUser.avatar))
        {
            netUser.did_join = true;
        }

        if (Data.FindUser(netUser.userID).HasFlag(Data.UserFlags.Admin))
        {
            netUser.admin = true;
            foreach (string text in Config.GetMessages(Messages.RuMessages.RuMessage.NoticeConnectedAdminMessage,
                         netUser))
            {
                Broadcast.Message(netUser, text, null, 0f);
            }

            if (Data.FindUser(netUser.userID).HasFlag(Data.UserFlags.Invisible))
            {
                Broadcast.Message(netUser, "You now is invisibility.", null, 0f);
            }

            if (Config._Settings.GodMode)
            {
                Data.FindUser(netUser.userID).UserFlags.Add(Data.UserFlags.GodMode);
                Broadcast.Message(netUser, "You now with god mode.", null, 0f);
            }
        }
        else if (Data.FindUser(netUser.userID).HasFlag(Data.UserFlags.Invisible))
        {
            if (Data.FindUser(netUser.userID).HasFlag(Data.UserFlags.Invisible))
            {
                Broadcast.Message(netUser, "You now is invisibility.", null, 0f);
            }
        }
        else
        {
            foreach (string text2 in Config.GetMessages(Messages.RuMessages.RuMessage.NoticeConnectedPlayerMessage,
                         netUser))
            {
                Broadcast.Message(netUser, text2, null, 0f);
            }
        }
        Economy.Balance(netUser, Data.FindUser(netUser.userID), "balance", null);
        if (netUser.playerClient.hasLastKnownPosition)
        {
            Data.FindUser(netUser.userID).Zone = Zones.Get(netUser.playerClient.lastKnownPosition);
        }
        if (!string.IsNullOrEmpty(Data.FindUser(netUser.userID).Clan) && !string.IsNullOrEmpty(Clans.Find(Data.FindUser(netUser.userID).Clan).MOTD))
        {
            Broadcast.MessageClan(netUser, Clans.Find(Data.FindUser(netUser.userID).Clan).MOTD);
        }
        if (Config.ChatQuery.ContainsKey(netUser.userID))
        {
            Broadcast.Message(netUser, Config.ChatQuery[netUser.userID].Query, null, 0f);
        }
    }

    [HookMethod(typeof(ResourceTarget), "TryInitialize")]
    private static void TryInitialize(ResourceTarget hook)
    {
        if (!hook._initialized)
        {
            // Server Initialize //
            foreach (var pair in hook.resourcesAvailable)
            {
                var multiplier = hook.type switch
                {
                    ResourceTarget.ResourceTargetType.WoodPile => Config._Settings.AmountWoodMultiplier,
                    ResourceTarget.ResourceTargetType.Animal => Config._Settings.AmountFlayMultiplier,
                    ResourceTarget.ResourceTargetType.Rock1 => Config._Settings.AmountRockMultiplier,
                    ResourceTarget.ResourceTargetType.Rock2 => Config._Settings.AmountRockMultiplier,
                    ResourceTarget.ResourceTargetType.Rock3 => Config._Settings.AmountRockMultiplier,
                    _ => 1.0f
                };

                if (multiplier == 0) multiplier = 0.01f;
                pair.amountMin = (int)Math.Abs(pair.amountMin * multiplier);
                pair.amountMax = (int)Math.Abs(pair.amountMax * multiplier);
                pair.CalcAmount();
            }

            // Oxide2 Core Interface: CallHook //
            if (Interface.Oxide != null)
            {
                var args = new object[] { hook };
                Interface.CallHook("OnResourceNodeLoaded", args);
            }
        }
        hook._initialized = true;
    }
    [HookMethod(typeof(ServerSaveManager), "Save")]
    private static void Save(string path)
    {
        Interface.CallHook("OnServerSave");
        try
        {
            var restart = SystemTimestamp.Restart;

            if (path == string.Empty) path = "savedgame.sav";
            if (!path.EndsWith(".sav")) path += ".sav";

            if (ServerSaveManager._loading)
            {
                Debug.LogError("Currently loading, aborting save to " + path);
                return;
            }

            Broadcast.MessageAll(
                Messages.RuMessages.RuMessage.ServerWorldSaving);
            ServerSaveManager._saving = true;

            Debug.Log("Saving to '" + path + "'");
            if (!ServerSaveManager._loadedOnce)
            {
                if (File.Exists(path))
                {
                    var text = string.Concat(path, ".",
                        ServerSaveManager.DateTimeFileString(File.GetLastWriteTime(path)), ".",
                        ServerSaveManager.DateTimeFileString(DateTime.Now), ".bak");
                    File.Copy(path, text);
                    Debug.LogError(
                        "A save file exists at target path, but it was never loaded!\n\tbacked up:" +
                        Path.GetFullPath(text));
                }

                ServerSaveManager._loadedOnce = true;
            }

            SystemTimestamp restart2;
            SystemTimestamp restart3;
            WorldSave worldSave;

            using (var recycler = WorldSave.Recycler())
            {
                var builder = recycler.OpenBuilder();
                restart2 = SystemTimestamp.Restart;
                ServerSaveManager.Get().DoSave(ref builder);
                restart2.Stop();
                restart3 = SystemTimestamp.Restart;
                worldSave = builder.Build();
                restart3.Stop();
            }

            var objectsCount = worldSave.SceneObjectCount + worldSave.InstanceObjectCount;

            if (save.friendly)
            {
                using var fileStream = File.Open(path + ".json", FileMode.Create, FileAccess.Write);

                var jsonFormatWriter = JsonFormatWriter.CreateInstance(fileStream);
                jsonFormatWriter.Formatted();
                jsonFormatWriter.WriteMessage(worldSave);
            }

            var timeAll = SystemTimestamp.Restart;
            var timeStream = SystemTimestamp.Restart;

            using (var fileStream2 = File.Open(path + ".new", FileMode.Create, FileAccess.Write))
            {
                worldSave.WriteTo(fileStream2);
                fileStream2.Flush();
            }

            timeStream.Stop();

            if (Config._Settings.SaveBackupCount > 0)
            {
                var count = Config._Settings.SaveBackupCount - 1;
                if (File.Exists(path + ".old." + count)) File.Delete(path + ".old." + count);
                for (var i = count - 1; i >= 0; i--)
                    if (File.Exists(path + ".old." + i))
                        File.Move(path + ".old." + i, path + ".old." + (i + 1));
                if (File.Exists(path)) File.Move(path, path + ".old.0");
            }

            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".new")) File.Move(path + ".new", path);

            try { Data.SaveUsers(); } catch { /*ignored*/ }
            try { Zones.SaveAsFile(); } catch { /*ignored*/ }
            try { Clans.SaveAsTextFile(); } catch { /*ignored*/ }

            timeAll.Stop();
            restart.Stop();

            if (save.profile)
            {
                var args = new object[]
                {
                    objectsCount, restart2.ElapsedSeconds, restart2.ElapsedSeconds / restart.ElapsedSeconds,
                    restart3.ElapsedSeconds, restart3.ElapsedSeconds / restart.ElapsedSeconds,
                    timeStream.ElapsedSeconds, timeStream.ElapsedSeconds / restart.ElapsedSeconds,
                    timeAll.ElapsedSeconds, timeAll.ElapsedSeconds / restart.ElapsedSeconds, restart.ElapsedSeconds,
                    restart.ElapsedSeconds / restart.ElapsedSeconds
                };
                Debug.Log(string.Format(
                    " Saved {0} Object(s) [times below are in elapsed seconds]\r\n  Logic:\t{1,-16:0.000000}\t{2,7:0.00%}\r\n  Build:\t{3,-16:0.000000}\t{4,7:0.00%}\r\n  Stream:\t{5,-16:0.000000}\t{6,7:0.00%}\r\n  All IO:\t{7,-16:0.000000}\t{8,7:0.00%}\r\n  Total:\t{9,-16:0.000000}\t{10,7:0.00%}",
                    args));
                return;
            }
            ConsoleSystem.Print(string.Concat(" Saved ", objectsCount, " Object(s). Took ", restart.ElapsedSeconds,
                " seconds."));
            Broadcast.MessageAll(
                Messages.RuMessages.RuMessage.ServerWorldSaved.Replace(
                    "%SECONDS%",
                    restart.ElapsedSeconds.ToString("0.0000")));
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }


        ServerSaveManager._saving = false;
    }

    [HookMethod(typeof(DeployableObject), "BelongsTo")]
    private static bool BelongsTo(DeployableObject hook, Controllable controllable)
    {
        var userData = Data.FindUser(hook.ownerID);
        if (controllable == null || controllable.netUser == null) return false;

        var netUser = controllable.netUser;
        if (userData == null || netUser.userID == hook.ownerID) return true;

        if (netUser.admin && netUser.userID != hook.ownerID) return hook.GetComponent<BasicDoor>() != null;
        return userData.SharedList.Contains(netUser.userID);
    }

    [HookMethod(typeof(DeployableItemDataBlock), "DoAction1")]
    private static void DoAction1(DeployableItemDataBlock hook, BitStream stream, ItemRepresentation rep,
        ref uLink.NetworkMessageInfo info)
    {
        NetCull.VerifyRPC(ref info);
        if (!rep.Item<IDeployableItem>(out var item) || item.uses <= 0) return;

        var origin = stream.ReadVector3();
        var direction = stream.ReadVector3();

        if (!PlayerClient.Find(info.sender, out var player)) return;
        var character = player.controllable.character;

        var ray = new Ray(origin, direction);
        if (!hook.CheckPlacement(ray, out var position, out var rotation, out var carrier))
        {
            Notice.Popup(info.sender, "", "You can't place that here");
            return;
        }

        if (Physics.OverlapSphere(origin, 0.2f).Select(obj => obj.gameObject.GetComponent<IDBase>())
            .Any(objBase => objBase != null && objBase.idMain is StructureMaster))
        {
            Notice.Popup(info.sender, "", "You can't do it standing here");
            return;
        }

        var startPos = new Vector3(position.x, position.y + 100f, position.z);
        if (Physics.RaycastAll(startPos, Vector3.down, 100f, -1).Any(castHit =>
                castHit.collider.name.IsEmpty() && castHit.collider.tag == "Untagged"))
        {
            Notice.Popup(info.sender, "", "You can't place that in here");
            return;
        }

        /* Get placement world zone */
        var PlacementZone = Zones.Get(position);

        if (PlacementZone is { NoBuild: true })
        {
            /* Users can't place items into not deployable zone */
            Notice.Popup(info.sender, "", "You can't place that here");
            return;
        }
        else if (PlacementZone != null && hook.category == ItemDataBlock.ItemCategory.Weapons && (PlacementZone.Safe || PlacementZone.NoPvP))
        {
            /* Users can't place explosives into no pvp or safety zones */
            Notice.Popup(info.sender, "", "You can't place that here", 4f);
            return;
        }

        if (Config._Settings.OwnershipNotOwnerDenyDeploy.Contains(hook.name))
            foreach (var collider in Physics.OverlapSphere(position, 1f))
            {
                var idBase = collider.gameObject.GetComponent<IDBase>();
                if (idBase == null) continue;
                User ownerData = null;
                var structure = idBase.idMain as StructureMaster;
                var deploy = idBase.idMain as DeployableObject;
                if (structure == null && deploy == null) continue;
                if (structure != null) ownerData = Data.FindUser(structure.ownerID);
                if (deploy != null) ownerData = Data.FindUser(deploy.ownerID);
                if (ownerData != null && ownerData.SteamID == player.userID) continue;
                if (ownerData != null && ownerData.SharedList.Contains(player.userID)) continue;
                Notice.Popup(info.sender, "", "You can't place not on your ownership");
                return;
            }

        if (hook.category == ItemDataBlock.ItemCategory.Survival)
            if (Physics.OverlapSphere(position + Vector3.up, 0.25f)
                .Select(obj => obj.gameObject.GetComponent<IDBase>())
                .Any(objBase => objBase != null && objBase.idMain is StructureMaster))
            {
                Notice.Popup(info.sender, "", "You can't place that here");
                return;
            }

        var deployedObject = NetCull.InstantiateStatic(hook.DeployableObjectPrefabName, position, rotation)
            .GetComponent<DeployableObject>();
        if (deployedObject == null) return;
        try
        {
            deployedObject.SetupCreator(item.controllable);
            hook.SetupDeployableObject(stream, rep, ref info, deployedObject, carrier);
            Interface.CallHook("OnItemDeployed", deployedObject, item);
        }
        finally
        {
            var count = 1;
            if (item.Consume(ref count)) item.inventory.RemoveItem(item.slot);
        }
    }

    [HookMethod(typeof(StructureComponentDataBlock), "DoAction1")]
    private static void DoAction1(StructureComponentDataBlock hook, BitStream stream, ItemRepresentation rep,
        ref uLink.NetworkMessageInfo info)
    {
        NetCull.VerifyRPC(ref info);
        if (!rep.Item(out IStructureComponentItem item) || item.uses <= 0) return;
        var structureToPlacePrefab = hook.structureToPlacePrefab;
        var origin = stream.ReadVector3();
        var direction = stream.ReadVector3();
        var position = stream.ReadVector3();
        var rotation = stream.ReadQuaternion();
        var viewID = stream.ReadNetworkViewID();
        StructureMaster master = null;

        try
        {
            var placementZone = Zones.Get(position);
            if (!PlayerClient.Find(info.sender, out var client))
            {
                // Client Not Found //
            }
            else if (client.netUser.admin)
            {
                // Users with administration rights can placement all items //
            }
            else if (placementZone is { NoBuild: true })
            {
                // Users can't place items into not deployable zone //
                Notice.Popup(info.sender, "", "You can't place that structure here");
                return;
            }

            switch (structureToPlacePrefab.type)
            {
                /* Players can't place foundation on deployable items */
                case StructureComponent.StructureComponentType.Foundation:
                {
                    var placePosition = position + new Vector3(0f, 2f, 0f);
                    foreach (var collider in Physics.OverlapSphere(placePosition, 4f,
                                 GameConstant.Layer.kMask_ServerExplosion))
                    {
                        var main = IDBase.GetMain(collider.gameObject);
                        if (main == null) continue;
                        var objectDeploy = main.GetComponent<DeployableObject>();
                        if (objectDeploy == null || objectDeploy.transform.position.y > position.y + 4f) continue;
                        Notice.Popup(info.sender, "", "You can't place on " + Helper.NiceName(objectDeploy.name));
                        return;
                    }

                    break;
                }
                case StructureComponent.StructureComponentType.Ceiling:
                    /* No checks for Ceiling */
                    break;
                case StructureComponent.StructureComponentType.Pillar:
                {
                    /* Players can't place pillars on deployable items */
                    foreach (var collider in Physics.OverlapSphere(position, 0.2f,
                                 GameConstant.Layer.kMask_ServerExplosion))
                    {
                        var main = IDBase.GetMain(collider.gameObject);
                        if (main == null) continue;
                        var objectDeploy = main.GetComponent<DeployableObject>();
                        if (objectDeploy == null) continue;
                        Notice.Popup(info.sender, "", "You can't place on " + Helper.NiceName(objectDeploy.name));
                        return;
                    }

                    break;
                }
                default:
                {
                    /* Players can't place twice structure in one place and deployable items */
                    foreach (var collider in Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(position, 3f))
                    {
                        var main = IDBase.GetMain(collider);
                        if (main == null) continue;
                        var deployable = main.GetComponent<DeployableObject>();
                        if (deployable != null)
                        {
                            var objectPos = deployable.transform.position;
                            var zDistance = Mathf.Abs(objectPos.y - position.y);
                            if (TransformHelpers.Dist2D(objectPos, position) < 2.0f && zDistance < 0.1f)
                            {
                                Notice.Popup(info.sender, "",
                                    "You can't place near a " + Helper.NiceName(deployable.name));
                                return;
                            }

                            if (TransformHelpers.Dist2D(objectPos, position) < 1.0f && zDistance < 0.1f)
                            {
                                Notice.Popup(info.sender, "",
                                    "You can't place on " + Helper.NiceName(deployable.name));
                                return;
                            }
                        }

                        var structure = main.GetComponent<StructureComponent>();
                        if (structure != null)
                        {
                            if (structure.type != structureToPlacePrefab.type) continue;
                            if (Vector3.Distance(structure.transform.position, position) == 0)
                            {
                                Notice.Popup(info.sender, "", "You can't place that structure here");
                                return;
                            }
                        }
                    }

                    break;
                }
            }

            if (!client.netUser.admin && Config._Settings.OwnerMaxComponents > 0 &&
                Helper.GetPlayerComponents(client.netUser.userID) > Config._Settings.OwnerMaxComponents)
            {
                Notice.Popup(info.sender, "", "You reached limit of available components for building");
                return;
            }

            if (viewID == uLink.NetworkViewID.unassigned)
            {
                if (hook.MasterFromRay(new Ray(origin, direction))) return;

                if (structureToPlacePrefab.type != StructureComponent.StructureComponentType.Foundation)
                {
                    Debug.Log("ERROR, tried to place non foundation structure on terrain!");
                }
                else
                {
                    master = NetCull.InstantiateClassic(
                        Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), position,
                        rotation, 0);
                    master.SetupCreator(item.controllable);
                }
            }
            else
            {
                master = uLink.NetworkView.Find(viewID).gameObject.GetComponent<StructureMaster>();
            }

            if (!client.netUser.admin && Config._Settings.OwnerMaxComponents > 0 &&
                Helper.GetPlayerComponents(master.ownerID) > Config._Settings.OwnerMaxComponents)
            {
                Notice.Popup(info.sender, "", "Owner reached limit of available components for building");
                return;
            }

            if (master == null)
            {
                Debug.Log("NO master, something seriously wrong");
                return;
            }

            if (!client.netUser.admin && Config._Settings.BuildMaxComponents > 0 &&
                master._structureComponents.Count > Config._Settings.BuildMaxComponents)
            {
                Notice.Popup(info.sender, "", "You can't place components anymore for this building");
                return;
            }

            if (!PlayerClient.Find(info.sender, out client))
            {
                //Client Not Found //
            }
            else if (client != null && client.netUser.admin)
            {
                // Users with administration rights can placement all items //
            }
            else if (Config._Settings.OwnershipNotOwnerDenyBuild && master.ownerID != client.userID &&
                     !Data.FindUser(client.userID).SharedList.Contains(master.ownerID))
            {
                Notice.Popup(info.sender, "", "You can't place not on your ownership");
                return;
            }

            if (!hook._structureToPlace.CheckLocation(master, position, rotation) ||
                !hook.CheckBlockers(position)) return;
            var comp = NetCull.InstantiateStatic(hook.structureToPlaceName, position, rotation)
                .GetComponent<StructureComponent>();
            if (comp == null) return;
#pragma warning disable CS0618
            master.AddStructureComponent(comp);
#pragma warning restore CS0618
            master.GetStructureSize(out var maxWidth, out var maxLength, out _);
            var height = Math.Abs((comp.transform.position.y - master.transform.position.y) / 4f);
            switch (client.netUser.admin)
            {
                case false when Config._Settings.MaxHeight > 0 && height > Config._Settings.MaxHeight:
                    master.RemoveComponent(comp);
                    NetCull.Destroy(comp.gameObject);
                    Notice.Popup(info.sender, "", "This building reached a maximum of height");
                    return;
                case false when Config._Settings.MaxLength > 0 && maxLength > Config._Settings.MaxLength:
                    master.RemoveComponent(comp);
                    NetCull.Destroy(comp.gameObject);
                    Notice.Popup(info.sender, "", "This building reached a maximum of length");
                    return;
                case false when Config._Settings.MaxWidth > 0 && maxWidth > Config._Settings.MaxWidth:
                    master.RemoveComponent(comp);
                    NetCull.Destroy(comp.gameObject);
                    Notice.Popup(info.sender, "", "This building reached a maximum of width");
                    return;
            }

            Interface.CallHook("OnStructureBuilt", comp, item);

            var count = 1;
            if (item.Consume(ref count)) item.inventory.RemoveItem(item.slot);
        }
        catch
        {
        }
    }

    [HookMethod(typeof(ItemPickup), "PlayerUse")]
    private static bool PlayerUse(ItemPickup hook, Controllable controllable)
    {
        IInventoryItem item;
        var inventory = hook.GetLocal<Inventory>();
        var playerInventory = controllable.GetLocal<Inventory>();
        if (playerInventory == null) return false;

        var itemPosition = hook.transform.position;
        itemPosition.y += 0.1f;
        var playerOrigin = controllable.character.eyesRay.origin;
        foreach (var obj in Physics.OverlapSphere(playerOrigin, 0.25f))
        {
            var idBase = obj.gameObject.GetComponent<IDBase>();
            if (idBase != null && idBase.idMain is StructureMaster) return false;
        }

        if (Physics.Linecast(playerOrigin, itemPosition, out var hit, -1))
        {
            var idBase = hit.collider.gameObject.GetComponent<IDBase>();
            if (idBase != null && idBase.idMain is StructureMaster) return false;
        }

        if (inventory == null || ReferenceEquals(item = inventory.firstItem, null))
        {
            hook.RemoveThis();
            return false;
        }

        switch (playerInventory.AddExistingItem(item, false))
        {
            case Inventory.AddExistingItemResult.Moved: break;
            case Inventory.AddExistingItemResult.Failed: return false;
            case Inventory.AddExistingItemResult.CompletlyStacked:
                inventory.RemoveItem(item);
                break;
            case Inventory.AddExistingItemResult.PartiallyStacked:
                hook.UpdateItemInfo(item);
                return true;
            case Inventory.AddExistingItemResult.BadItemArgument:
                hook.RemoveThis();
                return false;
            default: throw new NotImplementedException();
        }

        hook.RemoveThis();
        return true;
    }

    [HookMethod(typeof(InventoryHolder), "TryGiveDefaultItems")]
    private static void TryGiveDefaultItems(InventoryHolder holder)
    {
        var loadOut = holder.GetTrait<CharacterLoadoutTrait>().loadout;
        if (loadOut == null) return;

        var userRank = Data.FindUser(holder.netUser.userID).Rank;
        if (LoadOut.DataList.Count == 0)
        {
            loadOut.ApplyTo(holder.inventory);
            return;
        }

        var inventory = (PlayerInventory)holder.inventory;
        foreach (var data in
                 LoadOut.DataList.Where(data => data.Ranks.Length == 0 || data.Ranks.Contains(userRank)))
        {
            foreach (var inventoryItem in data.InventoryItems.Where(inventoryItem =>
                         ReferenceEquals(inventory.FindItem(DatablockDictionary.GetByName(inventoryItem.Key)),
                             null)))
                Helper.GiveItem(inventory, DatablockDictionary.GetByName(inventoryItem.Key),
                    Inventory.Slot.KindFlags.Default, inventoryItem.Value);
            foreach (var inventoryItem in data.BeltItems.Where(inventoryItem =>
                         ReferenceEquals(inventory.FindItem(DatablockDictionary.GetByName(inventoryItem.Key)),
                             null)))
                Helper.GiveItem(inventory, DatablockDictionary.GetByName(inventoryItem.Key),
                    Inventory.Slot.KindFlags.Belt, inventoryItem.Value);
            foreach (var inventoryItem in data.ArmorItems.Where(inventoryItem =>
                         ReferenceEquals(inventory.FindItem(DatablockDictionary.GetByName(inventoryItem.Key)),
                             null)))
                Helper.GiveItem(inventory, DatablockDictionary.GetByName(inventoryItem.Key),
                    Inventory.Slot.KindFlags.Armor);

            foreach (var blueprint in data.Blueprints.Where(blueprint => blueprint != "*"))
                inventory.BindBlueprint((BlueprintDataBlock)DatablockDictionary.GetByName(blueprint));
        }
    }
}