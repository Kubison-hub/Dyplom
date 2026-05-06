using UnityEngine;
using UnityEngine.InputSystem;

public class MagnifierGlassController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera hiddenCluesCamera;
    [SerializeField] private RenderTexture hiddenCluesTexture;
    [SerializeField] private Camera loupeSceneCamera;
    [SerializeField] private RenderTexture loupeSceneTexture;
    [Space]
    [SerializeField] private EagleVisionScanner scanner;
    [SerializeField] private RectTransform loupeFrame;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Material magnifierMaterial;
    [Space]
    [SerializeField, Range(0f, 1f)] private float desaturateScene = 0.7f;
    [SerializeField] private float hiddenCluesStrength = 1f;

    [Header("Settings")]
    [SerializeField] private float zoom = 2.0f;

    [Tooltip("Mno¿nik promienia szk³a wzglêdem szerokoœci ramki. 0.5 = po³owa szerokoœci.")]
    [SerializeField] private float lensRadiusMultiplier = 0.32f;

    [Tooltip("Przesuniêcie obrazka lupy wzglêdem pozycji myszy")]
    [SerializeField] private Vector2 loupeFrameOffset = Vector2.zero;

    [Tooltip("Przesuniêcie œrodka szk³a wzglêdem pozycji myszy")]
    [SerializeField] private Vector2 lensCenterOffset = Vector2.zero;

    private readonly Vector3[] corners = new Vector3[4];

    public bool hideCursor = true;

    private void Start()
    {
        if (magnifierMaterial != null)
        {
            if (hiddenCluesTexture != null)
                magnifierMaterial.SetTexture("_HiddenCluesTex", hiddenCluesTexture);

            if (loupeSceneTexture != null)
                magnifierMaterial.SetTexture("_SceneTex", loupeSceneTexture);

            magnifierMaterial.SetFloat("_HiddenCluesStrength", 1f);
        }

        HideLoupe();
    }

    private void Update()
    {
        
        bool active = scanner != null && scanner.isScanning;

        if (loupeSceneCamera != null)
            loupeSceneCamera.enabled = active;

        if (hiddenCluesCamera != null)
            hiddenCluesCamera.enabled = active;

        if (!active)
        {
            HideLoupe();
            return;
        }

        if (Mouse.current == null || magnifierMaterial == null || loupeFrame == null)
            return;

        ShowLoupe();

        Vector2 mousePos = Mouse.current.position.ReadValue();
        loupeFrame.position = mousePos + loupeFrameOffset;

        Vector2 lensCenterScreen = mousePos + lensCenterOffset;

        Vector2 centerUV = new Vector2(
            lensCenterScreen.x / Screen.width,
            lensCenterScreen.y / Screen.height
        );

        float normalizedRadius = GetLoupeRadiusNormalized();

        magnifierMaterial.SetVector("_Center", new Vector4(centerUV.x, centerUV.y, 0f, 0f));
        magnifierMaterial.SetFloat("_Radius", normalizedRadius);
        magnifierMaterial.SetFloat("_Zoom", zoom);
        magnifierMaterial.SetFloat("_DesaturateScene", desaturateScene);
        magnifierMaterial.SetFloat("_HiddenCluesStrengt", hiddenCluesStrength);

    }

    private float GetLoupeRadiusNormalized()
    {
        loupeFrame.GetWorldCorners(corners);

        float widthOnScreen = Vector3.Distance(corners[0], corners[3]);
        float heightOnScreen = Vector3.Distance(corners[0], corners[1]);

        float diameter = Mathf.Min(widthOnScreen, heightOnScreen);
        float radiusPixels = diameter * lensRadiusMultiplier;

        return radiusPixels / Mathf.Min(Screen.width, Screen.height);
    }

    private void ShowLoupe()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }
        if (hideCursor)
        {
            Cursor.visible = false;
        }
        
    }

    private void HideLoupe()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }

        Cursor.visible = true;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }
}