using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using UnityEngine;
using RustExtended;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Ammo", "DiFF1x", "1.0.3")]
    class Ammo : RustLegacyPlugin
    {
        public static string tagChat;
        public static string nameInput;
        public static int QuantityInput;
        public static int setall;

        public static string nameOut1;
        public static int QuantityOut1;

        public static string nameOut2;
        public static int QuantityOut2;
        [ChatCommand("ammo")]
        void AmmoHelp(NetUser netuser, string command, string[] args)
        {
            rust.SendChatMessage(netuser, "Оптовка", "[color#FFFFFF]Команды для обмена : ");
            rust.SendChatMessage(netuser, "Оптовка", "[color#FFFFFF]-[color#FF7433] /pistol [color#FFFFFF]- получите 9mm Ammo");
            rust.SendChatMessage(netuser, "Оптовка", "[color#FFFFFF]-[color#FF7433] /rifle [color#FFFFFF]- получите 556 Ammo");
            rust.SendChatMessage(netuser, "Оптовка", "[color#FFFFFF]- [color#FF7433]/shotgun [color#FFFFFF]- получите Shotgun Shells");
        }
        [ChatCommand("pistol")]
        void Cmd1(NetUser netuser, string command, string[] args)
        {
            string tagChat = "Оптовка";
            string nameInput = "9mm Ammo";
            int QuantityInput = 250;
            int setall = 0;
            string nameOut1 = "Gunpowder";
            int QuantityOut1 = 250;
            string nameOut2 = "Metal Fragments";
            int QuantityOut2 = 250;
            Inventory inventory = netuser.playerClient.controllable.GetComponent<Inventory>();
            ItemDataBlock ItemInput = DatablockDictionary.GetByName(nameInput);
            ItemDataBlock ItemOut1 = DatablockDictionary.GetByName(nameOut1);
            int SoldAmount1 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut1);
            ItemDataBlock ItemOut2 = DatablockDictionary.GetByName(nameOut2);
            int SoldAmount2 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut2);
            if (QuantityOut1 <= SoldAmount1)
            { setall++; }
            if (QuantityOut2 <= SoldAmount2)
            { setall++; }
            if (setall >= 2)
            {
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut1, QuantityOut1);
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut2, QuantityOut2);

                RustExtended.Helper.GiveItem(netuser.playerClient, ItemInput, QuantityInput);
                rust.Notice(netuser, "Вы получили патроны");
            }
            else
            {
                string filename1 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Gunpowder [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut1.ToString(), SoldAmount1.ToString());
                rust.SendChatMessage(netuser, tagChat, filename1);

                string filename2 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Metal Fragments [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut2.ToString(), SoldAmount2.ToString());
                rust.SendChatMessage(netuser, tagChat, filename2);

            }
        }
            [ChatCommand("rifle")]
        void Cmd2(NetUser netuser, string command, string[] args)
        {
            string tagChat = "Оптовка";
            string nameInput = "556 Ammo";
            int QuantityInput = 250;
            int setall = 0;
            string nameOut1 = "Gunpowder";
            int QuantityOut1 = 250;
            string nameOut2 = "Metal Fragments";
            int QuantityOut2 = 500;
            Inventory inventory = netuser.playerClient.controllable.GetComponent<Inventory>();
            ItemDataBlock ItemInput = DatablockDictionary.GetByName(nameInput);
            ItemDataBlock ItemOut1 = DatablockDictionary.GetByName(nameOut1);
            int SoldAmount1 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut1);
            ItemDataBlock ItemOut2 = DatablockDictionary.GetByName(nameOut2);
            int SoldAmount2 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut2);
            if (QuantityOut1 <= SoldAmount1)
            { setall++; }
            if (QuantityOut2 <= SoldAmount2)
            { setall++; }
            if (setall >= 2)
            {
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut1, QuantityOut1);
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut2, QuantityOut2);
                RustExtended.Helper.GiveItem(netuser.playerClient, ItemInput, QuantityInput);
                rust.Notice(netuser, "Вы получили патроны");
            }
            else
            {
                string filename1 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Gunpowder [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut1.ToString(), SoldAmount1.ToString());
                rust.SendChatMessage(netuser, tagChat, filename1);
                string filename2 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Metal Fragments [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut2.ToString(), SoldAmount2.ToString());
                rust.SendChatMessage(netuser, tagChat, filename2);

            }
        }
        [ChatCommand("shotgun")]
        void Cmd3(NetUser netuser, string command, string[] args)
        {
            string tagChat = "Обмен гильз";
            string nameInput = "Shotgun Shells";
            int QuantityInput = 250;
            int setall = 0;
            string nameOut1 = "Gunpowder";
            int QuantityOut1 = 250;
            string nameOut2 = "Metal Fragments";
            int QuantityOut2 = 250;
            Inventory inventory = netuser.playerClient.controllable.GetComponent<Inventory>();
            ItemDataBlock ItemInput = DatablockDictionary.GetByName(nameInput);
            ItemDataBlock ItemOut1 = DatablockDictionary.GetByName(nameOut1);
            int SoldAmount1 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut1);
            ItemDataBlock ItemOut2 = DatablockDictionary.GetByName(nameOut2);
            int SoldAmount2 = RustExtended.Helper.InventoryItemCount(inventory, ItemOut2);
            if (QuantityOut1 <= SoldAmount1)
            { setall++; }
            if (QuantityOut2 <= SoldAmount2)
            { setall++; }
            if (setall >= 2)
            {
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut1, QuantityOut1);
                RustExtended.Helper.InventoryItemRemove(inventory, ItemOut2, QuantityOut2);
                RustExtended.Helper.GiveItem(netuser.playerClient, ItemInput, QuantityInput);
                rust.Notice(netuser, "Вы получили патроны");
            }
            else
            {
                string filename1 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Gunpowder [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut1.ToString(), SoldAmount1.ToString());
                rust.SendChatMessage(netuser, tagChat, filename1);
                string filename2 = string.Format("[color#FFFFFF]Нужно [color#1E90FF]Metal Fragments [color#FFFFFF]в количестве [color#1E90FF]{0}  [color#FFFFFF]у вас есть [color#1E90FF]{1}", QuantityOut2.ToString(), SoldAmount2.ToString());
                rust.SendChatMessage(netuser, tagChat, filename2);
            }
        }
    }
}


