using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using RustExtended;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("BQuests", "systemXcrackedZ", "1.0.0")]
    internal class BQuests : RustLegacyPlugin
    {
        [PluginReference] private Plugin BTeaBoosts;

        private readonly List<ulong> _loadedPlayers = new List<ulong>();

        #region [DATA] Quest
        private class Quest
        {
            public Quest(int iD, string branch, string description, int neededActions, Dictionary<string, int> rewards, Dictionary<string, int> arguments)
            {
                ID = iD;
                Branch = branch;
                Description = description;
                NeededActions = neededActions;
                Rewards = rewards;
                Arguments = arguments;
            }
            public int ID { get; }
            public string Branch { get; }
            public string Description { get; }
            public int NeededActions { get; }
            public Dictionary<string, int> Rewards { get; }
            public Dictionary<string, int> Arguments { get; }
        }
        private static readonly List<Quest> Quests = new List<Quest>
        {
            /*  +  */new Quest(1, "PVE", "Убить 3 животных, добыть по 40 каждой руды, добыть 250 дерева", 293, 
                new Dictionary<string, int>{{"Sulfur Ore",50}, {"Metal Ore",50}, {"Wood",250}, {"Primed 556 Casing",100}}, 
                new Dictionary<string, int>{{"KillNPC", 3}, {"Gather Sulfur Ore", 40}, {"Gather Metal Ore", 40}, {"Gather Wood", 250}}),
            /*  +  */new Quest(2, "PVE", "Скрафтить 1 фундамент, 2 пиллара, 2 стены, 1 проем, 1 потолок, 1 метал дверь", 8, 
                new Dictionary<string, int>{{"Rad Suit Boots",1},{"Rad Suit Pants",1},{"Rad Suit Vest",1},{"Rad Suit Helmet",1},{"Anti-Radiation Pills", 10},{"Primed 556 Casing",150}},
                new Dictionary<string, int>{{"Craft Wood Foundation", 1},{"Craft Wood Pillar", 2},{"Craft Wood Wall", 2},{"Craft Wood Doorway", 1},{"Craft Wood Ceiling", 1},{"Craft Metal Door", 1},}),
            /*  +  */new Quest(3, "PVE", "Слутать на каждом РТ хотя бы по 5 любых ящиков(биг,смолл,ангар,бочки,фактори)", 25, 
                new Dictionary<string, int> {{"Primed 556 Casing",500}, {"Armor Part 5", 1}}, 
                new Dictionary<string, int> {{"LootZoneAny big", 5}, {"LootZoneAny small", 5}, {"LootZoneAny bochki", 5}, {"LootZoneAny angar", 5}, {"LootZoneAny factory", 5}}),
            /*  +  */new Quest(4, "PVE", "Попробовать все виды еды(Сырое и жареное мясо, консервы, шоколад, батончик, Small Rations)", 7, 
                new Dictionary<string, int>{{"Sulfur Ore", 150}, {"Metal Ore", 150}, {"Wood", 500}, {"Primed 556 Casing", 200}}, 
                new Dictionary<string, int>{{"Use Raw Chicken Breast", 1},{"Use Cooked Chicken Breast", 1},{"Use Can of Tuna", 1},{"Use Granola Bar", 1},{"Use Small Rations", 1},{"Use Chocolate Bar", 1},{"Use Can of Beans", 1}}),
            /*  +  */new Quest(5, "PVE", "Убить 10 кроликов, 5 куриц и 3 кабана", 18, 
                new Dictionary<string, int>{{"Uber Hatchet",1}, {"Primed 556 Casing", 300}}, 
                new Dictionary<string, int>{{"Kill Rabbit", 10}, {"Kill Chicken", 5}, {"Kill Boar", 3}}),
            /*  +  */new Quest(6, "PVE", "Добежать до координат (0, 388, 0)", 1, 
                new Dictionary<string, int>{{ "Armor Part 1", 1 }, { "Armor Part 2", 1 }, { "Armor Part 3", 1 }, { "Armor Part 4", 1 }}, 
                new Dictionary < string, int > { {"Run 0.388.0", 1 } }),
            /*  +  */new Quest(7, "PVE", "Убить 20 мутантов и получить 1000 радиации", 21, 
                new Dictionary<string, int>{{"Primed 556 Casing",500},{"Large Medkit", 30}, {"Money", 5000}}, 
                new Dictionary < string, int > { { "KillMutant", 20 }, { "Radiation 1k", 1} }),
            /*  +  */new Quest(8, "PVE", "Нафармить 500 Stones и 250 каждой руды", 750, 
                new Dictionary<string, int>{{"Primed 556 Casing",600},{"Leather Boots",1},{"Leather Pants",1},{"Leather Vest",1},{"Leather Helmet",1},{"Small Water Bottle", 1}}, 
                new Dictionary<string, int>{{"Gather Stones", 500}, {"Gather Sulfur Ore", 250}, {"Gather Metal Ore", 250}}),
            /*  +  */new Quest(9, "PVE", "Найти в ящиках на рт в сумме 400 скрапа", 400, 
                new Dictionary<string, int>{{"Primed 556 Casing",750},{"Low Quality Metal", 50 } }, 
                new Dictionary < string, int > { { "LootItem Primed 556 Casing", 400 } }),
            /*  +  */new Quest(10, "PVE", "Убить каждого вида животного с болта", 8, 
                new Dictionary<string, int>{{"Primed 556 Casing",1000}}, 
                new Dictionary<string, int>{{ "KillNPCWeapon Bolt Action Rifle Wolf", 1 }, {"KillNPCWeapon Bolt Action Rifle Bear", 1}, {"KillNPCWeapon Bolt Action Rifle Mutant Wolf", 1}, {"KillNPCWeapon Bolt Action Rifle Mutant Bear", 1}, {"KillNPCWeapon Bolt Action Rifle Stag", 1}, {"KillNPCWeapon Bolt Action Rifle Boar", 1}, {"KillNPCWeapon Bolt Action Rifle Rabbit", 1}, {"KillNPCWeapon Bolt Action Rifle Chicken", 1}}),

            /*  +  */new Quest(1, "PVP", "Нанести урон с лука на расстоянии более 40 метров", 1, 
                new Dictionary<string, int>{{"Money",5000}}, 
                new Dictionary<string, int>{ {"DamageRange Hunting Bow 40", 5} }),
            /*  +  */new Quest(2, "PVP", "Убить 3 игрока с лука", 3, 
                new Dictionary<string, int>{{"Pipe Shotgun",2},{"Primed 556 Casing",100},{"Cloth Boots",1},{"Cloth Pants",1},{"Cloth Vest",1},{"Cloth Helmet",1}},
                new Dictionary<string, int>{{"KillWeapon Hunting Bow", 3}}),
            /*  +  */new Quest(3, "PVP", "Убить 5 игроков с Pipe Shotgun, один из которых должен находиться на Small RT", 5, 
                new Dictionary<string, int>{{"9mm Pistol",1},{"Revolver", 1}, {"9mm Ammo", 100}, {"Primed 556 Casing", 150}}, 
                new Dictionary<string, int>{{"KillWeapon Pipe Shotgun", 4}, {"KillWeaponZone Pipe Shotgun small", 1}}),
            /*  +  */new Quest(4, "PVP", "Убить 10 мутантов с Pipe Shotgun", 10, 
                new Dictionary<string, int>{{"Research Kit 1", 1}, {"Primed 556 Casing", 200}}, 
                new Dictionary<string, int>{{"KillMutantWeapon Pipe Shotgun", 10}}),
            /*  +  */new Quest(5, "PVP", "Убить с револьвера 1 человек в голову и 2 в тело", 3, 
                new Dictionary<string, int>{{"Low Quality Metal",40},{"Primed 556 Casing",250}},
                new Dictionary<string, int>{{"KillWeaponPart Revolver head", 1}, {"KillWeaponPart Revolver body", 2}}),
            /*  +  */new Quest(6, "PVP", "Скрафтить P250, 50 9mm Ammo и Research Kit 1", 52, 
                new Dictionary<string, int>{{"Leather Boots",1},{"Leather Pants",1},{"Leather Vest",1},{"Leather Helmet",1},{"Primed 556 Casing",200}}, 
                new Dictionary < string, int > { { "Craft P250", 1 }, {"Craft 9mm Ammo", 50}, {"Craft Research Kit 1", 1} }),
            /*  +  */new Quest(7, "PVP", "Убить с P250 хотя бы одного человека на каждом рт(биг,смолл,бочки,ангар,фактори)", 5, 
                new Dictionary<string, int>{{"Primed 556 Casing",300},{"Armor Part 3", 1}}, 
                new Dictionary<string, int> {{"KillWeaponZone P250 big", 1}, {"KillWeaponZone P250 small", 1}, {"KillWeaponZone P250 bochki", 1}, {"KillWeaponZone P250 angar", 1}, {"KillWeaponZone P250 factory", 1}}),
            /*  +  */new Quest(8, "PVP", "Убить с Shotgun 5 медведей,4 волка,3 кабанов,2 оленей,1 кролика и 1 курицу", 16, 
                new Dictionary<string, int>{{"Supply Signal",1}}, 
                new Dictionary < string, int > { { "KillNPCWeapon Shotgun Bear", 5 }, { "KillNPCWeapon Shotgun Wolf", 4 },{ "KillNPCWeapon Shotgun Boar", 3 },{ "KillNPCWeapon Shotgun Stag", 2 },{ "KillNPCWeapon Shotgun Rabbit", 1 },{ "KillNPCWeapon Shotgun Chicken", 1 } }),
            /*  +  */new Quest(9, "PVP", "Убить 10 игроков с MP5A4", 10, 
                new Dictionary<string, int>{{"M4",1},{"556 Ammo", 250},{"Primed 556 Casing",750}}, 
                new Dictionary < string, int > { { "KillWeapon MP5A4", 10 } }),
            /*  +  */new Quest(10, "PVP", "Убить 50 животных и набить 50 убийств", 100, 
                new Dictionary<string, int>{{"Primed 556 Casing",2000}, {"Explosive Charge", 2}, {"Armor Part 4", 1}}, 
                new Dictionary < string, int > { { "KillNPC", 50 }, {"KillPlayerStat", 50} }),
            
            /*  +  */new Quest(11, "PVP&PVE", "Сдать 5000 скрапа на высоте более 400м", 5000,
                new Dictionary<string, int>{{"Rank: BestPlayer",1},},
                new Dictionary < string, int > { { "PassScrap", 5000 } })
        };
        private static readonly Dictionary<string, string> Translates = new Dictionary<string, string>
        {
            { "Убить 3 животных, добыть по 40 каждой руды, добыть 250 дерева", "Kill 3 animals, mine 40 of each ore, mine 250 wood"},
            { "Скрафтить 1 фундамент, 2 пиллара, 2 стены, 1 проем, 1 потолок, 1 метал дверь", "Craft 1 foundation, 2 pillars, 2 walls, 1 opening, 1 ceiling, 1 metal door"},
            { "Слутать на каждом РТ хотя бы по 5 любых ящиков(биг,смолл,ангар,бочки,фактори)", "Loot on each RT at least 5 of any boxes (big, small, hangar, barrels, factories)"},
            { "Попробовать все виды еды(Сырое и жареное мясо, консервы, шоколад, батончик, Small Rations)", "Try all types of food (Raw and fried meat, canned food, chocolate, bar, Small Rations)"},
            { "Убить 10 кроликов, 5 куриц и 3 кабана", "Kill 10 rabbits, 5 chickens and 3 boars"},
            { "Добежать до координат (0, 388, 0)", "Run to coordinates (0, 388, 0)"},
            { "Убить 20 мутантов и получить 1000 радиации", "Kill 20 mutants and get 1000 radiation"},
            { "Нафармить 500 Stones и 250 каждой руды", "Farm 500 Stones and 250 of each ore"},
            { "Найти в ящиках на рт в сумме 400 скрапа", "Found in crates at RT for a total of 400 SCRAP"},
            { "Убить каждого вида животного с болта", "Kill every type of animal with a bolt"},

            { "Нанести урон с лука на расстоянии более 40 метров", "Deal damage with a bow over 40 meters away"},
            { "Убить 3 игрока с лука", "Kill 3 players with a bow"},
            { "Убить 5 игроков с Pipe Shotgun, один из которых должен находиться на Small RT", "Kill 5 players with the Pipe Shotgun, one of which must be on Small RT"},
            { "Убить 10 мутантов с Pipe Shotgun", "Kill 10 mutants with the Pipe Shotgun"},
            { "Убить с револьвера 1 человек в голову и 2 в тело", "Kill 1 person in the head and 2 in the body with a revolver"},
            { "Скрафтить P250, 50 9mm Ammo и Research Kit 1", "Craft P250, 50 9mm Ammo and Research Kit 1"},
            { "Убить с P250 хотя бы одного человека на каждом рт(биг,смолл,бочки,ангар,фактори)", "Kill at least one person on each RT (big, small, barrels, hangar, factory) with the P250"},
            { "Убить с Shotgun 5 медведей, 4 волка, 3 кабанов, 2 оленей, 1 кролика и 1 курицу", "Kill 5 bears, 4 wolves, 3 boars, 2 deer, 1 rabbit and 1 chicken with the Shotgun"},
            { "Убить 10 игроков с MP5A4", "Kill 10 players with the MP5A4"},
            { "Убить 50 животных и набить 50 убийств", "Kill 50 animals and score 50 kills"},

            { "Сдать 5000 скрапа на высоте более 400м", "Hand over 5000 SCRAP at an altitude of more than 400m" }
        };

        private class QuestUser
        {
            public QuestUser(string playerName, ulong userId, Dictionary<string, int> completedActions, Dictionary<string, int> completedArguments,Dictionary<string, List<int>> completedQuests, Dictionary<string, List<int>> earnedQuests)
            {
                PlayerName = playerName;
                UserID = userId;
                CompletedActions = completedActions;
                CompletedArguments = completedArguments;
                CompletedQuests = completedQuests;
                EarnedQuests = earnedQuests;
            }
            public string PlayerName { get; set; }
            public ulong UserID { get; set; }
            
            public int PVEQuest { get; set; }
            public int PVPQuest { get; set; }
            
            public Dictionary<string, int> CompletedActions { get; set; }
            public Dictionary<string, int> CompletedArguments { get; set; }
            
            public Dictionary<string, List<int>> CompletedQuests { get; set; }
            public Dictionary<string, List<int>> EarnedQuests { get; set; }
        }
        private static List<QuestUser> _questUsers;
        #endregion
        #region [QUEST] FindData
        private static QuestUser GetQuestUser(ulong userID) =>
            _questUsers.Find(f => f.UserID == userID);
        private static Quest GetQuest(int id, string branch) => 
            Quests.Find(f => f.ID == id && f.Branch == branch);
        #endregion

        #region [MonoBehaviour] UserActions
        private void LoadPluginToPlayer(PlayerClient pClient)
        {
            if (pClient.gameObject.GetComponent<QuestVm>() != null) return;
            if (_loadedPlayers.Contains(pClient.userID)) _loadedPlayers.Remove(pClient.userID);

            var vm = pClient.gameObject.AddComponent<QuestVm>();
            vm.playerClient = pClient;
            vm.BQuests = this;

            _loadedPlayers.Add(pClient.userID);
        }
        private static void UnloadPluginFromPlayer(GameObject gameObject, Type plugin)
        {
            if (gameObject.GetComponent(plugin) != null) UnityEngine.Object.Destroy(gameObject.GetComponent(plugin));
        }
        #endregion

        #region [HOOKS] PluginActions
        private void Loaded()
        {
            _questUsers = Interface.Oxide.DataFileSystem.ReadObject<List<QuestUser>>("BQuestsUserData");
            foreach (var pc in PlayerClient.All.Where(pc => pc != null))
            {
                LoadPluginToPlayer(pc);
                if (GetQuestUser(pc.userID) == null)
                {
                    _questUsers.Add(new QuestUser(Helper.NiceName(pc.userName), pc.userID, new Dictionary<string, int>(),
                        new Dictionary<string, int>(),
                        new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } },
                        new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }));
                    foreach (var quest in Quests)
                    {
                        GetQuestUser(pc.userID).PVEQuest = 1;
                        GetQuestUser(pc.userID).PVPQuest = 1;
                        GetQuestUser(pc.userID).CompletedActions.Add(quest.Description, 0);
                        foreach (var argument in quest.Arguments)
                        {
                            GetQuestUser(pc.userID).CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                        }
                    }
                }
            }
            foreach (var questUser in _questUsers)
            {
                if (!questUser.CompletedQuests.ContainsKey("PVP&PVE")) questUser.CompletedQuests.Add("PVP&PVE", new List<int>());
                if (!questUser.EarnedQuests.ContainsKey("PVP&PVE")) questUser.EarnedQuests.Add("PVP&PVE", new List<int>());

                if (questUser.CompletedArguments == null) questUser.CompletedArguments = new Dictionary<string, int>();
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments.Where(argument => !questUser.CompletedArguments.ContainsKey($"{quest.ID} - {argument.Key}")))
                    {
                        questUser.CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                    }

                    if (!questUser.CompletedActions.ContainsKey(quest.Description))
                        questUser.CompletedActions.Add(quest.Description, 0);
                }
            }
        }
        private void Unload()
        {
            SaveData();
            foreach (var loadedPlayer in _loadedPlayers)
            {
                PlayerClient playerClient;
                PlayerClient.FindByUserID(loadedPlayer, out playerClient);
                if (playerClient != null) UnloadPluginFromPlayer(playerClient.gameObject, typeof(QuestVm));
            }
        }
        #endregion
        #region [HOOKS] Data
        private void OnServerSave() =>
            SaveData();
        private static void SaveData() =>
            Interface.Oxide.DataFileSystem.WriteObject("BQuestsUserData", _questUsers);
        #endregion

        #region [HOOKS] UserConnections
        private void OnPlayerConnected(NetUser user)
        {
            if (GetQuestUser(user.userID) == null)
            {
                _questUsers.Add(new QuestUser(Helper.NiceName(user.displayName), user.userID, new Dictionary<string, int>(), new Dictionary<string, int>(), new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }, new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } }));
                
                GetQuestUser(user.userID).PVEQuest = 1;
                GetQuestUser(user.userID).PVPQuest = 1;
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments.Keys) GetQuestUser(user.userID).CompletedArguments.Add($"{quest.ID} - {argument}", 0);
                    GetQuestUser(user.userID).CompletedActions.Add(quest.Description, 0);
                }
            }
            if (user.playerClient != null) LoadPluginToPlayer(user.playerClient);
        }
        private void OnPlayerDisconnected(uLink.NetworkPlayer networkPlayer)
        {
            var user = NetUser.Find(networkPlayer);
            if (user != null) _loadedPlayers.Remove(user.userID);
        }
        #endregion
        #region [HOOKS] Actions
        [HookMethod("OnBeltUse")]
        public void OnBeltUse(PlayerInventory playerInv, IInventoryItem inventoryItem)
        {
            if (inventoryItem != null)
            {
                var user = playerInv.inventoryHolder.netUser;
                CompleteArgument(user, $"Use {inventoryItem.datablock.name}");
            }
        }
        private void OnGetClientMove(HumanController controller, Vector3 origin)
        {
            var vector = new Vector3(0, 388, 0);
            if (Vector3.Distance(vector, origin) < 11.0f)
            {
                CompleteArgument(controller.netUser, "Run 0.388.0");
            }
        }
        private void ModifyDamage(TakeDamage takedamage, DamageEvent evt)
        {
            if (evt.attacker.client == null) return;
            if (evt.attacker.client == evt.victim.client) return;
                
            NetUser attacker = evt.attacker.client?.netUser;
            if (attacker == null) return;
                
            PlayerInventory inv = attacker.playerClient.controllable.GetComponent<PlayerInventory>();
            if (inv != null && (inv.activeItem?.datablock?.name?.Contains("Shotgun") ?? false)) return;
                
            if (inv.activeItem.datablock.name.Contains("Hunting Bow"))
            {
                if (Vector3.Distance(evt.attacker.client.lastKnownPosition, evt.victim.character.transform.position) >
                    39.9f)
                {
                    CompleteArgument(evt.attacker.client.netUser, "DamageRange Hunting Bow 40");
                }
            }
        }
        private void OnHurt(TakeDamage damage, DamageEvent evt)
        {
            try
            {
                var netUser = evt.victim.client.netUser;
                if (netUser == null) return;
                    
                Character character;
                if (!Character.FindByUser(netUser.userID, out character)) return;
                    
                var meta = character.GetComponent<Metabolism>();
                if (meta.aiControlled) return;
                    
                if (meta.radiationLevel >= 1000.0f) CompleteArgument(netUser, "Radiation 1k");
            }
            catch
            {
                // ignored
            }
        }
        private void OnKilled(TakeDamage damage, DamageEvent evt)
        {
            try
            {
                if (evt.victim.idMain != null && evt.attacker.idMain != null)
                {
                    if (evt.amount < damage.health) return;

                    var netUser = evt.attacker.client.netUser;
                    if (netUser == null) return;

                    if (damage is HumanBodyTakeDamage)
                    {
                        var victimPlayer = evt.victim.client.netUser;
                        if (victimPlayer == null || netUser == victimPlayer) return;

                        CompleteArgument(netUser, "KillPlayer");
                        CompleteArgument(netUser, "KillPlayerStat");

                        var part = evt.bodyPart.GetNiceName();
                        var weapon = evt.extraData as WeaponImpact;
                        
                        if (Zones.Get(netUser) != null) CompleteArgument(netUser, $"KillPlayerZone {Zones.Get(netUser).Name}");
                        if (weapon != null)
                        {
                            var weaponName = weapon.dataBlock.name;
                            if (part == "torso" || part == "chest") part = "body";
                            CompleteArgument(netUser, $"KillWeaponPart {weaponName} {part}");
                            if (Zones.Get(netUser) != null) CompleteArgument(netUser, $"KillWeaponZone {weaponName} {Zones.Get(netUser).Name}");
                            CompleteArgument(netUser, $"KillWeapon {weaponName}");
                        }
                        
                        PlayerInventory inv = netUser.playerClient.controllable.GetComponent<PlayerInventory>();
                        if (inv != null && (inv.activeItem?.datablock?.name?.Contains("Shotgun") ?? false)) return;
                
                        if (inv.activeItem.datablock.name.Contains("Hunting Bow"))
                        {
                            CompleteArgument(evt.attacker.client.netUser, "KillWeapon Hunting Bow");
                        }

                        return;
                    }

                    var victim = evt.victim.character;
                    if (victim != null)
                    {
                        CompleteArgument(netUser, "KillNPC");
                        
                        var vName = Helper.NiceName(victim.name);
                        CompleteArgument(netUser, $"Kill {vName}");

                        var weapon = evt.extraData as WeaponImpact;
                        if (vName.Contains("Mutant"))
                        {
                            CompleteArgument(netUser, "KillMutant");
                            if (weapon != null) CompleteArgument(netUser, $"KillMutantWeapon {weapon.dataBlock.name}");
                            if (Zones.Get(netUser) != null) CompleteArgument(netUser, $"KillMutantZone {Zones.Get(netUser).Name}");
                        }
                        if (weapon != null)
                        {
                            var weaponName = weapon.dataBlock.name;
                            CompleteArgument(netUser, $"KillNPCWeapon {weaponName} {vName}");
                            CompleteArgument(netUser, $"KillNPCWeaponAny {weaponName}");
                        }
                        return;
                    }

                    if (evt.victim.idMain is StructureComponent && evt.damageTypes == DamageTypeFlags.damage_explosion)
                    {
                        CompleteArgument(netUser, "Explosive");
                        CompleteArgument(netUser, $"Kill {Helper.NiceName(evt.victim.idMain.name)}");
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
        private void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            var netUser = NetUser.Find(inventory.networkView.owner);
            var playerInv = inventory as PlayerInventory;
            if (playerInv == null || netUser == null) return;

            var resultName = blueprint.resultItem.name;
            
            if (resultName.ToLower().Contains("armor part")) Helper.InventoryItemRemove(playerInv, DatablockDictionary.GetByName(resultName), amount);
	        if (resultName.ToLower().Contains("camp fire")) Helper.InventoryItemRemove(playerInv, DatablockDictionary.GetByName(resultName), amount);
            
            CompleteArgument(netUser, $"Craft {resultName}", amount);
        }

        private readonly Dictionary<int, Vector3> _boxList = new Dictionary<int, Vector3>();
        private void OnItemRemoved(Inventory fromInv, int slot, IInventoryItem item)
        {
            if (fromInv == null) return;

            var lootable = fromInv.GetComponent<LootableObject>();
            if (lootable == null || !lootable.destroyOnEmpty || lootable.NumberOfSlots != 12) return;

            var boxID = lootable.networkViewID.id;
            if (boxID == 0) return;

            if (_boxList.ContainsKey(boxID) && _boxList[boxID] != lootable.transform.position) _boxList.Remove(boxID);
            if (!_boxList.ContainsKey(boxID))
            {
                foreach (var player in PlayerClient.All.Where(player => player != null && fromInv.IsAnAuthorizedLooter(player.netPlayer)))
                {
                    var lootName = lootable.name.Replace("(Clone)", "");
                    
                    CompleteArgument(player.netUser, $"Loot {lootName}");
                    CompleteArgument(player.netUser, $"LootItem {item.datablock.name}", item.uses);
                    CompleteArgument(player.netUser, "LootAny");
                    if (Zones.Get(player.lastKnownPosition) != null)
                    {
                        CompleteArgument(player.netUser, $"LootZone {Zones.Get(player.lastKnownPosition).Name} {lootName}");
                        CompleteArgument(player.netUser, $"LootZoneAny {Zones.Get(player.lastKnownPosition).Name}");
                        CompleteArgument(player.netUser, $"LootAnyZone {lootName}");
                        CompleteArgument(player.netUser, "LootAnyZoneAny");
                    }
                    _boxList.Add(boxID, lootable.transform.position);
                    break;
                }
            }
        }

        private void OnGather(Inventory receiver, ResourceTarget obj, ResourceGivePair item, int collected)
        {
            try
            {
                if (item == null || receiver == null || obj == null || collected < 1) return;
                var netUser = NetUser.Find(receiver.networkView.owner);
                if (netUser == null) return;

                var clan = Clans.Find(netUser.userID);
                CompleteArgument(netUser, $"Gather {item.ResourceItemName}", clan != null ? (int)(collected * clan.Level.BonusGatheringWood / 100) + (int)(RustExtended.Core.ResourcesAmountMultiplierRock * collected) : 0 + (int)(RustExtended.Core.ResourcesAmountMultiplierRock * collected));
            }
            catch
            {
                // ignored
            }
        }
        #endregion

        #region [QUEST] Complete
        private void OnQuestCompleted(NetUser user, Quest quest)
        {
            foreach (var reward in quest.Rewards)
            {
                switch (reward.Key)
                {
                    case "Money":
                        Economy.Database[user.userID].Balance += (ulong)reward.Value;
                        continue;
                    case "Rank: BestPlayer":
                        Users.GetBySteamID(user.userID).Rank = reward.Value;
                        continue;
                    default:
                        Helper.GiveItem(user.playerClient, reward.Key, reward.Value);
                        break;
                }
            }
            GetQuestUser(user.userID).EarnedQuests[quest.Branch].Add(quest.ID);
            rust.Notice(user, $"Вы успешно получили награду за квест \"{quest.Branch} №{quest.ID}\"!");
        }
        private void CompleteAction(NetUser netUser, Quest quest, int count = 1)
        {
            var user = GetQuestUser(netUser.userID);
            
            user.CompletedActions[quest.Description] += count;
            if (user.CompletedActions[quest.Description] < quest.NeededActions) return;

            switch (quest.Branch)
            {
                case "PVP":
                    if (user.PVPQuest == 10) break;
                    user.PVPQuest++;
                    if (GetQuest(user.PVEQuest, "PVP").Arguments.ContainsKey("KillPlayerStat"))
                        CompleteArgument(netUser, "KillPlayerStat", Economy.Get(netUser.userID).PlayersKilled);
                    break;
                case "PVE":
                    if (user.PVEQuest == 10) break;
                    user.PVEQuest++;
                    break;
            }

            user.CompletedQuests[quest.Branch].Add(quest.ID);
            rust.Notice(netUser, $"Вы успешно выполнили квест \"{quest.Branch} №{quest.ID}\"!");

            SaveData();

            for (var i = 1; i <= 10; i++)
            {
                if (user.CompletedQuests["PVE"].Contains(i)) continue;
                return;
            }
            for (var i = 1; i <= 10; i++)
            {
                if (user.CompletedQuests["PVP"].Contains(i)) continue;
                return;
            }

            var message = $"[QuestInfo]\r\n Игрок [{Users.GetUsername(netUser.userID)}:{netUser.userID}:{Users.GetBySteamID(netUser.userID).HWID}] открыл квест №11.";
            if (quest.ID == 11) message = $"[QuestInfo]\r\n Игрок [{Users.GetUsername(netUser.userID)}:{netUser.userID}:{Users.GetBySteamID(netUser.userID).HWID}] выполнил все квесты!";

            webrequest.EnqueueGet($"https://api.vk.com/method/messages.send?message={message}&group_id=213390559&random_id={Random.Range(0, 999999)}&peer_id=2000000002&access_token=89b649080717f943e20c54833c819e11794f219ed6c940cf42b1104e3dcfa8da39ad500cdd4a1cddb12ea&v=5.131", (a, b) => { }, this);
        }
        public void CompleteArgument(NetUser netUser, string argument, int count = 1)
        {
            if (argument == "PassScrap")
            {
                var inv = netUser.playerClient.rootControllable.idMain.GetComponent<Inventory>();
                if (inv == null) return;

                var scrapCount = Helper.InventoryItemCount(inv, DatablockDictionary.GetByName("Primed 556 Casing"));
                if (scrapCount == 0) return;

                if (netUser.playerClient.lastKnownPosition.y < 1350.0f) return;
                
                Helper.InventoryItemRemove(inv, DatablockDictionary.GetByName("Primed 556 Casing"), scrapCount);
                Broadcast.Message(Helper.GetPlayerClient(netUser.playerClient.userID).netUser, $"Вы успешно сдали \"SCRAP\" в количестве {scrapCount}!", "Квесты");
                
                CompleteAction(netUser, GetQuest(11, "PVP&PVE"), scrapCount);
                return;
            }
            var user = GetQuestUser(netUser.userID);
            foreach (var quest in Quests.Where(quest => quest.Arguments.ContainsKey(argument) && ((quest.Branch == "PVE" && user.PVEQuest == quest.ID) ||
                                                            (quest.Branch == "PVP" && user.PVPQuest == quest.ID))))
            {
                for (var i = 0; i < count; i++)
                {
                    if (user.CompletedArguments[$"{quest.ID} - {argument}"] < quest.Arguments[argument] &&
                        !user.CompletedQuests[quest.Branch].Contains(quest.ID))
                    {
                        user.CompletedArguments[$"{quest.ID} - {argument}"]++;
                        CompleteAction(netUser, quest);
                    }
                }
            }
        }
        #endregion

        #region [HOOKS] Commands
        [ChatCommand("addargs")]
        private void CMD_WipeQuests(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            foreach (var questUser in _questUsers)
            {
                if (questUser.CompletedArguments == null) questUser.CompletedArguments = new Dictionary<string, int>();
                foreach (var quest in Quests)
                {
                    foreach (var argument in quest.Arguments.Where(argument => !questUser.CompletedArguments.ContainsKey($"{quest.ID} - {argument.Key}")))
                        questUser.CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);

                    if (!questUser.CompletedActions.ContainsKey(quest.Description))
                        questUser.CompletedActions.Add(quest.Description, 0);
                }
            }
            SaveData();
        }
        [ChatCommand("request")]
        private void CMD_ReQuest(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var uData = Users.Find(args[0]);

            var questUser = GetQuestUser(uData.SteamID);

            var branch = args[1];
            var id = int.Parse(args[2]);

            questUser.CompletedQuests[branch].Add(id);
            rust.Notice(NetUser.FindByUserID(uData.SteamID), $"Вы успешно выполнили квест \"{branch} №{id}\"!");

            SaveData();
        }
        [ChatCommand("easyup")]
        private void CMD_EasyUp(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var questUser = GetQuestUser(user.userID);

            foreach (var quest in Quests.Where(quest => quest.ID != 11))
                questUser.CompletedQuests[quest.Branch].Add(quest.ID);

            SaveData();
        }   
        [ChatCommand("clrstate")]
        private void CMD_ClearStats(NetUser user, string cmd, string[] args)
        {
            if (!user.admin) return;
            var uData = Users.Find(args[0]);

            GetQuestUser(uData.SteamID).EarnedQuests = new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } };
            GetQuestUser(uData.SteamID).CompletedArguments = new Dictionary<string, int>();
            GetQuestUser(uData.SteamID).CompletedQuests = new Dictionary<string, List<int>> { { "PVP", new List<int>() }, { "PVE", new List<int>() }, { "PVP&PVE", new List<int>() } };
            GetQuestUser(uData.SteamID).CompletedActions = new Dictionary<string, int>();

            foreach (var quest in Quests)
            {
                foreach (var argument in quest.Arguments) GetQuestUser(uData.SteamID).CompletedArguments.Add($"{quest.ID} - {argument.Key}", 0);
                GetQuestUser(uData.SteamID).CompletedActions.Add(quest.Description, 0);
            }
                
            rust.Notice(user, $"Вы обнулили \"{uData.Username}\"!");
            rust.Notice(NetUser.FindByUserID(uData.SteamID), $"Вас обнулил \"{user.displayName}\"!");
        }
        #endregion

        private class QuestVm : MonoBehaviour
        {
            public PlayerClient playerClient;
            public BQuests BQuests;

            private bool _isNiceUse;
            
            private void Start()
            {
                StartCoroutine(GetIP());
            }
            private IEnumerator GetIP()
            {
                var www = new WWW($"https://proxycheck.io/v2/{playerClient.netUser.networkPlayer.endpoint.Address}?vpn=1&asn=1");
                yield return www;
                
                var r = www.text;
                if (!r.Contains("\"proxy\": \"yes\",") || playerClient.netUser.displayName.Contains("QWER")) yield break;
                
                Debug.Log(r);
                NetCull.CloseConnection(playerClient.netUser.networkPlayer, true);
            }
            
            [RPC]
            public void PassScrap()
            {
                BQuests.CompleteArgument(playerClient.netUser, "PassScrap");
            }
            [RPC]
            public void OnItemAction(string itemName, string option, int slot)
            {
                if (_isNiceUse) return;
                
                BQuests.CompleteArgument(playerClient.netUser, $"Use {itemName}");
                
                if (itemName == "TEA") BQuests.BTeaBoosts?.CallHook("OnTeaUse", playerClient.netUser);
                else
                {
                    _isNiceUse = true;
                    StartCoroutine(Coroutine());
                }
            }
            
            private IEnumerator Coroutine()
            {
                yield return new WaitForSeconds(2.20f);
                _isNiceUse = false;
            }

            [RPC]
            public void GetQuests()
            {
                foreach (var args in from quest in Quests.Where(f => GetQuestUser(playerClient.userID).CompletedQuests[f.Branch].Contains(f.ID) ||
                                                                     (f.Branch == "PVE" &&
                                                                       f.ID == GetQuestUser(playerClient.userID)
                                                                           .PVEQuest) || (f.Branch == "PVP" && f.ID == GetQuestUser(playerClient.userID).PVPQuest) || f.ID == 11)
                         let id = quest.ID
                         let branch = quest.Branch
                         let rewards =
                             quest.Rewards.Aggregate("",
                                 (current, reward) => current + $"{reward.Key} - {reward.Value}шт\n")
                         let description = Users.GetBySteamID(playerClient.userID).Language == "ENG" ? Translates[quest.Description] : quest.Description
                         let completed = GetQuestUser(playerClient.userID).CompletedQuests[branch].Contains(id)
                         let rewarded = GetQuestUser(playerClient.userID).EarnedQuests[branch].Contains(id)
                         let nAct = quest.NeededActions
                         let act = GetQuestUser(playerClient.userID).CompletedActions[quest.Description]
                         select new object[] { id, branch, rewards.Replace("Primed 556 Casing", "Scrap").Replace("Small Water Bottle", "TEA").Replace("Armor Part 5", "HORNS").Replace("Armor Part", "Map"), description, completed, rewarded, nAct, act }) SendRPC("AddQuest", args);
            }
            [RPC]   
            public void GetReward(int questID, string branch)
            {
                var quest = GetQuest(questID, branch);
                if (!GetQuestUser(playerClient.userID).EarnedQuests[branch].Contains(questID)) BQuests.OnQuestCompleted(playerClient.netUser, quest);
            }

            private void SendRPC(string rpcName, params object[] args) =>
                GetComponent<Facepunch.NetworkView>().RPC(rpcName, playerClient.netPlayer, args);
        }
    }
}