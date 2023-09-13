using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using RustExtended;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    class Warps : RustLegacyPlugin
    {
    
        [PluginReference]
		Plugin BRaidBlock;

        private int warpCD = 150;

        private Dictionary<ulong, DateTime> CDUsers = new Dictionary<ulong, DateTime>();

        private WarpManager _warpManager;
        private Dictionary<ulong, Timer> IncomingTimers = new Dictionary<ulong, Timer>();
        private Dictionary<ulong, Vector3> IncomingPositions = new Dictionary<ulong, Vector3>();
        private Dictionary<ulong, NewWarp> IncomingWarps = new Dictionary<ulong, NewWarp>();
        public class WarpManager
        {
            [JsonProperty("Префикс")] public string Prefix;
            [JsonProperty("Список варпов")] public List<NewWarp> AllWarps;
        }

        public class NewWarp
        {
            [JsonProperty("Название файла")] public string Name;
            [JsonProperty("Точки для рандом тп")] public List<string> PositionList;
            [JsonProperty("Цена за тп")] public ulong Price;
            [JsonProperty("Задержка перед тп")] public int Timeout;
        }

        private WarpManager TryDefault()
        {
            var manager = new WarpManager
            {
                Prefix = "Warps",
                AllWarps = new List<NewWarp>
                {
                    new NewWarp
                    {
                        Name = "Big",
                        PositionList = new List<string>
                        {
      "(5119.7, 371.0, -4831.5)",
      "(5175.0, 369.3, -4866.6)",
      "(5269.5, 374.7, -4674.5)",
      "(5128.9, 378.9, -4718.0)",
      "(5228.7, 369.9, -4830.0)"
                        },
                        Price = 100,
                        Timeout = 25
                    },
                    new NewWarp
                    {
                        Name = "Small",
                        PositionList = new List<string>
                        {
                            "(6156, 376.3, -3487.1)",
                            "(6099.3, 377.6, -3470)",
                            "(6051.4, 378, -3623.9)"
                        },
                        Price = 100,
                        Timeout = 25
                    },
                    new NewWarp
                    {
                        Name = "Angar",
                        PositionList = new List<string>
                        {
                            "(6568.6, 367.5, -4306.9)",
                            "(6829.5, 339.1, -4079.7)",
                            "(6907.7, 320.7, -4382.9)",
                            "(6731.6, 348.0, -4280.6)",
                            "(6482.8, 380.7, -4091.9)"
                        },
                        Price = 100,
                        Timeout = 25 //123
                    },
                    new NewWarp
                    {
                        Name = "Medvega",
                        PositionList = new List<string>
                        {
      "(4980.5, 423.0, -4000.9)",
      "(4775.8, 460.5, -4129.6)",
      "(4755.8, 439.3, -3731.5)",
      "(4577.6, 457.6, -3825.3)",
      "(4755.8, 439.3, -3731.5)"
                        },
                        Price = 100,
                        Timeout = 25
                    },
                    new NewWarp
                    {
                        Name = "Hackerka",
                        PositionList = new List<string>
                        {
                            "(5423.84, 429.88, -2421.48)",
                            "(5141.87, 452.86, -3029.79)",
                            "(4897.12, 458.22, -2887.08)",
                        },
                        Price = 100,
                        Timeout = 25
                    }
                }
            };
            return manager;
        }

        [HookMethod("Loaded")]
        internal void Loaded()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("WarpManager"))
                _warpManager = Interface.Oxide.DataFileSystem.ReadObject<WarpManager>("WarpManager");
            else
                Interface.Oxide.DataFileSystem.WriteObject("WarpManager", _warpManager = TryDefault(), true);
        }

        [ChatCommand("t")]
        private void cmd_t(NetUser netuser, string command, string[] args)
        {
            Puts($"({netuser.playerClient.controllable.transform.position.x}, {netuser.playerClient.controllable.transform.position.y}, {netuser.playerClient.controllable.transform.position.z}");
        }

        [ChatCommand("w")]
        private void CmdWarp(NetUser netuser, string command, string[] args)
        {       
            if (BRaidBlock?.CallHook("IsCMDBlocked", netuser.playerClient.lastKnownPosition) is bool && (bool)BRaidBlock?.CallHook("IsCMDBlocked", netuser.playerClient.lastKnownPosition))
            {
				rust.Notice(netuser, $"Нельзя использовать команду \"{command}\" в зоне рейдблока!");
				return;
			}

            if (CDUsers.ContainsKey(netuser.userID))
            {
                if ((CDUsers[netuser.userID] - DateTime.Now) > new TimeSpan(0))
                {
                    TimeSpan timeDiff = CDUsers[netuser.userID] - DateTime.Now;
                    rust.Notice(netuser, $"Вы не можете использовать варпы ещё: {timeDiff.Minutes}:{timeDiff.Seconds}!");
                    return;
                }
                else CDUsers.Remove(netuser.userID);
            }

            if (args.Length == 0)
            {
                Broadcast.Message(netuser, "Доступные варпы:", _warpManager.Prefix);
                _warpManager.AllWarps.ForEach(warp =>
                {
                    Broadcast.Message(netuser,
                        $"[color#FFFFFF]/w [color#FFFFFF] {warp.Name} - [color#FF7433]{warp.Price}{Economy.CurrencySign}",
                        _warpManager.Prefix);
                });
                return;
            }

            var findWarp = _warpManager.AllWarps.Find(warp => warp.Name.ToLower() == args[0]);
            if (findWarp == null)
            {
                Broadcast.Message(netuser, $"Варпа [color#FF7433]{args[0]} не существует", _warpManager.Prefix);
                return;
            }

            if (IncomingWarps.ContainsKey(netuser.userID))
            {
                rust.Notice(netuser, $"Вы уже имеете активный запрос на варп \"{IncomingWarps[netuser.userID].Name}\"!");
                return;
            }

            if (findWarp.Price > 0 && !netuser.admin)
            {
                var economy = Economy.Get(netuser.userID);
                if (economy == null) return;
                if (economy.Balance < findWarp.Price)
                {
                    Broadcast.Message(netuser,
                        $"[color#FFFFFF]Вам не хватает [color#FF7433]{findWarp.Price - economy.Balance}{Economy.CurrencySign} [color##FFFFFF]для телепортации",
                        _warpManager.Prefix);
                    return;
                }

                economy.Balance -= findWarp.Price;
            }

            if (findWarp.Timeout > 0 && !netuser.admin)
            {                     
                Broadcast.Message(netuser, $"[color#FFFFFF]Телепортация на варп [color#FF7433]{args[0].ToLower()} [color#FFFFFF]через [color#FF7433]{findWarp.Timeout} [color#FFFFFF]секунд",
                    _warpManager.Prefix);
                var tmr = timer.Once(float.Parse(findWarp.Timeout.ToString()), () => { TeleportToWarp(findWarp, netuser); });
                IncomingTimers.Add(netuser.userID, tmr);
                IncomingPositions.Add(netuser.userID, netuser.playerClient.lastKnownPosition);
                IncomingWarps.Add(netuser.userID, findWarp);
                return;
            }

            TeleportToWarp(findWarp, netuser);
        }

        private void TeleportToWarp(NewWarp warp, NetUser user)
        {
            if (warp.PositionList == null || warp.PositionList.Count <= 0) return;

            var randomwarp = UnityEngine.Random.Range(0, warp.PositionList.Count);
            if (user == null) return;

            var management = RustServerManagement.Get();
            management.TeleportPlayerToWorld(user.playerClient.netPlayer,
                StringToVector3(warp.PositionList.ElementAt(randomwarp)));
            Broadcast.Message(user, "[color#FFFFFF]Вы успешно телепортированы", _warpManager.Prefix);

            IncomingTimers.Remove(user.userID);
            IncomingPositions.Remove(user.userID);
            IncomingWarps.Remove(user.userID);

            if (!user.admin && Users.GetBySteamID(user.userID).Rank < 40) CDUsers.Add(user.userID, DateTime.Now.Add(new TimeSpan(0, warpCD / 60, warpCD % 60)));

            timer.Once(1f, () =>
            {
                var userData = Users.GetBySteamID(user.userID);
                management.TeleportPlayerToWorld(user.playerClient.netPlayer,
                    StringToVector3(warp.PositionList.ElementAt(randomwarp)));
            });
        }

        public static Vector3 StringToVector3(string sVector)
        {
            if (sVector.StartsWith("(") && sVector.EndsWith(")")) sVector = sVector.Substring(1, sVector.Length - 2);
            var sArray = sVector.Split(',');
            var result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));
            return result;
        }

        void OnGetClientMove(HumanController controller, Vector3 newPos)
        {
            var netUser = controller?.netUser ?? null;
            if (netUser == null || netUser.playerClient == null) return;

            if (IncomingPositions.ContainsKey(netUser.userID))
            {
                if (Vector3.Distance(netUser.playerClient.controllable.character.transform.position, IncomingPositions[netUser.userID]) > 2f)
                {
                    IncomingPositions.Remove(netUser.userID);
                    rust.SendChatMessage(netUser, "Warps", "Вы сдвинулись с места, телепортация прервана!");

                    var tmr = IncomingTimers[netUser.userID];

                    timer.Destroy(ref tmr);
                    IncomingTimers.Remove(netUser.userID);

                    var economy = Economy.Get(netUser.userID);
                    if (economy == null) return;

                    economy.Balance += IncomingWarps[netUser.userID].Price;
                    IncomingWarps.Remove(netUser.userID);
                }
            }
        }
    }
}
