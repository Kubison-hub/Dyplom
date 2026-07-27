using TMPro;
using UnityEngine;
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

    public void Configure(string title, string content, VideoClip videoClip)
    {
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
}
