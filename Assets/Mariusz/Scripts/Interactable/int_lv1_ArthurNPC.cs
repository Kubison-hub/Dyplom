using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class int_lv1_ArthurNPC : Int_lv1_NpcDialogBase
{
    [Header("Dialogue Rig")]
    [SerializeField] private Rig dialogueRig;
    [SerializeField, Min(0.01f)] private float rigBlendDuration = 0.25f;

    private Coroutine rigBlendCoroutine;

    protected override InteractionType RequiredInteractionType => InteractionType.int_lv1_ArthurNPC;
    protected override bool ShouldFacePlayerBeforeDialogue => false;
    protected override bool ShouldFaceInteractingPlayerTowardNpcBeforeDialogue => true;

    private void Awake()
    {
        if (dialogueRig == null)
            dialogueRig = GetComponentInChildren<Rig>(true);

        SetRigWeightImmediate(0f);
    }

    protected override void Start()
    {
        base.Start();
        SetRigWeightImmediate(0f);
    }

    protected override void OnNpcDialogueStarted()
    {
        BlendRigWeight(1f);
    }

    protected override void OnNpcDialogueFinished()
    {
        BlendRigWeight(0f);
    }

    private void OnDisable()
    {
        if (rigBlendCoroutine != null)
            StopCoroutine(rigBlendCoroutine);

        SetRigWeightImmediate(0f);
    }

    private void BlendRigWeight(float targetWeight)
    {
        if (dialogueRig == null)
            return;

        if (rigBlendCoroutine != null)
            StopCoroutine(rigBlendCoroutine);

        rigBlendCoroutine = StartCoroutine(BlendRigWeightRoutine(targetWeight));
    }

    private IEnumerator BlendRigWeightRoutine(float targetWeight)
    {
        float startWeight = dialogueRig.weight;
        float elapsed = 0f;

        while (elapsed < rigBlendDuration)
        {
            elapsed += Time.deltaTime;
            dialogueRig.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / rigBlendDuration);
            yield return null;
        }

        dialogueRig.weight = targetWeight;
        rigBlendCoroutine = null;
    }

    private void SetRigWeightImmediate(float weight)
    {
        if (dialogueRig != null)
            dialogueRig.weight = weight;
    }
}
