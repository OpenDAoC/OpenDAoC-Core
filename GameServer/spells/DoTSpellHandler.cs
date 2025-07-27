using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.GameObject;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Damage Over Time spell handler
	/// </summary>
	[SpellHandler(eSpellType.DamageOverTime)]
	public class DoTSpellHandler : SpellHandler
	{
		public int CriticalDamage { get; protected set; } = 0;
		private bool _firstTick = true;

		public DoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
		{
			return new DamageOverTimeECSGameEffect(initParams);
		}

		/// <summary>
		/// Execute damage over time spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
		{
			return 0;
		}

		protected override double CalculateDistanceFallOff(int distance, int radius)
		{
			return 0;
		}

		public override bool HasConflictingEffectWith(ISpellHandler compare)
		{
			return Spell.SpellType == compare.Spell.SpellType && Spell.DamageType == compare.Spell.DamageType && SpellLine.IsBaseLine == compare.SpellLine.IsBaseLine;
		}

        public override AttackData CalculateDamageToTarget(GameLiving target)
        {
            AttackData ad = base.CalculateDamageToTarget(target);

            if (SpellLine.KeyName is GlobalSpellsLines.Mundane_Poisons && Caster.effectListComponent.ContainsEffectForEffectType(eEffect.Viper))
                ad.Damage *= 2;

            /* GameSpellEffect iWarLordEffect = FindEffectOnTarget(target, "CleansingAura");
            if (iWarLordEffect != null)
                ad.Damage *= (int) (1.00 - iWarLordEffect.Spell.Value * 0.01);*/

            return ad;
        }

        /// <summary>
        /// Sends damage text messages but makes no damage
        /// </summary>
        public override void SendDamageMessages(AttackData ad)
        {
            GamePlayer player = null;

            if (m_caster is GamePlayer)
                player = m_caster as GamePlayer;
            else if (m_caster is GameNPC npc && npc.Brain is IControlledBrain brain)
                player = brain.GetPlayerOwner();

            if (player == null)
                return;

            if (SpellLine.KeyName is GlobalSpellsLines.Item_Effects)
                MessageToCaster(string.Format(LanguageMgr.GetTranslation(player.Client, "DoTSpellHandler.SendDamageMessages.YouHitFor", ad.Target.GetName(0, false), ad.Damage)), eChatType.CT_Spell);
            else
                MessageToCaster(string.Format(LanguageMgr.GetTranslation(player.Client, "DoTSpellHandler.SendDamageMessages.YourHitsFor", Spell.Name, ad.Target.GetName(0, false), ad.Damage)), eChatType.CT_Spell);

            if (CriticalDamage > 0)
                MessageToCaster($"You critically hit for an additional {CriticalDamage} damage! ({m_caster.DebuffCriticalChance}%)", eChatType.CT_Spell);
        }

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			//((compare is DoTSpellHandler dot) && Spell.Damage + this.CriticalDamage < compare.Spell.Damage + dot.CriticalDamage)
			// var dots = target.effectListComponent.GetSpellEffects(eEffect.DamageOverTime)
			// 									 .Where(x => x.SpellHandler?.Spell != null)
			// 									 .Select(x => x.SpellHandler)
			// 									 .Where(x => x.Spell.SpellType == Spell.SpellType &&
			// 												 x.Spell.DamageType == Spell.DamageType &&
			// 												 x.SpellLine.IsBaseLine == SpellLine.IsBaseLine);

			// foreach (var dotEffect in dots)
            // {
			// 	var dotHandler = (dotEffect as DoTSpellHandler);

			// 	if (dotHandler == null)
			// 		continue;

			// 	// Check for Overwriting.
			// 	if (dotEffect.Spell.Damage + dotHandler.CriticalDamage >= Spell.Damage + CriticalDamage)
			// 	{
			// 		// Old Spell is Better than new one

			// 		//apply first hit, then quit
			// 		OnDirectEffect(target, effectiveness);
					
			// 		this.MessageToCaster(eChatType.CT_SpellResisted, "{0} already has that effect.", target.GetName(0, true));
			// 		MessageToCaster("Wait until it expires. Spell Failed.", eChatType.CT_SpellResisted);
			// 		// Prevent Adding.
			// 		return;
			// 	}
			// }

			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			// damage is not reduced with distance
            return new GameSpellEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			SendEffectAnimation(effect.Owner, 0, false, 1);
		}

		public override void OnEffectPulse(GameSpellEffect effect)
		{
			base.OnEffectPulse(effect);

			if (effect.Owner.IsAlive)
			{
				// An acidic cloud surrounds you!
				MessageToLiving(effect.Owner, Spell.Message1, eChatType.CT_Spell);
				// {0} is surrounded by an acidic cloud!
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
				OnDirectEffect(effect.Owner);
			}
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			base.OnEffectExpires(effect, noMessages);
			if (!noMessages)
			{
				// The acidic mist around you dissipates.
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				// The acidic mist around {0} dissipates.
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}
			return 0;
		}

		public override void OnDirectEffect(GameLiving target)
		{
			if (target == null || !target.IsAlive || target.ObjectState is not eObjectState.Active)
				return;

			AttackData ad = CalculateDamageToTarget(target);
			ad.CriticalDamage = CalculateCriticalDamage(ad);
			ad.CausesCombat = _firstTick;
			SendDamageMessages(ad);
			DamageTarget(ad, false);

			if (_firstTick)
				_firstTick = false;
		}

		private int CalculateCriticalDamage(AttackData ad)
		{
			if (CriticalDamage > 0 || !_firstTick)
				return CriticalDamage;

			ad.CriticalChance = Caster.DebuffCriticalChance;

			if (ad.CriticalChance < 0)
				return 0;

			double randNum = Util.RandomDouble() * 100;
			int critCap = Math.Min(50, ad.CriticalChance);

			if (Caster is GamePlayer spellCaster && spellCaster.UseDetailedCombatLog && critCap > 0)
				spellCaster.Out.SendMessage($"dot crit chance: {critCap:0.##} random: {randNum:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

			if (critCap > randNum && (ad.Damage >= 1))
			{
				int critmax = (ad.Target is GamePlayer) ? ad.Damage / 2 : ad.Damage;
				CriticalDamage = Util.Random(ad.Damage / 10, critmax); //tThink min crit is 10% of damage
			}

			return CriticalDamage;
		}

		protected override double CalculateBuffDebuffEffectiveness()
		{
			return 1.0; // Unused by DoTs.
		}
	}
}
