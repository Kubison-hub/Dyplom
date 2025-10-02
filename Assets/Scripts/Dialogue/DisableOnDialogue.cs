using UnityEngine;

[DisallowMultipleComponent]
public class DisableOnDialogue : MonoBehaviour
{
    [Tooltip("Behaviour (component) to disable during dialogue. If left empty, this GameObject will be set inactive.")]
    public Behaviour behaviourToDisable;

    private bool wasActiveBefore = true;

    private void OnEnable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += OnStart;
            DialogueManager.Instance.OnDialogueEnded += OnEnd;
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= OnStart;
            DialogueManager.Instance.OnDialogueEnded -= OnEnd;
        }
    }

    private void OnStart()
    {
        if (behaviourToDisable != null)
        {
            behaviourToDisable.enabled = false;
        }
        else
        {
            wasActiveBefore = gameObject.activeSelf;
            gameObject.SetActive(false);
        }
    }

    private void OnEnd()
    {
        if (behaviourToDisable != null)
        {
            behaviourToDisable.enabled = true;
        }
        else
        {
            gameObject.SetActive(wasActiveBefore);
        }
    }
}
