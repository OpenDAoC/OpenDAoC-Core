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
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS
{
	public class NecromancerPet : GameSummonedPet
	{
		public override GameObject TargetObject
		{
			get => base.TargetObject;
			set
			{
				if (TargetObject == value)
					return;

				bool newTarget = value != null;
				base.TargetObject = value;

				if (newTarget)
				{
					// 1.60:
					// - A Necromancer's target window will now update to reflect a target his pet has acquired, if he does not already have a target.
					if (newTarget && Owner is GamePlayer playerOwner && playerOwner.TargetObject == null)
						playerOwner.Client.Out.SendChangeTarget(value);

					if (newTarget && EffectList.GetOfType<TauntEffect>() != null)
						Taunt();
				}
			}
		}

		public override long DamageRvRMemory
		{
			get => m_damageRvRMemory;
			set => m_damageRvRMemory = value;
		}

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
		}

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
		/// Get modified bonuses for the pet; some bonuses come from the shade, some come from the pet.
		/// </summary>
		public override int GetModified(eProperty property)
		{
			if (Brain == null || (Brain as IControlledBrain) == null)
				return base.GetModified(property);

			GameLiving livingOwner = (Brain as IControlledBrain).GetLivingOwner();
			GamePlayer playerOwner = livingOwner as GamePlayer;

			switch (property)
			{
				case eProperty.Resist_Body:
				case eProperty.Resist_Cold:
				case eProperty.Resist_Crush:
				case eProperty.Resist_Energy:
				case eProperty.Resist_Heat:
				case eProperty.Resist_Matter:
				case eProperty.Resist_Slash:
				case eProperty.Resist_Spirit:
				case eProperty.Resist_Thrust:
					return base.GetModified(property);
				case eProperty.Strength:
				case eProperty.Dexterity:
				case eProperty.Quickness:
				case eProperty.Intelligence:
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

		public override int Health
		{
			get => base.Health;
			set
			{
				value = Math.Min(value, MaxHealth);
				value = Math.Max(value, 0);

				if (Health == value)
				{
					base.Health = value; // Needed to start regeneration.
					return;
				}

				int oldPercent = HealthPercent;
				base.Health = value;
				if (oldPercent != HealthPercent)
				{
					// Update pet health in group window.
					GamePlayer owner = (Brain as IControlledBrain).Owner as GamePlayer;
					owner.Group?.UpdateMember(owner, false, false);
				}
			}
		}

		/// <summary>
		/// Set stats according to necro pet server properties.
		/// </summary>
		public override void AutoSetStats(Mob dbMob = null)
		{
			int levelMinusOne = Level - 1;

			if (Name.ToUpper() == "GREATER NECROSERVANT")
			{
				Strength = Properties.NECRO_GREATER_PET_STR_BASE;
				Constitution = (short) (Properties.NECRO_GREATER_PET_CON_BASE + m_summonConBonus);
				Dexterity = Properties.NECRO_GREATER_PET_DEX_BASE;
				Quickness = Properties.NECRO_GREATER_PET_QUI_BASE;
				Intelligence = Properties.NECRO_GREATER_PET_INT_BASE;

				if (Level > 1)
				{
					Strength += (short) Math.Round(levelMinusOne * Properties.NECRO_GREATER_PET_STR_MULTIPLIER);
					Constitution += (short) Math.Round(levelMinusOne * Properties.NECRO_GREATER_PET_CON_MULTIPLIER);
					Dexterity += (short) Math.Round(levelMinusOne * Properties.NECRO_GREATER_PET_DEX_MULTIPLIER);
					Quickness += (short) Math.Round(levelMinusOne * Properties.NECRO_GREATER_PET_QUI_MULTIPLIER);
					Intelligence += (short) Math.Round(levelMinusOne * Properties.NECRO_GREATER_PET_INT_MULTIPLIER);
				}
			}
			else
			{
				Strength = Properties.NECRO_PET_STR_BASE;
				Constitution = (short) (Properties.NECRO_PET_CON_BASE + m_summonConBonus);
				Dexterity = Properties.NECRO_PET_DEX_BASE;
				Quickness = Properties.NECRO_PET_QUI_BASE;
				Intelligence = Properties.NECRO_PET_INT_BASE;

				if (Level > 1)
				{
					Strength += (short) Math.Round(levelMinusOne * Properties.NECRO_PET_STR_MULTIPLIER);
					Constitution += (short) Math.Round(levelMinusOne * Properties.NECRO_PET_CON_MULTIPLIER);
					Dexterity += (short) Math.Round(levelMinusOne * Properties.NECRO_PET_DEX_MULTIPLIER);
					Quickness += (short) Math.Round(levelMinusOne * Properties.NECRO_PET_QUI_MULTIPLIER);
					Intelligence += (short) Math.Round(levelMinusOne * Properties.NECRO_PET_INT_MULTIPLIER);
				}
			}

			Empathy = 30;
			Piety = 30;
			Charisma = 30;

			// Stats are scaled using the current template.
			if (NPCTemplate != null)
			{
				if (NPCTemplate.Strength > 0)
					Strength = (short) Math.Round(Strength * (NPCTemplate.Strength / 100.0));
				if (NPCTemplate.Constitution > 0)
					Constitution = (short) Math.Round(Constitution * (NPCTemplate.Constitution / 100.0));
				if (NPCTemplate.Quickness > 0)
					Quickness = (short) Math.Round(Quickness * (NPCTemplate.Quickness / 100.0));
				if (NPCTemplate.Dexterity > 0)
					Dexterity = (short) Math.Round(Dexterity * (NPCTemplate.Dexterity / 100.0));
				if (NPCTemplate.Intelligence > 0)
					Intelligence = (short) Math.Round(Intelligence * (NPCTemplate.Intelligence / 100.0));
				if (NPCTemplate.Empathy > 0)
					Empathy = NPCTemplate.Empathy;
				if (NPCTemplate.Piety > 0)
					Piety = NPCTemplate.Piety;
				if (NPCTemplate.Charisma > 0)
					Charisma = NPCTemplate.Charisma;
			}
		}

		#endregion

		#region Melee

		private void ToggleTauntMode()
		{
			TauntEffect tauntEffect = EffectList.GetOfType<TauntEffect>();
			GamePlayer owner = (Brain as IControlledBrain).Owner as GamePlayer;

			if (tauntEffect != null)
			{
				tauntEffect.Stop();
				owner.Out.SendMessage(string.Format("{0} seems to be less aggressive than before.", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else
			{
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
		/// Called when necro pet is hit to see if spellcasting is interrupted.
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

		public override void ModifyAttack(AttackData attackData)
		{
			base.ModifyAttack(attackData);

			if ((Owner as GamePlayer).Client.Account.PrivLevel > (int)ePrivLevel.Player)
			{
				attackData.Damage = 0;
				attackData.CriticalDamage = 0;
			}
		}

		private void Empower()
		{
			if (attackComponent.AttackState)
				return;

			SpellLine buffLine = SkillBase.GetSpellLine(PetInstaSpellLine);

			if (buffLine == null)
				return;

			List<Spell> buffList = SkillBase.GetSpellList(PetInstaSpellLine);

			if (buffList.Count == 0)
				return;

			int maxLevel = Level;
			Spell strBuff = null;
			Spell dexBuff = null;

			// Find the best baseline buffs for this level.
			foreach (Spell spell in buffList)
			{
				if (spell.Level <= maxLevel)
				{
					switch (spell.SpellType)
					{
						case eSpellType.StrengthBuff:
						{
							strBuff = strBuff == null ? spell : (strBuff.Level < spell.Level) ? spell : strBuff;
							break;
						}
						case eSpellType.DexterityBuff:
						{
							dexBuff = dexBuff == null ? spell : (dexBuff.Level < spell.Level) ? spell : dexBuff;
							break;
						}
					}
				}
			}

			if (strBuff != null)
				CastSpell(strBuff, buffLine);

			if (dexBuff != null)
				CastSpell(dexBuff, buffLine);
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

			Spell tauntSpell = null;

			// Find the best paladin taunt for this level.
			foreach (Spell spell in chantsList)
			{
				if (spell.SpellType == eSpellType.Taunt && spell.Level <= Level)
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

		public override bool WhisperReceive(GameLiving source, string text)
		{
			// Everything below this comment should not exist in a strict 1.65 level. Feel free to add it back in if desired.
			return false;
			GamePlayer owner = (Brain as IControlledBrain).Owner as GamePlayer;

			if (source == null || source != owner)
				return false;

			switch (text.ToLower())
			{
				case "arawn":
				{
					string taunt = "As one of the many cadaverous servants of Arawn, I am able to [taunt] your enemies so that they will focus on me instead of you.";
					string empower = "You may also [empower] me with just a word.";

					switch (Name.ToLower())
					{
						case "minor zombie servant":
						case "lesser zombie servant":
						case "zombie servant":
						case "reanimated servant":
						case "necroservant":
						{
							SayTo(owner, taunt);
							return true;
						}
						case "greater necroservant":
						{
							SayTo(owner, $"{taunt} I can also inflict [poison] or [disease] on your enemies. {empower}");
							return true;
						}
						case "abomination":
						{
							SayTo(owner, $"As one of the chosen warriors of Arawn, I have a mighty arsenal of weapons at your disposal. If you wish it, I am able to [taunt] your enemies so that they will focus on me instead of you. {empower}");
							return true;
						}
						default:
							return false;
					}
				}
				case "disease":
				{
					InventoryItem item = Inventory?.GetItem(eInventorySlot.RightHandWeapon);

					if (item != null)
					{
						item.ProcSpellID = (int)Procs.Disease;
						SayTo(owner, eChatLoc.CL_SystemWindow, "As you command.");
					}

					return true;
				}
				case "empower":
				{
					SayTo(owner, eChatLoc.CL_SystemWindow, "As you command.");
					Empower();
					return true;
				}
				case "poison":
				{
					InventoryItem item = Inventory?.GetItem(eInventorySlot.RightHandWeapon);

					if (item != null)
					{
						item.ProcSpellID = (int)Procs.Poison;
						SayTo(owner, eChatLoc.CL_SystemWindow, "As you command.");
					}

					return true;
				}
				case "taunt":
				{
					ToggleTauntMode();
					return true;
				}
				case "weapons":
				{
					if (Name != "abomination")
						return false;

					SayTo(owner, "What weapon do you command me to wield? A [fiery sword], [icy sword], [poisonous sword] or a [flaming mace], [frozen mace], [venomous mace]?");
					return true;
				}
				case "fiery sword":
				case "icy sword":
				case "poisonous sword":
				case "flaming mace":
				case "frozen mace":
				case "venomous mace":
				{
					if (Name != "abomination")
						return false;

					string templateID = string.Format("{0}_{1}", Name, text.Replace(" ", "_"));

					if (LoadEquipmentTemplate(templateID))
						SayTo(owner, eChatLoc.CL_SystemWindow, "As you command.");

					return true;
				}
				default:
					return false;
			}
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

			GameNpcInventoryTemplate inventoryTemplate = new();

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
