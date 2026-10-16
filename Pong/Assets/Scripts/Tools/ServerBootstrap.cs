using System;
using System.Linq;
using FishNet;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    private void Start()
    {
        bool server = Array.IndexOf(Environment.GetCommandLineArgs(), "-server") >= 0;
        #if UNITY_EDITOR
        server |= Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.Contains("Server");
        #endif
        if (server) InstanceFinder.ServerManager.StartConnection();
    }
}
