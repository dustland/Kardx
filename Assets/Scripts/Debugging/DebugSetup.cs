using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Kardx.Views.Match;
using Kardx.Controllers.DragHandlers;

namespace Kardx
{
    /// <summary>
    /// Automatically sets up debugging tools for the scene.
    /// Attach this to a GameObject in the scene to enable debugging features.
    /// </summary>
    public class DebugSetup : MonoBehaviour
    {
        [SerializeField]
        private bool enableEventDebugging = true;

        [SerializeField]
        private bool debugOpponentCardSlots = true;

        [SerializeField]
        private bool debugPlayerCardSlots = false;

        [SerializeField]
        private bool debugDragHandlers = false;

        private void Start()
        {
            // Wait a frame to ensure all objects are initialized
            StartCoroutine(SetupDebugComponents());
        }

        private IEnumerator SetupDebugComponents()
        {
            // Wait for a frame to ensure everything is initialized
            yield return null;

            if (enableEventDebugging)
            {
                if (debugOpponentCardSlots)
                {
                    SetupOpponentCardSlotDebugging();
                }

                if (debugPlayerCardSlots)
                {
                    SetupPlayerCardSlotDebugging();
                }

                if (debugDragHandlers)
                {
                    SetupDragHandlerDebugging();
                }
            }

            Debug.Log("[DebugSetup] Debug components setup complete");
        }

        private void SetupOpponentCardSlotDebugging()
        {
            OpponentCardSlot[] slots = FindObjectsByType<OpponentCardSlot>(FindObjectsSortMode.None);
            Debug.Log($"[DebugSetup] Found {slots.Length} OpponentCardSlots");

            foreach (OpponentCardSlot slot in slots)
            {
                // Add EventDebugger if it doesn't already have one
                if (slot.GetComponent<EventDebugger>() == null)
                {
                    EventDebugger debugger = slot.gameObject.AddComponent<EventDebugger>();
                    Debug.Log($"[DebugSetup] Added EventDebugger to {slot.gameObject.name}");
                }

                // Ensure the slot has a proper Image component for raycasting
                UnityEngine.UI.Image image = slot.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    // Make sure it has some alpha for raycasting
                    Color color = image.color;
                    if (color.a <= 0)
                    {
                        color.a = 0.01f;
                        image.color = color;
                        Debug.Log($"[DebugSetup] Fixed image alpha on {slot.gameObject.name}");
                    }

                    // Ensure raycast target is enabled
                    if (!image.raycastTarget)
                    {
                        image.raycastTarget = true;
                        Debug.Log($"[DebugSetup] Enabled raycastTarget on {slot.gameObject.name}");
                    }
                }
            }
        }

        private void SetupPlayerCardSlotDebugging()
        {
            PlayerCardSlot[] slots = FindObjectsOfType<PlayerCardSlot>();
            Debug.Log($"[DebugSetup] Found {slots.Length} PlayerCardSlots");

            foreach (PlayerCardSlot slot in slots)
            {
                // Add EventDebugger if it doesn't already have one
                if (slot.GetComponent<EventDebugger>() == null)
                {
                    EventDebugger debugger = slot.gameObject.AddComponent<EventDebugger>();
                    Debug.Log($"[DebugSetup] Added EventDebugger to {slot.gameObject.name}");
                }
            }
        }

        private void SetupDragHandlerDebugging()
        {
            CardDragController[] handlers = FindObjectsByType<CardDragController>(FindObjectsSortMode.None);
            Debug.Log($"[DebugSetup] Found {handlers.Length} CardDragControllers");

            foreach (CardDragController handler in handlers)
            {
                // Add EventDebugger if it doesn't already have one
                if (handler.GetComponent<EventDebugger>() == null)
                {
                    EventDebugger debugger = handler.gameObject.AddComponent<EventDebugger>();
                    Debug.Log($"[DebugSetup] Added EventDebugger to {handler.gameObject.name}");
                }
            }
        }
    }
}
