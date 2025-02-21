using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
  // 点击按钮时调用
  public void GoToShopScene()
  {
    SceneManager.LoadScene("ShopScene");
  }

  public void GoToGameScene()
  {
    SceneManager.LoadScene("GameScene");
  }
}