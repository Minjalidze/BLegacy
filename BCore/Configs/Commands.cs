using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

public class Commands
{
    public static List<Command> CommandList;

    public static void Initialize()
    {
        if (File.Exists(@"serverdata\cfg\BCore\commands.cfg"))
        {
            LoadData();
        }
        else
        {
            CommandList = new List<Command>();
            foreach (var command in CommandHook.CommandList)
                CommandList.Add(new Command
                {
                    CmdName = command.CmdName,
                    CmdRanks = command.Ranks,
                    CmdRuDescription = command.RuDescription,
                    CmdEngDescription = command.EngDescription
                });
            SaveData();
        }
    }

    public static void SaveData()
    {
        File.WriteAllText(@"serverdata\cfg\BCore\commands.cfg",
            JsonConvert.SerializeObject(CommandList, Formatting.Indented));
        Debug.Log("[BCore]: Commands Data Saved!");
    }

    public static void LoadData()
    {
        CommandHook.LoadDisabledCommands();
        var file = File.ReadAllText(@"serverdata\cfg\BCore\commands.cfg");
        CommandList = JsonConvert.DeserializeObject<List<Command>>(file);
        foreach (var command in CommandHook.CommandList)
        {
            var cmd = CommandList?.Find(f => f.CmdName == command.CmdName);
            if (cmd is null) continue;

            command.RuDescription = cmd.CmdRuDescription;
            command.EngDescription = cmd.CmdEngDescription;
            command.Ranks = cmd.CmdRanks;
        }

        Debug.Log("[BCore]: Commands Data Loaded!");
    }

    public class Command
    {
        public string CmdName { get; set; }
        public string[] CmdRuDescription { get; set; }
        public string[] CmdEngDescription { get; set; }
        public int[] CmdRanks { get; set; }
    }
}