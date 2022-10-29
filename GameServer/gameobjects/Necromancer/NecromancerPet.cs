/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// The necromancer pets.
	/// </summary>
	/// <author>Aredhel</author>
	public class NecromancerPet : GamePet
	{
		public override GameObject TargetObject
		{
			get => base.TargetObject;
			set
			{
				// 1.60:
				// - A Necromancer's target window will now update to reflect a target his pet has acquired, if he does not already have a target.
				if (TargetObject != value && Owner is GamePlayer playerOwner && playerOwner.TargetObject == null)
					playerOwner.Client.Out.SendChangeTarget(value);
				base.TargetObject = value;
			}
		}

		/// <summary>
		/// gets the DamageRvR Memory of this NecromancerPet
		/// </summary>
		public override long DamageRvRMemory
		{
			get => m_damageRvRMemory;
			set => m_damageRvRMemory = value;
		}

		public override short MaxSpeed => (short)GetModified(eProperty.MaxSpeed);

        /// <summary>
		/// Proc IDs for various pet weapons.
		/// </summary>
		private enum Procs
		{
			Cold = 32050,
			Disease = 32014,
			Heat = 32053,
			Poison = 32013,
			Stun = 2165
		};

		/// <summary>
		/// Create necromancer pet from template. Con and hit bonuses from
		/// items the caster was wearing when the summon started, will be
		/// transferred to the pet.
		/// </summary>
		/// <param name="npcTemplate"></param>
		/// <param name="owner">Player who summoned this pet.</param>
		/// <param name="summonConBonus">Item constitution bonuses of the player.</param>
		/// <param name="summonHitsBonus">Hits bonuses of the player.</param>
		public NecromancerPet(INpcTemplate npcTemplate, int summonConBonus, int summonHitsBonus) : base(npcTemplate)
		{
			// Transfer bonuses.
			m_summonConBonus = summonConBonus;
			m_summonHitsBonus = summonHitsBonus;

			// Set immunities/load equipment/etc.

			switch (Name.ToLower())
			{
				case "lesser zombie servant":
				case "zombie servant":
					EffectList.Add(new MezzRootImmunityEffect());
					LoadEquipmentTemplate("barehand_weapon");
					InventoryItem item;
					if (Inventory != null && (item = Inventory.GetItem(eInventorySlot.RightHandWeapon)) != null)
						item.ProcSpellID = (int)Procs.Stun;
					break;
				case "reanimated servant" :
					LoadEquipmentTemplate("reanimated_servant");
					break;
				case "necroservant":
					LoadEquipmentTemplate("necroservant");
					break;
				case "greater necroservant":
					LoadEquipmentTemplate("barehand_weapon");
					if (Inventory != null && (item = Inventory.GetItem(eInventorySlot.RightHandWeapon)) != null)
						item.ProcSpellID = (int)Procs.Poison;
					break;
				case "abomination":
					LoadEquipmentTemplate("abomination_fiery_sword");
					break;
				default:
					LoadEquipmentTemplate("barehand_weapon");
					break;
			}
		}

		#region Stats

		private int m_summonConBonus;
		private int m_summonHitsBonus;

		/// <summary>
		/// Get modified bonuses for the pet; some bonuses come from the shade,
		/// some come from the pet.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public override int GetModified(eProperty property)
		{
			if (Brain == null || (Brain as IControlledBrain) == null)
				return base.GetModified(property);

            GameLiving livingOwner = (Brain as IControlledBrain).GetLivingOwner();
			GamePlayer playerOwner = livingOwner as GamePlayer;

			switch (property)
			{
				case eProperty.Strength:
				case eProperty.Dexterity:
				case eProperty.Quickness:
				case eProperty.Intelligence:
				case eProperty.Resist_Crush:
				case eProperty.Resist_Body:
				case eProperty.Resist_Cold:
				case eProperty.Resist_Energy:
				case eProperty.Resist_Heat:
				case eProperty.Resist_Matter:
				case eProperty.Resist_Slash:
				case eProperty.Resist_Spirit:
				case eProperty.Resist_Thrust:
					{
						// Get item bonuses from the shade, but buff bonuses from the pet.
						int itemBonus = livingOwner.GetModifiedFromItems(property);
						int buffBonus = GetModifiedFromBuffs(property);
						int debuff = DebuffCategory[(int)property];

						// Base stats from the pet; add this to item bonus
						// afterwards, as it is treated the same way for
						// debuffing purposes.
						int baseBonus = 0;
						int augRaBonus = 0;

						switch (property)
						{
							case eProperty.Strength:
								baseBonus = Strength;
								augRaBonus = AtlasRAHelpers.GetStatEnhancerAmountForLevel(playerOwner != null ? AtlasRAHelpers.GetAugStrLevel(playerOwner) : 0);
								break;
							case eProperty.Dexterity:
								baseBonus = Dexterity;
								augRaBonus = AtlasRAHelpers.GetStatEnhancerAmountForLevel(playerOwner != null ? AtlasRAHelpers.GetAugDexLevel(playerOwner) : 0);
								break;
							case eProperty.Quickness:
								baseBonus = Quickness;
								augRaBonus = AtlasRAHelpers.GetStatEnhancerAmountForLevel(playerOwner != null ? AtlasRAHelpers.GetAugQuiLevel(playerOwner) : 0);
								break;
							case eProperty.Intelligence:
								baseBonus = Intelligence;
								augRaBonus = AtlasRAHelpers.GetStatEnhancerAmountForLevel(playerOwner != null ? AtlasRAHelpers.GetAugAcuityLevel(playerOwner) : 0);
								break;
						}

						itemBonus += baseBonus + augRaBonus;

						// Apply debuffs. 100% Effectiveness for player buffs, but only 50% effectiveness for item bonuses.
						buffBonus -= Math.Abs(debuff);

						if (buffBonus < 0)
						{
							itemBonus += buffBonus / 2;
							buffBonus = 0;
							if (itemBonus < 0)
								itemBonus = 0;
						}

						return itemBonus + buffBonus;
					}
				case eProperty.Constitution:
					{
						int baseBonus = Constitution;
						int buffBonus = GetModifiedFromBuffs(eProperty.Constitution);
						int debuff = DebuffCategory[(int)property];

						// Apply debuffs. 100% Effectiveness for player buffs, but only 50% effectiveness for base bonuses.
						buffBonus -= Math.Abs(debuff);

						if (buffBonus < 0)
						{
							baseBonus += buffBonus / 2;
							buffBonus = 0;
							if (baseBonus < 0)
								baseBonus = 0;
						}

						return baseBonus + buffBonus;
					}
				case eProperty.MaxHealth:
					{
						int conBonus = (int)(3.1 * m_summonConBonus);
						int hitsBonus = 30 * Level + m_summonHitsBonus;
						int debuff = DebuffCategory[(int)property];

						// Apply debuffs. As only base constitution affects pet health, effectiveness is a flat 50%.
						conBonus -= Math.Abs(debuff) / 2;

						if (conBonus < 0)
							conBonus = 0;
						
						int totalBonus = conBonus + hitsBonus;

						AtlasOF_ToughnessAbility toughness = playerOwner?.GetAbility<AtlasOF_ToughnessAbility>();
						double toughnessMod = toughness != null ? 1 + toughness.GetAmountForLevel(toughness.Level) * 0.01 : 1;

						return (int)(totalBonus * toughnessMod);
					}
			}

			return base.GetModified(property);
		}

		/// <summary>
		/// Current health (absolute value).
		/// </summary>
		public override int Health
		{
			get
			{
				return base.Health;
			}
			set
			{
				value = Math.Min(value, MaxHealth);
				value = Math.Max(value, 0);

				if (Health == value)
				{
					base.Health = value; //needed to start regeneration
					return;
				}

				int oldPercent = HealthPercent;
				base.Health = value;
				if (oldPercent != HealthPercent)
				{
					// Update pet health in group window.

					GamePlayer owner = ((Brain as IControlledBrain).Owner) as GamePlayer;
					if (owner.Group != null)
						owner.Group.UpdateMember(owner, false, false);
				}
			}
		}

		/// <summary>
		/// Set stats according to necro pet server properties
		/// </summary>
		public override void AutoSetStats()
		{
			if (Name.ToUpper() == "GREATER NECROSERVANT")
			{
				Strength = ServerProperties.Properties.NECRO_GREATER_PET_STR_BASE;
				Constitution = (short)(ServerProperties.Properties.NECRO_GREATER_PET_CON_BASE + m_summonConBonus);
				Dexterity = ServerProperties.Properties.NECRO_GREATER_PET_DEX_BASE;
				Quickness = ServerProperties.Properties.NECRO_GREATER_PET_QUI_BASE;
				Intelligence = ServerProperties.Properties.NECRO_GREATER_PET_INT_BASE;

				if (Level > 1)
				{
					Strength += (short)Math.Round(ServerProperties.Properties.NECRO_GREATER_PET_STR_MULTIPLIER * Level);
					Constitution += (short)Math.Round(ServerProperties.Properties.NECRO_GREATER_PET_CON_MULTIPLIER * Level);
					Dexterity += (short)Math.Round(ServerProperties.Properties.NECRO_GREATER_PET_DEX_MULTIPLIER * Level);
					Quickness += (short)Math.Round(ServerProperties.Properties.NECRO_GREATER_PET_QUI_MULTIPLIER * Level);
					Intelligence += (short)Math.Round(ServerProperties.Properties.NECRO_GREATER_PET_INT_MULTIPLIER * Level);
				}
			}
			else
			{
				Strength = ServerProperties.Properties.NECRO_PET_STR_BASE;
				Constitution = (short)(ServerProperties.Properties.NECRO_PET_CON_BASE + m_summonConBonus);
				Dexterity = ServerProperties.Properties.NECRO_PET_DEX_BASE;
				Quickness = ServerProperties.Properties.NECRO_PET_QUI_BASE;
				Intelligence = ServerProperties.Properties.NECRO_PET_INT_BASE;

				if (Level > 1)
				{
					Strength += (short)Math.Round(ServerProperties.Properties.NECRO_PET_STR_MULTIPLIER * Level);
					Constitution += (short)Math.Round(ServerProperties.Properties.NECRO_PET_CON_MULTIPLIER * Level);
					Dexterity += (short)Math.Round(ServerProperties.Properties.NECRO_PET_DEX_MULTIPLIER * Level);
					Quickness += (short)Math.Round(ServerProperties.Properties.NECRO_PET_QUI_MULTIPLIER * Level);
					Intelligence += (short)Math.Round(ServerProperties.Properties.NECRO_PET_INT_MULTIPLIER * Level);
				}
			}

			Empathy = 60;
			Piety = 60;
			Charisma = 60;

			// Now scale them according to NPCTemplate values
			if (NPCTemplate != null)
			{
				if (NPCTemplate.Strength > 0)
					Strength = (short)Math.Round(Strength * NPCTemplate.Strength / 100.0);

				if (NPCTemplate.Constitution > 0)
					Constitution = (short)Math.Round(Constitution * NPCTemplate.Constitution / 100.0);
				
				if (NPCTemplate.Quickness > 0)
					Quickness = (short)Math.Round(Quickness * NPCTemplate.Quickness / 100.0);
				
				if (NPCTemplate.Dexterity > 0)
					Dexterity = (short)Math.Round(Dexterity * NPCTemplate.Dexterity / 100.0);
				
				if (NPCTemplate.Intelligence > 0)
					Intelligence = (short)Math.Round(Intelligence * NPCTemplate.Intelligence / 100.0);
				
				// Except for CHA, EMP, AND PIE as those don't have autoset values.
				if (NPCTemplate.Empathy > 0)
					Empathy = (short)(NPCTemplate.Empathy + Level);
				
				if (NPCTemplate.Piety > 0)
					Piety = (short)(NPCTemplate.Piety + Level);

				if (NPCTemplate.Charisma > 0)
					Charisma = (short)(NPCTemplate.Charisma + Level);
			}
		}

		#endregion

		#region Melee

		/// <summary>
		/// Toggle taunt mode on/off.
		/// </summary>
		private void ToggleTauntMode()
		{
			TauntEffect tauntEffect = EffectList.GetOfType<TauntEffect>();
			GamePlayer owner = (Brain as IControlledBrain).Owner as GamePlayer;

			if (tauntEffect != null)
			{
				// It's on, so let's switch it off.
				tauntEffect.Stop();
				if (owner != null)
					owner.Out.SendMessage(string.Format("{0} seems to be less aggressive than before.", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else
			{
				// It's off, so let's turn it on.
				if (owner != null)
					owner.Out.SendMessage(string.Format("{0} enters an aggressive stance.", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);

				new TauntEffect().Start(this);
			}
		}

		#endregion

		#region Spells

		/// <summary>
		/// Pet-only insta spells.
		/// </summary>
		public static string PetInstaSpellLine => "Necro Pet Insta Spells";

		/// <summary>
		/// Called when necro pet is hit to see if spellcasting is interrupted
		/// </summary>
		/// <param name="ad">information about the attack</param>
		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0)
			{
				GamePlayer player = Owner as GamePlayer;
				string modmessage = "";

				if (ad.Modifier > 0)
					modmessage = " (+" + ad.Modifier + ")";
				else if (ad.Modifier < 0)
					modmessage = " (" + ad.Modifier + ")";

				player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.HitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

				if (ad.CriticalDamage > 0)
					player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.CriticallyHitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
			}

			base.OnAttackedByEnemy(ad);
		}

		public override void StartInterruptTimer(int duration, AttackData.eAttackType attackType, GameLiving attacker)
		{
			// Necromancer pets don't get interrupt timers.
			if (attacker != this)
				castingComponent?.spellHandler?.CasterIsAttacked(attacker);

			return;
		}

		public AttackData MakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness, int interruptDuration, bool dualWield, bool ignoreLOS)
		{
			((NecromancerPetBrain)Brain).CheckAttackSpellQueue();
			return attackComponent.LivingMakeAttack(target, weapon, style, effectiveness, interruptDuration, dualWield, ignoreLOS);
		}

		/// <summary>
		/// Cast a specific spell from given spell line
		/// </summary>
		/// <param name="spell">spell to cast</param>
		/// <param name="line">Spell line of the spell (for bonus calculations)</param>
		/// <returns>Whether the spellcast started successfully</returns>
		public override bool CastSpell(Spell spell, SpellLine line)
		{
			if (IsStunned || IsMezzed)
			{
				Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));
				return false;
			}

			if (CurrentSpellHandler != null && spell.CastTime > 0)
			{
				Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.AlreadyCasting));
				return false;
			}

			bool cast = castingComponent.StartCastSpell(spell, line);
			ISpellHandler handler;

			if (spell.IsInstantCast)
				handler = castingComponent.instantSpellHandler;
			else
				handler = castingComponent.spellHandler;

			if (handler != null)
			{
				int power = handler.PowerCost(Owner);

				if (Owner.Mana < power)
				{
					Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.NotEnoughPower));
					return false;
				}
				
				if (!handler.Spell.IsInstantCast)
                {
					handler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
				}
			}
			
            return cast;
		}

		public override void OnAfterSpellCastSequence(ISpellHandler handler)
		{
			if (SpellTimer != null)
			{
				if (this == null
					|| ObjectState != eObjectState.Active
					|| !IsAlive
					|| TargetObject == null
					|| (TargetObject is GameLiving && TargetObject.ObjectState != eObjectState.Active)
					|| !(TargetObject as GameLiving).IsAlive)
					SpellTimer.Stop();
				else
					SpellTimer.Start(1);
			}
			if (CurrentSpellHandler != null)
				CurrentSpellHandler.CastingCompleteEvent -= new CastingCompleteCallback(OnAfterSpellCastSequence); // Prevent from relaunch

			Brain.Notify(GameLivingEvent.CastFinished, this, new CastingEventArgs(handler));
		}

		public override bool CanCastInCombat(Spell spell)
		{
			// Necromancer pets can always start to cast while in combat
			return true;
		}

		public override void ModifyAttack(AttackData attackData)
		{
			base.ModifyAttack(attackData);

			if ((Owner as GamePlayer).Client.Account.PrivLevel > (int)ePrivLevel.Player)
			{
				attackData.Damage = 0;
				attackData.CriticalDamage = 0;
			}
		}

		/// <summary>
		/// Taunt the current target.
		/// </summary>
		public void Taunt()
		{
			if (IsIncapacitated)
				return;

			SpellLine chantsLine = SkillBase.GetSpellLine("Chants");
			if (chantsLine == null)
				return;

			List<Spell> chantsList = SkillBase.GetSpellList("Chants");
			if (chantsList.Count == 0)
				return;

			// Find the best paladin taunt for this level.
			Spell tauntSpell = null;
			foreach (Spell spell in chantsList)
			{
				if (spell.SpellType == (byte)eSpellType.Taunt && spell.Level <= Level)
					tauntSpell = spell;
			}

			if (tauntSpell != null && GetSkillDisabledDuration(tauntSpell) == 0)
				CastSpell(tauntSpell, chantsLine);
		}

		#endregion

		public override bool SayReceive(GameLiving source, string str)
		{
			return WhisperReceive(source, str);
		}

		public override bool Interact(GamePlayer player)
		{
			return WhisperReceive(player, "arawn");
		}

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
			criticalAmount /= 2;
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

		/// <summary>
		/// Load equipment for the pet.
		/// </summary>
		/// <param name="templateID">Equipment Template ID.</param>
		/// <returns>True on success, else false.</returns>
		private bool LoadEquipmentTemplate(string templateID)
		{
			if (templateID.Length <= 0)
				return false;

			GameNpcInventoryTemplate inventoryTemplate = new GameNpcInventoryTemplate();

			if (inventoryTemplate.LoadFromDatabase(templateID))
			{
				Inventory = new GameNPCInventory(inventoryTemplate);
				InventoryItem item;

				if ((item = Inventory.GetItem(eInventorySlot.TwoHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 50;

					switch (templateID)
					{
						case "abomination_fiery_sword":
						case "abomination_flaming_mace":
							item.ProcSpellID = (int)Procs.Heat;
							break;
						case "abomination_icy_sword":
						case "abomination_frozen_mace":
							item.ProcSpellID = (int)Procs.Cold;
							break;
						case "abomination_poisonous_sword":
						case "abomination_venomous_mace":
							item.ProcSpellID = (int)Procs.Poison;
							break;
					}

					SwitchWeapon(eActiveWeaponSlot.TwoHanded);
				}
				else
				{
					if ((item = Inventory.GetItem(eInventorySlot.RightHandWeapon)) != null)
					{
						item.DPS_AF = (int)(Level * 3.3);
						item.SPD_ABS = 37;
					}

					if ((item = Inventory.GetItem(eInventorySlot.LeftHandWeapon)) != null)
					{
						item.DPS_AF = (int)(Level * 3.3);
						item.SPD_ABS = 37;
					}

					SwitchWeapon(eActiveWeaponSlot.Standard);
				}
			}

			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;

				player.Out.SendLivingEquipmentUpdate(this);
			}

			return true;
		}
		/// <summary>
		/// Pet stayed out of range for too long, despawn it.
		/// </summary>
		public void CutTether()
		{
			if ((Brain as IControlledBrain).Owner is not GamePlayer)
				return;

			Brain.Stop();
			Die(null);
		}
	}
}
