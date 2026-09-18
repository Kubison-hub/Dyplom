using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PxP.DOCS
{
    public class DynamicOcclusionCutoutSystem : MonoBehaviour
    {
        private sealed class OcclusionRendererState
        {
            public Renderer Renderer;
            public int[] MaterialIndices;
            public bool IsEligible;
            public bool HasAppliedState;
        }

        private const string LoupeSceneTextureName = "RT_LoupeScene";
        private const string HiddenCluesTextureName = "RT_HiddenClues";
        private const float RendererDepthTolerance = 0.05f;
        private static readonly int DepthEnabledProperty = Shader.PropertyToID("_DOCS_Depth_Enabled");

        [Header("Wall material reference")]
        [Tooltip("The material(s) contained in Project folders that has to be updated")]
        [SerializeField] private Material[] m_materials;

        [Header("Transparent materials")]
        [Tooltip("Optional glass materials using the DOCS - Transparent Glass shader.")]
        [SerializeField] private Material[] transparentMaterials;

        [Header("Scene objects reference")]
        [Tooltip("The target's tag\nAt Awake this Tag will be searched\nLeave empty if target is manually assigned")]
        [SerializeField] private string targetTag = "";
        [Tooltip("The Camera from which the occlusion is seen")]
        [SerializeField] private Camera m_camera;
        [Tooltip("Cameras that should render the wall materials without the cutout, for example the loupe cameras.")]
        [SerializeField] private Camera[] camerasIgnoringCutout;
        [Tooltip("The target behind the occluded objects\n(ex: Player behind wall)")]
        [SerializeField] public Transform m_target;

        [Header("Cutout parameters")]
        [Min(0.0f)]
        [Tooltip("The radius of the occlusion mask")]
        [SerializeField] float maskRadius = 4f;
        [Range(0.01f, 1.0f)]
        [Tooltip("Speed at which the transitions are made")]
        [SerializeField] float lerpSpeed = 0.05f;
        [Range(0.01f, 1.0f)]
        [Tooltip("Speed at which the mask follow")]
        [SerializeField] float positionSpeed = 0.8f;
        [Tooltip("Target height correction\nModify if the mask has to be higher or lower on the target")]
        [SerializeField] float targetHeightCorrection = 0.8f;

        [Header("Raycast Behaviour")]
        [Min(0.01f)]
        [SerializeField] float radius = 0.5f;
        [Tooltip("Only colliders on these layers can trigger the occlusion mask. Default is Walls.")]
        [SerializeField] LayerMask occlusionLayers = 1 << 12;

        [Header("Debug Settings")]
        [SerializeField] bool enableGizmos = true;

        
        Vector3 direction;
        Vector3 currentSpherePosition;
        Vector3 targetPosition;
        private readonly List<OcclusionRendererState> occlusionRenderers = new List<OcclusionRendererState>();
        private MaterialPropertyBlock rendererPropertyBlock;
        private float currentMaskRadius = 0.0f;
        private float targetMaskRadius = 0.0f;
        private float currentLerpTime = 0.0f;
        bool isHitting = false;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;

            ApplyMaterialState(Vector3.zero, 0.0f);
            ResetRendererEligibility();
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (!ShouldIgnoreCutout(renderingCamera)) return;

            ApplyMaterialState(currentSpherePosition, 0.0f);
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (!ShouldIgnoreCutout(renderingCamera)) return;

            ApplyMaterialState(currentSpherePosition, currentMaskRadius);
        }

        private bool ShouldIgnoreCutout(Camera renderingCamera)
        {
            if (renderingCamera == null || camerasIgnoringCutout == null) return false;

            foreach (Camera ignoredCamera in camerasIgnoringCutout)
            {
                if (ignoredCamera == renderingCamera) return true;
            }

            return false;
        }

        private void ApplyMaterialState(Vector3 position, float radiusValue)
        {
            ApplyMaterialState(m_materials, position, radiusValue);
            ApplyMaterialState(transparentMaterials, position, radiusValue);
        }

        private static void ApplyMaterialState(
            Material[] materials,
            Vector3 position,
            float radiusValue)
        {
            if (materials == null) return;

            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                mat.SetVector("_Target_Position", position);
                mat.SetFloat("_Radius", radiusValue);
            }
        }

        private void Awake()
        {
            rendererPropertyBlock = new MaterialPropertyBlock();

            if (targetTag != "" && m_target == null)
                m_target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
            if (m_camera == null)
                m_camera = Camera.main;
            if (camerasIgnoringCutout == null || camerasIgnoringCutout.Length == 0)
                ResolveDefaultIgnoredCameras();

            ValidateMaterials(m_materials, "wall");
            ValidateMaterials(transparentMaterials, "transparent");
            RefreshOcclusionRenderers();
        }

        private static void ValidateMaterials(Material[] materials, string materialType)
        {
            if (materials == null) return;

            foreach (Material material in materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_Target_Position") &&
                    material.HasProperty("_DOCS_Depth_Enabled") &&
                    material.HasProperty("_Radius"))
                    continue;

                Debug.LogWarning(
                    $"DOCS: {materialType} material '{material.name}' does not expose the required " +
                    "_Target_Position, _DOCS_Depth_Enabled and _Radius properties. Assign a current DOCS shader.",
                    material);
            }
        }

        [ContextMenu("Refresh Occlusion Renderers")]
        public void RefreshOcclusionRenderers()
        {
            ResetRendererEligibility();
            occlusionRenderers.Clear();

            HashSet<Material> docsMaterials = new HashSet<Material>();
            AddMaterials(docsMaterials, m_materials);
            AddMaterials(docsMaterials, transparentMaterials);
            if (docsMaterials.Count == 0) return;

            Renderer[] sceneRenderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Renderer sceneRenderer in sceneRenderers)
            {
                if (sceneRenderer == null) continue;

                int[] materialIndices = GetMatchingMaterialIndices(sceneRenderer, docsMaterials);
                if (materialIndices.Length == 0) continue;

                occlusionRenderers.Add(new OcclusionRendererState
                {
                    Renderer = sceneRenderer,
                    MaterialIndices = materialIndices
                });
            }
        }

        private static void AddMaterials(HashSet<Material> destination, Material[] materials)
        {
            if (materials == null) return;

            foreach (Material material in materials)
            {
                if (material != null) destination.Add(material);
            }
        }

        private static int[] GetMatchingMaterialIndices(
            Renderer sceneRenderer,
            HashSet<Material> docsMaterials)
        {
            Material[] sharedMaterials = sceneRenderer.sharedMaterials;
            List<int> matchingIndices = new List<int>();

            for (int index = 0; index < sharedMaterials.Length; index++)
            {
                Material sharedMaterial = sharedMaterials[index];
                if (sharedMaterial != null && docsMaterials.Contains(sharedMaterial))
                    matchingIndices.Add(index);
            }

            return matchingIndices.ToArray();
        }

        private void UpdateRendererEligibility(Vector3 cameraPosition, Vector3 playerPosition)
        {
            Vector3 cameraToPlayer = playerPosition - cameraPosition;
            float playerDepth = cameraToPlayer.magnitude;
            if (playerDepth <= Mathf.Epsilon)
            {
                SetAllRendererEligibility(false);
                return;
            }

            Vector3 depthDirection = cameraToPlayer / playerDepth;
            float maximumDepth = Mathf.Max(0.0f, playerDepth - RendererDepthTolerance);

            foreach (OcclusionRendererState state in occlusionRenderers)
            {
                Renderer sceneRenderer = state.Renderer;
                if (sceneRenderer == null) continue;

                Bounds bounds = sceneRenderer.bounds;
                float centerDepth = Vector3.Dot(bounds.center - cameraPosition, depthDirection);
                Vector3 extents = bounds.extents;
                float projectedExtent =
                    Mathf.Abs(depthDirection.x) * extents.x +
                    Mathf.Abs(depthDirection.y) * extents.y +
                    Mathf.Abs(depthDirection.z) * extents.z;

                float nearestDepth = centerDepth - projectedExtent;
                float farthestDepth = centerDepth + projectedExtent;
                bool isEligible = farthestDepth >= 0.0f && nearestDepth <= maximumDepth;
                ApplyRendererEligibility(state, isEligible);
            }
        }

        private void SetAllRendererEligibility(bool isEligible)
        {
            foreach (OcclusionRendererState state in occlusionRenderers)
            {
                ApplyRendererEligibility(state, isEligible);
            }
        }

        private void ResetRendererEligibility()
        {
            if (rendererPropertyBlock == null) return;
            SetAllRendererEligibility(true);
        }

        private void ApplyRendererEligibility(OcclusionRendererState state, bool isEligible)
        {
            if (state.Renderer == null ||
                state.MaterialIndices == null ||
                (state.HasAppliedState && state.IsEligible == isEligible))
                return;

            foreach (int materialIndex in state.MaterialIndices)
            {
                rendererPropertyBlock.Clear();
                state.Renderer.GetPropertyBlock(rendererPropertyBlock, materialIndex);
                rendererPropertyBlock.SetFloat(DepthEnabledProperty, isEligible ? 1.0f : 0.0f);
                state.Renderer.SetPropertyBlock(rendererPropertyBlock, materialIndex);
            }

            state.IsEligible = isEligible;
            state.HasAppliedState = true;
        }

        private bool HasConfiguredMaterials()
        {
            return HasMaterial(m_materials) || HasMaterial(transparentMaterials);
        }

        private static bool HasMaterial(Material[] materials)
        {
            if (materials == null) return false;

            foreach (Material material in materials)
            {
                if (material != null) return true;
            }

            return false;
        }

        private void ResolveDefaultIgnoredCameras()
        {
            Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<Camera> resolvedCameras = new List<Camera>();

            foreach (Camera sceneCamera in sceneCameras)
            {
                if (sceneCamera == null || sceneCamera.targetTexture == null) continue;

                string textureName = sceneCamera.targetTexture.name;
                if (textureName == LoupeSceneTextureName || textureName == HiddenCluesTextureName)
                    resolvedCameras.Add(sceneCamera);
            }

            camerasIgnoringCutout = resolvedCameras.ToArray();
        }

        void Start()
        {
            if (m_target == null || m_camera == null || !HasConfiguredMaterials()) return;

            currentSpherePosition = m_target.position;
            currentMaskRadius = 0.0f;
            Vector3 playerPosition = m_target.position + (Vector3.up * targetHeightCorrection);
            UpdateRendererEligibility(m_camera.transform.position, playerPosition);
        }

        void Update()
        {
            if (m_target == null || m_camera == null || !HasConfiguredMaterials())
            {
                this.enabled = false;
                return;
            }

            Vector3 targetPosition = m_target.position + (Vector3.up * targetHeightCorrection);
            direction = m_camera.transform.position - targetPosition;
            float cameraDistance = direction.magnitude;
            Vector3 cameraDirection = cameraDistance > Mathf.Epsilon ? direction / cameraDistance : Vector3.zero;

            if (cameraDistance > Mathf.Epsilon && Physics.SphereCast(
                    targetPosition,
                    radius,
                    cameraDirection,
                    out RaycastHit hitInfo,
                    cameraDistance,
                    occlusionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (!isHitting)
                {
                    isHitting = true;
                    currentLerpTime = 0.0f;
                }

                this.targetPosition = hitInfo.point;
                targetMaskRadius = maskRadius;
            }
            else
            {
                if (isHitting)
                {
                    isHitting = false;
                    currentLerpTime = 0.0f;
                }

                this.targetPosition = targetPosition;
                targetMaskRadius = 0.0f;
            }

            if (currentLerpTime < 1.0f) currentLerpTime += Time.deltaTime * positionSpeed;
            currentSpherePosition = Vector3.Lerp(currentSpherePosition, this.targetPosition, currentLerpTime);
            currentMaskRadius = Mathf.Lerp(currentMaskRadius, targetMaskRadius, lerpSpeed);

            UpdateRendererEligibility(m_camera.transform.position, targetPosition);
            ApplyMaterialState(currentSpherePosition, currentMaskRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_target == null || m_camera == null || !HasConfiguredMaterials() || !enableGizmos) return;

            Vector3 origin = m_target.position + (Vector3.up * targetHeightCorrection);
            Vector3 dir = (m_camera.transform.position - origin).normalized;
            Vector3 end = m_camera.transform.position;
            float distance = Vector3.Distance(origin, end);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawLine(origin, end);

            if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, distance, occlusionLayers, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, radius);
                Gizmos.DrawLine(origin, hit.point);

#if UNITY_EDITOR
                string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                Handles.Label(hit.point, $"DOCS hit: {hit.collider.name} [{layerName}]");
#endif
            }
        }
    }
}
