using UnityEngine;

public class TurnManager : MonoBehaviour
{
  public static TurnManager Instance;
  private bool isPlayerTurn = true;

  void Awake()
  {
    Instance = this;
  }

  public void EndTurn()
  {
    isPlayerTurn = !isPlayerTurn;
    Debug.Log("当前回合：" + (isPlayerTurn ? "玩家" : "AI"));

    if (!isPlayerTurn)
    {
      Invoke("AITurn", 2f);
    }
  }

  void AITurn()
  {
    Debug.Log("AI 执行动作...");
    EndTurn();
  }
}