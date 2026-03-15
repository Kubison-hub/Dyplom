using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    [Tooltip("Przeci¹gnij tutaj teskturê kursora")]
    public Texture2D cursorTexture;

    [Tooltip("Punkt klikniêcia kursora")]
    public Vector2 hotSpot = Vector2.zero;

    public CursorMode cursorMode = CursorMode.Auto;

    private void Start()
    {
        //Ustawia w³asny kursor na starcie gry
        Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }

    private void OnDisable()
    {
        //przywraca domyœlny kursor, gdy skrypt jest wy³¹czany
        Cursor.SetCursor(null, Vector2.zero, cursorMode);
    }
}
