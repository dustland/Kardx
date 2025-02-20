using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CardFlip : MonoBehaviour
{
  public Sprite frontImage;  // 卡牌正面图片
  public Sprite backImage;   // 卡牌背面图片
  private bool isFront = false;  // 当前是否是正面
  private Image cardImage;  // Image 组件

  void Start()
  {
    cardImage = GetComponent<Image>();  // 获取 Image 组件
    cardImage.sprite = backImage;  // 初始显示背面
  }

  public void FlipCard()
  {
    transform.DORotate(new Vector3(0, 90, 0), 0.3f).OnComplete(() =>
    {
      // 翻转到 90 度后，切换图片
      cardImage.sprite = isFront ? backImage : frontImage;
      isFront = !isFront;

      // 继续旋转回 0 度
      transform.DORotate(new Vector3(0, 0, 0), 0.3f);
    });
  }
}