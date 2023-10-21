namespace Core.GS;

public class HealingElementalPet : GameSummonedPet
{
    public override int MaxHealth
    {
        get { return Level * 10; }
    }

    public override void OnAttackedByEnemy(AttackData ad)
    {
    }

    public HealingElementalPet(INpcTemplate npcTemplate) : base(npcTemplate)
    {
    }
}