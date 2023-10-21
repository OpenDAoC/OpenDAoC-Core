using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Effects.Old;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.PacketHandler;
using Core.GS.PlayerClass;
using Core.GS.ServerProperties;
using Core.GS.SkillHandler;
using Core.Language;

namespace Core.GS.Spells
{
	public class SpellHandler : ISpellHandler
	{
		// Maximum number of sub-spells to get delve info for.
		protected const byte MAX_DELVE_RECURSION = 5;

		// Maximum number of Concentration spells that a single caster is allowed to cast.
		private const int MAX_CONC_SPELLS = 20;

		// Array of pulse spell groups allowed to exist with others.
		// Used to allow players to have more than one pulse spell refreshing itself automatically.
		private static readonly int[] PulseSpellGroupsIgnoringOtherPulseSpells = Array.Empty<int>();

		public ECastState CastState { get; set; }
		public GameLiving Target { get; set; }
		public bool HasLos { get; set; }

		/// <summary>
		/// The spell that we want to handle
		/// </summary>
		protected Spell m_spell;
		/// <summary>
		/// The spell line the spell belongs to
		/// </summary>
		protected SpellLine m_spellLine;
		/// <summary>
		/// The caster of the spell
		/// </summary>
		protected GameLiving m_caster;
		public double Effectiveness { get; protected set; } = 1;
		protected double _distanceFallOff;
		/// <summary>
		/// Has the spell been interrupted
		/// </summary>
		protected bool m_interrupted = false;
		/// <summary>
		/// Delayedcast Stage
		/// </summary>
		public int Stage
		{
			get { return m_stage; }
			set { m_stage = value; }
		}
		protected int m_stage = 0;

		/// <summary>
		/// Shall we start the reuse timer
		/// </summary>
		protected bool m_startReuseTimer = true;

		private long _castStartTick;
		public long CastStartTick { get { return _castStartTick; } }
		public bool StartReuseTimer
		{
			get { return m_startReuseTimer; }
		}

		/// <summary>
		/// Can this spell be queued with other spells?
		/// </summary>
		public virtual bool CanQueue
		{
			get { return true; }
		}

		/// <summary>
		/// Does this spell break stealth on start of cast?
		/// </summary>
		public virtual bool UnstealthCasterOnStart
		{
			get { return true; }
		}
		
		/// <summary>
		/// Does this spell break stealth on Finish of cast?
		/// </summary>
		public virtual bool UnstealthCasterOnFinish
		{
			get { return true; }
		}
		
		protected DbInventoryItem m_spellItem = null;

		/// <summary>
		/// Ability that casts a spell
		/// </summary>
		protected ISpellCastingAbilityHandler m_ability = null;

		/// <summary>
		/// Stores the current delve info depth
		/// </summary>
		private byte m_delveInfoDepth;

		/// <summary>
		/// AttackData result for this spell, if any
		/// </summary>
		protected AttackData m_lastAttackData = null;

		/// <summary>
		/// The property key for the interrupt timeout
		/// </summary>
		public const string INTERRUPT_TIMEOUT_PROPERTY = "CAST_INTERRUPT_TIMEOUT";

		protected bool m_ignoreDamageCap = false;

		private long _calculatedCastTime = 0;

		private long _lastDuringCastLosCheckTime;

		/// <summary>
		/// Does this spell ignore any damage cap?
		/// </summary>
		public bool IgnoreDamageCap
		{
			get { return m_ignoreDamageCap; }
			set { m_ignoreDamageCap = value; }
		}

		protected bool m_useMinVariance = false;

		/// <summary>
		/// Should this spell use the minimum variance for the type?
		/// Followup style effects, for example, always use the minimum variance
		/// </summary>
		public bool UseMinVariance
		{
			get { return m_useMinVariance; }
			set { m_useMinVariance = value; }
		}

		/// <summary>
		/// Can this SpellHandler Coexist with other Overwritable Spell Effect
		/// </summary>
		public virtual bool AllowCoexisting
		{
			get { return Spell.AllowCoexisting; }
		}

		public virtual bool IsSummoningSpell
		{
			get
			{
				if (m_spell.SpellType != ESpellType.Null)
					switch (m_spell.SpellType)
					{
						case ESpellType.Bomber:
						case ESpellType.Charm:
						case ESpellType.Pet:
						case ESpellType.SummonCommander:
						case ESpellType.SummonTheurgistPet:
						case ESpellType.Summon:
						case ESpellType.SummonJuggernaut:
						//case eSpellType.SummonMerchant:
						case ESpellType.SummonMinion:
						case ESpellType.SummonSimulacrum:
						case ESpellType.SummonUnderhill:
						//case eSpellType.SummonVaultkeeper:
						case ESpellType.SummonAnimistAmbusher:
						case ESpellType.SummonAnimistPet:
						case ESpellType.SummonDruidPet:
						case ESpellType.SummonHealingElemental:
						case ESpellType.SummonHunterPet:
						case ESpellType.SummonAnimistFnF:
						case ESpellType.SummonAnimistFnFCustom:
						case ESpellType.SummonSiegeBallista:
						case ESpellType.SummonSiegeCatapult:
						case ESpellType.SummonSiegeRam:
						case ESpellType.SummonSiegeTrebuchet:
						case ESpellType.SummonSpiritFighter:
						case ESpellType.SummonNecroPet:
						//case eSpellType.SummonNoveltyPet:
							return true;
						default:
							return false;
					}
				
				return false;
			}
		}

		/// <summary>
		/// spell handler constructor
		/// <param name="caster">living that is casting that spell</param>
		/// <param name="spell">the spell to cast</param>
		/// <param name="spellLine">the spell line that spell belongs to</param>
		/// </summary>
		public SpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
		{
			m_caster = caster;
			m_spell = spell;
			m_spellLine = spellLine;
		}

		/// <summary>
		/// Returns the string representation of the SpellHandler
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return new StringBuilder(128)
				.Append("Caster=").Append(Caster == null ? "(null)" : Caster.Name)
				.Append(", IsCasting=").Append(IsInCastingPhase)
				.Append(", m_interrupted=").Append(m_interrupted)
				.Append("\nSpell: ").Append(Spell == null ? "(null)" : Spell.ToString())
				.Append("\nSpellLine: ").Append(SpellLine == null ? "(null)" : SpellLine.ToString())
				.ToString();
		}

		#region Pulsing Spells

		/// <summary>
		/// When spell pulses
		/// </summary>
		public virtual void OnSpellPulse(PulsingSpellEffect effect)
		{
			if (Caster.IsMoving && Spell.IsFocus)
			{
				MessageToCaster("Your spell was cancelled.", EChatType.CT_SpellExpires);
				effect.Cancel(false);
				return;
			}

			if (Caster.IsAlive == false)
			{
				effect.Cancel(false);
				return;
			}

			if (Caster.ObjectState != GameObject.eObjectState.Active)
				return;

			if (Caster.IsStunned || Caster.IsMezzed)
				return;

			if (m_spell.InstrumentRequirement != 0 && !CheckInstrument())
			{
				MessageToCaster("You stop playing your song.", EChatType.CT_Spell);
				effect.Cancel(false);
				return;
			}

			if (Caster.Mana >= Spell.PulsePower)
			{
				Caster.Mana -= Spell.PulsePower;

				if (Spell.InstrumentRequirement != 0 || !HasPositiveEffect)
					SendEffectAnimation(Caster, 0, true, 1); // Pulsing auras or songs.

				StartSpell(Target);
			}
			else
			{
				MessageToCaster("You do not have enough power and your spell was canceled.", EChatType.CT_SpellExpires);
				effect.Cancel(false);
			}
		}

		/// <summary>
		/// Checks if caster holds the right instrument for this 
		/// </summary>
		/// <returns>true if right instrument</returns>
		protected bool CheckInstrument()
		{
			DbInventoryItem instrument = Caster.ActiveWeapon;

			// From patch 1.97:  Flutes, Lutes, and Drums will now be able to play any song type, and will no longer be limited to specific songs.
			if (instrument == null || instrument.Object_Type != (int)EObjectType.Instrument ) // || (instrument.DPS_AF != 4 && instrument.DPS_AF != m_spell.InstrumentRequirement))
				return false;

			return true;
		}

		/// <summary>
		/// Cancels first pulsing spell of type
		/// </summary>
		/// <param name="living">owner of pulsing spell</param>
		/// <param name="spellType">type of spell to cancel</param>
		/// <returns>true if any spells were canceled</returns>
		public virtual bool CancelPulsingSpell(GameLiving living, ESpellType spellType)
		{
			//lock (living.ConcentrationEffects)
			//{
			//	for (int i = 0; i < living.ConcentrationEffects.Count; i++)
			//	{
			//		PulsingSpellEffect effect = living.ConcentrationEffects[i] as PulsingSpellEffect;
			//		if (effect == null)
			//			continue;
			//		if (effect.SpellHandler.Spell.SpellType == spellType)
			//		{
			//			effect.Cancel(false);
			//			return true;
			//		}
			//	}
			//}

			lock (living.effectListComponent.EffectsLock)
			{
				var effects = living.effectListComponent.GetAllPulseEffects();

				for (int i = 0; i < effects.Count; i++)
				{
					EcsPulseEffect effect = effects[i];
					if (effect == null)
						continue;

					if (effect == null)
						continue;
					if (effect.SpellHandler.Spell.SpellType == spellType)
					{
						EffectService.RequestCancelConcEffect(effect);
						return true;
					}
				}
			}
			return false;
		}

		#endregion

