using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;
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
    [SerializeField] private bool returnWatsonToMarkerWhenUncontrolled = false;

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
        if (PlayerController.IsWorldInputLocked)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput)
            return;

        if (TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput)
            return;

        if (NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen)
            return;

        if (DialogueEditor.ConversationManager.Instance != null &&
            DialogueEditor.ConversationManager.Instance.IsConversationActive)
            return;

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
            float previousHorizontalAxis = 0f;
            bool shouldPreserveCameraAxis = index != activePlayerIndex &&
                                            TryGetCameraHorizontalAxis(activePlayerIndex, out previousHorizontalAxis);
            if (shouldPreserveCameraAxis)
                SetCameraHorizontalAxis(index, previousHorizontalAxis);
            for (int i = 0; i < players.Length; i++)
            {
                players[i].enabled = (i == index);

                playersCamera[i].Priority = (i == index) ? 10 : 0;
            }

            activePlayerIndex = index;

            if (index != 0)
                DetectiveIdeaManager.Instance?.ClearForNonSherlock();

            if (index == watsonPlayerIndex)
                StopWatsonReturn();
            else if (returnWatsonToMarkerWhenUncontrolled)
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

    private bool TryGetCameraHorizontalAxis(int playerIndex, out float horizontalAxis)
    {
        horizontalAxis = 0f;
        if (playersCamera == null || playerIndex < 0 || playerIndex >= playersCamera.Length ||
            playersCamera[playerIndex] == null)
            return false;

        CinemachineOrbitalFollow orbitalFollow =
            playersCamera[playerIndex].GetComponentInChildren<CinemachineOrbitalFollow>(true);
        if (orbitalFollow == null)
            return false;

        horizontalAxis = orbitalFollow.HorizontalAxis.Value;
        return true;
    }

    private void SetCameraHorizontalAxis(int playerIndex, float horizontalAxis)
    {
        if (playersCamera == null || playerIndex < 0 || playerIndex >= playersCamera.Length ||
            playersCamera[playerIndex] == null)
            return;

        CinemachineOrbitalFollow orbitalFollow =
            playersCamera[playerIndex].GetComponentInChildren<CinemachineOrbitalFollow>(true);
        if (orbitalFollow != null)
            orbitalFollow.HorizontalAxis.Value = horizontalAxis;
    }
    private void SetWallTransparencyTarget(Transform target)
    {

        dynamicOcclusionCutoutSystem.m_target = target;
    }


    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - aktywna postac
    // ---------------------------------------------------------------

    // Przywraca postac, ktora gracz sterowal w momencie zapisu.
    // SetActivePlayer dziala tylko gdy canSwitch == true, a po Start()
    // moze byc wylaczone - dlatego zdejmujemy blokade na czas przelaczenia.
    public void RestoreActivePlayer(int index)
    {
        if (players == null || index < 0 || index >= players.Length)
        {
            Debug.LogWarning("SwitchCharacter: zapisany indeks postaci (" + index +
                             ") jest poza zakresem. Zostawiam obecna postac.");
            return;
        }

        bool poprzednieCanSwitch = canSwitch;
        canSwitch = true;

        SetActivePlayer(index);

        canSwitch = poprzednieCanSwitch;

        Debug.Log("SwitchCharacter: przywrocono aktywna postac: " +
                  players[index].gameObject.name + " (indeks " + index + ").");
    }
}