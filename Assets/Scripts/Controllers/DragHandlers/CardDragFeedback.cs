using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Kardx.Models.Cards;
using Kardx.Utils;
using Kardx.Views.Cards;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Visual feedback after successful drag actions.
    /// </summary>
    public static class CardDragFeedback
    {
        public static void PlayOrderDeployedPulse(Card deployedCard, Transform searchRoot)
        {
            if (deployedCard == null || searchRoot == null)
                return;

            CardView targetCardView = null;
            foreach (var cv in searchRoot.GetComponentsInChildren<CardView>())
            {
                if (cv.Card != null && cv.Card.InstanceId.Equals(deployedCard.InstanceId))
                {
                    targetCardView = cv;
                    break;
                }
            }

            if (targetCardView == null)
                return;

            Image cardImage = targetCardView.GetComponent<Image>();
            DOTweenAnimationUtility.AnimateHeartbeat(
                targetTransform: targetCardView.transform,
                targetImage: cardImage,
                heartbeatColor: new Color(0.8f, 0.2f, 0.2f, 0.5f),
                pulseScale: 1.15f,
                pulseDuration: 0.5f,
                pulseCount: 2
            );
        }
    }
}
