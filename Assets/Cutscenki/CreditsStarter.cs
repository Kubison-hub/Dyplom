using UnityEngine;

public class CreditsStarter : MonoBehaviour
{
    // Zmienna statyczna - jej wartoœæ "prze¿ywa" zmianê sceny
    public static bool showCreditsOnLoad = false;

    [Tooltip("Przeci¹gnij tutaj swój Panel z napisami koñcowymi")]
    public GameObject creditsPanel;

    void Start()
    {
        // Sprawdzamy, czy otrzymaliœmy sygna³ z Outro
        if (showCreditsOnLoad)
        {
            // W³¹czamy panel z napisami
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }

            // Resetujemy flagê, aby przy normalnym wejœciu do menu napisy siê nie w³¹cza³y
            showCreditsOnLoad = false;
        }
    }
}