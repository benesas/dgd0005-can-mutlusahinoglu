using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }
    public readonly SyncVar<int> LeftScore = new SyncVar<int>();
    public readonly SyncVar<int> RightScore = new SyncVar<int>();
    public readonly SyncVar<int> ServingSide = new SyncVar<int>(0);
    public readonly SyncList<string> Feed = new SyncList<string>();
    private Func<string> panel;
    private void Awake() => Instance = this;
    private void Start()
    {
        panel = () => $"LEFT {LeftScore.Value} : {RightScore.Value} RIGHT\nServe: {PaddleController.SideName(ServingSide.Value)}\n\n{string.Join("\n", Feed)}";
        ConnectionHud.Panels.Add(panel);
    }
    private void OnDestroy()
    {
        ConnectionHud.Panels.Remove(panel);
        if (Instance == this) Instance = null;
    }
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized || scorerSide < 0 || scorerSide > 1) return;
        if (scorerSide == 0) LeftScore.Value++; else RightScore.Value++;
        ServingSide.Value = 1 - scorerSide;
        Announce($"{PaddleController.SideName(scorerSide)} scored! {LeftScore.Value} : {RightScore.Value}");
        foreach (PaddleController paddle in PaddleController.All)
        {
            if (!paddle.IsServerInitialized) continue;
            if (paddle.Side.Value == scorerSide) paddle.AnnounceGoal(LeftScore.Value, RightScore.Value);
            if (paddle.Side.Value == ServingSide.Value) paddle.YouServe(paddle.Owner);
        }
    }
    public static void Announce(string line)
    {
        if (Instance == null || !Instance.IsServerInitialized) return;
        Instance.Feed.Add(line);
        if (Instance.Feed.Count > 6) Instance.Feed.RemoveAt(0);
    }
}
