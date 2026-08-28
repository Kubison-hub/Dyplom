using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

/// <summary>
/// Visual template for every tutorial popup. The caller supplies its text and media.
/// </summary>
public class TutorialPopupWindow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private VideoPlayer tutorialVideoPlayer;
    [SerializeField] private GameObject tutorialVideoContainer;

    private float openedAtUnscaledTime;

    public void Configure(string title, string content, VideoClip videoClip)
    {
        PlayerTopText.Instance?.ClearAllTopText();
        openedAtUnscaledTime = Time.unscaledTime;

        if (titleText != null)
            titleText.text = title;

        if (contentText != null)
            contentText.text = content;

        if (tutorialVideoPlayer == null)
            return;

        tutorialVideoPlayer.Stop();
        tutorialVideoPlayer.clip = videoClip;

        if (tutorialVideoContainer != null)
            tutorialVideoContainer.SetActive(videoClip != null);

        tutorialVideoPlayer.enabled = videoClip != null;

        if (videoClip != null)
        {
            tutorialVideoPlayer.isLooping = true;
            tutorialVideoPlayer.Play();
        }
    }

    private void Update()
    {
        // A popup must be dismissible even when another tutorial state suppresses TutorialTimeline.Update.
        if (Time.unscaledTime - openedAtUnscaledTime < 0.15f)
            return;

        bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (leftClick || escape || Input.GetMouseButtonDown(0))
            TutorialTimeline.Instance?.CloseGameplayTutorialPopup();
    }
}
