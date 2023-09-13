using UnityEngine;

namespace BCore.WorldManagement;

public class World
{
    #region [Public] Get position from player origin of eyes

    public static bool LookAtPosition(PlayerClient player, out Vector3 position, float maxDistance = 100f)
    {
        position = new Vector3(0, 0, 0);
        var idMain = player.controllable.idMain;
        if (idMain == null || !Physics.Raycast(idMain.eyesRay, out var hit, maxDistance, -1)) return false;
        TransformHelpers.GetGroundInfo(hit.point, 100f, out position, out _);
        return true;
    }

    #endregion

    #region [Public] Spawn Object by position and rotation

    public static GameObject Spawn(string prefab, Vector3 position, Quaternion rotation, int count)
    {
        GameObject spawnObject = null;
        for (var i = 0; i < count; i++)
            if (prefab == ":player_soldier")
            {
                spawnObject = NetCull.InstantiateDynamic(uLink.NetworkPlayer.server, prefab, position, rotation);
            }
            else if (prefab.Contains("C130"))
            {
                spawnObject = NetCull.InstantiateClassic(prefab, position, rotation, 0);
            }
            else
            {
                spawnObject = NetCull.InstantiateStatic(prefab, position, rotation);
                spawnObject.GetComponent<StructureComponent>();
                var spawnDeploy = spawnObject.GetComponent<DeployableObject>();
                if (spawnDeploy == null) continue;
                spawnDeploy.ownerID = 0L;
                spawnDeploy.creatorID = 0L;
                spawnDeploy.CacheCreator();
                spawnDeploy.CreatorSet();
            }

        return spawnObject;
    }

    public static GameObject Spawn(string prefab, Vector3 position)
    {
        return Spawn(prefab, position, 1);
    }

    public static GameObject Spawn(string prefab, Vector3 position, int amount)
    {
        return Spawn(prefab, position, Quaternion.identity, amount);
    }

    public static GameObject Spawn(string prefab, float x, float y, float z)
    {
        return Spawn(prefab, x, y, z, 1);
    }

    public static GameObject Spawn(string prefab, float x, float y, float z, int count)
    {
        return Spawn(prefab, new Vector3(x, y, z), Quaternion.identity, count);
    }

    public static GameObject Spawn(string prefab, float x, float y, float z, Quaternion rot)
    {
        return Spawn(prefab, x, y, z, rot, 1);
    }

    public static GameObject Spawn(string prefab, float x, float y, float z, Quaternion rot, int count)
    {
        return Spawn(prefab, new Vector3(x, y, z), rot, count);
    }

    #endregion

    #region [Public] Spawn Object by Player position and rotation

    public static GameObject SpawnAtPlayer(string prefab, PlayerClient player)
    {
        IDMain idMain = player.controllable.idMain;
        var transform = idMain.transform;
        idMain.transform.GetGroundInfo(out var position, out var normal);
        var rotation = TransformHelpers.LookRotationForcedUp(player.transform.forward, normal);
        return Spawn(prefab, position, rotation, 1);
    }

    public static GameObject SpawnAtPlayer(string prefab, PlayerClient player, int count)
    {
        IDMain idMain = player.controllable.idMain;
        var transform = idMain.transform;
        idMain.transform.GetGroundInfo(out var position, out var normal);
        var rotation = TransformHelpers.LookRotationForcedUp(player.transform.forward, normal);
        return Spawn(prefab, position, rotation, count);
    }

    #endregion
}