using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;
using System.Collections.Generic;

[RequireComponent(typeof(Graphic))]
[AddComponentMenu("UI/Effects/Gradient Effect")]
public class UIGradient : BaseMeshEffect
{
    public enum Direction
    {
        Vertical,
        Horizontal
    }

    [Header("Цвета градиента")]
    public Color colorTop    = Color.white;   // цвет для "верха" (или левого, если горизонтально)
    public Color colorBottom = Color.black;   // цвет для "низа" (или правого)

    [Header("Направление градиента")]
    public Direction direction = Direction.Vertical;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
            return;

        // Собираем все вершины
        var verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        // Находим min/max координаты по нужной оси
        float minY = float.MaxValue, maxY = float.MinValue;
        float minX = float.MaxValue, maxX = float.MinValue;

        for (int i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            Vector3 pos = v.position;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
        }

        float height = maxY - minY;
        float width  = maxX - minX;

        // Обновляем цвет каждой вершины
        for (int i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            float t = 0f;

            if (direction == Direction.Vertical)
            {
                // нормируем по вертикали
                t = (v.position.y - minY) / height;
            }
            else
            {
                // нормируем по горизонтали
                t = (v.position.x - minX) / width;
            }

            // смешиваем цвета
            v.color = Color.Lerp(colorBottom, colorTop, t);

            verts[i] = v;
        }

        // Перезаписываем вершины обратно в Mesh
        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        // чтобы редактор сразу перерисовал
        graphic.SetVerticesDirty();
    }
    #endif
}
