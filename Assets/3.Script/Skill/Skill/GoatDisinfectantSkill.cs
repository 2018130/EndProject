public class GoatDisinfectantSkill : BaseSkill
{
    public GoatDisinfectantSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 염소가 나와 범위 내(5.0 Unit) 즉시 피 회복
        // 피 회복은 10~15%로 구성
        // 범위 안에 있을 시 아군 적군 둘 다 회복 가능
        player.UseSkill_ServerRpc(cardData.ID);
    }
}