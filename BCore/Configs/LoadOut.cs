using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

internal class LoadOut
{
    public static List<Data> DataList;

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\loadout.cfg"))
        {
            LoadData();
        }
        else
        {
            DataList = new List<Data>
            {
                new()
                {
                    Name = "Default",

                    Ranks = new[] { 0 },

                    BeltItems = new Dictionary<string, int>
                        { { "Stone Hatchet", 1 }, { "Small Medkit", 2 }, { "Torch", 1 } },

                    ArmorItems = new Dictionary<string, int>(),
                    InventoryItems = new Dictionary<string, int>(),
                    NoCrafting = new string[] { "Camp Fire" },
                    Blueprints = new[]
                    {
                        "Wood Storage Box Blueprint",
                        "Wooden Door Blueprint",
                        "Cloth Helmet BP",
                        "Cloth Vest BP",
                        "Cloth Pants BP",
                        "Cloth Boots BP",
                        "Bandage Blueprint",
                        "Wood Pillar BP",
                        "Wood Foundation BP",
                        "Wood Wall BP",
                        "Wood Doorway BP",
                        "Wood Window BP",
                        "Wood Stairs BP",
                        "Wood Ramp BP",
                        "Wood Ceiling BP",
                        "Revolver Blueprint",
                        "9mm Ammo Blueprint",
                        "Gunpowder Blueprint",
                        "Wood Planks Blueprint",
                        "Hatchet Blueprint",
                        "Low Grade Fuel Blueprint",
                        "Workbench Blueprint",
                        "Stone Hatchet Blueprint",
                        "Hunting Bow Blueprint",
                        "Arrow Blueprint",
                        "Furnace Blueprint",
                        "Torch Blueprint",
                        "Low Quality Metal Blueprint",
                        "Handmade Shell Blueprint",
                        "HandCannon Blueprint",
                        "Pipe Shotgun Blueprint",
                        "Spike Wall Blueprint",
                        "Large Spike Wall Blueprint",
                        "Wood Barricade Blueprint",
                        "Wood Gateway Blueprint",
                        "Wood Gate Blueprint",
                        "Metal Door Blueprint",
                        "Metal Window Bars Blueprint",
                        "Bed Blueprint",
                        "Handmade Lockpick Blueprint",
                        "Repair Bench Blueprint"
                    }
                }
            };
            SaveData();
        }
    }

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\loadout.cfg",
            JsonConvert.SerializeObject(DataList, Formatting.Indented));
        Debug.Log("[BCore]: LoadOut Data Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\loadout.cfg");
        DataList = JsonConvert.DeserializeObject<List<Data>>(file);
        Debug.Log("[BCore]: LoadOut Data Loaded!");
    }

    public class Data
    {
        [JsonProperty("Броня")] public Dictionary<string, int> ArmorItems;

        [JsonProperty("Быстрые слоты")] public Dictionary<string, int> BeltItems;

        [JsonProperty("Изучения")] public string[] Blueprints;
        [JsonProperty("Запрещено для крафта")] public string[] NoCrafting;
        [JsonProperty("Инвентарь")] public Dictionary<string, int> InventoryItems;
        [JsonProperty("Название")] public string Name;
        [JsonProperty("Ранги")] public int[] Ranks;
    }
}