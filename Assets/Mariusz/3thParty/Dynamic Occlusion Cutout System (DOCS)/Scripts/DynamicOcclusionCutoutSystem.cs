using UnityEngine;

namespace PxP.DOCS
{
    public class DynamicOcclusionCutoutSystem : MonoBehaviour
    {
        [Header("Wall material reference")]
        [Tooltip("The material(s) contained in Project folders that has to be updated")]
        [SerializeField] private Material[] m_materials;

        [Header("Scene objects reference")]
        [Tooltip("The target's tag\nAt Awake this Tag will be searched\nLeave empty if target is manually assigned")]
        [SerializeField] private string targetTag = "";
        [Tooltip("The Camera from which the occlusion is seen")]
        [SerializeField] private Camera m_camera;
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

        [Header("Debug Settings")]
        [SerializeField] bool enableGizmos = true;
        
        Vector3 direction;
        Vector3 currentSpherePosition;
        Vector3 targetPosition;
        private float currentMaskRadius = 0.0f;
        private float targetMaskRadius = 0.0f;
        private float currentLerpTime = 0.0f;
        bool isHitting = false;

        private void OnDisable()
        {
            foreach (var mat in m_materials)
            {
                mat.SetVector("_Target_Position", Vector3.zero);
                mat.SetFloat("_Radius", 0.0f);
            }
        }

        private void Awake()
        {
            if (targetTag != "" && m_target == null)
                m_target = GameObject.FindGameObjectWithTag(targetTag)?.transform;
            if (m_camera == null)
                m_camera = Camera.main;
        }

        void Start()
        {

            if (m_target == null || m_camera == null || m_materials == null || m_materials.Length == 0) return;

            currentSpherePosition = m_target.position;
            currentMaskRadius = 0.0f;
        }

        void Update()
        {
            if (m_target == null || m_camera == null || m_materials == null || m_materials.Length == 0)
            {
                
                this.enabled = false;
                return;
            }

            Vector3 targetPosition = m_target.position + (Vector3.up * targetHeightCorrection);
            direction = m_camera.transform.position - m_target.position;

            if (Physics.SphereCast(targetPosition, radius, direction, out RaycastHit hitInfo))
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

            foreach (var mat in m_materials)
            {
                mat.SetVector("_Target_Position", currentSpherePosition);
                mat.SetFloat("_Radius", currentMaskRadius);
            }

            
        }

        private void OnDrawGizmosSelected()
        {
            if (m_target == null || m_camera == null || m_materials == null || !enableGizmos) return;

            Vector3 origin = m_target.position + (Vector3.up * targetHeightCorrection);
            Vector3 dir = direction.normalized;
            Vector3 end = m_camera.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawWireSphere(end, radius);

            Gizmos.DrawLine(origin + Vector3.up * radius, end + Vector3.up * radius);
            Gizmos.DrawLine(origin + Vector3.down * radius, end + Vector3.down * radius);
            Gizmos.DrawLine(origin + Vector3.right * radius, end + Vector3.right * radius);
            Gizmos.DrawLine(origin + Vector3.left * radius, end + Vector3.left * radius);

            if (Physics.SphereCast(origin, radius, dir, out RaycastHit hit, 50.0f))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hit.point, radius);
            }
        }
    }
}