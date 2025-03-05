using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI
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
            // Create arrow body if needed
            if (arrowBody == null)
            {
                GameObject bodyObj = new GameObject("ArrowBody");
                bodyObj.transform.SetParent(transform, false);
                arrowBody = bodyObj.AddComponent<RectTransform>();

                // Configure the body transform to stretch across the entire arrow
                arrowBody.anchorMin = new Vector2(0, 0.5f);
                arrowBody.anchorMax = new Vector2(1, 0.5f);
                arrowBody.pivot = new Vector2(0.5f, 0.5f);
                arrowBody.anchoredPosition = Vector2.zero;
                arrowBody.sizeDelta = new Vector2(0, arrowWidth); // Width is controlled by parent

                // Add an image component
                Image bodyImage = bodyObj.AddComponent<Image>();
                bodyImage.color = arrowColor;

                // Set the sprite or create a simple rectangle
                if (arrowBodySprite != null)
                {
                    bodyImage.sprite = arrowBodySprite;
                }
                else
                {
                    bodyImage.sprite = CreateRectangleSprite();
                }
            }

            // Create arrow head if needed
            if (arrowHead == null)
            {
                GameObject headObj = new GameObject("ArrowHead");
                headObj.transform.SetParent(transform, false);
                arrowHead = headObj.AddComponent<RectTransform>();

                // Configure the head transform to be at the end of the arrow
                arrowHead.anchorMin = new Vector2(1, 0.5f);
                arrowHead.anchorMax = new Vector2(1, 0.5f);
                arrowHead.pivot = new Vector2(0, 0.5f);
                arrowHead.sizeDelta = new Vector2(arrowHeadSize, arrowHeadSize);
                arrowHead.anchoredPosition = Vector2.zero;

                // Add an image component
                Image headImage = headObj.AddComponent<Image>();
                headImage.color = arrowColor;

                // Set the sprite or create a simple triangle
                if (arrowHeadSprite != null)
                {
                    headImage.sprite = arrowHeadSprite;
                }
                else
                {
                    headImage.sprite = CreateTriangleSprite();
                }
            }

            Debug.Log("[AttackArrow] Created arrow components");
        }

        private Sprite CreateRectangleSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
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

            // Draw a triangle
            for (int y = 0; y < size; y++)
            {
                int width = size - y * size / (size - 1);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y + (size - 1) / 2 - y / 2, Color.white);
                    texture.SetPixel(x, (size - 1) / 2 + y / 2, Color.white);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0, 0.5f));
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

            if (matchView != null)
            {
                matchView.SetLogText($"Screen: {sourcePos} → {targetPos}\nCanvas: {canvasSourcePos} → {canvasTargetPos}\nDistance: {distance}");
            }

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
