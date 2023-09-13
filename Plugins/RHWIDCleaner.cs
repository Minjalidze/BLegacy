using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustExtended;

namespace Oxide.Plugins
{
    [Info("RHWIDCleaner", "Romanchik34 (vk.com/romanchik34)", "1.0.0")]
    class RHWIDCleaner : RustLegacyPlugin
    {
        [ConsoleCommand("oxide.hwidclean")]
        private void HWIDClean(ConsoleSystem.Arg arg)
        {
            if (arg.argUser != null) return;
            Users.Find(arg.Args[0]).HWID = "";
            Users.SaveAsTextFile();
        }
    }
}
