using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Events;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
	[SpellHandler("Conversion")]
	public class ConversionSpell : SpellHandler
	{
		public const string ConvertDamage = "Conversion";

		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			effect.Owner.TempProperties.SetProperty(ConvertDamage, 100000);
			GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));

			EChatType toLiving = (Spell.Pulse == 0) ? EChatType.CT_Spell : EChatType.CT_SpellPulse;
			EChatType toOther = (Spell.Pulse == 0) ? EChatType.CT_System : EChatType.CT_Spell;
			MessageToLiving(effect.Owner, Spell.Message1, toLiving);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
			effect.Owner.TempProperties.RemoveProperty(ConvertDamage);
			return 1;
		}
		
		protected virtual void OndamageConverted(AttackData ad, int DamageAmount)
		{
		}

		private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
			AttackData ad = null;
			if (attackedByEnemy != null)
			{
				ad = attackedByEnemy.AttackData;
			}
			int reduceddmg = living.TempProperties.GetProperty<int>(ConvertDamage);
			double absorbPercent = Spell.Damage;
			int damageConverted = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));

			if (damageConverted > reduceddmg)
			{
				damageConverted = reduceddmg;
				reduceddmg -= damageConverted;
				ad.Damage -= damageConverted;
				OndamageConverted(ad, damageConverted);
			}

			if (ad.Damage > 0)
				MessageToLiving(ad.Target, string.Format("You convert {0} damage into " + damageConverted + " Health.", damageConverted), EChatType.CT_Spell);
			MessageToLiving(ad.Attacker, string.Format("A magical spell absorbs {0} damage of your attack!", damageConverted), EChatType.CT_Spell);

			if (Caster.Health != Caster.MaxHealth)
			{
				MessageToCaster("You convert " + damageConverted + " damage into health.", EChatType.CT_Spell);
				Caster.Health = Caster.Health + damageConverted;

                #region PVP DAMAGE

                if (ad.Target is NecromancerPet &&
                    ((ad.Target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null
                    || ad.Target is GamePlayer)
                {
                    if (ad.Target.DamageRvRMemory > 0)
                        ad.Target.DamageRvRMemory -= (long)Math.Max(damageConverted, 0);
                }

                #endregion PVP DAMAGE

			}
			else
			{
				MessageToCaster("You cannot convert anymore health!", EChatType.CT_Spell);
			}

			if (Caster.Endurance != Caster.MaxEndurance)
			{
				MessageToCaster("You convert " + damageConverted + " damage into endurance", EChatType.CT_Spell);
				Caster.Endurance = Caster.Endurance + damageConverted;
			}
			else
			{
				MessageToCaster("You cannot convert anymore endurance!", EChatType.CT_Spell);
			}
			if (Caster.Mana != Caster.MaxMana)
			{
				MessageToCaster("You convert " + damageConverted + " damage into power.", EChatType.CT_Spell);
				Caster.Mana = Caster.Mana + damageConverted;
			}
			else
			{
				MessageToCaster("You cannot convert anymore power!", EChatType.CT_Spell);
			}

			if (reduceddmg <= 0)
			{
				GameSpellEffect effect = SpellHandler.FindEffectOnTarget(living, this);
				if (effect != null)
					effect.Cancel(false);
			}
		}
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Name: " + Spell.Name);
				list.Add("Description: " + Spell.Description);
				list.Add("Target: " + Spell.Target);
				if (Spell.Damage != 0)
				{
					list.Add("Damage Absorb: " + Spell.Damage + "%");
					list.Add("Health Return: " + Spell.Damage + "%");
					list.Add("Power Return: " + Spell.Damage + "%");
					list.Add("Endurance Return: " + Spell.Damage + "%");
				}
				return list;
			}
		}
		public ConversionSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
	
	[SpellHandler("MagicConversion")]
	public class MagicConversionSpell : ConversionSpell
	{
		//public const string ConvertDamage = "Conversion";

		private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
		{
			GameLiving living = sender as GameLiving;
			if (living == null) return;
			AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
			AttackData ad = null;
			if (attackedByEnemy != null)
			{
				ad = attackedByEnemy.AttackData;
			}


			if (ad.Damage > 0)
			{
				switch (attackedByEnemy.AttackData.AttackType)
				{
					case EAttackType.Spell:
						{
							int reduceddmg = living.TempProperties.GetProperty<int>(ConvertDamage, 0);
							double absorbPercent = Spell.Damage;
							int damageConverted = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));
							if (damageConverted > reduceddmg)
							{
								damageConverted = reduceddmg;
								reduceddmg -= damageConverted;
								ad.Damage -= damageConverted;
								OndamageConverted(ad, damageConverted);
							}
							if (reduceddmg <= 0)
							{
								GameSpellEffect effect = SpellHandler.FindEffectOnTarget(living, this);
								if (effect != null)
									effect.Cancel(false);
							}
							MessageToLiving(ad.Target, string.Format("You convert {0} damage into " + damageConverted + " Health.", damageConverted), EChatType.CT_Spell);
							MessageToLiving(ad.Attacker, string.Format("A magical spell absorbs {0} damage of your attack!", damageConverted), EChatType.CT_Spell);
							if (Caster.Health != Caster.MaxHealth)
							{
								MessageToCaster("You convert " + damageConverted + " damage into health.", EChatType.CT_Spell);
								Caster.Health = Caster.Health + damageConverted;
							}
							else
							{
								MessageToCaster("You cannot convert anymore health!", EChatType.CT_Spell);
							}

							if (Caster.Endurance != Caster.MaxEndurance)
							{
								MessageToCaster("You convert " + damageConverted + " damage into endurance", EChatType.CT_Spell);
								Caster.Endurance = Caster.Endurance + damageConverted;
							}
							else
							{
								MessageToCaster("You cannot convert anymore endurance!", EChatType.CT_Spell);
							}
							if (Caster.Mana != Caster.MaxMana)
							{
								MessageToCaster("You convert " + damageConverted + " damage into power.", EChatType.CT_Spell);
								Caster.Mana = Caster.Mana + damageConverted;
							}
							else
							{
								MessageToCaster("You cannot convert anymore power!", EChatType.CT_Spell);
							}
						}
						break;
				}
			}
		}

		public MagicConversionSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
