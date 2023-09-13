using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

public class Destroy
{
    public static Dictionary<string, string> DestroyResources = new()
    {
        { "RepairBench", "6 Stones, 30 Wood, 25 Metal Fragments, 3 Low Grade Fuel"},
        { "Furnace", "8 Stones, 10 Wood, 5 Low Grade Fuel"},                        // Печка
        { "SleepingBagA", "8 Cloth"}, 							                          // Спальник
        { "SingleBed", "20 Cloth, 50 Metal Fragments"},		                          // Кровать
        { "Wood_Shelter", "25 Wood"},                     		                          // Шелтер
        { "Workbench", "4 Stones,  25 Wood"},                                             // Верстак
        { "Barricade_Fence_Deployable", "15 Wood"},                                        // Баррикада
        { "WoodSpikeWall", "50 Wood" },                                                    // Маленькие колья
        { "LargeWoodSpikeWall", "100 Wood"},                                               // Большие колья
        { "WoodGateway", "200 Wood"}, 							                          // Открывающаяся/закрывающаяся часть деревянных ворот 
        { "WoodGate", "60 Wood"}, 							                              // Деревянные ворота
        { "WoodBoxLarge", "30 Wood"},						                              // Большой ящик
        { "WoodBox", "15 Wood"}, 						                                  // Маленький ящик
        { "SmallStash", "5 Leather"}, 						                              // Маленький мешочек для хранения вещей
        { "WoodenDoor", "15 Wood"}, 						                                  // Деревянная дверь
        { "WoodPillar", "1 Wood Planks"}, 						                          // Деревянный пиллар
        { "WoodFoundation", "4 Wood Planks"}, 				                              // Деревянный фундамент
        { "WoodWindowFrame", "2 Wood Planks"}, 					                          // Деревянный оконный проём
        { "WoodDoorFrame", "2 Wood Planks"},						                          // Деревянный дверной проём
        { "WoodWall", "2 Wood Planks"},						                              // Деревянная стена
        { "WoodCeiling", "3 Wood Planks"}, 						                          // Деревянный потолок
        { "WoodRamp", "3 Wood Planks"}, 						                              // Деревянная рампа 
        { "WoodStairs", "3 Wood Planks"}, 						                          // Деревянная лестница
        { "MetalDoor", "100 Metal Fragments"},				                              // Металлическая дверь
        { "MetalPillar", "1 Low Quality Metal"}, 				                          // Металлический пиллар
        { "MetalFoundation", "4 Low Quality Metal"}, 			                          // Металлический фундамент
        { "MetalWall", "2 Low Quality Metal"},				                              // Металлическая стена
        { "MetalDoorFrame", "2 Low Quality Metal"}, 				                          // Металлический дверной проём
        { "MetalWindowFrame", "2 Low Quality Metal"}, 			                          // Металлический оконный проём
        { "MetalStairs", "3 Low Quality Metal"}, 				                          // Металлическая лестница
        { "MetalRamp", "3 Low Quality Metal"}, 					                          // Металлическая рампа
        { "MetalCeiling", "3 Low Quality Metal"}, 			                              // Металлический потолок 
    };

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\destroy.cfg"))
        {
            LoadData();
        }
        else
        {
            SaveData();
        }
    }
    
    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\destroy.cfg",
            JsonConvert.SerializeObject(DestroyResources, Formatting.Indented));
        Debug.Log("[BCore]: Destroy Config Saved!");
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\destroy.cfg");
        DestroyResources = JsonConvert.DeserializeObject<Dictionary<string, string>>(file);
        Debug.Log("[BCore]: Destroy Config Loaded!");
    }
    
    /*
     { "Workbench", "0, "" } } },


     *
     *
     *
     *
     * 
     */
}