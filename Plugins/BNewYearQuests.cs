using System.Collections.Generic;
using System.Linq;
using RustExtended;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NewYearQuests", "Aluxx", "1.0.0")]
    public class BNewYearQuests : RustLegacyPlugin
    {
        public const float BoxActionDistance = 1.5f;

        public class QuestCharacter
        {
            public QuestCharacter(string name, string position, float actionDistance)
            {
                Name = name;
                Position = position;
                ActionDistance = actionDistance;
            }
            public string Name { get; set; }
            public string Position { get; set; }
            public float ActionDistance { get; set; }
        }
        public static List<QuestCharacter> Characters;
        private static QuestCharacter CreateCharacter(
            string name, 
            string position, 
            float actionDistance) =>
                new QuestCharacter(name, position, actionDistance);
        private static QuestCharacter FindCharacter(string name) => Characters.Find(f => f.Name == name);
        
        public class UserInfo
        {
            public ulong ID { get; set; }
            
            public int QuestID { get; set; }
            
            public Dictionary<string, int> Actions { get; set; }
            
            public List<QuestInfo> CompletedQuests { get; set; }
        }
        public static List<UserInfo> UsersData;
        private static UserInfo CreateUser(
            ulong id, 
            int questID, 
            Dictionary<string, int> actions,
            List<QuestInfo> completedQuests)
        {
            var user = new UserInfo
            {
                ID = id,
                QuestID = questID,
                Actions = actions,
                CompletedQuests = completedQuests
            };
            return user;
        }
        private static UserInfo FindUser(ulong id) => UsersData.Find(f => f.ID == id);

        public class QuestInfo
        {
            public int ID  { get; set; }
            
            public string Name { get; set; }
            public string Character { get; set; }
            
            public Dictionary<string, int> Actions { get; set; }
            public Dictionary<string, int> Rewards { get; set; }
        }
        public static List<QuestInfo> QuestsData;
        private static QuestInfo CreateQuest(
            string name, 
            string character, 
            Dictionary<string, int> actions,
            Dictionary<string, int> rewards)
        {
            var quest = new QuestInfo
            {
                ID = QuestsData.Count,
                Name = name,
                Character = character,
                Actions = actions,
                Rewards = rewards
            };
            return quest;
        }
        private static QuestInfo FindQuest(int id) => QuestsData.Find(f => f.ID == id);

        private void Loaded()
        {
            LoadData();
            
            if (QuestsData.Count == 0)
                QuestsData.Add(CreateQuest(
                    "ExampleName", 
                    "SnowTree", 
                    new Dictionary<string, int> {{"GatherRock", 150}}, 
                    new Dictionary<string, int> { {"Rock", 1} }));
            
            foreach (var userData in Users.All)
            {
                if (FindUser(userData.SteamID) == null)
                {
                    var questActions = FindQuest(0).Actions.ToDictionary(action => action.Key, action => 0);
                    UsersData.Add(CreateUser(
                        userData.SteamID, 
                        0, 
                        questActions, 
                        new List<QuestInfo>()));
                }
            }
            
            if (Characters.Count == 0)
                Characters.Add(CreateCharacter("SnowTree", new Vector3().ToString(), 3.5f));

            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("NewYearUsersData", UsersData);
            Interface.Oxide.DataFileSystem.WriteObject("NewYearQuestsData", QuestsData);
            Interface.Oxide.DataFileSystem.WriteObject("NewYearCharacters", Characters);
        }
        private void LoadData()
        {
            UsersData = Interface.Oxide.DataFileSystem.ReadObject<List<UserInfo>>("NewYearUsersData");
            QuestsData = Interface.Oxide.DataFileSystem.ReadObject<List<QuestInfo>>("NewYearQuestsData");
            Characters = Interface.Oxide.DataFileSystem.ReadObject<List<QuestCharacter>>("NewYearCharacters");
        }

        [ChatCommand("nyq")]
        private void OnCMD_NYQ(NetUser user, string cmd, string[] args)
        {
            if (user.admin)
            {
                
            }
        }
    }
}