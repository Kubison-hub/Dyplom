using UnityEngine;

public class SherlockWalkForwardScript : MonoBehaviour
{
    public float speed = 0;
    public bool thinking = false;
    public Animator animator;
    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward  * speed * Time.deltaTime);

        if (speed > 0)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsThinking", false);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }


        if (thinking)
        {
            animator.SetBool("IsThinking", true);
        }
        else
        {
            animator.SetBool("IsThinking", false);
        }
    }
}
