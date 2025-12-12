using UnityEngine;

public class SwitchSphere : MonoBehaviour
{
    public Transform secretWall;
    public Animator wallAnimator;
    public DoorSwitcher doorSwitcher;

    private void Start()
    {
        wallAnimator = secretWall.GetComponent<Animator>();
    }

    private void OnTriggerExit(Collider other)
    {


        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            if (!doorSwitcher.isOpen) return;
            wallAnimator.SetTrigger("Close");
            doorSwitcher.isOpen = false;

            

        }
    }

    
}
