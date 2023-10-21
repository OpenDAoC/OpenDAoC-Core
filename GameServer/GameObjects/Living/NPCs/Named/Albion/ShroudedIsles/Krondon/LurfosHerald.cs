using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS;

public class LurfosHerald : GameEpicBoss
{
	public LurfosHerald() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Lurfos the Herald Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 30;// dmg reduction for rest resists
		}
	}
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override int AttackRange
	{
		get { return 350; }
		set { }
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 30000; }
	}
	public override bool AddToWorld()
	{
		Model = 919;
		Level = (byte)(Util.Random(75, 78));
		Name = "Lurfos the Herald";
		Size = 120;

		Strength = 280;
		Dexterity = 150;
		Constitution = 100;
		Quickness = 80;
		Piety = 200;
		Intelligence = 200;
		Charisma = 200;
		Empathy = 400;

		MaxSpeedBase = 250;
		MaxDistance = 3500;
		TetherRange = 3800;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Slash;
		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		LurfosHeraldBrain sbrain = new LurfosHeraldBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
        {
			if(LurfosHeraldBrain.IsColdWeapon)
				CastSpell(Weapon_Cold, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (LurfosHeraldBrain.IsHeatWeapon)
				CastSpell(Weapon_Heat, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
        base.OnAttackEnemy(ad);
    }
	private Spell m_Weapon_Heat;
	private Spell Weapon_Heat
	{
		get
		{
			if (m_Weapon_Heat == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 1;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.TooltipId = 360;
				spell.Damage = 300;
				spell.Name = "Fire Strike";
				spell.Range = 500;
				spell.Radius = 300;
				spell.SpellID = 11885;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Heat;
				m_Weapon_Heat = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Weapon_Heat);
			}
			return m_Weapon_Heat;
		}
	}
	private Spell m_Weapon_Cold;
	private Spell Weapon_Cold
	{
		get
		{
			if (m_Weapon_Cold == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 1;
				spell.ClientEffect = 161;
				spell.Icon = 161;
				spell.TooltipId = 161;
				spell.Damage = 300;
				spell.Name = "Ice Strike";
				spell.Range = 500;
				spell.Radius = 300;
				spell.SpellID = 11886;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Cold;
				m_Weapon_Cold = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Weapon_Cold);
			}
			return m_Weapon_Cold;
		}
	}
}