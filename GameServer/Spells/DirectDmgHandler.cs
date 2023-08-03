using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps;
using DOL.Events;

namespace DOL.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandlerAttribute("DirectDamage")]
	public class DirectDmgHandler : SpellHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private bool m_castFailed = false;

		/// <summary>
		/// Execute direct damage spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			if (!m_castFailed)
			{
				m_caster.Mana -= PowerCost(target);
			}

			base.FinishSpellCast(target);
		}

		private const string LOSEFFECTIVENESS = "LOS Effectivness";



		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		/// <returns></returns>
		public override double CalculateDamageBase(GameLiving target)
		{
			GamePlayer player = Caster as GamePlayer;

			// % damage procs
			if (Spell.Damage < 0)
			{
				double spellDamage = 0;

				if (player != null)
				{
					// This equation is used to simulate live values - Tolakram
					spellDamage = (target.MaxHealth * -Spell.Damage * .01) / 2.5;
				}

				if (spellDamage < 0)
					spellDamage = 0;

				return spellDamage;
			}

			return base.CalculateDamageBase(target);
		}


		public override double DamageCap(double effectiveness)
		{
			if (Spell.Damage < 0)
			{
				return (Target.MaxHealth * -Spell.Damage * .01) * 3.0 * effectiveness;
			}

			return base.DamageCap(effectiveness);
		}


		/// <summary>
		/// execute direct effect
		/// </summary>
		/// <param name="target">target that gets the damage</param>
		/// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
		public override void OnDirectEffect(GameLiving target, double effectiveness)
		{
			if (target == null)
				return;

			if (Spell.Target.ToLower() == "cone" || (Spell.Target == "Enemy" && Spell.IsPBAoE))
			{
				GamePlayer checkPlayer = null;
				if (target is GamePlayer)
				{
					checkPlayer = target as GamePlayer;
				}
				else
				{
					if (Caster is GamePlayer)
					{
						checkPlayer = Caster as GamePlayer;
					}
					else if (Caster is GameNpc && (Caster as GameNpc).Brain is IControlledBrain)
					{
						IControlledBrain brain = (Caster as GameNpc).Brain as IControlledBrain;
						checkPlayer = brain.GetPlayerOwner();
					}
				}
				if (checkPlayer != null)
				{
                    checkPlayer.TempProperties.setProperty(LOSEFFECTIVENESS + target.ObjectID, effectiveness);
					checkPlayer.Out.SendCheckLOS(Caster, target, new CheckLOSResponse(DealDamageCheckLOS));
				}
				else
				{
					DealDamage(target, effectiveness);
				}
			}
			else
			{
				DealDamage(target, effectiveness);
			}
		}

		protected virtual void DealDamageCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if (player == null || Caster.ObjectState != GameObject.eObjectState.Active)
				return;

			if ((response & 0x100) == 0x100)
			{
				try
				{
					GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;
					if (target != null)
					{
                        double effectiveness = player.TempProperties.getProperty<double>(LOSEFFECTIVENESS + target.ObjectID, 1.0);
						DealDamage(target, effectiveness);
                        player.TempProperties.removeProperty(LOSEFFECTIVENESS + target.ObjectID);
						// Due to LOS check delay the actual cast happens after FinishSpellCast does a notify, so we notify again
						GameEventMgr.Notify(GameLivingEvent.CastFinished, m_caster, new CastingEventArgs(this, target, m_lastAttackData));
					}
				}
				catch (Exception e)
				{
					m_castFailed = true;

					if (log.IsErrorEnabled)
						log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
				}
			}
			else
			{
				if (Spell.Target.ToLower() == "enemy" && Spell.Radius == 0 && Spell.Range != 0)
				{
					m_castFailed = true;
					MessageToCaster("You can't see your target!", EChatType.CT_SpellResisted);
				}
			}
		}

		protected virtual void DealDamage(GameLiving target, double effectiveness)
		{
			if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

			// calc damage
			AttackData ad = CalculateDamageToTarget(target, effectiveness);

			SendDamageMessages(ad);
			DamageTarget(ad, true);
			
			target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		}


		/*
		 * We need to send resist spell los check packets because spell resist is calculated first, and
		 * so you could be inside keep and resist the spell and be interupted when not in view
		 */
		protected override void OnSpellResisted(GameLiving target)
		{
			if (target is GamePlayer)
			{
				GamePlayer player = target as GamePlayer;
				player.Out.SendCheckLOS(Caster, player, new CheckLOSResponse(ResistSpellCheckLOS));
			}
			else
			{
				SpellResisted(target);
			}
		}

		private void ResistSpellCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if ((response & 0x100) == 0x100)
			{
				try
				{
					GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;
					if (target != null)
						SpellResisted(target);
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
				}
			}
		}

		private void SpellResisted(GameLiving target)
		{
			base.OnSpellResisted(target);
		}

		// constructor
		public DirectDmgHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}