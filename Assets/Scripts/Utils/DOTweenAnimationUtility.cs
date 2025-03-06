using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;

namespace Kardx.Utils
{
    /// <summary>
    /// Utility class for common DOTween animations used throughout the Kardx game.
    /// Provides standard animation patterns to maintain consistency across the UI.
    /// </summary>
    public static class DOTweenAnimationUtility
    {
        #region Card Animations

        /// <summary>
        /// Animates a card zoom in effect for card detail view
        /// </summary>
        /// <param name="cardTransform">The card's RectTransform</param>
        /// <param name="startScale">Initial scale (typically smaller)</param>
        /// <param name="endScale">Target scale (typically original size)</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="rotationAmount">Optional Y-axis rotation for flip effect (in degrees)</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardZoomIn(
            RectTransform cardTransform,
            Vector3 startScale,
            Vector3 endScale,
            float duration = 0.5f,
            float rotationAmount = 180f)
        {
            if (cardTransform == null)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Set initial state
            cardTransform.localScale = startScale;
            
            if (rotationAmount != 0)
            {
                // Create a flip effect by rotating around Y-axis
                cardTransform.localRotation = Quaternion.Euler(0, rotationAmount, 0);
                
                // Animate rotation to zero (front facing)
                sequence.Append(
                    cardTransform.DOLocalRotate(Vector3.zero, duration * 0.6f)
                    .SetEase(Ease.OutQuad)
                );
            }

            // Zoom to final scale
            sequence.Join(
                cardTransform.DOScale(endScale, duration)
                .SetEase(Ease.OutBack)
            );

            return sequence;
        }

