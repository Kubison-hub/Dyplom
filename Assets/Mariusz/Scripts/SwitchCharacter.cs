using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.AI;
using TMPro;
using PxP.DOCS;

/// <summary>
/// Ten PlayerSwitcher po prostu zmienia PlayerInput na aktywn¹ postaæ i zmienia priorytet CinemachineCamery.
/// Dodatkowo obs³uguje zmianê tartetu do Assetu DynamicOcclusionCutoutSystem (transparentne œciany).
/// Jest tu te¿ zachowanie Watsona, ale nie powiino tu byæ, to tak na szybko. 
/// </summary>

public class SwitchCharacter : MonoBehaviour
{
  

    public static SwitchCharacter Instance;

    public bool canSwitchOnStart = false;

    public Transform sherlockTransform;
    public Transform watsonTransform;
    public float watsonRotationSpeed = 3f;

    public PlayerInput[] players;
    public CinemachineCamera[] playersCamera;
    public int activePlayerIndex = 0;

    public TextMeshProUGUI activePlayerText;

    [SerializeField] DynamicOcclusionCutoutSystem dynamicOcclusionCutoutSystem;
    [SerializeField] private Key characterSwitchKey = Key.Space;

    [Header("Watson Return")]
    [SerializeField] private int watsonPlayerIndex = 1;
    [SerializeField] private Transform watsonReturnPoint;
    [SerializeField] private NavMeshAgent watsonNavMeshAgent;
    [SerializeField, Min(0.1f)] private float watsonReturnSampleRadius = 1f;

    public bool canSwitch = true;

    private void Start()
    {
        Instance = this;

        ResolveWatsonNavMeshAgent();

        if (dynamicOcclusionCutoutSystem == null)
        {
            Debug.LogError("dynamicOcclusionCutoutSystem == null");
        }

        

        SetActivePlayer(0);



        if (canSwitchOnStart)
        {
            canSwitch = true;
        }
        else
        {
            canSwitch = false;
        }
            
        
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[characterSwitchKey].wasPressedThisFrame)
        {
            int nextIndex = (activePlayerIndex + 1) % players.Length;
            SetActivePlayer(nextIndex);
            
        }

        //if (players[1].enabled == false)
        //{
        //    RotateWatsonTowardSherlock();
        //}
    }

    public void SetActivePlayer(int index)
    {
        if (canSwitch)
        {
            for (int i = 0; i < players.Length; i++)
            {
                players[i].enabled = (i == index);

                playersCamera[i].Priority = (i == index) ? 10 : 0;
            }

            activePlayerIndex = index;

            if (index == watsonPlayerIndex)
                StopWatsonReturn();
            else
                ReturnWatsonToMarker();

            SetWallTransparencyTarget(players[index].gameObject.transform);

            activePlayerText.text = players[index].gameObject.name;

            if (EagleVisionSystem.Instance != null)
                EagleVisionSystem.Instance.RefreshScan();
            //Debug.Log("Zmiana na: " + players[index].gameObject.name);
        }



    }


    private void ResolveWatsonNavMeshAgent()
    {
        if (watsonNavMeshAgent != null || players == null ||
            watsonPlayerIndex < 0 || watsonPlayerIndex >= players.Length ||
            players[watsonPlayerIndex] == null)
            return;

        watsonNavMeshAgent = players[watsonPlayerIndex].GetComponent<NavMeshAgent>();
    }

    private void ReturnWatsonToMarker()
    {
        ResolveWatsonNavMeshAgent();

        if (watsonNavMeshAgent == null || watsonReturnPoint == null || !watsonNavMeshAgent.isOnNavMesh)
            return;

        if (!NavMesh.SamplePosition(
                watsonReturnPoint.position,
                out NavMeshHit navMeshHit,
                watsonReturnSampleRadius,
                NavMesh.AllAreas))
        {
            Debug.LogWarning("Watson Return Point is not close enough to the NavMesh.", watsonReturnPoint);
            return;
        }

        watsonNavMeshAgent.isStopped = false;
        watsonNavMeshAgent.SetDestination(navMeshHit.position);
    }

    private void StopWatsonReturn()
    {
        ResolveWatsonNavMeshAgent();

        if (watsonNavMeshAgent == null || !watsonNavMeshAgent.isOnNavMesh)
            return;

        watsonNavMeshAgent.ResetPath();
        watsonNavMeshAgent.isStopped = false;
    }
    private void RotateWatsonTowardSherlock()
    {
        Vector3 direction = sherlockTransform.position - watsonTransform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            watsonTransform.rotation = Quaternion.Lerp(
                watsonTransform.rotation,
                lookRotation,
                Time.deltaTime * watsonRotationSpeed
            );
        }
    }

    private void SetWallTransparencyTarget(Transform target)
    {

        dynamicOcclusionCutoutSystem.m_target = target;
    }

}
