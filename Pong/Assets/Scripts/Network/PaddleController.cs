using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    private const int Spectator = 2;
    private float _serverAxis;
    private const double MinInputGap = 0.030;
    private double _lastInputTime = -1;
    public int RejectedInputs;
    private Func<string> rejectedPanel;
    public readonly SyncVar<int> Side = new SyncVar<int>(2);
    public static readonly List<PaddleController> All = new List<PaddleController>();
    public static PaddleController Local { get; private set; }
    [SerializeField] private float speed = 8f;

    public override void OnStartNetwork() => TimeManager.OnTick += OnTick;
    public override void OnStopNetwork() => TimeManager.OnTick -= OnTick;

    private void Awake() => Side.OnChange += SideChanged;
    private void SideChanged(int previous, int next, bool asServer) => Place();
    public static string SideName(int side) => side == 0 ? "LEFT" : side == 1 ? "RIGHT" : "SPECTATOR";
    public override void OnStartServer()
    {
        _serverAxis = 0f;
        _lastInputTime = -1;
        RejectedInputs = 0;
        rejectedPanel = () => $"Rejected inputs (paddle {OwnerId}): {RejectedInputs}";
        ConnectionHud.Panels.Add(rejectedPanel);
        if (!All.Contains(this)) All.Add(this);
        bool left = false, right = false;
        foreach (PaddleController paddle in All)
        {
            if (paddle == this || !paddle.IsServerInitialized) continue;
            left |= paddle.Side.Value == 0;
            right |= paddle.Side.Value == 1;
        }
        Side.Value = !left ? 0 : !right ? 1 : 2;
        Place();
        Debug.Log($"[Server] Client {OwnerId}: {SideName(Side.Value)}");
        GameState.Announce($"Client {OwnerId} joined: {SideName(Side.Value)}");
    }
    public override void OnStartClient()
    {
        if (!All.Contains(this)) All.Add(this);
        if (IsOwner)
        {
            Local = this;
            Debug.Log($"[Client] I am client {OwnerId}, side {SideName(Side.Value)}");
        }
        Place();
    }
    public override void OnStopServer()
    {
        ConnectionHud.Panels.Remove(rejectedPanel);
        rejectedPanel = null;
        _serverAxis = 0f;
        All.Remove(this);
    }
    public override void OnStopClient()
    {
        All.Remove(this);
        if (Local == this) Local = null;
    }
    private void OnDestroy() => Side.OnChange -= SideChanged;
    private void Place()
    {
        GetComponent<Renderer>().enabled = Side.Value < 2;
        if (Side.Value >= 2) return;
        transform.position = new Vector3(Side.Value == 0 ? -Court.PaddleX : Court.PaddleX, 0.5f, 0);
        Court.Paint(gameObject, Side.Value == 0 ? new Color(0.30f, 0.68f, 1f) : new Color(1f, 0.65f, 0.30f));
    }
    private void Update()
    {
        if (!IsOwner) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.spaceKey.wasPressedThisFrame) RequestServe();
    }
    private void OnTick()
    {
        if (IsOwner && Side.Value != Spectator)
        {
            float axis = 0f;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) axis++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) axis--;
            }
            ServerSetAxis(axis);
        }
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
        }
    }
    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
        if (float.IsNaN(axis)) return;
        axis = Mathf.Clamp(axis, -1f, 1f);
        if (Side.Value == Spectator) return;
        double now = TimeManager.TicksToTime(TimeManager.Tick);
        if (_lastInputTime >= 0 && now - _lastInputTime < MinInputGap)
        {
            RejectedInputs++;
            Debug.Log($"[Server] Rejected input from client {OwnerId}: too fast.");
            return;
        }
        _lastInputTime = now;
        _serverAxis = axis;
    }
    [ServerRpc]
    public void RequestServe()
    {
        if (GameState.Instance == null || NetworkBall.Instance == null || Side.Value >= 2) return;
        if (Side.Value != GameState.Instance.ServingSide.Value || NetworkBall.Instance.InPlay.Value) return;
        NetworkBall.Instance.Serve(1 - Side.Value);
    }
    [ObserversRpc]
    public void AnnounceGoal(int left, int right) => ConnectionHud.Flash($"{SideName(Side.Value)} scored! {left} : {right}");
    [TargetRpc]
    public void YouServe(NetworkConnection target) => ConnectionHud.Flash("YOU SERVE");
}
