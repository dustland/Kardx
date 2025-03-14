using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.Views.Match
{
    /// <summary>
    /// Manages the drawing of an attack arrow between two points using UI Images
    /// </summary>
    public class AttackArrow : MonoBehaviour
    {
        [SerializeField]
        private RectTransform arrowBody;

        [SerializeField]
        private RectTransform arrowHead;

        [SerializeField]
        private Sprite arrowBodySprite;

        [SerializeField]
        private Sprite arrowHeadSprite;

        [SerializeField]
        private Color arrowColor = new Color(1f, 0f, 0f, 0.8f);

        [SerializeField]
        private float arrowWidth = 10f;

        [SerializeField]
        private float arrowHeadSize = 20f;

        private Canvas parentCanvas;
        private bool isActive = false;
        private Vector2 startPosition;

        private MatchView matchView;

        private void Awake()
        {
            // Find the parent canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                transform.SetParent(parentCanvas.transform, false);

                // Reset again after reparenting
                ResetRectTransform();
            }
            else
            {
                Debug.LogError("[AttackArrow] Parent canvas not found. Arrow will not be displayed.");
            }

            CreateArrowComponents();

            // Initially hide the arrow
            gameObject.SetActive(false);

            Debug.Log("[AttackArrow] Initialized with RectTransform values: " +
                     $"anchors: {GetComponent<RectTransform>().anchorMin}-{GetComponent<RectTransform>().anchorMax}, " +
                     $"position: {GetComponent<RectTransform>().localPosition}, " +
                     $"size: {GetComponent<RectTransform>().sizeDelta}");
        }

        private void ResetRectTransform()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Reset all RectTransform values to defaults
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(100, 100);
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;

                // Force layout update
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void CreateArrowComponents()
        {
            // Create arrow body
            GameObject bodyGO = new GameObject("ArrowBody");
            arrowBody = bodyGO.AddComponent<RectTransform>();
            arrowBody.SetParent(transform, false);
            
            // Set the body to expand from the left edge to the right edge
            arrowBody.anchorMin = new Vector2(0, 0.5f);
            arrowBody.anchorMax = new Vector2(1, 0.5f);
            arrowBody.pivot = new Vector2(0.5f, 0.5f);
            arrowBody.sizeDelta = new Vector2(0, arrowWidth);
            arrowBody.anchoredPosition = Vector2.zero;
            
            // Add image to body
            Image bodyImage = bodyGO.AddComponent<Image>();
            bodyImage.sprite = arrowBodySprite != null ? arrowBodySprite : CreateBodySprite();
            bodyImage.color = arrowColor;
            
            // Create arrow head
            GameObject headGO = new GameObject("ArrowHead");
            arrowHead = headGO.AddComponent<RectTransform>();
            arrowHead.SetParent(transform, false);
            
            // Position the head at the RIGHT edge of the arrow
            arrowHead.anchorMin = new Vector2(1, 0.5f);
            arrowHead.anchorMax = new Vector2(1, 0.5f);
            arrowHead.pivot = new Vector2(0, 0.5f); // Pivot at the left side of the head
            arrowHead.sizeDelta = new Vector2(arrowHeadSize, arrowHeadSize);
            arrowHead.anchoredPosition = Vector2.zero;
            
            // Add image to head and rotate it 180 degrees
            Image headImage = headGO.AddComponent<Image>();
            headImage.sprite = arrowHeadSprite != null ? arrowHeadSprite : CreateTriangleSprite();
            headImage.color = arrowColor;
            // Rotate the head 180 degrees to point in the right direction
            headGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
            
            Debug.Log("[AttackArrow] Created arrow components");
        }

        private Sprite CreateBodySprite()
        {
            int width = 32;
            int height = 32;
            Texture2D texture = new Texture2D(width, height);
            
            // Clear texture with transparent pixels
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            texture.SetPixels(colors);
            
            // Draw a rectangle in the center
            int thickness = height / 2; // Make the line thicker for better visibility
            int yStart = (height - thickness) / 2;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = yStart; y < yStart + thickness; y++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateTriangleSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);

            // Clear texture with transparent pixels
            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            texture.SetPixels(colors);

            // Define the points of a classic arrow head triangle
            // Pointing right-to-left (arrow head on the left side)
            Vector2[] points = new Vector2[]
            {
                new Vector2(0, size/2),            // Tip of the arrow (left middle)
                new Vector2(size-1, 0),            // Bottom right corner
                new Vector2(size-1, size-1)        // Top right corner
            };

            // Draw the triangle
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (IsPointInTriangle(new Vector2(x, y), points[0], points[1], points[2]))
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();
            // Create with pivot at the tip of the arrow (left side)
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0, 0.5f));
        }

        // Helper to check if a point is inside a triangle
        private bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float d1, d2, d3;
            bool hasNeg, hasPos;

            d1 = Sign(pt, v1, v2);
            d2 = Sign(pt, v2, v3);
            d3 = Sign(pt, v3, v1);

            hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public void Initialize(MatchView matchView)
        {
            this.matchView = matchView;
        }

        public void StartDrawing()
        {
            if (arrowBody == null || arrowHead == null)
            {
                Debug.LogError("[AttackArrow] Cannot start drawing: Arrow components missing");
                return;
            }

            isActive = true;
            gameObject.SetActive(true);

            // Reset transform to avoid any accumulated issues
            ResetRectTransform();

            // Make sure arrow components are active but initially invisible
            // They will be properly shown when positions are updated
            arrowBody.gameObject.SetActive(false);
            arrowHead.gameObject.SetActive(false);

            Debug.Log($"[AttackArrow] Started drawing, GameObject active: {gameObject.activeSelf}");
        }

        public void UpdateStartPosition(Vector2 startPosition)
        {
            if (!isActive)
            {
                Debug.LogWarning("[AttackArrow] Cannot update start position: Arrow is not active");
                return;
            }

            // Ensure the GameObject is active
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Store the start position
            this.startPosition = startPosition;
            Debug.Log($"[AttackArrow] Updated start position to {startPosition}");
        }

        public void UpdateEndPosition(Vector2 endPosition)
        {
            if (!isActive)
            {
                Debug.LogWarning("[AttackArrow] Cannot update end position: Arrow is not active");
                return;
            }

            // Ensure the GameObject is active
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Update the arrow with the current position and the new end position
            UpdateArrowTransform(startPosition, endPosition);

            Debug.Log($"[AttackArrow] Updated end position to {endPosition}");
        }

        private void UpdateArrowTransform(Vector2 sourcePos, Vector2 targetPos)
        {
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas == null)
                {
                    Debug.LogError("[AttackArrow] No canvas found for position conversion");
                    return;
                }
            }

            // Convert screen positions to local canvas positions for proper distance calculation
            Vector2 canvasSourcePos = ScreenToCanvasPoint(sourcePos);
            Vector2 canvasTargetPos = ScreenToCanvasPoint(targetPos);
            
            // Calculate the direction and distance in canvas space
            Vector2 direction = canvasTargetPos - canvasSourcePos;
            float distance = direction.magnitude;

            // Don't draw if too short
            if (distance < 1f)
            {
                arrowBody.gameObject.SetActive(false);
                arrowHead.gameObject.SetActive(false);
                return;
            }

            // Make sure arrow components are active
            arrowBody.gameObject.SetActive(true);
            arrowHead.gameObject.SetActive(true);

            // Calculate angle in degrees
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Set position - we need to position in screen space, but size in canvas space
            transform.position = (sourcePos + targetPos) * 0.5f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Set the size of the arrow to span the entire distance in canvas coordinates
            GetComponent<RectTransform>().sizeDelta = new Vector2(distance, arrowWidth);

            Debug.Log($"[AttackArrow] Arrow from {sourcePos} to {targetPos}, canvas distance: {distance}");
        }
        
        // Convert screen position to canvas local position
        private Vector2 ScreenToCanvasPoint(Vector2 screenPoint)
        {
            if (parentCanvas == null) return screenPoint;
            
            // Get the appropriate camera
            Camera camera = null;
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                camera = parentCanvas.worldCamera;
            else if (parentCanvas.renderMode == RenderMode.WorldSpace)
                camera = parentCanvas.worldCamera ?? Camera.main;
                
            // Get local point in the canvas rect
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            Vector2 localPoint;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camera, out localPoint))
            {
                return localPoint;
            }
            
            return screenPoint;
        }

        public void CancelDrawing()
        {
            isActive = false;
            gameObject.SetActive(false);
            Debug.Log("[AttackArrow] Drawing cancelled, GameObject disabled");
        }
    }
}
