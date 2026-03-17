using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TMPro;
using PxP.DOCS;

/// <summary>
/// Ten PlayerSwitcher po prostu zmienia PlayerInput na aktywn¹ postaæ i zmienia priorytet CinemachineCamery.
/// Dodatkowo obs³uguje zmianê tartetu do Assetu DynamicOcclusionCutoutSystem (transparentne œciany).
/// Jest tu te¿ zachowanie Watsona, ale nie powiino tu byæ, to tak na szybko. 
/// </summary>

public class SwitchCharacter : MonoBehaviour
{
    public Transform sherlockTransform;
    public Transform watsonTransform;
    public float watsonRotationSpeed = 3f;

    public PlayerInput[] players;
    public CinemachineCamera[] playersCamera;
    private int activePlayerIndex = 0;

    public TextMeshProUGUI activePlayerText;

    [SerializeField] DynamicOcclusionCutoutSystem dynamicOcclusionCutoutSystem;


    private void Start()
    {
        if (dynamicOcclusionCutoutSystem == null)
        {
            Debug.LogError("dynamicOcclusionCutoutSystem == null");
        }

        SetActivePlayer(0);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            int nextIndex = (activePlayerIndex + 1) % players.Length;
            SetActivePlayer(nextIndex);
        }

        //if (players[1].enabled == false)
        //{
        //    RotateWatsonTowardSherlock();
        //}
    }

    private void SetActivePlayer(int index)
    {
        for (int i = 0; i < players.Length; i++)
        {
            players[i].enabled = (i == index);

            playersCamera[i].Priority = (i == index) ? 10 : 0;
        }

        activePlayerIndex = index;

        SetWallTransparencyTarget(players[index].gameObject.transform);

        activePlayerText.text = players[index].gameObject.name;
        //Debug.Log("Zmiana na: " + players[index].gameObject.name);

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
