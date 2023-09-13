using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BClient.AntiCheat;
using BClient.Hooks;
using BClient.UserReferences;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using EditorHooksPrivate;
using System.Reflection;
using static UICamera.Mouse;

namespace BClient
{
    public class Bootstrapper
    {
        internal static GameObject LoaderObject;
        public static void Initialize()
        {
            //CheckHash.DoCheck();
            if (HookLoader.LoadHooks())
            {
                /*var file = File.ReadAllBytes(@"rust_Data\Managed\System.Runtime.Serialization.dll");
                var assembly = Assembly.Load(file);*/
                
                //AssemblyHandler.AA.Add(new FileInfo(typeof(Locale).Assembly.ManifestModule.FullyQualifiedName).Name);
                Debug.Log("[color lime][BClient]: Initialized!");
            }
            else
            {
                Debug.Log("[color lime][BClient]: Hooks loading error.");
                Application.Quit();
                Process.GetCurrentProcess().Kill();
            }
        }

        public static void OnPlayerInitialized()
        {
            LoaderObject = new GameObject();
            LoaderObject.AddComponent<PlayerChecker>();
        }

        public static void OnPlayerDisconnected()
        {
            HookListener.Mods.Clear();
            Debug.Log("[color purple][BAC]: Disconnected from BAC server.");
        }

        [DllImport("kernel32.dll")]
        private static extern void ExitProcess(uint uExitCode);

        public static void DPIHook(Type mouseTrigger)
        {
            if (!Initialized) { HookListener.ClickQueue.Add(mouseTrigger); return; }

            var gaga = "";
            var result = "";

            if (Initialized && HookListener.ClickQueue.Count > 0)
            {
                HookListener.ClickQueue.Add(mouseTrigger);
                if (PlayerClient.GetLocalPlayer() is null) { return; }
                for (var i = 0; i < HookListener.ClickQueue.Count; i++)
                {
                    var i1 = i;
                    var t1 = new Thread(() =>
                    {
                        var type = HookListener.ClickQueue[i1];

                        if (File.Exists(HookListener.ClickQueue[i1].Assembly.Location) && Directory
                                .GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed")
                                .Contains(HookListener.ClickQueue[i1].Assembly.GetName().Name)) return;
                        result = OnDPIChanged(type, ref gaga);
                        if (result == "728839") return;

                        Executor.DoBan(gaga);

                        ExitProcess(0);
                        Application.Quit();
                        Process.GetCurrentProcess().Kill();
                    })
                    {
                        IsBackground = true
                    };
                    t1.Start();
                }
                HookListener.ClickQueue.Clear();
            }
            else if (Initialized && HookListener.ClickQueue.Count == 0)
            {
                if (File.Exists(mouseTrigger.Assembly.Location) && Directory
                        .GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed")
                        .Contains(mouseTrigger.Assembly.GetName().Name)) return;
                var t = new Thread(() =>
                {
                    result = OnDPIChanged(mouseTrigger, ref gaga);
                    if (result == "728839") return;

                    Executor.DoBan(gaga);

                    ExitProcess(0);

                    Application.Quit();
                    Process.GetCurrentProcess().Kill();
                });
                t.Start();
            }
        }
        internal static bool Initialized = false;

        internal static string OnDPIChanged(Type type)
        {
            var mouseName = "";
            return OnDPIChanged(type, ref mouseName);
        }

        private static string CheckDPIValue(Assembly assembly, ref string value)
        {
            if (assembly != HookListener.CurrentAssembly)
            {
                if (File.Exists(assembly.Location))
                {
                    var file = File.ReadAllBytes(assembly.Location);
                    if (file.Length is 0 or < 0)
                    {
                        value = "unknown PEB header";
                        return "8383883";
                    }
                    if (!HookListener.HookIDs.Contains(CRC32.Quick(file)))
                    {
                        value = $"{assembly.GetName().Name}.dll";
                        return "436126";
                    }
                }
                else if (!File.Exists(assembly.Location) && !assembly.ManifestModule.Name.StartsWith("data-"))
                {
                    value = $"{assembly.GetName().Name}.dll";
                    return "487897466";
                }
            }
            value = "cleared";
            return "728839";
        }
        internal static string OnDPIChanged(Type type, ref string value)
        {
            var assembly = type.Assembly;
            return CheckDPIValue(assembly, ref value);
        }
    }
}