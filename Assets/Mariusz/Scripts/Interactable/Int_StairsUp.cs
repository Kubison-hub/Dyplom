using System.Collections;
using UnityEngine;

public class Int_StairsUp : MonoBehaviour
{

    public bool canGoUpStairs = false;
    public bool performed = false;

    public GameObject SherlockGO;

    public Transform moveDestination;

    private Interactable interactable;

    public Transform level2StartingPoint;
    public GameObject level_1;
    public GameObject level_2;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        SherlockGO = GameObject.Find("Sherlock");
    }
    private void Update()
    {
        if (canGoUpStairs)
        {
            interactable.interactabePoint = moveDestination;
        }
        else
        {
            interactable.interactabePoint = SherlockGO.transform;
        }
    }

    public void PerformInteraction(PlayerController player)
    {

        if (canGoUpStairs)
        {
            performed = true;

            
            player.currentInteractable = null;

            
            StartCoroutine(GoUpStairs(player));
        }
        else
        {
            PlayerTopText.Instance.ShowTopText("Madam Selma pilnuje schodów na górê, interesuj¹ce", 
                "Na razie nie przejdziemy, rozejrzyjmy siê po domu.");
            player.currentInteractable = null;
        }
  
    }

    private IEnumerator GoUpStairs(PlayerController player)
    {
        

        level_2.SetActive(true);
        yield return null;
        player.navMeshAgent.ResetPath();
        player.navMeshAgent.Warp(level2StartingPoint.position);
        player.transform.rotation = level2StartingPoint.rotation;
        yield return null;

        level_1.SetActive(false);
        player.currentInteractable = null;

        yield return null;
    }
}
