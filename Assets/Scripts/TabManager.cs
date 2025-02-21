using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
  // 各个内容面板
  public GameObject panel1;
  public GameObject panel2;
  public GameObject panel3;

  // Tab 按钮（可选，用于切换状态）
  public Button tab1;
  public Button tab2;
  public Button tab3;

  void Start()
  {
    // 默认显示武器面板
    ShowPanel(panel1);

    // 添加按钮点击事件（注意：若在编辑器中直接设置OnClick事件也可以）
    tab1.onClick.AddListener(() => ShowPanel(panel1));
    tab2.onClick.AddListener(() => ShowPanel(panel2));
    tab3.onClick.AddListener(() => ShowPanel(panel3));
  }

  void ShowPanel(GameObject activePanel)
  {
    // 关闭所有面板
    panel1.SetActive(false);
    panel2.SetActive(false);
    panel3.SetActive(false);
    // 激活选中的面板
    activePanel.SetActive(true);
  }
}
