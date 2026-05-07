using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILine : MaskableGraphic
{
    [SerializeField] private Vector2 startPoint;
    [SerializeField] private Vector2 endPoint;
    [SerializeField] private float thickness = 8f;

    public void SetPoints(Vector2 start, Vector2 end)
    {
        startPoint = start;
        endPoint = end;
        SetVerticesDirty();
    }

    public void SetThickness(float value)
    {
        thickness = Mathf.Max(0.1f, value);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Vector2 direction = endPoint - startPoint;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
        Vector2 offset = normal * (thickness * 0.5f);

        Vector2 v0 = startPoint - offset;
        Vector2 v1 = startPoint + offset;
        Vector2 v2 = endPoint + offset;
        Vector2 v3 = endPoint - offset;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = v0;
        vh.AddVert(vertex);

        vertex.position = v1;
        vh.AddVert(vertex);

        vertex.position = v2;
        vh.AddVert(vertex);

        vertex.position = v3;
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }
}