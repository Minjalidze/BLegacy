using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BCore.Commands;
using BCore.Users;
using Oxide.Core;
using Oxide.Game.RustLegacy.Libraries;
using UnityEngine;
using Ping = BCore.Commands.Ping;
using String = Facepunch.Utility.String;

namespace BCore;

public static class CommandHook
{
    public static RustLegacy Rust;
    public static List<ICommand> CommandList;
    public static readonly List<string> PluginCommands = new();

    private static List<string> _disabledCommands;

    public static ICommand GetCommand(string cmdName)
    {
        return CommandList.Find(f => string.Equals(f.CmdName, cmdName, StringComparison.CurrentCultureIgnoreCase));
    }

    public static void RegisterCommand(ICommand type)
    {
        CommandList.Add(type);
    }

    public static void Initialize()
    {
        Rust = Interface.Oxide.GetLibrary<RustLegacy>("Rust");
        CommandList = new List<ICommand>
        {
            new Kit(), new Kits(),
            new Help(),
            new Config(), new Suicide(), new Commands.Users(),
            new Clan(),
            new Lang(), new Ping(),
            new Set(),
            new Zone(),
            new About(),
            new Who(), new PM(),
            new Shop(), new Buy(), new Sell(),
            new Players(), new Online(),
            new Destroy(),
            new Tp(),
            new Tele(),
            new Share(), new UnShare(), new Reply(),
            new Balance(), new Money(),
            new Home(), new Admin(), new History(), new Give(), new Transfer()
        };
        _disabledCommands = new List<string>();

        if (!File.Exists(@"serverdata\cfg\BCore\disabledcommands.cfg"))
            File.WriteAllText(@"serverdata\cfg\BCore\disabledcommands.cfg", "");
        else
            LoadDisabledCommands();

        Debug.Log($"[BCore]: Disabled Commands Count: {_disabledCommands.Count}");

        Configs.Commands.Initialize();
    }

    public static void LoadDisabledCommands()
    {
        _disabledCommands.Clear();
        var file = File.ReadAllLines(@"serverdata\cfg\BCore\disabledcommands.cfg");
        _disabledCommands.AddRange(file);
    }

    public static bool OnCommand(ConsoleSystem.Arg arg)
    {
        var args = String.SplitQuotesStrings(arg.GetString(0).Trim());
        var command = args[0].Trim().ToLower().Replace("/", "");
        if (args.Length < 2)
        {
            args = new string[0];
        }
        else
        {
            Array.Copy(args, 1, args, 0, args.Length - 1);
            Array.Resize(ref args, args.Length - 1);
        }

        var user = arg.argUser;

        if (PluginCommands.Contains(command))
        {
            var argT = args.Aggregate("", (current, t) => current + $" {t}");
            if (chat.serverlog)
                Debug.Log("[CMD] " + String.QuoteSafe(user.user.Displayname) + ":" + $" /{command}{argT}");

            if (!Configs.Config.History.ContainsKey(user.userID))
                Configs.Config.History.Add(user.userID, new List<HistoryRecord>());
            if (Configs.Config.History[user.userID].Count > Configs.Config._Settings.ChatHistoryStored)
                Configs.Config.History[user.userID].RemoveAt(0);
            Configs.Config.History[user.userID].Add(new HistoryRecord().Init("Command", arg.GetString(0).Trim()));

            return false;
        }

        var cmd = GetCommand(command);
        if (cmd is null || _disabledCommands.Contains(command.ToLower()))
            //Rust.Notice(arg.argUser, "Неизвестная команда. Используйте /help.");
            return false;

        if (!cmd.Ranks.Contains(Data.FindUser(user.userID).Rank))
            //Rust.Notice(arg.argUser, "Команда недоступна для Вас. Используйте /help.");
            return false;

        if (!Configs.Config.History.ContainsKey(user.userID))
            Configs.Config.History.Add(user.userID, new List<HistoryRecord>());
        if (Configs.Config.History[user.userID].Count > Configs.Config._Settings.ChatHistoryStored)
            Configs.Config.History[user.userID].RemoveAt(0);
        Configs.Config.History[user.userID].Add(new HistoryRecord().Init("Command", arg.GetString(0).Trim()));

        cmd.Execute(user, command, args, Data.FindUser(user.userID));
        var argument = args.Aggregate("", (current, t) => current + $" {t}");
        if (chat.serverlog)
            Debug.Log("[CMD] " + String.QuoteSafe(user.user.Displayname) + ":" + $" /{command}{argument}");
        return true;
    }
}