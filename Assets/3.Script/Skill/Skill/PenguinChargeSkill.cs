public class PenguinChargeSkill : BaseSkill
{
    public PenguinChargeSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 플레이어가 바라본 방향으로 돌진
        // 일자 직선, HP 10%에 해당하는 고정 데미지
        // 돌진을 맞을 시 뒤로 밀리는 넉백
        player.UseSkill_ServerRpc(cardData.ID);
    }
}