using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using RustExtended;

namespace Oxide.Plugins
{
    public class InfoViewer : RustLegacyPlugin
    {
        Dictionary<ulong, float> PlayerTimeOnServer = new Dictionary<ulong, float>();
        Dictionary<ulong, int> PlayerObjRaid = new Dictionary<ulong, int>();
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
        
        private void Loaded()
        {
            StartTimer();
        }

        private void ReadData()
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
        }

        private void StartTimer()
        {
            timer.Repeat(300, 0, () =>
            {
                ReadData();
                
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
            });
        }
    }
}