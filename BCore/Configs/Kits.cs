using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

public class Kits
{
    public static List<Kit> KitData;

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\kits.cfg"))
        {
            LoadData();
        }
        else
        {
            KitData = new List<Kit>
            {
                new()
                {
                    KitName = "start",
                    Cooldown = 300,
                    Ranks = new[] { 0, 90 },
                    Items = new Dictionary<string, int> { { "Uber Hatchet", 1 } }
                }
            };
            SaveData();
        }
    }

    public static Kit GetKit(string name) => KitData.Find(f => string.Equals(f.KitName, name, StringComparison.CurrentCultureIgnoreCase));
    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\kits.cfg", JsonConvert.SerializeObject(KitData, Formatting.Indented));
        Debug.Log("[BCore]: Kits Data Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\kits.cfg");
        KitData = JsonConvert.DeserializeObject<List<Kit>>(file);
        Debug.Log("[BCore]: Kits Data Loaded!");
    }

    public class Kit
    {
        public int Cooldown;
        public Dictionary<string, int> Items;
        public string KitName;
        public int[] Ranks;
    }
}