using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Wymagane dla Korutyn

public class ComicCutscene : MonoBehaviour
{
    [Header("Ustawienia Komiksu")]
    public Sprite[] pages;
    public string nextSceneName;

    [Tooltip("Czas trwania zanikania i pojawiania siê (w sekundach)")]
    public float fadeDuration = 0.5f;

    [Header("Referencje UI")]
    public Image comicDisplay;
    public Button nextButton;
    public Button prevButton;
    public Button actionButton;

    [Tooltip("Zaznacz to TYLKO w scenie Outro, aby odpaliæ napisy w Main Menu")]
    public bool isOutro = false;

    private int currentPageIndex = 0;
    private bool isFading = false; // Zapobiega spamowaniu przycisków

    void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
        actionButton.onClick.AddListener(FinishCutscene);

        // Ustaw pierwsz¹ stronê bez animacji na start
        if (pages.Length > 0)
        {
            comicDisplay.sprite = pages[currentPageIndex];
        }
        UpdateUI();
    }

    void NextPage()
    {
        if (currentPageIndex < pages.Length - 1 && !isFading)
        {
            StartCoroutine(FadeToPage(currentPageIndex + 1));
        }
    }

    void PrevPage()
    {
        if (currentPageIndex > 0 && !isFading)
        {
            StartCoroutine(FadeToPage(currentPageIndex - 1));
        }
    }

    void FinishCutscene()
    {
        if (!isFading)
        {
            // Zaznacz pole w Inspektorze, jeœli ta scena komiksu to Outro
            if (isOutro)
            {
                CreditsStarter.showCreditsOnLoad = true;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator FadeToPage(int newIndex)
    {
        isFading = true;

        // 1. Zablokuj klikanie przycisków na czas animacji
        nextButton.interactable = false;
        prevButton.interactable = false;
        actionButton.interactable = false;

        // 2. Fade Out (przyciemnianie obecnej strony)
        float elapsedTime = 0f;
        Color c = comicDisplay.color;

        while (elapsedTime < fadeDuration)
        {
            // P³ynne przejœcie Alpha od 1 (w pe³ni widoczne) do 0 (niewidoczne)
            c.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            comicDisplay.color = c;
            elapsedTime += Time.deltaTime;
            yield return null; // Czekaj do nastêpnej klatki
        }
        c.a = 0f;
        comicDisplay.color = c;

        // 3. Zmiana grafiki na now¹, gdy obraz jest w pe³ni przezroczysty
        currentPageIndex = newIndex;
        comicDisplay.sprite = pages[currentPageIndex];

        // 4. Fade In (pojawianie siê nowej strony)
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            // P³ynne przejœcie Alpha od 0 do 1
            c.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            comicDisplay.color = c;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        c.a = 1f;
        comicDisplay.color = c;

        // 5. Zakoñczenie animacji i przywrócenie UI
        isFading = false;
        UpdateUI();
    }

    void UpdateUI()
    {
        // Kontrolujemy widocznoœæ i interaktywnoœæ przycisków
        bool isFirstPage = (currentPageIndex == 0);
        bool isLastPage = (currentPageIndex == pages.Length - 1);

        prevButton.gameObject.SetActive(!isFirstPage);
        prevButton.interactable = true;

        if (isLastPage)
        {
            nextButton.gameObject.SetActive(false);
            actionButton.gameObject.SetActive(true);
            actionButton.interactable = true;
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            actionButton.gameObject.SetActive(false);
        }
    }
}