		public virtual void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			// Base function should be empty once all effects are moved to their own effect class.
			new EcsGameSpellEffect(initParams);
		}

		public virtual void CreateECSPulseEffect(GameLiving target, double effectiveness)
		{
			int freq = Spell != null ? Spell.Frequency : 0;

			new EcsPulseEffect(target, this, CalculateEffectDuration(target, effectiveness), freq, effectiveness, Spell.Icon);
		}

		/// <summary>
		/// Is called when the caster moves
		/// </summary>
		public virtual void CasterMoves()
		{
			if (Spell.InstrumentRequirement != 0)
				return;

			if (Spell.MoveCast)
				return;

			if (Caster is GamePlayer)
			{
				if (CastState != ECastState.Focusing)
					(Caster as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "SpellHandler.CasterMove"), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				else
					Caster.CancelFocusSpell(true);
			}

			InterruptCasting();
		}

		/// <summary>
		/// This sends the spell messages to the player/target.
		///</summary>
		public virtual void SendSpellMessages()
		{
			if (Spell.SpellType != ESpellType.PveResurrectionIllness && Spell.SpellType != ESpellType.RvrResurrectionIllness)
			{
				if (Spell.InstrumentRequirement == 0)
				{
					if (Caster is GamePlayer playerCaster)
					{
						// Message: You begin casting a {0} spell!
						MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CastSpell.Msg.YouBeginCasting", Spell.Name), EChatType.CT_Spell);
					}
					if (Caster is NecromancerPet {Owner: GamePlayer casterOwner})
					{
						// Message: {0} begins casting a {1} spell!
						casterOwner.Out.SendMessage(LanguageMgr.GetTranslation(casterOwner.Client.Account.Language, "SpellHandler.CastSpell.Msg.PetBeginsCasting", Caster.GetName(0, true), Spell.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
				}
				else if (Caster is GamePlayer songCaster)
				{
					// Message: You begin playing {0}!
					MessageToCaster(LanguageMgr.GetTranslation(songCaster.Client, "SpellHandler.CastSong.Msg.YouBeginPlaying", Spell.Name), EChatType.CT_Spell);
				}
			}
		}

		public virtual bool CasterIsAttacked(GameLiving attacker)
		{
			// [StephenxPimentel] Check if the necro has MoC effect before interrupting.
			if (Caster is NecromancerPet necroPet && necroPet.Owner is GamePlayer necroOwner)
			{
				if (necroOwner.effectListComponent.ContainsEffectForEffectType(EEffect.MasteryOfConcentration))
					return false;
			}

			if (Spell.Uninterruptible)
				return false;

			if (Caster.effectListComponent.ContainsEffectForEffectType(EEffect.MasteryOfConcentration)
				|| Caster.effectListComponent.ContainsEffectForEffectType(EEffect.FacilitatePainworking)
				|| Caster.effectListComponent.ContainsEffectForEffectType(EEffect.QuickCast))
				return false;

			// Only interrupt if we're under 50% of the way through the cast.
			if (IsInCastingPhase && (GameLoop.GameLoopTime < _castStartTick + _calculatedCastTime * 0.5))
			{
				if (Caster is GameSummonedPet petCaster && petCaster.Owner is GamePlayer casterOwner)
				{
					casterOwner.LastInterruptMessage = $"Your {Caster.Name} was attacked by {attacker.Name} and their spell was interrupted!";
					MessageToLiving(casterOwner, casterOwner.LastInterruptMessage, EChatType.CT_SpellResisted);
				}
				else if (Caster is GamePlayer playerCaster)
				{
					playerCaster.LastInterruptMessage = $"{attacker.GetName(0, true)} attacks you and your spell is interrupted!";
					MessageToLiving(playerCaster, playerCaster.LastInterruptMessage, EChatType.CT_SpellResisted);
				}

				InterruptCasting(); // Always interrupt at the moment.
				return true;
			}

			return false;
		}

		#region begin & end cast check

		public virtual bool CheckBeginCast(GameLiving selectedTarget)
		{
			return CheckBeginCast(selectedTarget, false);
		}

		/// <summary>
		/// All checks before any casting begins
		/// </summary>
		public virtual bool CheckBeginCast(GameLiving selectedTarget, bool quiet)
		{
			if (m_caster.ObjectState != GameObject.eObjectState.Active)
				return false;
 
			if (!m_caster.IsAlive)
			{
				if (!quiet)
					MessageToCaster("You are dead and can't cast!", EChatType.CT_System);

				return false;
			}

			Target = selectedTarget;

			switch (Spell.Target)
			{
				case ESpellTarget.SELF:
				{
					// Self spells should ignore whatever we actually have selected.
					Target = Caster;
					break;
				}
				case ESpellTarget.PET:
				{
					// Get the current target if we don't have one already.
					if (Target == null)
						Target = Caster?.TargetObject as GameLiving;

					// Pet spells are automatically casted on the controlled NPC, but only if the current target isn't a subpet or a turret.
					if (((Target as GameNpc)?.Brain as IControlledBrain)?.GetPlayerOwner() != Caster && Caster.ControlledBrain?.Body != null)
						Target = Caster.ControlledBrain.Body;

					break;
				}
				default:
				{
					// Get the current target if we don't have one already.
					if (Target == null)
						Target = Caster?.TargetObject as GameLiving;

					if (Target == null && Caster is NecromancerPet nPet)
						Target = (nPet.Brain as NecromancerPetBrain).GetSpellTarget();

					break;
				}
			}


			// Initial LoS state.
			HasLos = Caster.TargetInView;

			if (Caster is GameNpc npcOwner)
			{
				// Reset for LoS checks during cast.
				HasLos = true;

				if (!Spell.IsInstantCast)
				{
					if (npcOwner.IsMoving)
						npcOwner.StopMoving();
				}

				if (npcOwner != Target)
					npcOwner.TurnTo(Target);
			}

			if (m_spell.IsPulsing && m_spell.Frequency > 0)
			{
				if (m_caster.ActivePulseSpells.TryRemove(m_spell.SpellType, out Spell _))
				{
					EcsPulseEffect effect = EffectListService.GetPulseEffectOnTarget(m_caster, m_spell);
					EffectService.RequestImmediateCancelConcEffect(effect);

					if (m_spell.InstrumentRequirement == 0)
						MessageToCaster("You cancel your effect.", EChatType.CT_Spell);
					else
						MessageToCaster("You stop playing your song.", EChatType.CT_Spell);

					return false;
				}
			}

			m_caster.CancelFocusSpell();

			var quickCast = EffectListService.GetAbilityEffectOnTarget(m_caster, EEffect.QuickCast);

			if (quickCast != null)
				quickCast.ExpireTick = GameLoop.GameLoopTime + quickCast.Duration;

			if (m_caster is GamePlayer playerCaster)
			{
				long nextSpellAvailTime = m_caster.TempProperties.GetProperty<long>(GamePlayer.NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION);

				if (nextSpellAvailTime > m_caster.CurrentRegion.Time && Spell.CastTime > 0) // instant spells ignore the potion cast delay
				{
					playerCaster.Out.SendMessage(LanguageMgr.GetTranslation(playerCaster.Client, "GamePlayer.CastSpell.MustWaitBeforeCast", (nextSpellAvailTime - m_caster.CurrentRegion.Time) / 1000), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return false;
				}

				if (playerCaster.Steed is GameSiegeRam)
				{
					if (!quiet)
						MessageToCaster("You can't cast in a siege ram!", EChatType.CT_System);

					return false;
				}
			}

			/*
			GameSpellEffect Phaseshift = FindEffectOnTarget(Caster, "Phaseshift");
			if (Phaseshift != null && (Spell.InstrumentRequirement == 0 || Spell.SpellType == eSpellType.Mesmerize))
			{
				if (!quiet) MessageToCaster("You're phaseshifted and can't cast a spell", eChatType.CT_System);
				return false;
			}*/

			// Apply Mentalist RA5L.
			if (Spell.Range > 0)
			{
				NfRaSelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<NfRaSelectiveBlindnessEffect>();
				if (SelectiveBlindness != null)
				{
					GameLiving EffectOwner = SelectiveBlindness.EffectSource;
					if(EffectOwner==Target)
					{
						if (m_caster is GamePlayer && !quiet)
							((GamePlayer)m_caster).Out.SendMessage(string.Format("{0} is invisible to you!", Target.GetName(0, true)), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);

						return false;
					}
				}
			}

			if (Target !=null && Target.HasAbility("DamageImmunity") && Spell.SpellType == ESpellType.DirectDamage && Spell.Radius == 0)
			{
				if (!quiet)
					MessageToCaster(Target.Name + " is immune to this effect!", EChatType.CT_SpellResisted);

				return false;
			}

			if (m_spell.InstrumentRequirement != 0)
			{
				if (!CheckInstrument())
				{
					if (!quiet)
						MessageToCaster("You are not wielding the right type of instrument!", EChatType.CT_SpellResisted);

					return false;
				}
			}
			// Songs can be played even if sitting.
			else if (m_caster.IsSitting)
			{
				// Purge can be cast while sitting but only if player has negative effect that doesn't allow standing up (like stun or mez).
				if (!quiet)
					MessageToCaster("You can't cast while sitting!", EChatType.CT_SpellResisted);

				return false;
			}

			// Stop our melee attack. NPC brains will resume it automatically.
			if (!Spell.IsInstantCast && m_caster.attackComponent.AttackState && !m_caster.CanCastWhileAttacking())
				m_caster.attackComponent.StopAttack();

			// Check interrupt timer.
			if (!m_spell.Uninterruptible && !m_spell.IsInstantCast && Caster.InterruptAction > 0 && Caster.IsBeingInterrupted)
			{
				if (m_caster is GamePlayer)
				{
					if (!m_caster.effectListComponent.ContainsEffectForEffectType(EEffect.QuickCast) &&
						!m_caster.effectListComponent.ContainsEffectForEffectType(EEffect.MasteryOfConcentration))
					{
						if (!quiet)
							MessageToCaster($"You must wait {(Caster.InterruptTime - GameLoop.GameLoopTime) / 1000 + 1} seconds to cast a spell!", EChatType.CT_SpellResisted);

						return false;
					}
				}
				else if (m_caster is NecromancerPet necroPet && necroPet.Brain is NecromancerPetBrain)
				{
					if (!necroPet.effectListComponent.ContainsEffectForEffectType(EEffect.FacilitatePainworking))
					{
						if (!quiet)
							MessageToCaster($"Your {necroPet.Name} must wait {(Caster.InterruptTime - GameLoop.GameLoopTime) / 1000 + 1} seconds to cast a spell!", EChatType.CT_SpellResisted);

						return false;
					}
				}
				else
					return false;
			}

			if (m_spell.RecastDelay > 0)
			{
				int left = m_caster.GetSkillDisabledDuration(m_spell);

				if (left > 0)
				{
					if (m_caster is NecromancerPet && ((m_caster as NecromancerPet).Owner as GamePlayer).Client.Account.PrivLevel > (int)EPrivLevel.Player)
					{
						// Ignore Recast Timer
					}
					else
					{
						if (!quiet)
							MessageToCaster("You must wait " + (left / 1000 + 1).ToString() + " seconds to use this spell!", EChatType.CT_System);
						return false;
					}
				}
			}

			switch (Spell.Target)
			{
				case ESpellTarget.PET:
				{
					if (Target == null || ((Target as GameNpc)?.Brain as IControlledBrain)?.GetPlayerOwner() != Caster)
					{
						if (!quiet)
							MessageToCaster("You must cast this spell on a creature you are controlling.", EChatType.CT_System);

						return false;
					}

					break;
				}
				case ESpellTarget.AREA:
				{
					if (!m_caster.IsWithinRadius(m_caster.GroundTarget, CalculateSpellRange()))
					{
						if (!quiet)
							MessageToCaster("Your area target is out of range. Select a closer target.", EChatType.CT_SpellResisted);

						return false;
					}

					break;
				}
				case ESpellTarget.REALM:
				case ESpellTarget.ENEMY:
				case ESpellTarget.CORPSE:
				{
					if (m_spell.Range <= 0)
						break;

					// All spells that need a target.
					if (Target == null || Target.ObjectState != GameObject.eObjectState.Active)
					{
						if (!quiet)
							MessageToCaster("You must select a target for this spell!", EChatType.CT_SpellResisted);

						return false;
					}

					if (!m_caster.IsWithinRadius(Target, CalculateSpellRange()))
					{
						if (Caster is GamePlayer && !quiet)
							MessageToCaster("That target is too far away!", EChatType.CT_SpellResisted);

						Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, ECastFailedReasons.TargetTooFarAway));

						if (Caster is GameNpc npc)
							npc.Follow(Target, Spell.Range - 100, GameNpc.STICK_MAXIMUM_RANGE);

						return false;
					}

					switch (m_spell.Target)
					{
						case ESpellTarget.ENEMY:
						{
							if (Target == m_caster)
							{
								if (!quiet)
									MessageToCaster("You can't attack yourself! ", EChatType.CT_System);

								return false;
							}

							if (FindStaticEffectOnTarget(Target, typeof(NecromancerShadeEffect)) != null)
							{
								if (!quiet)
									MessageToCaster("Invalid target.", EChatType.CT_System);

								return false;
							}

							if (m_spell.SpellType == ESpellType.Charm && m_spell.CastTime == 0 && m_spell.Pulse != 0)
								break;

							if (Caster is TurretPet)
								return true;

							// Pet spells (shade) don't require the target to be in front.
							if (!HasLos || m_spell.SpellType != ESpellType.PetSpell && !m_caster.IsObjectInFront(Target, 180))
							{
								if (!quiet)
									MessageToCaster("Your target is not visible!", EChatType.CT_SpellResisted);

								Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, ECastFailedReasons.TargetNotInView));
								return false;
							}

							if (!GameServer.ServerRules.IsAllowedToAttack(Caster, Target, quiet))
								return false;

							break;
						}
						case ESpellTarget.CORPSE:
						{
							if (Target.IsAlive || !GameServer.ServerRules.IsSameRealm(Caster, Target, true))
							{
								if (!quiet)
									MessageToCaster("This spell only works on dead members of your realm!", EChatType.CT_SpellResisted);

								return false;
							}

							break;
						}
						case ESpellTarget.REALM:
						{
							if (!GameServer.ServerRules.IsSameRealm(Caster, Target, true))
								return false;

							break;
						}
					}

					if (!HasLos)
					{
						if (!quiet)
							MessageToCaster("Your target is not visible!", EChatType.CT_SpellResisted);

						Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, ECastFailedReasons.TargetNotInView));
						return false;
					}

					if (m_spell.Target != ESpellTarget.CORPSE && !Target.IsAlive)
					{
						if (!quiet)
							MessageToCaster(Target.GetName(0, true) + " is dead!", EChatType.CT_SpellResisted);

						return false;
					}

					break;
				}
			}

			//Ryan: don't want mobs to have reductions in mana
			if (Spell.Power != 0 && m_caster is GamePlayer && (m_caster as GamePlayer).PlayerClass.ID != (int)EPlayerClass.Savage && m_caster.Mana < PowerCost(Target) && EffectListService.GetAbilityEffectOnTarget(Caster, EEffect.QuickCast) == null && Spell.SpellType != ESpellType.Archery)
			{
				if (!quiet)
					MessageToCaster("You don't have enough power to cast that!", EChatType.CT_SpellResisted);

				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0)
			{
				if (m_caster.Concentration < m_spell.Concentration)
				{
					if (!quiet)
						MessageToCaster("This spell requires " + m_spell.Concentration + " concentration points to cast!", EChatType.CT_SpellResisted);
					return false;
				}

				var maxConc = MAX_CONC_SPELLS;

				//self buff charge IDs should not count against conc cap
				if (m_caster is GamePlayer p)
				{
					maxConc += p.effectListComponent.ConcentrationEffects.Count(concentrationEffect => concentrationEffect.SpellHandler?.Spell?.ID != null 
																				&& p.SelfBuffChargeIDs.Contains(concentrationEffect.SpellHandler.Spell.ID));
				}

				if (m_caster.effectListComponent.ConcentrationEffects.Count >= maxConc)
				{
					if (!quiet)
						MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", EChatType.CT_SpellResisted);
					return false;
				}
			}

			// Cancel engage if user starts attack
			if (m_caster.IsEngaging)
			{
				EngageEcsAbilityEffect engage = (EngageEcsAbilityEffect) EffectListService.GetEffectOnTarget(m_caster, EEffect.Engage);

				if (engage != null)
					engage.Cancel(false, false);
			}

			if (Caster is NecromancerPet necromancerPet && necromancerPet.Brain is NecromancerPetBrain necromancerPetBrain)
				necromancerPetBrain.OnPetBeginCast(Spell, SpellLine);

			return true;
		}

		private void CheckPlayerLosDuringCastCallback(GamePlayer player, ushort response, ushort sourceOID, ushort targetOID)
		{
			if (player == null || sourceOID == 0 || targetOID == 0)
				return;
			
			HasLos = (response & 0x100) == 0x100;

			if (!HasLos && Properties.CHECK_LOS_DURING_CAST_INTERRUPT)
			{
				if (IsInCastingPhase)
					MessageToCaster("You can't see your target from here!", EChatType.CT_SpellResisted);

				InterruptCasting();
			}
		}

		private void CheckPetLosDuringCastCallback(GameLiving living, ushort response, ushort sourceOID, ushort targetOID)
		{
			if (living == null || sourceOID == 0 || targetOID == 0)
				return;

			HasLos = (response & 0x100) == 0x100;

			if (!HasLos && Properties.CHECK_LOS_DURING_CAST_INTERRUPT)
				InterruptCasting();
		}

		/// <summary>
		/// Checks after casting before spell is executed
		/// </summary>
		public virtual bool CheckEndCast(GameLiving target)
		{
			if (IsSummoningSpell && Caster.CurrentRegion.IsCapitalCity)
			{
				// Message: You can't summon here!
				ChatUtil.SendErrorMessage(Caster as GamePlayer, "GamePlayer.CastEnd.Fail.BadRegion", null);
				return false;
			}
			
			if (Caster != target && Caster is GameNpc casterNPC && Caster is not NecromancerPet)
				casterNPC.TurnTo(target);

			if (m_caster.ObjectState != GameObject.eObjectState.Active)
				return false;

			if (!m_caster.IsAlive)
			{
				MessageToCaster("You are dead and can't cast!", EChatType.CT_System);
				return false;
			}

			if (m_spell.InstrumentRequirement != 0)
			{
				if (!CheckInstrument())
				{
					MessageToCaster("You are not wielding the right type of instrument!", EChatType.CT_SpellResisted);
					return false;
				}
			}
			else if (m_caster.IsSitting) // Songs can be played when sitting.
			{
				// Purge can be cast while sitting but only if player has negative effect that doesn't allow standing up (like stun or mez).
				MessageToCaster("You can't cast while sitting!", EChatType.CT_SpellResisted);
				return false;
			}

			if (m_spell.Target == ESpellTarget.AREA)
			{
				if (!m_caster.IsWithinRadius(m_caster.GroundTarget, CalculateSpellRange()))
				{
					MessageToCaster("Your area target is out of range. Select a closer target.", EChatType.CT_SpellResisted);
					return false;
				}
			}
			else if (m_spell.Target != ESpellTarget.SELF && m_spell.Target != ESpellTarget.GROUP && m_spell.Target != ESpellTarget.CONE && m_spell.Range > 0)
			{
				if (m_spell.Target != ESpellTarget.PET)
				{
					// All other spells that need a target.
					if (target == null || target.ObjectState != GameObject.eObjectState.Active)
					{
						if (Caster is GamePlayer)
							MessageToCaster("You must select a target for this spell!", EChatType.CT_SpellResisted);

						return false;
					}

					if (!m_caster.IsWithinRadius(target, CalculateSpellRange()))
					{
						if (Caster is GamePlayer)
							MessageToCaster("That target is too far away!", EChatType.CT_SpellResisted);

						return false;
					}
				}

				switch (m_spell.Target)
				{
					case ESpellTarget.ENEMY:
					{
						if (m_spell.SpellType == ESpellType.Charm)
							break;

						if (m_spell.SpellType != ESpellType.PetSpell)
						{
							// The target must be visible and in front of the caster
							if (target.IsStealthed || !HasLos || !Caster.IsObjectInFront(target, 180, Caster.TargetInViewAlwaysTrueMinRange))
							{
								// Avoid flute mez's chat log spam.
								if (m_spell.IsPulsing && m_spell.SpellType == ESpellType.Mesmerize)
								{
									MesmerizeSpell mesmerizeSpellHandler = this as MesmerizeSpell;

									if (GameLoop.GameLoopTime - mesmerizeSpellHandler.FluteMezLastEndOfCastMessage < MesmerizeSpell.FLUTE_MEZ_END_OF_CAST_MESSAGE_INTERVAL)
										return false;

									mesmerizeSpellHandler.FluteMezLastEndOfCastMessage = GameLoop.GameLoopTime;
								}
								
								MessageToCaster("You can't see your target from here!", EChatType.CT_SpellResisted);

								return false;
							}
						}

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, false))
							return false;

						break;
					}
					case ESpellTarget.CORPSE:
					{
						if (target.IsAlive || !GameServer.ServerRules.IsSameRealm(Caster, target, true))
						{
							MessageToCaster("This spell only works on dead members of your realm!", EChatType.CT_SpellResisted);
							return false;
						}

						break;
					}
					case ESpellTarget.PET:
					{
						if (!m_caster.IsWithinRadius(target, CalculateSpellRange()))
						{
							MessageToCaster("That target is too far away!", EChatType.CT_SpellResisted);
							return false;
						}

						break;
					}
				}
			}

			if (m_caster.Mana <= 0 && Spell.Power > 0 && Spell.SpellType != ESpellType.Archery)
			{
				MessageToCaster("You have exhausted all of your power and cannot cast spells!", EChatType.CT_SpellResisted);
				return false;
			}

			if (Spell.Power > 0 && m_caster.Mana < PowerCost(target) && EffectListService.GetAbilityEffectOnTarget(Caster, EEffect.QuickCast) == null && Spell.SpellType != ESpellType.Archery)
			{
				MessageToCaster("You don't have enough power to cast that!", EChatType.CT_SpellResisted);
				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0 && m_caster.Concentration < m_spell.Concentration)
			{
				MessageToCaster("This spell requires " + m_spell.Concentration + " concentration points to cast!", EChatType.CT_SpellResisted);
				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0 && m_caster.effectListComponent.ConcentrationEffects.Count >= MAX_CONC_SPELLS)
			{
				MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", EChatType.CT_SpellResisted);
				return false;
			}

			return true;
		}

		public virtual bool CheckDuringCast(GameLiving target)
		{
			return CheckDuringCast(target, false);
		}

		public virtual bool CheckDuringCast(GameLiving target, bool quiet)
		{
			if (m_interrupted)
				return false;

			if (m_caster.ObjectState != GameObject.eObjectState.Active)
				return false;

			if (!m_caster.IsAlive)
			{
				if (!quiet)
					MessageToCaster("You are dead and can't cast!", EChatType.CT_System);

				return false;
			}

			if (Caster is GameNpc npcOwner)
			{
				if (Spell.CastTime > 0)
				{
					if (npcOwner.IsMoving)
						npcOwner.StopFollowing();
				}

				if (npcOwner != Target)
					npcOwner.TurnTo(Target);
			}

			if (m_spell.InstrumentRequirement != 0)
			{
				if (!CheckInstrument())
				{
					if (!quiet)
						MessageToCaster("You are not wielding the right type of instrument!", EChatType.CT_SpellResisted);

					return false;
				}
			}
			else if (m_caster.IsSitting) // songs can be played if sitting
			{
				//Purge can be cast while sitting but only if player has negative effect that
				//don't allow standing up (like stun or mez)
				if (!quiet)
					MessageToCaster("You can't cast while sitting!", EChatType.CT_SpellResisted);

				return false;
			}

			if (m_spell.Target == ESpellTarget.AREA)
			{
				if (!m_caster.IsWithinRadius(m_caster.GroundTarget, CalculateSpellRange()))
				{
					if (!quiet)
						MessageToCaster("Your area target is out of range.  Select a closer target.", EChatType.CT_SpellResisted);

					return false;
				}
			}
			else if (m_spell.Target is not ESpellTarget.SELF and not ESpellTarget.GROUP and not ESpellTarget.CONE && m_spell.Range > 0)
			{
				if (m_spell.Target != ESpellTarget.PET)
				{
					//all other spells that need a target
					if (target == null || target.ObjectState != GameObject.eObjectState.Active)
					{
						if (Caster is GamePlayer && !quiet)
							MessageToCaster("You must select a target for this spell!", EChatType.CT_SpellResisted);

						return false;
					}

					if (Properties.CHECK_LOS_DURING_CAST && GameLoop.GameLoopTime > _lastDuringCastLosCheckTime + Properties.CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL)
					{
						_lastDuringCastLosCheckTime = GameLoop.GameLoopTime;

						if (Caster is GameNpc npc && npc.Brain is IControlledBrain npcBrain)
							npcBrain.GetPlayerOwner()?.Out.SendCheckLOS(npc, target, CheckPetLosDuringCastCallback);
						else if (Caster is GamePlayer player)
							player.Out.SendCheckLOS(player, target, CheckPlayerLosDuringCastCallback);
					}
				}

				switch (m_spell.Target)
				{
					case ESpellTarget.ENEMY:
					{
						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, quiet))
							return false;

						break;
					}
					case ESpellTarget.CORPSE:
					{
						if (target.IsAlive || !GameServer.ServerRules.IsSameRealm(Caster, target, quiet))
						{
							if (!quiet)
								MessageToCaster("This spell only works on dead members of your realm!", EChatType.CT_SpellResisted);

							return false;
						}

						break;
					}
				}
			}

			if (m_caster.Mana <= 0 && Spell.Power > 0 && Spell.SpellType != ESpellType.Archery)
			{
				if (!quiet)
					MessageToCaster("You have exhausted all of your power and cannot cast spells!", EChatType.CT_SpellResisted);

				return false;
			}

			if (Spell.Power != 0 && m_caster.Mana < PowerCost(target) && EffectListService.GetAbilityEffectOnTarget(Caster, EEffect.QuickCast) == null && Spell.SpellType != ESpellType.Archery)
			{
				if (!quiet)
					MessageToCaster("You don't have enough power to cast that!", EChatType.CT_SpellResisted);

				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0 && m_caster.Concentration < m_spell.Concentration)
			{
				if (!quiet)
					MessageToCaster("This spell requires " + m_spell.Concentration + " concentration points to cast!", EChatType.CT_SpellResisted);

				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0 && m_caster.effectListComponent.ConcentrationEffects.Count >= MAX_CONC_SPELLS)
			{
				if (!quiet)
					MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", EChatType.CT_SpellResisted);

				return false;
			}
			
			if ((m_caster.IsMoving || m_caster.IsStrafing) && !Spell.MoveCast)
			{
				CasterMoves();
				return false;
			}

			return true;
		}

		#endregion

		//This is called after our pre-cast checks are done (Range, valid target, mana pre-req, and standing still?) and checks for the casting states
		public void Tick(long currentTick)
		{
			switch (CastState)
			{
				case ECastState.Precast:
				{
					if (CheckBeginCast(Target))
					{
						_castStartTick = currentTick;

						if (Spell.IsInstantCast)
						{
							if (!CheckEndCast(Target))
								CastState = ECastState.Interrupted;
							else
							{
								// Unsure about this. Calling 'SendCastAnimation' on non-harmful instant spells plays an annoying deep hum that overlaps with the
								// sound of the spell effect (but is fine to have on harmful ones). For certain spells (like Skald's resist chants) it instead
								// plays the audio of the spell effect a second time.
								// It may prevent certain animations from playing, but I don't think there's any non-harmful instant spell with a casting animation.
								if (Spell.IsHarmful)
									SendCastAnimation(0);

								CastState = ECastState.Finished;
							}
						}
						else
						{
							SendSpellMessages();
							SendCastAnimation();
							CastState = ECastState.Casting;
						}
					}
					else
					{
						if (Caster.InterruptAction > 0 && Caster.InterruptTime > GameLoop.GameLoopTime)
							CastState = ECastState.Interrupted;
						else
							CastState = ECastState.Cleanup;
					}

					break;
				}
				case ECastState.Casting:
				{
					if (!CheckDuringCast(Target))
						CastState = ECastState.Interrupted;
					if (_castStartTick + _calculatedCastTime < currentTick)
					{
						if (!(m_spell.IsPulsing && m_spell.SpellType == ESpellType.Mesmerize))
						{
							if (!CheckEndCast(Target))
								CastState = ECastState.Interrupted;
							else
								CastState = ECastState.Finished;
						}
						else
						{
							if (CheckEndCast(Target))
								CastState = ECastState.Finished;
						}
					}

					break;
				}
				case ECastState.Interrupted:
				{
					InterruptCasting();
					SendInterruptCastAnimation();
					CastState = ECastState.Cleanup;
					break;
				}
				case ECastState.Focusing:
				{
					if ((Caster is GamePlayer && (Caster as GamePlayer).IsStrafing) || Caster.IsMoving)
					{
						CasterMoves();
						CastState = ECastState.Cleanup;
					}

					break;
				}
			}

			//Process cast on same tick if finished.
			if (CastState == ECastState.Finished)
			{
				FinishSpellCast(Target);
				if (Spell.IsFocus)
				{
					if (Spell.SpellType != ESpellType.GatewayPersonalBind)
					{
						CastState = ECastState.Focusing;
					}
					else
					{
						CastState = ECastState.Cleanup;

						var stone = Caster.Inventory.GetFirstItemByName("Personal Bind Recall Stone", EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
						stone.CanUseAgainIn = stone.CanUseEvery;

						//.SetCooldown();
					}
				}
				else
					CastState = ECastState.Cleanup;
			}

			if (CastState == ECastState.Cleanup)
				Caster.castingComponent.OnSpellHandlerCleanUp(Spell);
		}

		/// <summary>
		/// Calculates the power to cast the spell
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public virtual int PowerCost(GameLiving target)
		{
			/*
			// warlock
			GameSpellEffect effect = SpellHandler.FindEffectOnTarget(m_caster, "Powerless");
			if (effect != null && !m_spell.IsPrimary)
				return 0;*/

			//1.108 - Valhallas Blessing now has a 75% chance to not use power.
			NfRaValhallasBlessingEffect ValhallasBlessing = m_caster.EffectList.GetOfType<NfRaValhallasBlessingEffect>();
			if (ValhallasBlessing != null && Util.Chance(75))
				return 0;

			//patch 1.108 increases the chance to not use power to 50%.
			NfRaFungalUnionEffect FungalUnion = m_caster.EffectList.GetOfType<NfRaFungalUnionEffect>();
			{
				if (FungalUnion != null && Util.Chance(50))
					return 0;
			}

			// Arcane Syphon chance
			int syphon = Caster.GetModified(EProperty.ArcaneSyphon);
			if (syphon > 0)
			{
				if(Util.Chance(syphon))
				{
					return 0;
				}
			}

			double basepower = m_spell.Power; //<== defined a basevar first then modified this base-var to tell %-costs from absolut-costs

			// percent of maxPower if less than zero
			if (basepower < 0)
			{
				if (Caster is GamePlayer && ((GamePlayer)Caster).PlayerClass.ManaStat != EStat.UNDEFINED)
				{
					GamePlayer player = Caster as GamePlayer;
					basepower = player.CalculateMaxMana(player.Level, player.GetBaseStat(player.PlayerClass.ManaStat)) * basepower * -0.01;
				}
				else
				{
					basepower = Caster.MaxMana * basepower * -0.01;
				}
			}

			double power = basepower * 1.2; //<==NOW holding basepower*1.2 within 'power'

			EProperty focusProp = SkillBase.SpecToFocus(SpellLine.Spec);
			if (focusProp != EProperty.Undefined)
			{
				double focusBonus = Caster.GetModified(focusProp) * 0.4;
				if (Spell.Level > 0)
					focusBonus /= Spell.Level;
				if (focusBonus > 0.4)
					focusBonus = 0.4;
				else if (focusBonus < 0)
					focusBonus = 0;
				if (Caster is GamePlayer)
				{
					var spec = ((GamePlayer)Caster).GetModifiedSpecLevel(SpellLine.Spec);
					double specBonus = Math.Min(spec, 50) / (Spell.Level * 1.0);
					if (specBonus > 1)
						specBonus = 1;
					focusBonus *= specBonus;
				}
				power -= basepower * focusBonus; //<== So i can finally use 'basepower' for both calculations: % and absolut
			}
			else if (Caster is GamePlayer && ((GamePlayer)Caster).PlayerClass.ClassType == EPlayerClassType.Hybrid)
			{
				double specBonus = 0;
				if (Spell.Level != 0) specBonus = (((GamePlayer)Caster).GetBaseSpecLevel(SpellLine.Spec) * 0.4 / Spell.Level);

				if (specBonus > 0.4)
					specBonus = 0.4;
				else if (specBonus < 0)
					specBonus = 0;
				power -= basepower * specBonus;
			}
			// doubled power usage if quickcasting
			if (EffectListService.GetAbilityEffectOnTarget(Caster, EEffect.QuickCast) != null && Spell.CastTime > 0)
				power *= 2;
			return (int)power;
		}

		/// <summary>
		/// Calculates the enduance cost of the spell
		/// </summary>
		/// <returns></returns>
		public virtual int CalculateEnduranceCost()
		{
			return 5;
		}

		/// <summary>
		/// Calculates the range to target needed to cast the spell
		/// NOTE: This method returns a minimum value of 32
		/// </summary>
		/// <returns></returns>
		public virtual int CalculateSpellRange()
		{
			int range = Math.Max(32, (int)(Spell.Range * Caster.GetModified(EProperty.SpellRange) * 0.01));
			return range;
			//Dinberg: add for warlock range primer
		}

		/// <summary>
		/// Called whenever the casters casting sequence is to interrupt immediately
		/// </summary>
		public virtual void InterruptCasting()
		{
			if (m_interrupted)
				return;

			m_interrupted = true;
			Caster.castingComponent.InterruptCasting();
			CastState = ECastState.Interrupted;
			m_startReuseTimer = false;
		}

		/// <summary>
		/// Calculates the effective casting time
		/// </summary>
		/// <returns>effective casting time in milliseconds</returns>
		public virtual int CalculateCastingTime()
		{
			return m_caster.CalculateCastingTime(m_spellLine, m_spell);
		}

		#region animations

		/// <summary>
		/// Sends the cast animation
		/// </summary>
		public virtual void SendCastAnimation()
		{
			if (Spell.CastTime == 0)
				SendCastAnimation(0);
			else
				SendCastAnimation((ushort)(CalculateCastingTime() / 100));
		}

		/// <summary>
		/// Sends the cast animation
		/// </summary>
		/// <param name="castTime">The cast time</param>
		public virtual void SendCastAnimation(ushort castTime)
		{
			_calculatedCastTime = castTime * 100;

			foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					return;

				player.Out.SendSpellCastAnimation(m_caster, m_spell.ClientEffect, castTime);
			}
		}

		/// <summary>
		/// Send the Effect Animation
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="boltDuration">The duration of a bolt</param>
		/// <param name="noSound">sound?</param>
		/// <param name="success">spell success?</param>
		public virtual void SendEffectAnimation(GameObject target, ushort boltDuration, bool noSound, byte success)
		{
			if (target == null)
				target = m_caster;

			foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(m_caster, target, m_spell.ClientEffect, boltDuration, noSound, success);
		}

		/// <summary>
		/// Send the Interrupt Cast Animation
		/// </summary>
		public virtual void SendInterruptCastAnimation()
		{
			foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendInterruptAnimation(m_caster);
		}

		public virtual void SendEffectAnimation(GameObject target, ushort clientEffect, ushort boltDuration, bool noSound, byte success)
		{
			if (target == null)
				target = m_caster;

			foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(m_caster, target, clientEffect, boltDuration, noSound, success);
		}
		#endregion

		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public virtual void FinishSpellCast(GameLiving target)
		{
			GamePlayer playerCaster = Caster as GamePlayer;
			DbInventoryItem playerWeapon = null;

			if (playerCaster != null)
			{
				playerWeapon = playerCaster.ActiveWeapon;

				if (!HasPositiveEffect)
				{
					if (playerCaster.IsOnHorse)
						playerCaster.IsOnHorse = false;

					(playerWeapon as GameInventoryItem)?.OnSpellCast(playerCaster, target, Spell);
				}

				if (UnstealthCasterOnFinish)
					playerCaster.Stealth(false);
			}

			// Messages
			if (Spell.InstrumentRequirement == 0 && Spell.ClientEffect != 0)
			{
				if (Spell.SpellType != ESpellType.PveResurrectionIllness && Spell.SpellType != ESpellType.RvrResurrectionIllness)
				{
					if (playerCaster != null)
						// Message: You cast a {0} spell!
						MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CastSpell.Msg.YouCastSpell", Spell.Name), EChatType.CT_Spell);
					if (Caster is NecromancerPet {Owner: GamePlayer casterOwner})
						// Message: {0} cast a {1} spell!
						casterOwner.Out.SendMessage(LanguageMgr.GetTranslation(casterOwner.Client.Account.Language, "SpellHandler.CastSpell.Msg.PetCastSpell", Caster.GetName(0, true), Spell.Name), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
					{
						if (player != m_caster)
							// Message: {0} casts a spell!
							player.MessageFromArea(m_caster, LanguageMgr.GetTranslation(player.Client, "SpellHandler.CastSpell.Msg.LivingCastsSpell", Caster.GetName(0, true)), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					}
				}
			}

			// Cancel existing pulse effects, using 'SpellGroupsCancellingOtherPulseSpells'.
			if (m_spell.IsPulsing)
			{
				if (!PulseSpellGroupsIgnoringOtherPulseSpells.Contains(m_spell.Group))
				{
					IEnumerable<EcsPulseEffect> effects = m_caster.effectListComponent.GetAllPulseEffects().Where(x => !PulseSpellGroupsIgnoringOtherPulseSpells.Contains(x.SpellHandler.Spell.Group));

					foreach (EcsPulseEffect effect in effects)
						EffectService.RequestImmediateCancelConcEffect(effect);
				}

				if (m_spell.SpellType != ESpellType.Mesmerize)
				{
					CreateECSPulseEffect(Caster, Caster.Effectiveness);
					Caster.ActivePulseSpells.AddOrUpdate(m_spell.SpellType, m_spell, (x, y) => m_spell);
				}
			}

			if (playerWeapon != null)
				StartSpell(target, playerWeapon);
			else
				StartSpell(target);

			/*
			//Dinberg: This is where I moved the warlock part (previously found in gameplayer) to prevent
			//cancelling before the spell was fired.
			if (m_spell.SpellType != eSpellType.Powerless && m_spell.SpellType != eSpellType.Range && m_spell.SpellType != eSpellType.Uninterruptable)
			{
				GameSpellEffect effect = SpellHandler.FindEffectOnTarget(m_caster, "Powerless");
				if (effect == null)
					effect = SpellHandler.FindEffectOnTarget(m_caster, "Range");
				if (effect == null)
					effect = SpellHandler.FindEffectOnTarget(m_caster, "Uninterruptable");

				//if we found an effect, cancel it!
				if (effect != null)
					effect.Cancel(false);
			}*/

			//the quick cast is unallowed whenever you miss the spell
			//set the time when casting to can not quickcast during a minimum time
			if (m_caster is GamePlayer)
			{
				QuickCastEcsAbilityEffect quickcast = (QuickCastEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(m_caster, EEffect.QuickCast);
				if (quickcast != null && Spell.CastTime > 0)
				{
					m_caster.TempProperties.SetProperty(GamePlayer.QUICK_CAST_CHANGE_TICK, m_caster.CurrentRegion.Time);
					((GamePlayer)m_caster).DisableSkill(SkillBase.GetAbility(Abilities.Quickcast), QuickCastAbilityHandler.DISABLE_DURATION);
					//EffectService.RequestImmediateCancelEffect(quickcast, false);
					quickcast.Cancel(false);
				}
			}

			if (m_ability != null)
				m_caster.DisableSkill(m_ability.Ability, (m_spell.RecastDelay == 0 ? 3000 : m_spell.RecastDelay));

			// disable spells with recasttimer (Disables group of same type with same delay)
			if (m_spell.RecastDelay > 0 && m_startReuseTimer)
			{
				if (m_caster is GamePlayer)
				{
					ICollection<Tuple<Skill, int>> toDisable = new List<Tuple<Skill, int>>();
					
					GamePlayer gp_caster = m_caster as GamePlayer;
					foreach (var skills in gp_caster.GetAllUsableSkills())
						if (skills.Item1 is Spell &&
							(((Spell)skills.Item1).ID == m_spell.ID || ( ((Spell)skills.Item1).SharedTimerGroup != 0 && ( ((Spell)skills.Item1).SharedTimerGroup == m_spell.SharedTimerGroup) ) ))
							toDisable.Add(new Tuple<Skill, int>((Spell)skills.Item1, m_spell.RecastDelay));
					
					foreach (var sl in gp_caster.GetAllUsableListSpells())
						foreach(var sp in sl.Item2)
							if (sp is Spell &&
								( ((Spell)sp).ID == m_spell.ID || ( ((Spell)sp).SharedTimerGroup != 0 && ( ((Spell)sp).SharedTimerGroup == m_spell.SharedTimerGroup) ) ))
							toDisable.Add(new Tuple<Skill, int>((Spell)sp, m_spell.RecastDelay));
					
					m_caster.DisableSkills(toDisable);
				}
				else if (m_caster is GameNpc)
					m_caster.DisableSkill(m_spell, m_spell.RecastDelay);
			}

			/*if(Caster is GamePlayer && target != null)
			{
				(Caster as GamePlayer).Out.SendObjectUpdate(target);
			}*/
			if(!this.Spell.IsPulsingEffect && !this.Spell.IsPulsing && Caster is GamePlayer {PlayerClass: not ClassSavage})
				m_caster.ChangeEndurance(m_caster, EEnduranceChangeType.Spell, -5);

			GameEventMgr.Notify(GameLivingEvent.CastFinished, m_caster, new CastingEventArgs(this, target, m_lastAttackData));
		}

		/// <summary>
		/// Select all targets for this spell
		/// </summary>
		/// <param name="castTarget"></param>
		/// <returns></returns>
		public virtual IList<GameLiving> SelectTargets(GameObject castTarget)
		{
			List<GameLiving> list = new(8);
			GameLiving target = castTarget as GameLiving;
			ESpellTarget modifiedTarget = Spell.Target;
			ushort modifiedRadius = (ushort) Spell.Radius;

			if (modifiedTarget == ESpellTarget.PET && !HasPositiveEffect)
				modifiedTarget = ESpellTarget.ENEMY;

			switch (modifiedTarget)
			{
				case ESpellTarget.AREA:
				{
					//Dinberg - fix for animists turrets, where before a radius of zero meant that no targets were ever selected!
					if (Spell.SpellType == ESpellType.SummonAnimistPet || Spell.SpellType == ESpellType.SummonAnimistFnF)
						list.Add(Caster);
					else if (modifiedRadius > 0)
					{
						ConcurrentBag<GamePlayer> aoePlayers = new();

						foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(Caster.CurrentRegionID, Caster.GroundTarget, modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							{
								// Apply Mentalist RA5L
								NfRaSelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<NfRaSelectiveBlindnessEffect>();
								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;
									if (EffectOwner == player)
									{
										if (Caster is GamePlayer player1)
											player1.Out.SendMessage(string.Format("{0} is invisible to you!", player.GetName(0, true)), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
									}
									else
										aoePlayers.Add(player);
								}
								else
									aoePlayers.Add(player);
							}
						}

						list.AddRange(aoePlayers);
						ConcurrentBag<GameNpc> aoeMobs = new();

						foreach (GameNpc npc in WorldMgr.GetNPCsCloseToSpot(Caster.CurrentRegionID, Caster.GroundTarget, modifiedRadius))
						{
							if (npc is GameStorm)
								aoeMobs.Add(npc);
							else if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							{
								if (!npc.HasAbility("DamageImmunity"))
									aoeMobs.Add(npc);
							}
						}

						list.AddRange(aoeMobs);
					}

					break;
				}
				case ESpellTarget.CORPSE:
				{
					if (target != null && !target.IsAlive)
						list.Add(target);

					break;
				}
				case ESpellTarget.PET:
				{
					// PBAE spells.
					if (modifiedRadius > 0 && Spell.Range == 0)
					{
						foreach (GameNpc npcInRadius in Caster.GetNPCsInRadius(modifiedRadius))
						{
							if (Caster.IsControlledNPC(npcInRadius))
								list.Add(npcInRadius);
						}

						return list;
					}

					if (target == null)
						break;

					GameNpc pet = target as GameNpc;

					if (pet != null && Caster.IsWithinRadius(pet, Spell.Range))
					{
						if (Caster.IsControlledNPC(pet))
							list.Add(pet);
					}

					// Check 'ControlledBrain' if 'target' isn't a valid target.
					if (!list.Any() && Caster.ControlledBrain != null)
					{
						if (Caster is GamePlayer player && player.PlayerClass.Name.ToLower() == "bonedancer")
						{
							foreach (GameNpc npcInRadius in player.GetNPCsInRadius((ushort) Spell.Range))
							{
								if (npcInRadius is CommanderPet commander && commander.Owner == player)
									list.Add(commander);
								else if (npcInRadius is SubPet {Brain: IControlledBrain brain} subpet && brain.GetPlayerOwner() == player)
								{
									if (!Spell.IsHealing)
										list.Add(subpet);
								}
							}
						}
						else
						{
							pet = Caster.ControlledBrain.Body;

							if (pet != null && Caster.IsWithinRadius(pet, Spell.Range))
								list.Add(pet);
						}
					}

					if (Spell.Radius == 0)
						return list;

					// Buffs affect every pet around the targetted pet (same owner).
					if (pet != null)
					{
						foreach (GameNpc npcInRadius in pet.GetNPCsInRadius(modifiedRadius))
						{
							if (npcInRadius == pet || !Caster.IsControlledNPC(npcInRadius) || npcInRadius.Brain is BomberBrain)
								continue;

							list.Add(npcInRadius);
						}
					}

					break;
				}
				case ESpellTarget.ENEMY:
				{
					if (modifiedRadius > 0)
					{
						if (Spell.SpellType != ESpellType.TurretPBAoE && (target == null || Spell.Range == 0))
							target = Caster;
						if (target == null)
							return null;

						ConcurrentBag<GamePlayer> aoePlayers = new();

						foreach (GamePlayer player in  target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							{
								NfRaSelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<NfRaSelectiveBlindnessEffect>();

								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;
									if (EffectOwner == player)
									{
										if (Caster is GamePlayer player1)
											player1.Out.SendMessage(string.Format("{0} is invisible to you!", player.GetName(0, true)), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
									}
									else
										aoePlayers.Add(player);
								}
								else
									aoePlayers.Add(player);
							}
						}

						list.AddRange(aoePlayers);
						ConcurrentBag<GameNpc> aoeMobs = new();

						foreach (GameNpc npc in  target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							{
								if (!npc.HasAbility("DamageImmunity"))
									aoeMobs.Add(npc);
							}
						}

						list.AddRange(aoeMobs);
					}
					else
					{
						if (target != null && GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
						{
							// Apply Mentalist RA5L
							if (Spell.Range > 0)
							{
								NfRaSelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<NfRaSelectiveBlindnessEffect>();
								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;
									if (EffectOwner == target)
									{
										if (Caster is GamePlayer player)
											player.Out.SendMessage(string.Format("{0} is invisible to you!", target.GetName(0, true)), EChatType.CT_Missed, EChatLoc.CL_SystemWindow);
									}
									else if (!target.HasAbility("DamageImmunity"))
										list.Add(target);
								}
								else if (!target.HasAbility("DamageImmunity"))
									list.Add(target);
							}
							else if (!target.HasAbility("DamageImmunity"))
								list.Add(target);
						}
					}

					break;
				}
				case ESpellTarget.REALM:
				{
					if (modifiedRadius > 0)
					{
						if (target == null || Spell.Range == 0)
							target = Caster;

						ConcurrentBag<GameLiving> aoePlayers = new();

						foreach (GamePlayer player in target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsSameRealm(Caster, player, true))
							{
								if (player.PlayerClass.ID == (int)EPlayerClass.Necromancer && player.IsShade)
								{
									if (!Spell.IsBuff)
										aoePlayers.Add(player.ControlledBrain.Body);
									else
										aoePlayers.Add(player);
								}
								else
									aoePlayers.Add(player);
							}
						}

						list.AddRange(aoePlayers);
						ConcurrentBag<GameNpc> aoeMobs = new();

						foreach (GameNpc npc in target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsSameRealm(Caster, npc, true))
							{
								if (npc.Brain is BomberBrain)
									continue;

								aoeMobs.Add(npc);
							}
						}

						list.AddRange(aoeMobs);
					}
					else
					{
						if (target != null && GameServer.ServerRules.IsSameRealm(Caster, target, true))
						{
							if (target is GamePlayer player && player.PlayerClass.ID == (int)EPlayerClass.Necromancer && player.IsShade)
							{
								// Only buffs, Necromancer's power transfer, and teleport spells can be casted on the shade
								if (Spell.IsBuff || Spell.SpellType == ESpellType.PowerTransferPet || Spell.SpellType == ESpellType.UniPortal)
									list.Add(player);
								else
									list.Add(player.ControlledBrain.Body);
							}
							else
								list.Add(target);
						}
					}

					break;
				}
				case ESpellTarget.SELF:
				{
					if (modifiedRadius > 0)
					{
						if (target == null || Spell.Range == 0)
							target = Caster;

						ConcurrentBag<GamePlayer> aoePlayers = new();

						foreach (GamePlayer player in target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true) == false)
								aoePlayers.Add(player);
						}

						list.AddRange(aoePlayers);
						ConcurrentBag<GameNpc> aoeMobs = new();

						foreach (GameNpc npc in target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true) == false)
								aoeMobs.Add(npc);
						}

						list.AddRange(aoeMobs);
					}
					else
						list.Add(Caster);

					break;
				}
				case ESpellTarget.GROUP:
				{
					GroupUtil group = m_caster.Group;
						
					int spellRange;
					if (Spell.Range == 0)
						spellRange = modifiedRadius;
					else
						spellRange = CalculateSpellRange();

					if (group == null)
					{
						if (m_caster is GamePlayer)
						{
							list.Add(m_caster);

							IControlledBrain npc = m_caster.ControlledBrain;
							if (npc != null)
							{
								//Add our first pet
								GameNpc petBody2 = npc.Body;
								if (m_caster.IsWithinRadius(petBody2, spellRange))
									list.Add(petBody2);

								//Now lets add any subpets!
								if (petBody2 != null && petBody2.ControlledNpcList != null)
								{
									foreach (IControlledBrain icb in petBody2.ControlledNpcList)
									{
										if (icb != null && m_caster.IsWithinRadius(icb.Body, spellRange))
											list.Add(icb.Body);
									}
								}
							}
						}// if (m_caster is GamePlayer)
						else if (m_caster is GameNpc && (m_caster as GameNpc).Brain is ControlledNpcBrain)
						{
							IControlledBrain casterbrain = (m_caster as GameNpc).Brain as IControlledBrain;

							GamePlayer player = casterbrain.GetPlayerOwner();

							if (player != null)
							{
								if (player.Group == null)
								{
									// No group, add both the pet and owner to the list
									list.Add(player);
									list.Add(m_caster);
								}
								else
									// Assign the owner's group so they are added to the list
									group = player.Group;
							}
							else
								list.Add(m_caster);
						}// else if (m_caster is GameNPC...
						else
							list.Add(m_caster);
					}// if (group == null)
						
					//We need to add the entire group
					if (group != null)
					{
						foreach (GameLiving living in group.GetMembersInTheGroup())
						{
							// only players in range
							if (m_caster.IsWithinRadius(living, spellRange))
							{
								list.Add(living);

								IControlledBrain npc = living.ControlledBrain;
								if (npc != null)
								{
									//Add our first pet
									GameNpc petBody2 = npc.Body;
									if (m_caster.IsWithinRadius(petBody2, spellRange))
										list.Add(petBody2);

									//Now lets add any subpets!
									if (petBody2 != null && petBody2.ControlledNpcList != null)
									{
										foreach (IControlledBrain icb in petBody2.ControlledNpcList)
										{
											if (icb != null && m_caster.IsWithinRadius(icb.Body, spellRange))
												list.Add(icb.Body);
										}
									}
								}
							}
						}
					}

					break;
				}
				case ESpellTarget.CONE:
				{
					target = Caster;

					ConcurrentBag<GamePlayer> aoePlayers = new();

					foreach (GamePlayer player in target.GetPlayersInRadius((ushort) Spell.Range))
					{
						if (player == Caster)
							continue;

						if (!m_caster.IsObjectInFront(player, (Spell.Radius != 0 ? Spell.Radius : 100)))
							continue;

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							continue;

						aoePlayers.Add(player);
					}

					list.AddRange(aoePlayers);
					ConcurrentBag<GameNpc> aoeMobs = new();

					foreach (GameNpc npc in target.GetNPCsInRadius((ushort) Spell.Range))
					{
						if (npc == Caster)
							continue;

						if (!m_caster.IsObjectInFront(npc, (Spell.Radius != 0 ? Spell.Radius : 100)))
							continue;

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							continue;

						if (!npc.HasAbility("DamageImmunity"))
							aoeMobs.Add(npc);
					}

					list.AddRange(aoeMobs);
					break;
				}
			}

			return list;
		}

		/// <summary>
		/// Cast all subspell recursively
		/// </summary>
		/// <param name="target"></param>
		public virtual void CastSubSpells(GameLiving target)
		{
			List<int> subSpellList = new List<int>();
			if (m_spell.SubSpellID > 0)
				subSpellList.Add(m_spell.SubSpellID);
			
			foreach (int spellID in subSpellList.Union(m_spell.MultipleSubSpells))
			{
				Spell spell = SkillBase.GetSpellByID(spellID);
				//we need subspell ID to be 0, we don't want spells linking off the subspell
				if (target != null && spell != null && spell.SubSpellID == 0)
				{
					// We have to scale pet subspells when cast
					if (Caster is GameSummonedPet pet && !(Caster is NecromancerPet))
						pet.ScalePetSpell(spell);

					ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_caster, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
					spellhandler.StartSpell(target);
				}
			}
		}

		public virtual List<GameLiving> GetGroupAndPets(Spell spell)
		{
			List<GameLiving> livingsInRange = new();
			ICollection<GameLiving> groupMembers = Caster.Group?.GetMembersInTheGroup() ?? (Caster as NecromancerPet)?.Owner.Group?.GetMembersInTheGroup();

			if (groupMembers == null)
				groupMembers = new List<GameLiving>(){ Caster };

			foreach (GameLiving living in groupMembers)
			{
				IControlledBrain controlledBrain = living.ControlledBrain;
				IControlledBrain[] subControlledBrains = controlledBrain?.Body.ControlledNpcList;

				if (subControlledBrains != null)
				{
					foreach (IControlledBrain subControlledBrain in subControlledBrains.Where(x => x != null && Caster.IsWithinRadius(x.Body, spell.Range)))
						livingsInRange.Add(subControlledBrain.Body);
				}

				if (controlledBrain != null)
				{
					if (Caster.IsWithinRadius(controlledBrain.Body, spell.Range))
						livingsInRange.Add(controlledBrain.Body);
				}

				if (Caster == living || Caster.IsWithinRadius(living, spell.Range))
					livingsInRange.Add(living);
			}

			return livingsInRange;
		}

		/// <summary>
		/// Tries to start a spell attached to an item (/use with at least 1 charge)
		/// Override this to do a CheckBeginCast if needed, otherwise spell will always cast and item will be used.
		/// </summary>
		public virtual bool StartSpell(GameLiving target, DbInventoryItem item)
		{
			m_spellItem = item;
			return StartSpell(target);
		}

		/// <summary>
		/// Called when spell effect has to be started and applied to targets
		/// This is typically called after calling CheckBeginCast
		/// </summary>
		/// <param name="target">The current target object, only used if 'SpellHandler.Target' is null.</param>
		public virtual bool StartSpell(GameLiving target)
		{
			if (Caster.IsMezzed || Caster.IsStunned)
			{
				Caster.CancelFocusSpell();
				return false;
			}

			if (Spell.SpellType != ESpellType.TurretPBAoE && Spell.IsPBAoE)
				Target = Caster;
			else if (Target == null)
				Target = target;

			if (Target != null)
			{
				if (Spell.IsFocus && (!Target.IsAlive || !Caster.IsWithinRadius(Target, Spell.Range)))
				{
					Caster.CancelFocusSpell();
					return false;
				}

				if (HasPositiveEffect && Target is GamePlayer p && Caster is GamePlayer c && Target != Caster && p.NoHelp)
				{
					c.Out.SendMessage(Target.Name + " has chosen to walk the path of solitude, and your spell fails.", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
					return false;
				}
			}

			IList<GameLiving> targets;
			if (Spell.Target == ESpellTarget.REALM
				&& (Target == Caster || Caster is NecromancerPet nPet && Target == nPet.Owner)
				&& !Spell.IsConcentration
				&& !Spell.IsHealing
				&& Spell.IsBuff
				&& Spell.SpellType != ESpellType.Bladeturn
				&& Spell.SpellType != ESpellType.Bomber)
				targets = GetGroupAndPets(Spell);
			else
				targets = SelectTargets(Target);

			Effectiveness = Caster.Effectiveness;

			if (SpellLine.KeyName == "OffensiveProc" &&  Caster is GameSummonedPet gpet && !Spell.ScaledToPetLevel)
				gpet.ScalePetSpell(Spell);

			/// [Atlas - Takii] No effectiveness drop in OF MOC.
// 			if (Caster.EffectList.GetOfType<MasteryofConcentrationEffect>() != null)
// 			{
// 				AtlasOF_MasteryofConcentration ra = Caster.GetAbility<AtlasOF_MasteryofConcentration>();
// 				if (ra != null && ra.Level > 0)
// 				{
// 					Effectiveness *= System.Math.Round((double)ra.GetAmountForLevel(ra.Level) / 100, 2);
// 				}
// 			}

			//[StephenxPimentel] Reduce Damage if necro is using MoC
// 			if (Caster is NecromancerPet)
// 			{
// 				if ((Caster as NecromancerPet).Owner.EffectList.GetOfType<MasteryofConcentrationEffect>() != null)
// 				{
// 					AtlasOF_MasteryofConcentration necroRA = (Caster as NecromancerPet).Owner.GetAbility<AtlasOF_MasteryofConcentration>();
// 					if (necroRA != null && necroRA.Level > 0)
// 					{
// 						Effectiveness *= System.Math.Round((double)necroRA.GetAmountForLevel(necroRA.Level) / 100, 2);
// 					}
// 				}
// 			}

			if (Caster is GamePlayer && (Caster as GamePlayer).PlayerClass.ID == (int)EPlayerClass.Warlock && m_spell.IsSecondary)
			{
				Spell uninterruptibleSpell = Caster.TempProperties.GetProperty<Spell>(UninterruptableSpell.WARLOCK_UNINTERRUPTABLE_SPELL);

				if (uninterruptibleSpell != null && uninterruptibleSpell.Value > 0)
				{
					double nerf = uninterruptibleSpell.Value;
					Effectiveness *= (1 - (nerf * 0.01));
					Caster.TempProperties.RemoveProperty(UninterruptableSpell.WARLOCK_UNINTERRUPTABLE_SPELL);
				}
			}

			foreach (GameLiving targetInList in targets)
			{
				if (CheckSpellResist(targetInList))
					continue;

				if (Spell.Radius == 0 || HasPositiveEffect)
					ApplyEffectOnTarget(targetInList);
				else
				{
					if (Spell.Target == ESpellTarget.AREA)
						_distanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster.GroundTarget), Spell.Radius);
					else if (Spell.Target == ESpellTarget.CONE)
						_distanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster), Spell.Range);
					else
						_distanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Target), Spell.Radius);

					ApplyEffectOnTarget(targetInList);
				}

				if (Spell.IsConcentration && Caster is GameNpc npc && npc.Brain is ControlledNpcBrain npcBrain && Spell.IsBuff)
					npcBrain.AddBuffedTarget(Target);
			}

			CastSubSpells(Target);
			return true;
		}

		protected virtual double CalculateDistanceFallOff(int distance, int radius)
		{
			return distance / (double) radius;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected virtual int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			if (Spell.Duration == 0)
				return 0;
			
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			if (Spell.InstrumentRequirement != 0)
			{
				DbInventoryItem instrument = Caster.ActiveWeapon;
				if (instrument != null)
				{
					duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level); // up to 200% duration for songs
					duration *= instrument.Condition / (double)instrument.MaxCondition * instrument.Quality / 100;
				}
			}

			duration *= effectiveness;
			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = (Spell.Duration * 4);
			return (int)duration;
		}

		/// <summary>
		/// Creates the corresponding spell effect for the spell
		/// </summary>
		/// <param name="target"></param>
		/// <param name="effectiveness"></param>
		/// <returns></returns>
		protected virtual GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			int freq = Spell != null ? Spell.Frequency : 0;
			return new GameSpellEffect(this, CalculateEffectDuration(target, effectiveness), freq, effectiveness);
		}

		public virtual void ApplyEffectOnTarget(GameLiving target)
		{
			if ((target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent))
			{
				bool isAllowed = false;
				bool isSilent = false;

				if (Spell.Radius == 0)
				{
					switch (Spell.SpellType)
					{
						case ESpellType.Archery:
						case ESpellType.Bolt:
						case ESpellType.Bomber:
						case ESpellType.DamageSpeedDecrease:
						case ESpellType.DirectDamage:
						case ESpellType.MagicalStrike:
						case ESpellType.SiegeArrow:
						case ESpellType.Lifedrain:
						case ESpellType.SiegeDirectDamage:
						case ESpellType.SummonTheurgistPet:
						case ESpellType.DirectDamageWithDebuff:
							isAllowed = true;
							break;
					}
				}

				if (Spell.Radius > 0)
				{
					// pbaoe is allowed, otherwise door is in range of a AOE so don't spam caster with a message
					if (Spell.Range == 0)
						isAllowed = true;
					else
						isSilent = true;
				}

				if (!isAllowed)
				{
					if (!isSilent)
						MessageToCaster($"Your spell has no effect on the {target.Name}!", EChatType.CT_SpellResisted);

					return;
				}
			}
			
			if (Spell.Radius == 0 &&
				//(m_spellLine.KeyName == GlobalSpellsLines.Item_Effects ||
				(m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect || 
				//m_spellLine.KeyName == GlobalSpellsLines.Potions_Effects || 
				m_spellLine.KeyName == Specs.Savagery || 
				m_spellLine.KeyName == GlobalSpellsLines.Character_Abilities || 
				m_spellLine.KeyName == "OffensiveProc"))
				Effectiveness = 1.0; // TODO player.PlayerEffectiveness


			if (Spell.Radius == 0 && (m_spellLine.KeyName == GlobalSpellsLines.Potions_Effects || m_spellLine.KeyName == GlobalSpellsLines.Item_Effects))
				Effectiveness = 1.0;

			if (Effectiveness <= 0)
				return;

			if ((Spell.Duration > 0 && Spell.Target != ESpellTarget.AREA) || Spell.Concentration > 0)
				OnDurationEffectApply(target);
			else
				OnDirectEffect(target);
				
			if (!HasPositiveEffect)
			{
				AttackData ad = new AttackData();
				ad.Attacker = Caster;
				ad.Target = target;
				ad.AttackType = EAttackType.Spell;
				ad.SpellHandler = this;
				ad.AttackResult = EAttackResult.HitUnstyled;
				ad.IsSpellResisted = false;
				ad.Damage = (int)Spell.Damage;
				ad.DamageType = Spell.DamageType;

				m_lastAttackData = ad;
				Caster.OnAttackEnemy(ad);

				// Harmful spells that deal no damage (ie. debuffs) should still trigger OnAttackedByEnemy.
				// Exception for DoTs here since the initial landing of the DoT spell reports 0 damage
				// and the first tick damage is done by the pulsing effect, which takes care of firing OnAttackedByEnemy.
				if (ad.Damage == 0 && ad.SpellHandler.Spell.SpellType != ESpellType.DamageOverTime)
					target.OnAttackedByEnemy(ad);
			}
		}

		/// <summary>
		/// Determines wether this spell is better than given one
		/// </summary>
		/// <param name="oldeffect"></param>
		/// <param name="neweffect"></param>
		/// <returns>true if this spell is better version than compare spell</returns>
		public virtual bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
		{
			Spell oldspell = oldeffect.Spell;
			Spell newspell = neweffect.Spell;
//			if (oldspell.SpellType != newspell.SpellType)
//			{
//				if (Log.IsWarnEnabled)
//					Log.Warn("Spell effect compare with different types " + oldspell.SpellType + " <=> " + newspell.SpellType + "\n" + Environment.StackTrace);
//				return false;
//			}
			if (oldspell.IsConcentration)
				return false;
			if (newspell.Damage < oldspell.Damage)
				return false;
			if (newspell.Value < oldspell.Value)
				return false;
			//makes problems for immunity effects
			if (!oldeffect.ImmunityState && !newspell.IsConcentration)
			{
				if (neweffect.Duration <= oldeffect.RemainingTime)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Determines wether this spell is compatible with given spell
		/// and therefore overwritable by better versions
		/// spells that are overwritable cannot stack
		/// </summary>
		/// <param name="compare"></param>
		/// <returns></returns>
		public virtual bool IsOverwritable(EcsGameSpellEffect compare)
		{
			if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
			if (compare.SpellHandler.Spell.SpellType != Spell.SpellType)
				return false;
			return true;
		}

		/// <summary>
		/// Determines wether this spell can be disabled
		/// by better versions spells that stacks without overwriting
		/// </summary>
		/// <param name="compare"></param>
		/// <returns></returns>
		public virtual bool IsCancellable(GameSpellEffect compare)
		{
			if (compare.SpellHandler != null)
			{
				if ((compare.SpellHandler.AllowCoexisting || AllowCoexisting)
					&& (!compare.SpellHandler.SpellLine.KeyName.Equals(SpellLine.KeyName, StringComparison.OrdinalIgnoreCase)
						|| compare.SpellHandler.Spell.IsInstantCast != Spell.IsInstantCast))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Determines wether new spell is better than old spell and should disable it
		/// </summary>
		/// <param name="oldeffect"></param>
		/// <param name="neweffect"></param>
		/// <returns></returns>
		public virtual bool IsCancellableEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
		{
			if (neweffect.SpellHandler.Spell.Value >= oldeffect.SpellHandler.Spell.Value)
				return true;
			
			return false;
		}

		public virtual void OnDurationEffectApply(GameLiving target)
		{
			if (!target.IsAlive || target.effectListComponent == null)
				return;

			double durationEffectiveness = Effectiveness;

			// Duration is reduced for AoE spells based on the distance from the center, but only in RvR combat and if the spell doesn't have a damage component.
			if (_distanceFallOff > 0 && Spell.Damage == 0 && (target is GamePlayer || (target is GameNpc npcTarget && npcTarget.Brain is IControlledBrain)))
				durationEffectiveness *= 1 - _distanceFallOff / 2;

			CreateECSEffect(new EcsGameEffectInitParams(target, CalculateEffectDuration(target, durationEffectiveness), Effectiveness, this));
		}
		
		/// <summary>
		/// Called when Effect is Added to target Effect List
		/// </summary>
		/// <param name="effect"></param>
		public virtual void OnEffectAdd(GameSpellEffect effect) { }
		
		/// <summary>
		/// Check for Spell Effect Removed to Enable Best Cancellable
		/// </summary>
		/// <param name="effect"></param>
		/// <param name="overwrite"></param>
		public virtual void OnEffectRemove(GameSpellEffect effect, bool overwrite)
		{
		}

		public virtual void OnDirectEffect(GameLiving target) { }

		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public virtual void OnEffectStart(GameSpellEffect effect)
		{
			if (Spell.Pulse == 0)
				SendEffectAnimation(effect.Owner, 0, false, 1);
		}

		/// <summary>
		/// When an applied effect pulses
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public virtual void OnEffectPulse(GameSpellEffect effect)
		{
			if (effect.Owner.IsAlive == false)
				effect.Cancel(false);
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public virtual int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			return 0;
		}

		/// <summary>
		/// Calculates the chance that the spell lands on target
		/// can be negative or above 100%
		/// </summary>
		/// <param name="target">spell target</param>
		/// <returns>chance that the spell lands on target</returns>
		public virtual int CalculateToHitChance(GameLiving target)
		{
			int spellLevel = Spell.Level + m_caster.GetModified(EProperty.SpellLevel);

			if (m_caster is GamePlayer playerCaster)
			{
				if (spellLevel > playerCaster.MaxLevel)
					spellLevel = playerCaster.MaxLevel;

				if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect || m_spellLine.KeyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
				{
					AttackData lastAD = playerCaster.TempProperties.GetProperty<AttackData>("LastAttackData", null);
					spellLevel = (lastAD != null && lastAD.Style != null) ? lastAD.Style.Level : Math.Min(playerCaster.MaxLevel, target.Level);
				}
			}

			/*
			http://www.camelotherald.com/news/news_article.php?storyid=704

			Q: Spell resists. Can you give me more details as to how the system works?

			A: Here's the answer, straight from the desk of the spell designer:

			"Spells have a factor of (spell level / 2) added to their chance to hit. (Spell level defined as the level the spell is awarded, chance to hit defined as
			the chance of avoiding the "Your target resists the spell!" message.) Subtracted from the modified to-hit chance is the target's (level / 2).
			So a L50 caster casting a L30 spell at a L50 monster or player, they have a base chance of 85% to hit, plus 15%, minus 25% for a net chance to hit of 75%.
			If the chance to hit goes over 100% damage or duration is increased, and if it goes below 55%, you still have a 55% chance to hit but your damage
			or duration is penalized. If the chance to hit goes below 0, you cannot hit at all. Once the spell hits, damage and duration are further modified
			by resistances.

			Note:  The last section about maintaining a chance to hit of 55% has been proven incorrect with live testing.  The code below is very close to live like.
			- Tolakram
			 */

			int hitChance = m_caster.GetModified(EProperty.ToHitBonus);

			if (m_caster is GameNpc)
				hitChance += (int)(87.5 - (target.Level - m_caster.Level));
			else
			{
				hitChance += 88 + (spellLevel - target.Level) / 2;

				if (target is GameNpc)
				{
					double mobScalar = m_caster.GetConLevel(target) > 3 ? 3 : m_caster.GetConLevel(target);
					hitChance -= (int)(mobScalar * Properties.PVE_SPELL_CONHITPERCENT);
					hitChance += Math.Max(0, target.attackComponent.Attackers.Count - 1) * Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
				}
			}

			if (m_caster.effectListComponent.ContainsEffectForEffectType(EEffect.PiercingMagic))
			{
				EcsGameEffect effect = m_caster.effectListComponent.GetSpellEffects().FirstOrDefault(e => e.EffectType == EEffect.PiercingMagic);

				if (effect != null)
					hitChance += (int)effect.SpellHandler.Spell.Value;
			}

			// Check for active RAs.
			if (m_caster.effectListComponent.ContainsEffectForEffectType(EEffect.MajesticWill))
			{
				EcsGameEffect effect = m_caster.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == EEffect.MajesticWill);

				if (effect != null)
					hitChance += (int)effect.Effectiveness * 5;
			}

			return hitChance;
		}

		/// <summary>
		/// Calculates chance of spell getting resisted
		/// </summary>
		/// <param name="target">the target of the spell</param>
		/// <returns>chance that spell will be resisted for specific target</returns>
		public virtual int CalculateSpellResistChance(GameLiving target)
		{
			if (HasPositiveEffect)
				return 0;

			if (m_spellLine.KeyName == GlobalSpellsLines.Item_Effects && m_spellItem != null)
			{
				if (Caster is GamePlayer playerCaster)
				{
					int itemSpellLevel = m_spellItem.Template.LevelRequirement > 0 ? m_spellItem.Template.LevelRequirement : Math.Min(playerCaster.MaxLevel, m_spellItem.Level);
					return 100 - (85 + (itemSpellLevel - target.Level) / 2);
				}
			}

			return 100 - CalculateToHitChance(target);
		}

		public virtual bool CheckSpellResist(GameLiving target)
		{
			int spellResistChance = CalculateSpellResistChance(target);

			if (spellResistChance > 0)
			{
				int spellResistRoll;
				
				if (!Properties.OVERRIDE_DECK_RNG && Caster is GamePlayer player)
					spellResistRoll = player.RandomNumberDeck.GetInt();
				else
					spellResistRoll = Util.CryptoNextInt(100);

				if (Caster is GamePlayer playerCaster && playerCaster.UseDetailedCombatLog)
					playerCaster.Out.SendMessage($"Target chance to resist: {spellResistChance} RandomNumber: {spellResistRoll}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

				if (target is GamePlayer playerTarget && playerTarget.UseDetailedCombatLog)
					playerTarget.Out.SendMessage($"Your chance to resist: {spellResistChance} RandomNumber: {spellResistRoll}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

				if (spellResistChance > spellResistRoll)
				{
					OnSpellResisted(target);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// When spell was resisted
		/// </summary>
		/// <param name="target">the target that resisted the spell</param>
		protected virtual void OnSpellResisted(GameLiving target)
		{
			SendSpellResistAnimation(target);
			SendSpellResistMessages(target);
			SendSpellResistNotification(target);
			StartSpellResistInterruptTimer(target);
			StartSpellResistLastAttackTimer(target);

			// Treat resists as attacks to trigger an immediate response and BAF
			if (target is GameNpc)
			{
				if (Caster.Realm == 0 || target.Realm == 0)
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
		}

		/// <summary>
		/// Send Spell Resisted Animation
		/// </summary>
		public virtual void SendSpellResistAnimation(GameLiving target)
		{
			if (Spell.Pulse == 0 || !HasPositiveEffect)
				SendEffectAnimation(target, 0, false, 0);
		}
		
		/// <summary>
		/// Send Spell Resist Messages to Caster and Target
		/// </summary>
		public virtual void SendSpellResistMessages(GameLiving target)
		{
			// Deliver message to the target, if the target is a pet, to its owner instead.
			if (target is GameNpc npcTarget)
			{
				if (npcTarget.Brain is IControlledBrain npcTargetBrain)
				{
					GamePlayer owner = npcTargetBrain.GetPlayerOwner();

					if (owner != null)
						this.MessageToLiving(owner, EChatType.CT_SpellResisted, "Your {0} resists the effect!", target.Name);
				}
			}
			else
				MessageToLiving(target, "You resist the effect!", EChatType.CT_SpellResisted);

			// Deliver message to the caster as well.
			this.MessageToCaster(EChatType.CT_SpellResisted, "{0} resists the effect!" + " (" + CalculateSpellResistChance(target).ToString("0.0") + "%)", target.GetName(0, true));
		}
		
		/// <summary>
		/// Send Spell Attack Data Notification to Target when Spell is Resisted
		/// </summary>
		public virtual void SendSpellResistNotification(GameLiving target)
		{
			// Report resisted spell attack data to any type of living object, no need
			// to decide here what to do. For example, NPCs will use their brain.
			// "Just the facts, ma'am, just the facts."
			AttackData ad = new AttackData();
			ad.Attacker = Caster;
			ad.Target = target;
			ad.AttackType = EAttackType.Spell;
			ad.SpellHandler = this;
			ad.AttackResult = EAttackResult.Missed;
			ad.IsSpellResisted = true;
			target.OnAttackedByEnemy(ad);
			Caster.OnAttackEnemy(ad);
		}
		
		/// <summary>
		/// Start Spell Interrupt Timer when Spell is Resisted
		/// </summary>
		public virtual void StartSpellResistInterruptTimer(GameLiving target)
		{
			// Spells that would have caused damage or are not instant will still
			// interrupt a casting player.
			if(!(Spell.SpellType.ToString().IndexOf("debuff", StringComparison.OrdinalIgnoreCase) >= 0 && Spell.CastTime == 0))
				target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);			
		}
		
		/// <summary>
		/// Start Last Attack Timer when Spell is Resisted
		/// </summary>
		public virtual void StartSpellResistLastAttackTimer(GameLiving target)
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
		
		#region messages

		/// <summary>
		/// Sends a message to the caster, if the caster is a controlled
		/// creature, to the player instead (only spell hit and resisted
		/// messages).
		/// </summary>
		public void MessageToCaster(string message, EChatType type)
		{
			if (Caster is GamePlayer playerCaster)
				playerCaster.MessageToSelf(message, type);
			else if (Caster is GameNpc npcCaster && npcCaster.Brain is IControlledBrain npcCasterBrain
					 && (type is EChatType.CT_YouHit or EChatType.CT_SpellResisted or EChatType.CT_Spell))
			{
				GamePlayer playerOwner = npcCasterBrain.GetPlayerOwner();
				if (npcCasterBrain.GetPlayerOwner() != null)
					playerOwner.MessageToSelf(message, type);
			}
		}

		/// <summary>
		/// sends a message to a living
		/// </summary>
		public void MessageToLiving(GameLiving living, string message, EChatType type)
		{
			if (message != null && message.Length > 0)
			{
				living.MessageToSelf(message, type);
			}
		}

		/// <summary>
		/// Hold events for focus spells
		/// </summary>
		public virtual void FocusSpellAction(bool moving = false)
		{
			CastState = ECastState.Cleanup;

			Caster.ActivePulseSpells.TryRemove(m_spell.SpellType, out Spell _);

			if (moving)
				MessageToCaster("You move and interrupt your focus!", EChatType.CT_Important);
			else
				MessageToCaster($"You lose your focus on your {Spell.Name} spell.", EChatType.CT_SpellExpires);
		}

		#endregion

		/// <summary>
		/// Ability to cast a spell
		/// </summary>
		public ISpellCastingAbilityHandler Ability
		{
			get { return m_ability; }
			set { m_ability = value; }
		}
		/// <summary>
		/// The Spell
		/// </summary>
		public Spell Spell
		{
			get { return m_spell; }
		}

		/// <summary>
		/// The Spell Line
		/// </summary>
		public SpellLine SpellLine
		{
			get { return m_spellLine; }
		}

		/// <summary>
		/// The Caster
		/// </summary>
		public GameLiving Caster
		{
			get { return m_caster; }
		}

		/// <summary>
		/// Is the spell being cast?
		/// </summary>
		public bool IsInCastingPhase
		{
			get { return CastState == ECastState.Casting; }//return m_castTimer != null && m_castTimer.IsAlive; }
		}

		/// <summary>
		/// Does the spell have a positive effect?
		/// </summary>
		public virtual bool HasPositiveEffect
		{
			get { return m_spell.IsHelpful; }
		}

		/// <summary>
		/// Is this Spell purgeable
		/// </summary>
		public virtual bool IsUnPurgeAble
		{
			get { return false; }
		}

		/// <summary>
		/// Current depth of delve info
		/// </summary>
		public byte DelveInfoDepth
		{
			get { return m_delveInfoDepth; }
			set { m_delveInfoDepth = value; }
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public virtual IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>(32);
				//list.Add("Function: " + (Spell.SpellType == "" ? "(not implemented)" : Spell.SpellType));
				//list.Add(" "); //empty line
				GamePlayer p = null;

				if (Caster is GamePlayer || Caster is GameNpc && (Caster as GameNpc).Brain is IControlledBrain &&
				((Caster as GameNpc).Brain as IControlledBrain).GetPlayerOwner() != null)
				{
					p = Caster is GamePlayer ? (Caster as GamePlayer) : ((Caster as GameNpc).Brain as IControlledBrain).GetPlayerOwner();
				}
				list.Add(Spell.Description);
				list.Add(" "); //empty line
				if (Spell.InstrumentRequirement != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));
				if (Spell.Damage != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
				if (Spell.LifeDrainReturn != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.HealthReturned", Spell.LifeDrainReturn) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.HealthReturned", Spell.LifeDrainReturn));
				else if (Spell.Value != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Value", Spell.Value.ToString("0.###;0.###'%'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Value", Spell.Value.ToString("0.###;0.###'%'")));
				list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Target", Spell.Target) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Target", Spell.Target));
				if (Spell.Range != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Range", Spell.Range) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Range", Spell.Range));
				if (Spell.Duration >= ushort.MaxValue * 1000)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Duration") + " Permanent." : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " Permanent.");
				else if (Spell.Duration > 60000)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Duration") + " " + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min" : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " " + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min");
				else if (Spell.Duration != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Duration") + " " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'") : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
				if (Spell.Frequency != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
				if (Spell.Power != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
				list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
				if (Spell.RecastDelay > 60000)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min" : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
				else if (Spell.RecastDelay > 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 1000).ToString() + " sec" : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 1000).ToString() + " sec");
				if (Spell.Concentration != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.ConcentrationCost", Spell.Concentration) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.ConcentrationCost", Spell.Concentration));
				if (Spell.Radius != 0)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Radius", Spell.Radius) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Radius", Spell.Radius));
				if (Spell.DamageType != EDamageType.Natural)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)) : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));
				if (Spell.IsFocus)
					list.Add(p != null ? LanguageMgr.GetTranslation(p.Client, "DelveInfo.Focus") : LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Focus"));

				return list;
			}
		}
		// warlock add
		public static GameSpellEffect FindEffectOnTarget(GameLiving target, string spellType, string spellName)
		{
			lock (target.EffectList)
			{
				foreach (IGameEffect fx in target.EffectList)
				{
					if (!(fx is GameSpellEffect))
						continue;
					GameSpellEffect effect = (GameSpellEffect)fx;
					if (fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
						continue; // ignore immunity effects

					if (effect.SpellHandler.Spell != null && (effect.SpellHandler.Spell.SpellType.ToString() == spellType) && (effect.SpellHandler.Spell.Name == spellName))
					{
						return effect;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// Find effect by spell type
		/// </summary>
		/// <param name="target"></param>
		/// <param name="spellType"></param>
		/// <returns>first occurance of effect in target's effect list or null</returns>
		public static GameSpellEffect FindEffectOnTarget(GameLiving target, string spellType)
		{
			if (target == null)
				return null;

			lock (target.EffectList)
			{
				foreach (IGameEffect fx in target.EffectList)
				{
					if (!(fx is GameSpellEffect))
						continue;
					GameSpellEffect effect = (GameSpellEffect)fx;
					if (fx is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)fx).ImmunityState)
						continue; // ignore immunity effects
					if (effect.SpellHandler.Spell != null && (effect.SpellHandler.Spell.SpellType.ToString() == spellType))
					{
						return effect;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Find effect by spell handler
		/// </summary>
		/// <param name="target"></param>
		/// <param name="spellHandler"></param>
		/// <returns>first occurance of effect in target's effect list or null</returns>
		public static GameSpellEffect FindEffectOnTarget(GameLiving target, ISpellHandler spellHandler)
		{
			lock (target.EffectList)
			{
				foreach (IGameEffect effect in target.EffectList)
				{
					GameSpellEffect gsp = effect as GameSpellEffect;
					if (gsp == null)
						continue;
					if (gsp.SpellHandler != spellHandler)
						continue;
					if (gsp is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)gsp).ImmunityState)
						continue; // ignore immunity effects
					return gsp;
				}
			}
			return null;
		}

		/// <summary>
		/// Find effect by spell handler
		/// </summary>
		/// <param name="target"></param>
		/// <param name="spellHandler"></param>
		/// <returns>first occurance of effect in target's effect list or null</returns>
		public static GameSpellEffect FindEffectOnTarget(GameLiving target, Type spellHandler)
		{
			if (spellHandler.IsInstanceOfType(typeof(SpellHandler)) == false)
				return null;

			lock (target.EffectList)
			{
				foreach (IGameEffect effect in target.EffectList)
				{
					GameSpellEffect gsp = effect as GameSpellEffect;
					if (gsp == null)
						continue;
					if (gsp.SpellHandler.GetType().IsInstanceOfType(spellHandler) == false)
						continue;
					if (gsp is GameSpellAndImmunityEffect && ((GameSpellAndImmunityEffect)gsp).ImmunityState)
						continue; // ignore immunity effects
					return gsp;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns true if the target has the given static effect, false
		/// otherwise.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="effectType"></param>
		/// <returns></returns>
		public static IGameEffect FindStaticEffectOnTarget(GameLiving target, Type effectType)
		{
			if (target == null)
				return null;

			lock (target.EffectList)
			{
				foreach (IGameEffect effect in target.EffectList)
					if (effect.GetType() == effectType)
						return effect;
			}
			return null;
		}

		/// <summary>
		/// Find pulsing spell by spell handler
		/// </summary>
		/// <param name="living"></param>
		/// <param name="handler"></param>
		/// <returns>first occurance of spellhandler in targets' conc list or null</returns>
		public static PulsingSpellEffect FindPulsingSpellOnTarget(GameLiving living, ISpellHandler handler)
		{
			lock (living.effectListComponent.ConcentrationEffectsLock)
			{
				foreach (IConcentrationEffect concEffect in living.effectListComponent.ConcentrationEffects)
				{
					PulsingSpellEffect pulsingSpell = concEffect as PulsingSpellEffect;
					if (pulsingSpell == null) continue;
					if (pulsingSpell.SpellHandler == handler)
						return pulsingSpell;
				}
				return null;
			}
		}

		#region various helpers

		/// <summary>
		/// Level mod for effect between target and caster if there is any
		/// </summary>
		/// <returns></returns>
		public virtual double GetLevelModFactor()
		{
			return 0.02;  // Live testing done Summer 2009 by Bluraven, Tolakram  Levels 40, 45, 50, 55, 60, 65, 70
		}

		/// <summary>
		/// Calculates min damage variance %
		/// </summary>
		/// <param name="target">spell target</param>
		/// <param name="min">returns min variance</param>
		/// <param name="max">returns max variance</param>
		public virtual void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			if (m_spellLine.KeyName == GlobalSpellsLines.Item_Effects)
			{
				min = .75;
				max = 1.0;
				return;
			}

			if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
			{
				if (UseMinVariance)
				{
					min = 1.0;
				}
				else
				{
					min = .75;
				}

				max = 1.0;

				return;
			}

			if (m_spellLine.KeyName == GlobalSpellsLines.Reserved_Spells)
			{
				min = max = 1.0;
				return;
			}

			if (m_spellLine.KeyName == GlobalSpellsLines.Mob_Spells)
			{
				min = .75;
				max = 1.0;
				return;
			}

			int speclevel = 1;

			if (m_caster is GameSummonedPet)
			{
				IControlledBrain brain = (m_caster as GameNpc).Brain as IControlledBrain;
				speclevel = brain.GetLivingOwner().Level;
			}
			else if (m_caster is GamePlayer)
			{
				speclevel = ((GamePlayer)m_caster).GetModifiedSpecLevel(m_spellLine.Spec);
			}
			
			/*
			 * June 21st 2022 - Fen: Removing a lot of DoL code that should not be here for 1.65 calculations.
			 *
			 * Vanesyra lays out variance calculations here: https://www.ignboards.com/threads/melee-speed-melee-and-style-damage-or-why-pure-grothrates-are-wrong.452406879/page-3
			 * Most importantly, variance should be .25 at its lowest, 1.0 at its max, and never exceed 1.0.
			 *
			 * Base DoL calculations were adding an extra 10-30% damage above 1.0, which has now been removed.
			 */
			min = .2;
			max = 1;
			
			if (target.Level > 0)
			{
				var varianceMod = (speclevel - 1) / (double) target.Level;
				if (varianceMod > 1) varianceMod = 1;
				min = varianceMod;
			}
			/*
			if (speclevel - 1 > target.Level)
			{
				double overspecBonus = (speclevel - 1 - target.Level) * 0.005;
				min += overspecBonus;
				max += overspecBonus;
				Console.WriteLine($"overspec bonus {overspecBonus}");
			}*/
			
			// add level mod
			if (m_caster is GamePlayer)
			{
				min += GetLevelModFactor() * (m_caster.Level - target.Level);
				max += GetLevelModFactor() * (m_caster.Level - target.Level);
			}
			else if (m_caster is GameNpc && ((GameNpc)m_caster).Brain is IControlledBrain)
			{
				//Get the root owner
				GameLiving owner = ((IControlledBrain)((GameNpc)m_caster).Brain).GetLivingOwner();
				if (owner != null)
				{
					min += GetLevelModFactor() * (owner.Level - target.Level);
					max += GetLevelModFactor() * (owner.Level - target.Level);
				}
			}

			if (max < 0.25)
				max = 0.25;
			if (min > max)
				min = max;
			if (min < .2)
				min = .2;
		}

		/// <summary>
		/// Player pet damage cap
		/// This simulates a player casting a baseline nuke with the capped damage near (but not exactly) that of the equivilent spell of the players level.
		/// This cap is not applied if the player is level 50
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual double CapPetSpellDamage(double damage, GamePlayer player)
		{
			double cappedDamage = damage;

			if (player.Level < 13)
			{
				cappedDamage = 4.1 * player.Level;
			}

			if (player.Level < 50)
			{
				cappedDamage = 3.8 * player.Level;
			}

			return Math.Min(damage, cappedDamage);
		}


		/// <summary>
		/// Put a calculated cap on NPC damage to solve a problem where an npc is given a high level spell but needs damage
		/// capped to the npc level.  This uses player spec nukes to calculate damage cap.
		/// NPC's level 50 and above are not capped
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual double CapNPCSpellDamage(double damage, GameNpc npc)
		{
			if (npc.Level < 50)
			{
				return Math.Min(damage, 4.7 * npc.Level);
			}

			return damage;
		}

		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		/// <returns></returns>
		public virtual double CalculateDamageBase(GameLiving target)
		{
			double spellDamage = Spell.Damage;
			GamePlayer player = Caster as GamePlayer;

			if (Spell.SpellType == ESpellType.Lifedrain)
				spellDamage *= (1 + Spell.LifeDrainReturn * .001);

			// For pets the stats of the owner have to be taken into account.

			if (Caster is GameNpc && ((Caster as GameNpc).Brain) is IControlledBrain)
			{
				player = (((Caster as GameNpc).Brain) as IControlledBrain).Owner as GamePlayer;
			}

			if (player != null)
			{
				if (Caster is GameSummonedPet pet)
				{
					// There is no reason to cap pet spell damage if it's being scaled anyway.
					if (ServerProperties.Properties.PET_SCALE_SPELL_MAX_LEVEL <= 0)
						spellDamage = CapPetSpellDamage(spellDamage, player);

					if (pet is NecromancerPet nPet)
					{
						/*
						int ownerIntMod = 125;
						if (pet.Owner is GamePlayer own) ownerIntMod += own.Intelligence;
						spellDamage *= ((nPet.GetModified(eProperty.Intelligence) + ownerIntMod) / 275.0);
						if (spellDamage < Spell.Damage) spellDamage = Spell.Damage;
*/
						
						if (pet.Owner is GamePlayer own)
						{
							//Delve * (acu/200+1) * (plusskillsfromitems/200+1) * (Relicbonus+1) * (mom+1) * (1 - enemyresist) 
							int manaStatValue = own.GetModified((EProperty)own.PlayerClass.ManaStat);
							//spellDamage *= ((manaStatValue - 50) / 275.0) + 1;
							spellDamage *= ((manaStatValue - own.Level) * 0.005) + 1;
						}
						
					}
					else
					{
						int ownerIntMod = 125;
						if (pet.Owner is GamePlayer own) ownerIntMod += own.Intelligence / 2;
						spellDamage *= ((pet.Intelligence + ownerIntMod ) / 275.0);
					}
						
					
					int modSkill = pet.Owner.GetModifiedSpecLevel(m_spellLine.Spec) -
								   pet.Owner.GetBaseSpecLevel(m_spellLine.Spec);
					spellDamage *= 1 + (modSkill * .005);
				}
				else if (SpellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
				{
					double weaponskillScalar = (3 + .02 * player.GetWeaponStat(player.ActiveWeapon)) /
											   (1 + .005 * player.GetWeaponStat(player.ActiveWeapon));
					spellDamage *= (player.GetWeaponSkill(player.ActiveWeapon) * weaponskillScalar /3 + 100) / 200;
				}
				else if (player.PlayerClass.ManaStat != EStat.UNDEFINED
					&& SpellLine.KeyName != GlobalSpellsLines.Combat_Styles_Effect
					&& m_spellLine.KeyName != GlobalSpellsLines.Mundane_Poisons
					&& SpellLine.KeyName != GlobalSpellsLines.Item_Effects
					&& player.PlayerClass.ID != (int)EPlayerClass.MaulerAlb
					&& player.PlayerClass.ID != (int)EPlayerClass.MaulerMid
					&& player.PlayerClass.ID != (int)EPlayerClass.MaulerHib
					&& player.PlayerClass.ID != (int)EPlayerClass.Vampiir)
				{
					//Delve * (acu/200+1) * (plusskillsfromitems/200+1) * (Relicbonus+1) * (mom+1) * (1 - enemyresist) 
					int manaStatValue = player.GetModified((EProperty)player.PlayerClass.ManaStat);
					//spellDamage *= ((manaStatValue - 50) / 275.0) + 1;
					spellDamage *= ((manaStatValue - player.Level) * 0.005) + 1;
					int modSkill = player.GetModifiedSpecLevel(m_spellLine.Spec) -
								   player.GetBaseSpecLevel(m_spellLine.Spec);
					spellDamage *= 1 + (modSkill * .005);

					//list casters get a little extra sauce
					if ((EPlayerClass) player.PlayerClass.ID is EPlayerClass.Wizard
						or EPlayerClass.Theurgist
						or EPlayerClass.Cabalist or EPlayerClass.Sorcerer or EPlayerClass.Necromancer
						or EPlayerClass.Eldritch or EPlayerClass.Enchanter or EPlayerClass.Mentalist
						or EPlayerClass.Animist or EPlayerClass.Valewalker
						or EPlayerClass.Runemaster or EPlayerClass.Spiritmaster or EPlayerClass.Bonedancer)
					{
						spellDamage *= 1.10;
					}
					
					if (spellDamage < Spell.Damage) spellDamage = Spell.Damage;
				}
			}
			else if (Caster is GameNpc)
			{
				var npc = (GameNpc) Caster;
				int manaStatValue = npc.GetModified(EProperty.Intelligence);
				spellDamage = CapNPCSpellDamage(spellDamage, npc)*(manaStatValue + 200)/275.0;
			}

			if (spellDamage < 0)
				spellDamage = 0;

			return spellDamage;
		}

		/// <summary>
		/// Adjust damage based on chance to hit.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="hitChance"></param>
		/// <returns></returns>
		public virtual int AdjustDamageForHitChance(int damage, int hitChance)
		{
			int adjustedDamage = damage;

			if (hitChance < 55)
				adjustedDamage += (int) (adjustedDamage * (hitChance - 55) * Properties.SPELL_HITCHANCE_DAMAGE_REDUCTION_MULTIPLIER * 0.01);

			return Math.Max(adjustedDamage, 1);
		}

		public virtual AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = new()
			{
				Attacker = m_caster,
				Target = target,
				AttackType = EAttackType.Spell,
				SpellHandler = this,
				AttackResult = EAttackResult.HitUnstyled
			};

			CalculateDamageVariance(target, out double minVariance, out double maxVariance);
			double spellDamage = CalculateDamageBase(target);
			GamePlayer playerCaster = m_caster is GameSummonedPet pet ? pet.Owner as GamePlayer : m_caster as GamePlayer;
			double effectiveness = Effectiveness;

			if (playerCaster != null)
			{
				// Relic bonus applied to damage, does not alter effectiveness or increase cap
				spellDamage *= 1.0 + RelicMgr.GetRelicBonusModifier(playerCaster.Realm, ERelicType.Magic);
				effectiveness *= 1 + playerCaster.GetModified(EProperty.SpellDamage) * 0.01;
			}

			spellDamage *= effectiveness;

			if (_distanceFallOff > 0)
				spellDamage *= 1 - _distanceFallOff;

			int finalDamage = Util.Random((int)(minVariance * spellDamage), (int)(maxVariance * spellDamage));

			// Live testing done Summer 2009 by Bluraven, Tolakram. Levels 40, 45, 50, 55, 60, 65, 70.
			// Damage reduced by chance < 55, no extra damage increase noted with hitchance > 100.
			int hitChance = CalculateToHitChance(ad.Target);
			finalDamage = AdjustDamageForHitChance(finalDamage, hitChance);

			if (m_caster is GamePlayer || (m_caster is GameNpc && (m_caster as GameNpc).Brain is IControlledBrain && m_caster.Realm != 0))
			{
				if (target is GamePlayer)
					finalDamage = (int) (finalDamage * Properties.PVP_SPELL_DAMAGE);
				else if (target is GameNpc)
					finalDamage = (int) (finalDamage * Properties.PVE_SPELL_DAMAGE);
			}

			// Calculate resistances and conversion.
			finalDamage = ModifyDamageWithTargetResist(ad, finalDamage);
			double conversionMod = AttackComponent.CalculateTargetConversion(ad.Target, finalDamage);
			int preConversionDamage = finalDamage;
			finalDamage = (int) (finalDamage * conversionMod);
			ad.Modifier += finalDamage - preConversionDamage;

			// Apply damage cap.
			if (finalDamage > DamageCap(effectiveness))
				finalDamage = (int) DamageCap(effectiveness);

			// Apply conversion.
			if (conversionMod < 1)
			{
				double conversionAmount = conversionMod > 0 ? finalDamage / conversionMod - finalDamage : finalDamage;
				AttackComponent.ApplyTargetConversionRegen(ad.Target, (int) conversionAmount);
			}

			if (finalDamage < 0)
				finalDamage = 0;

			// DoTs can only crit with Wild Arcana. This is handled by the DoTSpellHandler directly.
			int criticalChance = this is not DamageOverTimeSpell ? m_caster.SpellCriticalChance : 0;
			int criticalDamage = 0;
			int randNum = Util.CryptoNextInt(0, 100);
			int criticalCap = Math.Min(50, criticalChance);

			if (Caster is GamePlayer spellCaster && spellCaster.UseDetailedCombatLog && criticalCap > 0)
				spellCaster.Out.SendMessage($"spell crit chance: {criticalCap} random: {randNum}", EChatType.CT_DamageAdd, EChatLoc.CL_SystemWindow);

			if (criticalCap > randNum && finalDamage > 0)
			{
				int criticalMax = (ad.Target is GamePlayer) ? finalDamage / 2 : finalDamage;
				criticalDamage = Util.Random(finalDamage / 10, criticalMax);
			}

			ad.Damage = finalDamage;
			ad.CriticalDamage = criticalDamage;
			ad.Target.ModifyAttack(ad); // Attacked living may modify the attack data. Primarily used for keep doors and components.
			m_lastAttackData = ad;
			return ad;
		}

		public virtual int ModifyDamageWithTargetResist(AttackData ad, int damage)
		{
			// Since 1.65 there are different categories of resist.
			// - First category contains Item / Race/ Buff / RvrBanners resists.
			// - Second category contains resists that are obtained from RAs such as Avoidance of Magic and Brilliant Aura of Deflection.
			// However the second category affects ONLY the spell damage. Not the duration, not the effectiveness of debuffs.
			// For spell damage, the calculation is 'finaldamage * firstCategory * secondCategory'.
			// -> Remark for the future: VampirResistBuff is Category2 too.

			EDamageType damageType = DetermineSpellDamageType();
			EProperty property = ad.Target.GetResistTypeForDamage(damageType);
			int primaryResistModifier = ad.Target.GetResist(damageType);
			int secondaryResistModifier = Math.Min(80, ad.Target.SpecBuffBonusCategory[(int) property]);

			// Resist Pierce is a special bonus which has been introduced with ToA.
			// It reduces the resistance that the victim receives through items by the specified percentage.
			// http://de.daocpedia.eu/index.php/Resistenz_durchdringen (translated)
			int resitPierce = Caster.GetModified(EProperty.ResistPierce);

			// Substract max ItemBonus of property of target, but at least 0.
			if (resitPierce > 0 && Spell.SpellType != ESpellType.Archery)
				primaryResistModifier -= Math.Max(0, Math.Min(ad.Target.ItemBonus[(int) property], resitPierce));

			int resistModifier = 0;
			resistModifier += (int)(damage * (double) primaryResistModifier * -0.01);
			resistModifier += (int)((damage + (double) resistModifier) * secondaryResistModifier * -0.01);
			damage += resistModifier;

			// Update AttackData.
			ad.Modifier = resistModifier;
			ad.DamageType = damageType;

			return damage;
		}

		public virtual double DamageCap(double effectiveness)
		{
			return Spell.Damage * 3.0 * effectiveness;
		}

		/// <summary>
		/// What damage type to use.  Overriden by archery
		/// </summary>
		/// <returns></returns>
		public virtual EDamageType DetermineSpellDamageType()
		{
			return Spell.DamageType;
		}

		/// <summary>
		/// Sends damage text messages but makes no damage
		/// </summary>
		/// <param name="ad"></param>
		public virtual void SendDamageMessages(AttackData ad)
		{
			string modmessage = "";
			if (ad.Modifier > 0)
				modmessage = " (+" + ad.Modifier + ")";
			if (ad.Modifier < 0)
				modmessage = " (" + ad.Modifier + ")";
			if (Caster is GamePlayer || Caster is NecromancerPet)
				MessageToCaster(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit);
			else if (Caster is GameNpc)
				MessageToCaster(string.Format("Your " + Caster.Name + " hits {0} for {1}{2} damage!",
											  ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit);
			if (ad.CriticalDamage > 0)
				MessageToCaster("You critically hit for an additional " + ad.CriticalDamage + " damage!" + " (" + m_caster.SpellCriticalChance + "%)", EChatType.CT_YouHit);
		}

		/// <summary>
		/// Make damage to target and send spell effect but no messages
		/// </summary>
		public virtual void DamageTarget(AttackData ad, bool showEffectAnimation)
		{
			DamageTarget(ad, showEffectAnimation, 0x14); // Spell damage attack result.
		}

		/// <summary>
		/// Make damage to target and send spell effect but no messages
		/// </summary>
		public virtual void DamageTarget(AttackData ad, bool showEffectAnimation, int attackResult)
		{
			ad.AttackResult = EAttackResult.HitUnstyled;

			// Send animation before dealing damage else dying livings show no animation
			if (showEffectAnimation)
				SendEffectAnimation(ad.Target, 0, false, 1);

			ad.Target.OnAttackedByEnemy(ad);
			ad.Attacker.DealDamage(ad);

			if (ad.Damage == 0 && ad.Target is GameNpc targetNpc)
			{
				if (targetNpc.Brain is IOldAggressiveBrain brain)
					brain.AddToAggroList(Caster, 1);

				if (this is not DamageOverTimeSpell and not StyleBleedingEffect)
				{
					if (Caster.Realm == 0 || ad.Target.Realm == 0)
					{
						ad.Target.LastAttackedByEnemyTickPvE = GameLoop.GameLoopTime;
						Caster.LastAttackTickPvE = GameLoop.GameLoopTime;
					}
					else
					{
						ad.Target.LastAttackedByEnemyTickPvP = GameLoop.GameLoopTime;
						Caster.LastAttackTickPvP = GameLoop.GameLoopTime;
					}
				}
			}

			if (ad.Damage > 0)
			{
				foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, (byte) attackResult, ad.Target.HealthPercent);
			}

			m_lastAttackData = ad;
		}

		#endregion

		#region saved effects
		public virtual DbPlayerXEffect GetSavedEffect(GameSpellEffect effect)
		{
			return null;
		}

		public virtual void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{ }

		public virtual int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			return 0;
		}
		#endregion
		
		#region tooltip handling
		/// <summary>
		/// Return the given Delve Writer with added keyvalue pairs.
		/// </summary>
		/// <param name="dw"></param>
		/// <param name="id"></param>
		public virtual void TooltipDelve(ref MiniDelveWriter dw)
		{
			if (dw == null)
				return;

			dw.AddKeyValuePair("Function", GetDelveType(Spell.SpellType));
			dw.AddKeyValuePair("Index", unchecked((ushort)Spell.InternalID));
			dw.AddKeyValuePair("Name", Spell.Name);
			
			if (Spell.CastTime > 2000)
				dw.AddKeyValuePair("cast_timer", Spell.CastTime - 2000); //minus 2 seconds (why mythic?)
			else if (!Spell.IsInstantCast)
				dw.AddKeyValuePair("cast_timer", 0); //minus 2 seconds (why mythic?)
			
			if (Spell.IsInstantCast)
				dw.AddKeyValuePair("instant","1");
			if ((int)Spell.DamageType > 0)
			{
				//Added to fix the mis-match between client and server
				int addTo;
				switch ((int)Spell.DamageType)
				{
					case 10:
						addTo = 6;
						break;
					case 12:
						addTo = 10;
						break;
					case 15:
						addTo = 2;
						break;
					default:
						addTo = 1;
						break;
				}
				dw.AddKeyValuePair("damage_type", (int)Spell.DamageType + addTo); // Damagetype not the same as dol
			}
			if (Spell.Level > 0)
			{
				dw.AddKeyValuePair("level", Spell.Level);
				dw.AddKeyValuePair("power_level", Spell.Level);
			}
			if (Spell.CostPower)
				dw.AddKeyValuePair("power_cost", Spell.Power);
			if (Spell.Range > 0)
				dw.AddKeyValuePair("range", Spell.Range);
			if (Spell.Duration > 0)
				dw.AddKeyValuePair("duration", Spell.Duration / 1000); //seconds
			if (GetDurationType() > 0)
				dw.AddKeyValuePair("dur_type", GetDurationType());
			if (Spell.HasRecastDelay)
				dw.AddKeyValuePair("timer_value", Spell.RecastDelay / 1000);
			
			if (GetSpellTargetType() > 0)
				dw.AddKeyValuePair("target", GetSpellTargetType());

			//if (!string.IsNullOrEmpty(Spell.Description))
			//	dw.AddKeyValuePair("description_string", Spell.Description);

			if (Spell.IsAoE)
				dw.AddKeyValuePair("radius", Spell.Radius);
			if (Spell.IsConcentration)
				dw.AddKeyValuePair("concentration_points", Spell.Concentration);
			if (Spell.Frequency > 0)
				dw.AddKeyValuePair("frequency", Spell.SpellType == ESpellType.OffensiveProc || Spell.SpellType == ESpellType.OffensiveProc ? Spell.Frequency / 100 : Spell.Frequency);

			WriteBonus(ref dw);
			WriteParm(ref dw);
			WriteDamage(ref dw);
			WriteSpecial(ref dw);

			if (Spell.HasSubSpell)
				if (Spell.SpellType == ESpellType.Bomber || Spell.SpellType == ESpellType.SummonAnimistFnF)
					dw.AddKeyValuePair("delve_spell", SkillBase.GetSpellByID(Spell.SubSpellID).InternalID);
				else
					dw.AddKeyValuePair("parm", SkillBase.GetSpellByID(Spell.SubSpellID).InternalID);

			if (!dw.Values.ContainsKey("parm") && Spell.SpellType != ESpellType.MesmerizeDurationBuff)
				dw.AddKeyValuePair("parm", "1");
		}

		private string GetDelveType(ESpellType spellType)
		{
			switch (spellType)
			{
				case ESpellType.AblativeArmor:
					return "hit_buffer";
				case ESpellType.AcuityBuff:
				case ESpellType.DexterityQuicknessBuff:
				case ESpellType.StrengthConstitutionBuff:
					return "twostat";
				case ESpellType.Amnesia:
					return "amnesia";
				case ESpellType.ArmorAbsorptionBuff:
					return "absorb";
				case ESpellType.ArmorAbsorptionDebuff:
					return "nabsorb";
				case ESpellType.ArmorFactorBuff:
				case ESpellType.PaladinArmorFactorBuff:
					return "shield";
				case ESpellType.Bolt:
					return "bolt";
				case ESpellType.Bladeturn:
				case ESpellType.CelerityBuff:
				case ESpellType.CombatSpeedBuff:
				case ESpellType.CombatSpeedDebuff:
				case ESpellType.Confusion:
				case ESpellType.Mesmerize:
				case ESpellType.Mez:
				case ESpellType.Nearsight:
				case ESpellType.SavageCombatSpeedBuff:
				case ESpellType.SavageEvadeBuff:
				case ESpellType.SavageParryBuff:
				case ESpellType.SpeedEnhancement:
					return "combat";
				case ESpellType.BodyResistBuff:
				case ESpellType.BodySpiritEnergyBuff:
				case ESpellType.ColdResistBuff:
				case ESpellType.EnergyResistBuff:
				case ESpellType.HeatColdMatterBuff:
				case ESpellType.HeatResistBuff:
				case ESpellType.MatterResistBuff:
				case ESpellType.SavageCrushResistanceBuff:
				case ESpellType.SavageSlashResistanceBuff:
				case ESpellType.SavageThrustResistanceBuff:
				case ESpellType.SpiritResistBuff:
					return "resistance";
				case ESpellType.BodyResistDebuff:
				case ESpellType.ColdResistDebuff:
				case ESpellType.EnergyResistDebuff:
				case ESpellType.HeatResistDebuff:
				case ESpellType.MatterResistDebuff:
				case ESpellType.SpiritResistDebuff:
					return "nresistance";
				case ESpellType.SummonTheurgistPet:
				case ESpellType.Bomber:
				case ESpellType.SummonAnimistFnF:
					return "dsummon";
				case ESpellType.Charm:
					return "charm";
				case ESpellType.CombatHeal:
				case ESpellType.Heal:
					return "heal";
				case ESpellType.ConstitutionBuff:
				case ESpellType.DexterityBuff:
				case ESpellType.StrengthBuff:
				case ESpellType.AllStatsBarrel:
					return "stat";
				case ESpellType.ConstitutionDebuff:
				case ESpellType.DexterityDebuff:
				case ESpellType.StrengthDebuff:
					return "nstat";
				case ESpellType.CureDisease:
				case ESpellType.CurePoison:
				case ESpellType.CureNearsightCustom:
					return "rem_eff_ty";
				case ESpellType.CureMezz:
					return "remove_eff";
				case ESpellType.DamageAdd:
					return "dmg_add";
				case ESpellType.DamageOverTime:
				case ESpellType.StyleBleeding:
					return "dot";
				case ESpellType.DamageShield:
					return "dmg_shield";
				case ESpellType.DamageSpeedDecrease:
				case ESpellType.SpeedDecrease:
				case ESpellType.UnbreakableSpeedDecrease:
					return "snare";
				case ESpellType.DefensiveProc:
					return "def_proc";
				case ESpellType.DexterityQuicknessDebuff:
				case ESpellType.StrengthConstitutionDebuff:
					return "ntwostat";
				case ESpellType.DirectDamage:
					return "direct";
				case ESpellType.DirectDamageWithDebuff:
					return "nresist_dam";
				case ESpellType.Disease:
					return "disease";
				case ESpellType.EnduranceRegenBuff:
				case ESpellType.HealthRegenBuff:
				case ESpellType.PowerRegenBuff:
					return "enhancement";
				case ESpellType.HealOverTime:
					return "regen";
				case ESpellType.Lifedrain:
					return "lifedrain";
				case ESpellType.LifeTransfer:
					return "transfer";
				case ESpellType.MeleeDamageDebuff:
					return "ndamage";
				case ESpellType.MesmerizeDurationBuff:
					return "mez_dampen";
				case ESpellType.OffensiveProc:
					return "off_proc";
				case ESpellType.PetConversion:
					return "reclaim";
				case ESpellType.Resurrect:
					return "raise_dead";
				case ESpellType.SavageEnduranceHeal:
					return "fat_heal";
				case ESpellType.SpreadHeal:
					return "spreadheal";
				case ESpellType.Stun:
					return "paralyze";				
				case ESpellType.SummonCommander:
				case ESpellType.SummonDruidPet:
				case ESpellType.SummonHunterPet:
				case ESpellType.SummonSimulacrum:
				case ESpellType.SummonSpiritFighter:
				case ESpellType.SummonUnderhill:
					return "summon";
				case ESpellType.SummonMinion:
					return "gsummon";
				case ESpellType.SummonNecroPet:
					return "ssummon";
				case ESpellType.StyleCombatSpeedDebuff:
				case ESpellType.StyleStun:
				case ESpellType.StyleSpeedDecrease:				
					return "add_effect";
				case ESpellType.StyleTaunt:
					if (Spell.Value > 0)
						return "taunt";
					else
						return "detaunt";
				case ESpellType.Taunt:
					return "taunt";
				case ESpellType.PetSpell:
				case ESpellType.SummonAnimistPet:
					return "petcast";
				case ESpellType.PetLifedrain:
					return "lifedrain";
				case ESpellType.PowerDrainPet:
					return "powerdrain";
				case ESpellType.PowerTransferPet:
					return "power_xfer";
				case ESpellType.ArmorFactorDebuff:
					return "nshield";
				case ESpellType.Grapple:
					return "Grapple";
				default:
					return "light";

			}
		}

		private void WriteBonus(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case ESpellType.AblativeArmor:
					dw.AddKeyValuePair("bonus", Spell.Damage > 0 ? Spell.Damage : 25);
					break;
				case ESpellType.AcuityBuff:
				case ESpellType.ArmorAbsorptionBuff:
				case ESpellType.ArmorAbsorptionDebuff:
				case ESpellType.ArmorFactorBuff:
				case ESpellType.BodyResistBuff:
				case ESpellType.BodyResistDebuff:
				case ESpellType.BodySpiritEnergyBuff:
				case ESpellType.ColdResistBuff:
				case ESpellType.ColdResistDebuff:
				case ESpellType.CombatSpeedBuff:
				case ESpellType.CelerityBuff:
				case ESpellType.ConstitutionBuff:
				case ESpellType.ConstitutionDebuff:
				case ESpellType.DexterityBuff:
				case ESpellType.DexterityDebuff:
				case ESpellType.DexterityQuicknessBuff:
				case ESpellType.DexterityQuicknessDebuff:
				case ESpellType.DirectDamageWithDebuff:
				case ESpellType.EnergyResistBuff:
				case ESpellType.EnergyResistDebuff:
				case ESpellType.HealOverTime:
				case ESpellType.HeatColdMatterBuff:
				case ESpellType.HeatResistBuff:
				case ESpellType.HeatResistDebuff:
				case ESpellType.MatterResistBuff:
				case ESpellType.MatterResistDebuff:
				case ESpellType.MeleeDamageBuff:
				case ESpellType.MeleeDamageDebuff:
				case ESpellType.MesmerizeDurationBuff:
				case ESpellType.PaladinArmorFactorBuff:
				case ESpellType.PetConversion:
				case ESpellType.SavageCombatSpeedBuff:
				case ESpellType.SavageCrushResistanceBuff:
				case ESpellType.SavageDPSBuff:
				case ESpellType.SavageEvadeBuff:
				case ESpellType.SavageParryBuff:
				case ESpellType.SavageSlashResistanceBuff:
				case ESpellType.SavageThrustResistanceBuff:
				case ESpellType.SpeedEnhancement:
				case ESpellType.SpeedOfTheRealm:
				case ESpellType.SpiritResistBuff:
				case ESpellType.SpiritResistDebuff:
				case ESpellType.StrengthBuff:
				case ESpellType.AllStatsBarrel:
				case ESpellType.StrengthConstitutionBuff:
				case ESpellType.StrengthConstitutionDebuff:
				case ESpellType.StrengthDebuff:
				case ESpellType.ToHitBuff:
				case ESpellType.FumbleChanceDebuff:
				case ESpellType.AllStatsPercentDebuff:
				case ESpellType.CrushSlashThrustDebuff:
				case ESpellType.EffectivenessDebuff:
				case ESpellType.ParryBuff:
				case ESpellType.SavageEnduranceHeal:
				case ESpellType.SlashResistDebuff:
				case ESpellType.ArmorFactorDebuff:
				case ESpellType.WeaponSkillBuff:
				case ESpellType.FlexibleSkillBuff:
					dw.AddKeyValuePair("bonus", Spell.Value);
					break;
				case ESpellType.DamageSpeedDecrease:
				case ESpellType.SpeedDecrease:
				case ESpellType.StyleSpeedDecrease:
				case ESpellType.UnbreakableSpeedDecrease:
					dw.AddKeyValuePair("bonus", 100 - Spell.Value);
					break;
				case ESpellType.DefensiveProc:
				case ESpellType.OffensiveProc:
					dw.AddKeyValuePair("bonus", Spell.Frequency / 100);
					break;
				case ESpellType.Lifedrain:
				case ESpellType.PetLifedrain:
					dw.AddKeyValuePair("bonus", Spell.LifeDrainReturn / 10);
					break;
				case ESpellType.PowerDrainPet:
					dw.AddKeyValuePair("bonus", Spell.LifeDrainReturn);
					break;
				case ESpellType.Resurrect:
					dw.AddKeyValuePair("bonus", Spell.ResurrectMana);
					break;
			}
		}

		private void WriteParm(ref MiniDelveWriter dw)
		{
			string parm = "parm";
			switch (Spell.SpellType)
			{
				case ESpellType.CombatSpeedDebuff:
				
				case ESpellType.DexterityBuff:
				case ESpellType.DexterityDebuff:
				case ESpellType.DexterityQuicknessBuff:
				case ESpellType.DexterityQuicknessDebuff:
				case ESpellType.PowerRegenBuff:
				case ESpellType.StyleCombatSpeedDebuff:
					dw.AddKeyValuePair(parm, "2");
					break;
				case ESpellType.AcuityBuff:
				case ESpellType.ConstitutionBuff:
				case ESpellType.AllStatsBarrel:
				case ESpellType.ConstitutionDebuff:
				case ESpellType.EnduranceRegenBuff:
					dw.AddKeyValuePair(parm, "3");
					break;
				case ESpellType.Confusion:
					dw.AddKeyValuePair(parm, "5");
					break;
				case ESpellType.CureMezz:
				case ESpellType.Mesmerize:
					dw.AddKeyValuePair(parm, "6");
					break;
				case ESpellType.Bladeturn:
					dw.AddKeyValuePair(parm, "9");
					break;
				case ESpellType.HeatResistBuff:
				case ESpellType.HeatResistDebuff:
				case ESpellType.SpeedEnhancement:
					dw.AddKeyValuePair(parm, "10");
					break;
				case ESpellType.ColdResistBuff:
				case ESpellType.ColdResistDebuff:
				case ESpellType.CurePoison:
				case ESpellType.Nearsight:
				case ESpellType.CureNearsightCustom:
					dw.AddKeyValuePair(parm, "12");
					break;
				case ESpellType.MatterResistBuff:
				case ESpellType.MatterResistDebuff:
				case ESpellType.SavageParryBuff:
					dw.AddKeyValuePair(parm, "15");
					break;
				case ESpellType.BodyResistBuff:
				case ESpellType.BodyResistDebuff:
				case ESpellType.SavageEvadeBuff:
					dw.AddKeyValuePair(parm, "16");
					break;
				case ESpellType.SpiritResistBuff:
				case ESpellType.SpiritResistDebuff:
					dw.AddKeyValuePair(parm, "17");
					break;
				case ESpellType.StyleBleeding:
					dw.AddKeyValuePair(parm, "20");
					break;
				case ESpellType.EnergyResistBuff:
				case ESpellType.EnergyResistDebuff:
					dw.AddKeyValuePair(parm, "22");
					break;
				case ESpellType.SpeedOfTheRealm:
					dw.AddKeyValuePair(parm, "35");
					break;
				case ESpellType.CelerityBuff:
				case ESpellType.SavageCombatSpeedBuff:
				case ESpellType.CombatSpeedBuff:
					dw.AddKeyValuePair(parm, "36");
					break;
				case ESpellType.HeatColdMatterBuff:
					dw.AddKeyValuePair(parm, "97");
					break;
				case ESpellType.BodySpiritEnergyBuff:
					dw.AddKeyValuePair(parm, "98");
					break;
				case ESpellType.DirectDamageWithDebuff:
					//Added to fix the mis-match between client and server
					int addTo;
					switch ((int)Spell.DamageType)
					{
						case 10:
							addTo = 6;
							break;
						case 12:
							addTo = 10;
							break;
						case 15:
							addTo = 2;
							break;
						default:
							addTo = 1;
							break;
					}
					dw.AddKeyValuePair(parm, (int)Spell.DamageType + addTo);
					break;
				case ESpellType.SavageCrushResistanceBuff:
					dw.AddKeyValuePair(parm, (int)EDamageType.Crush);
					break;
				case ESpellType.SavageSlashResistanceBuff:
					dw.AddKeyValuePair(parm, (int)EDamageType.Slash);
					break;
				case ESpellType.SavageThrustResistanceBuff:
					dw.AddKeyValuePair(parm, (int)EDamageType.Thrust);
					break;
				case ESpellType.DefensiveProc:
				case ESpellType.OffensiveProc:
					dw.AddKeyValuePair(parm, SkillBase.GetSpellByID((int)Spell.Value).InternalID);
					break;
			}
		}

		private void WriteDamage(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case ESpellType.AblativeArmor:
				case ESpellType.CombatHeal:
				case ESpellType.EnduranceRegenBuff:
				case ESpellType.Heal:
				case ESpellType.HealOverTime:
				case ESpellType.HealthRegenBuff:
				case ESpellType.LifeTransfer:
				case ESpellType.PowerRegenBuff:
				case ESpellType.SavageEnduranceHeal:
				case ESpellType.SpreadHeal:
				case ESpellType.Taunt:
					dw.AddKeyValuePair("damage", Spell.Value);
					break;
				case ESpellType.Bolt:
				case ESpellType.DamageAdd:
				case ESpellType.DamageShield:
				case ESpellType.DamageSpeedDecrease:
				case ESpellType.DirectDamage:
				case ESpellType.DirectDamageWithDebuff:
				case ESpellType.Lifedrain:
				case ESpellType.PetLifedrain:
				case ESpellType.PowerDrainPet:
					dw.AddKeyValuePair("damage", Spell.Damage * 10);
					break;
				case ESpellType.DamageOverTime:
				case ESpellType.StyleBleeding:
					dw.AddKeyValuePair("damage", Spell.Damage);
					break;
				case ESpellType.Resurrect:
					dw.AddKeyValuePair("damage", Spell.ResurrectHealth);
					break;
				case ESpellType.StyleTaunt:
					dw.AddKeyValuePair("damage", Spell.Value < 0 ? -Spell.Value : Spell.Value);
					break;
				case ESpellType.PowerTransferPet:
					dw.AddKeyValuePair("damage", Spell.Value * 10);
					break;
				case ESpellType.SummonHunterPet:
				case ESpellType.SummonSimulacrum:
				case ESpellType.SummonSpiritFighter:
				case ESpellType.SummonUnderhill:
					dw.AddKeyValuePair("damage", 44);
					break;
				case ESpellType.SummonCommander:
				case ESpellType.SummonDruidPet:
				case ESpellType.SummonMinion:
					dw.AddKeyValuePair("damage", Spell.Value);
					break;
			}
		}

		private void WriteSpecial(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case ESpellType.Bomber:
					//dw.AddKeyValuePair("description_string", "Summon an elemental sprit to fight for the caster briefly.");
						break;
				case ESpellType.Charm:
					dw.AddKeyValuePair("power_level", Spell.Value);

					// var baseMessage = "Attempts to bring the target monster under the caster's control.";
					switch ((CharmSpell.eCharmType)Spell.AmnesiaChance)
					{
						case CharmSpell.eCharmType.All:
							// Message: Attempts to bring the target monster under the caster's control. Spell works on all monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.AllMonsterTypes"));
							break;
						case CharmSpell.eCharmType.Animal:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Animal"));
							break;
						case CharmSpell.eCharmType.Humanoid:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on humanoids. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Humanoid"));
							break;
						case CharmSpell.eCharmType.Insect:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on insects. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Insect"));
							break;
						case CharmSpell.eCharmType.HumanoidAnimal:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals and humanoids. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimal"));
							break;
						case CharmSpell.eCharmType.HumanoidAnimalInsect:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, humanoids, insects, and reptiles. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsect"));
							break;
						case CharmSpell.eCharmType.HumanoidAnimalInsectMagical:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, elemental, humanoids, insects, magical, plant, and reptile monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagical"));
							break;
						case CharmSpell.eCharmType.HumanoidAnimalInsectMagicalUndead:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, elemental, humanoids, insects, magical, plant, reptile, and undead monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagicalUndead"));
							break;
					}
					break;
				case ESpellType.CombatSpeedBuff:
				case ESpellType.CelerityBuff:
					dw.AddKeyValuePair("power_level", Spell.Value * 2);
					break;

				case ESpellType.Confusion:
					dw.AddKeyValuePair("power_level", Spell.Value > 0 ? Spell.Value : 100);
					break;
				case ESpellType.CombatSpeedDebuff:
					dw.AddKeyValuePair("power_level", -Spell.Value);
					break;
				case ESpellType.CureMezz:
					dw.AddKeyValuePair("type1", "8");
					break;
				case ESpellType.Disease:
					dw.AddKeyValuePair("delve_string", "Inflicts a wasting disease on the target that slows target by 15 %, reduces strength by 7.5 % and inhibits healing by 50 %");
					break;
				//case eSpellType.DefensiveProc:
				//case eSpellType.OffensiveProc:
				//	dw.AddKeyValuePair("delve_spell", Spell.Value);
				//	break;
				case ESpellType.FatigueConsumptionBuff:
					dw.AddKeyValuePair("delve_string", $"The target's actions require {(int)Spell.Value}% less endurance.");
					break;
				case ESpellType.FatigueConsumptionDebuff:
					dw.AddKeyValuePair("delve_string", $"The target's actions require {(int)Spell.Value}% more endurance.");
					break;
				case ESpellType.MeleeDamageBuff:
					dw.AddKeyValuePair("delve_string", $"Increases your melee damage by {(int)Spell.Value}%.");
					break;
				case ESpellType.MesmerizeDurationBuff:
					dw.AddKeyValuePair("damage_type", "22");
					dw.AddKeyValuePair("dur_type", "2");
					dw.AddKeyValuePair("power_level", "29");
					break;
				case ESpellType.Nearsight:
					dw.AddKeyValuePair("power_level", Spell.Value);
					break;
				case ESpellType.PetConversion:
					dw.AddKeyValuePair("delve_string", "Banishes the caster's pet and reclaims some of its energy.");
					break;
				case ESpellType.Resurrect:
					dw.AddKeyValuePair("amount_increase", Spell.ResurrectMana);
					dw.AddKeyValuePair("type1", "65");
					dw.Values["target"] = 8.ToString();
					break;
				case ESpellType.SavageCombatSpeedBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("power_level", Spell.Value * 2);
					break;
				case ESpellType.SavageCrushResistanceBuff:
				case ESpellType.SavageEnduranceHeal:
				case ESpellType.SavageParryBuff:
				case ESpellType.SavageSlashResistanceBuff:
				case ESpellType.SavageThrustResistanceBuff:
					dw.AddKeyValuePair("cost_type", "2");
					break;
				case ESpellType.SavageDPSBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("delve_string", $"Increases your melee damage by {(int)Spell.Value}%.");
					break;
				case ESpellType.SavageEvadeBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("delve_string", $"Increases your chance to evade by {(int)Spell.Value}%.");
					break;
				case ESpellType.SummonAnimistPet:
				case ESpellType.SummonCommander:
				case ESpellType.SummonDruidPet:
				case ESpellType.SummonHunterPet:
				case ESpellType.SummonNecroPet:
				case ESpellType.SummonSimulacrum:
				case ESpellType.SummonSpiritFighter:
				case ESpellType.SummonUnderhill:
					dw.AddKeyValuePair("power_level", Spell.Damage);
					//dw.AddKeyValuePair("delve_string", "Summons a Pet to serve you.");
					//dw.AddKeyValuePair("description_string", "Summons a Pet to serve you.");
					break;
				case ESpellType.SummonMinion:
					dw.AddKeyValuePair("power_level", Spell.Value);
					break;
				case ESpellType.StyleStun:
					dw.AddKeyValuePair("type1", "22");
					break;
				case ESpellType.StyleSpeedDecrease:
					dw.AddKeyValuePair("type1", "39");
					break;
				case ESpellType.StyleCombatSpeedDebuff:
					dw.AddKeyValuePair("type1", "8");
					dw.AddKeyValuePair("power_level", -Spell.Value);
					break;
				case ESpellType.TurretPBAoE:
					dw.AddKeyValuePair("delve_string", $"Target takes {(int)Spell.Damage} damage. Spell affects everyone in the immediate radius of the caster's pet, and does less damage the further away they are from the caster's pet.");
					break;
				case ESpellType.TurretsRelease:
					dw.AddKeyValuePair("delve_string", "Unsummons all the animist turret(s) in range.");
					break;
				case ESpellType.StyleRange:
					dw.AddKeyValuePair("delve_string", $"Hits target up to {(int)Spell.Value} units away.");
					break;
				case ESpellType.MultiTarget:
					dw.AddKeyValuePair("delve_string", $"Hits {(int)Spell.Value} additonal target(s) within melee range.");
					break;
				case ESpellType.PiercingMagic:
					dw.AddKeyValuePair("delve_string", $"Effectiveness of the target's spells is increased by {(int)Spell.Value}%. Against higher level opponents than the target, this should reduce the chance of a full resist.");
					break;
				case ESpellType.StyleTaunt:
					if (Spell.Value < 0)
						dw.AddKeyValuePair("delve_string", $"Decreases your threat to monster targets by {-(int)Spell.Value} damage.");
					break;
				case ESpellType.NaturesShield:
					dw.AddKeyValuePair("delve_string", $"Gives the user a {(int)Spell.Value}% base chance to block ranged melee attacks while this style is prepared.");
					break;
				case ESpellType.SlashResistDebuff:
					dw.AddKeyValuePair("delve_string", $"Decreases target's resistance to Slash by {(int)Spell.Value}% for {(int)Spell.Duration / 1000} seconds.");
					break;
					
			}
		}

		/// <summary>
		/// Returns delve code for target
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		protected virtual int GetSpellTargetType()
		{
			return Spell.Target switch
			{
				ESpellTarget.REALM => 7,
				ESpellTarget.SELF => 0,
				ESpellTarget.ENEMY => 1,
				ESpellTarget.PET => 6,
				ESpellTarget.GROUP => 3,
				ESpellTarget.AREA => 0,// TODO
				_ => 0
			};
		}

		protected virtual int GetDurationType()
		{
			//2-seconds, 4-conc, 5-focus
			if (Spell.Duration > 0)
				return 2;

			if (Spell.IsConcentration)
				return 4;

			return 0;
		}

		#endregion
	}
}
