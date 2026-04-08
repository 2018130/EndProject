public class SharkTubeSkill : BaseSkill
{
    public SharkTubeSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 즉시 상어튜브 소환
        // 경로에 있는 모든 아군 적군을 밀침
        // 시전 종료시 자연스럽게 내려옴
        player.UseSkill_ServerRpc(cardData.ID);
    }
}