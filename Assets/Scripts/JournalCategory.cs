using UnityEngine;
using UnityEngine.UI;

public class JournalCategory : MonoBehaviour
{
    [Header("Referencje UI")]
    [Tooltip("Przycisk g³ównej kategorii (np. 'Osoby')")]
    public Button categoryButton;

    [Tooltip("Obiekt zawieraj¹cy listê wpisów (ten bêdzie ukrywany/pokazywany)")]
    public GameObject contentContainer;

    [Tooltip("Ikonka strza³ki (opcjonalnie)")]
    public RectTransform arrowIcon;

    [Header("Ustawienia")]
    [Tooltip("Czy kategoria ma byæ rozwiniêta na starcie?")]
    public bool isExpanded = false;

    private void Start()
    {
        // Przypisanie akcji do przycisku
        if (categoryButton != null)
        {
            categoryButton.onClick.AddListener(ToggleCategory);
        }

        // Ustawienie stanu pocz¹tkowego
        UpdateState();
    }

    // Funkcja wywo³ywana po klikniêciu w kategoriê
    private void ToggleCategory()
    {
        isExpanded = !isExpanded;
        UpdateState();

        // Wymuszenie odœwie¿enia uk³adu, aby ScrollView od razu dopasowa³o swój rozmiar
        if (contentContainer.transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer.transform.parent.GetComponent<RectTransform>());
        }
    }

    // Aktualizacja wygl¹du UI
    private void UpdateState()
    {
        // W³¹cz lub wy³¹cz kontener z notatkami
        if (contentContainer != null)
        {
            contentContainer.SetActive(isExpanded);
        }

        // Obróæ strza³kê (0 stopni gdy zwiniête, -90 w dó³ gdy rozwiniête)
        if (arrowIcon != null)
        {
            float targetAngle = isExpanded ? -90f : 0f;
            arrowIcon.localRotation = Quaternion.Euler(0, 0, targetAngle);
        }
    }
}