using System;
using System.Collections;
using BClient.AntiCheat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BClient.UserReferences;

public class PlayerChecker : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadElements());
    }
    private static IEnumerator LoadElements()
    {
        while (PlayerClient.GetLocalPlayer() == null) yield return new WaitForSeconds(1.0f);
        
        var playerClient = PlayerClient.GetLocalPlayer();
        Debug.Log("[color purple][BAC]: Connected to BAC server.");
            
        playerClient.gameObject.AddComponent<AssemblyHandler>();
        playerClient.gameObject.AddComponent<FastLoot>();
        playerClient.gameObject.AddComponent<FPSBooster>();
    }
}