using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kardx.UI.Scenes.Common
{
  public class SceneSwitcher : MonoBehaviour
  {
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string battleScene = "Battle";
    [SerializeField] private string deckBuilderScene = "DeckBuilder";
    [SerializeField] private string shopScene = "Shop";

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private GameObject transitionEffect;

    public void LoadMainMenu()
    {
      StartCoroutine(LoadSceneWithTransition(mainMenuScene));
    }

    public void LoadBattle()
    {
      StartCoroutine(LoadSceneWithTransition(battleScene));
    }

    public void LoadDeckBuilder()
    {
      StartCoroutine(LoadSceneWithTransition(deckBuilderScene));
    }

    public void LoadShop()
    {
      StartCoroutine(LoadSceneWithTransition(shopScene));
    }

    private System.Collections.IEnumerator LoadSceneWithTransition(string sceneName)
    {
      // Show transition effect
      if (transitionEffect != null)
      {
        transitionEffect.SetActive(true);
      }

      // Wait for transition animation
      yield return new WaitForSeconds(transitionDuration);

      // Load the new scene
      SceneManager.LoadSceneAsync(sceneName);
    }
  }
}