using Core.GS.GameUtils;

namespace Core.GS.Expansions.TrialsOfAtlantis;

public class ZoarkatPet : GameSummonedPet
{
    public override int MaxHealth
    {
        get { return Level*10; }
    }
    public override void OnAttackedByEnemy(AttackData ad) { }
    public ZoarkatPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
}