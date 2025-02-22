using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kardx.UI.Scenes.Common
{
  public class SceneSwitcher : MonoBehaviour
  {
    [Header("Scene Names")]
    [SerializeField] private string homeScene = "HomeScene";
    [SerializeField] private string battleScene = "BattleScene";
    [SerializeField] private string deckBuilderScene = "DeckBuilderScene";
    [SerializeField] private string cardScene = "CardScene";

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private GameObject transitionEffect;

    public void GoHome()
    {
      // StartCoroutine(LoadSceneWithTransition(homeScene));
      Debug.Log("Going to home scene");
      SceneManager.LoadScene(homeScene);
    }

    public void GoToBattle()
    {
      // StartCoroutine(LoadSceneWithTransition(battleScene));
      Debug.Log("Going to battle scene");
      SceneManager.LoadScene(battleScene);
    }

    public void GoToDeckBuilder()
    {
      // StartCoroutine(LoadSceneWithTransition(deckBuilderScene));
      Debug.Log("Going to deck builder scene");
      SceneManager.LoadScene(deckBuilderScene);
    }

    public void GoToCard()
    {
      Debug.Log("Going to card scene");
      // StartCoroutine(LoadSceneWithTransition(shopScene));
      SceneManager.LoadScene(cardScene);
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