using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Editing.NetworkProfiler;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using UnityEngine;

public class ConnectionHud : MonoBehaviour
{
    public static readonly List<Func<string>> Panels = new List<Func<string>>();
    private static string message = "";
    private static float flashUntil;
    private NetworkManager manager;
    private NetworkTrafficStatistics statistics;
    private string address = "127.0.0.1";
    private double serverIn, serverOut, clientIn, clientOut;
    private double inServer, outServer, inClient, outClient;
    private float sampleAt;
    private static readonly FieldInfo inbound = typeof(BidirectionalNetworkTraffic).GetField("InboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo outbound = typeof(BidirectionalNetworkTraffic).GetField("OutboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo bytes = inbound.FieldType.GetField("Bytes");
    public static void Flash(string text)
    {
        message = Time.unscaledTime < flashUntil ? message + "\n" + text : text;
        flashUntil = Time.unscaledTime + 3f;
    }
    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
        if (manager.StatisticsManager.TryGetNetworkTrafficStatistics(out statistics)) statistics.OnNetworkTraffic += Traffic;
        sampleAt = Time.unscaledTime;
    }
    private void Traffic(uint tick, BidirectionalNetworkTraffic server, BidirectionalNetworkTraffic client)
    {
        inServer += Count(server, inbound); outServer += Count(server, outbound);
        inClient += Count(client, inbound); outClient += Count(client, outbound);
    }
    private static ulong Count(BidirectionalNetworkTraffic traffic, FieldInfo direction)
    {
        object data = direction.GetValue(traffic);
        return data == null ? 0 : (ulong)bytes.GetValue(data);
    }
    private void Update()
    {
        float duration = Time.unscaledTime - sampleAt;
        if (duration < 1f) return;
        serverIn = inServer / duration; serverOut = outServer / duration;
        clientIn = inClient / duration; clientOut = outClient / duration;
        inServer = outServer = inClient = outClient = 0;
        sampleAt = Time.unscaledTime;
    }
    private void OnDestroy()
    {
        if (statistics != null) statistics.OnNetworkTraffic -= Traffic;
    }
    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(new Rect(12, 12, 270, Screen.height - 24));
        address = GUILayout.TextField(address);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(manager.IsServerStarted ? "Stop Server" : "Start Server"))
        {
            if (manager.IsServerStarted) manager.ServerManager.StopConnection(true); else manager.ServerManager.StartConnection();
        }
        if (GUILayout.Button(manager.IsClientStarted ? "Stop Client" : "Start Client"))
        {
            if (manager.IsClientStarted) manager.ClientManager.StopConnection(); else manager.ClientManager.StartConnection(address, 7770);
        }
        GUILayout.EndHorizontal();
        GUILayout.Label($"RTT {manager.TimeManager.RoundTripTime} ms | Tick {manager.TimeManager.Tick}");
        if (manager.IsServerStarted) GUILayout.Label($"Clients {manager.ServerManager.Clients.Count} | Server in/out {serverIn:0}/{serverOut:0} B/s");
        if (manager.IsClientStarted) GUILayout.Label($"Client in/out {clientIn:0}/{clientOut:0} B/s");
        foreach (Func<string> panel in Panels) GUILayout.Label(panel());
        if (Time.unscaledTime < flashUntil) GUILayout.Label(message);
        GUILayout.EndArea();
    }
}
