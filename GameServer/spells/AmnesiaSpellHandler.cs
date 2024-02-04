using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
	[SpellHandlerAttribute("Amnesia")]
	public class AmnesiaSpellHandler : SpellHandler
	{
		public override ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new AmnesiaECSEffect(initParams);
		}
		
		/// <summary>
		/// Execute direct damage spell
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

			/// [Atlas - Takii] This is a silly change by a silly person because disallowing Amnesia while MoC'd has never been a thing in this game.
			//if (Caster.EffectList.GetOfType<MasteryofConcentrationEffect>() != null)
 			//	return;

			//have to do it here because OnAttackedByEnemy is not called to not get aggro
			//if (target.Realm == 0 || Caster.Realm == 0)
			  //target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
			//else target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
			SendEffectAnimation(target, 0, false, 1);

			if (target is GamePlayer)
			{
				((GamePlayer)target).styleComponent.NextCombatStyle = null;
				((GamePlayer)target).styleComponent.NextCombatBackupStyle = null;
			}

			//Amnesia only affects normal spells and not song activation (still affects pulses from songs though)
			if (target.CurrentSpellHandler?.Spell.InstrumentRequirement == 0)
				target.castingComponent.ClearUpSpellHandlers(); //stop even if MoC or QC

			target.rangeAttackComponent.AutoFireTarget = null;
			//if(target is GamePlayer)
				//target.TargetObject = null;

            if (target is GamePlayer)
                MessageToLiving(target, LanguageMgr.GetTranslation((target as GamePlayer).Client, "Amnesia.MessageToTarget"), eChatType.CT_Spell);

            /*
            GameSpellEffect effect;
            effect = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
            if (effect != null)
            {
                effect.Cancel(false);
                return;
            }*/
			
			//Targets next tick for pulsing speed enhancement spell will be skipped.
            // if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Pulse))
            // {
	        //     //EffectListService.TryCancelFirstEffectOfTypeOnTarget(target, eEffect.Pulse);
			// 	foreach(ECSGameEffect e in target.effectListComponent.GetAllPulseEffects())
			// 	{
					
			// 		if(e is ECSGameSpellEffect effect && effect.SpellHandler.Spell.SpellType == eSpellType.SpeedEnhancement)
			// 		{
			// 			Console.WriteLine($"effect: {effect} SpellType: {effect.SpellHandler.Spell.SpellType} PulseFreq: {effect.PulseFreq} ");
			// 			if(effect.ExpireTick > GameLoop.GameLoopTime && effect.ExpireTick < (GameLoop.GameLoopTime + 10000))
			// 			{
			// 				effect.ExpireTick += effect.PulseFreq;
			// 				if(effect.ExpireTick < (GameLoop.GameLoopTime + 10000))
			// 					effect.ExpireTick += effect.PulseFreq;

			// 			}
			// 		}
			// 	}
            // }

			// //Casters next tick for pulsing speed enhancement spell will be skipped
			// if (Caster.effectListComponent.ContainsEffectForEffectType(eEffect.Pulse))
            // {
	        //     //EffectListService.TryCancelFirstEffectOfTypeOnTarget(target, eEffect.Pulse);
			// 	foreach(ECSGameEffect e in Caster.effectListComponent.GetAllPulseEffects())
			// 	{
					
			// 		if(e is ECSGameSpellEffect effect && effect.SpellHandler.Spell.SpellType == eSpellType.SpeedEnhancement)
			// 		{
			// 			Console.WriteLine($"effect: {effect} SpellType: {effect.SpellHandler.Spell.SpellType} PulseFreq: {effect.PulseFreq} Duration: {effect.Duration} ");
						
			// 			if(effect.ExpireTick > GameLoop.GameLoopTime && effect.ExpireTick < (GameLoop.GameLoopTime + 10000))
			// 			{
			// 				effect.ExpireTick += effect.PulseFreq;
			// 				if(effect.ExpireTick < (GameLoop.GameLoopTime + 10000))
			// 					effect.ExpireTick += effect.PulseFreq;

			// 			}
			// 		}
			// 	}
            // }

			// //Cancel Mez on target if Amnesia hits.
			// if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Mez))
			// {
			// 	var effect = EffectListService.GetEffectOnTarget(target, eEffect.Mez);

			// 	if (effect != null)
			// 		EffectService.RequestImmediateCancelEffect(effect);
			// }

			if (target is GameNPC)
			{
				GameNPC npc = (GameNPC)target;
				IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
				{
					if (Util.Chance(Spell.AmnesiaChance) && npc.TargetObject != null && npc.TargetObject is GameLiving living)
					{
						aggroBrain.ClearAggroList();
						aggroBrain.AddToAggroList(living, 1);
					}
						
				}
			}
		}

		/// <summary>
		/// When spell was resisted
		/// </summary>
		/// <param name="target">the target that resisted the spell</param>
		protected override void OnSpellResisted(GameLiving target)
		{
			base.OnSpellResisted(target);
			if (Spell.CastTime == 0)
			{
				// start interrupt even for resisted instant amnesia
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
			}
		}

		// constructor
		public AmnesiaSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
