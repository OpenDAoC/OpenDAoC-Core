namespace DOL.GS
{
    public class ElementalPet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public ElementalPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}