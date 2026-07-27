using System.Collections;
using UnityEngine;

public class lvl2_Int_StairsExit : MonoBehaviour
{
    private Interactable interactable;

    public string text = "Nie ma sensu na razie wracaæ na dó³";
    public string text2 = "Korci mnie aby sprawdziæ, czy jest tu gdzieœ drugie wyjœcie";
    public string text3 = "Powiniem sprawdziæ dok¹d prowadzi tajne przejœcie z pokoju Ethel";

    public string allLettersText = "Chyba mam wszystko, czego tutaj potrzebujê";
    public string ethelText = "Muszê jeszcze znaleŸæ ma³¹ Ethel";

    public bool performed = false;
    public bool ethelFounded = false;

    [SerializeField] private int requiredLetters = 3;
    private int lettersCollected = 0;

    
    public bool hiddenDoorDiscovered = false;
    public bool canExitByStairs = false;

    public GameObject level_2;
    public GameObject level_1;


    public Transform level1StartPoint;
    public void AddLetter()
    {
        lettersCollected++;
        if (HasAllLetters())
        {
            StartCoroutine(AddAllLettersText());
        }
    }

    public bool HasAllLetters()
    {
        return lettersCollected >= requiredLetters;
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");


        TryExit(player);


        player.currentInteractable = null;



    }

    public void TryExit(PlayerController player)
    {

        if (canExitByStairs)
        {
            StartCoroutine(GoDownStairs(player));
            return;
        }

        if (!HasAllLetters())
        {
            StartCoroutine(AddText(text));
            Debug.Log("Brakuje listów");
            return;
        }
        else
        {
            if (hiddenDoorDiscovered)
            {
                StartCoroutine(AddText(text3));
            }
            else
            {
                StartCoroutine(AddText(text2));
            }
        }
    }

       
        

    private IEnumerator AddText(string stringText)
    {

        ClueManager.Instance.SherlockText.text = stringText;
        yield return new WaitForSeconds(3);
        ClueManager.Instance.SherlockText.text = "";
        interactable.isInteractableActive = true;

    }

    private IEnumerator AddAllLettersText()
    {
        yield return new WaitForSeconds(3);
        ClueManager.Instance.SherlockText.text = allLettersText;
        yield return new WaitForSeconds(3);
        

        if (!ethelFounded)
        {
            ClueManager.Instance.SherlockText.text = ethelText;
            yield return new WaitForSeconds(3);
            ClueManager.Instance.SherlockText.text = "";
        }
        else
        {
            ClueManager.Instance.SherlockText.text = "";
        }

    }

    private IEnumerator GoDownStairs(PlayerController player)
    {
        player.navMeshAgent.ResetPath();


        level_1.SetActive(true);
        yield return null;
        player.navMeshAgent.ResetPath();
        player.navMeshAgent.Warp(level1StartPoint.position);
        player.transform.rotation = level1StartPoint.rotation;
        yield return null;

        level_2.SetActive(false);
        player.currentInteractable = null;

        yield return null;
    }
}
