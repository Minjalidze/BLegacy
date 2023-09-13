using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using BClient.UserReferences;

namespace BClient.AntiCheat;

internal static class Executor
{
    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr h, string m, string c, int type);

    public static void Test(string args) => MessageBox(IntPtr.Zero, args, "Bless Anti-Cheat", 0);
    public static void DoBan(object reason)
    {
        SocketConnection.SendPacket("Ban", reason);
        MessageBox(IntPtr.Zero, $"You has been banned by \"{reason}\"" +
                                "\nНе согласны с блокировкой?" +
                                "\nОбратитесь в группу - vk.com/host_fun", "Bless Anti-Cheat", 0);
    }

    public static void InternalBan(string reason) => DoBan(reason);
    public static void InternalTest(string args) => Test(args);
    
    internal static void Disconnect(string reason = null)
    {                           
        if (reason is { Length: > 0 }) ChatUI.AddLine("[BAC]", "[COLOR#FF0000]"+reason+"[/COLOR]");     
        SocketConnection.SendPacket("Kick", reason);           
        NetCull.isMessageQueueRunning = true; NetCull.Disconnect();
    }
}