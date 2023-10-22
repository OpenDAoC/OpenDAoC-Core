using Core.GS.GameUtils;

namespace Core.GS.Expansions.TrialsOfAtlantis;

public class AstralPet : GameSummonedPet
{
    public override int MaxHealth
    {
        get { return Level * 10; }
    }

    public override void OnAttackedByEnemy(AttackData ad) { }
    public AstralPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
}