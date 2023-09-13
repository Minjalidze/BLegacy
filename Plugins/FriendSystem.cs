using Oxide.Core;
using RustExtended;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FriendSystem", "Sh1ne", "1.1.0")]
    class FriendSystem : RustLegacyPlugin
    {
        const string ChatName = "Друзья";
        const int MaxFriends = 12;
        const int RequestTimeout = 30;

        Dictionary<ulong, Dictionary<ulong, string>> FriendsData;

        // 1 - тот кому кинули запрос, 2 - тот кто кинул запрос
        Dictionary<ulong, KeyValuePair<ulong, string>> LastRequestsReverse = new Dictionary<ulong, KeyValuePair<ulong, string>>();

        void OnServerInitialized()
        {
            LoadDefaultMessages();
        }

        void Init()
        {
            try
            {
                FriendsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<ulong, string>>>("FriendsData");
            }
            catch
            {
                FriendsData = new Dictionary<ulong, Dictionary<ulong, string>>();
            }
        }

        #region Локализация
        protected void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Help_Add", "Добавить друга /f <add/+> <ник/часть ника>" },
                { "Help_Remove", "Удалить из друзей /f <remove/-> <ник/часть ника>" },
                { "Help_List", "Список друзей /f list" },
                { "Help_Accept", "Принять последний запрос /f accept" },
                { "Help_Decline", "Отклонить последний запрос /f decline" },

                { "CommandNotFound", "Неизвестная команда!" },

                { "PlayerNotFound", "Игрок {0} не найден!" },
                { "CantAddSelf", "Вы не можете добавить себя!" },
                { "AlreadyOnList", "{0} уже ваш друг!" },
                { "FriendlistFull", "Ваш список друзей заполнен!" },
                { "PlayerHaveRequest", "Игрок еще не ответил на последний запрос!" },
                { "AcceptTimeout", "{0} не успел принять запрос!" },
                { "RequestSendPlayer", "Вы отправили запрос в друзья {0}" },
                { "RequestSendTarget", "Игрок {0} предлагает вам дружбу. Напишите /f accept чтобы принять" },
                { "YouDontAccepted", "Вы не успели принять запрос от игрока {0}" },

                { "PlayerNotInList", "Игрока {0} нет в вашем списке друзей!" },
                { "MultiplePlayers", "Найдено несколько таких друзей:" },
                { "MultiplePlayersItem", "{0}" },
                { "PlayerRemoved", "Вы удалили игрока {0} из списка друзей!" },
                { "PlayerRemoveYou", "Игрок {0} удалил вас из списка друзей!" },

                { "RequestNotFound", "У вас нет активных запросов!" },
                { "FriendAdded", "Теперь вы и {0} друзья!" },

                { "RequestCanceled", "Вы отменили запрос {0}" },
                { "YourRequestCanceled", "Ваш запрос {0} был отменен!" },

                { "FriendList", "Список ваших друзей: {0}." },
                { "FriendListEmpty", "Ваш список друзей пуст!" },
            }, this);
        }
        #endregion

        void AddFriend(ulong playerId, ulong friendId, string friendName)
        {
            if (!FriendsData.ContainsKey(playerId))
                FriendsData.Add(playerId, new Dictionary<ulong, string>());

            FriendsData[playerId].Add(friendId, friendName);
        }

        void RemoveFriend(ulong playerId, ulong friendId)
        {
            if (FriendsData.ContainsKey(playerId) && FriendsData[playerId].ContainsKey(friendId))
            {
                FriendsData[playerId].Remove(friendId);
            }
        }

        Dictionary<ulong, string> GetFriendList(ulong playerId)
        {
            if (FriendsData.ContainsKey(playerId))
                return FriendsData[playerId];
            return new Dictionary<ulong, string>();
        }

        void CheckAccept(NetUser netUser, ulong targetId, string targetName)
        {
            if (LastRequestsReverse.ContainsKey(targetId))
            {
                LastRequestsReverse.Remove(targetId);
                if (netUser != null)
                {
                    Reply(netUser, "AcceptTimeout", targetName);

                    NetUser friend = NetUser.FindByUserID(targetId);
                    if (friend != null) Reply(friend, "YouDontAccepted", netUser.displayName);
                }
            }
        }

        #region [ChatCommand] /f, /friend
        [ChatCommand("f")]
        void cmdF(NetUser netUser, string command, string[] args)
        {
            cmdFriend(netUser, command, args);
        }

        [ChatCommand("friend")]
        void cmdFriend(NetUser netUser, string command, string[] args)
        {
            string text = $"Command [{netUser.displayName}:{netUser.userID}] /" + command;
            foreach (string s in args) text += " " + s;
            Helper.LogChat(text, true);

            if (args.Length == 0)
            {
                Reply(netUser, "Help_Add");
                Reply(netUser, "Help_Accept");
                Reply(netUser, "Help_Remove");
                Reply(netUser, "Help_Decline");
                Reply(netUser, "Help_List");
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                case "+":
                    if (args.Length < 2)
                    {
                        Reply(netUser, "Help_Add");
                        return;
                    }

                    PlayerClient target = Helper.GetPlayerClient(args[1]);

                    if (target == null)
                    {
                        Reply(netUser, "PlayerNotFound", args[1]);
                        return;
                    }

                    if (target.netUser == netUser)
                    {
                        Reply(netUser, "CantAddSelf");
                        return;
                    }

                    var friendList = GetFriendList(netUser.userID);
                    if (friendList.ContainsKey(target.userID))
                    {
                        Reply(netUser, "AlreadyOnList", target.netUser.displayName);
                        return;
                    }

                    if (friendList.Count >= MaxFriends)
                    {
                        Reply(netUser, "FriendlistFull", target.netUser.displayName);
                        return;
                    }

                    if (LastRequestsReverse.ContainsKey(target.userID))
                    {
                        Reply(netUser, "PlayerHaveRequest");
                        return;
                    }

                    LastRequestsReverse.Add(target.userID, new KeyValuePair<ulong, string>(netUser.userID, netUser.displayName));

                    Reply(netUser, "RequestSendPlayer", target.netUser.displayName);
                    Reply(target.netUser, "RequestSendTarget", netUser.displayName);

                    timer.Once(RequestTimeout, () => CheckAccept(netUser, target.userID, target.netUser.displayName));
                    return;

                case "remove":
                case "-":
                    if (args.Length < 2)
                    {
                        Reply(netUser, "Help_Remove");
                        return;
                    }

                    if (!FriendsData.ContainsKey(netUser.userID) || FriendsData[netUser.userID].Count == 0)
                    {
                        Reply(netUser, "FriendListEmpty");
                        return;
                    }

                    PlayerClient rTarget = Helper.GetPlayerClient(args[1]);

                    ulong rFriendId = 0;
                    if (rTarget != null)
                    {
                        if (FriendsData[netUser.userID].ContainsKey(rTarget.userID))
                        {
                            rFriendId = rTarget.userID;
                        }
                        else
                        {
                            Reply(netUser, "PlayerNotInList", rTarget.netUser.displayName);
                            return;
                        }
                    }
                    else
                    {
                        List<string> FoundFriends = new List<string>();
                        foreach (var pair in FriendsData[netUser.userID])
                        {
                            if (pair.Value.Contains(args[1]))
                            {
                                rFriendId = pair.Key;
                                FoundFriends.Add(pair.Value);
                            }
                        }

                        if (FoundFriends.Count == 0)
                        {
                            Reply(netUser, "PlayerNotInList", args[1]);
                            return;
                        }
                        else if (FoundFriends.Count > 1)
                        {
                            Reply(netUser, "MultiplePlayers");
                            foreach (var item in FoundFriends)
                            {
                                Reply(netUser, "MultiplePlayersItem", item);
                            }
                            return;
                        }
                    }

                    Reply(netUser, "PlayerRemoved", FriendsData[netUser.userID][rFriendId]);

                    NetUser friend = NetUser.FindByUserID(rFriendId);
                    if (friend != null)
                    {
                        Reply(friend, "PlayerRemoveYou", netUser.displayName);
                    }

                    RemoveFriend(netUser.userID, rFriendId);
                    RemoveFriend(rFriendId, netUser.userID);
                    return;

                case "list":
                    if (FriendsData.ContainsKey(netUser.userID))
                    {
                        if (FriendsData[netUser.userID].Count > 0)
                        {
                            string tempFriendList = "";
                            foreach (var pair in FriendsData[netUser.userID])
                            {
                                tempFriendList += pair.Value + ", ";
                            }

                            tempFriendList = tempFriendList.Substring(0, tempFriendList.Length - 2);
                            Reply(netUser, "FriendList", tempFriendList);
                            return;
                        }
                    }
                    Reply(netUser, "FriendListEmpty");
                    return;

                case "accept":
                    if (!LastRequestsReverse.ContainsKey(netUser.userID))
                    {
                        Reply(netUser, "RequestNotFound");
                        return;
                    }

                    ulong friendId = LastRequestsReverse[netUser.userID].Key;
                    string friendName = LastRequestsReverse[netUser.userID].Value;

                    AddFriend(netUser.userID, friendId, friendName);
                    AddFriend(friendId, netUser.userID, netUser.displayName);
                    SaveFriendsData();

                    LastRequestsReverse.Remove(netUser.userID);

                    Reply(netUser, "FriendAdded", friendName);

                    PlayerClient friendClient = null;
                    PlayerClient.FindByUserID(friendId, out friendClient);
                    if (friendClient != null) Reply(friendClient.netUser, "FriendAdded", netUser.displayName);

                    return;

                case "decline":
                    if (!LastRequestsReverse.ContainsKey(netUser.userID))
                    {
                        Reply(netUser, "RequestNotFound");
                        return;
                    }

                    Reply(netUser, "RequestCanceled", LastRequestsReverse[netUser.userID].Value);

                    PlayerClient friendClient2 = null;
                    PlayerClient.FindByUserID(LastRequestsReverse[netUser.userID].Key, out friendClient2);
                    if (friendClient2 != null) Reply(friendClient2.netUser, "YourRequestCanceled", netUser.displayName);

                    LastRequestsReverse.Remove(netUser.userID);
                    return;
            }

            Reply(netUser, "CommandNotFound");
        }
        #endregion

        void SaveFriendsData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("FriendsData", FriendsData);
        }

        private void Reply(NetUser netUser, string langKey, params object[] args)
        {
            rust.SendChatMessage(netUser, ChatName, string.Format(lang.GetMessage(langKey, this, netUser.userID.ToString()), args));
        }

        // FriendSystem

        public static List<ulong> playersWithPlugin = new List<ulong>();

        void AddPluginToPlayer(PlayerClient pc)
        {
            if (pc.gameObject.GetComponent<FriendsVM>() == null)
            {
                if (playersWithPlugin.Contains(pc.userID)) playersWithPlugin.Remove(pc.userID);

                var friendsVm = pc.gameObject.AddComponent<FriendsVM>();
                friendsVm.playerClient = pc;
                friendsVm.friendSystem = this;

                var marksVm = pc.gameObject.AddComponent<MarksVM>();
                marksVm.playerClient = pc;
                marksVm.friendSystem = this;

                playersWithPlugin.Add(pc.userID);
            }
        }

        void Loaded()
        {
            foreach (var pc in PlayerClient.All)
            {
                if (pc != null && pc.netPlayer != null)
                {
                    AddPluginToPlayer(pc);
                }
            }
        }

        void OnPlayerConnected(NetUser netUser)
        {
            if (netUser.playerClient != null)
            {
                AddPluginToPlayer(netUser.playerClient);
            }
        }

        void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            foreach (ulong _steamID in playersWithPlugin)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(_steamID, out pclient);
                if (pclient == null || pclient.netPlayer == networkPlayer)
                {
                    playersWithPlugin.Remove(_steamID);
                    break;
                }
            }
        }

        void UnloadPlugin(GameObject obj, Type plugin)
        {
            if (obj.GetComponent(plugin) != null)
                UnityEngine.Object.Destroy(obj.GetComponent(plugin));
        }

        void Unload()
        {
            foreach (var _steamID in playersWithPlugin)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(_steamID, out pclient);
                if (pclient != null)
                {
                    UnloadPlugin(pclient.gameObject, typeof(FriendsVM));
                    UnloadPlugin(pclient.gameObject, typeof(MarksVM));
                }
            }
        }

        class FriendsVM : MonoBehaviour
        {
            public PlayerClient playerClient = null;
            public FriendSystem friendSystem = null;
            new Facepunch.NetworkView networkView = null;

            void Start()
            {
                networkView = GetComponent<Facepunch.NetworkView>();
            }

            [RPC]
            public void UpdateFriendList()
            {
                if (friendSystem == null)
                {
                    throw new NullReferenceException();
                }

                SendRPC("ClearFriends", playerClient);
                foreach (var pair in friendSystem.GetFriendList(playerClient.userID))
                {
                    var friend = NetUser.FindByUserID(pair.Key);
                    if (friend != null)
                    {
                        SendRPC("GetFriend", playerClient, pair.Key, pair.Value);
                    }
                }
                SendRPC("FinishReceive", playerClient);
            }

            [RPC]
            public void UpdateFriendsPos()
            {                
                foreach (var pair in friendSystem.GetFriendList(playerClient.userID))
                {
                    var friend = NetUser.FindByUserID(pair.Key);
                    if (friend != null)
                    {
                        SendRPC("GetFriendPos", playerClient, friend.userID, friend.playerClient.lastKnownPosition);
                    }
                }
            }

            private void SendRPC(string rpcName, PlayerClient player, params object[] param)
            {
                networkView.RPC(rpcName, player.netPlayer, param);
            }
        }

        class MarksVM : MonoBehaviour
        {
            public PlayerClient playerClient = null;
            public FriendSystem friendSystem = null;
            new Facepunch.NetworkView networkView = null;

            void Start()
            {
                networkView = GetComponent<Facepunch.NetworkView>();
            }

            [RPC]
            public void CreateMark(Vector3 position)
            {
                foreach (var pair in friendSystem.GetFriendList(playerClient.userID))
                {
                    var friend = NetUser.FindByUserID(pair.Key);
                    if (friend != null)
                    {
                        friend.playerClient.networkView.RPC("ReceiveMark", friend.playerClient.netPlayer, playerClient.userName, position);
                    }
                }
            }
        }
    }
}