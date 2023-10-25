using Core.GS.GameUtils;

namespace Core.GS;

public class AstralPet : GameSummonedPet
{
    public override int MaxHealth
    {
        get { return Level * 10; }
    }

    public override void OnAttackedByEnemy(AttackData ad) { }
    public AstralPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
}