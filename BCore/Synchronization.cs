using System.Collections;
using BCore.Users;
using UnityEngine;

namespace BCore;

public class Synchronization : MonoBehaviour
{
    public Synchronization instance;

    private void Start()
    {
        instance = this;
        StartCoroutine(SynchronizeBans());
    } // ReSharper disable Unity.PerformanceAnalysis
    public static IEnumerator GetPlayerRPCs(NetUser user)
    {
        while (user.playerClient is null) yield return new WaitForSeconds(1.0f);
        var clientRPC = user.playerClient.gameObject.AddComponent<ClientRPC>();
        clientRPC.playerClient = user.playerClient;

        //yield return new WaitForSeconds(25.0f);
        if (clientRPC.IsClient) yield break;
        //Debug.Log($"[BAC] User \"{user.playerClient.userName}\" Anti-Cheat connection not found.");
        //user.playerClient.netUser.Kick(NetError.Facepunch_Connector_AuthTimeout, true);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private static IEnumerator SynchronizeBans()
    {
        while (true)
        {
            Bans.InitBans();
            yield return new WaitForSeconds(5 * 60);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}