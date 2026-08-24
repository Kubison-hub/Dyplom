using UnityEngine;

/// <summary>
/// One scene-level source of character voices for dialogue-line interactions.
/// Assign these character sources once on a persistent scene object.
/// </summary>
[DisallowMultipleComponent]
public class DialogueAudioRegistry : MonoBehaviour
{
    public static DialogueAudioRegistry Instance { get; private set; }

    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;
    [SerializeField] private AudioSource violetVoiceSource;

    public AudioSource SherlockVoiceSource => sherlockVoiceSource;
    public AudioSource WatsonVoiceSource => watsonVoiceSource;
    public AudioSource SelmaVoiceSource => selmaVoiceSource;
    public AudioSource VioletVoiceSource => violetVoiceSource;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
