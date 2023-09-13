using System;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
using RustExtended;
using Oxide.Plugins;

namespace Oxide.Plugins
{
    [Info("MenuGUI", "Sh1ne", "1.0.0")]
    internal class MenuGUI : RustLegacyPlugin
    {
        public static List<ulong> playersWithPlugin = new List<ulong>();

        public static Dictionary<string, string> RussianTexts = new Dictionary<string, string>
        {
            { "Scrap", "Cерверная валюта,которая меняется на ресурсы в обменнике(F4). Можно найти на различных заброшенных зданиях,заводах,а также можно найти в кладе,аирдропе или в мутантах." },
            { "Обменник Scrap'a", "Позволяет обменивать Scrap на различные ресурсы и наоборот.(Открыть - F4)" },
            { "Система квестов", "Добавляет в игру много интересных заданий,выполняя которые вы получаете различные награды. Квесты разделены на 2 ветки - PVE и PVP(можно выполнять одновременно 2 ветки) . Награда за 11 финальное задание - привилегия на следующий вайп." },
            { "Автолутер", "Позволяет удобнее переносить вещи к себе в инвентарь и наоборот. (Использовать сочетание клавиш - Shift+ЛКМ)" },
            { "Рога", "Выпадают с определенным шансом при добыче оленя. Используются для крафта чая(Рога нужно переработать в переработчике)" },
            { "Чай", "Выпив его(обязательно через горячий слот),вы получите бонус к фарму ресурсов на 20 минут(После смерти бонус пропадает). Сделать можно переработав рога." },
            { "Переработчик", "Позволяет переработать не нужные вещи на ресурсы. Находится на ангаре,биге,смоле в определенных белых домах." },
            { "Карта", "Небольшая карта мира(Открывается на M)" },
            { "Клады", "Существуют 4 вида карт клада(Map 1,Map 2,Map 3,Map 4). Карты тематические. Можно найти на различных заброшенных зданиях,заводах,а также можно найти в кладе,аирдропе или в мутантах. " },
            { "Map 1", "Low Quality Metal и с определенным шансом изучения железного дома. " },
            { "Map 2", "Рога и с определенным шансом ресурсы" },
            { "Map 3", "P250 и с определенным шансов оружия/патроны" },
            { "Map 4", "F1 Grenade 3шт. и с определенным шансом с4,экспа,сопля,порох, металл" },
            { "Трейд", "Удобная и надёжная панель для торговли с игроками. (Использовать - /trade и ник игрока)" }
        };
        public static Dictionary<string, string> EngTexts = new Dictionary<string, string>
        {
            { "Scrap", "Is a server currency that is exchanged for resources in the exchanger (F4). It can be found in various abandoned buildings, factories, and can also be found in the treasure, airdrop or mutants." },
            { "Scrap Exchanger", "Allows you to exchange Scrap for various resources and vice versa.(Open - F4)" },
            { "Quest system", "Adds a lot of interesting tasks to the game, completing which you receive various rewards. Quests are divided into 2 branches - PVE and PVP (2 branches can be performed simultaneously) . The reward for the 11th final task is a privilege for the next vape." },
            { "FastLoot", "Allows you to more conveniently transfer things to your inventory and vice versa. (Use the keyboard shortcut - Shift+LMB)" },
            { "Horns", "Fall out with a certain chance when hunting deer. Used for crafting tea (Horns need to be processed in a processor)" },
            { "Tea", "After drinking it (necessarily through a hot slot), you will receive a bonus to farm resources for 20 minutes (After death, the bonus disappears). You can do this by recycling the horns." },
            { "Recycler", "Allows you to recycle unnecessary things for resources. It is located on the hangar, big,small rt in certain white houses." },
            { "Map", "Small world map(Open on M)" },
            { "Treasures", "There are 4 types of treasure maps (Map 1, Map 2, Map 3, Map 4). The maps are themed. It can be found in various abandoned buildings, factories, and can also be found in a treasure trove, airdrop, or mutants" },
            { "Map 1", "Low Quality Metal and with a certain chance of exploring the iron house" },
            { "Map 2", "Horns and with a certain chance resources" },
            { "Map 3", "P250 and with a certain chance of weapons/ammo" },
            { "Map 4", "F1 Grenade 3 pcs. and with a certain chance c4,expa,snot, gunpowder, metal" },
            { "Trade", "Is a convenient and reliable panel for trading with players. (Use - /trade and the player's nickname)" },
            { "Friends", "You can add your friend to your \"friend list\". (Command - /friend) A friend will be highlighted on your screen, and also see your labels (Put a label on the mouse wheel)" }
        };

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
                    UnloadPlugin(pclient.gameObject, typeof(KitsVM));
                    UnloadPlugin(pclient.gameObject, typeof(ShopVM));
                }
            }
        }

        void AddPluginToPlayer(PlayerClient pc)
        {
            if (pc.gameObject.GetComponent<KitsVM>() == null)
            {
                if (playersWithPlugin.Contains(pc.userID)) playersWithPlugin.Remove(pc.userID);

                var kitsVm = pc.gameObject.AddComponent<KitsVM>();
                kitsVm.playerClient = pc;

                var shopVm = pc.gameObject.AddComponent<ShopVM>();
                shopVm.playerClient = pc;

                playersWithPlugin.Add(pc.userID);
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

        class KitsVM : MonoBehaviour
        {
            public PlayerClient playerClient = null;

            //TODO: AntiDDOS of RPC

            [RPC]
            public void GetHelpText()
            {
                if (Users.GetBySteamID(playerClient.userID).Language == "ENG")
                {
                    foreach (var engText in EngTexts)
                    {
                        SendRPC("SetHelpText", playerClient, engText.Key, engText.Value);
                    }
                }
                else
                {
                    foreach (var rusText in RussianTexts)
                    {
                        SendRPC("SetHelpText", playerClient, rusText.Key, rusText.Value);
                    }
                }
            }

            public void SendKitItems()
            {
                UserData userData = Users.Find(playerClient.userID);
                if (userData == null) return;

                SendRPC("ClearKitItems", playerClient);

                foreach (string Kit in RustExtended.Core.Kits.Keys)
                {
                    List<string> KitList = (List<string>)RustExtended.Core.Kits[Kit];
                    string KitRank = KitList.Find(K => K.ToLower().StartsWith("rank")); int Rank;
                    bool KitAvailabled = (String.IsNullOrEmpty(KitRank) || !KitRank.Contains("="));
                    if (!KitAvailabled) { KitRank = KitRank.Split('=')[1].Trim(); KitAvailabled = String.IsNullOrEmpty(KitRank); }
                    if (!KitAvailabled) foreach (string kitRank in KitRank.Split(',')) if (KitAvailabled = (int.TryParse(kitRank, out Rank) && Rank == userData.Rank)) break;
                    if (KitAvailabled)
                    {
                        // Get Expires and Countdown Time of Kit //
                        string KitCD = KitList.Find(K => K.ToLower().StartsWith("countdown"));

                        int KitTimeleft = 0;
                        if (!String.IsNullOrEmpty(KitCD) && KitCD.Contains("=")) int.TryParse(KitCD.Split('=')[1], out KitTimeleft);

                        // Try get user countdown for specified kit
                        Countdown KitCountdown = Users.CountdownList(userData.SteamID).Find(F => F.Command == $"kit.{Kit.ToLower().Trim()}");

                        // Check exists countdown of user for specified kit
                        if (KitCountdown != null)
                        {
                            if (!KitCountdown.Expires && KitTimeleft == -1)
                            {
                                // Если кит одноразовый
                                SendRPC("GetKitItem", playerClient, Kit, TimeSpan.MaxValue);
                                continue;
                            }
                            if (KitCountdown.TimeLeft > -1 && KitTimeleft > -1)
                            {
                                // Если сейчас есть КД на кит
                                SendRPC("GetKitItem", playerClient, Kit, TimeSpan.FromSeconds(KitCountdown.TimeLeft));
                                continue;
                            }
                        }

                        // Если кит доступен
                        SendRPC("GetKitItem", playerClient, Kit, TimeSpan.Zero);
                    }
                }

                SendRPC("FinishReceiving", playerClient);
            }

            private void SendRPC(string rpcName, PlayerClient player, params object[] param)
            {
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, player.netPlayer, param);
            }

            [RPC]
            public void UpdateKitItems()
            {
                SendKitItems();
            }
        }

        class ShopVM : MonoBehaviour
        {
            public PlayerClient playerClient = null;

            private void SendRPC(string rpcName, PlayerClient player, params object[] param)
            {
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, player.netPlayer, param);
            }

            [RPC]
            public void GetBalance()
            {
                var economy = Economy.Get(playerClient.userID);
                if (economy == null) return;

                SendRPC("GetBalance", playerClient, economy.Balance);
            }

            [RPC]
            public void GetAllCategories()
            {
                if (!Shop.Initialized) Shop.Initialize();

                SendRPC("ClearShopItems", playerClient);
                for (int i = 0; i < Shop.GroupCount; i++)
                {
                    string groupName;
                    List<ShopItem> ShopItems = Shop.GetItems(i, out groupName);
                    if (ShopItems == null) continue;

                    foreach (var shopItem in ShopItems)
                    {
                        if (shopItem == null || shopItem.itemData == null) continue;
                        SendRPC("GetShopItem", playerClient, i, shopItem.itemData.uniqueID, shopItem.Index, shopItem.SellPrice);
                    }
                }
            }
        }
    }
}