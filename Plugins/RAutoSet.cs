using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AutoSet", "Romanchik34", "1.0.0")]
    class RAutoSet : RustLegacyPlugin
    {
        private Dictionary<string, int> Commands = new Dictionary<string, int>()
        {
            { "serv.remove \"Barricade Fence Deployable\"", 1800 },
            { "crafting.timescale 1", 5 },
            { "conditionloss.armorhealthmult 0.25", 5 },
            { "conditionloss.damagemultiplier 0.50", 5 },
            { "save.autosavetime 360", 5 },
            { "airdrop.min_players 10", 5 },
            { "env.daylength 140", 5 },
            { "env.nightlength 1", 5 },
            { "voice.distance 20", 5 },
            { "crafting.instant true", 5 },
            { "server.pvp true", 5 },
            { "falldamage.enabled false", 5 },
            { "server.sendrate 5", 5 },
            { "server.framerate 30", 5 },
            { "falldamage.min_vel 24", 5 },
            { "falldamage.max_vel 38", 5 },
        };

        private void Loaded()
        {
            foreach (var command in Commands)
            {
                timer.Repeat(command.Value, 0, () =>
                    rust.RunServerCommand(command.Key));
            }
        }
    }
}