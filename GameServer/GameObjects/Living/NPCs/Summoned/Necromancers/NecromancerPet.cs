using System;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Calculators;
using Core.GS.Effects;
using Core.GS.Effects.Old;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.RealmAbilities;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS;

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

				if (newTarget && EffectList.GetOfType<PetTauntEffect>() != null)
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
	public NecromancerPet(INpcTemplate npcTemplate) : base(npcTemplate)
	{
		// Update max health on summon.
		GetModified(EProperty.MaxHealth);
		// Set immunities/load equipment/etc.
		switch (Name.ToLower())
		{
			case "lesser zombie servant":
			case "zombie servant":
				EffectList.Add(new MezzRootImmunityEffect());
				LoadEquipmentTemplate("barehand_weapon");
				DbInventoryItem item;
				if (Inventory != null && (item = Inventory.GetItem(EInventorySlot.RightHandWeapon)) != null)
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
				if (Inventory != null && (item = Inventory.GetItem(EInventorySlot.RightHandWeapon)) != null)
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

	/// <summary>
	/// Get modified bonuses for the pet; some bonuses come from the shade, some come from the pet.
	/// </summary>
	public override int GetModified(EProperty property)
	{
		if (Brain == null || (Brain as IControlledBrain) == null)
			return base.GetModified(property);

		switch (property)
		{
			case EProperty.MaxHealth:
			{
				int hitsCap = MaxHitPointsCalculator.GetItemBonusCap(Owner) + MaxHitPointsCalculator.GetItemBonusCapIncrease(Owner);
				int conFromRa = 0;
				int conFromItems = 0;
				int maxHealthFromItems = 0;
				double toughnessMod = 1.0;
				
				if ((Brain as IControlledBrain).GetLivingOwner() is GamePlayer playerOwner)
				{
					conFromRa = OfRaHelpers.GetStatEnhancerAmountForLevel(OfRaHelpers.GetAugConLevel(playerOwner));
					conFromItems = playerOwner.GetModifiedFromItems(EProperty.Constitution);
					maxHealthFromItems = playerOwner.ItemBonus[(int) EProperty.MaxHealth];
					OfRaToughnessAbility toughness = playerOwner.GetAbility<OfRaToughnessAbility>();

					if (toughness != null)
						toughnessMod = 1 + toughness.GetAmountForLevel(toughness.Level) * 0.01;
				}

				int conBonus = (int) ((conFromItems + conFromRa) * 3.1);
				int hitsBonus = 30 * Level + Math.Min(maxHealthFromItems, hitsCap);
				int totalBonus = conBonus + hitsBonus;
				return (int) (totalBonus * toughnessMod);
			}
			default:
				return base.GetModified(property);
		}
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
	public override void AutoSetStats(DbMob dbMob = null)
	{
		int levelMinusOne = Level - 1;

		if (Name.ToUpper() == "GREATER NECROSERVANT")
		{
			Strength = ServerProperty.NECRO_GREATER_PET_STR_BASE;
			Constitution = ServerProperty.NECRO_GREATER_PET_CON_BASE;
			Dexterity = ServerProperty.NECRO_GREATER_PET_DEX_BASE;
			Quickness = ServerProperty.NECRO_GREATER_PET_QUI_BASE;
			Intelligence = ServerProperty.NECRO_GREATER_PET_INT_BASE;

			if (Level > 1)
			{
				Strength += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_GREATER_PET_STR_MULTIPLIER);
				Constitution += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_GREATER_PET_CON_MULTIPLIER);
				Dexterity += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_GREATER_PET_DEX_MULTIPLIER);
				Quickness += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_GREATER_PET_QUI_MULTIPLIER);
				Intelligence += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_GREATER_PET_INT_MULTIPLIER);
			}
		}
		else
		{
			Strength = ServerProperty.NECRO_PET_STR_BASE;
			Constitution = ServerProperty.NECRO_PET_CON_BASE;
			Dexterity = ServerProperty.NECRO_PET_DEX_BASE;
			Quickness = ServerProperty.NECRO_PET_QUI_BASE;
			Intelligence = ServerProperty.NECRO_PET_INT_BASE;

			if (Level > 1)
			{
				Strength += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_PET_STR_MULTIPLIER);
				Constitution += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_PET_CON_MULTIPLIER);
				Dexterity += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_PET_DEX_MULTIPLIER);
				Quickness += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_PET_QUI_MULTIPLIER);
				Intelligence += (short) Math.Round(levelMinusOne * ServerProperty.NECRO_PET_INT_MULTIPLIER);
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
		PetTauntEffect tauntEffect = EffectList.GetOfType<PetTauntEffect>();
		GamePlayer owner = (Brain as IControlledBrain).Owner as GamePlayer;

		if (tauntEffect != null)
		{
			tauntEffect.Stop();
			owner.Out.SendMessage(string.Format("{0} seems to be less aggressive than before.", GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
		else
		{
			owner.Out.SendMessage(string.Format("{0} enters an aggressive stance.", GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			new PetTauntEffect().Start(this);
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
		if (ad.AttackType == EAttackType.Spell && ad.Damage > 0)
		{
			GamePlayer player = Owner as GamePlayer;
			string modmessage = "";

			if (ad.Modifier > 0)
				modmessage = " (+" + ad.Modifier + ")";
			else if (ad.Modifier < 0)
				modmessage = " (" + ad.Modifier + ")";

			player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.HitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.Damage, modmessage), EChatType.CT_Damaged, EChatLoc.CL_SystemWindow);

			if (ad.CriticalDamage > 0)
				player.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameLiving.AttackData.CriticallyHitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.CriticalDamage), EChatType.CT_Damaged, EChatLoc.CL_SystemWindow);
		}

		base.OnAttackedByEnemy(ad);
	}

	public override void ModifyAttack(AttackData attackData)
	{
		base.ModifyAttack(attackData);

		if ((Owner as GamePlayer).Client.Account.PrivLevel > (int)EPrivLevel.Player)
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
					case ESpellType.StrengthBuff:
					{
						strBuff = strBuff == null ? spell : (strBuff.Level < spell.Level) ? spell : strBuff;
						break;
					}
					case ESpellType.DexterityBuff:
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
			if (spell.SpellType == ESpellType.Taunt && spell.Level <= Level)
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
				DbInventoryItem item = Inventory?.GetItem(EInventorySlot.RightHandWeapon);

				if (item != null)
				{
					item.ProcSpellID = (int)Procs.Disease;
					SayTo(owner, EChatLoc.CL_SystemWindow, "As you command.");
				}

				return true;
			}
			case "empower":
			{
				SayTo(owner, EChatLoc.CL_SystemWindow, "As you command.");
				Empower();
				return true;
			}
			case "poison":
			{
				DbInventoryItem item = Inventory?.GetItem(EInventorySlot.RightHandWeapon);

				if (item != null)
				{
					item.ProcSpellID = (int)Procs.Poison;
					SayTo(owner, EChatLoc.CL_SystemWindow, "As you command.");
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
					SayTo(owner, EChatLoc.CL_SystemWindow, "As you command.");

				return true;
			}
			default:
				return false;
		}
	}

	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
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
			Inventory = new GameNpcInventory(inventoryTemplate);
			DbInventoryItem item;

			if ((item = Inventory.GetItem(EInventorySlot.TwoHandWeapon)) != null)
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

				SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			}
			else
			{
				if ((item = Inventory.GetItem(EInventorySlot.RightHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 37;
				}

				if ((item = Inventory.GetItem(EInventorySlot.LeftHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 37;
				}

				SwitchWeapon(EActiveWeaponSlot.Standard);
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