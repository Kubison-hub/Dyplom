using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryFigureDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ItemType figureType;
    [SerializeField] private Canvas dragCanvas;

    private CanvasGroup canvasGroup;
    private Image sourceImage;
    private RectTransform dragVisual;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        sourceImage = GetComponent<Image>();

        if (dragCanvas == null)
            dragCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        canvasGroup.blocksRaycasts = false;
        CreateDragVisual(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
            dragVisual.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragVisual == null)
            return;

        bool placed = TryPlaceInWorld(eventData.position);
        Destroy(dragVisual.gameObject);
        dragVisual = null;
        canvasGroup.blocksRaycasts = true;

        if (placed)
            gameObject.SetActive(false);
    }

    private bool CanDrag()
    {
        return TableFigurePuzzle.ActivePuzzle != null &&
               TableFigurePuzzle.ActivePuzzle.IsPuzzleActive &&
               InventoryManager.Instance != null &&
               InventoryManager.Instance.items.Contains(figureType);
    }

    private bool TryPlaceInWorld(Vector2 screenPosition)
    {
        if (Camera.main == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        foreach (RaycastHit hit in hits)
        {
            TableFigureSlot slot = hit.collider.GetComponentInParent<TableFigureSlot>();
            if (slot != null && slot.TryPlace(figureType))
                return true;
        }

        return false;
    }

    private void CreateDragVisual(Vector2 screenPosition)
    {
        if (dragCanvas == null || sourceImage == null)
            return;

        GameObject visual = new GameObject(name + "_DragVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragVisual = visual.GetComponent<RectTransform>();
        dragVisual.SetParent(dragCanvas.transform, false);
        dragVisual.sizeDelta = sourceImage.rectTransform.sizeDelta;
        dragVisual.position = screenPosition;

        Image visualImage = visual.GetComponent<Image>();
        visualImage.sprite = sourceImage.sprite;
        visualImage.color = new Color(1f, 1f, 1f, 0.8f);
        visualImage.raycastTarget = false;
    }
}
