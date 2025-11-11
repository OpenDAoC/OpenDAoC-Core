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
using DOL.GS.Styles;
using DOL.Language;
using DOL.Logging;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Default class for spell handler
	/// should be used as a base class for spell handler
	/// </summary>
	public class SpellHandler : ISpellHandler
	{
		private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// Maximum number of sub-spells to get delve info for.
		protected const byte MAX_DELVE_RECURSION = 5;

		// Minimum lower variance bound. Not supposed to be changed.
		private const double MIN_LOWER_VARIANCE_BOUND = 0.208;

		// Maximum number of Concentration spells that a single caster is allowed to cast.
		private const int MAX_CONC_SPELLS = 20;
		private const int PULSING_SPELL_END_OF_CAST_MESSAGE_INTERVAL = 2000;

		public virtual string ShortDescription => Spell.Description;
		protected string TargetPronoun => Spell.Target is eSpellTarget.SELF ? "your" : "the target's";
		protected string TargetPronounCapitalized => Spell.Target is eSpellTarget.SELF ? "Your" : "The target's";

		public GameLiving Target { get; set; }
		public eCastState CastState { get; private set; }
		protected bool HasLos { get; private set; }
		protected double DistanceFallOff { get; private set; }
		protected double CasterEffectiveness { get; private set; } = 1.0; // Needs to default to 1 since some spell handlers override `StartSpell`, preventing it from being set.
		protected virtual bool IsDualComponentSpell => false; // Dual component spells have a higher chance to be resisted.

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
						case eSpellType.SummonMinion:
						case eSpellType.SummonSimulacrum:
						case eSpellType.SummonUnderhill:
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
			foreach (ECSPulseEffect effect in living.effectListComponent.GetPulseEffects())
			{
				if (effect.SpellHandler.Spell.SpellType == spellType)
				{
					effect.End();
					return true;
				}
			}

			return false;
		}

		#endregion

		public virtual ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
		{
			return ECSGameEffectFactory.Create(initParams, static (in ECSGameEffectInitParams i) => new ECSGameSpellEffect(i));
		}

		public virtual ECSPulseEffect CreateECSPulseEffect(GameLiving target, double effectiveness)
		{
			int frequency = Spell != null ? Spell.Frequency : 0;
			return ECSGameEffectFactory.Create(new(target, CalculateEffectDuration(target), effectiveness, this), frequency, static (in ECSGameEffectInitParams i, int pulseFreq) => new ECSPulseEffect(i, pulseFreq));
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

			InterruptCasting(true);
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
			if (Spell.Uninterruptible)
				return false;

			NecromancerPet necromancerPet = Caster as NecromancerPet;

			// MoC isn't given to the Necromancer pet.
			if (necromancerPet != null)
			{
				if (necromancerPet.Owner?.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration) == true)
					return false;
			}

			if (Caster.effectListComponent.ContainsEffectForEffectType(eEffect.MasteryOfConcentration)
				|| Caster.effectListComponent.ContainsEffectForEffectType(eEffect.FacilitatePainworking)
				|| IsQuickCasting)
				return false;

			if (CastState is not eCastState.Focusing)
			{
				// Only interrupt if we're under 50% of the way through the cast.
				if (!IsInCastingPhase || GameLoop.GameLoopTime >= _castStartTick + _calculatedCastTime * 0.5)
					return false;
			}

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

			InterruptCasting(false);

			// In case the Necromancer pet is in passive mode, check the queue here.
			if (necromancerPet != null)
				(necromancerPet.Brain as NecromancerPetBrain).CheckAttackSpellQueue();

			return true;
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

			GamePlayer playerCaster = m_caster as GamePlayer;

			// Even with the spell queue disabled, spells are allowed to be queued silently to help counteract the client's anti spam feature.
			// But this means we should use the most up-to-date target, as no actual queuing behavior is expected.
			if (playerCaster != null && !playerCaster.SpellQueue)
				Target = playerCaster.TargetObject as GameLiving;
			else
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
					Target ??= Caster.TargetObject as GameLiving;

					// Pet spells are automatically casted on the controlled NPC, but only if the current target isn't a subpet or a turret.
					if (((Target as GameNPC)?.Brain as IControlledBrain)?.GetPlayerOwner() != Caster && Caster.ControlledBrain?.Body != null)
						Target = Caster.ControlledBrain.Body;

					break;
				}
				default:
				{
					// Get the current target if we don't have one already.
					Target ??= Caster.TargetObject as GameLiving;
					break;
				}
			}

			// Initial LoS state.
			if (playerCaster != null)
			{
				// This may be wrong. This is the LoS state at the time the player used the spell, not necessarily for the target the spell is being cast on, assuming it can change.
				// It should be fine since it's updated at the same time as `TargetObject`, and the spell handler doesn't receive a target explicitly. But it needs more testing.
				HasLos = Caster.TargetInView;
			}
			else if (Caster is GameNPC npcOwner)
			{
				// NPCs initial LoS checks are handled by the casting component before ticking the spell handler.
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

				if (effect != null && effect.End())
				{
					if (m_spell.InstrumentRequirement == 0)
						MessageToCaster("You cancel your effect.", eChatType.CT_Spell);
					else
						MessageToCaster("You stop playing your song.", eChatType.CT_Spell);

					return false;
				}
			}

			CancelFocusSpells();
			_quickcast = EffectListService.GetAbilityEffectOnTarget(m_caster, eEffect.QuickCast) as QuickCastECSGameEffect;

			if (IsQuickCasting)
				_quickcast.ExpireTick = GameLoop.GameLoopTime + _quickcast.Duration;

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
					MessageToCaster("Your target is immune to this effect!", eChatType.CT_SpellResisted);

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
					if (!m_caster.IsWithinRadius(m_caster.GroundTarget, Spell.CalculateEffectiveRange(m_caster)))
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

					if (!m_caster.IsWithinRadius(Target, Spell.CalculateEffectiveRange(m_caster)))
					{
						if (Caster is GamePlayer && !quiet)
							MessageToCaster("That target is too far away!", eChatType.CT_SpellResisted);

						Caster.Notify(GameLivingEvent.CastFailed, new CastFailedEventArgs(this, CastFailedEventArgs.Reasons.TargetTooFarAway));

						if (Caster is GameNPC npc)
							npc.Follow(Target, m_spell.CalculateEffectiveRange(npc) - 100, npc.StickMaximumRange);

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

			if (!CheckConcentrationCost(quiet))
				return false;

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

		public bool CheckConcentrationCost(bool quiet)
		{
			if (m_spell.Concentration == 0 || Caster is not GamePlayer playerCaster)
				return true;

			if (m_caster.Concentration < m_spell.Concentration)
			{
				if (!quiet)
					MessageToCaster($"This spell requires {m_spell.Concentration} concentration points to cast!", eChatType.CT_SpellResisted);

				return false;
			}

			if (m_caster.effectListComponent.GetConcentrationEffects().Count >= MAX_CONC_SPELLS)
			{
				if (!quiet)
					MessageToCaster($"You can only cast up to {MAX_CONC_SPELLS} simultaneous concentration spells!", eChatType.CT_SpellResisted);

				return false;
			}

			return true;
		}

		private void CheckPlayerLosDuringCastCallback(GamePlayer player, LosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			HasLos = response is LosCheckResponse.True;
		}

		private void CheckNpcLosDuringCastCallback(GameLiving living, LosCheckResponse response, ushort sourceOID, ushort targetOID)
		{
			HasLos = response is LosCheckResponse.True;

			if (!HasLos)
				InterruptCasting(false);
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
				if (!m_caster.IsWithinRadius(m_caster.GroundTarget, Spell.CalculateEffectiveRange(m_caster)))
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

				if (!m_caster.IsWithinRadius(target, Spell.CalculateEffectiveRange(m_caster)))
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

			Caster.castingComponent.OnSpellCast(Spell);
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

				if (m_spell.Target is not eSpellTarget.SELF and not eSpellTarget.GROUP and not eSpellTarget.CONE and not eSpellTarget.PET &&
					m_spell.Range > 0 &&
					LosChecker != null)
				{
					if (Caster is GameNPC npc)
						LosChecker.Out.SendCheckLos(npc, target, CheckNpcLosDuringCastCallback);
					else if (Caster is GamePlayer player)
						LosChecker.Out.SendCheckLos(player, target, CheckPlayerLosDuringCastCallback);
				}
			}

			return true;
		}

		public bool IsCastEndingSoon(int millisecondsBeforeEnd)
		{
			return GameServiceUtils.ShouldTick(_castEndTick - millisecondsBeforeEnd);
		}

		#endregion

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
								// We're also excluding Necromancer's instant spells because it can get a little overwhelming and was often reported.
								if (Spell.IsHarmful && Spell.SpellType is not eSpellType.PetSpell)
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

					if (GameServiceUtils.ShouldTick(_castEndTick))
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
			}

			// Process cast on same tick if interrupted or finished.
			switch (CastState)
			{
				case eCastState.Interrupted:
				{
					InterruptCasting(false);
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

			if (playerCaster != null && playerCaster.CharacterClass.IsFocusCaster)
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
			return 5;
		}

		/// <summary>
		/// Called whenever the casters casting sequence is to interrupt immediately
		/// </summary>
		protected virtual void InterruptCasting(bool isMoving)
		{
			if (m_interrupted)
				return;

			m_interrupted = true;
			Caster.castingComponent.InterruptCasting(isMoving);
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

			// Always create the pulse effect and let the effect list component keep the latest one alive, since there may be already pending ones.
			// We exclude flute mez, since `EffectListService` won't handle it correctly.
			if (m_spell.IsPulsing)
			{
				if (m_spell.SpellType is not eSpellType.Mesmerize)
					PulseEffect = CreateECSPulseEffect(Caster, CasterEffectiveness);
				else
					Caster.effectListComponent.CancelIncompatiblePulseEffects(this);
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
		public virtual List<GameLiving> SelectTargets(GameObject castTarget)
		{
			List<GameLiving> list = GameLoop.GetListForTick<GameLiving>();
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

					if (pet != null && Caster.IsWithinRadius(pet, Spell.CalculateEffectiveRange(Caster)))
					{
						if (Caster.IsControlledNPC(pet))
							list.Add(pet);
					}

					// Check 'ControlledBrain' if 'target' isn't a valid target.
					if (list.Count == 0 && Caster.ControlledBrain != null)
					{
						if (Caster is GamePlayer player && (eCharacterClass) player.CharacterClass.ID is eCharacterClass.Bonedancer)
						{
							foreach (GameNPC npcInRadius in player.GetNPCsInRadius((ushort) Spell.CalculateEffectiveRange(player)))
							{
								if (npcInRadius is CommanderPet commander && commander.Owner == player)
									list.Add(commander);
								else if (npcInRadius is BdSubPet subPet && subPet.Brain is IControlledBrain brain && brain.GetPlayerOwner() == player)
								{
									if (!Spell.IsHealing)
										list.Add(subPet);
								}
							}
						}
						else
						{
							pet = Caster.ControlledBrain.Body;

							if (pet != null && Caster.IsWithinRadius(pet, Spell.CalculateEffectiveRange(Caster)))
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
						spellRange = Spell.CalculateEffectiveRange(m_caster);

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
					foreach (GamePlayer player in Caster.GetPlayersInRadius((ushort) Spell.CalculateEffectiveRange(Caster)))
					{
						if (player == Caster)
							continue;

						if (!m_caster.IsObjectInFront(player, (Spell.Radius != 0 ? Spell.Radius : 100)))
							continue;

						if (!GameServer.ServerRules.IsAllowedToAttack(Caster, player, true))
							continue;

						list.Add(player);
					}

					foreach (GameNPC npc in Caster.GetNPCsInRadius((ushort) Spell.CalculateEffectiveRange(Caster)))
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

		public virtual void CastSubSpells(GameLiving target)
		{
			if (m_spell.SubSpellID > 0)
				CastSingleSubSpell(target, m_spell);

			foreach (int spellID in m_spell.MultipleSubSpells)
			{
				if (spellID != m_spell.SubSpellID)
					CastSingleSubSpell(target, SkillBase.GetSpellByID(spellID));
			}
		}

		private void CastSingleSubSpell(GameLiving target, Spell spell)
		{
			if (target == null || spell == null || spell.SubSpellID != 0)
				return;

			// Sub spells aren't scaled on initialization.
			if (Caster is GameNPC npc)
				spell = npc.GetScaledSpell(spell);

			ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(m_caster, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
			spellHandler.StartSpell(target);
		}

		public virtual List<GameLiving> GetGroupAndPets(Spell spell)
		{
			List<GameLiving> livingsInRange = GameLoop.GetListForTick<GameLiving>();
			List<GameLiving> groupMembers = Caster.Group?.GetMembersInTheGroup() ?? (Caster as NecromancerPet)?.Owner.Group?.GetMembersInTheGroup();

			if (groupMembers == null)
			{
				groupMembers = GameLoop.GetListForTick<GameLiving>();
				groupMembers.Add(Caster);
			}

			foreach (GameLiving living in groupMembers)
			{
				IControlledBrain controlledBrain = living.ControlledBrain;
				IControlledBrain[] subControlledBrains = controlledBrain?.Body.ControlledNpcList;

				if (subControlledBrains != null)
				{
					foreach (IControlledBrain subControlledBrain in subControlledBrains.Where(x => x != null && Caster.IsWithinRadius(x.Body, Spell.CalculateEffectiveRange(Caster))))
						livingsInRange.Add(subControlledBrain.Body);
				}

				if (controlledBrain != null)
				{
					if (Caster.IsWithinRadius(controlledBrain.Body, Spell.CalculateEffectiveRange(Caster)))
						livingsInRange.Add(controlledBrain.Body);
				}

				if (Caster == living || Caster.IsWithinRadius(living, Spell.CalculateEffectiveRange(Caster)))
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
			if (Spell.SpellType is not eSpellType.TurretPBAoE && Spell.IsPBAoE)
				Target = Caster;
			else if (Target == null)
				Target = target;

			if (Target != null)
			{
				if (Spell.IsFocus && (!Target.IsAlive || !Caster.IsWithinRadius(Target, Spell.CalculateEffectiveRange(Caster))))
				{
					CancelFocusSpells();
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

			for (int i = 0; i < targets.Count; i++)
			{
				GameLiving targetInList = targets[i];

				if (CheckSpellResist(targetInList))
					continue;

				if (Spell.Radius == 0 || HasPositiveEffect)
					ApplyEffectOnTarget(targetInList);
				else
				{
					if (Spell.Target == eSpellTarget.AREA)
						DistanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster.GroundTarget), Spell.Radius);
					else if (Spell.Target == eSpellTarget.CONE)
						DistanceFallOff = CalculateDistanceFallOff(targetInList.GetDistanceTo(Caster), Spell.CalculateEffectiveRange(Caster));
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
			return CasterEffectiveness * 1 + Caster.GetModified(eProperty.SpellDamage) * 0.01;
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
			if (Caster.Chance(RandomDeckEvent.CriticalChance, Caster.DebuffCriticalChance))
			{
				double min = 0.1;
				double max = 1.0;
				double criticalMod = min + Caster.GetPseudoDoubleIncl(RandomDeckEvent.CriticalVariance) * (max - min);
				(Caster as GamePlayer)?.Out.SendMessage($"Your {Spell.Name} critically debuffs the enemy for {criticalMod * 100:0}% additional effect!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
				return 1.0 + criticalMod;
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
				if (ad.Damage == 0)
					target.OnAttackedByEnemy(ad);
			}
		}

		public virtual bool HasConflictingEffectWith(ISpellHandler compare)
		{
			if (Spell.EffectGroup != 0 || compare.Spell.EffectGroup != 0)
				return Spell.EffectGroup == compare.Spell.EffectGroup;

			return Spell.SpellType == compare.Spell.SpellType;
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

			ECSGameSpellEffect effect = CreateECSEffect(new(target, CalculateEffectDuration(target), CalculateBuffDebuffEffectiveness(), this));

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
			GamePlayer playerCaster;

			if (m_caster is NecromancerPet necromancerPet && necromancerPet.Brain is IControlledBrain brain)
				playerCaster = brain.GetPlayerOwner();
			else
				playerCaster = m_caster as GamePlayer;

			if (m_spellLine.KeyName is GlobalSpellsLines.Item_Effects && m_spellItem != null)
				spellLevel = m_spellItem.Template.LevelRequirement > 0 ? m_spellItem.Template.LevelRequirement : m_spellItem.Level;
			else if (m_spellLine.KeyName is GlobalSpellsLines.Realm_Spells or GlobalSpellsLines.Reserved_Spells || playerCaster == null)
				spellLevel = m_caster.EffectiveLevel; // NPCs go there too.
			else
			{
				if (m_spellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect || m_spellLine.KeyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
				{
					Style style = playerCaster.attackComponent.attackAction.LastAttackData?.Style;

					if (style == null)
					{
						if (log.IsDebugEnabled)
							log.Debug($"Style is null for {playerCaster.Name} while calculating ToHitChance for spell {Spell.Name}.");

						spellLevel = playerCaster.Level; // Fallback to caster's level.
					}
					else
						spellLevel = style.Level;
				}
				else
					spellLevel = Spell.Level;

				spellLevel = Math.Min(GamePlayer.MAX_LEVEL, spellLevel + playerCaster.GetModified(eProperty.SpellLevel));
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
			double hitChance = 87.5;

			if (IsDualComponentSpell)
				hitChance -= 2.5;

			hitChance += (spellLevel - target.Level) / 2.0;
			hitChance += m_caster.GetModified(eProperty.ToHitBonus);

			if (playerCaster == null || target is not GamePlayer)
			{
				// 1 per level difference.
				hitChance += m_caster.EffectiveLevel - target.EffectiveLevel;
				hitChance += Math.Max(0, target.attackComponent.AttackerTracker.Count - 1) * Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
			}

			GameLiving casterToUse = playerCaster ?? m_caster;
			List<ECSGameEffect> effects = casterToUse.effectListComponent.GetEffects();
			ECSGameEffect piercingMagic = effects.FirstOrDefault(e => e.EffectType is eEffect.PiercingMagic);

			if (piercingMagic != null)
				hitChance += piercingMagic.SpellHandler.Spell.Value;

			ECSGameEffect majesticWill = effects.FirstOrDefault(e => e.EffectType is eEffect.MajesticWill);

			if (majesticWill != null)
				hitChance += majesticWill.Effectiveness * 5;

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
				double spellResistRoll = Caster.GetPseudoDouble(RandomDeckEvent.Miss);
				spellResistRoll *= 100;

				if (Caster is GamePlayer playerCaster && playerCaster.UseDetailedCombatLog)
					playerCaster.Out.SendMessage($"Target chance to resist: {spellResistChance:0.##} RandomNumber: {spellResistRoll:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

				if (target is GamePlayer playerTarget && playerTarget.UseDetailedCombatLog)
					playerTarget.Out.SendMessage($"Your chance to resist: {spellResistChance:0.##} RandomNumber: {spellResistRoll:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

				if (spellResistChance > spellResistRoll)
				{
					OnSpellNegated(target, SpellNegatedReason.Resisted);
					return true;
				}
			}

			return false;
		}

		protected virtual void OnSpellNegated(GameLiving target, SpellNegatedReason reason)
		{
			if (reason is SpellNegatedReason.Resisted)
				SendSpellResistMessages(target);

			SendSpellResistAnimation(target);
			SendSpellNegatedNotification(target);
			StartSpellNegatedInterruptTimer(target);
			StartSpellNegatedLastAttackTimer(target);
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
						MessageToLiving(owner, $"Your {target.Name} resists the effect!", eChatType.CT_SpellResisted);
				}
			}
			else
				MessageToLiving(target, "You resist the effect!", eChatType.CT_SpellResisted);

			// Deliver message to the caster as well.
			MessageToCaster($"{target.GetName(0, true)} resists the effect! ({CalculateSpellResistChance(target):0.0}%)", eChatType.CT_SpellResisted);
		}

		/// <summary>
		/// Send Spell Attack Data Notification to Target when Spell is Resisted
		/// </summary>
		public virtual void SendSpellNegatedNotification(GameLiving target)
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
		public virtual void StartSpellNegatedInterruptTimer(GameLiving target)
		{
			// Spells that would have caused damage or are not instant will still interrupt.
			if (Spell.Damage > 0 || Spell.CastTime != 0)
				target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
		}

		/// <summary>
		/// Start Last Attack Timer when Spell is Resisted
		/// </summary>
		public virtual void StartSpellNegatedLastAttackTimer(GameLiving target)
		{
			if (target.Realm is eRealm.None || Caster.Realm is eRealm.None)
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

		public virtual void CancelFocusSpells()
		{
			CastState = eCastState.Cleanup;

			foreach (ECSGameSpellEffect pulseSpell in Caster.effectListComponent.GetSpellEffects(eEffect.Pulse))
			{
				if (!pulseSpell.SpellHandler.Spell.IsFocus)
					continue;

				pulseSpell.End();
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

		public virtual SpellCostType CostType => SpellCostType.Power;

		public GamePlayer LosChecker { get; set; }

        /// <summary>
        /// Is the spell being cast?
        /// </summary>
        public bool IsInCastingPhase => CastState == eCastState.Casting;

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
				list.Add(ShortDescription);
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

		#region various helpers

		public virtual double CalculateDamageVarianceOffsetFromLevelDifference(GameLiving caster, GameLiving target)
		{
			// Was previously 2% per level difference, but this didn't match live results at lower level.
			// Assuming 2% was correct at level 50, it means it should be dynamic, either based on the caster's level of the target's.
			// This new formula increases the modifier the lower the level of the caster is: 10% at level 0, 2% at level 50 and above.
			return (caster.Level - target.Level) * Math.Max(2, 10 - caster.Level * 0.16) * 0.01;
		}

		protected static double CalculateLowerVarianceBound(int specLevel, int targetLevel)
		{
			// Vanesyra outlines the variance calculations here: https://www.ignboards.com/threads/melee-speed-melee-and-style-damage-or-why-pure-grothrates-are-wrong.452406879/page-3
			// However, the formula only works at level 50 and results in extremely low variance at lower levels, meaning the step count (one step = 0.8%) is too low.
			// Our adjusted formula corrects this and ensures a logical minimum variance across all levels.
			// The absolute minimum variance is 20.8%.
			// On live, steps represent the number of possible values. That isn't the case here: we're only interested in the minimum variance and allow any value between min and max.

			if (targetLevel < 1)
				targetLevel = 1;

			double levelDiff = targetLevel + 1 - specLevel;
			int steps = (int) (levelDiff * 100 / targetLevel) - 1;
			double minBound = 1 - 0.008 * steps;
			return Math.Clamp(minBound, MIN_LOWER_VARIANCE_BOUND, 1);
		}

		/// <summary>
		/// Calculates min damage variance %
		/// </summary>
		/// <param name="target">spell target</param>
		/// <param name="min">returns min variance</param>
		/// <param name="max">returns max variance</param>
		public virtual void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			GameLiving casterToUse = m_caster;

			switch (m_spellLine.KeyName)
			{
				// Further research should be done on these.
				// The variance range is tied to the base damage calculation.
				case GlobalSpellsLines.Mob_Spells:
				case GlobalSpellsLines.Nightshade:
				{
					// Mob spells are modified by acuity stats.
					// Nightshade spells aren't tied to any trainable specialization and thus require a fixed variance.
					// Lower bound is similar to what the variance calculation would return if we used 26 for the specialization and 50 for the target level.
					max = 1.0;
					min = 0.6;
					break;
				}
				case GlobalSpellsLines.Item_Effects:
				case GlobalSpellsLines.Potions_Effects:
				{
					// Procs and charges normally aren't modified by any stat, but are shown to be able to do about 25% more damage than their base value.
					max = 1.25;
					min = 0.75; // 1.25 * 0.6.
					break;
				}
				case GlobalSpellsLines.Combat_Styles_Effect:
				case GlobalSpellsLines.Reserved_Spells:
				{
					// Neither RAs or combat styles have any variance.
					max = 1.0;
					min = 1.0;
					break;
				}
				default:
				{
					max = 1.0;

					// Spells casted by a necromancer pet use the owner's spec.
					if (m_caster is NecromancerPet necromancerPet && necromancerPet.Brain is IControlledBrain brain)
						casterToUse = brain.GetPlayerOwner();

					min = CalculateLowerVarianceBound(casterToUse.GetModifiedSpecLevel(m_spellLine.Spec), target.Level);
					break;
				}
			}

			// Apply variance offset based on level difference.
			double varianceOffset = CalculateDamageVarianceOffsetFromLevelDifference(casterToUse, target);
			min += varianceOffset;
			max += varianceOffset;

			max = Math.Max(MIN_LOWER_VARIANCE_BOUND, max);
			min = Math.Clamp(min, MIN_LOWER_VARIANCE_BOUND, max);
		}

		/// <summary>
		/// Calculates the base 100% spell damage which is then modified by damage variance factors
		/// </summary>
		public virtual double CalculateDamageBase(GameLiving target)
		{
			double spellDamage = Spell.Damage;

			// Life drain spells receive a bonus based on the life drain return value.
			if (Spell.SpellType is eSpellType.Lifedrain)
				spellDamage *= 1 + Spell.LifeDrainReturn * 0.001;

			// Item effects (procs, charges), potion effects, and poisons don't scale with anything.
			if (SpellLine.KeyName is GlobalSpellsLines.Item_Effects or GlobalSpellsLines.Potions_Effects or GlobalSpellsLines.Mundane_Poisons or GlobalSpellsLines.Realm_Spells)
				return Math.Max(0, spellDamage);

			double stat = 0.0;
			double spec = 0.0;

			// Special handling for combat styles.
			// Based on live testing, June 2025.
			// Test parameters:
			// Class: Valewalker.
			// Level: 50.
			// Delve: 125, 150, 198.
			// Strength: 149~343.
			// Skill bonus: 0, 4, 8.
			// Target: Dummy.
			// No ToA or relic bonus.
			// Scales with weapon skill (not weapon stat directly) and specialization bonus from items.
			// Results overestimated damage by 0.1% on average.
			if (SpellLine.KeyName is GlobalSpellsLines.Combat_Styles_Effect)
			{
				stat = Caster.GetWeaponSkill(Caster.ActiveWeapon);
				DbInventoryItem weapon = Caster.ActiveWeapon;

				// We can't retrieve the skill from the spell, so we have to use the currently equipped weapon instead.
				if (weapon != null)
				{
					string specName = SkillBase.ObjectTypeToSpec((eObjectType) weapon.Object_Type);
					spec = specName == null ? 0 : Caster.GetModifiedFromItems(SkillBase.SpecToSkill(specName));
				}
				else
					spec = 0;

				spellDamage = stat * (spellDamage / 124 + 1 / 23) * (1 + spec * 0.004);
				return Math.Max(0, spellDamage);
			}

			// Stats are only partially transferred to the necromancer pet, so we don't use its intelligence at all.
			// Other pets use their own stats and level.
			GameLiving modifiedCaster = Caster is NecromancerPet necromancerPet ? necromancerPet.Owner : Caster;

			if (modifiedCaster is GameNPC)
				stat = modifiedCaster.GetModified(eProperty.Intelligence);
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
						// Spell damage seems to be based on strength around 1.65 (but the mana stat is dexterity).
						// 1.62 made them benefit from Augmented Acuity, but `StatCalculator` doesn't add it to strength to prevent melee damage from increasing too.
						// So we have to do it here.
						// It's also unclear if both Augmented Strength and Augmented Acuity should contribute, but this is currently the case.
						stat = playerCaster.GetModified(eProperty.Strength) + playerCaster.AbilityBonus[eProperty.Acuity];
						break;
					}
					default:
					{
						if (playerCaster.CharacterClass.ManaStat is not eStat.UNDEFINED)
							stat = playerCaster.GetModified((eProperty) playerCaster.CharacterClass.ManaStat);

						spec = playerCaster.GetModifiedFromItems(SkillBase.SpecToSkill(m_spellLine.Spec)); // Only item bonus increases damage.
						break;
					}
				}
			}

			// A nerf of about 10% to spell damage was supposedly applied around 2014. We are not applying it.
			// This formula is also somewhat inaccurate. Even with that nerf applied, the intelligence modifier is too high when comparing damage on live at low level.
			spellDamage *= (1 + stat * 0.005) * (1 + spec * 0.005);
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
				Weapon = m_caster.ActiveWeapon,
				SpellHandler = this,
				AttackResult = eAttackResult.HitUnstyled
			};

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

			double variance = minVariance + Caster.GetPseudoDoubleIncl(RandomDeckEvent.DamageVariance) * (maxVariance - minVariance);
			double finalDamage = spellDamage * variance;

			// Live testing done Summer 2009 by Bluraven, Tolakram. Levels 40, 45, 50, 55, 60, 65, 70.
			// Damage reduced by chance < 55, no extra damage increase noted with hitchance > 100.
			double hitChance = CalculateToHitChance(ad.Target);
			finalDamage = AdjustDamageForHitChance(finalDamage, hitChance);

			GamePlayer playerCaster = Caster as GamePlayer;

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

			if (playerCaster != null && playerCaster.UseDetailedCombatLog)
				playerCaster.Out.SendMessage($"BaseDamage: {baseDamage:0.##} | SpecMod: {variance:0.##} ({minVariance:0.00}~{maxVariance:0.00})", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

			if (Caster.Chance(RandomDeckEvent.CriticalChance, criticalChance))
			{
				double min = 0.1;
				double max = ad.Target is GamePlayer ? 0.5 : 1.0;
				double criticalMod = min + Caster.GetPseudoDoubleIncl(RandomDeckEvent.CriticalVariance) * (max - min);
				criticalDamage = (int) (finalDamage * criticalMod);
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
			eProperty property = GameLiving.GetResistTypeForDamage(damageType);
			int primaryResistModifier = ad.Target.GetResist(damageType);
			int secondaryResistModifier = Math.Min(80, ad.Target.SpecBuffBonusCategory[property]);

			// Resist Pierce is a special bonus which has been introduced with ToA.
			// It reduces the resistance that the victim receives through items by the specified percentage.
			// http://de.daocpedia.eu/index.php/Resistenz_durchdringen (translated)
			int resistPierce = Caster.GetModified(eProperty.ResistPierce);

			// Subtract max ItemBonus of property of target, but at least 0.
			if (resistPierce > 0 && Spell.SpellType != eSpellType.Archery)
				primaryResistModifier -= Math.Max(0, Math.Min(ad.Target.ItemBonus[property], resistPierce));

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


			foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendCombatAnimation(ad.Attacker, ad.Target, 0, 0, 0, 0, (byte) attackResult, ad.Target.HealthPercent);

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
	}
}
