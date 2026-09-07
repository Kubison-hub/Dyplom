using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    [Header("Scene Interactions")]
    [SerializeField] private List<Interactable> interactions = new List<Interactable>();
    [SerializeField] private List<Interactable> completedInteractions = new List<Interactable>();

    public IReadOnlyList<Interactable> Interactions => interactions;
    public IReadOnlyList<Interactable> CompletedInteractions => completedInteractions;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RefreshInteractionList();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [ContextMenu("Refresh Interaction List")]
    public void RefreshInteractionList()
    {
        interactions.Clear();

        Interactable[] sceneInteractions = FindObjectsByType<Interactable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Interactable interaction in sceneInteractions)
        {
            if (interaction != null && interaction.gameObject.scene == gameObject.scene)
                interactions.Add(interaction);
        }

        interactions.Sort((left, right) =>
            string.CompareOrdinal(left.gameObject.name, right.gameObject.name));

        RebuildCompletedInteractions();
    }

    public void RegisterCompletedInteraction(Interactable interaction)
    {
        if (interaction == null)
            return;

        if (!interactions.Contains(interaction))
            interactions.Add(interaction);

        if (!completedInteractions.Contains(interaction))
            completedInteractions.Add(interaction);
    }

    public void UnregisterCompletedInteraction(Interactable interaction)
    {
        if (interaction != null)
            completedInteractions.Remove(interaction);
    }

    public bool IsInteractionCompleted(Interactable interaction)
    {
        return interaction != null && interaction.IsCompleted;
    }

    public bool IsInteractionCompleted(string saveId)
    {
        if (string.IsNullOrWhiteSpace(saveId))
            return false;

        foreach (Interactable interaction in completedInteractions)
        {
            if (interaction != null && interaction.SaveId == saveId)
                return true;
        }

        return false;
    }

    public List<string> GetCompletedInteractionIds()
    {
        List<string> completedIds = new List<string>();

        foreach (Interactable interaction in completedInteractions)
        {
            if (interaction != null && !string.IsNullOrWhiteSpace(interaction.SaveId))
                completedIds.Add(interaction.SaveId);
        }

        return completedIds;
    }

    public bool RestoreInteractionCompletedState(string saveId, bool completed)
    {
        if (string.IsNullOrWhiteSpace(saveId))
            return false;

        foreach (Interactable interaction in interactions)
        {
            if (interaction == null || interaction.SaveId != saveId)
                continue;

            interaction.RestoreCompletedState(completed);
            return true;
        }

        return false;
    }

    private void RebuildCompletedInteractions()
    {
        completedInteractions.Clear();

        foreach (Interactable interaction in interactions)
        {
            if (interaction != null && interaction.IsCompleted)
                completedInteractions.Add(interaction);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Missing Interaction Save IDs")]
    private void GenerateMissingInteractionSaveIds()
    {
        RefreshInteractionList();

        HashSet<string> usedIds = new HashSet<string>();
        int generatedCount = 0;

        foreach (Interactable interaction in interactions)
        {
            if (interaction == null)
                continue;

            string currentId = interaction.SaveId;
            if (!string.IsNullOrWhiteSpace(currentId) && usedIds.Add(currentId))
                continue;

            string newId;
            do
            {
                newId = System.Guid.NewGuid().ToString("N");
            }
            while (!usedIds.Add(newId));

            Undo.RecordObject(interaction, "Generate Interaction Save ID");
            SerializedObject serializedInteraction = new SerializedObject(interaction);
            SerializedProperty saveIdProperty = serializedInteraction.FindProperty("saveId");
            saveIdProperty.stringValue = newId;
            serializedInteraction.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            EditorUtility.SetDirty(interaction);
            generatedCount++;
        }

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"GameProgressManager: generated {generatedCount} interaction Save IDs.", this);
    }
#endif
}
