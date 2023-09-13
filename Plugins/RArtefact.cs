using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustExtended;
using Oxide.Core.Plugins;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Artefact", "Romanchik34 (vk.com/romanchik34)", "1.0.0")]
    [Description("Кто спиздит плагин тому снесу сервер")]
    class RArtefact : RustLegacyPlugin
    {
        [PluginReference]
        Plugin RFacepunchQuests;

        public const string ChatName = "Epic Ivent";

        // Позиции для спавна лута
        public static List<Vector3> SpawnPositions = new List<Vector3>()
        {
            new Vector3(5801.50f, 416.14f, -3278.46f),
            new Vector3(5931.69f, 426.71f, -3138.86f),
            new Vector3(6359.38f, 411.15f, -3113.56f),
            new Vector3(6626.44f, 377.18f, -3028.86f),
            new Vector3(7068.21f, 455.33f, -3364.50f),
            new Vector3(7289.85f, 255.73f, -3373.59f),
			new Vector3(5721.71f, 437.07f, -4401.82f),
			new Vector3(6374.23f, 397.20f, -3838.35f),
			new Vector3(4779.49f, 459.04f, -4141.84f),
			new Vector3(4557.12f, 658.97f, -3476.91f),
			new Vector3(5413.66f, 383.73f, -4335.53f),
			new Vector3(4635.77f, 400.82f, -4922.54f),
			new Vector3(7222.231f, 330.1155f, -3509.197f),
			new Vector3(7134.928f, 391.0293f, -4092.261f),
			new Vector3(6193.805f, 351.9184f, -4614.899f),
			new Vector3(5841.721f, 400.6029f, -4601.967f),
			new Vector3(5447.837f, 351.606f, -4801.274f),
			new Vector3(4891.091f, 516.8986f, -4512.566f),
			new Vector3(4194.432f, 528.5795f, -4069.433f),
			new Vector3(4859.334f, 467.4309f, -3512.853f),
			new Vector3(5206.327f, 528.1317f, -3630.668f),
			new Vector3(5583.995f, 416.883f, -3422.965f),
			new Vector3(6522.032f, 363.6799f, -2371.375f),
			new Vector3(6876.834f, 352.1f, -3856.695f),			
        };

        private List<Vector3> _spawnedPositions = new List<Vector3>();

        /*
         * Armor Part 1 - Карта 1
         * Armor Part 2 - Карта 2
         * Armor Part 3 - Карта 3
         * Armor Part 4 - Карта 4
         * Armor Part 5 - Артефакт 1
         * Armor Part 6 - Артефакт 2
         */

        // Рандомные наборы лута
        public static Dictionary<string, List<RandomItem>> RandomKits = new Dictionary<string, List<RandomItem>>()
        {
            { 
                "Armor Part 1", 
                new List<RandomItem>() 
                {
                    new RandomItem("Low Quality Metal", 50, 100),
                    new RandomItem("Metal Foundation BP", 1, 25),
                    new RandomItem("Metal Pillar BP", 1, 25),
					new RandomItem("Metal Ceiling BP", 1, 25),
                    new RandomItem("Metal Ramp BP", 1, 25),
					new RandomItem("Metal Wall BP", 1, 25),
                    new RandomItem("Metal Doorway BP", 1, 25),
                }
            },
            {
                "Armor Part 2",
                 new List<RandomItem>()
                 {
                     new RandomItem("Armor Part 5", 1, 100),
					 new RandomItem("Sulfur Ore", 100, 10),
					 new RandomItem("Metal Ore", 100, 10),
					 new RandomItem("Wood", 250, 30),
					 new RandomItem("Low Grade Fuel", 75, 10),
					 new RandomItem("Leather", 100, 10),	

                 }
            },
            {
                "Armor Part 3",
                new List<RandomItem>()
                {
                    new RandomItem("P250", 1, 100),
                    new RandomItem("M4", 1, 10),
					new RandomItem("MP5A4", 1, 20),
					new RandomItem("Shotgun", 1, 20),
					new RandomItem("Bolt Action Rifle", 1, 10),
					new RandomItem("556 Ammo", 100, 20),
					new RandomItem("9mm Ammo", 150, 20),
					new RandomItem("Shotgun Shells", 50, 20),
                }
            },
            {
                "Armor Part 4",
                new List<RandomItem>()
                {
                    new RandomItem("F1 Grenade", 3, 100),
					new RandomItem("Explosive Charge", 1, 10),
					new RandomItem("Explosives", 10, 12),
					new RandomItem("Gunpowder", 200, 15),
					new RandomItem("Metal Fragments", 200, 20),
					new RandomItem("Supply Signal", 1, 3),
					
                }   									
            },

        };

        private static Dictionary<ulong, Vector3> OpenedQuests = new Dictionary<ulong, Vector3>();

        #region [Class] RandomKit
        public class RandomKit : IEnumerable
        {
            public RandomKit(List<RandomItem> items)
            {
                this.Items = items;
            }

            public List<RandomItem> Items;

            public IEnumerator GetEnumerator()
            {
                return Items.GetEnumerator();
            }
        }
        #endregion

        #region [Class] RandomItem
        public class RandomItem
        {
            public RandomItem(string itemName, int count, int chance)
            {
                this.ItemName = itemName;
                this.Count = count;
                this.Chance = chance;
            }

            public string ItemName;
            public int Count;
            public int Chance;
        }
        #endregion

        #region [FUNCTION] IsInChance(int chance)
        private bool IsInChance(int chance)
        {
            int random = UnityEngine.Random.Range(0, 101);

            if (chance >= random)
                return true;

            return false;
        }
        #endregion

        #region [HOOK] GetClientMove
        private void OnGetClientMove(HumanController controller, Vector3 origin)
        {
            if (controller == null) return;

            NetUser netUser = controller.netUser;
            if (netUser == null) return;

            foreach (var quest in new Dictionary<ulong, Vector3>(OpenedQuests))
            {
                if (Vector3.Distance(origin, quest.Value) < 15)
                {
                    rust.SendChatMessage(netUser, ChatName, "Поздравляем! Вы нашли сокровище");
                    _spawnedPositions.Remove(quest.Value);

                    // Если нашёл не открыватель карты
                    if (netUser.userID != quest.Key)
                    {
                        NetUser founder = NetUser.FindByUserID(quest.Key);
                        if (founder != null)
                        {
                            rust.SendChatMessage(founder, ChatName, "К сожалению, ваш лут нашёл другой игрок");
                            founder.playerClient.networkView.RPC("DeleteMark", founder.playerClient.netPlayer, quest.Value);
                        }
                    }

                    netUser.playerClient.networkView.RPC("DeleteMark", netUser.playerClient.netPlayer, quest.Value);
                    OpenedQuests.Remove(quest.Key);
                    return;
                }
            }
        }
        #endregion

        #region [HOOK] OnBeltUse()
        [HookMethod("OnBeltUse")]
        public object OnBeltUse(PlayerInventory playerInv, IInventoryItem inventoryItem)
        {
            // Если использование ноутбука
            if (inventoryItem != null && RandomKits.ContainsKey(inventoryItem.datablock.name))
            {
                // Ищем NetUser'а по инвентарю
                NetUser netuser = NetUser.Find(playerInv.networkView.owner);
                if (netuser == null) return null;

                var availablePositions = SpawnPositions.Where(f => _spawnedPositions.All(f2 => f != f2)).ToList();
                if (availablePositions.Count == 0)
                {
                    rust.SendChatMessage(netuser, ChatName, "К сожалению, все точки для кладов заняты. Пожалуйста, подождите.");
                    return null;
                }
                if (OpenedQuests.ContainsKey(netuser.userID))
                {
                    rust.SendChatMessage(netuser, ChatName, "Вы уже имеете действующий квест");
                    return null;
                }

                // Удаляем ноутбук из инвентаря
                Helper.InventoryItemRemove(playerInv, inventoryItem.datablock, 1);

                // Берём рандомную позицию для лута
                Vector3 randomPos = availablePositions[UnityEngine.Random.Range(0, availablePositions.Count)];

                // Спавн лута и очищение ему инвентаря
                GameObject lootable = World.Spawn("WeaponLootBox", randomPos);
                var inv = lootable.GetComponent<LootableObject>()._inventory;
                inv.Clear();
                lootable.GetComponent<LootableObject>().lifeTime = 99999;

                // Берём рандомный кит с лутом из RandomKits
                List<RandomItem> randomKit = RandomKits[inventoryItem.datablock.name];

                // И кладём кит в инвентарь сопли
                foreach (RandomItem item in randomKit)
                {
                    if (IsInChance(item.Chance))
                        inv.AddItemAmount(DatablockDictionary.GetByName(item.ItemName), item.Count);
                }

                // Ставим точку на экране
                netuser.playerClient.networkView.RPC("AddMark", netuser.playerClient.netPlayer, randomPos);

                rust.SendChatMessage(netuser, ChatName, "Вы открыли карту сокровищ и узнали данные о координатах сокровищ!");
                rust.SendChatMessage(netuser, ChatName, "Красной точкой место помечено у вас на карте (английская M)");

                if (RFacepunchQuests != null)
                {
                    RFacepunchQuests.CallHook("OnPlayerUseMap", netuser.userID);
                }

                OpenedQuests.Add(netuser.userID, randomPos);
                return true;
            }

            return null;
        }
        #endregion
    }
}
