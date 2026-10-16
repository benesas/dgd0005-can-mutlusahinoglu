using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkBall : NetworkBehaviour
{
    public static NetworkBall Instance { get; private set; }
    public readonly SyncVar<bool> InPlay = new SyncVar<bool>();
    [SerializeField] private float speed = 7f;
    private Vector3 velocity;
    private void Awake()
    {
        Instance = this;
        InPlay.OnChange += PlayChanged;
    }
    private void PlayChanged(bool previous, bool next, bool asServer) => Court.Paint(gameObject, next ? Color.white : Color.gray);
    public override void OnStartClient() => PlayChanged(false, InPlay.Value, false);
    public override void OnStartServer() => PlayChanged(false, InPlay.Value, true);
    private void OnDestroy()
    {
        InPlay.OnChange -= PlayChanged;
        if (Instance == this) Instance = null;
    }
    public void Serve(int towardSide)
    {
        if (!IsServerInitialized || InPlay.Value) return;
        velocity = new Vector3(towardSide == 0 ? -1f : 1f, 0, Random.Range(-0.6f, 0.6f)).normalized * speed;
        InPlay.Value = true;
        GameState.Announce($"{PaddleController.SideName(1 - towardSide)} served");
    }
    private void Update()
    {
        if (!IsServerInitialized) return;
        if (!InPlay.Value) return;
        float remaining = Time.deltaTime;
        while (remaining > 0 && InPlay.Value)
        {
            float step = Mathf.Min(remaining, 0.01f);
            Move(step);
            remaining -= step;
        }
    }
    private void Move(float delta)
    {
        Vector3 p = transform.position + velocity * delta;
        float zLimit = Court.HalfHeight - Court.BallRadius;
        if (Mathf.Abs(p.z) > zLimit)
        {
            p.z = Mathf.Sign(p.z) * zLimit;
            velocity.z = -velocity.z;
        }
        foreach (PaddleController paddle in PaddleController.All)
        {
            if (!paddle.IsServerInitialized || paddle.Side.Value >= 2) continue;
            Vector3 pp = paddle.transform.position;
            bool towards = (pp.x < 0 && velocity.x < 0) || (pp.x > 0 && velocity.x > 0);
            if (!towards) continue;
            if (Mathf.Abs(p.x - pp.x) < 0.5f && Mathf.Abs(p.z - pp.z) < Court.PaddleHalfLength + Court.BallRadius)
            {
                velocity.x = -velocity.x;
                velocity.z += (p.z - pp.z) * 3f;
                velocity = velocity.normalized * speed;
                p.x = pp.x + (pp.x < 0 ? 0.5f : -0.5f);
            }
        }
        transform.position = p;
        if (Mathf.Abs(p.x) <= Court.HalfWidth + 0.5f) return;
        int scorer = p.x < 0 ? 1 : 0;
        velocity = Vector3.zero;
        InPlay.Value = false;
        transform.position = new Vector3(0, Court.BallY, 0);
        GameState.Instance.Goal(scorer);
    }
}
