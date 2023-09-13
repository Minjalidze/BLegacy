using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using BClient.Hooks;
using Debug = UnityEngine.Debug;
using System.Linq;
using System.Text;
using Random = UnityEngine.Random;

namespace BClient.AntiCheat;

internal class AssemblyHandler : MonoBehaviour
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("kernel32.dll")]
    private static extern void ExitProcess(uint uExitCode);

    private string[] _forbiddenProcessModules;

    private string[] _forbiddenWindows;
    private string[] _forbiddenProcesses;

    private string[] _fA;
    
    private static List<Module> AllowedPenis = new ();
    private static List<Assembly> AllowedDicks = new();
    private static List<Component> AllowedSocks = new();
    private static List<string> _allowedNames;

    private const float UpdateInterval = 0.5F;

    private float _accum = 0; // FPS accumulated over the interval
    private int _frames = 0; // Frames drawn over the interval
    private float _timeleft; // Left time for current interval
    private int _fps = 60;

    public static int Ping = 0;

    private void Start()
    {
        _forbiddenWindows = new [] 
        {
            "RIP Rust Legacy", "cheat engine", "form", "MInjector - by EquiFox (x86)", "Sharp Mono Injector", "SharpMonoInjector.Console", "SharpMonoInjector.Gui", "Static LagSwitch", "LagSwitch"
        };
        _forbiddenProcesses = new []
        {
            "PE Injector"
        };
        _fA = new []
            { "BabaGanoush.dll", "yAGdDgoiGJiRBtfLAvnLFauIAkBm.dll", "DizzyClient.dll", "jacked", "KaboomCheat.dll", "RustNoWipeJune2018Zoom.dll", "KMF.dll", "TitaniumExample.dll" };

        _forbiddenProcessModules = new [] 
        {
            "fshieldBypass",
            "BabaGanoush.dll", "yAGdDgoiGJiRBtfLAvnLFauIAkBm.dll", "DizzyClient.dll", "KaboomCheat.dll", "RustNoWipeJune2018Zoom.dll", "KMF.dll", "TitaniumExample.dll", "adolf", "cheatengine"
        };
        _allowedNames = new List<string>
        {
            "КТО", "ПРОЧИТАЛ", "ТОТ", "ГЕЙ"
        };
        
        var files = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed");
        foreach (var file in files)
        {
            if (!HookListener.AA.Contains(FileName(file))) HookListener.AA.Add(FileName(file));
        }
        if (!HookListener.AA.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("cnVzdF9EYXRhL01vbm8vbGliYw==")))) HookListener.AA.Add(Encoding.UTF8.GetString(Convert.FromBase64String("cnVzdF9EYXRhL01vbm8vbGliYw==")));
        if (!HookListener.AA.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("cnVzdF9EYXRhL01vbm8vLlxsaWJj"))))HookListener.AA.Add(Encoding.UTF8.GetString(Convert.FromBase64String("cnVzdF9EYXRhL01vbm8vLlxsaWJj")));
        
        StartCoroutine(CheckServerResponse());
        InvokeRepeating("OnDrawUI", .0f, 5.0f);
    }

    private void Update()
    {
        _timeleft -= Time.deltaTime;
        _accum += Time.timeScale / Time.deltaTime;
        ++_frames;

        if (!(_timeleft <= 0.0)) return;
        
        _fps = (int)(_accum / _frames);
        _timeleft = UpdateInterval;
        _accum = 0.0f;
        _frames = 0;
    }
    private void OnGUI()
    {
        var style2 = new GUIStyle
        {
            normal =
            {
                textColor = Color.white
            },
            fontSize = 14
        };
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Bless Anti-Cheat", style2);
        GUI.Label(new Rect(0, 13, Screen.width, Screen.height), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), style2);
        
        var style = new GUIStyle
        {
            fontSize = 14,
            normal =
            {
                textColor = _fps switch
                {
                    < 10 => Color.red,
                    > 45 => Color.green,
                    _ => Color.yellow
                }
            }
        };
        var style1 = new GUIStyle
        {
            fontSize = 14,
            normal =
            {
                textColor = Ping switch
                {
                    > 100 => Color.red,
                    < 80 => Color.green,
                    _ => Color.yellow
                }
            }
        };
        GUI.Label(new Rect(0, 26, Screen.width, Screen.height), "PING: " + Ping, style1);
        GUI.Label(new Rect(0, 26 + 13, Screen.width, Screen.height), "FPS: " + _fps, style);
    }
    
    private static IEnumerator CheckServerResponse()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);

            var www = new WWW(Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovL2JsZXNzcnVzdC5zaXRl")));
            yield return www;

            if (www.error is null) continue;

            Executor.Disconnect(Encoding.UTF8.GetString(Convert.FromBase64String("RXRoZXJuZXQgY29ubmVjdGlvbiBoYXMgYmVlbiBsb3N0LiAoUG9zc2libGUgY2FibGUgc3dpdGNoKQ==")));
            break;
        }
        yield break;
    }

    private Thread _doClickEvent;
    
    private static List<Assembly> GetAssemblies => AppDomain.CurrentDomain.GetAssemblies().ToList();
    private static List<Module> GetModules => Assembly.GetExecutingAssembly().GetModules().ToList();
    private static ProcessModuleCollection GetPModules => Process.GetCurrentProcess().Modules;
    private static IEnumerable<GameObject> GetGameObjects => FindObjectsOfType<GameObject>();
    private static IEnumerable<Component> GetComponents => FindObjectsOfType<Component>().ToList();
    private static List<string> _niceSrtr = new();

    private void OnMouseClick()
    {
        return;
        var a = GetGameObjects;
        var b = GetComponents;
        var c = GetAssemblies;
        var d = GetPModules;
    }
    private void OnDrawUI()
    {
        _doClickEvent = new Thread(() =>
        {
            var ass = "";

            var result = CheckAssemblies(ref ass);
            if (result is 728839) return;

            Executor.DoBan(ass);

            ExitProcess(0);
            Application.Quit();
            Process.GetCurrentProcess().Kill();
        });
        _doClickEvent.Start();
    }
    
    private int CheckAssemblies(ref string ass)
    {
        var gaga = "";
        var onDPIChanged = "";
        foreach (var forbiddenWindow in _forbiddenWindows)
        {   
            var hWndTargetWindow = FindWindow(null, forbiddenWindow);
            if (hWndTargetWindow == IntPtr.Zero) continue;
            
            ass = forbiddenWindow == "form" ? "RustExploit" : forbiddenWindow;
            return 1488;
        }

        foreach (var module in GetAssemblies)
        {
            if (AllowedDicks.Contains(module)) continue;
            
            if (!File.Exists(module.Location) || !Directory
                    .GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed")
                    .Contains(module.GetName().Name))
            {
                var t1 = new Thread(()=>
                {
                    onDPIChanged = Bootstrapper.OnDPIChanged(module.GetType(), ref gaga);
                    if (onDPIChanged == "728839")
                    {
                        if (!AllowedDicks.Contains(module)) AllowedDicks.Add(module);
                        return;
                    }

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
            else
            {
                if (!AllowedDicks.Contains(module)) AllowedDicks.Add(module);
            }
        }
        
        
        var log = new FileInfo(Directory.GetCurrentDirectory() + Encoding.UTF8.GetString(Convert.FromBase64String("XHJ1c3RfRGF0YVxvdXRwdXRfbG9nLnR4dA==")));
        using (var streamReader = new StreamReader(log.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            while (!streamReader.EndOfStream)
            {
                var text = streamReader.ReadLine();
                if (_niceSrtr.Contains(text)) continue;

                if (!_niceSrtr.Contains(text) && !text!.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("Tm9uIHBsYXRmb3JtIGFzc2VtYmx5Og=="))) && !text.Contains(Encoding.UTF8.GetString(Convert.FromBase64String("RmFsbGJhY2sgaGFuZGxlciBjb3VsZCBub3QgbG9hZCBsaWJyYXJ5")))) 
                { _niceSrtr.Add(text); continue; }

                foreach (var dummy in HookListener.AA.Where(aa => text != null && text.ToLower().Contains(aa.ToLower()))) _niceSrtr.Add(text);
                if (_niceSrtr.Contains(text)) continue;

                foreach (var allowedDick in AllowedDicks.Where(allowedDick => text != null && text.ToLower().Contains(allowedDick.ManifestModule.Name)))
                {
                    _niceSrtr.Add(text); continue;
                }
                
                foreach (var aa in HookListener.AA.Where(aa => text != null && !text.ToLower().Contains(aa.ToLower())))
                {
                    Executor.DoBan(GetAssemblies[Random.Range(0, GetAssemblies.Count())].GetName().Name + "_MODDED");
                    ExitProcess(0);
                    
                    Application.Quit();
                    Process.GetCurrentProcess().Kill();
                }
            }
        }
        
        ass = "checked";
        return 728839;
    }

    private static string FileName(string file) => new FileInfo(file).Name;
    internal static byte[] GetBytes(string file) => File.ReadAllBytes(file);
}