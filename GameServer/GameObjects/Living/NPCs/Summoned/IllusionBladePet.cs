using Core.GS.GameUtils;

namespace Core.GS
{
    public class IllusionBladePet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public IllusionBladePet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}