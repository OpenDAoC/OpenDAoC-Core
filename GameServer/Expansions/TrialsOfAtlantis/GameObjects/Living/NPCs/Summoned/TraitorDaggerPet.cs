using Core.GS.GameUtils;

namespace Core.GS.Expansions.TrialsOfAtlantis
{
    public class TraitorDaggerPet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 15; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public TraitorDaggerPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}