using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.EventSystem;
using BCore.Users;
using Oxide.Core.Libraries;
using UnityEngine;

namespace BCore.WorldManagement;

[Flags]
public enum ZoneFlags
{
    Radiation = 0x0001,
    NoDecay = 0x0002,
    NoBuild = 0x0008,
    NoPvp = 0x0010,
    Safe = 0x0020,
    NoSleep = 0x0040,
    NoCraft = 0x0100,
    NoEnter = 0x0200,
    NoLeave = 0x0400
}

public class WorldZone
{
    public Vector2 Center;
    public ZoneFlags Flags;
    public string[] ForbiddenCommand;
    public List<WorldZone> Internal;
    public string[] MessageOnEnter;
    public string[] MessageOnLeave;
    public string Name;

    public string NoticeOnEnter;
    public string NoticeOnLeave;

    public ClanData Owned;

    public List<Vector2> Points;
    public List<Vector3> Spawns;
    public long WarpTime;
    public WorldZone WarpZone;

    public WorldZone(string name = null, ZoneFlags flags = 0)
    {
        Name = name;
        Flags = flags;
        Points = new List<Vector2>();
        Spawns = new List<Vector3>();
        Internal = new List<WorldZone>();
        ForbiddenCommand = new string[0];
        MessageOnEnter = new string[0];
        MessageOnLeave = new string[0];
        WarpZone = null;
        WarpTime = 0;
    }

    public string DefName => Zones.GetDefName(this);

    public bool Radiation => (Flags & ZoneFlags.Radiation) == ZoneFlags.Radiation;
    public bool NoSleepers => (Flags & ZoneFlags.NoSleep) == ZoneFlags.NoSleep;
    public bool NoBuild => (Flags & ZoneFlags.NoBuild) == ZoneFlags.NoBuild;
    public bool NoCraft => (Flags & ZoneFlags.NoCraft) == ZoneFlags.NoCraft;
    public bool NoDecay => (Flags & ZoneFlags.NoDecay) == ZoneFlags.NoDecay;
    public bool NoPvP => (Flags & ZoneFlags.NoPvp) == ZoneFlags.NoPvp;
    public bool Safe => (Flags & ZoneFlags.Safe) == ZoneFlags.Safe;
    public bool Warp => WarpZone != null;
    public bool NoEnter => (Flags & ZoneFlags.NoEnter) == ZoneFlags.NoEnter;
    public bool NoLeave => (Flags & ZoneFlags.NoLeave) == ZoneFlags.NoLeave;

    public void SetFlags(string flags)
    {
        if (string.IsNullOrEmpty(flags)) return;
        flags = flags.Trim().Replace(" ", "");
        Flags = (ZoneFlags)Enum.Parse(typeof(ZoneFlags), flags);
    }
}

public static class Zones
{
    private const string SaveFilename = "rust_zones.txt";
    public static Dictionary<string, WorldZone> Database;
    public static readonly List<GameObject> Markers = new();
    public static Vector3 JailPosition = Vector3.zero;
    public static string SaveFilePath { get; private set; }

    public static int Count => Database.Count;
    public static WorldZone LastZone { get; private set; }

    public static Dictionary<string, WorldZone> All => Database;

    public static bool IsBuild => LastZone != null;

