
namespace DOL.GS {
    public class SINeckBoss : GameNPC {
        
        private static SpellLine sl = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
        private Spell spell = SkillBase.FindSpell(31250, sl);
        
        public SINeckBoss() : base()
        {
        }

        public override void OnAttackEnemy(AttackData ad)
        {
            if(Util.Chance(20))
                CastSpell(spell, sl, false);
            
            base.OnAttackEnemy(ad);
        }
    }
}
