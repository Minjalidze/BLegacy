// Reference: Facepunch.ID
using System.Collections.Generic;
using RustExtended;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
	[Info("ShowDamage", "Sh1ne", "1.0.0")]
	[Description("Shows damage as given by player")]
	public class ShowDamage : RustLegacyPlugin
	{
        private const string PluginDataName = "ShowDamageData";

        private const string color = "[COLOR#FFFFFF]";            
        private const string UNKNOWN = "Unknown";
        public Dictionary<ulong, bool> DamageUsers = new Dictionary<ulong, bool>();

        void Loaded()
        {
            DamageUsers = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>(PluginDataName);
        }

        void SavePluginData()
        {
            Interface.GetMod().DataFileSystem.WriteObject(PluginDataName, DamageUsers);
        }

        void OnHurt(TakeDamage takeDamage, DamageEvent damage)
		{
			if (damage.attacker.client == null) return;
            if (damage.attacker.client == damage.victim.client) return;

            NetUser attacker = damage.attacker.client?.netUser;
            if (attacker == null) return;

            bool showdmg = true;

            if (DamageUsers.ContainsKey(attacker.userID))
                showdmg = DamageUsers[attacker.userID];

            if (!showdmg) return;

			WeaponImpact impact = damage.extraData as WeaponImpact;
			string weapon = impact?.dataBlock.name ?? UNKNOWN;
			
			double dmg = Math.Floor(damage.amount);
			if (dmg == 0) return;
			
			string weaponm = "";			
			if (weapon != UNKNOWN) weaponm = string.Format("с {0}", weapon);
			
			PlayerInventory inv = attacker.playerClient.controllable.GetComponent<PlayerInventory>();
            if (inv != null && (inv.activeItem?.datablock?.name?.Contains("Shotgun") ?? false)) return;
			
			rust.InventoryNotice(attacker, $"{dmg} урона");
		}

        [ChatCommand("dmg")]
        void cmdDmg(NetUser netUser, string command, string[] args)
        {
            string text = $"Command [{netUser.displayName}:{netUser.userID}] /" + command;
            foreach (string s in args) text += " " + s;
            Helper.LogChat(text, true);

            if (DamageUsers.ContainsKey(netUser.userID))
            {
                bool dmg = !DamageUsers[netUser.userID];
                DamageUsers[netUser.userID] = dmg;
                if (dmg)
                {
                    rust.SendChatMessage(netUser, "Показ урона", $"{color}Теперь у вас отображается урон, который вы наносите");
                }
                else
                {
                    rust.SendChatMessage(netUser, "Показ урона", $"{color}Теперь у вас не отображается урон, который вы наносите");
                }
            }
            else
            {   
                DamageUsers.Add(netUser.userID, false);
                rust.SendChatMessage(netUser, "Показ урона", $"{color}Теперь у вас не отображается урон, который вы наносите");
            }
            SavePluginData();
        }
    }
}