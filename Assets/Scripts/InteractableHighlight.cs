using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class InteractableHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Materia³, który zast¹pi oryginalny materia³ bazowy.")]
    public Material highlightMaterial;

    // Dwie tablice materia³ów, które bêdziemy podmieniaæ:
    private Material[] originalMaterials;     // Oryginalny zestaw
    private Material[] highlightedMaterials;  // Zestaw z podmienionym materia³em [0]

    private Renderer rend;
    private bool isHighlighted = false;
    private bool canHighlight = true; // Flaga bezpieczeñstwa

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("InteractableHighlight nie znalaz³ komponentu Renderer!", this);
            canHighlight = false;
            return;
        }

        if (highlightMaterial == null)
        {
            Debug.LogWarning("Brak przypisanego 'highlightMaterial'! Podœwietlanie nie bêdzie dzia³aæ.", this);
            canHighlight = false;
            return;
        }

        // --- NOWA LOGIKA ---
        // 1. Zapisz oryginalne materia³y
        originalMaterials = rend.materials;

        if (originalMaterials.Length == 0)
        {
            Debug.LogWarning("Obiekt nie ma ¿adnych materia³ów do podœwietlenia.", this);
            canHighlight = false;
            return;
        }

        // 2. Stwórz "na zapas" zestaw materia³ów podœwietlonych
        // Klonujemy tablicê, aby nie modyfikowaæ orygina³u
        highlightedMaterials = (Material[])originalMaterials.Clone();

        // 3. PODMIEÑ materia³ na indeksie 0 na nasz materia³ podœwietlenia
        highlightedMaterials[0] = highlightMaterial;
        // --- KONIEC NOWEJ LOGIKI ---
    }

    /// <summary>
    /// Ta publiczna metoda bêdzie wywo³ywana przez HighlightManager.
    /// </summary>
    public void Highlight(bool show)
    {
        // Wykonaj akcjê tylko jeœli stan siê zmienia I mo¿emy podœwietlaæ
        if (show == isHighlighted || !canHighlight)
            return;

        // --- ZNACZNIE UPROSZCZONA LOGIKA ---
        if (show)
        {
            // Nie tworzymy nowych tablic, po prostu przypisujemy gotowy zestaw
            rend.materials = highlightedMaterials;
        }
        else
        {
            // Przywracamy oryginalny zestaw
            rend.materials = originalMaterials;
        }
        // --- KONIEC UPROSZCZENIA ---

        isHighlighted = show; // Zaktualizuj stan
    }
}