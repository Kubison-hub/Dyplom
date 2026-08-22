using UnityEngine;

/// <summary>
/// Applies optional gameplay cursor overrides while preserving the project's CustomCursor default.
/// </summary>
public static class CustomCursorBridge
{
    public static void SetOverride(Texture2D texture, Vector2 hotSpot)
    {
        if (texture == null)
        {
            RestoreDefault();
            return;
        }

        Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
    }

    public static void RestoreDefault()
    {
        CustomCursor defaultCursor = Object.FindFirstObjectByType<CustomCursor>();
        if (defaultCursor != null)
            Cursor.SetCursor(defaultCursor.cursorTexture, defaultCursor.hotSpot, defaultCursor.cursorMode);
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
