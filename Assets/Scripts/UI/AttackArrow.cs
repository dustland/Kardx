using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI
{
    /// <summary>
    /// Manages the drawing of an attack arrow between two points
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AttackArrow : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private Transform sourceTransform;
        private Canvas parentCanvas;
        private bool isActive = false;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer();
            }

            // Find the parent canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                // Find any canvas in the scene
                parentCanvas = FindAnyObjectByType<Canvas>();
                if (parentCanvas != null)
                {
                    transform.SetParent(parentCanvas.transform, false);
                }
            }
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.positionCount = 3; // Start, control, and end points
            lineRenderer.startWidth = 5f;
            lineRenderer.endWidth = 5f;
            lineRenderer.startColor = new Color(1f, 0f, 0f, 0.8f); // Red with slight transparency
            lineRenderer.endColor = new Color(1f, 0f, 0f, 0.8f);
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sortingOrder = 100;
            lineRenderer.enabled = false;
            
            Debug.Log("[AttackArrow] LineRenderer configured");
        }

        public void SetSource(Transform source)
        {
            sourceTransform = source;
            Debug.Log($"[AttackArrow] Source set to {source.name}");
        }

        public void StartDrawing()
        {
            if (sourceTransform == null)
                return;

            isActive = true;
            lineRenderer.enabled = true;
            Vector3 sourcePos = GetCanvasPosition(sourceTransform.position);
            lineRenderer.SetPosition(0, sourcePos);
            lineRenderer.SetPosition(1, sourcePos); // Control point starts at source
            lineRenderer.SetPosition(2, sourcePos); // End point starts at source
            
            Debug.Log($"[AttackArrow] Started drawing from {sourcePos}");
        }

        public void UpdatePosition(Vector2 targetPosition)
        {
            if (!isActive || sourceTransform == null)
                return;

            Vector3 sourcePos = GetCanvasPosition(sourceTransform.position);
            Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, sourcePos.z);
            
            // Calculate a control point for a curved line
            Vector3 controlPoint = sourcePos + (targetPos - sourcePos) * 0.5f;
            controlPoint.y += 50f; // Curve upward
            
            // Set the positions
            lineRenderer.SetPosition(0, sourcePos);
            lineRenderer.SetPosition(1, controlPoint);
            lineRenderer.SetPosition(2, targetPos);
            
            Debug.Log($"[AttackArrow] Updated positions: source={sourcePos}, target={targetPos}");
        }

        public void CancelDrawing()
        {
            isActive = false;
            lineRenderer.enabled = false;
            Debug.Log("[AttackArrow] Drawing cancelled");
        }

        private Vector3 GetCanvasPosition(Vector3 worldPosition)
        {
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                return worldPosition;
            else
                return worldPosition; // Fallback
        }
    }
}
