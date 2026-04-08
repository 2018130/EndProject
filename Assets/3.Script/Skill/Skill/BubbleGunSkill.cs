public class BubbleGunSkill : BaseSkill
{
    public BubbleGunSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 피격 시 상대 사야 일부를 3~4초 차단
        // 버블 이펙트를 화면에 표시
        player.UseSkill_ServerRpc(cardData.ID);
    }
}