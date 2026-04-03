public abstract class BaseSkill
{
    protected CardData cardData;

    public BaseSkill(CardData card)
    {
        cardData = card;
    }

    public abstract void Execute(PlayerNetwork player);
}
