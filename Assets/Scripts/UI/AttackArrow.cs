using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI
{
    [RequireComponent(typeof(LineRenderer))]
    public class AttackArrow : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField]
        private float arrowWidth = 5f;

        [SerializeField]
        private Color arrowColor = new Color(1f, 0f, 0f, 0.8f); // Red with slight transparency

        [SerializeField]
        private Material arrowMaterial;

        private Transform sourceTransform;
        private Transform targetTransform;
        private Canvas parentCanvas;
        private Camera uiCamera;
        private bool isActive = false;

        private void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            // Configure LineRenderer
            lineRenderer.positionCount = 3; // Start point, control point, end point
            lineRenderer.startWidth = arrowWidth;
            lineRenderer.endWidth = arrowWidth;
            lineRenderer.startColor = arrowColor;
            lineRenderer.endColor = arrowColor;
            lineRenderer.useWorldSpace = true;

            // If material is provided, use it, otherwise create a default one
            if (arrowMaterial != null)
                lineRenderer.material = arrowMaterial;
            else
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            // Initially hide the arrow
            lineRenderer.enabled = false;

            // Find the parent canvas and camera
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                uiCamera = parentCanvas.worldCamera;
            else
                uiCamera = Camera.main;
        }

        public void SetSource(Transform source)
        {
            sourceTransform = source;
        }

        public void StartDrawing()
        {
            if (sourceTransform == null)
                return;

            isActive = true;
            lineRenderer.enabled = true;

            // Set initial position to the source card
            Vector3 sourcePos = GetCanvasPosition(sourceTransform.position);
            lineRenderer.SetPosition(0, sourcePos);
            lineRenderer.SetPosition(1, sourcePos);
            lineRenderer.SetPosition(2, sourcePos);
        }

        public void UpdatePosition(Vector3 currentPosition)
        {
            if (!isActive || sourceTransform == null)
                return;

            Vector3 sourcePos = GetCanvasPosition(sourceTransform.position);
            Vector3 targetPos = GetCanvasPosition(currentPosition);

            // Calculate a control point for a slight curve
            Vector3 direction = (targetPos - sourcePos).normalized;
            float distance = Vector3.Distance(sourcePos, targetPos);
            Vector3 controlPoint =
                sourcePos + direction * (distance * 0.5f) + Vector3.up * (distance * 0.2f);

            // Update the line positions
            lineRenderer.SetPosition(0, sourcePos);
            lineRenderer.SetPosition(1, controlPoint);
            lineRenderer.SetPosition(2, targetPos);
        }

        public void FinishDrawing(Transform target)
        {
            if (!isActive)
                return;

            targetTransform = target;
            if (sourceTransform != null && targetTransform != null)
            {
                Vector3 sourcePos = GetCanvasPosition(sourceTransform.position);
                Vector3 targetPos = GetCanvasPosition(targetTransform.position);

                // Calculate a control point for a slight curve
                Vector3 direction = (targetPos - sourcePos).normalized;
                float distance = Vector3.Distance(sourcePos, targetPos);
                Vector3 controlPoint =
                    sourcePos + direction * (distance * 0.5f) + Vector3.up * (distance * 0.2f);

                // Update the line positions
                lineRenderer.SetPosition(0, sourcePos);
                lineRenderer.SetPosition(1, controlPoint);
                lineRenderer.SetPosition(2, targetPos);
            }
        }

        public void Hide()
        {
            isActive = false;
            lineRenderer.enabled = false;
            sourceTransform = null;
            targetTransform = null;
        }

        public void CancelDrawing()
        {
            // Cancel the current drawing operation
            isActive = false;
            lineRenderer.enabled = false;
            targetTransform = null;
        }

        public void StopDrawing()
        {
            // Stop drawing and hide the arrow
            isActive = false;
            lineRenderer.enabled = false;
            sourceTransform = null;
            targetTransform = null;
        }

        private Vector3 GetCanvasPosition(Vector3 worldPosition)
        {
            // Convert screen position to world position for the line renderer
            if (
                parentCanvas != null
                && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera
                && uiCamera != null
            )
            {
                // For Screen Space - Camera
                return worldPosition;
            }
            else if (
                parentCanvas != null
                && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            )
            {
                // For Screen Space - Overlay
                Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
                return new Vector3(screenPoint.x, screenPoint.y, 0);
            }
            else
            {
                // Fallback
                return worldPosition;
            }
        }
    }
}
