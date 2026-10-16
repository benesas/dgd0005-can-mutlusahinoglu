using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class ConnectionLogger : MonoBehaviour
{
    private NetworkManager manager;
    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
        manager.ServerManager.OnServerConnectionState += ServerState;
        manager.ServerManager.OnRemoteConnectionState += RemoteState;
        manager.ClientManager.OnClientConnectionState += ClientState;
    }
    private void ServerState(ServerConnectionStateArgs args) => Debug.Log($"[Server] {args.ConnectionState}");
    private void ClientState(ClientConnectionStateArgs args) => Debug.Log($"[Client] {args.ConnectionState}");
    private void RemoteState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        Debug.Log($"[Server] Client {conn.ClientId} {args.ConnectionState}; clients {manager.ServerManager.Clients.Count}");
        if (args.ConnectionState == RemoteConnectionState.Stopped)
            GameState.Announce($"Client {conn.ClientId} left");
    }
    private void OnDestroy()
    {
        if (manager == null) return;
        manager.ServerManager.OnServerConnectionState -= ServerState;
        manager.ServerManager.OnRemoteConnectionState -= RemoteState;
        manager.ClientManager.OnClientConnectionState -= ClientState;
    }
}
