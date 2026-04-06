public class WaterBalloonSkill : BaseSkill
{
    public WaterBalloonSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 넓은 범위와 강한 폭발 소유
        // 피격 시 전체 피 100 기준으로 65 데미지
        // 강한 만큼 사용시 모든 자원(물) 소모
        // 같은 팀도 피격 됨
        player.UseSkill_ServerRpc(cardData.ID);
    }
}