using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BClient.AntiCheat;

internal static class CheckHash
{
    private static Dictionary<string, string> _hashes;
    internal static void DoCheck()
    {
        var webClient = new WebClient();
        
        var hashList = webClient.DownloadString("http://blessrust.site/API/GetHashes.php").Split(':');
        var dictionary = new Dictionary<string, string>();
        foreach (var hash in hashList)
        {
            if (!string.IsNullOrEmpty(hash))
            {
                var strings = hash.Split('|');
                dictionary.Add(Directory.GetCurrentDirectory() + @"\" + strings[0], strings[1]);
            }
        }

        _hashes = dictionary;
        
        var files = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed\");
        var plugins = Directory.GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\");
        
        /*if (!plugins.Contains(Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\BNative.dll"))
        {
            webClient.DownloadFile("http://blessrust.site/BF/BNative.dll", Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\BNative.dll");
            Process.Start("BCUpdate.exe");
            
            Application.Quit();
            Process.GetCurrentProcess().Kill();
        }
        else
        {
            if (!IsValidHash(Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\BNative.dll", out var hash))
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\log.ini", $@"rust_Data\Plugins\BNative.dll|{hash}");
                
                webClient.DownloadFile(new Uri("http://blessrust.site/BF/BNative.dll"), Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\BNative.dll");
                Process.Start("BCUpdate.exe");
            
                Application.Quit();
                Process.GetCurrentProcess().Kill();
            }
        }*/
        if (!plugins.Contains(Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\MinHook.dll"))
        {
            webClient.DownloadFile(new Uri("http://blessrust.site/BF/MinHook.dll"),  Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\MinHook.dll");
            Process.Start("BCUpdate.exe");
            
            Application.Quit();
            Process.GetCurrentProcess().Kill();
        }
        else
        {
            if (!IsValidHash(Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\MinHook.dll", out var hash))
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + @"\log.ini", $@"rust_Data\Plugins\MinHook.dll|{hash}");
                
                webClient.DownloadFile(new Uri("http://blessrust.site/BF/MinHook.dll"), Directory.GetCurrentDirectory()  + @"\rust_Data\Plugins\MinHook.dll");
                Process.Start("BCUpdate.exe");
            
                Application.Quit();
                Process.GetCurrentProcess().Kill();
            }
        }
        foreach (var hash in _hashes.Keys.Where(hash => !files.Contains(hash)))
        {
            if (hash == Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\MinHook.dll" || hash == Directory.GetCurrentDirectory() + @"\rust_Data\Plugins\BNative.dll") continue;
            Process.Start("BCUpdate.exe");
            
            Application.Quit();
            Process.GetCurrentProcess().Kill();
        }

        var temp = false;
        foreach (var file in files)
        {
            if (!_hashes.Keys.Contains(file))
            {
                File.Delete(file);
                continue;
            }

            if (string.IsNullOrEmpty(file) || IsValidHash(file, out var hash)) continue;
            File.AppendAllText(Directory.GetCurrentDirectory() + @"\log.ini", $"{file}|{hash}");
            temp = true;
        }

        if (!temp)
        {
            Process.Start("BCUpdate.exe");

            Application.Quit();
            Process.GetCurrentProcess().Kill();
        }

        foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory() + @"\rust_Data\Managed"))
            Hooks.HookListener.HookIDs.Add(CRC32.Quick(AssemblyHandler.GetBytes(file)));

        Bootstrapper.Initialized = true;
    }
    
    private static bool IsValidHash(string file, out string hash)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(file);
        
        hash = BytesToHex(md5.ComputeHash(stream));
        return _hashes[file] == hash;
    }
    private static string BytesToHex(byte[] bytes) 
    { 
        return string.Concat(Array.ConvertAll(bytes, x => x.ToString("X2"))); 
    }
}