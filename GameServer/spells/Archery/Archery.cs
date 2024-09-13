using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.Archery)]
	public class Archery : ArrowSpellHandler
	{
		public enum eShotType
		{
			Other = 0,
			Critical = 1,
			Power = 2,
			PointBlank = 3,
			Rapid = 4
		}

		/// <summary>
		/// Does this spell break stealth on start?
		/// </summary>
		public override bool UnstealthCasterOnStart
		{
			get { return false; }
		}

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (!base.CheckBeginCast(selectedTarget))
				return false;

			// Is Shield Disarm ?
			ShieldTripDisarmEffect shieldDisarm = Caster.EffectList.GetOfType<ShieldTripDisarmEffect>();
			if (shieldDisarm != null)
			{
				MessageToCaster("You're disarmed and can't cast a spell", eChatType.CT_System);
				return false;
			}

			if (Caster is GamePlayer && Caster.ActiveWeapon != null && GlobalConstants.IsBowWeapon((eObjectType) Caster.ActiveWeapon.Object_Type))
			{
				if (Spell.LifeDrainReturn == (int) eShotType.Critical && !Caster.IsStealthed)
				{
					MessageToCaster("You must be stealthed and wielding a bow to use this ability!", eChatType.CT_SpellResisted);
					return false;
				}

				return true;
			}
			else
			{
				if (Spell.LifeDrainReturn == (int) eShotType.Critical)
				{
					MessageToCaster("You must be stealthed and wielding a bow to use this ability!", eChatType.CT_SpellResisted);
					return false;
				}

				MessageToCaster("You must be wielding a bow to use this ability!", eChatType.CT_SpellResisted);
				return false;
			}
		}
		
		public override void SendSpellMessages()
		{
			MessageToCaster("You prepare a " + Spell.Name, eChatType.CT_YouHit);
		}

		public override double CalculateToHitChance(GameLiving target)
		{
			// miss rate is 0 on same level opponent
			double hitChance = 100 + Caster.GetModified(eProperty.ToHitBonus);

			if (Caster is not GamePlayer || target is not GamePlayer)
			{
				// 1.33 per level difference.
				hitChance += (Caster.EffectiveLevel - target.EffectiveLevel) * (1 + 1 / 3.0);
				hitChance += Math.Max(0, target.attackComponent.Attackers.Count - 1) * ServerProperties.Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
			}

			return hitChance;
		}

		/// <summary>
		/// Adjust damage based on chance to hit.
		/// </summary>
		public override double AdjustDamageForHitChance(double damage, double hitChance)
		{
			if (hitChance < 85)
				damage *= (hitChance - 85) * 0.038;

			return damage;
		}

		public override AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = base.CalculateDamageToTarget(target);
			GamePlayer player;
			//GameSpellEffect bladeturn = FindEffectOnTarget(target, "Bladeturn");
            target.effectListComponent.Effects.TryGetValue(eEffect.Bladeturn, out var bladeturn);
			if (bladeturn != null)
			{
				switch (Spell.LifeDrainReturn)
				{
					case (int)eShotType.Critical:
						{
							if (target is GamePlayer)
							{
								player = target as GamePlayer;
								player.Out.SendMessage("A shot penetrated your magic barrier!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
							}
							ad.AttackResult = eAttackResult.HitUnstyled;
						}
						break;

					case (int)eShotType.Power:
						{
							player = target as GamePlayer;
							player.Out.SendMessage("A shot penetrated your magic barrier!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
							ad.AttackResult = eAttackResult.HitUnstyled;
                            EffectService.RequestImmediateCancelEffect(bladeturn.FirstOrDefault());
                        }
                        break;

					case (int)eShotType.Other:
					default:
						{
							if (Caster is GamePlayer)
							{
								player = Caster as GamePlayer;
								player.Out.SendMessage("Your strike was absorbed by a magical barrier!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
							}
							if (target is GamePlayer)
							{
								player = target as GamePlayer;
								player.Out.SendMessage("The blow was absorbed by a magical barrier!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
								ad.AttackResult = eAttackResult.Missed;
								EffectService.RequestImmediateCancelEffect(bladeturn.FirstOrDefault());
							}
						}
						break;
				}
			}

			if (ad.AttackResult != eAttackResult.Missed)
			{
				GameNPC npc = target as GameNPC;
				if (npc != null)
				{
					if (npc.Brain != null && (npc.Brain is IControlledBrain) == false)
					{
						// boost for npc damage until we find exactly where calculation is going wrong -tolakram
						ad.Damage = (int)(ad.Damage * 1.57);
					}
				}

				// Volley damage reduction based on live testing - tolakram
				if (Spell.Target == eSpellTarget.AREA)
				{
					ad.Damage = (int)(ad.Damage * 0.815);
				}
			}

			return ad;
		}

		/// <summary>
		/// Determines what damage type to use.  For archery the player can choose.
		/// </summary>
		/// <returns></returns>
		public override eDamageType DetermineSpellDamageType()
		{
			GameSpellEffect ef = FindEffectOnTarget(Caster, "ArrowDamageTypes");
			if (ef != null)
			{
				return ef.SpellHandler.Spell.DamageType;
			}
			else
			{
				return eDamageType.Slash;
			}
		}

		public override void FinishSpellCast(GameLiving target)
		{
			if (target == null && Spell.Target != eSpellTarget.AREA)
				return;

			if (Caster == null)
				return;

			Caster.Stealth(false);

			if (Spell.Target == eSpellTarget.AREA)
			{
				// always put archer into combat when using area (volley)
				Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
				Caster.LastAttackTickPvP = GameLoop.GameLoopTime;

				foreach (GameLiving npc in WorldMgr.GetNPCsCloseToSpot(Caster.CurrentRegionID, Caster.GroundTarget.X, Caster.GroundTarget.Y, Caster.GroundTarget.Z, (ushort)Spell.Radius))
				{
					if (npc.Realm == 0 || Caster.Realm == 0)
					{
						npc.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
					}
				}
			}
			else
			{
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
			}

			base.FinishSpellCast(target);
		}

		public override int CalculateCastingTime()
		{
			return (eShotType) Spell.LifeDrainReturn is eShotType.Power ? 6000 : base.CalculateCastingTime();
		}

		public override int PowerCost(GameLiving target) { return 0; }

		public override int CalculateEnduranceCost()
		{
			#region [Freya] Nidel: Arcane Syphon chance
			int syphon = Caster.GetModified(eProperty.ArcaneSyphon);
			if (syphon > 0)
			{
				if(Util.Chance(syphon))
				{
					return 0;
				}
			}
			#endregion
			return (int)(Caster.MaxEndurance * (Spell.Power * .01));
		}
		
		public override bool CasterIsAttacked(GameLiving attacker)
		{
			if (Spell.Uninterruptible)
				return false;

			if (IsInCastingPhase)
			{
				double chance = 65;
				chance = Math.Max(1, chance);
				chance = Math.Min(99, chance);
				if (attacker is GamePlayer) chance = 100;
				if (Util.Chance((int)chance))
				{
					Caster.TempProperties.SetProperty(INTERRUPT_TIMEOUT_PROPERTY, GameLoop.GameLoopTime + Caster.SpellInterruptDuration);
					MessageToLiving(Caster, attacker.GetName(0, true) + " attacks you and your shot is interrupted!", eChatType.CT_SpellResisted);
					InterruptCasting();
					return true;
				}
			}
			return true;
		}
		
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				//list.Add("Function: " + (Spell.SpellType == string.Empty ? "(not implemented)" : Spell.SpellType));
				//list.Add(" "); //empty line
				list.Add(Spell.Description);
				list.Add(" "); //empty line
				if (Spell.InstrumentRequirement != 0)
					list.Add("Instrument require: " + GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement));
				if (Spell.Damage != 0)
					list.Add("Damage: " + Spell.Damage.ToString("0.###;0.###'%'"));
				else if (Spell.Value != 0)
					list.Add("Value: " + Spell.Value.ToString("0.###;0.###'%'"));
				list.Add("Target: " + Spell.Target);
				if (Spell.Range != 0)
					list.Add("Range: " + Spell.Range);
				if (Spell.Duration >= ushort.MaxValue * 1000)
					list.Add("Duration: Permanent.");
				else if (Spell.Duration > 60000)
					list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
				else if (Spell.Duration != 0)
					list.Add("Duration: " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
				if (Spell.Frequency != 0)
					list.Add("Frequency: " + (Spell.Frequency * 0.001).ToString("0.0"));
				if (Spell.Power != 0)
					list.Add("Endurance cost: " + Spell.Power.ToString("0;0'%'"));
				list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
				if (Spell.RecastDelay > 60000)
					list.Add("Recast time: " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
				else if (Spell.RecastDelay > 0)
					list.Add("Recast time: " + (Spell.RecastDelay / 1000).ToString() + " sec");
				if (Spell.Radius != 0)
					list.Add("Radius: " + Spell.Radius);
				if (Spell.DamageType != eDamageType.Natural)
					list.Add("Damage: " + GlobalConstants.DamageTypeToName(Spell.DamageType));
				return list;
			}
		}

		/// <summary>
		/// Do not trigger Subspells
		/// </summary>
		/// <param name="target"></param>
		public override void CastSubSpells(GameLiving target)
		{
		}
		
		public Archery(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
