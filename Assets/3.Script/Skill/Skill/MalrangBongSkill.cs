public class MalrangBongSkill : BaseSkill
{
    public MalrangBongSkill(CardData card) : base(card) { }

    public override void Execute(PlayerNetwork player)
    {
        // 패시브 스킬 - Q로 발동 아님
        // 모든 총기가 제한되며 근접 무기인 말랑봉 하나로 통일
        // 스킬 카드의 황청 카드로 구성
        // Execute 호출 안 됨 - SetSkill에서 패시브 처리 필요

        // GameDataManager , GameUIManager 의 무기 장착 method 이용해서 무기 교체
    }
}