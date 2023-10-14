namespace DOL.GS.Spells
{
    //no shared timer
    
    [SpellHandler("UnmakeCrystalseed")]
    public class UnmakeCrystalseedSpell : SpellHandler
    {
        /// <summary>
        /// Execute unmake crystal seed spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
            if (target == null || !target.IsAlive)
                return;

            foreach (GameNPC item in target.GetNPCsInRadius((ushort)m_spell.Radius))
            {
                if (item != null && item is GameMine)
                {
                    (item as GameMine).Delete();
                }
            }
        }

        public UnmakeCrystalseedSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}