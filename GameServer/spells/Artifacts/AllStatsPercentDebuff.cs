using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// All stats debuff spell handler
    /// </summary>
    [SpellHandler(eSpellType.AllStatsPercentDebuff)]
	public class AllStatsPercentDebuff : SpellHandler
	{
        protected int StrDebuff = 0;
        protected int DexDebuff = 0;
        protected int ConDebuff = 0;
        protected int EmpDebuff = 0;
        protected int QuiDebuff = 0;
        protected int IntDebuff = 0;
        protected int ChaDebuff = 0;
        protected int PieDebuff = 0;

		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect); 
			//effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)m_spell.Value;
            double percentValue = (m_spell.Value) / 100;
            StrDebuff = (int)((double)effect.Owner.GetModified(eProperty.Strength) * percentValue);
            DexDebuff = (int)((double)effect.Owner.GetModified(eProperty.Dexterity) * percentValue);
            ConDebuff = (int)((double)effect.Owner.GetModified(eProperty.Constitution) * percentValue);
            EmpDebuff = (int)((double)effect.Owner.GetModified(eProperty.Empathy) * percentValue);
            QuiDebuff = (int)((double)effect.Owner.GetModified(eProperty.Quickness) * percentValue);
            IntDebuff = (int)((double)effect.Owner.GetModified(eProperty.Intelligence) * percentValue);
            ChaDebuff = (int)((double)effect.Owner.GetModified(eProperty.Charisma) * percentValue);
            PieDebuff = (int)((double)effect.Owner.GetModified(eProperty.Piety) * percentValue);
            

            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += DexDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] += StrDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] += ConDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] += PieDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] += EmpDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] += QuiDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] += IntDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] += ChaDebuff;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumbrance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
		}
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
            double percentValue = (m_spell.Value) / 100;

            effect.Owner.DebuffCategory[(int)eProperty.Dexterity] -= DexDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Strength] -= StrDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Constitution] -= ConDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Piety] -= PieDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Empathy] -= EmpDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Quickness] -= QuiDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Intelligence] -= IntDebuff;
            effect.Owner.DebuffCategory[(int)eProperty.Charisma] -= ChaDebuff;

			if (effect.Owner is GamePlayer)
			{
				GamePlayer player = effect.Owner as GamePlayer;
				player.Out.SendCharStatsUpdate();
				player.UpdateEncumbrance();
				player.UpdatePlayerStatus();
				player.Out.SendUpdatePlayer();
			}
			return base.OnEffectExpires(effect, noMessages);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			base.ApplyEffectOnTarget(target);
			if (target.Realm == 0 || Caster.Realm == 0)
			{
				target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
			}
			else
			{
				target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
			}
			if (target is GameNPC)
			{
				IOldAggressiveBrain aggroBrain = ((GameNPC)target).Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
			}
		}
        public AllStatsPercentDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
