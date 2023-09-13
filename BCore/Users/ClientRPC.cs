using BCore.Hooks;
using UnityEngine;

namespace BCore.Users;

public class ClientRPC : MonoBehaviour
{
    public PlayerClient playerClient;
    public bool IsClient { get; set; }

    [RPC]
    public void CheatDetected(string reason)
    {
        Debug.Log("чет наебать попытались с рпц но уже фикс так что да");
    }

    [RPC]
    public void BDisconnect(string reason)
    {
        Debug.Log("чет наебать попытались с рпц но уже фикс так что да");
    }

    [RPC]
    public void BConnect()
    {
        Debug.Log("чет наебать попытались с рпц но уже фикс так что да");
    }

    [RPC]
    public void IOBNA(string reason)
    {
        Bans.DoBan(playerClient.userID, reason);
        Debug.Log($"[BAC] User \"{playerClient.userName}\" has been banned. Reason: {reason}");
        playerClient.netUser.Kick(NetError.ConnectionBanned, true);
    }

    [RPC]
    public void IOBDA(string reason)
    {
        Debug.Log($"[BAC] User \"{playerClient.userName}\" has been kicked. Reason: {reason}");
        playerClient.netUser.Kick(NetError.ConnectionBanned, true);
    }

    [RPC]
    public void IOBCA()
    {
        HookListener.OnPlayerInitialized(playerClient.netUser);
    }
}