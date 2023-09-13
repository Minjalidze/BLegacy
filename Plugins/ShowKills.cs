using System;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
using RustExtended;
using Oxide.Plugins;

namespace Oxide.Plugins
{
    [Info("ShowKills", "Sh1ne", "1.0.0")]
    class ShowKills : RustLegacyPlugin
    {
        static Dictionary<ulong, KillsVM> playersWithPlugin = new Dictionary<ulong, KillsVM>();

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
        
        void AddPluginToPlayer(PlayerClient pc)
        {
            if (pc.gameObject.GetComponent<KillsVM>() == null)
            {
                if (playersWithPlugin.ContainsKey(pc.userID)) playersWithPlugin.Remove(pc.userID);

                var killsVm = pc.gameObject.AddComponent<KillsVM>();
                killsVm.playerClient = pc;

                playersWithPlugin.Add(pc.userID, killsVm);
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
            foreach (var steamId in playersWithPlugin.Keys)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(steamId, out pclient);
                if (pclient == null || pclient.netPlayer == networkPlayer)
                {
                    playersWithPlugin.Remove(steamId);
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
            foreach (var _steamID in playersWithPlugin.Keys)
            {
                PlayerClient pclient;
                PlayerClient.FindByUserID(_steamID, out pclient);
                if (pclient != null)
                {
                    UnloadPlugin(pclient.gameObject, typeof(KillsVM));
                }
            }
        }

        void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            if (evt.amount < damage.health) return;
            if (playersWithPlugin == null || playersWithPlugin.Count == 0) return;

            if (damage is HumanBodyTakeDamage)
            {
                NetUser victim = evt.victim.client?.netUser ?? null;
                if (victim == null) return;

                KillWeaponType weaponType = KillWeaponType.Other;
                string killerName = victim.displayName;

                NetUser killer = evt.attacker.client?.netUser ?? null;                
                if (killer != null)
                {
                    WeaponImpact impact = evt.extraData as WeaponImpact;
                    if (impact != null)
                    {
                        switch (impact.dataBlock.name)
                        {
                            case "Hatchet":
                            case "Pick Axe":
                            case "HandCannon":
                                weaponType = KillWeaponType.Melee;
                                break;
                            case "P250":
                            case "9mm Pistol":                            
                                weaponType = KillWeaponType.Pistol;
                                break;
                            case "M4":
                            case "MP5A4":
                            case "Shotgun":
                            case "Pipe Shotgun":
                                weaponType = KillWeaponType.BulletWeapon;
                                break;
                        }
                    }

                    killerName = killer.displayName;
                }
                else
                {
                    return;
                }

                if (evt.damageTypes == DamageTypeFlags.damage_explosion)
                {
                    weaponType = KillWeaponType.Explosion;
                }

                foreach (var pair in playersWithPlugin)
                {
                    if (pair.Value != null && pair.Value.isInited)
                    {
                        pair.Value.SendKillData(killerName, victim.displayName, weaponType);
                    }
                }
            }
        }

        public enum KillWeaponType
        {
            Melee,
            Pistol,
            BulletWeapon,
            Explosion,
            Other
        }

        class KillsVM : MonoBehaviour
        {
            public PlayerClient playerClient = null;
            new Facepunch.NetworkView networkView = null;

            public bool isInited = false;

            void Start()
            {
                networkView = GetComponent<Facepunch.NetworkView>();
            }

            [RPC]
            public void InitKVM()
            {
                isInited = true;
            }

            public void SendKillData(string killer, string victim, KillWeaponType weaponType)
            {
                SendRPC("GetKillData", playerClient, killer, victim, (int)weaponType);
            }

            public void SendRPC(string rpcName, PlayerClient player, params object[] param)
            {
                networkView.RPC(rpcName, player.netPlayer, param);
            }
        }
    }
}