using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public sealed class PlanarMirrorCamera : MonoBehaviour
{
    [Header("Mirror")]
    [SerializeField] private Transform mirrorSurface;
    [SerializeField] private Renderer mirrorRenderer;
    [SerializeField] private Vector3 localFrontNormal = Vector3.forward;
    [SerializeField] private bool fitFrustumToMirror = true;
    [SerializeField, Range(0f, 0.25f)] private float frustumPadding = 0.05f;
    [SerializeField, Range(-0.15f, 0.15f)] private float reflectionVerticalOffset;
    [SerializeField] private bool useObliqueClipPlane;
    [SerializeField, Min(0f)] private float clipPlaneOffset = 0.02f;
    [SerializeField] private bool flipRenderTextureY;

    [Header("Viewer")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private bool useMainCamera = true;

    private Camera mirrorCamera;
    private bool cullingInverted;
    private bool previousInvertCulling;
    private MaterialPropertyBlock mirrorProperties;

    private static readonly int MirrorTextureId = Shader.PropertyToID("_MirrorTexture");
    private static readonly int MirrorViewProjectionId = Shader.PropertyToID("_MirrorViewProjection");
    private static readonly int FlipRenderTextureYId = Shader.PropertyToID("_FlipRenderTextureY");
    private static readonly int MirrorVerticalOffsetId = Shader.PropertyToID("_MirrorVerticalOffset");

    private void Awake()
    {
        mirrorCamera = GetComponent<Camera>();
        ExcludeMirrorLayer();
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
        RestoreCulling();

        if (mirrorCamera != null)
        {
            mirrorCamera.ResetWorldToCameraMatrix();
            mirrorCamera.ResetProjectionMatrix();
        }
    }

    private void LateUpdate()
    {
        if (mirrorSurface == null || mirrorCamera == null)
            return;

        Camera viewer = useMainCamera ? Camera.main : sourceCamera;
        if (viewer == null || viewer == mirrorCamera)
            return;

        Vector3 normal = mirrorSurface.TransformDirection(localFrontNormal.normalized);
        if (normal.sqrMagnitude < 0.001f)
            return;

        normal.Normalize();
        Vector3 point = mirrorSurface.position;
        if (Vector3.Dot(viewer.transform.position - point, normal) < 0f)
            normal = -normal;

        float planeDistance = -Vector3.Dot(normal, point);
        Matrix4x4 reflection = Matrix4x4.identity;
        reflection.m00 = 1f - 2f * normal.x * normal.x;
        reflection.m01 = -2f * normal.x * normal.y;
        reflection.m02 = -2f * normal.x * normal.z;
        reflection.m03 = -2f * planeDistance * normal.x;
        reflection.m10 = -2f * normal.y * normal.x;
        reflection.m11 = 1f - 2f * normal.y * normal.y;
        reflection.m12 = -2f * normal.y * normal.z;
        reflection.m13 = -2f * planeDistance * normal.y;
        reflection.m20 = -2f * normal.z * normal.x;
        reflection.m21 = -2f * normal.z * normal.y;
        reflection.m22 = 1f - 2f * normal.z * normal.z;
        reflection.m23 = -2f * planeDistance * normal.z;

        mirrorCamera.transform.position = reflection.MultiplyPoint(viewer.transform.position);
        mirrorCamera.transform.rotation = Quaternion.LookRotation(
            reflection.MultiplyVector(viewer.transform.forward),
            reflection.MultiplyVector(viewer.transform.up));
        mirrorCamera.worldToCameraMatrix = viewer.worldToCameraMatrix * reflection;

        mirrorCamera.orthographic = viewer.orthographic;
        mirrorCamera.orthographicSize = viewer.orthographicSize;
        mirrorCamera.fieldOfView = viewer.fieldOfView;
        mirrorCamera.nearClipPlane = viewer.nearClipPlane;
        mirrorCamera.farClipPlane = viewer.farClipPlane;
        mirrorCamera.projectionMatrix = viewer.projectionMatrix;
        if (fitFrustumToMirror && mirrorRenderer != null)
            FitProjectionToMirror();

        if (useObliqueClipPlane)
        {
            Vector3 clipPoint = point + normal * clipPlaneOffset;
            Vector4 worldPlane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, clipPoint));
            Vector4 cameraPlane = mirrorCamera.worldToCameraMatrix.inverse.transpose * worldPlane;
            mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(cameraPlane);
        }

        if (mirrorRenderer != null && mirrorCamera.targetTexture != null)
        {
            mirrorProperties ??= new MaterialPropertyBlock();
            mirrorRenderer.GetPropertyBlock(mirrorProperties);
            mirrorProperties.SetTexture(MirrorTextureId, mirrorCamera.targetTexture);
            mirrorProperties.SetMatrix(MirrorViewProjectionId,
                GL.GetGPUProjectionMatrix(mirrorCamera.projectionMatrix, true) * mirrorCamera.worldToCameraMatrix);
            mirrorProperties.SetFloat(FlipRenderTextureYId, flipRenderTextureY ? 1f : 0f);
            mirrorProperties.SetFloat(MirrorVerticalOffsetId, reflectionVerticalOffset);
            mirrorRenderer.SetPropertyBlock(mirrorProperties);
        }
    }

    private void FitProjectionToMirror()
    {
        MeshFilter meshFilter = mirrorRenderer.GetComponent<MeshFilter>();
        Bounds bounds = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds
            : new Bounds(Vector3.zero, Vector3.zero);
        bool useMeshBounds = meshFilter != null && meshFilter.sharedMesh != null;
        if (!useMeshBounds)
            bounds = mirrorRenderer.bounds;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                (i & 1) == 0 ? -1f : 1f,
                (i & 2) == 0 ? -1f : 1f,
                (i & 4) == 0 ? -1f : 1f));
            if (useMeshBounds)
                corner = mirrorRenderer.transform.TransformPoint(corner);

            Vector3 viewPoint = mirrorCamera.worldToCameraMatrix.MultiplyPoint(corner);
            float depth = -viewPoint.z;
            if (depth <= 0.001f)
                return;

            float x = mirrorCamera.orthographic ? viewPoint.x : viewPoint.x / depth;
            float y = mirrorCamera.orthographic ? viewPoint.y : viewPoint.y / depth;
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        float width = maxX - minX;
        float height = maxY - minY;
        if (width < 0.0001f || height < 0.0001f)
            return;

        float padX = width * frustumPadding;
        float padY = height * frustumPadding;
        minX -= padX;
        maxX += padX;
        minY -= padY;
        maxY += padY;

        if (mirrorCamera.orthographic)
        {
            mirrorCamera.projectionMatrix = Matrix4x4.Ortho(
                minX, maxX, minY, maxY,
                mirrorCamera.nearClipPlane, mirrorCamera.farClipPlane);
        }
        else
        {
            float near = mirrorCamera.nearClipPlane;
            mirrorCamera.projectionMatrix = Matrix4x4.Frustum(
                minX * near, maxX * near, minY * near, maxY * near,
                near, mirrorCamera.farClipPlane);
        }
    }

    private void ExcludeMirrorLayer()
    {
        int mirrorLayer = LayerMask.NameToLayer("Mirror");
        if (mirrorCamera != null && mirrorLayer >= 0)
            mirrorCamera.cullingMask &= ~(1 << mirrorLayer);
    }

    private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != mirrorCamera)
            return;

        previousInvertCulling = GL.invertCulling;
        GL.invertCulling = !previousInvertCulling;
        cullingInverted = true;
    }

    private void HandleEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == mirrorCamera)
            RestoreCulling();
    }

    private void RestoreCulling()
    {
        if (!cullingInverted)
            return;

        GL.invertCulling = previousInvertCulling;
        cullingInverted = false;
    }
}
