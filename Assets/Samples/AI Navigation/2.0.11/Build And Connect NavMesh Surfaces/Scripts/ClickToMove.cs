using UnityEngine;
using UnityEngine.AI;

namespace Unity.AI.Navigation.Samples
{
    /// <summary>
    /// Use physics raycast hit from mouse click to set agent destination
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ClickToMove : MonoBehaviour
    {
        NavMeshAgent m_Agent;
        RaycastHit m_HitInfo = new RaycastHit();
        Animator m_Animator;
        void Start()
        {
            m_Agent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
                    m_Agent.destination = m_HitInfo.point;
            }

            if (m_Agent.velocity.magnitude != 0f)
            {
                m_Animator.SetBool("IsWalking", true);
            }
            else
            {
                m_Animator.SetBool("IsWalking", false);
            }

        }

        //private void OnAnimatorMove()
        //{
        //    if (m_Animator.GetBool("IsWalking"))
        //    {
        //        m_Agent.speed = (m_Animator.deltaPosition / Time.deltaTime).magnitude;
        //    }
        //}

    }
}