using UnityEngine;

public class Court : MonoBehaviour
{
    public const float HalfWidth = 8f, HalfHeight = 5f, PaddleX = 7f;
    public const float PaddleHalfLength = 1f, BallRadius = 0.25f, BallY = 0.25f;
    private static Material material;
    private static MaterialPropertyBlock colors;

    private void Awake()
    {
        Make("Floor", new Vector3(0, -0.1f, 0), new Vector3(16, 0.2f, 10), new Color(0.10f, 0.12f, 0.16f));
        Make("Top Wall", new Vector3(0, 0.25f, 5.25f), new Vector3(17, 0.5f, 0.5f), Color.gray);
        Make("Bottom Wall", new Vector3(0, 0.25f, -5.25f), new Vector3(17, 0.5f, 0.5f), Color.gray);
    }

    private void Make(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.localScale = scale;
        Destroy(go.GetComponent<Collider>());
        Paint(go, color);
    }

    public static void ClampPaddle(Transform paddle)
    {
        Vector3 p = paddle.position;
        p.z = Mathf.Clamp(p.z, -HalfHeight + PaddleHalfLength, HalfHeight - PaddleHalfLength);
        paddle.position = p;
    }

    public static void Paint(GameObject go, Color color)
    {
        if (!go.TryGetComponent(out Renderer renderer)) return;
        if (material == null) material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (colors == null) colors = new MaterialPropertyBlock();
        renderer.sharedMaterial = material;
        colors.SetColor("_BaseColor", color);
        renderer.SetPropertyBlock(colors);
    }
}
