using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Fade Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradientFade : BaseMeshEffect
{
    [Header("Ustawienia koloru")]
    [Tooltip("Kolor po lewej stronie")]
    public Color leftColor = Color.black;

    [Tooltip("Kolor po prawej stronie")]
    public Color rightColor = new Color(0f, 0f, 0f, 0f); // Czarny, ale z kana³em Alfa na 0 (przezroczysty)

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        // Szukamy skrajnych punktów szerokoœci obiektu
        float leftX = vh.currentVertCount > 0 ? float.MaxValue : 0;
        float rightX = vh.currentVertCount > 0 ? float.MinValue : 0;

        UIVertex vertex = new UIVertex();

        // Pierwsza pêtla: ustalamy szerokoœæ wierzcho³ków obrazka
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.x < leftX) leftX = vertex.position.x;
            if (vertex.position.x > rightX) rightX = vertex.position.x;
        }

        float width = rightX - leftX;

        // Zabezpieczenie przed b³êdem dzielenia przez zero
        if (width <= 0) return;

        // Druga pêtla: kolorujemy ka¿dy wierzcho³ek
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            // Obliczamy pozycjê od 0 (lewo) do 1 (prawo)
            float t = (vertex.position.x - leftX) / width;

            // Nak³adamy kolor na podstawie pozycji
            vertex.color = Color.Lerp(leftColor, rightColor, t);

            vh.SetUIVertex(vertex, i);
        }
    }
}