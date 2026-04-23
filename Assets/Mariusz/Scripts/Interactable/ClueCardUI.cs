using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClueCardUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    public CanvasGroup canvasGroup;

    public void Setup(string title, string description, Sprite icon = null)
    {
        titleText.text = title;
        descriptionText.text = description;
        if (iconImage != null && icon != null) iconImage.sprite = icon;

        // Opcjonalnie: Start animacji pojawiania siê
        StartCoroutine(FadeInAndOut());
    }

    private System.Collections.IEnumerator FadeInAndOut()
    {
        // Prosty Fade In
        float timer = 0;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = timer / 0.5f;
            yield return null;
        }

        yield return new WaitForSeconds(4f); // Czas wyœwietlania

        // Prosty Fade Out
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            canvasGroup.alpha = timer / 0.5f;
            yield return null;
        }
        Destroy(gameObject);
    }
}