    public static void Initialize()
    {
        SaveFilePath = Path.Combine(@"serverdata\", SaveFilename);
        Database = new Dictionary<string, WorldZone>();
        if (File.Exists(SaveFilePath)) LoadAsFile();
    }

    public static bool LoadAsFile()
    {
        if (Database == null) return false;
        Database.Clear();
        var data = File.ReadAllLines(SaveFilePath);
        WorldZone zone = null;
        var warpZones = new Dictionary<WorldZone, string>();
        foreach (var str in data)
        {
            string[] var;
            if (str.StartsWith("[") && str.EndsWith("]"))
            {
                zone = null;
                if (!str.StartsWith("[ZONE ", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError("Invalid section \"" + str + "\" from zones.");
                    continue;
                }

                var = Helper.SplitQuotes(str.Trim('[', ']'));
                if (Database.ContainsKey(var[1]))
                {
                    zone = Database[var[1]];
                }
                else
                {
                    zone = new WorldZone();
                    Database.Add(var[1], zone);
                }

                continue;
            }

            if (zone == null) continue;
            var = str.Split('=');
            if (var.Length < 2) continue;
            var[1] = var[1].Trim();
            if (string.IsNullOrEmpty(var[1])) continue;
            float x = 0;
            float y = 0;
            float z = 0;

            switch (var[0].ToUpper())
            {
                case "NAME":
                    zone.Name = var[1];
                    break;
                case "FLAGS":
                    zone.Flags = var[1].ToEnum<ZoneFlags>();
                    break;
                case "INTERNAL":
                    if (Database.ContainsKey(var[1])) continue;
                    var internalZone = new WorldZone();
                    Database.Add(var[1], internalZone);
                    zone.Internal.Add(internalZone);
                    break;
                case "CENTER":
                    var = var[1].Split(',');
                    if (var.Length > 0) float.TryParse(var[0], out x);
                    if (var.Length > 1) float.TryParse(var[1], out y);
                    zone.Center = new Vector2(x, y);
                    break;
                case "WARP":
                    var = var[1].Split(',');
                    warpZones.Add(zone, var[0].Trim());
                    if (var.Length > 1) long.TryParse(var[1], out zone.WarpTime);
                    break;
                case "POINT":
                    var = var[1].Split(',');
                    if (var.Length > 0) float.TryParse(var[0], out x);
                    if (var.Length > 1) float.TryParse(var[1], out y);
                    zone.Points.Add(new Vector2(x, y));
                    break;
                case "SPAWN":
                    var = var[1].Split(',');
                    if (var.Length > 0) float.TryParse(var[0], out x);
                    if (var.Length > 1) float.TryParse(var[1], out y);
                    if (var.Length > 2) float.TryParse(var[2], out z);
                    zone.Spawns.Add(new Vector3(x, y, z));
                    break;
                case "FORBIDDEN.COMMAND":
                    zone.ForbiddenCommand = zone.ForbiddenCommand.Add(var[1]);
                    break;
                case "ENTER.NOTICE":
                    zone.NoticeOnEnter = var[1];
                    break;
                case "LEAVE.NOTICE":
                    zone.NoticeOnLeave = var[1];
                    break;
                case "ENTER.MESSAGE":
                    zone.MessageOnEnter = zone.MessageOnEnter.Add(var[1]);
                    break;
                case "LEAVE.MESSAGE":
                    zone.MessageOnLeave = zone.MessageOnLeave.Add(var[1]);
                    break;
            }
        }

        foreach (var zoneK in warpZones.Keys) zoneK.WarpZone = Get(warpZones[zoneK]);
        return true;
    }

    public static bool SaveAsFile()
    {
        if (Database == null) return false;
        using var zoneData = new StreamWriter(SaveFilePath);
        foreach (var defName in Database.Keys)
        {
            zoneData.WriteLine("[ZONE " + defName + "]");
            zoneData.WriteLine("NAME=" + Database[defName].Name);
            if (Database[defName].Flags != 0) zoneData.WriteLine("FLAGS=" + Database[defName].Flags);
            foreach (var zone in Database[defName].Internal) zoneData.WriteLine("INTERNAL=" + zone.DefName);
            if (Database[defName].Center != Vector2.zero)
                zoneData.WriteLine("CENTER=" + Database[defName].Center.AsString().Replace(" ", ""));
            if (Database[defName].WarpZone != null)
                zoneData.WriteLine("WARP=" + Database[defName].WarpZone.DefName + "," +
                                   Database[defName].WarpTime);
            foreach (var point in Database[defName].Points)
                zoneData.WriteLine("POINT=" + point.AsString().Replace(" ", ""));
            foreach (var spawn in Database[defName].Spawns)
                zoneData.WriteLine("SPAWN=" + spawn.AsString().Replace(" ", ""));
            foreach (var message in Database[defName].ForbiddenCommand)
                zoneData.WriteLine("FORBIDDEN.COMMAND=" + message);
            if (!string.IsNullOrEmpty(Database[defName].NoticeOnEnter))
                zoneData.WriteLine("ENTER.NOTICE=" + Database[defName].NoticeOnEnter);
            if (!string.IsNullOrEmpty(Database[defName].NoticeOnLeave))
                zoneData.WriteLine("LEAVE.NOTICE=" + Database[defName].NoticeOnLeave);
            foreach (var message in Database[defName].MessageOnEnter)
                zoneData.WriteLine("ENTER.MESSAGE=" + message);
            foreach (var message in Database[defName].MessageOnLeave)
                zoneData.WriteLine("ENTER.MESSAGE=" + message);
            zoneData.WriteLine();
        }

        return true;
    }

    public static WorldZone Get(string defName)
    {
        return Database.ContainsKey(defName) ? Database[defName] : null;
    }

    public static string GetDefName(WorldZone zone)
    {
        return Database.Keys.FirstOrDefault(key => Database[key] == zone);
    }

    public static bool BuildNew(string zoneName)
    {
        if (LastZone != null) return false;
        var defName = "z_" + zoneName.Trim().Replace(" ", "_").ToLower();
        if (Database.ContainsKey(defName)) return false;
        LastZone = new WorldZone(zoneName);
        return true;
    }

    public static bool BuildMark(Vector3 position)
    {
        if (LastZone == null) return false;
        LastZone.Points.Add(new Vector2(position.x, position.z));
        var parentZone = Get(position);
        if (parentZone != null && !parentZone.Internal.Contains(LastZone)) parentZone.Internal.Add(LastZone);
        var location = GetGround(position.x, position.z);
        Markers.Add(World.Spawn(";struct_metal_pillar", location));
        Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 4f, 0)));
        Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 8f, 0)));
        Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 12f, 0)));
        return true;
    }

    public static bool BuildSave()
    {
        if (LastZone == null || LastZone.Points.Count < 3) return false;
        var defName = "z_" + LastZone.Name.Trim().Replace(" ", "_").ToLower();
        LastZone.Center = GetCentroid(LastZone.Points);
        Database.Add(defName, LastZone);
        LastZone = null;
        return true;
    }

    public static WorldZone Find(string sName)
    {
        var searchName = sName.Trim('*');
        foreach (var defName in Database.Keys)
        {
            if (sName.StartsWith("*") && defName.EndsWith(searchName)) return Database[defName];
            if (sName.EndsWith("*") && defName.StartsWith(searchName)) return Database[defName];
            if (defName.Equals(searchName, StringComparison.OrdinalIgnoreCase)) return Database[defName];
            if (sName.StartsWith("*") && Database[defName].Name.EndsWith(searchName)) return Database[defName];
            if (sName.EndsWith("*") && Database[defName].Name.StartsWith(searchName)) return Database[defName];
            if (Database[defName].Name.Equals(searchName, StringComparison.OrdinalIgnoreCase)) return Database[defName];
        }

        return null;
    }

    public static bool Delete(WorldZone zone)
    {
        if (zone == null || !Database.ContainsValue(zone)) return false;
        Database.Remove(zone.DefName);
        return true;
    }

    private static Vector3 GetGround(float x, float z)
    {
        var origin = new Vector3(x, 2000f, z);
        var direction = new Vector3(0f, -1f, 0f);
        return Physics.RaycastAll(origin, direction)[0].point;
    }

    public static void ShowPoints(WorldZone zone)
    {
        foreach (var location in zone.Points.Select(point => GetGround(point.x, point.y)))
        {
            Markers.Add(World.Spawn(";struct_metal_pillar", location));
            Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 4f, 0)));
            Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 8f, 0)));
            Markers.Add(World.Spawn(";struct_metal_pillar", location + new Vector3(0, 12f, 0)));
        }
    }

    public static void HidePoints()
    {
        if (Markers == null || Markers.Count == 0) return;
        foreach (var go in Markers) NetCull.Destroy(go);
        Markers.Clear();
    }

    public static WorldZone Get(uLink.NetworkPlayer netPlayer)
    {
        return !PlayerClient.Find(netPlayer, out var pc) ? null : Get(pc.lastKnownPosition);
    }

    public static WorldZone Get(PlayerClient player)
    {
        return Get(player.lastKnownPosition);
    }

    public static WorldZone Get(NetUser netUser)
    {
        return Get(netUser.playerClient.lastKnownPosition);
    }

    public static WorldZone Get(Transform transform)
    {
        return Get(transform.position.x, transform.position.y, transform.position.z);
    }

    public static WorldZone Get(Vector3 position)
    {
        return Get(position.x, position.y, position.z);
    }

    public static WorldZone Get(float x, float y, float z)
    {
        foreach (var defName in Database.Keys)
        {
            if (!AtZone(Database[defName], x, y, z)) continue;
            var result = Database[defName];
            GetInternal(ref result, x, y, z);
            return result;
        }

        return null;
    }

    public static void GetInternal(ref WorldZone zone, float x, float y, float z)
    {
        foreach (var zoneI in zone.Internal)
            if (AtZone(zoneI, x, y, z))
            {
                zone = zoneI;
                GetInternal(ref zone, x, y, z);
            }
    }

    public static bool AtZone(WorldZone zone, uLink.NetworkPlayer netPlayer)
    {
        PlayerClient pc;
        if (!PlayerClient.Find(netPlayer, out pc)) return false;
        return AtZone(zone, pc.lastKnownPosition);
    }

    public static bool AtZone(WorldZone zone, PlayerClient player)
    {
        return AtZone(zone, player.lastKnownPosition);
    }

    public static bool AtZone(WorldZone zone, NetUser netUser)
    {
        return AtZone(zone, netUser.playerClient.lastKnownPosition);
    }

    public static bool AtZone(WorldZone zone, GameObject gameObject)
    {
        return AtZone(zone, gameObject.transform.position);
    }

    public static bool AtZone(WorldZone zone, Transform transform)
    {
        return AtZone(zone, transform.position);
    }

    public static bool AtZone(WorldZone zone, Vector3 position)
    {
        return AtZone(zone, position.x, position.y, position.z);
    }

    public static bool AtZone(WorldZone zone, float x, float y, float z)
    {
        return AtZone(zone.Points, new Vector2(x, z));
    }

    public static bool AtZone(List<Vector2> v, Vector2 p)
    {
        var count = 0;
        for (int i = 0, j = 0; i < v.Count; i++)
        {
            j = i + 1;
            if (j == v.Count) j = 0;
            if ((!(v[i].y <= p.y) || !(v[j].y > p.y)) && (!(v[i].y > p.y) || !(v[j].y <= p.y))) continue;
            double vt = (p.y - v[i].y) / (v[j].y - v[i].y);
            if (p.x < v[i].x + vt * (v[j].x - v[i].x)) count++;
        }

        return count % 2 != 0;
    }

    public static void OnPlayerMove(NetUser netUser, ref Vector3 newPos, ref TruthDetector.ActionTaken taken)
    {
        if (netUser == null || netUser.playerClient == null || netUser.playerClient.controllable == null)
        {
            return;
        }

        var position = netUser.playerClient.controllable.character.transform.position;
        if (position == newPos || Math.Abs(position.x - newPos.x) < 0 && Math.Abs(position.z - newPos.z) < 0)
        {
            return;
        }

        var user = Data.FindUser(netUser.userID);
        if (user == null)
        {
            return;
        }
        var moveZone = Get(newPos);
        if (user.Zone == moveZone)
        {
            return;
        }

        if (user.Zone != null)
        {
            if (user.Zone.NoLeave && !netUser.admin && (moveZone == null || !user.Zone.Internal.Contains(moveZone)))
            {
                newPos = position;
                taken = TruthDetector.ActionTaken.Moved;
                return;
            }

            if (!string.IsNullOrEmpty(user.Zone.NoticeOnLeave))
            {
                Broadcast.Notice(netUser, "☢", user.Zone.NoticeOnLeave);
            }

            var messageOnLeave = user.Zone.MessageOnLeave;
            foreach (var text in messageOnLeave)
            {
                Broadcast.Message(netUser, text);
            }
        }

        if (moveZone != null)
        {
            if (moveZone.NoEnter && !netUser.admin && (user.Zone == null || !moveZone.Internal.Contains(user.Zone)))
            {
                newPos = position;
                taken = TruthDetector.ActionTaken.Moved;
                return;
            }

            if (!string.IsNullOrEmpty(moveZone.NoticeOnEnter))
            {
                Broadcast.Notice(netUser, "☢", moveZone.NoticeOnEnter);
            }

            var messageOnLeave = moveZone.MessageOnEnter;
            foreach (var text2 in messageOnLeave)
            {
                Broadcast.Message(netUser, text2);
            }
        }

        user.Zone = moveZone;
    }

    #region [Public] GetCentroid: Get center points

    public static Vector2 GetCentroid(WorldZone zone)
    {
        return GetCentroid(zone.Points);
    }

    public static Vector2 GetCentroid(List<Vector2> points)
    {
        var area = 0.0f;
        var centerX = 0.0f;
        var centerY = 0.0f;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            var temp = points[i].x * points[j].y - points[j].x * points[i].y;
            centerX += (points[i].x + points[j].x) * temp;
            centerY += (points[i].y + points[j].y) * temp;
            area += temp;
        }

        if (area == 0) return Vector2.zero;
        area *= 3f;
        return new Vector2(centerX / area, centerY / area);
    }

    #endregion
}