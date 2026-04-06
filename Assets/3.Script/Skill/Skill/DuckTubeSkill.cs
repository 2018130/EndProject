public class DuckTubeSkill : BaseSkill
{
    public DuckTubeSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 즉시 오리튜브 소환
        // 경로에 있는 모든 아군, 적군 태워서 다님
        // 시전이 끝나면 모든 사람들 날라감
        player.UseSkill_ServerRpc(cardData.ID);
    }
}