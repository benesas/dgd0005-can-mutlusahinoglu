using FishNet.Object;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    public NetworkObject BallPrefab;
    public override void OnStartServer()
    {
        NetworkObject ball = Instantiate(BallPrefab, new Vector3(0, Court.BallY, 0), Quaternion.identity);
        Spawn(ball.gameObject);
    }
}