        /// <summary>
        /// Animates a card zoom out effect for card detail view
        /// </summary>
        /// <param name="cardTransform">The card's RectTransform</param>
        /// <param name="startScale">Initial scale (typically original size)</param>
        /// <param name="endScale">Target scale (typically smaller)</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="rotationAmount">Optional Y-axis rotation for flip effect (in degrees)</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardZoomOut(
            RectTransform cardTransform,
            Vector3 startScale,
            Vector3 endScale,
            float duration = 0.4f,
            float rotationAmount = 180f)
        {
            if (cardTransform == null)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Set initial state
            cardTransform.localScale = startScale;
            
            // Zoom to smaller scale
            sequence.Append(
                cardTransform.DOScale(endScale, duration)
                .SetEase(Ease.InBack)
            );
            
            if (rotationAmount != 0)
            {
                // Create a flip effect by rotating around Y-axis
                sequence.Join(
                    cardTransform.DOLocalRotate(new Vector3(0, rotationAmount, 0), duration * 0.8f)
                    .SetEase(Ease.InQuad)
                );
            }

            return sequence;
        }

        /// <summary>
        /// Animates a card appearing with scale and position effects
        /// </summary>
        /// <param name="cardTransform">The card's RectTransform</param>
        /// <param name="originalScale">The target scale to animate to</param>
        /// <param name="originalPosition">The target position to animate to</param>
        /// <param name="slideDistance">How far the card should slide from</param>
        /// <param name="duration">Animation duration</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardAppear(
            RectTransform cardTransform,
            Vector3 originalScale,
            Vector2 originalPosition,
            float slideDistance = 50f,
            float duration = 0.4f)
        {
            if (cardTransform == null)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Scale animation
            sequence.Append(
                cardTransform.DOScale(originalScale, duration)
                .SetEase(Ease.OutBack)
            );

            // Position animation (slide up)
            sequence.Join(
                cardTransform.DOAnchorPos(originalPosition, duration * 0.8f)
                .SetEase(Ease.OutQuint)
            );

            return sequence;
        }

        /// <summary>
        /// Animates a card disappearing with scale and position effects
        /// </summary>
        /// <param name="cardTransform">The card's RectTransform</param>
        /// <param name="originalScale">The starting scale</param>
        /// <param name="originalPosition">The starting position</param>
        /// <param name="slideDistance">How far the card should slide to</param>
        /// <param name="duration">Animation duration</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardDisappear(
            RectTransform cardTransform,
            Vector3 originalScale,
            Vector2 originalPosition,
            float slideDistance = 50f,
            float duration = 0.25f)
        {
            if (cardTransform == null)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Scale animation
            sequence.Append(
                cardTransform.DOScale(originalScale * 0.7f, duration)
                .SetEase(Ease.InBack)
            );

            // Position animation (slide down)
            sequence.Join(
                cardTransform.DOAnchorPos(originalPosition + new Vector2(0, -slideDistance), duration)
                .SetEase(Ease.InBack)
            );

            return sequence;
        }

        /// <summary>
        /// Animates a card dying with scale, position, rotation and fade effects
        /// </summary>
        /// <param name="cardTransform">The card's transform</param>
        /// <param name="canvasGroup">Canvas group for fading</param>
        /// <param name="floatDistance">How far the card should float up</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="onComplete">Action to execute when animation completes</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardDeath(
            Transform cardTransform,
            CanvasGroup canvasGroup,
            float floatDistance = 100f,
            float duration = 0.8f,
            Action onComplete = null)
        {
            if (cardTransform == null)
                return null;

            Sequence sequence = DOTween.Sequence();
            Vector3 floatUpPosition = cardTransform.position + new Vector3(0, floatDistance, 0);

            // Float up
            sequence.Append(
                cardTransform.DOMove(floatUpPosition, duration * 0.8f)
                .SetEase(Ease.OutQuad)
            );

            // Scale down
            sequence.Join(
                cardTransform.DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
            );

            // Rotate
            sequence.Join(
                cardTransform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-45f, 45f)), duration * 0.6f)
                .SetEase(Ease.OutQuad)
            );

            // Fade out (if canvas group exists)
            if (canvasGroup != null)
            {
                sequence.Join(
                    canvasGroup.DOFade(0, duration * 0.6f)
                    .SetEase(Ease.InQuad)
                );
            }

            if (onComplete != null)
            {
                sequence.OnComplete(() => onComplete());
            }

            return sequence;
        }

        /// <summary>
        /// Animates a card attacking with a lunge forward, rotation, and optional flash effect
        /// </summary>
        /// <param name="cardTransform">The card's transform</param>
        /// <param name="targetPosition">Target position to attack toward (optional)</param>
        /// <param name="lungeDistance">How far to lunge forward if no target is specified</param>
        /// <param name="duration">Total animation duration</param>
        /// <param name="flashColor">Color to flash the card during attack (use Color.clear for no flash)</param>
        /// <param name="onImpactCallback">Action to execute at the moment of impact</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateCardAttack(
            Transform cardTransform,
            Vector3? targetPosition = null,
            float lungeDistance = 30f,
            float duration = 0.5f,
            Color? flashColor = null,
            Action onImpactCallback = null)
        {
            if (cardTransform == null)
                return null;

            // Store original state
            Vector3 originalPosition = cardTransform.position;
            Quaternion originalRotation = cardTransform.rotation;
            
            // Calculate attack direction
            Vector3 attackPosition;
            
            if (targetPosition.HasValue)
            {
                // If target specified, calculate direction toward target but not all the way
                Vector3 direction = (targetPosition.Value - originalPosition).normalized;
                attackPosition = originalPosition + (direction * lungeDistance);
            }
            else
            {
                // Default: lunge forward along local forward axis
                attackPosition = originalPosition + (cardTransform.forward * lungeDistance);
            }
            
            // Get sprite renderer for flash effect
            SpriteRenderer spriteRenderer = cardTransform.GetComponentInChildren<SpriteRenderer>();
            Image cardImage = cardTransform.GetComponentInChildren<Image>();
            Color originalColor = Color.white;
            
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            else if (cardImage != null)
            {
                originalColor = cardImage.color;
            }
            
            Sequence sequence = DOTween.Sequence();
            
            // Wind-up (slight pull-back and rotation)
            sequence.Append(
                cardTransform.DOMove(
                    originalPosition - (attackPosition - originalPosition).normalized * 10f, 
                    duration * 0.2f
                ).SetEase(Ease.OutQuad)
            );
            
            sequence.Join(
                cardTransform.DORotate(
                    new Vector3(0, 0, -5f), 
                    duration * 0.2f
                ).SetEase(Ease.OutQuad)
            );
            
            // Attack lunge
            sequence.Append(
                cardTransform.DOMove(
                    attackPosition, 
                    duration * 0.3f
                ).SetEase(Ease.InQuad)
            );
            
            // Add rotation during attack
            sequence.Join(
                cardTransform.DORotate(
                    new Vector3(0, 0, 10f), 
                    duration * 0.3f
                ).SetEase(Ease.InOutQuad)
            );
            
            // Flash effect at impact moment
            if (flashColor.HasValue && flashColor.Value != Color.clear)
            {
                if (spriteRenderer != null)
                {
                    sequence.InsertCallback(duration * 0.4f, () => 
                    {
                        spriteRenderer.color = flashColor.Value;
                    });
                    
                    sequence.Insert(duration * 0.5f, 
                        spriteRenderer.DOColor(originalColor, duration * 0.2f)
                    );
                }
                else if (cardImage != null)
                {
                    sequence.InsertCallback(duration * 0.4f, () => 
                    {
                        cardImage.color = flashColor.Value;
                    });
                    
                    sequence.Insert(duration * 0.5f, 
                        cardImage.DOColor(originalColor, duration * 0.2f)
                    );
                }
            }
            
            // Execute impact callback
            if (onImpactCallback != null)
            {
                sequence.InsertCallback(duration * 0.5f, () => onImpactCallback());
            }
            
            // Return to original position
            sequence.Append(
                cardTransform.DOMove(
                    originalPosition, 
                    duration * 0.3f
                ).SetEase(Ease.OutQuad)
            );
            
            // Return to original rotation
            sequence.Join(
                cardTransform.DORotate(
                    originalRotation.eulerAngles, 
                    duration * 0.3f
                ).SetEase(Ease.OutQuad)
            );
            
            return sequence;
        }

        #endregion

        #region UI Text and Element Animations

        /// <summary>
        /// Animates text elements sequentially fading in
        /// </summary>
        /// <param name="textElements">Array of text elements to animate</param>
        /// <param name="duration">Duration per text element</param>
        /// <param name="stagger">Delay between elements</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateTextElementsSequentialFadeIn(
            TextMeshProUGUI[] textElements,
            float duration = 0.3f,
            float stagger = 0.1f)
        {
            if (textElements == null || textElements.Length == 0)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Process each text element
            for (int i = 0; i < textElements.Length; i++)
            {
                if (textElements[i] == null)
                    continue;

                // Ensure each text has a CanvasGroup component
                var canvasGroup = textElements[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = textElements[i].gameObject.AddComponent<CanvasGroup>();
                }
                
                // Set initial state
                canvasGroup.alpha = 0f;

                // Add to sequence
                sequence.Append(
                    canvasGroup.DOFade(1f, duration)
                    .SetEase(Ease.OutQuad)
                );
            }

            return sequence;
        }

        /// <summary>
        /// Animates text elements sequentially fading out
        /// </summary>
        /// <param name="textElements">Array of text elements to animate</param>
        /// <param name="duration">Duration per text element</param>
        /// <param name="stagger">Delay between elements</param>
        /// <returns>The animation sequence</returns>
        public static Sequence AnimateTextElementsSequentialFadeOut(
            TextMeshProUGUI[] textElements,
            float duration = 0.2f,
            float stagger = 0.05f)
        {
            if (textElements == null || textElements.Length == 0)
                return null;

            Sequence sequence = DOTween.Sequence();

            // Process each text element (in reverse order)
            for (int i = textElements.Length - 1; i >= 0; i--)
            {
                if (textElements[i] == null)
                    continue;

                // Ensure each text has a CanvasGroup component
                var canvasGroup = textElements[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = textElements[i].gameObject.AddComponent<CanvasGroup>();
                }

                // Add to sequence
                sequence.Append(
                    canvasGroup.DOFade(0f, duration)
                    .SetEase(Ease.InQuad)
                );
            }

            return sequence;
        }

        /// <summary>
        /// Animates a UI element popping in (scale effect)
        /// </summary>
        /// <param name="element">The UI element's transform</param>
        /// <param name="startScale">Initial scale</param>
        /// <param name="endScale">Target scale</param>
        /// <param name="duration">Animation duration</param>
        /// <returns>The tween</returns>
        public static Tween AnimateElementPopIn(
            Transform element,
            Vector3 startScale,
            Vector3 endScale,
            float duration = 0.3f)
        {
            if (element == null)
                return null;

            element.localScale = startScale;
            return element.DOScale(endScale, duration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Animates a UI panel sliding in
        /// </summary>
        /// <param name="panel">The panel's RectTransform</param>
        /// <param name="startPosition">Starting position</param>
        /// <param name="endPosition">Target position</param>
        /// <param name="duration">Animation duration</param>
        /// <returns>The tween</returns>
        public static Tween AnimatePanelSlideIn(
            RectTransform panel,
            Vector2 startPosition,
            Vector2 endPosition,
            float duration = 0.4f)
        {
            if (panel == null)
                return null;

            panel.anchoredPosition = startPosition;
            return panel.DOAnchorPos(endPosition, duration).SetEase(Ease.OutQuint);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Safely kills a tween if it's active
        /// </summary>
        /// <param name="tween">The tween to kill</param>
        public static void SafeKill(Tween tween)
        {
            if (tween != null && tween.IsActive() && tween.IsPlaying())
            {
                tween.Kill();
            }
        }

        /// <summary>
        /// Safely kills a sequence if it's active
        /// </summary>
        /// <param name="sequence">The sequence to kill</param>
        public static void SafeKill(Sequence sequence)
        {
            if (sequence != null && sequence.IsActive() && sequence.IsPlaying())
            {
                sequence.Kill();
            }
        }

        #endregion
    }
}
