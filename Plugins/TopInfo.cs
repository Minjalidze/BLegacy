using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustExtended;
using static UICamera.Mouse.Button;

namespace Oxide.Plugins
{
	[Info("TopInfo", "XBOCT", "1.3.2")]
	[Description("Top server killer & self stats info")]
	class TopInfo : RustLegacyPlugin
	{
		Dictionary<ulong, float> PlayerTimeOnServer = new Dictionary<ulong, float>();
		Dictionary<ulong, int> PlayerObjRaid = new Dictionary<ulong, int>();
		List<String> colors = new List<String>() { "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]", "[COLOR#FFFFFF]" };
		List<ClanData> topClanList = new List<ClanData>();
		List<TopPlayer> TopFarmList = new List<TopPlayer>();

		[Serializable]
		public class TopPlayer
		{
			[JsonProperty("SteamID")]
			public ulong UserID;
			[JsonProperty("Дерева")]
			public int WoodQuantity;
			[JsonProperty("Метала")]
			public int MetallQuantity;
			[JsonProperty("Серы")]
			public int SulfurQuantity;
			[JsonProperty("Кожы")]
			public int LeatherQuantity;
			[JsonProperty("Ткани")]
			public int ClothQuantity;
			[JsonProperty("Жира")]
			public int AnimalFatQuantity;
		}

