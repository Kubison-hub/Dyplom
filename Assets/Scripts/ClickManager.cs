using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public static bool clickBlocked = false;

    void LateUpdate()
    {
        // Resetujemy blokadê na koñcu ka¿dej klatki
        clickBlocked = false;
    }
}
