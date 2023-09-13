using System;
using System.Collections;
using System.Text;
using System.Threading;
using BClient.AntiCheat;
using RakNet;
using RakNet.Utils;
using UnityEngine;

namespace BClient.UserReferences;

public class SocketConnection : MonoBehaviour
{
    private static NetPeer _peer;
    private static NetManager _client;
    
    private IEnumerator Start()
    {
        var listener = new EventBasedNetListener();
        _client = new NetManager(listener);
        
        _client.Start();
        _client.Connect(PlayerPrefs.GetString("net.lasturl").Split(':')[0], int.Parse(PlayerPrefs.GetString("net.lasturl").Split(':')[1]) + 2, "TjZIN01KVllDVjAtUFExMjM5VzY30JrQm9Cu0KfQk9Ce0JLQndCQ0J3QkNCl0KPQmSkpKSDQmtCi0J4g0J/QoNCe0KfQmNCi0JDQmyDQotCe0KIg0JPQldCZ");

        listener.PeerConnectedEvent += netPeer =>
        {
            _peer = netPeer;
            SendPacket("Default", "Connected");
            var pizada = Connector.GetDiskId();
            var gavno = Connector.GetCpuId();
            var kartonka = pizada + gavno;

            var sb = new StringBuilder(kartonka);

            sb[sb.Length / 2] = '-';
            SendPacket("hwid", sb.ToString());
        };
        listener.NetworkReceiveEvent += (_, dataReader, _) =>
        {
            var rawMessage = dataReader.GetString(258);

            var rM = rawMessage.Replace("<", "");
            var sRm = rM.Split('>');

            var packet = sRm[0];
            var value = sRm[1];
            
            OnPacketReceive(packet, value);
            
            dataReader.Recycle();
        };

        while (true)
        {
            if (!NetCull.isClientRunning)
            {
                Destroy(this);
                yield break;
            }
            
            _client.PollEvents();
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void OnDestroy()
    {
        _client.DisconnectPeer(_peer);
        _client.Stop();
    }

    private static void OnPacketReceive(string packet, object value)
    {
        switch (packet)
        {
            case "UpdatePing":
            {
                AssemblyHandler.Ping = int.Parse(value.ToString());
                return;
            }
        }
    }
    internal static void SendPacket(string packet, object value)
    {
        var writer = new NetDataWriter();
        writer.Put($"<{packet}>{value}");
        _peer.Send(writer, DeliveryMethod.ReliableUnordered);
    }
}