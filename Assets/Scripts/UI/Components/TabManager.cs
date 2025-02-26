using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI.Components.Common
{
    public class TabManager : MonoBehaviour
    {
        [Header("Tab Panels")]
        [SerializeField]
        private GameObject[] panels;

        [Header("Tab Buttons")]
        [SerializeField]
        private Button[] tabButtons;

        [Header("Visual States")]
        [SerializeField]
        private Color activeTabColor = Color.white;

        [SerializeField]
        private Color inactiveTabColor = Color.gray;

        private int currentTabIndex = 0;

        private void Start()
        {
            // Set up tab button listeners
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i; // Capture the index for the lambda
                tabButtons[i].onClick.AddListener(() => SwitchToTab(index));
            }

            // Show initial tab
            SwitchToTab(currentTabIndex);
        }

        public void SwitchToTab(int index)
        {
            if (index < 0 || index >= panels.Length)
                return;

            // Hide all panels
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == index);
                UpdateTabVisuals(i, i == index);
            }

            currentTabIndex = index;
        }

        private void UpdateTabVisuals(int tabIndex, bool isActive)
        {
            if (tabIndex < 0 || tabIndex >= tabButtons.Length)
                return;

            var colors = tabButtons[tabIndex].colors;
            colors.normalColor = isActive ? activeTabColor : inactiveTabColor;
            tabButtons[tabIndex].colors = colors;
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabButtons[i].onClick.RemoveListener(() => SwitchToTab(index));
            }
        }
    }
}
