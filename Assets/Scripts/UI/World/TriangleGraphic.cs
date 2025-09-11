// Assets/Scripts/UI/World/TriangleGraphic.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TriangleGraphic : Graphic
{
    public enum Dir { Up, Down, Left, Right }
    public Dir direction = Dir.Down;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        switch (direction)
        {
            case Dir.Down:
                v.position = new Vector2(r.xMin, r.yMax); vh.AddVert(v);
                v.position = new Vector2(r.xMax, r.yMax); vh.AddVert(v);
                v.position = new Vector2((r.xMin + r.xMax) * 0.5f, r.yMin); vh.AddVert(v);
                vh.AddTriangle(0,1,2);
                break;
            case Dir.Up:
                v.position = new Vector2(r.xMin, r.yMin); vh.AddVert(v);
                v.position = new Vector2(r.xMax, r.yMin); vh.AddVert(v);
                v.position = new Vector2((r.xMin + r.xMax) * 0.5f, r.yMax); vh.AddVert(v);
                vh.AddTriangle(0,1,2);
                break;
            case Dir.Left:
                v.position = new Vector2(r.xMax, r.yMin); vh.AddVert(v);
                v.position = new Vector2(r.xMax, r.yMax); vh.AddVert(v);
                v.position = new Vector2(r.xMin, (r.yMin + r.yMax) * 0.5f); vh.AddVert(v);
                vh.AddTriangle(0,1,2);
                break;
            case Dir.Right:
                v.position = new Vector2(r.xMin, r.yMin); vh.AddVert(v);
                v.position = new Vector2(r.xMin, r.yMax); vh.AddVert(v);
                v.position = new Vector2(r.xMax, (r.yMin + r.yMax) * 0.5f); vh.AddVert(v);
                vh.AddTriangle(0,1,2);
                break;
        }
    }
}