		void Loaded()
		{
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("TopFarmer")) TopFarmList = Interface.Oxide.DataFileSystem.ReadObject<List<TopPlayer>>("TopFarmer");
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("TopRaiders")) PlayerObjRaid = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("TopRaiders");
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("TopOnline"))
			{
				PlayerTimeOnServer = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, float>>("TopOnline");
			}
			else
			{
				Interface.Oxide.DataFileSystem.WriteObject("TopOnline", PlayerTimeOnServer, true);
			}
			foreach (var x in rust.GetAllNetUsers()) if (PlayerTimeOnServer.ContainsKey(x.userID) == false) PlayerTimeOnServer.Add(x.userID, 0);
			CallTimer();
		}
		void OnServerSave() 
		{ 
			Interface.Oxide.DataFileSystem.WriteObject("TopOnline", PlayerTimeOnServer, true); 
			Interface.Oxide.DataFileSystem.WriteObject("TopFarmer", TopFarmList, true);
            Interface.Oxide.DataFileSystem.WriteObject("TopRaiders", PlayerObjRaid, true);
        }


        private ulong _attackerId;
        private ulong _victimId;

		[ChatCommand("statvk")]
		private void CMD_StatVK(NetUser user, string cmd, string[] args)
		{
			if (!user.admin) return;

            var Order = TopFarmList.OrderByDescending(pair => pair.SulfurQuantity).ToList();
            var Order1 = PlayerObjRaid.OrderByDescending(pair => pair.Value).ToList();
            var Order2 = PlayerTimeOnServer.OrderByDescending(pair => pair.Value).ToList();
            var Order3 = Users.All.OrderByDescending(pair => Economy.Get(pair.SteamID).AnimalsKilled).ToList();
            var Order4 = Users.All.OrderByDescending(pair => Economy.Get(pair.SteamID).PlayersKilled).ToList();
            var Order5 = Clans.All.OrderByDescending(pair => pair.Experience).ToList();

            var times = TimeSpan.FromMinutes(Order2[0].Value); string msgs = "";
            if (times.TotalDays >= 1) msgs = $"{times.TotalDays:F0}д. {times.Hours:F0}ч. {times.Minutes:D2}м.";
            else if (times.TotalHours >= 1) msgs = $"{times.TotalHours:F0}ч. {times.Minutes:D2}м.";
            else msgs = $"{times.Minutes}м.";

            var message = "[STATS]:" +
				$"\r\nТоп Убийств игроков: {Order4[0].Username} ({Economy.Get(Order4[0].SteamID).PlayersKilled} игроков)" +
                $"\r\nТоп Убийств животных: {Order3[0].Username} ({Economy.Get(Order3[0].SteamID).AnimalsKilled} животных)" +
                $"\r\nТоп Фарма: {Users.Find(Order[0].UserID).Username} ({Order[0].SulfurQuantity} серы)" +
                $"\r\nТоп Кланов: {Order5[0].Name} ({Order5[0].Experience} опыта)" +
				$"\r\nТоп Рейдов: {Users.Find(Order1[0].Key).Username} ({Order1.Count} объектов)" +
				$"\r\nТоп Онлайна: {Users.Find(Order2[0].Key).Username} ({msgs})";
			
			webrequest.EnqueueGet($"https://api.vk.com/method/messages.send?message={message}&group_id=213390559&random_id={UnityEngine.Random.Range(0, 999999)}&peer_id=2000000002&access_token=89b649080717f943e20c54833c819e11794f219ed6c940cf42b1104e3dcfa8da39ad500cdd4a1cddb12ea&v=5.131", (a, b) => { }, this);
        }

        void CallTimer()
		{
			timer.Once(60f, () =>
			{
				foreach (var SPlayer in rust.GetAllNetUsers().ToList())
				{
					if (PlayerTimeOnServer.ContainsKey(SPlayer.userID))
					{
						PlayerTimeOnServer[SPlayer.userID] += 1;
					}
					else
					{
						PlayerTimeOnServer.Add(SPlayer.userID, 1);
					}
				}
				CallTimer();
			});
		}

        void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            try
            {
                if (evt.damageTypes != DamageTypeFlags.damage_explosion) return;
                if (evt.amount < damage.health) return;
                var attacker = evt.attacker.client?.netUser ?? null;
                if (attacker == null) return;

                var victimIsCharacter = (evt.victim.idMain is Character);
                var victimIsObject = !victimIsCharacter;
			
                if (evt.attacker.client) _attackerId = evt.attacker.client.userID;
                if (evt.victim.client)
                    if (evt.victim.client != null)
                        _victimId = evt.victim.client.userID;
                if (evt.attacker.idMain is StructureComponent)
                    _attackerId = ((StructureComponent)evt.attacker.idMain)._master.ownerID;
                if (evt.victim.idMain is StructureComponent)
                    _victimId = ((StructureComponent)evt.victim.idMain)._master.ownerID;
                if (!victimIsObject) return;
			
                if (_victimId == _attackerId) return;
                if (PlayerObjRaid.ContainsKey(_attackerId)) ++PlayerObjRaid[_attackerId];
                else PlayerObjRaid.Add(_attackerId, 1);
            }
            catch
            {
                // ignored
            }
        }


        [ChatCommand("top")]
		void cmdTop(NetUser netuser, string cmd, string[] args)
		{
			if (args.Length == 0)
			{
				Broadcast.Message(netuser, $"[COLOR#FF7433]/{cmd} [COLOR#ffffff]<kill|anim|farm|clan|raid|time> [COLOR#FF7433][число 1 - 15]");
				Broadcast.Message(netuser, "[COLOR#ffffff]kill[COLOR#FF7433] - топ убийц сервера [COLOR#ffffff](по убийствам игроков)");
				Broadcast.Message(netuser, "[COLOR#ffffff]anim[COLOR#FF7433] - топ убийц сервера [COLOR#ffffff](по убийствам животных)");
				Broadcast.Message(netuser, "[COLOR#ffffff]farm[COLOR#FF7433] - топ фармил сервера [COLOR#ffffff](по добыче серы)");
				Broadcast.Message(netuser, "[COLOR#ffffff]clan[COLOR#FF7433] - топ кланов сервера [COLOR#ffffff](по опыту клана)");
				Broadcast.Message(netuser, "[COLOR#ffffff]raid[COLOR#FF7433] - топ рейдеров сервера [COLOR#ffffff](по уничтоженным объектам)");
				Broadcast.Message(netuser, "[COLOR#ffffff]time[COLOR#FF7433] - топ постояльцев сервера [COLOR#ffffff](по проведенному на сервере времени)");
				return;
			}

			if (args.Length > 0 && args.Length < 3)
			{
				int num = 5;
				if (args.Length == 2)
				{
					bool isNum = int.TryParse(args[1], out num);
					if (!isNum || num > 15 || num == 0)
					{
						Broadcast.Message(netuser, "[COLOR#FF7433]Число должно быть [COLOR#ffffff]меньше 15 и больше чем 0");
						return;
					}
				}

				if (args[0].ToLower() == "kill")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]убийц сервера:");
					TopKill(netuser, num, false);
				}
				else if (args[0].ToLower() == "anim")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]убийц животных:");
					TopKill(netuser, num, true);
				}
				else if (args[0].ToLower() == "farm")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]фармил сервера:");
					TopFarm(netuser, num);
				}
				else if (args[0].ToLower() == "clan")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]кланов сервера:");
					TopClan(netuser, num);
				}
				else if (args[0].ToLower() == "raid")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]рейдеров сервера:");
					TopRaid(netuser, num);
				}
				else if (args[0].ToLower() == "time")
				{
					Broadcast.Message(netuser, $"[COLOR#FF7433]Топ {num} [COLOR#FF7433]постояльцев сервера:");
					TopTime(netuser, num);
				}
				else
				{
					Broadcast.Message(netuser, "[COLOR#FF7433]Что то не так ввели, [COLOR#FF7433]попробуйте еще раз!");
				}
				return;
			}
			Broadcast.Message(netuser, "[COLOR#FF7433]Что то не так ввели, [COLOR#FF7433]попробуйте еще раз!");
		}

		void TopKill(NetUser netuser, int Num, bool animal)
		{
			List<UserData> topKillList = new List<UserData>();

			for (int n = 1; n <= Num; n++)
			{
				UserData topPlayer = null;
				bool flag = false;
				float maxKD = 0;

				foreach (UserData userData in Users.All)
				{
					var who = RustExtended.Economy.Get(userData.SteamID);
					float forWhat = animal ? who.AnimalsKilled : who.PlayersKilled;
					if ((!flag || forWhat > maxKD) && !topKillList.Contains(userData))
					{
						flag = true;
						maxKD = forWhat;
						topPlayer = userData;
					}
				}
				topKillList.Add(topPlayer);
				var stats = RustExtended.Economy.Get(topPlayer.SteamID);
				var kills = stats.PlayersKilled; var akills = stats.AnimalsKilled; var mkills = stats.MutantsKilled; var deaths = stats.Deaths;
				if (animal)
					Broadcast.Message(netuser, $"[COLOR#FF7433]{n}. {colors[n - 1]}{topPlayer.Username}[COLOR#ffffff] - {akills} [COLOR#FF7433]животных убил, [COLOR#ffffff]{deaths} [COLOR#FF7433]смертей, [COLOR#ffffff]{kills} [COLOR#FF7433]убийств, [COLOR#ffffff]{mkills} [COLOR#FF7433]мутантов убил");
				else
					Broadcast.Message(netuser, $"[COLOR#FF7433]{n}. {colors[n - 1]}{topPlayer.Username}[COLOR#ffffff] - {kills} [COLOR#FF7433]убийств, [COLOR#ffffff]{deaths} [COLOR#FF7433]смертей, [COLOR#ffffff]{akills} [COLOR#FF7433]животных убил, [COLOR#ffffff]{mkills} [COLOR#FF7433]мутантов убил");
			}
			topKillList.Clear();
		}
		void TopFarm(NetUser netuser, int Num)
		{
			TopFarmList = Interface.Oxide.DataFileSystem.ReadObject<List<TopPlayer>>("TopFarmer");
			if (TopFarmList == null || TopFarmList.Count < 1)
			{
				Broadcast.Message(netuser, "[COLOR#FF7433]Кажись фармил еще нету!");
				return;
			}
			var Order = TopFarmList.OrderByDescending(pair => pair.SulfurQuantity).ToList();
			for (int i = 1; i <= Num; i++)
			{
				if (TopFarmList.ElementAtOrDefault(i) == null) return;
				Broadcast.Message(netuser, $"[COLOR#FF7433]{i}. {colors[i - 1]}{Users.GetUsername(Order[i - 1].UserID)}[COLOR#ffffff] - {Order[i - 1].SulfurQuantity} [COLOR#FF7433]серы, [COLOR#ffffff]{Order[i - 1].MetallQuantity} [COLOR#FF7433]метала, [COLOR#ffffff]{Order[i - 1].WoodQuantity} [COLOR#FF7433]дерева, [COLOR#ffffff]{Order[i - 1].ClothQuantity} [COLOR#FF7433]ткани, [COLOR#ffffff]{Order[i - 1].LeatherQuantity} [COLOR#FF7433]кожи, [COLOR#ffffff]{Order[i - 1].AnimalFatQuantity} [COLOR#FF7433]жира");
			}
		}
		void TopClan(NetUser netuser, int Num)
		{
			if (Num > Clans.Count) Num = Clans.Count;
			ClanData clanPlayer = null;

			for (int n = 1; n <= Num; n++)
			{
				bool flag = false;
				var maxLVL = 0f;

				foreach (ClanData clanData in Clans.All)
				{
					var forWhat = clanData.Experience;
					if ((!flag || forWhat > maxLVL) && !topClanList.Contains(clanData))
					{
						flag = true;
						maxLVL = forWhat;
						clanPlayer = clanData;
					}
				}
				topClanList.Add(clanPlayer);
				var cName = clanPlayer.Name; var cExp = clanPlayer.Experience;
				var cLevel = clanPlayer.Level.Id; var cMembers = clanPlayer.Members.Count; var cLeader = Users.GetBySteamID(clanPlayer.LeaderID).Username;
				Broadcast.Message(netuser, $"[COLOR#FF7433]{n}. {colors[n - 1]}{cName}[COLOR#ffffff] - [COLOR#ffffff]{cExp} [COLOR#FF7433]текущий опыт, [COLOR#ffffff]{cLeader} [COLOR#FF7433]владелец, [COLOR#ffffff]{cLevel} [COLOR#FF7433]уровень, [COLOR#ffffff]{cMembers} [COLOR#FF7433]игроков");
			}
			topClanList.Clear();
		}
		void TopRaid(NetUser netuser, int Num)
		{
			PlayerObjRaid = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("TopRaiders");
			if (PlayerObjRaid == null || PlayerObjRaid.Count < 1)
			{
				Broadcast.Message(netuser, "[COLOR#FF7433]Кажись еще никто не рейдил!");
				return;
			}

			var Order = PlayerObjRaid.OrderByDescending(pair => pair.Value).ToList();//.ToDictionary(pair => pair.Key, pair => pair.Value);
			for (int i = 1; i <= Num; i++)
			{
				Broadcast.Message(netuser, $"[COLOR#FF7433]{i}. {colors[i - 1]}{Users.GetBySteamID(Order[i - 1].Key).Username}[COLOR#ffffff] - за время текущего вайпа уничтожил: [COLOR#00ff00]{Order[i - 1].Value} объектов");
			}
		}
		void TopTime(NetUser netuser, int Num)
		{
			if (PlayerTimeOnServer == null || PlayerTimeOnServer.Count < 1)
			{
				Broadcast.Message(netuser, "[COLOR#FF7433]Кажись онлайна еще нету!");
				return;
			}

			var Order = PlayerTimeOnServer.OrderByDescending(pair => pair.Value).ToList();//.ToDictionary(pair => pair.Key, pair => pair.Value);
			for (int i = 1; i <= Num; i++)
			{
				var times = TimeSpan.FromMinutes(Order[i - 1].Value); string msgs = "";
				if (times.TotalDays >= 1) msgs = $"{times.TotalDays:F0}д. {times.Hours:F0}ч. {times.Minutes:D2}м.";
				else if (times.TotalHours >= 1) msgs = $"{times.TotalHours:F0}ч. {times.Minutes:D2}м.";
				else msgs = $"{times.Minutes}м.";
				Broadcast.Message(netuser, $"[COLOR#FF7433]{i}. {colors[i - 1]}{Users.GetBySteamID(Order[i - 1].Key).Username}[COLOR#ffffff] - всего провел времени на сервере: [COLOR#00ff00]{msgs}");
			}
		}

		private int GetFreeSlot(Inventory inv)
		{
			for (int i = 0; i < inv.slotCount - 4; i++)
			{
				if (inv.IsSlotVacant(i))
				{
					return i;
				}
			}
			return -1;
		}

		private void OnGather(Inventory receiver, ResourceTarget obj, ResourceGivePair item, int collected)
		{
			if (item == null || obj == null || receiver == null || collected < 1 || receiver.networkView.owner == null) return;
			
			NetUser netUser = NetUser.Find(receiver.networkView.owner);
			UserData userData = Users.GetBySteamID(netUser.playerClient.userID);
			
			int slot = GetFreeSlot(receiver);
			if (slot == -1)
			{
				rust.InventoryNotice(netUser, "Нет места в инвентаре!");
				obj.gatherProgress = 0;
				return;
			}
			else
			{
				var profile = TopFarmList.FirstOrDefault(select => select.UserID == netUser.userID);
				if (profile == null)
				{
					profile = new TopPlayer
					{
						UserID = netUser.userID,
						MetallQuantity = 0,
						SulfurQuantity = 0,
						WoodQuantity = 0,
						LeatherQuantity = 0,
						ClothQuantity = 0,
						AnimalFatQuantity = 0
					};

					TopFarmList.Add(profile);
				}

				switch (item.ResourceItemName)
				{
					case "Metal Ore":
						profile.MetallQuantity += collected;
						break;
					case "Sulfur Ore":
						profile.SulfurQuantity += collected;
						break;
					case "Wood":
						profile.WoodQuantity += collected;
						break;
					case "Leather":
						profile.LeatherQuantity += collected;
						break;
					case "Cloth":
						profile.ClothQuantity += collected;
						break;
					case "Animal Fat":
						profile.AnimalFatQuantity += collected;
						break;
					default:
						break;
				}
			}
		}

		[ChatCommand("stat")]
		void cmdStat(NetUser netuser, string cmd, string[] args)
		{
			var userData = Users.GetBySteamID(netuser.userID);
			var stats = RustExtended.Economy.Get(netuser.userID);
			var prank = RustExtended.Core.Ranks[userData.Rank];
			var pclan = "еще не в клане";
			if (userData.Clan != null) pclan = userData.Clan.Name + " с " + userData.Clan.Level.Id + " уровнем";
			Broadcast.Message(netuser, $"[COLOR#FF7433]Ваша статистика на сервере: [COLOR#ffffff]{RustExtended.Core.ServerName}");
			var plkil = stats.PlayersKilled; var death = stats.Deaths; var atkil = stats.AnimalsKilled; var mtkil = stats.MutantsKilled;
			float time = 0; int raids = 0; string servTime = "только зашел"; string raidObj = "еще не рейдил";
			if (PlayerTimeOnServer.TryGetValue(netuser.userID, out time))
			{
				var times = TimeSpan.FromMinutes(time);
				if (times.TotalDays >= 1) servTime = $"{times.TotalDays:F0}д. {times.Hours:F0}ч. {times.Minutes:D2}м.";
				else if (times.TotalHours >= 1) servTime = $"{times.TotalHours:F0}ч. {times.Minutes:D2}м.";
				else servTime = $"{times.Minutes}м.";
			}
			PlayerObjRaid = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("TopRaiders");
			if (PlayerObjRaid.TryGetValue(netuser.userID, out raids)) raidObj = raids + " объектов";

			Broadcast.Message(netuser, $"[COLOR#FF7433]Ваш ранг :[COLOR#ffffff] {prank}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Время проведенное на сервере:[COLOR#ffffff] {servTime}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Состояние в клане:[COLOR#ffffff] {pclan}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Всего объектов уничтожено:[COLOR#ffffff] {raidObj}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Убили игроков:[COLOR#ffffff] {plkil}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Погибли:[COLOR#ffffff] {death}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Животных убито:[COLOR#ffffff] {atkil}");
			Broadcast.Message(netuser, $"[COLOR#FF7433]Мутантов убито:[COLOR#ffffff] {mtkil}");
		}
	}
}