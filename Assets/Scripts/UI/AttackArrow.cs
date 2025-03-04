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
        private Color arrowColor = new Color(1f, 0f, 0f, 0.8f);

        [SerializeField]
        private float arrowWidth = 10f;

        [SerializeField]
        private float arrowHeadSize = 20f;

        private Canvas parentCanvas;
        private bool isActive = false;
        private Vector2 targetPosition;

        private void Awake()
        {
            // Reset the RectTransform completely
            ResetRectTransform();

            // Find the parent canvas
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                // Find any canvas in the scene
                parentCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
                if (parentCanvas != null)
                {
                    transform.SetParent(parentCanvas.transform, false);

                    // Reset again after reparenting
                    ResetRectTransform();
                }
            }

            // Create arrow components if they don't exist
            if (arrowBody == null || arrowHead == null)
            {
                CreateArrowComponents();
            }
            else
            {
                // Configure existing components
                ConfigureArrowComponents();
            }

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

        private void ConfigureArrowComponents()
        {
            if (arrowBody != null)
            {
                Image bodyImage = arrowBody.GetComponent<Image>();
                if (bodyImage != null)
                {
                    bodyImage.color = arrowColor;
                }

                // Set the arrow body to stretch from left to right
                arrowBody.anchorMin = new Vector2(0, 0.5f);
                arrowBody.anchorMax = new Vector2(0, 0.5f);
                arrowBody.pivot = new Vector2(0, 0.5f);
                arrowBody.anchoredPosition = Vector2.zero;
            }

            if (arrowHead != null)
            {
                Image headImage = arrowHead.GetComponent<Image>();
                if (headImage != null)
                {
                    headImage.color = arrowColor;
                }

                // Set the arrow head anchors and pivot
                arrowHead.anchorMin = new Vector2(0, 0.5f);
                arrowHead.anchorMax = new Vector2(0, 0.5f);
                arrowHead.pivot = new Vector2(0, 0.5f);
                arrowHead.sizeDelta = new Vector2(arrowHeadSize, arrowHeadSize);
                arrowHead.anchoredPosition = Vector2.zero;
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
                Image bodyImage = bodyObj.AddComponent<Image>();
                bodyImage.color = arrowColor;

                // Create a simple texture for the arrow body
                Texture2D bodyTexture = new Texture2D(1, 1);
                bodyTexture.SetPixel(0, 0, Color.white);
                bodyTexture.Apply();
                Sprite bodySprite = Sprite.Create(bodyTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                bodyImage.sprite = bodySprite;

                // Set the arrow body to stretch from left to right
                arrowBody.anchorMin = new Vector2(0, 0.5f);
                arrowBody.anchorMax = new Vector2(0, 0.5f);
                arrowBody.pivot = new Vector2(0, 0.5f);
                arrowBody.sizeDelta = new Vector2(100, arrowWidth);
                arrowBody.anchoredPosition = Vector2.zero;
            }

            // Create arrow head if needed
            if (arrowHead == null)
            {
                GameObject headObj = new GameObject("ArrowHead");
                headObj.transform.SetParent(transform, false);
                arrowHead = headObj.AddComponent<RectTransform>();
                Image headImage = headObj.AddComponent<Image>();
                headImage.color = arrowColor;

                // Create a triangle texture for the arrow head
                Texture2D headTexture = CreateTriangleTexture();
                Sprite headSprite = Sprite.Create(headTexture, new Rect(0, 0, headTexture.width, headTexture.height), new Vector2(0, 0.5f));
                headImage.sprite = headSprite;

                // Set the arrow head size and position
                arrowHead.anchorMin = new Vector2(0, 0.5f);
                arrowHead.anchorMax = new Vector2(0, 0.5f);
                arrowHead.pivot = new Vector2(0, 0.5f);
                arrowHead.sizeDelta = new Vector2(arrowHeadSize, arrowHeadSize);
                arrowHead.anchoredPosition = Vector2.zero;
            }

            Debug.Log("[AttackArrow] Created arrow components");
        }

        private Texture2D CreateTriangleTexture()
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
            return texture;
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
            Vector2 startScreenPos = startPosition;
            
            // Set the arrow's position directly
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.position = startScreenPos;
            
            Debug.Log($"[AttackArrow] Updated start position to {startScreenPos}");
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
            
            // Store the end position
            Vector2 endScreenPos = endPosition;
            
            // Update the arrow with the current position and the new end position
            UpdateArrowTransform(transform.position, endScreenPos);
            
            Debug.Log($"[AttackArrow] Updated end position to {endScreenPos}");
        }

        private void UpdateArrowTransform(Vector2 sourcePos, Vector2 targetPos)
        {
            // Calculate the direction and distance
            Vector2 direction = targetPos - sourcePos;
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
            
            // Position the arrow body at the source position
            arrowBody.position = sourcePos;
            
            // Calculate the midpoint between source and target for the body pivot
            Vector2 midPoint = (sourcePos + targetPos) / 2;
            arrowBody.position = midPoint;
            
            // Set the body length and rotation
            arrowBody.sizeDelta = new Vector2(distance, arrowWidth);
            arrowBody.rotation = Quaternion.Euler(0, 0, angle);
            
            // Position the arrow head directly at the target position
            arrowHead.position = targetPos;
            arrowHead.rotation = Quaternion.Euler(0, 0, angle);

            Debug.Log($"[AttackArrow] Arrow drawn from {sourcePos} to {targetPos}, distance: {distance}, angle: {angle}");
        }

        public void CancelDrawing()
        {
            isActive = false;
            gameObject.SetActive(false);
            Debug.Log("[AttackArrow] Drawing cancelled, GameObject disabled");
        }
    }
}
