using UnityEngine;

public class CatGunSkill : BaseSkill
{
    public CatGunSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 플레이어 위치에 머신건 고양이 소환
        // 고양이가 2초동안 지속, 무차별 난사
        Debug.Log($"Execute 호출됨: {cardData.CardType}");
        player.UseSkill_ServerRpc(cardData.ID);
    }
}