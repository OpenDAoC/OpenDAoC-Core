using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Abomos Soultrapper
public class AbomosSoultrapper : GameEpicBoss
{
	public AbomosSoultrapper() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Abomos the Soultrapper Initializing...");
	}
	public override void OnAttackEnemy(AttackData ad) //on enemy actions
	{
		if (Util.Chance(35))
		{
			if (ad != null && (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle))
			{
				CastSpell(LifedrainProc, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
		}
		base.OnAttackEnemy(ad);
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20; // dmg reduction for melee dmg
			case EDamageType.Crush: return 20; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
			default: return 30; // dmg reduction for rest resists
		}
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
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (IsOutOfTetherRange)
			{
				if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
					|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
					|| damageType == EDamageType.Slash)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
			}
			else//take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
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
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157522);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 841, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		VisibleActiveWeaponSlots = 34;
		MeleeDamageType = EDamageType.Slash;

		AbomosSoultrapperBrain sbrain = new AbomosSoultrapperBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	private Spell m_LifedrainProc;
	private Spell LifedrainProc
	{
		get
		{
			if (m_LifedrainProc == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 710;
				spell.Icon = 710;
				spell.TooltipId = 710;
				spell.Value = -50;
				spell.LifeDrainReturn = 50;
				spell.Name = "Lifedrain";
				spell.Damage = 150;
				spell.Range = 350;
				spell.SpellID = 11793;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.Lifedrain.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_LifedrainProc = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LifedrainProc);
			}
			return m_LifedrainProc;
		}
	}
}
#endregion Abomos Soultrapper

#region Abomos adds
public class AbomosAdd : GameNpc
{
	public AbomosAdd() : base()
	{
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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 5000; }
	}
	public static int AddsCount= 0;
	public override void Die(GameObject killer)
	{
		--AddsCount;
		base.Die(killer);
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
	public override bool AddToWorld()
	{
		Model = 826;
		Name = "Abomos Servant";
		RespawnInterval = -1;
		++AddsCount;

		Size = (byte)Util.Random(80, 100);
		Level = (byte)Util.Random(50,55);
		MaxSpeedBase = 200;

		Faction = FactionMgr.GetFactionByID(93);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(93));
		Realm = ERealm.None;

		Strength = 100;
		Dexterity = 200;
		Constitution = 100;
		Quickness = 125;
		Piety = 150;
		Intelligence = 150;

		AbomosAddBrain adds = new AbomosAddBrain();
		SetOwnBrain(adds);
		LoadedFromScript = false;
		base.AddToWorld();
		return true;
	}
}
#endregion Abomos adds