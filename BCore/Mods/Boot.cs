using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using BitStream = uLink.BitStream;

namespace BCore.Mods;

public class Boot : MonoBehaviour
{
    private static string _dataPath;
    private static string _modsPath;
    private static string _cppModsPath;

    private static readonly List<Mod> Mods = new();
    private static readonly List<Mod> CPPMods = new();

    public static void LoadMods()
    {
        _dataPath = Directory.GetCurrentDirectory() + "\\serverdata\\BCore\\";
        _modsPath = Directory.GetCurrentDirectory() + "\\serverdata\\BCore\\Modifications\\";
        _cppModsPath = Directory.GetCurrentDirectory() + "\\serverdata\\BCore\\CPPModifications\\";

        if (!Directory.Exists(_dataPath)) Directory.CreateDirectory(_dataPath);
        if (!Directory.Exists(_modsPath)) Directory.CreateDirectory(_modsPath);
        if (!Directory.Exists(_cppModsPath)) Directory.CreateDirectory(_cppModsPath);

        Mods.Clear();
        foreach (var text in Directory.GetFiles(_modsPath))
            Mods.Add(new Mod
            {
                FileName = new FileInfo(text).Name,
                RawData = File.ReadAllBytes(text),
                InvokeMethods = GetInvokeMethods(new FileInfo(text).Name)
            });
        Debug.Log($"[BCore]: Mods initialized. \r\n[BCore]: Total Mods Count: {Mods.Count}.");
    }

    public static void SendMods(ref BitStream stream)
    {
        if (Mods.Count <= 0) return;
        stream.WriteInt32(Mods.Count);
        foreach (var mod in Mods)
        {
            stream.WriteString(mod.FileName);
            stream.WriteBytes(mod.RawData);
            stream.WriteInt32(mod.InvokeMethods.Count);
            foreach (var t in mod.InvokeMethods) stream.WriteString(t);
        }
    }

    private static List<string> GetInvokeMethods(string fileName)
    {
        return (from text in File.ReadAllLines(_dataPath + "invokes.cfg")
            where text.Contains("[Assembly=" + fileName + "]")
            select text.Replace("[Assembly=" + fileName + "] ", "")).ToList();
    }
}