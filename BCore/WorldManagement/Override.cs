using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BCore.Configs;
using BCore.Users;
using UnityEngine;
using String = Facepunch.Utility.String;

namespace BCore.WorldManagement;

public static class Override
{
    private static string _overridePath = "";

    private static List<string> _lootKeys = new();
    private static string LootsFile { get; set; }
    private static string ItemsFile { get; set; }
    public static int LootsCount { get; private set; }
    public static bool LootsInitialized { get; private set; }
    public static bool LootsFileCreated { get; private set; }
    public static int ItemsCount { get; private set; }
    public static bool ItemsInitialized { get; private set; }
    public static bool ItemsFileCreated { get; private set; }

    public static void Initialize()
    {
        _overridePath = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "serverdata"), "cfg\\RustOverride");
        if (!Directory.Exists(_overridePath)) Directory.CreateDirectory(_overridePath);
        LootsFile = Path.Combine(_overridePath, "LootsList.cfg");
        ItemsFile = Path.Combine(_overridePath, "ItemsList.cfg");

        LootsInitialized = false;
        LootsFileCreated = false;
        LootsCount = 0;
        ItemsInitialized = false;
        ItemsFileCreated = false;
        ItemsCount = 0;

        _lootKeys = DatablockDictionary._lootSpawnLists.Keys.ToList();
        if (!File.Exists(LootsFile)) LootsFileCreated = LootSaveFile();
        else if (Config._Settings.OverrideLoots) LootsInitialized = LootOverride();
        LootsCount = DatablockDictionary._lootSpawnLists.Count;

        if (!File.Exists(ItemsFile)) ItemsFileCreated = ItemSaveFile();
        else if (Config._Settings.OverrideItems) ItemsInitialized = ItemOverride();

        var itemsListFile = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "serverdata"),
            "cfg\\rust_items.txt");
        if (File.Exists(itemsListFile)) return;
        var itemDataList =
            DatablockDictionary.All.Aggregate("", (current, itemData) => current + itemData.name + Environment.NewLine);
        File.WriteAllText(itemsListFile, itemDataList);
    }
    public static void ApplyDamageTypeList(TakeDamage takeDamage, ref DamageEvent damage, DamageTypeList damageType)
    {
        ProtectionTakeDamage protectionTakeDamage = takeDamage as ProtectionTakeDamage;
        string str = damage.victim.idMain.name.Replace("(Clone)", "");
        for (int i = 0; i < 6; i++)
        {
            DamageTypeFlags damageTypeFlags = (DamageTypeFlags)(1 << i);
            float num = (protectionTakeDamage != null) ? protectionTakeDamage.GetArmorValue(i) : 0f;
            string key = str + "." + damageTypeFlags.ToString().Replace("damage_", "");
            if (damage.victim.idMain is not Character)
            {
	            if (Configs.Override.OverrideArmor.ContainsKey(key))
					num = Configs.Override.OverrideArmor[key];
	            else
	            {
		            num = 0;
	            }
            }
            if (num > 0f && damageType[i] > 0f)
            {
                int index = i;
                damageType[index] *= Mathf.Clamp01(1f - num / 200f);
            }
            if (!Mathf.Approximately(damageType[i], 0f))
            {
                damage.damageTypes |= damageTypeFlags;
                damage.amount += damageType[i];
            }
        }
    }
        public static bool DamageOverride(TakeDamage take, ref DamageEvent damage, ref TakeDamage.Quantity quantity)
        {
            if (damage.attacker.idMain == damage.victim.idMain)
			{
				return true;
			}
			if (!Config._Settings.OverrideDamage || float.IsInfinity(damage.amount))
			{
				return true;
			}
			if (damage.victim.id.GetComponent<Character>() == null && damage.attacker.client != null)
			{
				ulong num = damage.victim.client ? damage.victim.client.userID : 0UL;
				DeployableObject deployableObject;
				if ((deployableObject = (damage.victim.idMain as DeployableObject)) != null)
				{
					num = deployableObject.ownerID;
				}
				StructureComponent structureComponent;
				if ((structureComponent = (damage.victim.idMain as StructureComponent)) != null)
				{
					num = structureComponent._master.ownerID;
				}
				ulong num2 = damage.attacker.client ? damage.attacker.client.userID : 0UL;
				DeployableObject deployableObject2;
				if ((deployableObject2 = (damage.attacker.idMain as DeployableObject)) != null)
				{
					num2 = deployableObject2.ownerID;
				}
				StructureComponent structureComponent2;
				if ((structureComponent2 = (damage.attacker.idMain as StructureComponent)) != null)
				{
					num2 = structureComponent2._master.ownerID;
				}
				if ((num == num2 || Data.FindUser(num).HasShared(num2)) && Config.DestoryOwnership.ContainsKey(damage.attacker.client.userID))
				{
					damage.amount =
						(int)Configs.Override.OverrideDamage[damage.attacker.idMain.name.Replace("(Clone)", "") + ".DAMAGE"];
					return true;
				}
			}
			bool flag = true;
			if (damage.attacker.client && damage.attacker.idMain is Character)
			{
				WeaponImpact weaponImpact = damage.extraData as WeaponImpact;
				string text = (weaponImpact != null) ? weaponImpact.dataBlock.name : "Hunting Bow";
				string text2 = text.Replace(" ", "") + ".DAMAGE";
				string key = text2 + "." + damage.victim.idMain.name.Replace("(Clone)", "");
				string text3 = text2 + ".HEADSHOT";
				flag = (bool)Configs.Override.OverrideDamage[key];
				if (!flag)
				{
					return false;
				}
				float[] array2;
				if (weaponImpact == null)
				{
					float[] array = new float[2];
					array[0] = 75f;
					array2 = array;
					array[1] = 75f;
				}
				else
				{
					float[] array3 = new float[2];
					array3[0] = weaponImpact.dataBlock.damageMin;
					array2 = array3;
					array3[1] = weaponImpact.dataBlock.damageMax;
				}
				float[] array4 = array2;
				damage.amount = damage.bodyPart == BodyPart.Head
					? (int)Configs.Override.OverrideDamage[text3]
					: (int)Configs.Override.OverrideDamage[text2];
				if (weaponImpact != null && damage.extraData is BulletWeaponImpact)
				{
					quantity = new DamageTypeList(0f, damage.amount, 0f, 0f, 0f, 0f);
				}
				else
				{
					quantity = new DamageTypeList(0f, 0f, damage.amount, 0f, 0f, 0f);
				}
				damage.amount = 0f;
				if (quantity.Unit == TakeDamage.Unit.List)
				{
					Override.ApplyDamageTypeList(take, ref damage, quantity.DamageTypeList);
				}
				Helper.Log(string.Concat(new object[]
				{
					"Damage: ",
					damage.attacker.idMain,
					"[",
					damage.attacker.networkViewID,
					"] from ",
					text,
					" hit ",
					damage.victim.idMain,
					"[",
					damage.victim.networkViewID,
					"] on ",
					damage.amount,
					"(",
					array4[0],
					"-",
					array4[1],
					") pts."
				}), false);
			}
			else if (!(damage.attacker.idMain is Character))
			{
				float num3 = 0f;
				float num4 = 0f;
				TimedGrenade timedGrenade;
				if ((timedGrenade = (damage.attacker.id as TimedGrenade)) != null)
				{
					num3 = timedGrenade.damage;
					num4 = timedGrenade.explosionRadius;
				}
				TimedExplosive timedExplosive;
				if ((timedExplosive = (damage.attacker.id as TimedExplosive)) != null)
				{
					num3 = timedExplosive.damage;
					num4 = timedExplosive.explosionRadius;
				}
				SpikeWall spikeWall;
				if ((spikeWall = (damage.attacker.id as SpikeWall)) != null)
				{
					num3 = spikeWall.baseReturnDmg;
					num4 = 0f;
				}
				if (num3 > 0f)
				{
					string text4 = damage.attacker.idMain.name.Replace("(Clone)", "") + ".DAMAGE";
					string key2 = text4 + "." + damage.victim.idMain.name.Replace("(Clone)", "");
					flag = (bool)Configs.Override.OverrideDamage[key2];
					if (!flag)
					{
						return false;
					}

					num3 = (int)Configs.Override.OverrideDamage[text4];
					if (num4 > 0f)
					{
						Vector3 center = damage.attacker.idMain.collider.bounds.center;
						ExplosionHelper.Point point = new ExplosionHelper.Point(center, num4, 271975425, -1, null);
						ExplosionHelper.Surface[] array5 = point.ToArray();
						int i = 0;
						while (i < array5.Length)
						{
							ExplosionHelper.Surface surface = array5[i];
							if (surface.idMain == damage.victim.idMain)
							{
								damage.amount = (1f - Mathf.Clamp01(surface.work.distanceToCenter / num4)) * num3;
								if (surface.blocked)
								{
									damage.amount *= 0.1f;
									break;
								}
								break;
							}
							else
							{
								i++;
							}
						}
					}
					quantity = ((damage.attacker.id is SpikeWall) ? new DamageTypeList(0f, 0f, damage.amount, 0f, 0f, 0f) : new DamageTypeList(0f, 0f, 0f, damage.amount, 0f, 0f));
					damage.amount = 0f;
					if (quantity.Unit == TakeDamage.Unit.List)
					{
						ApplyDamageTypeList(take, ref damage, quantity.DamageTypeList);
					}
					Helper.Log(string.Concat(new object[]
					{
						"Damage: ",
						damage.attacker.idMain,
						"[",
						damage.attacker.networkViewID,
						"] owned ",
						damage.attacker.client,
						" hit ",
						damage.victim.idMain,
						"[",
						damage.victim.networkViewID,
						"] on ",
						damage.amount,
						"(",
						num3,
						") pts."
					}), false);
				}
			}
			return true;
        }
    private static bool LootSaveFile()
    {
        using var data = File.CreateText(LootsFile);
        foreach (var spawn in _lootKeys.Select(elem => DatablockDictionary._lootSpawnLists[elem]))
        {
            data.WriteLine("[" + spawn.name + "]");
            data.WriteLine("PackagesToSpawn=" + spawn.minPackagesToSpawn + "," + spawn.maxPackagesToSpawn);
            data.WriteLine("SpawnOneOfEach=" + spawn.spawnOneOfEach);
            data.WriteLine("NoDuplicates=" + spawn.noDuplicates);
            data.WriteLine("// Type   Weight	List/Item		Min	Max");
            foreach (var weightEntry in spawn.LootPackages)
            {
                if (weightEntry.obj == null) continue;
                data.Write(weightEntry.obj is ItemDataBlock ? "PackageItem=" : "PackageList=");
                data.Write(weightEntry.weight + "\t");
                data.Write(weightEntry.obj.name + new string('\t', 4 - weightEntry.obj.name.Length / 8));
                data.Write(weightEntry.amountMin + "\t" + weightEntry.amountMax);
                data.WriteLine();
            }

            data.WriteLine();
        }

        return true;
    }

    private static bool LootOverride()
    {
        var @override = File.ReadAllLines(LootsFile).ToList();
        if (!@override.Exists(s => s.Contains("[AILootList]")))
        {
            ConsoleSystem.PrintError("ERROR: Spawn list for \"AILootList\" not found in \"lootslist.cfg\".");
            return false;
        }

        if (!@override.Exists(s => s.Contains("[AmmoSpawnList]")))
        {
            ConsoleSystem.PrintError("ERROR: Spawn list for \"AmmoSpawnList\" not found in \"lootslist.cfg\".");
            return false;
        }

        if (!@override.Exists(s => s.Contains("[JunkSpawnList]")))
        {
            ConsoleSystem.PrintError("ERROR: Spawn list for \"JunkSpawnList\" not found in \"lootslist.cfg\".");
            return false;
        }

        if (!@override.Exists(s => s.Contains("[MedicalSpawnList]")))
        {
            ConsoleSystem.PrintError("ERROR: Spawn list for \"MedicalSpawnList\" not found in \"lootslist.cfg\".");
            return false;
        }

        if (!@override.Exists(s => s.Contains("[WeaponSpawnList]")))
        {
            ConsoleSystem.PrintError("ERROR: Spawn list for \"WeaponSpawnList\" not found in \"lootslist.cfg\".");
            return false;
        }

        if (!@override.Exists(s => s.Contains("[SupplyDropSpawnListMaster]")))
        {
            ConsoleSystem.PrintError(
                "ERROR: Spawn list for \"SupplyDropSpawnListMaster\" not found in \"lootslist.cfg\".");
            return false;
        }

        DatablockDictionary._lootSpawnLists.Clear();

        var cacheSpawns = new Dictionary<string, LootSpawnList>();
        foreach (var var in @override.Select(data => data.Trim())
                     .Where(var => !string.IsNullOrEmpty(var) && !var.StartsWith("//")))
        {
            var trim = var;
            if (var.Contains("//")) trim = var.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (string.IsNullOrEmpty(trim)) continue;

            if (!trim.StartsWith("[") || !trim.EndsWith("]")) continue;
            trim = trim.Substring(1, trim.Length - 2);
            cacheSpawns[trim] = ScriptableObject.CreateInstance<LootSpawnList>();
        }

        LootSpawnList newSpawnList = null;
        List<LootSpawnList.LootWeightedEntry> newWeightedList = null;

        foreach (var var in @override.Select(data => data.Trim())
                     .Where(var => !string.IsNullOrEmpty(var) && !var.StartsWith("//")))
        {
            var trim = var;
            if (var.Contains("//")) trim = var.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (string.IsNullOrEmpty(trim)) continue;

            if (trim.StartsWith("[") && trim.EndsWith("]"))
            {
                var lootSpawnName = trim.Substring(1, trim.Length - 2);
                newSpawnList = cacheSpawns[lootSpawnName];
                newSpawnList.name = lootSpawnName;
                DatablockDictionary._lootSpawnLists.Add(newSpawnList.name, newSpawnList);
                newWeightedList = new List<LootSpawnList.LootWeightedEntry>();
                continue;
            }

            if (trim.Contains("=") && newSpawnList != null)
            {
                var vars = trim.Split('=');
                if (vars.Length < 2) continue;
                LootSpawnList.LootWeightedEntry newWeightedEntry;
                switch (vars[0].ToUpper())
                {
                    case "PACKAGESTOSPAWN":
                        vars = vars[1].Contains(",") ? vars[1].Split(',') : new[] { vars[1], vars[1] };
                        int.TryParse(vars[0], out newSpawnList.minPackagesToSpawn);
                        int.TryParse(vars[1], out newSpawnList.maxPackagesToSpawn);
                        break;
                    case "SPAWNONEOFEACH":
                        bool.TryParse(vars[1], out newSpawnList.spawnOneOfEach);
                        break;
                    case "NODUPLICATES":
                        bool.TryParse(vars[1], out newSpawnList.noDuplicates);
                        break;
                    case "PACKAGELIST":
                        vars = vars[1].Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        newWeightedEntry = new LootSpawnList.LootWeightedEntry();
                        if (!cacheSpawns.ContainsKey(vars[1]))
                        {
                            ConsoleSystem.LogError(
                                $"Package {newSpawnList.name} has a reference to an spawn list named {vars[1]}, but it not exist.");
                            continue;
                        }

                        newWeightedEntry.obj = cacheSpawns[vars[1]];
                        float.TryParse(vars[0], out newWeightedEntry.weight);
                        int.TryParse(vars[2], out newWeightedEntry.amountMin);
                        int.TryParse(vars[3], out newWeightedEntry.amountMax);
                        newWeightedList.Add(newWeightedEntry);
                        newSpawnList.LootPackages = newWeightedList.ToArray();
                        break;
                    case "PACKAGEITEM":
                        vars = vars[1].Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        newWeightedEntry = new LootSpawnList.LootWeightedEntry
                        {
                            obj = DatablockDictionary.GetByName(vars[1])
                        };
                        if (newWeightedEntry.obj == null)
                        {
                            ConsoleSystem.LogError(
                                $"Package {newSpawnList.name} has a reference to an item named {vars[1]}, but it not exist.");
                            continue;
                        }

                        float.TryParse(vars[0], out newWeightedEntry.weight);
                        int.TryParse(vars[2], out newWeightedEntry.amountMin);
                        int.TryParse(vars[3], out newWeightedEntry.amountMax);
                        newWeightedList.Add(newWeightedEntry);
                        newSpawnList.LootPackages = newWeightedList.ToArray();
                        break;
                }
            }
        }

        return true;
    }

    private static bool ItemSaveFile()
    {
        using var data = File.CreateText(ItemsFile);
        data.WriteLine("// - Help of item properties -");
        data.WriteLine(
            "// SlotFlags = Flags [Belt|Chest|Cooked|Debris|Equip|Feet|FuelBasic|Head|Legs|Raw|Safe|Storage]");
        data.WriteLine("// TransientMode = Flags [None|Untransferable|DoesNotSave|Full]");
        data.WriteLine("// Changed properties change only on server side. But it work.");
        data.WriteLine("");
        foreach (var item in DatablockDictionary.All)
        {
            data.WriteLine("[" + item.name + "]");
            //DATA.WriteLine("Category=" + Item.category);
            data.WriteLine("Description=" + item.GetItemDescription());
            //DATA.WriteLine("SlotFlags=" + Item._itemFlags);
            data.WriteLine("IsRepairable=" + item.isRepairable);
            data.WriteLine("IsRecycleable=" + item.isRecycleable);
            data.WriteLine("IsResearchable=" + item.isResearchable);
            data.WriteLine("IsSplittable=" + item._splittable);
            data.WriteLine("TransientMode=" + item.transientMode);
            data.WriteLine("LoseDurability=" + item.doesLoseCondition);
            data.WriteLine("MaxDurability=" + item._maxCondition);
            if (FindBlueprintForItem(item, out var blueprint))
            {
                var ingredients = blueprint.ingredients.Aggregate("",
                    (current, entry) => current + entry.amount + " \"" + entry.Ingredient.name + "\", ");
                if (ingredients.Length > 0) ingredients = ingredients.Substring(0, ingredients.Length - 2);
                data.WriteLine("Crafting.Ingredients=" + ingredients);
                data.WriteLine("Crafting.RequireWorkbench=" + blueprint.RequireWorkbench);
                data.WriteLine("Crafting.Duration=" + blueprint.craftingDuration);
                data.WriteLine("Crafting.Amount=" + blueprint.numResultItem);
            }

            data.WriteLine("MinUses=" + item._minUsesForDisplay);
            data.WriteLine("MaxUses=" + item._maxUses);
            data.WriteLine();
        }

        return true;
    }

    private static bool ItemOverride()
    {
        ItemDataBlock item = null;
        var @override = File.ReadAllLines(ItemsFile).ToList();
        foreach (var var in @override.Select(data => data.Trim())
                     .Where(var => !string.IsNullOrEmpty(var) && !var.StartsWith("//")))
        {
            var trim = var;
            if (var.Contains("//")) trim = var.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            if (string.IsNullOrEmpty(trim)) continue;

            if (var.StartsWith("[") && var.EndsWith("]"))
            {
                var itemName = var.Substring(1, trim.Length - 2);
                item = DatablockDictionary.GetByName(itemName);
                if (item != null) ItemsCount++;
                else
                    ConsoleSystem.LogError(
                        $"Item named of {itemName} not exist in dictionary.");
                continue;
            }

            if (var.Contains("=") && item != null)
            {
                var vars = var.Split('=');
                if (vars.Length < 2) continue;
                BlueprintDataBlock blueprint;
                switch (vars[0].ToUpper())
                {
                    case "DESCRIPTION":
                        item.itemDescriptionOverride = vars[1];
                        break;
                    case "ISREPAIRABLE":
                        item.isRepairable = bool.Parse(vars[1]);
                        break;
                    case "ISRECYCLEABLE":
                        item.isRecycleable = bool.Parse(vars[1]);
                        break;
                    case "ISRESEARCHABLE":
                        item.isResearchable = bool.Parse(vars[1]);
                        break;
                    case "ISSPLITTABLE":
                        item._splittable = bool.Parse(vars[1]);
                        break;
                    case "TRANSIENTMODE":
                        if (vars[1].IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
                            item.transientMode = ItemDataBlock.TransientMode.Full;
                        if (vars[1].IndexOf("doesnotsave", StringComparison.OrdinalIgnoreCase) >= 0)
                            item.transientMode = ItemDataBlock.TransientMode.DoesNotSave;
                        if (vars[1].IndexOf("untransferable", StringComparison.OrdinalIgnoreCase) >= 0)
                            item.transientMode = ItemDataBlock.TransientMode.Untransferable;
                        if (vars[1].IndexOf("none", StringComparison.OrdinalIgnoreCase) >= 0)
                            item.transientMode = ItemDataBlock.TransientMode.None;
                        break;
                    case "LOSEDURABILITY":
                        item.doesLoseCondition = bool.Parse(vars[1]);
                        break;
                    case "MAXDURABILITY":
                        item._maxCondition = float.Parse(vars[1]);
                        break;
                    case "CRAFTING.INGREDIENTS":
                        if (FindBlueprintForItem(item, out blueprint))
                        {
                            var ingredients = vars[1].Split(',');
                            var ingredient = new List<BlueprintDataBlock.IngredientEntry>();
                            foreach (var entry in ingredients)
                            {
                                var @params = String.SplitQuotesStrings(entry);
                                if (@params.Length < 2) @params = new[] { "1", @params[0] };
                                var ingredientItem = DatablockDictionary.GetByName(@params[1]);
                                if (ingredientItem != null)
                                {
                                    var ingredientEntry = new BlueprintDataBlock.IngredientEntry
                                    {
                                        amount = int.Parse(@params[0]),
                                        Ingredient = ingredientItem
                                    };
                                    ingredient.Add(ingredientEntry);
                                }
                                else
                                {
                                    ConsoleSystem.LogError(
                                        $"Blueprint ingredient {@params[1]} not exist for item {item.name}.");
                                }
                            }

                            blueprint.ingredients = ingredient.ToArray();
                        }
                        else
                        {
                            ConsoleSystem.LogError($"Blueprint for item {item.name} not exist.");
                        }

                        break;
                    case "CRAFTING.REQUIREWORKBENCH":
                        if (FindBlueprintForItem(item, out blueprint))
                            blueprint.RequireWorkbench = bool.Parse(vars[1]);
                        else
                            ConsoleSystem.LogError($"Blueprint for item {item.name} not exist.");
                        break;
                    case "CRAFTING.DURATION":
                        if (FindBlueprintForItem(item, out blueprint))
                            blueprint.craftingDuration = float.Parse(vars[1]);
                        else
                            ConsoleSystem.LogError($"Blueprint for item {item.name} not exist.");
                        break;
                    case "CRAFTING.AMOUNT":
                        if (FindBlueprintForItem(item, out blueprint))
                            blueprint.numResultItem = int.Parse(vars[1]);
                        else
                            ConsoleSystem.LogError($"Blueprint for item {item.name} not exist.");
                        break;
                    case "MINUSES":
                        item._minUsesForDisplay = int.Parse(vars[1]);
                        break;
                    case "MAXUSES":
                        item._maxUses = int.Parse(vars[1]);
                        break;
                }
            }
        }

        return true;
    }

    private static bool FindBlueprintForItem(ItemDataBlock item, out BlueprintDataBlock blueprint)
    {
        foreach (var block in DatablockDictionary.All)
        {
            var local = block as BlueprintDataBlock;
            if (local == null || local.resultItem != item) continue;
            blueprint = local;
            return true;
        }

        blueprint = null;
        return false;
    }
}