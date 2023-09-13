using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

public class Ranks
{
    public static List<Rank> RankList;

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\ranks.cfg"))
        {
            LoadData();
        }
        else
        {
            RankList = new List<Rank>
            {
                new()
                {
                    Number = 0,
                    Name = ""
                }
            };
            File.WriteAllText(@"serverdata\cfg\BCore\ranks.cfg",
                JsonConvert.SerializeObject(RankList, Formatting.Indented));
        }
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\ranks.cfg");
        RankList = JsonConvert.DeserializeObject<List<Rank>>(file);
        Debug.Log("[BCore]: Ranks Config Loaded!");
    }

    public class Rank
    {
        public string Name;
        public int Number;
    }
}