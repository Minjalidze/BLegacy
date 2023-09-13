using System;
using System.Threading;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.EventSystem;
using BCore.Hooks;
using BCore.Mods;
using BCore.Users;
using BCore.WorldManagement;
using Oxide.Core;
using UnityEngine;
using Object = UnityEngine.Object;
using Override = BCore.Configs.Override;

namespace BCore;

public static class Bootstrapper
{
    public static GameObject BootObject;

    public static void Load()
    {
        if (HookLoader.LoadHooks())
        {
            if (Interface.Oxide == null) Interface.Initialize();
            Debug.Log(
                "Мало кто это знал, но единственная фраза, которая мотивировала писать данное ядро - \"Monzi Pidor\".\r\n");

            Config.Initialize();
            Messages.Instantiate();
            Data.LoadUsers();
            Boot.LoadMods();

            BootObject = new GameObject();
            BootObject.AddComponent<Synchronization>();
            BootObject.AddComponent<BootModules>();
            BootObject.AddComponent<Events>();
            Object.DontDestroyOnLoad(BootObject);

            Ranks.Initialize();
            Zones.Initialize();
            Economy.Initialize();
            Kits.Initialize();
            Clans.Initialize();
            LoadOut.Initialize();
            Override.Initialize();
            Destroy.Initialize();
            
            CommandHook.Initialize();
            Debug.Log("[BCore]: Server Mod Initialized!");
        }
        else
        {
            Debug.Log("[BCore]: Error on server initialize.");
        }
    }

    public static void OnResourcesInitialized()
    {
        if (WorldManagement.Override.LootsFileCreated)
            Debug.Log(" Loots file has been created.");
        else if (WorldManagement.Override.LootsInitialized) Debug.Log("  - " + WorldManagement.Override.LootsCount + " Overridden Loot(s)");
        if (WorldManagement.Override.ItemsFileCreated)
            Debug.Log(" Items file has been created.");
        else if (WorldManagement.Override.ItemsInitialized) Debug.Log("  - " + WorldManagement.Override.ItemsCount + " Overridden Item(s)");
    }
}

public class BootModules : MonoBehaviour
{
    private void Update()
    {
        if (DateTime.Now.Subtract(Events.EventTimeDoServer).TotalMilliseconds > 1000.0)
        {
            Events.EventTimeDoServer = DateTime.Now;
            new Thread(new ThreadStart(Events.DoServer))
            {
                IsBackground = true
            }.Start();
        }
        if (DateTime.Now.Subtract(Events.EventTimeDoPlayers).TotalMilliseconds > 1000.0)
        {
            Events.EventTimeDoPlayers = DateTime.Now;
            new Thread(new ThreadStart(Events.DoPlayers))
            {
                IsBackground = true
            }.Start();
        }
    }
}