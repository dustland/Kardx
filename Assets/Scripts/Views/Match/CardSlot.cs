using UnityEngine;
using UnityEngine.UI;

namespace Kardx.Views.Match
{
    /// <summary>
    /// Base class for card slots in the battlefield.
    /// Provides common functionality for both player and opponent card slots.
    /// </summary>
    public abstract class CardSlot : MonoBehaviour
    {
        [SerializeField]
        protected Image highlightImage;

        [SerializeField]
        protected Transform cardContainer;

        /// <summary>
        /// The index of this slot in the battlefield
        /// </summary>
        protected int slotIndex;

        /// <summary>
        /// Gets the transform where cards should be placed
        /// </summary>
        public Transform CardContainer => cardContainer;

        /// <summary>
        /// Sets the index of this slot in the battlefield
        /// </summary>
        public void SetSlotIndex(int index)
        {
            slotIndex = index;
        }

        /// <summary>
        /// Gets the index of this slot in the battlefield
        /// </summary>
        public int GetSlotIndex()
        {
            return slotIndex;
        }

        /// <summary>
        /// Clears any highlight on this slot
        /// </summary>
        public virtual void ClearHighlight()
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = false;
            }
        }
    }
}
