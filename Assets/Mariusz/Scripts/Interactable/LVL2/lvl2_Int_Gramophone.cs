using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class lvl2_Int_Gramophone : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;

    public bool isPlaying = false;

    public AudioSource audioSource;

    public Animator[] animators;

    [SerializeField] AudioSource audioGramStart;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        //Debug.Log(interactable.name + ", interaction Performed");

        StartCoroutine(ToggleMusic());
        player.currentInteractable = null;

    }

    private IEnumerator ToggleMusic()
    {

        if (!isPlaying)
        {
            interactable.isInteractableActive = false;
            audioGramStart.Play();
            audioSource.Play();

            foreach (var animator in animators)
            {
                animator.SetBool("IsPlaying", true);
            }

            
            isPlaying = true;
            
            yield return new WaitForSeconds(3);
            interactable.isInteractableActive = true;
        }
        else
        {
            interactable.isInteractableActive = false;
            audioGramStart.Play();
            audioSource.Stop();
            foreach (var animator in animators)
            {
                animator.SetBool("IsPlaying", false);
            }

            isPlaying = false;

            yield return new WaitForSeconds(3);
            interactable.isInteractableActive = true;
        }

    }


}
