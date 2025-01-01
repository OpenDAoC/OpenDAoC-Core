using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Default class for spell handler
	/// should be used as a base class for spell handler
	/// </summary>
	public class SpellHandler : ISpellHandler
	{
		// Maximum number of sub-spells to get delve info for.
		protected const byte MAX_DELVE_RECURSION = 5;

		// Maximum number of Concentration spells that a single caster is allowed to cast.
		private const int MAX_CONC_SPELLS = 20;
		private const int PULSING_SPELL_END_OF_CAST_MESSAGE_INTERVAL = 2000;

		// Array of pulse spell groups allowed to exist with others.
		// Used to allow players to have more than one pulse spell refreshing itself automatically.
		private static readonly int[] PulseSpellGroupsIgnoringOtherPulseSpells = [];

		public GameLiving Target { get; set; }
		public eCastState CastState { get; private set; }
		protected bool HasLos { get; private set; }
		protected double DistanceFallOff { get; private set; }
		protected double CasterEffectiveness { get; private set; } = 1.0; // Needs to default to 1 since some spell handlers override `StartSpell`, preventing it from being set.

		protected Spell m_spell;
		protected SpellLine m_spellLine;
		protected GameLiving m_caster;
		protected bool m_interrupted = false;

		/// <summary>
		/// Shall we start the reuse timer
		/// </summary>
		protected bool m_startReuseTimer = true;

		private QuickCastECSGameEffect _quickcast;
		private long _castStartTick;
		private long _castEndTick;
		private long _calculatedCastTime;
		private long _puslingSpellLastEndOfCastMessage;

		public bool IsQuickCasting => _quickcast != null;
		public long CastStartTick => _castStartTick;
		public bool StartReuseTimer => m_startReuseTimer;

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

		private long _lastDuringCastLosCheckTime;
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
				if (m_spell.SpellType != eSpellType.None)
					switch (m_spell.SpellType)
					{
						case eSpellType.Bomber:
						case eSpellType.Charm:
						case eSpellType.Pet:
						case eSpellType.SummonCommander:
						case eSpellType.SummonTheurgistPet:
						case eSpellType.Summon:
						case eSpellType.SummonJuggernaut:
						//case eSpellType.SummonMerchant:
						case eSpellType.SummonMinion:
						case eSpellType.SummonSimulacrum:
						case eSpellType.SummonUnderhill:
						//case eSpellType.SummonVaultkeeper:
						case eSpellType.SummonAnimistAmbusher:
						case eSpellType.SummonAnimistPet:
						case eSpellType.SummonDruidPet:
						case eSpellType.SummonHealingElemental:
						case eSpellType.SummonHunterPet:
						case eSpellType.SummonAnimistFnF:
						case eSpellType.SummonAnimistFnFCustom:
						case eSpellType.SummonSiegeBallista:
						case eSpellType.SummonSiegeCatapult:
						case eSpellType.SummonSiegeRam:
						case eSpellType.SummonSiegeTrebuchet:
						case eSpellType.SummonSpiritFighter:
						case eSpellType.SummonNecroPet:
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
				MessageToCaster("Your spell was cancelled.", eChatType.CT_SpellExpires);
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
				MessageToCaster("You stop playing your song.", eChatType.CT_Spell);
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
				MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
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
			if (instrument == null || instrument.Object_Type != (int)eObjectType.Instrument ) // || (instrument.DPS_AF != 4 && instrument.DPS_AF != m_spell.InstrumentRequirement))
				return false;

			return true;
		}

		/// <summary>
		/// Cancels first pulsing spell of type
		/// </summary>
		/// <param name="living">owner of pulsing spell</param>
		/// <param name="spellType">type of spell to cancel</param>
		/// <returns>true if any spells were canceled</returns>
		public virtual bool CancelPulsingSpell(GameLiving living, eSpellType spellType)
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
					ECSPulseEffect effect = effects[i];
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

		public virtual ECSGameSpellEffect CreateECSEffect(ECSGameEffectInitParams initParams)
		{
			return new ECSGameSpellEffect(initParams);
		}

		public virtual ECSPulseEffect CreateECSPulseEffect(GameLiving target, double effectiveness)
		{
			int freq = Spell != null ? Spell.Frequency : 0;
			return new ECSPulseEffect(target, this, CalculateEffectDuration(target), freq, effectiveness, Spell.Icon);
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

			if (Caster is GamePlayer playerCaster)
			{
				if (CastState is not eCastState.Focusing)
					playerCaster.Out.SendMessage(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CasterMove"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				else
					CancelFocusSpells(true);
			}

			InterruptCasting();
		}

		/// <summary>
		/// This sends the spell messages to the player/target.
		///</summary>
		public virtual void SendSpellMessages()
		{
			if (Spell.SpellType is not eSpellType.PveResurrectionIllness and not eSpellType.RvrResurrectionIllness)
			{
				if (Spell.InstrumentRequirement == 0)
				{
					if (Caster is GamePlayer playerCaster)
					{
						// Message: You begin casting a {0} spell!
						MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CastSpell.Msg.YouBeginCasting", Spell.Name), eChatType.CT_Spell);
					}
					else if (Caster is NecromancerPet petCaster && petCaster.Owner is GamePlayer casterOwner)
					{
						// Message: {0} begins casting a {1} spell!
						casterOwner.Out.SendMessage(LanguageMgr.GetTranslation(casterOwner.Client.Account.Language, "SpellHandler.CastSpell.Msg.PetBeginsCasting", Caster.GetName(0, true), Spell.Name), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
					}
				}
				else if (Caster is GamePlayer songCaster)
				{
					// Message: You begin playing {0}!
					MessageToCaster(LanguageMgr.GetTranslation(songCaster.Client, "SpellHandler.CastSong.Msg.YouBeginPlaying", Spell.Name), eChatType.CT_Spell);
				}
			}
		}

		public virtual bool CasterIsAttacked(GameLiving attacker)
		{
			// [StephenxPimentel] Check if the necro has MoC effect before interrupting.
			if (Caster is NecromancerPet necroPet && necroPet.Owner is GamePlayer necroOwner)
			{
				if (necroOwner.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration))
					return false;
			}

			if (Spell.Uninterruptible)
				return false;

			if (Caster.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration)
				|| Caster.effectListComponent.ContainsEffectForEffectType(eEffect.FacilitatePainworking)
				|| IsQuickCasting)
				return false;

			// Only interrupt if we're under 50% of the way through the cast.
			if (IsInCastingPhase && (GameLoop.GameLoopTime < _castStartTick + _calculatedCastTime * 0.5))
			{
				if (Caster is GameSummonedPet petCaster && petCaster.Owner is GamePlayer casterOwner)
				{
					casterOwner.LastInterruptMessage = $"Your {Caster.Name} was attacked by {attacker.Name} and their spell was interrupted!";
					MessageToLiving(casterOwner, casterOwner.LastInterruptMessage, eChatType.CT_SpellResisted);
				}
				else if (Caster is GamePlayer playerCaster)
				{
					playerCaster.LastInterruptMessage = $"{attacker.GetName(0, true)} attacks you and your spell is interrupted!";
					MessageToLiving(playerCaster, playerCaster.LastInterruptMessage, eChatType.CT_SpellResisted);
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
					MessageToCaster("You are dead and can't cast!", eChatType.CT_System);

				return false;
			}

			Target = selectedTarget;

			switch (Spell.Target)
			{
				case eSpellTarget.SELF:
				{
					// Self spells should ignore whatever we actually have selected.
					Target = Caster;
					break;
				}
				case eSpellTarget.PET:
				{
					// Get the current target if we don't have one already.
					if (Target == null)
						Target = Caster?.TargetObject as GameLiving;

					// Pet spells are automatically casted on the controlled NPC, but only if the current target isn't a subpet or a turret.
					if (((Target as GameNPC)?.Brain as IControlledBrain)?.GetPlayerOwner() != Caster && Caster.ControlledBrain?.Body != null)
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

			if (Caster is GameNPC npcOwner)
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
				ECSPulseEffect effect = EffectListService.GetPulseEffectOnTarget(m_caster, m_spell);

				if (EffectService.RequestImmediateCancelConcEffect(effect))
				{
					if (m_spell.InstrumentRequirement == 0)
						MessageToCaster("You cancel your effect.", eChatType.CT_Spell);
					else
						MessageToCaster("You stop playing your song.", eChatType.CT_Spell);

					return false;
				}
			}

			CancelFocusSpells(false);
			_quickcast = EffectListService.GetAbilityEffectOnTarget(m_caster, eEffect.QuickCast) as QuickCastECSGameEffect;

			if (IsQuickCasting)
				_quickcast.ExpireTick = GameLoop.GameLoopTime + _quickcast.Duration;

			GamePlayer playerCaster = m_caster as GamePlayer;

			if (playerCaster != null)
			{
				long nextSpellAvailTime = m_caster.TempProperties.GetProperty<long>(GamePlayer.NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION);

				if (nextSpellAvailTime > m_caster.CurrentRegion.Time && Spell.CastTime > 0) // instant spells ignore the potion cast delay
				{
					playerCaster.Out.SendMessage(LanguageMgr.GetTranslation(playerCaster.Client, "GamePlayer.CastSpell.MustWaitBeforeCast", (nextSpellAvailTime - m_caster.CurrentRegion.Time) / 1000), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				if (playerCaster.Steed is GameSiegeRam)
				{
					if (!quiet)
						MessageToCaster("You can't cast in a siege ram!", eChatType.CT_System);

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
				SelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<SelectiveBlindnessEffect>();

				if (SelectiveBlindness != null)
				{
					GameLiving EffectOwner = SelectiveBlindness.EffectSource;

					if (EffectOwner==Target)
					{
						if (playerCaster != null && !quiet)
							playerCaster.Out.SendMessage(string.Format("{0} is invisible to you!", Target.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

						return false;
					}
				}
			}

			if (Target !=null && Target.HasAbility("DamageImmunity") && Spell.SpellType == eSpellType.DirectDamage && Spell.Radius == 0)
			{
				if (!quiet)
					MessageToCaster(Target.Name + " is immune to this effect!", eChatType.CT_SpellResisted);

				return false;
			}

			if (m_spell.InstrumentRequirement != 0)
			{
				if (!CheckInstrument())
				{
					if (!quiet)
						MessageToCaster("You are not wielding the right type of instrument!", eChatType.CT_SpellResisted);

					return false;
				}
			}
			// Songs can be played even if sitting.
			else if (m_caster.IsSitting)
			{
				// Purge can be cast while sitting but only if player has negative effect that doesn't allow standing up (like stun or mez).
				if (!quiet)
					MessageToCaster("You can't cast while sitting!", eChatType.CT_SpellResisted);

				return false;
			}

			// Stop our melee attack. NPC brains will resume it automatically.
			if (!Spell.IsInstantCast && m_caster.attackComponent.AttackState && !m_caster.CanCastWhileAttacking())
				m_caster.attackComponent.StopAttack();

			// Check interrupt timer.
			if (!m_spell.Uninterruptible && !m_spell.IsInstantCast)
			{
				long interruptRemainingDuration = Caster.InterruptRemainingDuration;

				if (interruptRemainingDuration > 0)
				{
					interruptRemainingDuration /= 1000 + 1;

					if (playerCaster != null)
					{
						if (!IsQuickCasting &&
							!m_caster.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration))
						{
							if (!quiet)
								MessageToCaster($"You must wait {interruptRemainingDuration} seconds to cast a spell!", eChatType.CT_SpellResisted);

							return false;
						}
					}
					else if (m_caster is NecromancerPet necroPet && necroPet.Brain is NecromancerPetBrain)
					{
						if (!necroPet.effectListComponent.ContainsEffectForEffectType(eEffect.FacilitatePainworking))
						{
							if (!quiet)
								MessageToCaster($"Your {necroPet.Name} must wait {interruptRemainingDuration} seconds to cast a spell!", eChatType.CT_SpellResisted);

							return false;
						}
					}
					else
						return false;
				}
			}

			if (m_spell.RecastDelay > 0)
			{
				int left = m_caster.GetSkillDisabledDuration(m_spell);

				if (left > 0)
				{
					if (m_caster is NecromancerPet && ((m_caster as NecromancerPet).Owner as GamePlayer).Client.Account.PrivLevel > (int)ePrivLevel.Player)
					{
						// Ignore Recast Timer
					}
					else
					{
						if (!quiet)
							MessageToCaster("You must wait " + (left / 1000 + 1).ToString() + " seconds to use this spell!", eChatType.CT_System);
						return false;
					}
				}
			}

			switch (Spell.Target)
			{
				case eSpellTarget.PET:
				{
					if (Target == null || ((Target as GameNPC)?.Brain as IControlledBrain)?.GetPlayerOwner() != Caster)
					{
						if (!quiet)
							MessageToCaster("You must cast this spell on a creature you are controlling.", eChatType.CT_System);

						return false;
					}

					break;
				}
				case eSpellTarget.AREA:
				{
					if (!m_caster.IsWithinRadius(m_caster.GroundTarget, CalculateSpellRange()))
					{
						if (!quiet)
							MessageToCaster("Your area target is out of range. Select a closer target.", eChatType.CT_SpellResisted);

						return false;
					}

					break;
				}
				case eSpellTarget.REALM:
				case eSpellTarget.ENEMY:
				case eSpellTarget.CORPSE:
				{
					if (m_spell.Range <= 0)
						break;

					// All spells that need a target.
					if (Target == null || Target.ObjectState != GameObject.eObjectState.Active)
					{
						if (!quiet)
							MessageToCaster("You must select a target for this spell!", eChatType.CT_SpellResisted);

						return false;
					}

					if (!m_caster.IsWithinRadius(Target, CalculateSpellRange()))
					{
						if (Caster is GamePlayer && !quiet)
							MessageToCaster("That target is too far away!", eChatType.CT_SpellResisted);

						Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, CastFailedEventArgs.Reasons.TargetTooFarAway));

						if (Caster is GameNPC npc)
							npc.Follow(Target, Spell.Range - 100, npc.StickMaximumRange);

						return false;
					}

					if (!HasLos)
					{
						if (!quiet)
							MessageToCaster("You can't see your target from here!", eChatType.CT_SpellResisted);

						Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, CastFailedEventArgs.Reasons.TargetNotInView));
						return false;
					}

					switch (m_spell.Target)
					{
						case eSpellTarget.ENEMY:
						{
							if (Target == m_caster)
							{
								if (!quiet)
									MessageToCaster("You can't attack yourself! ", eChatType.CT_System);

								return false;
							}

							if (FindStaticEffectOnTarget(Target, typeof(NecromancerShadeEffect)) != null)
							{
								if (!quiet)
									MessageToCaster("Invalid target.", eChatType.CT_System);

								return false;
							}

							if (m_spell.SpellType == eSpellType.Charm && m_spell.CastTime == 0 && m_spell.Pulse != 0)
								break;

							if (Caster is TurretPet)
								return true;

							// Pet spells (shade) don't require the target to be in front.
							if ((m_spell.SpellType is not eSpellType.PetSpell && !m_caster.IsObjectInFront(Target, 180)) ||
								(playerCaster != null && !playerCaster.CanDetect(Target)))
							{
								if (!quiet)
									MessageToCaster("Your target is not visible!", eChatType.CT_SpellResisted);

								Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, CastFailedEventArgs.Reasons.TargetNotInView));
								return false;
							}

							if (!GameServer.ServerRules.IsAllowedToAttack(Caster, Target, quiet))
								return false;

							break;
						}
						case eSpellTarget.CORPSE:
						{
							if (Target.IsAlive || !GameServer.ServerRules.IsSameRealm(Caster, Target, true))
							{
								if (!quiet)
									MessageToCaster("This spell only works on dead members of your realm!", eChatType.CT_SpellResisted);

								return false;
							}

							break;
						}
						case eSpellTarget.REALM:
						{
							if (!GameServer.ServerRules.IsSameRealm(Caster, Target, true))
								return false;

							break;
						}
					}

					if (m_spell.Target is not eSpellTarget.CORPSE && !Target.IsAlive)
					{
						if (!quiet)
							MessageToCaster(Target.GetName(0, true) + " is dead!", eChatType.CT_SpellResisted);

						return false;
					}

					break;
				}
			}

			if (Spell.Power != 0 &&
				(playerCaster == null || (eCharacterClass) playerCaster.CharacterClass.ID is not eCharacterClass.Savage) &&
				m_caster.Mana < PowerCost(Target) &&
				!IsQuickCasting &&
				Spell.SpellType is not eSpellType.Archery)
			{
				if (!quiet)
					MessageToCaster("You don't have enough power to cast that!", eChatType.CT_SpellResisted);

				return false;
			}

			if (playerCaster != null && m_spell.Concentration > 0)
			{
				if (m_caster.Concentration < m_spell.Concentration)
				{
					if (!quiet)
						MessageToCaster("This spell requires " + m_spell.Concentration + " concentration points to cast!", eChatType.CT_SpellResisted);

					return false;
				}

				var maxConc = MAX_CONC_SPELLS;

				//self buff charge IDs should not count against conc cap
				maxConc += playerCaster.effectListComponent.ConcentrationEffects.Count(concentrationEffect =>
				{
					return concentrationEffect.SpellHandler?.Spell?.ID != null && playerCaster.SelfBuffChargeIDs.Contains(concentrationEffect.SpellHandler.Spell.ID);
				});

				if (m_caster.effectListComponent.ConcentrationEffects.Count >= maxConc)
				{
					if (!quiet)
						MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", eChatType.CT_SpellResisted);

					return false;
				}
			}

			// Cancel engage if user starts attack
			if (m_caster.IsEngaging)
			{
				EngageECSGameEffect engage = (EngageECSGameEffect) EffectListService.GetEffectOnTarget(m_caster, eEffect.Engage);

				if (engage != null)
					engage.Cancel(false, false);
			}

			if (UnstealthCasterOnStart)
				Caster.Stealth(false);

			if (Caster is NecromancerPet necromancerPet && necromancerPet.Brain is NecromancerPetBrain necromancerPetBrain)
				necromancerPetBrain.OnPetBeginCast(Spell, SpellLine);

			return true;
		}

		private void CheckPlayerLosDuringCastCallback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			HasLos = response is eLosCheckResponse.TRUE;

			if (!HasLos && Properties.CHECK_LOS_DURING_CAST_INTERRUPT)
			{
				if (IsInCastingPhase)
					MessageToCaster("You can't see your target from here!", eChatType.CT_SpellResisted);

				InterruptCasting();
			}
		}

		private void CheckPetLosDuringCastCallback(GameLiving living, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			HasLos = response is eLosCheckResponse.TRUE;

			if (!HasLos && Properties.CHECK_LOS_DURING_CAST_INTERRUPT)
				InterruptCasting();
		}

		/// <summary>
		/// Checks after casting before spell is executed
		/// </summary>
		public virtual bool CheckEndCast(GameLiving target)
		{
			bool verbose = CheckVerbosity();

			if (IsSummoningSpell && Caster.CurrentRegion.IsCapitalCity)
			{
				// Message: You can't summon here!
				if (verbose)
					ChatUtil.SendErrorMessage(Caster as GamePlayer, "GamePlayer.CastEnd.Fail.BadRegion", null);

				return false;
			}
			
			if (Caster != target && Caster is GameNPC casterNPC && Caster is not NecromancerPet)
				casterNPC.TurnTo(target);

			if (m_caster.ObjectState is not GameObject.eObjectState.Active)
				return false;

			if (!m_caster.IsAlive)
			{
				if (verbose)
					MessageToCaster("You are dead and can't cast!", eChatType.CT_System);

				return false;
			}

			if (m_spell.InstrumentRequirement != 0)
			{
				if (!CheckInstrument())
				{
					if (verbose)
						MessageToCaster("You are not wielding the right type of instrument!", eChatType.CT_SpellResisted);

					return false;
				}
			}
			else if (m_caster.IsSitting) // Songs can be played when sitting.
			{
				// Purge can be cast while sitting but only if player has negative effect that doesn't allow standing up (like stun or mez).
				if (verbose)
					MessageToCaster("You can't cast while sitting!", eChatType.CT_SpellResisted);

				return false;
			}

			if (m_spell.Target is eSpellTarget.AREA)
			{
				if (!m_caster.IsWithinRadius(m_caster.GroundTarget, CalculateSpellRange()))
				{
					if (verbose)
						MessageToCaster("Your area target is out of range. Select a closer target.", eChatType.CT_SpellResisted);

					return false;
				}
			}
			else if (m_spell.Target is not eSpellTarget.SELF && m_spell.Target is not eSpellTarget.GROUP && m_spell.Target is not eSpellTarget.CONE && m_spell.Range > 0)
			{
				if (m_spell.Target is not eSpellTarget.PET)
				{
					// All other spells that need a target.
					if (target == null || target.ObjectState is not GameObject.eObjectState.Active)
					{
						if (verbose)
							MessageToCaster("You must select a target for this spell!", eChatType.CT_SpellResisted);

						return false;
					}

					if (!HasLos)
					{
						if (verbose)
							MessageToCaster("You can't see your target from here!", eChatType.CT_SpellResisted);

						return false;
					}
				}

				if (!m_caster.IsWithinRadius(target, CalculateSpellRange()))
				{
					if (verbose)
						MessageToCaster("That target is too far away!", eChatType.CT_SpellResisted);

					return false;
				}

				switch (m_spell.Target)
				{
					case eSpellTarget.ENEMY:
					{
						if (m_spell.SpellType is eSpellType.Charm)
							break;

						// Pet spells (shade) don't require the target to be in front.
						if ((m_spell.SpellType is not eSpellType.PetSpell && !m_caster.IsObjectInFront(Target, 180, Caster.TargetInViewAlwaysTrueMinRange)) ||
							(m_caster is GamePlayer playerCaster && !playerCaster.CanDetect(Target)))
						{
							if (verbose)
								MessageToCaster("Your target is not visible!", eChatType.CT_SpellResisted);

							return false;
						}

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, target, false))
							return false;

						break;
					}
					case eSpellTarget.CORPSE:
					{
						if (target.IsAlive || !GameServer.ServerRules.IsSameRealm(Caster, target, true))
						{
							if (verbose)
								MessageToCaster("This spell only works on dead members of your realm!", eChatType.CT_SpellResisted);

							return false;
						}

						break;
					}
				}
			}

			if (m_caster.Mana <= 0 && Spell.Power > 0 && Spell.SpellType is not eSpellType.Archery)
			{
				if (verbose)
					MessageToCaster("You have exhausted all of your power and cannot cast spells!", eChatType.CT_SpellResisted);

				return false;
			}

			if (Spell.Power > 0 && m_caster.Mana < PowerCost(target) && !IsQuickCasting && Spell.SpellType is not eSpellType.Archery)
			{
				if (verbose)
					MessageToCaster("You don't have enough power to cast that!", eChatType.CT_SpellResisted);

				return false;
			}

			if (m_caster is GamePlayer && m_spell.Concentration > 0)
			{
				if (m_caster.Concentration < m_spell.Concentration)
				{
					if (verbose)
						MessageToCaster("This spell requires " + m_spell.Concentration + " concentration points to cast!", eChatType.CT_SpellResisted);

					return false;
				}

				if (m_caster.effectListComponent.ConcentrationEffects.Count >= MAX_CONC_SPELLS)
				{
					if (verbose)
						MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", eChatType.CT_SpellResisted);

					return false;
				}
			}

			return true;

			bool CheckVerbosity()
			{
				if (!m_spell.IsPulsing)
					return true;

				if (GameLoop.GameLoopTime - _puslingSpellLastEndOfCastMessage >= PULSING_SPELL_END_OF_CAST_MESSAGE_INTERVAL)
				{
					_puslingSpellLastEndOfCastMessage = GameLoop.GameLoopTime;
					return true;
				}

				return false;
			}
		}

		public virtual bool CheckDuringCast(GameLiving target)
		{
			return CheckDuringCast(target, false);
		}

		public virtual bool CheckDuringCast(GameLiving target, bool quiet)
		{
			if (m_interrupted)
				return false;

			if (Caster is GameNPC npcOwner)
			{
				if (Spell.CastTime > 0)
				{
					if (npcOwner.IsMoving)
						npcOwner.StopFollowing();
				}

				if (npcOwner != Target)
					npcOwner.TurnTo(Target);
			}

			if (Properties.CHECK_LOS_DURING_CAST && GameLoop.GameLoopTime > _lastDuringCastLosCheckTime + Properties.CHECK_LOS_DURING_CAST_MINIMUM_INTERVAL)
			{
				_lastDuringCastLosCheckTime = GameLoop.GameLoopTime;

				if (m_spell.Target is not eSpellTarget.SELF and not eSpellTarget.GROUP and not eSpellTarget.CONE and not eSpellTarget.PET && m_spell.Range > 0)
				{

					if (Caster is GameNPC npc && npc.Brain is IControlledBrain npcBrain)
						npcBrain.GetPlayerOwner()?.Out.SendCheckLos(npc, target, CheckPetLosDuringCastCallback);
					else if (Caster is GamePlayer player)
						player.Out.SendCheckLos(player, target, CheckPlayerLosDuringCastCallback);
				}
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
		public void Tick()
		{
			switch (CastState)
			{
				case eCastState.Precast:
				{
					if (CheckBeginCast(Target))
					{
						_castStartTick = GameLoop.GameLoopTime;

						if (Spell.IsInstantCast)
						{
							if (!CheckEndCast(Target))
								CastState = eCastState.Interrupted;
							else
							{
								// Unsure about this. Calling 'SendCastAnimation' on non-harmful instant spells plays an annoying deep hum that overlaps with the
								// sound of the spell effect (but is fine to have on harmful ones). For certain spells (like Skald's resist chants) it instead
								// plays the audio of the spell effect a second time.
								// It may prevent certain animations from playing, but I don't think there's any non-harmful instant spell with a casting animation.
								if (Spell.IsHarmful)
									SendCastAnimation(0);

								CastState = eCastState.Finished;
							}
						}
						else
						{
							SendSpellMessages();
							SendCastAnimation();
							CastState = eCastState.Casting;
						}
					}
					else
					{
						if (Caster.IsBeingInterrupted)
							CastState = eCastState.Interrupted;
						else
							CastState = eCastState.Cleanup;
					}

					break;
				}
				case eCastState.Casting:
				case eCastState.CastingRetry:
				{
					if (!CheckDuringCast(Target))
						CastState = eCastState.Interrupted;

					if (ServiceUtils.ShouldTick(_castEndTick))
					{
						if (!CheckEndCast(Target))
						{
							// Allow flute mez to keep trying (1.65 compliance).
							if (m_spell.IsPulsing && m_spell.SpellType is eSpellType.Mesmerize)
								CastState = eCastState.CastingRetry;
							else
								CastState = eCastState.Interrupted;
						}
						else
							CastState = eCastState.Finished;
					}

					break;
				}
				case eCastState.Focusing:
				{
					if (Caster.IsStrafing || Caster.IsMoving)
					{
						CasterMoves();
						CastState = eCastState.Cleanup;
					}

					break;
				}
			}

			// Process cast on same tick if interrupted or finished.
			switch (CastState)
			{
				case eCastState.Interrupted:
				{
					InterruptCasting();
					CastState = eCastState.Cleanup;
					break;
				}
				case eCastState.Finished:
				{
					FinishSpellCast(Target);

					if (Spell.IsFocus)
					{
						if (Spell.SpellType is not eSpellType.GatewayPersonalBind)
							CastState = eCastState.Focusing;
						else
						{
							CastState = eCastState.Cleanup;
							DbInventoryItem stone = Caster.Inventory.GetFirstItemByName("Personal Bind Recall Stone", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

							if (stone != null)
								stone.CanUseAgainIn = stone.CanUseEvery;
						}
					}
					else
						CastState = eCastState.Cleanup;

					break;
				}
			}

			if (CastState is eCastState.Cleanup)
				Caster.castingComponent.OnSpellHandlerCleanUp(Spell);
		}

		/// <summary>
		/// Calculates the power to cast the spell
		/// </summary>
		public virtual int PowerCost(GameLiving target)
		{
			// Warlock.
			/* GameSpellEffect effect = SpellHandler.FindEffectOnTarget(m_caster, "Powerless");
			if (effect != null && !m_spell.IsPrimary)
				return 0;*/

			// 1.108 - Valhalla's Blessing now has a 75% chance to not use power.
			if (m_caster.EffectList.GetOfType<ValhallasBlessingEffect>() != null && Util.Chance(75))
				return 0;

			// Patch 1.108 increases the chance to not use power to 50%.
			if (m_caster.EffectList.GetOfType<FungalUnionEffect>() != null && Util.Chance(50))
				return 0;

			// Arcane Syphon.
			int syphon = Caster.GetModified(eProperty.ArcaneSyphon);
			if (syphon > 0 && Util.Chance(syphon))
				return 0;

			double powerCost = m_spell.Power;
			GamePlayer playerCaster = Caster as GamePlayer;

			// Percent of max power if less than zero.
			if (powerCost < 0)
			{
				if (playerCaster != null && playerCaster.CharacterClass.ManaStat is not eStat.UNDEFINED)
					powerCost = playerCaster.CalculateMaxMana(playerCaster.Level, playerCaster.GetBaseStat(playerCaster.CharacterClass.ManaStat)) * powerCost * -0.01;
				else
					powerCost = Caster.MaxMana * powerCost * -0.01;
			}

			if (playerCaster != null && playerCaster.CharacterClass.FocusCaster)
			{
				eProperty focusProp = SkillBase.SpecToFocus(SpellLine.Spec);

				if (focusProp is not eProperty.Undefined)
				{
					double focusBonus = Caster.GetModified(focusProp) * 0.4;

					if (Spell.Level > 0)
						focusBonus /= Spell.Level;

					if (focusBonus > 0.4)
						focusBonus = 0.4;
					else if (focusBonus < 0)
						focusBonus = 0;

					focusBonus *= Math.Min(1, playerCaster.GetModifiedSpecLevel(SpellLine.Spec) / (double) Spell.Level);
					powerCost *= 1.2 - focusBonus; // Between 120% and 80% of base power cost.
				}
			}

			// Doubled power usage if using QuickCast.
			if (IsQuickCasting && Spell.CastTime > 0)
				powerCost *= 2;

			return (int) powerCost;
		}

		public virtual int CalculateEnduranceCost()
		{
			return Spell.IsPulsing ? 0 : 5;
		}

		/// <summary>
		/// Calculates the range to target needed to cast the spell
		/// NOTE: This method returns a minimum value of 32
		/// </summary>
		/// <returns></returns>
		public virtual int CalculateSpellRange()
		{
			int range = Math.Max(32, (int)(Spell.Range * Caster.GetModified(eProperty.SpellRange) * 0.01));
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
			CastState = eCastState.Interrupted;
			m_startReuseTimer = false;
		}

		/// <summary>
		/// Calculates the effective casting time
		/// </summary>
		/// <returns>effective casting time in milliseconds</returns>
		public virtual int CalculateCastingTime()
		{
			return m_caster.CalculateCastingTime(this);
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
			_castEndTick = _castStartTick + _calculatedCastTime;

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
				if (Spell.SpellType is not eSpellType.PveResurrectionIllness and not eSpellType.RvrResurrectionIllness)
				{
					GameLiving toExclude = null;

					if (playerCaster != null)
					{
						// Message: You cast a {0} spell!
						MessageToCaster(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CastSpell.Msg.YouCastSpell", Spell.Name), eChatType.CT_Spell);
						toExclude = playerCaster;
					}
					else if (Caster is NecromancerPet pet && pet.Owner is GamePlayer casterOwner)
					{
						// Message: {0} cast a {1} spell!
						casterOwner.Out.SendMessage(LanguageMgr.GetTranslation(casterOwner.Client.Account.Language, "SpellHandler.CastSpell.Msg.PetCastSpell", Caster.GetName(0, true), Spell.Name), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
						toExclude = casterOwner;
					}

					foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
					{
						if (player != toExclude)
							// Message: {0} casts a spell!
							player.MessageFromArea(m_caster, LanguageMgr.GetTranslation(player.Client, "SpellHandler.CastSpell.Msg.LivingCastsSpell", Caster.GetName(0, true)), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
					}
				}
			}

			// Cancel existing pulse effects, using 'SpellGroupsCancellingOtherPulseSpells'.
			if (m_spell.IsPulsing)
			{
				if (!PulseSpellGroupsIgnoringOtherPulseSpells.Contains(m_spell.Group))
				{
					IEnumerable<ECSPulseEffect> effects = m_caster.effectListComponent.GetAllPulseEffects().Where(x => !PulseSpellGroupsIgnoringOtherPulseSpells.Contains(x.SpellHandler.Spell.Group));

					foreach (ECSPulseEffect effect in effects)
						EffectService.RequestImmediateCancelConcEffect(effect);
				}

				// Prevent `EffectListService` from pulsing flute mez, since it won't handle it correctly.
				if (m_spell.SpellType is not eSpellType.Mesmerize)
					PulseEffect = CreateECSPulseEffect(Caster, CasterEffectiveness);
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
			if (playerCaster != null)
			{
				if (IsQuickCasting && Spell.CastTime > 0)
				{
					m_caster.TempProperties.SetProperty(GamePlayer.QUICK_CAST_CHANGE_TICK, m_caster.CurrentRegion.Time);
					playerCaster.DisableSkill(SkillBase.GetAbility(Abilities.Quickcast), QuickCastAbilityHandler.DISABLE_DURATION);
					_quickcast.Cancel(false);
				}
			}

			if (m_ability != null)
				m_caster.DisableSkill(m_ability.Ability, (m_spell.RecastDelay == 0 ? 3000 : m_spell.RecastDelay));

			DisableSpellAndSpellsOfSameGroup();
			int enduranceCost = CalculateEnduranceCost();

			if (enduranceCost > 0)
				m_caster.ChangeEndurance(m_caster, eEnduranceChangeType.Spell, -enduranceCost);

			GameEventMgr.Notify(GameLivingEvent.CastFinished, m_caster, new CastingEventArgs(this, target, m_lastAttackData));
		}

		private void DisableSpellAndSpellsOfSameGroup()
		{
			if (m_spell.RecastDelay <= 0 || !m_startReuseTimer)
				return;

			if (m_caster is GamePlayer playerCaster)
			{
				List<Tuple<Skill, int>> toDisable = [];

				foreach (Tuple<Skill, Skill> skill in playerCaster.GetAllUsableSkills())
				{
					if (IsSameSpellOrOfSameGroup(skill.Item1 as Spell))
						toDisable.Add(new Tuple<Skill, int>(skill.Item1, m_spell.RecastDelay));
				}

				foreach (Tuple<SpellLine, List<Skill>> spellLine in playerCaster.GetAllUsableListSpells())
				{
					foreach (Skill skill in spellLine.Item2)
					{
						if (IsSameSpellOrOfSameGroup(skill as Spell))
							toDisable.Add(new Tuple<Skill, int>(skill, m_spell.RecastDelay));
					}
				}

				m_caster.DisableSkills(toDisable);
			}
			else if (m_caster is GameNPC)
				m_caster.DisableSkill(m_spell, m_spell.RecastDelay);

			bool IsSameSpellOrOfSameGroup(Spell otherSpell)
			{
				if (otherSpell == null)
					return false;

				if (otherSpell.ID == m_spell.ID)
					return true;

				if (otherSpell.SharedTimerGroup != 0 && (otherSpell.SharedTimerGroup == m_spell.SharedTimerGroup))
					return true;

				return false;
			}
		}

		/// <summary>
		/// Select all targets for this spell
		/// </summary>
		/// <param name="castTarget"></param>
		/// <returns></returns>
		public virtual IList<GameLiving> SelectTargets(GameObject castTarget)
		{
			List<GameLiving> list = new();
			GameLiving target = castTarget as GameLiving;
			eSpellTarget modifiedTarget = Spell.Target;
			ushort modifiedRadius = (ushort) Spell.Radius;

			if (modifiedTarget is eSpellTarget.PET && !HasPositiveEffect)
				modifiedTarget = eSpellTarget.ENEMY;

			switch (modifiedTarget)
			{
				case eSpellTarget.AREA:
				{
					if (Spell.SpellType is eSpellType.SummonAnimistPet or eSpellType.SummonAnimistFnF)
						list.Add(Caster);
					else if (modifiedRadius > 0)
					{
						foreach (GamePlayer player in WorldMgr.GetPlayersCloseToSpot(Caster.CurrentRegionID, Caster.GroundTarget, modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							{
								// Apply Mentalist RA5L
								SelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<SelectiveBlindnessEffect>();
								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;

									if (EffectOwner == player)
										(Caster as GamePlayer)?.Out.SendMessage($"{player.GetName(0, true)} is invisible to you!", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
									else
										list.Add(player);
								}
								else
									list.Add(player);
							}
						}

						foreach (GameNPC npc in WorldMgr.GetNPCsCloseToSpot(Caster.CurrentRegionID, Caster.GroundTarget, modifiedRadius))
						{
							if (npc is GameStorm)
								list.Add(npc);
							else if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							{
								if (!npc.HasAbility("DamageImmunity"))
									list.Add(npc);
							}
						}
					}

					break;
				}
				case eSpellTarget.CORPSE:
				{
					if (target == null || target.IsAlive)
						break;

					if (!IsAllowedTarget(target))
						break;

					list.Add(target);
					break;
				}
				case eSpellTarget.PET:
				{
					// PBAE spells.
					if (modifiedRadius > 0 && Spell.Range == 0)
					{
						foreach (GameNPC npcInRadius in Caster.GetNPCsInRadius(modifiedRadius))
						{
							if (Caster.IsControlledNPC(npcInRadius))
								list.Add(npcInRadius);
						}

						return list;
					}

					if (target == null)
						break;

					GameNPC pet = target as GameNPC;

					if (pet != null && Caster.IsWithinRadius(pet, Spell.Range))
					{
						if (Caster.IsControlledNPC(pet))
							list.Add(pet);
					}

					// Check 'ControlledBrain' if 'target' isn't a valid target.
					if (list.Count == 0 && Caster.ControlledBrain != null)
					{
						if (Caster is GamePlayer player && player.CharacterClass.Name.Equals("bonedancer", StringComparison.OrdinalIgnoreCase))
						{
							foreach (GameNPC npcInRadius in player.GetNPCsInRadius((ushort) Spell.Range))
							{
								if (npcInRadius is CommanderPet commander && commander.Owner == player)
									list.Add(commander);
								else if (npcInRadius is BdSubPet {Brain: IControlledBrain brain} subpet && brain.GetPlayerOwner() == player)
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
						foreach (GameNPC npcInRadius in pet.GetNPCsInRadius(modifiedRadius))
						{
							if (npcInRadius == pet || !Caster.IsControlledNPC(npcInRadius) || npcInRadius.Brain is BomberBrain)
								continue;

							list.Add(npcInRadius);
						}
					}

					break;
				}
				case eSpellTarget.ENEMY:
				{
					if (modifiedRadius > 0)
					{
						if (Spell.SpellType != eSpellType.TurretPBAoE && (target == null || Spell.Range == 0))
							target = Caster;

						if (target == null)
							return null;

						foreach (GamePlayer player in target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							{
								SelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<SelectiveBlindnessEffect>();

								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;

									if (EffectOwner == player)
										(Caster as GamePlayer)?.Out.SendMessage($"{player.GetName(0, true)} is invisible to you!", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
									else
										list.Add(player);
								}
								else
									list.Add(player);
							}
						}

						foreach (GameNPC npc in target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							{
								if (!npc.HasAbility("DamageImmunity"))
									list.Add(npc);
							}
						}
					}
					else
					{
						if (target == null)
							break;

						if (!IsAllowedTarget(target))
							break;

						if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
						{
							// Apply Mentalist RA5L
							if (Spell.Range > 0)
							{
								SelectiveBlindnessEffect SelectiveBlindness = Caster.EffectList.GetOfType<SelectiveBlindnessEffect>();

								if (SelectiveBlindness != null)
								{
									GameLiving EffectOwner = SelectiveBlindness.EffectSource;

									if (EffectOwner == target)
										(Caster as GamePlayer)?.Out.SendMessage($"{target.GetName(0, true)} is invisible to you!", eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
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
				case eSpellTarget.REALM:
				{
					if (modifiedRadius > 0)
					{
						if (target == null || Spell.Range == 0)
							target = Caster;

						foreach (GamePlayer player in target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsSameRealm(Caster, player, true))
							{
								if (player.ControlledBrain is NecromancerPetBrain necromancerPetBrain)
								{
									if (Spell.IsBuff)
										list.Add(player);
									else
										list.Add(necromancerPetBrain.Body);
								}
								else
									list.Add(player);
							}
						}

						foreach (GameNPC npc in target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsSameRealm(Caster, npc, true))
							{
								if (npc.Brain is BomberBrain)
									continue;

								list.Add(npc);
							}
						}
					}
					else
					{
						if (target == null)
							break;

						if (!IsAllowedTarget(target))
							break;

						if (GameServer.ServerRules.IsSameRealm(Caster, target, true))
						{
							if (target is GamePlayer player && player.ControlledBrain is NecromancerPetBrain necromancerPetBrain)
							{
								// Only buffs, Necromancer's power transfer, teleport spells, and heals when the pet is already at 100% can be casted on the shade.
								if (Spell.IsBuff ||
									Spell.SpellType is eSpellType.PowerTransferPet or eSpellType.UniPortal ||
									(Spell.IsHealing && Spell.Value > 0 && necromancerPetBrain.Body.HealthPercent >= 100))
								{
									list.Add(player);
								}
								else
									list.Add(player.ControlledBrain.Body);
							}
							else
								list.Add(target);
						}
					}

					break;
				}
				case eSpellTarget.SELF:
				{
					if (modifiedRadius > 0)
					{
						if (target == null || Spell.Range == 0)
							target = Caster;

						foreach (GamePlayer player in target.GetPlayersInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, player, true) == false)
								list.Add(player);
						}

						foreach (GameNPC npc in target.GetNPCsInRadius(modifiedRadius))
						{
							if (GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true) == false)
								list.Add(npc);
						}
					}
					else
						list.Add(Caster);

					break;
				}
				case eSpellTarget.GROUP:
				{
					Group group = m_caster.Group;
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
								GameNPC petBody2 = npc.Body;

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
						else if (m_caster is GameNPC && (m_caster as GameNPC).Brain is ControlledMobBrain casterBrain)
						{
							GamePlayer player = casterBrain.GetPlayerOwner();

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
						}
						else
							list.Add(m_caster);
					}

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
									GameNPC petBody2 = npc.Body;

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
				case eSpellTarget.CONE:
				{
					target = Caster;

					foreach (GamePlayer player in target.GetPlayersInRadius((ushort) Spell.Range))
					{
						if (player == Caster)
							continue;

						if (!m_caster.IsObjectInFront(player, (Spell.Radius != 0 ? Spell.Radius : 100)))
							continue;

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							continue;

						list.Add(player);
					}

					foreach (GameNPC npc in target.GetNPCsInRadius((ushort) Spell.Range))
					{
						if (npc == Caster)
							continue;

						if (!m_caster.IsObjectInFront(npc, (Spell.Radius != 0 ? Spell.Radius : 100)))
							continue;

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, npc, true))
							continue;

						if (!npc.HasAbility("DamageImmunity"))
							list.Add(npc);
					}

					break;
				}
			}

			return list;

			bool IsAllowedTarget(GameLiving target)
			{
				if (target is GameKeepDoor or GameKeepComponent && Spell.SpellType is not eSpellType.SiegeDirectDamage or eSpellType.SiegeArrow && !IsSummoningSpell)
				{
					MessageToCaster($"Your spell has no effect on the {target.Name}.", eChatType.CT_SpellResisted);
					return false;
				}

				return true;
			}
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
				CancelFocusSpells(false);
				return false;
			}

			if (Spell.SpellType is not eSpellType.TurretPBAoE && Spell.IsPBAoE)
				Target = Caster;
			else if (Target == null)
				Target = target;

			if (Target != null)
			{
				if (Spell.IsFocus && (!Target.IsAlive || !Caster.IsWithinRadius(Target, Spell.Range)))
				{
					CancelFocusSpells(false);
					return false;
				}

				if (HasPositiveEffect && Target is GamePlayer p && Caster is GamePlayer c && Target != Caster && p.NoHelp)
				{
					c.Out.SendMessage(Target.Name + " has chosen to walk the path of solitude, and your spell fails.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
					return false;
				}
			}

			IList<GameLiving> targets;
			if (Spell.Target == eSpellTarget.REALM
				&& (Target == Caster || Caster is NecromancerPet nPet && Target == nPet.Owner)
				&& !Spell.IsConcentration
				&& !Spell.IsHealing
				&& Spell.IsBuff
				&& Spell.SpellType != eSpellType.Bladeturn
				&& Spell.SpellType != eSpellType.Bomber)
				targets = GetGroupAndPets(Spell);
			else
				targets = SelectTargets(Target);

			CasterEffectiveness = Caster.Effectiveness;

			/// [Atlas - Takii] No effectiveness drop in OF MOC.
// 			if (Caster.EffectList.GetOfType<MasteryofConcentrationEffect>() != null)
// 			{
// 				AtlasOF_MasteryofConcentration ra = Caster.GetAbility<AtlasOF_MasteryofConcentration>();
// 				if (ra != null && ra.Level > 0)
// 				{
// 					_casterEffectiveness *= System.Math.Round((double)ra.GetAmountForLevel(ra.Level) / 100, 2);
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
// 						_casterEffectiveness *= System.Math.Round((double)necroRA.GetAmountForLevel(necroRA.Level) / 100, 2);
// 					}
// 				}
// 			}

			if (Caster is GamePlayer && (Caster as GamePlayer).CharacterClass.ID == (int)eCharacterClass.Warlock && m_spell.IsSecondary)
			{
				Spell uninterruptibleSpell = Caster.TempProperties.GetProperty<Spell>(UninterruptableSpellHandler.WARLOCK_UNINTERRUPTABLE_SPELL);

				if (uninterruptibleSpell != null && uninterruptibleSpell.Value > 0)
				{
					CasterEffectiveness *= 1 - uninterruptibleSpell.Value * 0.01;
					Caster.TempProperties.RemoveProperty(UninterruptableSpellHandler.WARLOCK_UNINTERRUPTABLE_SPELL);
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
					if (Spell.Target == eSpellTarget.AREA)
						DistanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster.GroundTarget), Spell.Radius);
					else if (Spell.Target == eSpellTarget.CONE)
						DistanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster), Spell.Range);
					else
						DistanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Target), Spell.Radius);

					ApplyEffectOnTarget(targetInList);
				}

				if (Spell.IsConcentration && Caster is GameNPC npc && npc.Brain is ControlledMobBrain npcBrain && Spell.IsBuff)
					npcBrain.AddBuffedTarget(Target);
			}

			CastSubSpells(Target);
			return true;
		}

		protected virtual double CalculateDistanceFallOff(int distance, int radius)
		{
			return distance / (double) radius;
		}

		protected virtual double CalculateDamageEffectiveness()
		{
			if (Caster is GamePlayer playerCaster)
				return CasterEffectiveness * 1 + playerCaster.GetModified(eProperty.SpellDamage) * 0.01;
			else
				return CasterEffectiveness;
		}

		protected virtual double CalculateBuffDebuffEffectiveness()
		{
			double effectiveness;

			if (SpellLine.KeyName is GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Item_Effects or GlobalSpellsLines.Combat_Styles_Effect or GlobalSpellsLines.Realm_Spells || Spell.Level <= 0)
				effectiveness = 1.0;
			else if (Spell.IsBuff)
				effectiveness = 1 + m_caster.GetModified(eProperty.BuffEffectiveness) * 0.01;
			else if (Spell.IsDebuff)
			{
				effectiveness = 1 + m_caster.GetModified(eProperty.DebuffEffectiveness) * 0.01;
				effectiveness *= GetDebuffEffectivenessCriticalModifier();
			}
			else
				effectiveness = 1.0; // Neither a potion, item, buff, or debuff.

			if (Caster is GamePlayer playerCaster && playerCaster.UseDetailedCombatLog && effectiveness != 1)
				playerCaster.Out.SendMessage($"Effectiveness (bonus / crit): {effectiveness:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

			return effectiveness;
		}

		protected virtual double GetDebuffEffectivenessCriticalModifier()
		{
			if (Util.Chance(Caster.DebuffCriticalChance))
			{
				double min = 0.1;
				double max = 1.0;
				double criticalModifier = min + Util.RandomDoubleIncl() * (max - min);
				(Caster as GamePlayer)?.Out.SendMessage($"Your {Spell.Name} critically debuffs the enemy for {criticalModifier * 100:0}% additional effect!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
				return 1.0 + criticalModifier;
			}
			else
				return 1.0;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		protected virtual int CalculateEffectDuration(GameLiving target)
		{
			if (Spell.Duration == 0)
				return 0;

			double effectiveness = CasterEffectiveness;

			// Duration is reduced for AoE spells based on the distance from the center, but only in RvR combat and if the spell doesn't have a damage component.
			if (DistanceFallOff > 0 && Spell.Damage == 0 && (target is GamePlayer || (target is GameNPC npcTarget && npcTarget.Brain is IControlledBrain)))
				effectiveness *= 1 - DistanceFallOff / 2;

			double duration = Spell.Duration * (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);

			if (Spell.InstrumentRequirement != 0)
			{
				DbInventoryItem instrument = Caster.ActiveWeapon;

				if (instrument != null)
				{
					duration *= 1.0 + Math.Min(1.0, instrument.Level / (double) Caster.Level); // Up to 200% duration for songs.
					duration *= instrument.Condition / (double) instrument.MaxCondition * instrument.Quality / 100;
				}
			}

			duration *= effectiveness;

			if (duration < 1)
				duration = 1;
			else if (duration > (Spell.Duration * 4))
				duration = Spell.Duration * 4;

			return (int) duration;
		}

		/// <summary>
		/// Creates the corresponding spell effect for the spell
		/// </summary>
		protected virtual GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			int freq = Spell != null ? Spell.Frequency : 0;
			return new GameSpellEffect(this, CalculateEffectDuration(target), freq, effectiveness);
		}

		public virtual void ApplyEffectOnTarget(GameLiving target)
		{
			// Potion and item effects aren't character abilities and so shouldn't be affected by effectiveness.
			if (m_spellLine.KeyName is GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Item_Effects)
				CasterEffectiveness = 1.0;

			if ((Spell.Duration > 0 && Spell.Target is not eSpellTarget.AREA) || Spell.Concentration > 0)
				OnDurationEffectApply(target);
			else
				OnDirectEffect(target);

			if (!HasPositiveEffect)
			{
				AttackData ad = new()
				{
					Attacker = Caster,
					Target = target,
					AttackType = AttackData.eAttackType.Spell,
					SpellHandler = this,
					AttackResult = eAttackResult.HitUnstyled,
					IsSpellResisted = false,
					Damage = (int) Spell.Damage,
					DamageType = Spell.DamageType
				};

				m_lastAttackData = ad;
				Caster.OnAttackEnemy(ad);

				// Harmful spells that deal no damage (ie. debuffs) should still trigger OnAttackedByEnemy.
				// Exception for DoTs here since the initial landing of the DoT spell reports 0 damage
				// and the first tick damage is done by the pulsing effect, which takes care of firing OnAttackedByEnemy.
				if (ad.Damage == 0 && ad.SpellHandler.Spell.SpellType is not eSpellType.DamageOverTime)
					target.OnAttackedByEnemy(ad);
			}
		}

		/// <summary>
		/// Determines wether this spell is compatible with given spell
		/// and therefore overwritable by better versions
		/// spells that are overwritable cannot stack
		/// </summary>
		/// <param name="compare"></param>
		/// <returns></returns>
		public virtual bool IsOverwritable(ECSGameSpellEffect compare)
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

		public void OnDurationEffectApply(GameLiving target)
		{
			if (!target.IsAlive || target.effectListComponent == null)
				return;

			ECSGameSpellEffect effect = CreateECSEffect(new ECSGameEffectInitParams(target, CalculateEffectDuration(target), CalculateBuffDebuffEffectiveness(), this));

			if (PulseEffect != null)
				PulseEffect.ChildEffects[target] = effect;
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
		public virtual double CalculateToHitChance(GameLiving target)
		{
			int spellLevel;
			GamePlayer playerCaster = m_caster as GamePlayer;

			if (m_spellLine.KeyName is GlobalSpellsLines.Item_Effects && m_spellItem != null)
				spellLevel = m_spellItem.Template.LevelRequirement > 0 ? m_spellItem.Template.LevelRequirement : m_spellItem.Level;
			else if (m_spellLine.KeyName is GlobalSpellsLines.Realm_Spells or GlobalSpellsLines.Reserved_Spells || playerCaster == null)
				spellLevel = m_caster.EffectiveLevel; // NPCs go there too.
			else
			{
				spellLevel = Spell.Level + m_caster.GetModified(eProperty.SpellLevel);

				if (spellLevel > playerCaster.MaxLevel)
					spellLevel = playerCaster.MaxLevel;

				if (m_spellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect || m_spellLine.KeyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
				{
					AttackData lastAD = playerCaster.TempProperties.GetProperty<AttackData>("LastAttackData");
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

			Note:  The last section about maintaining a chance to hit of 55% has been proven incorrect with live testing.
			 */

			// 12.5% resist rate based on live tests done for Uthgard.
			double hitChance = 87.5 + (spellLevel - target.Level) / 2.0;
			hitChance += m_caster.GetModified(eProperty.ToHitBonus);

			if (playerCaster == null || target is not GamePlayer)
			{
				// 1 per level difference.
				hitChance += m_caster.EffectiveLevel - target.EffectiveLevel;
				hitChance += Math.Max(0, target.attackComponent.Attackers.Count - 1) * Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
			}

			if (m_caster.effectListComponent.ContainsEffectForEffectType(eEffect.PiercingMagic))
			{
				ECSGameEffect effect = m_caster.effectListComponent.GetSpellEffects().FirstOrDefault(e => e.EffectType == eEffect.PiercingMagic);

				if (effect != null)
					hitChance += effect.SpellHandler.Spell.Value;
			}

			// Check for active RAs.
			if (m_caster.effectListComponent.ContainsEffectForEffectType(eEffect.MajesticWill))
			{
				ECSGameEffect effect = m_caster.effectListComponent.GetAllEffects().FirstOrDefault(e => e.EffectType == eEffect.MajesticWill);

				if (effect != null)
					hitChance += effect.Effectiveness * 5;
			}

			return hitChance;
		}

		/// <summary>
		/// Calculates chance of spell getting resisted
		/// </summary>
		/// <param name="target">the target of the spell</param>
		/// <returns>chance that spell will be resisted for specific target</returns>
		public virtual double CalculateSpellResistChance(GameLiving target)
		{
			if (HasPositiveEffect)
				return 0;

			return 100 - CalculateToHitChance(target);
		}

		protected virtual bool CheckSpellResist(GameLiving target)
		{
			double spellResistChance = CalculateSpellResistChance(target);

			if (spellResistChance > 0)
			{
				double spellResistRoll;

				if (!Properties.OVERRIDE_DECK_RNG && Caster is GamePlayer player)
					spellResistRoll = player.RandomNumberDeck.GetPseudoDouble();
				else
					spellResistRoll = Util.CryptoNextDouble();

				spellResistRoll *= 100;

				if (Caster is GamePlayer playerCaster && playerCaster.UseDetailedCombatLog)
					playerCaster.Out.SendMessage($"Target chance to resist: {spellResistChance:0.##} RandomNumber: {spellResistRoll:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

				if (target is GamePlayer playerTarget && playerTarget.UseDetailedCombatLog)
					playerTarget.Out.SendMessage($"Your chance to resist: {spellResistChance:0.##} RandomNumber: {spellResistRoll:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

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
		protected virtual void OnSpellResisted(GameLiving target)
		{
			SendSpellResistAnimation(target);
			SendSpellResistMessages(target);
			SendSpellResistNotification(target);
			StartSpellResistInterruptTimer(target);
			StartSpellResistLastAttackTimer(target);
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
			if (target is GameNPC npcTarget)
			{
				if (npcTarget.Brain is IControlledBrain npcTargetBrain)
				{
					GamePlayer owner = npcTargetBrain.GetPlayerOwner();

					if (owner != null)
						this.MessageToLiving(owner, eChatType.CT_SpellResisted, "Your {0} resists the effect!", target.Name);
				}
			}
			else
				MessageToLiving(target, "You resist the effect!", eChatType.CT_SpellResisted);

			// Deliver message to the caster as well.
			this.MessageToCaster(eChatType.CT_SpellResisted, "{0} resists the effect!" + " (" + CalculateSpellResistChance(target).ToString("0.0") + "%)", target.GetName(0, true));
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
			ad.AttackType = AttackData.eAttackType.Spell;
			ad.SpellHandler = this;
			ad.AttackResult = eAttackResult.Missed;
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
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
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
		public void MessageToCaster(string message, eChatType type)
		{
			if (Caster is GamePlayer playerCaster)
				playerCaster.MessageToSelf(message, type);
			else if (Caster is GameNPC npcCaster && npcCaster.Brain is IControlledBrain npcCasterBrain
					 && (type is eChatType.CT_YouHit or eChatType.CT_SpellResisted or eChatType.CT_Spell))
			{
				GamePlayer playerOwner = npcCasterBrain.GetPlayerOwner();
				playerOwner?.MessageToSelf(message, type);
			}
		}

		/// <summary>
		/// sends a message to a living
		/// </summary>
		public void MessageToLiving(GameLiving living, string message, eChatType type)
		{
			if (message != null && message.Length > 0)
			{
				living.MessageToSelf(message, type);
			}
		}

		public virtual void CancelFocusSpells(bool moving)
		{
			CastState = eCastState.Cleanup;
			bool cancelled = false;

			foreach (ECSGameSpellEffect pulseSpell in Caster.effectListComponent.GetSpellEffects(eEffect.Pulse))
			{
				if (!pulseSpell.SpellHandler.Spell.IsFocus)
					continue;

				if (EffectService.RequestImmediateCancelEffect(pulseSpell))
					cancelled = true;

				foreach (ECSGameEffect petEffect in pulseSpell.SpellHandler.Target.effectListComponent.GetSpellEffects(eEffect.FocusShield))
				{
					if (petEffect.SpellHandler.Spell.IsFocus)
						EffectService.RequestImmediateCancelEffect(petEffect);
				}
			}

			if (cancelled)
			{
				if (moving)
					MessageToCaster("You move and interrupt your focus!", eChatType.CT_Important);
				else
					MessageToCaster($"You lose your focus on your {Spell.Name} spell.", eChatType.CT_SpellExpires);
			}
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
			get { return CastState == eCastState.Casting; }//return m_castTimer != null && m_castTimer.IsAlive; }
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

		public virtual ECSPulseEffect PulseEffect { get; private set; }

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
				//list.Add("Function: " + (Spell.SpellType == string.Empty ? "(not implemented)" : Spell.SpellType));
				//list.Add(" "); //empty line
				GamePlayer p = null;

				if (Caster is GamePlayer || Caster is GameNPC && (Caster as GameNPC).Brain is IControlledBrain &&
				((Caster as GameNPC).Brain as IControlledBrain).GetPlayerOwner() != null)
				{
					p = Caster is GamePlayer ? (Caster as GamePlayer) : ((Caster as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
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
				if (Spell.DamageType != eDamageType.Natural)
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

		public virtual double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
		{
			// Was previously 2% per level difference, but this didn't match live results at lower level.
			// Assuming 2% was correct at level 50, it means it should be dynamic, either based on the caster's level of the target's.
			// This new formula increases the modifier the lower the level of the caster is: 10% at level 0, 2% at level 50.
			return (caster.Level - target.Level) * (10 - caster.Level * 0.16) * 0.01;
		}

		/// <summary>
		/// Calculates min damage variance %
		/// </summary>
		/// <param name="target">spell target</param>
		/// <param name="min">returns min variance</param>
		/// <param name="max">returns max variance</param>
		public virtual void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			// Vanesyra lays out variance calculations here: https://www.ignboards.com/threads/melee-speed-melee-and-style-damage-or-why-pure-grothrates-are-wrong.452406879/page-3
			// However, this results in an extremely low variance at low level without adjusting some parameters, and it doesn't seem to match live results.
			// It's possible live changed how variance is calculated at some point, but this would need to be proven.

			GameLiving casterToUse = m_caster;

			switch (m_spellLine.KeyName)
			{
				// Further research should be done on these.
				// The variance range is tied to the base damage calculation.
				case GlobalSpellsLines.Mob_Spells:
				case GlobalSpellsLines.Combat_Styles_Effect:
				case GlobalSpellsLines.Nightshade:
				{
					// Mob spells are modified by acuity stats.
					// Style effects use a custom damage calculation currently expecting the upper bound to be 1.0.
					// Nightshade spells aren't tied to any trainable specialization and thus require a fixed variance.
					// Lower bound is similar to what the variance calculation would return if we used 31 for the specialization and 50 for the target level.
					max = 1.0;
					min = 0.6;
					break;
				}
				case GlobalSpellsLines.Item_Effects:
				case GlobalSpellsLines.Potions_Effects:
				{
					// Procs and charges normally aren't modified by any stat, but are shown to be able to do about 25% more damage than their base value.
					max = 1.25;
					min = UseMinVariance ? 1.25 : 0.75; // 0.6 * 1.25
					break;
				}
				case GlobalSpellsLines.Reserved_Spells:
				{
					max = 1.0;
					min = 1.0;
					break;
				}
				default:
				{
					max = 1.0;

					if (target.Level <= 0)
						min = max;
					else
					{
						// Spells casted by a necromancer pet use the owner's spec.
						if (m_caster is NecromancerPet necromancerPet && necromancerPet.Brain is IControlledBrain brain)
							casterToUse = brain.GetPlayerOwner();

						min = (casterToUse.GetModifiedSpecLevel(m_spellLine.Spec) - 1) / (double) target.Level;
					}

					break;
				}
			}

			// 0.2 is a guess.
			double varianceOffset = CalculateDamageVarianceOffsetFromLevelDifference(casterToUse, target);
			max = Math.Max(0.2, max + varianceOffset);
			min = Math.Clamp(min + varianceOffset, 0.2, max);
		}

		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		public virtual double CalculateDamageBase(GameLiving target)
		{
			double spellDamage = Spell.Damage;

			if (Spell.SpellType is eSpellType.Lifedrain)
				spellDamage *= 1 + Spell.LifeDrainReturn * 0.001;

			// Combat style effects have their own calculation, using weapon spec and stat.
			// Item effects (procs, charges), potion effects, and poisons don't use stats.
			if (SpellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect)
			{
				int weaponStat = Caster.GetWeaponStat(Caster.ActiveWeapon);
				double weaponSkillScalar = (3 + 0.02 * weaponStat) / (1 + 0.005 * weaponStat);
				spellDamage *= (Caster.GetWeaponSkill(Caster.ActiveWeapon) * weaponSkillScalar / 3.0 + 100) / 200.0;
				return Math.Max(0, spellDamage);
			}
			else if (SpellLine.KeyName is GlobalSpellsLines.Item_Effects or GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Mundane_Poisons or GlobalSpellsLines.Realm_Spells)
				return Math.Max(0, spellDamage);

			// Stats are only partially transferred to the necromancer pet, so we don't use its intelligence at all.
			// Other pets use their own stats and level.
			GameLiving modifiedCaster = Caster is NecromancerPet necromancerPet ? necromancerPet.Owner : Caster;
			double acuity = 0.0;
			double specBonus = 0.0;

			if (modifiedCaster is GameNPC)
				acuity = modifiedCaster.GetModified(eProperty.Intelligence);
			else if (modifiedCaster is GamePlayer playerCaster)
			{
				switch ((eCharacterClass) playerCaster.CharacterClass.ID)
				{
					case eCharacterClass.MaulerAlb:
					case eCharacterClass.MaulerMid:
					case eCharacterClass.MaulerHib:
					case eCharacterClass.Vampiir:
						break;
					case eCharacterClass.Nightshade:
					{
						// Special rule for Nightshade.
						// Spell damage seems to be based on strength around 1.65, but the mana stat is dexterity.
						// 1.62 made them benefit from Augmented Acuity, but the calculator isn't adding it to prevent melee damage from increasing too, so we have to do it here.
						acuity = playerCaster.GetModified((eProperty) playerCaster.Strength) + playerCaster.AbilityBonus[eProperty.Acuity];
						break;
					}
					default:
					{
						if (playerCaster.CharacterClass.ManaStat is not eStat.UNDEFINED)
							acuity = playerCaster.GetModified((eProperty) playerCaster.CharacterClass.ManaStat);

						specBonus = playerCaster.ItemBonus[SkillBase.SpecToSkill(m_spellLine.Spec)]; // Only item bonus increases damage.
						break;
					}
				}
			}

			// A nerf of about 10% to spell damage was supposedly applied around 2014. We are not applying it.
			// This formula is also somewhat inaccurate. Even with that nerf applied, the intelligence modifier is too high when comparing damage on live at low level.
			spellDamage *= (1 + acuity * 0.005) * (1 + specBonus * 0.005);
			return Math.Max(0, spellDamage);
		}

		/// <summary>
		/// Adjust damage based on chance to hit.
		/// </summary>
		public virtual double AdjustDamageForHitChance(double damage, double hitChance)
		{
			if (hitChance < 55)
				damage += (hitChance - 55) * Properties.SPELL_HITCHANCE_DAMAGE_REDUCTION_MULTIPLIER * 0.01;

			return damage;
		}

		public virtual AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = new()
			{
				Attacker = m_caster,
				Target = target,
				AttackType = AttackData.eAttackType.Spell,
				SpellHandler = this,
				AttackResult = eAttackResult.HitUnstyled
			};

			GamePlayer playerCaster = Caster as GamePlayer;

			CalculateDamageVariance(target, out double minVariance, out double maxVariance);
			double baseDamage = CalculateDamageBase(target);
			double spellDamage = baseDamage;
			double effectiveness = CalculateDamageEffectiveness();

			// Relic bonus is applied to damage directly instead of effectiveness (does not increase cap)
			// This applies to bleeds. Is that intended?
			spellDamage *= 1.0 + RelicMgr.GetRelicBonusModifier(Caster.Realm, eRelicType.Magic);
			spellDamage *= effectiveness;

			if (DistanceFallOff > 0)
				spellDamage *= 1 - DistanceFallOff;

			double variance = minVariance + Util.RandomDoubleIncl() * (maxVariance - minVariance);
			double finalDamage = spellDamage * variance;

			// Live testing done Summer 2009 by Bluraven, Tolakram. Levels 40, 45, 50, 55, 60, 65, 70.
			// Damage reduced by chance < 55, no extra damage increase noted with hitchance > 100.
			double hitChance = CalculateToHitChance(ad.Target);
			finalDamage = AdjustDamageForHitChance(finalDamage, hitChance);

			if (playerCaster != null || (Caster is GameNPC casterNpc && casterNpc.Brain is IControlledBrain && Caster.Realm != 0))
			{
				if (target is GamePlayer)
					finalDamage *= Properties.PVP_SPELL_DAMAGE;
				else if (target is GameNPC)
					finalDamage *= Properties.PVE_SPELL_DAMAGE;
			}

			// Calculate resistances and conversion.
			finalDamage = ModifyDamageWithTargetResist(ad, finalDamage);
			double conversionMod = AttackComponent.CalculateTargetConversion(ad.Target);
			double preConversionDamage = finalDamage;
			finalDamage *= conversionMod;
			ad.Modifier += (int) Math.Floor(finalDamage - preConversionDamage);

			// Apply damage cap.
			finalDamage = Math.Min(finalDamage, DamageCap(effectiveness));

			// Apply conversion.
			if (conversionMod < 1)
			{
				double conversionAmount = conversionMod > 0 ? finalDamage / conversionMod - finalDamage : finalDamage;
				AttackComponent.ApplyTargetConversionRegen(ad.Target, (int) conversionAmount);
			}

			if (finalDamage < 0)
				finalDamage = 0;

			// DoTs can only crit with Wild Arcana. This is handled by the DoTSpellHandler directly.
			int criticalDamage = 0;
			int criticalChance = this is not DoTSpellHandler ? Math.Min(50, m_caster.SpellCriticalChance) : 0;
			int randNum = Util.CryptoNextInt(0, 100);

			if (playerCaster != null && playerCaster.UseDetailedCombatLog)
			{
				if (criticalChance > 0)
					playerCaster.Out.SendMessage($"Spell crit chance: {criticalChance:0.##} random: {randNum:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

				playerCaster.Out.SendMessage($"BaseDamage: {baseDamage:0.##} | Variance: {variance:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
			}

			if (criticalChance > randNum && finalDamage > 0)
			{
				int criticalMax = ad.Target is GamePlayer ? (int) finalDamage / 2 : (int) finalDamage;
				criticalDamage = Util.Random((int) finalDamage / 10, criticalMax);
			}

			ad.Damage = (int) finalDamage;
			ad.CriticalDamage = criticalDamage;
			ad.CriticalChance = criticalChance;
			ad.Target.ModifyAttack(ad); // Attacked living may modify the attack data. Primarily used for keep doors and components.
			m_lastAttackData = ad;
			return ad;
		}

		public virtual double ModifyDamageWithTargetResist(AttackData ad, double damage)
		{
			// Since 1.65 there are different categories of resist.
			// - First category contains Item / Race/ Buff / RvrBanners resists.
			// - Second category contains resists that are obtained from RAs such as Avoidance of Magic and Brilliant Aura of Deflection.
			// However the second category affects ONLY the spell damage. Not the duration, not the effectiveness of debuffs.
			// For spell damage, the calculation is 'finaldamage * firstCategory * secondCategory'.
			// -> Remark for the future: VampirResistBuff is Category2 too.

			eDamageType damageType = DetermineSpellDamageType();
			eProperty property = ad.Target.GetResistTypeForDamage(damageType);
			int primaryResistModifier = ad.Target.GetResist(damageType);
			int secondaryResistModifier = Math.Min(80, ad.Target.SpecBuffBonusCategory[(int) property]);

			// Resist Pierce is a special bonus which has been introduced with ToA.
			// It reduces the resistance that the victim receives through items by the specified percentage.
			// http://de.daocpedia.eu/index.php/Resistenz_durchdringen (translated)
			int resistPierce = Caster.GetModified(eProperty.ResistPierce);

			// Subtract max ItemBonus of property of target, but at least 0.
			if (resistPierce > 0 && Spell.SpellType != eSpellType.Archery)
				primaryResistModifier -= Math.Max(0, Math.Min(ad.Target.ItemBonus[(int) property], resistPierce));

			double resistModifier = damage * primaryResistModifier * -0.01;
			resistModifier += (damage + resistModifier) * secondaryResistModifier * -0.01;
			damage += resistModifier;

			// Update AttackData.
			ad.Modifier = (int) Math.Floor(resistModifier);
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
		public virtual eDamageType DetermineSpellDamageType()
		{
			return Spell.DamageType;
		}

		/// <summary>
		/// Sends damage text messages but makes no damage
		/// </summary>
		/// <param name="ad"></param>
		public virtual void SendDamageMessages(AttackData ad)
		{
			string modMessage = string.Empty;

			if (ad.Modifier > 0)
				modMessage = $" (+{ad.Modifier})";
			else if (ad.Modifier < 0)
				modMessage = $" ({ad.Modifier})";

			if (Caster is GamePlayer or NecromancerPet)
				MessageToCaster(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modMessage), eChatType.CT_YouHit);
			else if (Caster is GameNPC)
				MessageToCaster(string.Format("Your {0} hits {1} for {2}{3} damage!", Caster.Name, ad.Target.GetName(0, false), ad.Damage, modMessage), eChatType.CT_YouHit);

			if (ad.CriticalDamage > 0)
				MessageToCaster($"You critically hit for an additional {ad.CriticalDamage} damage! ({ad.CriticalChance}%)", eChatType.CT_YouHit);
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
			ad.AttackResult = eAttackResult.HitUnstyled;

			// Send animation before dealing damage else dying livings show no animation
			if (showEffectAnimation)
				SendEffectAnimation(ad.Target, 0, false, 1);

			ad.Target.OnAttackedByEnemy(ad);
			ad.Attacker.DealDamage(ad);

			if (ad.Damage == 0 && ad.Target is GameNPC targetNpc)
			{
				if (targetNpc.Brain is IOldAggressiveBrain brain)
					brain.AddToAggroList(Caster, 1);

				if (this is not DoTSpellHandler and not StyleBleeding)
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
				dw.AddKeyValuePair("frequency", Spell.SpellType == eSpellType.OffensiveProc || Spell.SpellType == eSpellType.OffensiveProc ? Spell.Frequency / 100 : Spell.Frequency);

			WriteBonus(ref dw);
			WriteParm(ref dw);
			WriteDamage(ref dw);
			WriteSpecial(ref dw);

			if (Spell.HasSubSpell)
				if (Spell.SpellType == eSpellType.Bomber || Spell.SpellType == eSpellType.SummonAnimistFnF)
					dw.AddKeyValuePair("delve_spell", SkillBase.GetSpellByID(Spell.SubSpellID).InternalID);
				else
					dw.AddKeyValuePair("parm", SkillBase.GetSpellByID(Spell.SubSpellID).InternalID);

			if (!dw.Values.ContainsKey("parm") && Spell.SpellType != eSpellType.MesmerizeDurationBuff)
				dw.AddKeyValuePair("parm", "1");
		}

		private string GetDelveType(eSpellType spellType)
		{
			switch (spellType)
			{
				case eSpellType.AblativeArmor:
					return "hit_buffer";
				case eSpellType.AcuityBuff:
				case eSpellType.DexterityQuicknessBuff:
				case eSpellType.StrengthConstitutionBuff:
					return "twostat";
				case eSpellType.Amnesia:
					return "amnesia";
				case eSpellType.ArmorAbsorptionBuff:
					return "absorb";
				case eSpellType.ArmorAbsorptionDebuff:
					return "nabsorb";
				case eSpellType.BaseArmorFactorBuff:
				case eSpellType.SpecArmorFactorBuff:
				case eSpellType.PaladinArmorFactorBuff:
					return "shield";
				case eSpellType.Bolt:
					return "bolt";
				case eSpellType.Bladeturn:
				case eSpellType.CelerityBuff:
				case eSpellType.CombatSpeedBuff:
				case eSpellType.CombatSpeedDebuff:
				case eSpellType.Confusion:
				case eSpellType.Mesmerize:
				case eSpellType.Mez:
				case eSpellType.Nearsight:
				case eSpellType.SavageCombatSpeedBuff:
				case eSpellType.SavageEvadeBuff:
				case eSpellType.SavageParryBuff:
				case eSpellType.SpeedEnhancement:
					return "combat";
				case eSpellType.BodyResistBuff:
				case eSpellType.BodySpiritEnergyBuff:
				case eSpellType.ColdResistBuff:
				case eSpellType.EnergyResistBuff:
				case eSpellType.HeatColdMatterBuff:
				case eSpellType.HeatResistBuff:
				case eSpellType.MatterResistBuff:
				case eSpellType.SavageCrushResistanceBuff:
				case eSpellType.SavageSlashResistanceBuff:
				case eSpellType.SavageThrustResistanceBuff:
				case eSpellType.SpiritResistBuff:
					return "resistance";
				case eSpellType.BodyResistDebuff:
				case eSpellType.ColdResistDebuff:
				case eSpellType.EnergyResistDebuff:
				case eSpellType.HeatResistDebuff:
				case eSpellType.MatterResistDebuff:
				case eSpellType.SpiritResistDebuff:
					return "nresistance";
				case eSpellType.SummonTheurgistPet:
				case eSpellType.Bomber:
				case eSpellType.SummonAnimistFnF:
					return "dsummon";
				case eSpellType.Charm:
					return "charm";
				case eSpellType.CombatHeal:
				case eSpellType.Heal:
					return "heal";
				case eSpellType.ConstitutionBuff:
				case eSpellType.DexterityBuff:
				case eSpellType.StrengthBuff:
				case eSpellType.AllStatsBarrel:
					return "stat";
				case eSpellType.ConstitutionDebuff:
				case eSpellType.DexterityDebuff:
				case eSpellType.StrengthDebuff:
					return "nstat";
				case eSpellType.CureDisease:
				case eSpellType.CurePoison:
				case eSpellType.CureNearsightCustom:
					return "rem_eff_ty";
				case eSpellType.CureMezz:
					return "remove_eff";
				case eSpellType.DamageAdd:
					return "dmg_add";
				case eSpellType.DamageOverTime:
				case eSpellType.StyleBleeding:
					return "dot";
				case eSpellType.DamageShield:
					return "dmg_shield";
				case eSpellType.DamageSpeedDecrease:
				case eSpellType.SpeedDecrease:
				case eSpellType.UnbreakableSpeedDecrease:
					return "snare";
				case eSpellType.DefensiveProc:
					return "def_proc";
				case eSpellType.DexterityQuicknessDebuff:
				case eSpellType.StrengthConstitutionDebuff:
					return "ntwostat";
				case eSpellType.DirectDamage:
					return "direct";
				case eSpellType.DirectDamageWithDebuff:
					return "nresist_dam";
				case eSpellType.Disease:
					return "disease";
				case eSpellType.EnduranceRegenBuff:
				case eSpellType.HealthRegenBuff:
				case eSpellType.PowerRegenBuff:
					return "enhancement";
				case eSpellType.HealOverTime:
					return "regen";
				case eSpellType.Lifedrain:
					return "lifedrain";
				case eSpellType.LifeTransfer:
					return "transfer";
				case eSpellType.MeleeDamageDebuff:
					return "ndamage";
				case eSpellType.MesmerizeDurationBuff:
					return "mez_dampen";
				case eSpellType.OffensiveProc:
					return "off_proc";
				case eSpellType.PetConversion:
					return "reclaim";
				case eSpellType.Resurrect:
					return "raise_dead";
				case eSpellType.SavageEnduranceHeal:
					return "fat_heal";
				case eSpellType.SpreadHeal:
					return "spreadheal";
				case eSpellType.Stun:
					return "paralyze";				
				case eSpellType.SummonCommander:
				case eSpellType.SummonDruidPet:
				case eSpellType.SummonHunterPet:
				case eSpellType.SummonSimulacrum:
				case eSpellType.SummonSpiritFighter:
				case eSpellType.SummonUnderhill:
					return "summon";
				case eSpellType.SummonMinion:
					return "gsummon";
				case eSpellType.SummonNecroPet:
					return "ssummon";
				case eSpellType.StyleCombatSpeedDebuff:
				case eSpellType.StyleStun:
				case eSpellType.StyleSpeedDecrease:				
					return "add_effect";
				case eSpellType.StyleTaunt:
					if (Spell.Value > 0)
						return "taunt";
					else
						return "detaunt";
				case eSpellType.Taunt:
					return "taunt";
				case eSpellType.PetSpell:
				case eSpellType.SummonAnimistPet:
					return "petcast";
				case eSpellType.PetLifedrain:
					return "lifedrain";
				case eSpellType.PowerDrainPet:
					return "powerdrain";
				case eSpellType.PowerTransferPet:
					return "power_xfer";
				case eSpellType.ArmorFactorDebuff:
					return "nshield";
				case eSpellType.Grapple:
					return "Grapple";
				default:
					return "light";

			}
		}

		private void WriteBonus(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case eSpellType.AblativeArmor:
					dw.AddKeyValuePair("bonus", Spell.Damage > 0 ? Spell.Damage : 25);
					break;
				case eSpellType.AcuityBuff:
				case eSpellType.ArmorAbsorptionBuff:
				case eSpellType.ArmorAbsorptionDebuff:
				case eSpellType.BaseArmorFactorBuff:
				case eSpellType.SpecArmorFactorBuff:
				case eSpellType.PaladinArmorFactorBuff:
				case eSpellType.BodyResistBuff:
				case eSpellType.BodyResistDebuff:
				case eSpellType.BodySpiritEnergyBuff:
				case eSpellType.ColdResistBuff:
				case eSpellType.ColdResistDebuff:
				case eSpellType.CombatSpeedBuff:
				case eSpellType.CelerityBuff:
				case eSpellType.ConstitutionBuff:
				case eSpellType.ConstitutionDebuff:
				case eSpellType.DexterityBuff:
				case eSpellType.DexterityDebuff:
				case eSpellType.DexterityQuicknessBuff:
				case eSpellType.DexterityQuicknessDebuff:
				case eSpellType.DirectDamageWithDebuff:
				case eSpellType.EnergyResistBuff:
				case eSpellType.EnergyResistDebuff:
				case eSpellType.HealOverTime:
				case eSpellType.HeatColdMatterBuff:
				case eSpellType.HeatResistBuff:
				case eSpellType.HeatResistDebuff:
				case eSpellType.MatterResistBuff:
				case eSpellType.MatterResistDebuff:
				case eSpellType.MeleeDamageBuff:
				case eSpellType.MeleeDamageDebuff:
				case eSpellType.MesmerizeDurationBuff:
				case eSpellType.PetConversion:
				case eSpellType.SavageCombatSpeedBuff:
				case eSpellType.SavageCrushResistanceBuff:
				case eSpellType.SavageDPSBuff:
				case eSpellType.SavageEvadeBuff:
				case eSpellType.SavageParryBuff:
				case eSpellType.SavageSlashResistanceBuff:
				case eSpellType.SavageThrustResistanceBuff:
				case eSpellType.SpeedEnhancement:
				case eSpellType.SpeedOfTheRealm:
				case eSpellType.SpiritResistBuff:
				case eSpellType.SpiritResistDebuff:
				case eSpellType.StrengthBuff:
				case eSpellType.AllStatsBarrel:
				case eSpellType.StrengthConstitutionBuff:
				case eSpellType.StrengthConstitutionDebuff:
				case eSpellType.StrengthDebuff:
				case eSpellType.ToHitBuff:
				case eSpellType.FumbleChanceDebuff:
				case eSpellType.AllStatsPercentDebuff:
				case eSpellType.CrushSlashThrustDebuff:
				case eSpellType.EffectivenessDebuff:
				case eSpellType.ParryBuff:
				case eSpellType.SavageEnduranceHeal:
				case eSpellType.SlashResistDebuff:
				case eSpellType.ArmorFactorDebuff:
				case eSpellType.WeaponSkillBuff:
				case eSpellType.FlexibleSkillBuff:
					dw.AddKeyValuePair("bonus", Spell.Value);
					break;
				case eSpellType.DamageSpeedDecrease:
				case eSpellType.SpeedDecrease:
				case eSpellType.StyleSpeedDecrease:
				case eSpellType.UnbreakableSpeedDecrease:
					dw.AddKeyValuePair("bonus", 100 - Spell.Value);
					break;
				case eSpellType.DefensiveProc:
				case eSpellType.OffensiveProc:
					dw.AddKeyValuePair("bonus", Spell.Frequency / 100);
					break;
				case eSpellType.Lifedrain:
				case eSpellType.PetLifedrain:
					dw.AddKeyValuePair("bonus", Spell.LifeDrainReturn / 10);
					break;
				case eSpellType.PowerDrainPet:
					dw.AddKeyValuePair("bonus", Spell.LifeDrainReturn);
					break;
				case eSpellType.Resurrect:
					dw.AddKeyValuePair("bonus", Spell.ResurrectMana);
					break;
			}
		}

		private void WriteParm(ref MiniDelveWriter dw)
		{
			string parm = "parm";
			switch (Spell.SpellType)
			{
				case eSpellType.CombatSpeedDebuff:
				
				case eSpellType.DexterityBuff:
				case eSpellType.DexterityDebuff:
				case eSpellType.DexterityQuicknessBuff:
				case eSpellType.DexterityQuicknessDebuff:
				case eSpellType.PowerRegenBuff:
				case eSpellType.StyleCombatSpeedDebuff:
					dw.AddKeyValuePair(parm, "2");
					break;
				case eSpellType.AcuityBuff:
				case eSpellType.ConstitutionBuff:
				case eSpellType.AllStatsBarrel:
				case eSpellType.ConstitutionDebuff:
				case eSpellType.EnduranceRegenBuff:
					dw.AddKeyValuePair(parm, "3");
					break;
				case eSpellType.Confusion:
					dw.AddKeyValuePair(parm, "5");
					break;
				case eSpellType.CureMezz:
				case eSpellType.Mesmerize:
					dw.AddKeyValuePair(parm, "6");
					break;
				case eSpellType.Bladeturn:
					dw.AddKeyValuePair(parm, "9");
					break;
				case eSpellType.HeatResistBuff:
				case eSpellType.HeatResistDebuff:
				case eSpellType.SpeedEnhancement:
					dw.AddKeyValuePair(parm, "10");
					break;
				case eSpellType.ColdResistBuff:
				case eSpellType.ColdResistDebuff:
				case eSpellType.CurePoison:
				case eSpellType.Nearsight:
				case eSpellType.CureNearsightCustom:
					dw.AddKeyValuePair(parm, "12");
					break;
				case eSpellType.MatterResistBuff:
				case eSpellType.MatterResistDebuff:
				case eSpellType.SavageParryBuff:
					dw.AddKeyValuePair(parm, "15");
					break;
				case eSpellType.BodyResistBuff:
				case eSpellType.BodyResistDebuff:
				case eSpellType.SavageEvadeBuff:
					dw.AddKeyValuePair(parm, "16");
					break;
				case eSpellType.SpiritResistBuff:
				case eSpellType.SpiritResistDebuff:
					dw.AddKeyValuePair(parm, "17");
					break;
				case eSpellType.StyleBleeding:
					dw.AddKeyValuePair(parm, "20");
					break;
				case eSpellType.EnergyResistBuff:
				case eSpellType.EnergyResistDebuff:
					dw.AddKeyValuePair(parm, "22");
					break;
				case eSpellType.SpeedOfTheRealm:
					dw.AddKeyValuePair(parm, "35");
					break;
				case eSpellType.CelerityBuff:
				case eSpellType.SavageCombatSpeedBuff:
				case eSpellType.CombatSpeedBuff:
					dw.AddKeyValuePair(parm, "36");
					break;
				case eSpellType.HeatColdMatterBuff:
					dw.AddKeyValuePair(parm, "97");
					break;
				case eSpellType.BodySpiritEnergyBuff:
					dw.AddKeyValuePair(parm, "98");
					break;
				case eSpellType.DirectDamageWithDebuff:
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
				case eSpellType.SavageCrushResistanceBuff:
					dw.AddKeyValuePair(parm, (int)eDamageType.Crush);
					break;
				case eSpellType.SavageSlashResistanceBuff:
					dw.AddKeyValuePair(parm, (int)eDamageType.Slash);
					break;
				case eSpellType.SavageThrustResistanceBuff:
					dw.AddKeyValuePair(parm, (int)eDamageType.Thrust);
					break;
				case eSpellType.DefensiveProc:
				case eSpellType.OffensiveProc:
					dw.AddKeyValuePair(parm, SkillBase.GetSpellByID((int)Spell.Value).InternalID);
					break;
			}
		}

		private void WriteDamage(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case eSpellType.AblativeArmor:
				case eSpellType.CombatHeal:
				case eSpellType.EnduranceRegenBuff:
				case eSpellType.Heal:
				case eSpellType.HealOverTime:
				case eSpellType.HealthRegenBuff:
				case eSpellType.LifeTransfer:
				case eSpellType.PowerRegenBuff:
				case eSpellType.SavageEnduranceHeal:
				case eSpellType.SpreadHeal:
				case eSpellType.Taunt:
					dw.AddKeyValuePair("damage", Spell.Value);
					break;
				case eSpellType.Bolt:
				case eSpellType.DamageAdd:
				case eSpellType.DamageShield:
				case eSpellType.DamageSpeedDecrease:
				case eSpellType.DirectDamage:
				case eSpellType.DirectDamageWithDebuff:
				case eSpellType.Lifedrain:
				case eSpellType.PetLifedrain:
				case eSpellType.PowerDrainPet:
					dw.AddKeyValuePair("damage", Spell.Damage * 10);
					break;
				case eSpellType.DamageOverTime:
				case eSpellType.StyleBleeding:
					dw.AddKeyValuePair("damage", Spell.Damage);
					break;
				case eSpellType.Resurrect:
					dw.AddKeyValuePair("damage", Spell.ResurrectHealth);
					break;
				case eSpellType.StyleTaunt:
					dw.AddKeyValuePair("damage", Spell.Value < 0 ? -Spell.Value : Spell.Value);
					break;
				case eSpellType.PowerTransferPet:
					dw.AddKeyValuePair("damage", Spell.Value * 10);
					break;
				case eSpellType.SummonHunterPet:
				case eSpellType.SummonSimulacrum:
				case eSpellType.SummonSpiritFighter:
				case eSpellType.SummonUnderhill:
					dw.AddKeyValuePair("damage", 44);
					break;
				case eSpellType.SummonCommander:
				case eSpellType.SummonDruidPet:
				case eSpellType.SummonMinion:
					dw.AddKeyValuePair("damage", Spell.Value);
					break;
			}
		}

		private void WriteSpecial(ref MiniDelveWriter dw)
		{
			switch (Spell.SpellType)
			{
				case eSpellType.Bomber:
					//dw.AddKeyValuePair("description_string", "Summon an elemental sprit to fight for the caster briefly.");
						break;
				case eSpellType.Charm:
					dw.AddKeyValuePair("power_level", Spell.Value);

					// var baseMessage = "Attempts to bring the target monster under the caster's control.";
					switch ((CharmSpellHandler.eCharmType)Spell.AmnesiaChance)
					{
						case CharmSpellHandler.eCharmType.All:
							// Message: Attempts to bring the target monster under the caster's control. Spell works on all monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.AllMonsterTypes"));
							break;
						case CharmSpellHandler.eCharmType.Animal:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Animal"));
							break;
						case CharmSpellHandler.eCharmType.Humanoid:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on humanoids. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Humanoid"));
							break;
						case CharmSpellHandler.eCharmType.Insect:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on insects. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.Insect"));
							break;
						case CharmSpellHandler.eCharmType.HumanoidAnimal:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals and humanoids. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimal"));
							break;
						case CharmSpellHandler.eCharmType.HumanoidAnimalInsect:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, humanoids, insects, and reptiles. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsect"));
							break;
						case CharmSpellHandler.eCharmType.HumanoidAnimalInsectMagical:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, elemental, humanoids, insects, magical, plant, and reptile monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagical"));
							break;
						case CharmSpellHandler.eCharmType.HumanoidAnimalInsectMagicalUndead:
							// Message: Attempts to bring the target monster under the caster's control. Spell only works on animals, elemental, humanoids, insects, magical, plant, reptile, and undead monster types. Cannot charm named or epic monsters.
							dw.AddKeyValuePair("delve_string", LanguageMgr.GetTranslation(((GamePlayer) Caster).Client, "CharmSpell.DelveInfo.Desc.HumanoidAnimalInsectMagicalUndead"));
							break;
					}
					break;
				case eSpellType.CombatSpeedBuff:
				case eSpellType.CelerityBuff:
					dw.AddKeyValuePair("power_level", Spell.Value * 2);
					break;

				case eSpellType.Confusion:
					dw.AddKeyValuePair("power_level", Spell.Value > 0 ? Spell.Value : 100);
					break;
				case eSpellType.CombatSpeedDebuff:
					dw.AddKeyValuePair("power_level", -Spell.Value);
					break;
				case eSpellType.CureMezz:
					dw.AddKeyValuePair("type1", "8");
					break;
				case eSpellType.Disease:
					dw.AddKeyValuePair("delve_string", "Inflicts a wasting disease on the target that slows target by 15 %, reduces strength by 7.5 % and inhibits healing by 50 %");
					break;
				//case eSpellType.DefensiveProc:
				//case eSpellType.OffensiveProc:
				//	dw.AddKeyValuePair("delve_spell", Spell.Value);
				//	break;
				case eSpellType.FatigueConsumptionBuff:
					dw.AddKeyValuePair("delve_string", $"The target's actions require {(int)Spell.Value}% less endurance.");
					break;
				case eSpellType.FatigueConsumptionDebuff:
					dw.AddKeyValuePair("delve_string", $"The target's actions require {(int)Spell.Value}% more endurance.");
					break;
				case eSpellType.MeleeDamageBuff:
					dw.AddKeyValuePair("delve_string", $"Increases your melee damage by {(int)Spell.Value}%.");
					break;
				case eSpellType.MesmerizeDurationBuff:
					dw.AddKeyValuePair("damage_type", "22");
					dw.AddKeyValuePair("dur_type", "2");
					dw.AddKeyValuePair("power_level", "29");
					break;
				case eSpellType.Nearsight:
					dw.AddKeyValuePair("power_level", Spell.Value);
					break;
				case eSpellType.PetConversion:
					dw.AddKeyValuePair("delve_string", "Banishes the caster's pet and reclaims some of its energy.");
					break;
				case eSpellType.Resurrect:
					dw.AddKeyValuePair("amount_increase", Spell.ResurrectMana);
					dw.AddKeyValuePair("type1", "65");
					dw.Values["target"] = 8.ToString();
					break;
				case eSpellType.SavageCombatSpeedBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("power_level", Spell.Value * 2);
					break;
				case eSpellType.SavageCrushResistanceBuff:
				case eSpellType.SavageEnduranceHeal:
				case eSpellType.SavageParryBuff:
				case eSpellType.SavageSlashResistanceBuff:
				case eSpellType.SavageThrustResistanceBuff:
					dw.AddKeyValuePair("cost_type", "2");
					break;
				case eSpellType.SavageDPSBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("delve_string", $"Increases your melee damage by {(int)Spell.Value}%.");
					break;
				case eSpellType.SavageEvadeBuff:
					dw.AddKeyValuePair("cost_type", "2");
					dw.AddKeyValuePair("delve_string", $"Increases your chance to evade by {(int)Spell.Value}%.");
					break;
				case eSpellType.SummonAnimistPet:
				case eSpellType.SummonCommander:
				case eSpellType.SummonDruidPet:
				case eSpellType.SummonHunterPet:
				case eSpellType.SummonNecroPet:
				case eSpellType.SummonSimulacrum:
				case eSpellType.SummonSpiritFighter:
				case eSpellType.SummonUnderhill:
					dw.AddKeyValuePair("power_level", Spell.Damage);
					//dw.AddKeyValuePair("delve_string", "Summons a Pet to serve you.");
					//dw.AddKeyValuePair("description_string", "Summons a Pet to serve you.");
					break;
				case eSpellType.SummonMinion:
					dw.AddKeyValuePair("power_level", Spell.Value);
					break;
				case eSpellType.StyleStun:
					dw.AddKeyValuePair("type1", "22");
					break;
				case eSpellType.StyleSpeedDecrease:
					dw.AddKeyValuePair("type1", "39");
					break;
				case eSpellType.StyleCombatSpeedDebuff:
					dw.AddKeyValuePair("type1", "8");
					dw.AddKeyValuePair("power_level", -Spell.Value);
					break;
				case eSpellType.TurretPBAoE:
					dw.AddKeyValuePair("delve_string", $"Target takes {(int)Spell.Damage} damage. Spell affects everyone in the immediate radius of the caster's pet, and does less damage the further away they are from the caster's pet.");
					break;
				case eSpellType.TurretsRelease:
					dw.AddKeyValuePair("delve_string", "Unsummons all the animist turret(s) in range.");
					break;
				case eSpellType.StyleRange:
					dw.AddKeyValuePair("delve_string", $"Hits target up to {(int)Spell.Value} units away.");
					break;
				case eSpellType.MultiTarget:
					dw.AddKeyValuePair("delve_string", $"Hits {(int)Spell.Value} additonal target(s) within melee range.");
					break;
				case eSpellType.PiercingMagic:
					dw.AddKeyValuePair("delve_string", $"Effectiveness of the target's spells is increased by {(int)Spell.Value}%. Against higher level opponents than the target, this should reduce the chance of a full resist.");
					break;
				case eSpellType.StyleTaunt:
					if (Spell.Value < 0)
						dw.AddKeyValuePair("delve_string", $"Decreases your threat to monster targets by {-(int)Spell.Value} damage.");
					break;
				case eSpellType.NaturesShield:
					dw.AddKeyValuePair("delve_string", $"Gives the user a {(int)Spell.Value}% base chance to block ranged melee attacks while this style is prepared.");
					break;
				case eSpellType.SlashResistDebuff:
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
				eSpellTarget.REALM => 7,
				eSpellTarget.SELF => 0,
				eSpellTarget.ENEMY => 1,
				eSpellTarget.PET => 6,
				eSpellTarget.GROUP => 3,
				eSpellTarget.AREA => 0,// TODO
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
