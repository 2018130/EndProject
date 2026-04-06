using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] private Image cardIcon;
    [SerializeField] private Image coolTimeImg;
    [SerializeField] private TMP_Text skillCoolTime;

    public void SetSkill(CardData card)
    {
        cardIcon.sprite = card.SkillIcon;
        coolTimeImg.fillAmount = 0f; // 쿨타임 초기화
        skillCoolTime.text = "";
    }

    public void UpdateCoolTime(float remaining, float total)
    {
        coolTimeImg.fillAmount = remaining / total;
        skillCoolTime.text = remaining > 0 ? Mathf.CeilToInt(remaining).ToString() : "";
    }

    public void ClearSkill()
    {
        cardIcon.sprite = null;
        coolTimeImg.fillAmount = 0f;
        skillCoolTime.text = "";
    }
}