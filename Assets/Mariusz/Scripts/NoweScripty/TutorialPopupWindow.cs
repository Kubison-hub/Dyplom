using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
    private RawImage tutorialVideoImage;
    private CanvasGroup popupCanvasGroup;
    private bool videoConfigured;

    private void Awake()
    {
        popupCanvasGroup = GetComponent<CanvasGroup>();
        if (popupCanvasGroup == null)
            popupCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (tutorialVideoPlayer != null)
            tutorialVideoImage = tutorialVideoPlayer.GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        if (videoConfigured)
            PrepareVideo();
    }

    private void OnDisable()
    {
        StopAndReleaseVideo();
    }

    private void OnDestroy()
    {
        StopAndReleaseVideo();
    }

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

        UnsubscribeFromVideoEvents();
        tutorialVideoPlayer.Stop();
        tutorialVideoPlayer.playOnAwake = false;
        tutorialVideoPlayer.waitForFirstFrame = true;
        tutorialVideoPlayer.clip = videoClip;
        videoConfigured = videoClip != null;

        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = videoConfigured ? 0f : 1f;

        if (tutorialVideoContainer != null)
            tutorialVideoContainer.SetActive(videoClip != null);

        if (tutorialVideoImage != null)
            tutorialVideoImage.enabled = false;

        tutorialVideoPlayer.enabled = videoClip != null;

        if (videoConfigured && isActiveAndEnabled)
            PrepareVideo();
    }

    private void PrepareVideo()
    {
        if (tutorialVideoPlayer == null || tutorialVideoPlayer.clip == null)
            return;

        UnsubscribeFromVideoEvents();
        tutorialVideoPlayer.prepareCompleted += HandleVideoPrepared;
        tutorialVideoPlayer.frameReady += HandleFirstVideoFrameReady;
        tutorialVideoPlayer.sendFrameReadyEvents = true;
        tutorialVideoPlayer.isLooping = true;
        tutorialVideoPlayer.Prepare();
    }

    private void HandleVideoPrepared(VideoPlayer source)
    {
        source.Play();
    }

    private void HandleFirstVideoFrameReady(VideoPlayer source, long frameIndex)
    {
        if (tutorialVideoImage != null)
            tutorialVideoImage.enabled = true;

        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = 1f;

        source.sendFrameReadyEvents = false;
        UnsubscribeFromVideoEvents();
    }

    private void UnsubscribeFromVideoEvents()
    {
        if (tutorialVideoPlayer == null)
            return;

        tutorialVideoPlayer.prepareCompleted -= HandleVideoPrepared;
        tutorialVideoPlayer.frameReady -= HandleFirstVideoFrameReady;
    }

    public void StopAndReleaseVideo()
    {
        UnsubscribeFromVideoEvents();

        if (tutorialVideoPlayer != null)
        {
            tutorialVideoPlayer.sendFrameReadyEvents = false;
            tutorialVideoPlayer.Stop();
            tutorialVideoPlayer.clip = null;
            tutorialVideoPlayer.enabled = false;
        }

        videoConfigured = false;

        if (tutorialVideoImage != null)
            tutorialVideoImage.enabled = false;

        if (tutorialVideoContainer != null)
            tutorialVideoContainer.SetActive(false);
    }

    private void Update()
    {
        // A popup must be dismissible even when another tutorial state suppresses TutorialTimeline.Update.
        if (Time.unscaledTime - openedAtUnscaledTime < 0.15f)
            return;

        bool tab = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        if (tab)
            TutorialTimeline.Instance?.CloseGameplayTutorialPopup();
    }
}
