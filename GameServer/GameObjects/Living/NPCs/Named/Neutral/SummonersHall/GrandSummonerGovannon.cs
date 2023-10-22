using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Grand Summoner Govannon
public class GrandSummonerGovannon : GameEpicBoss
{
	public GrandSummonerGovannon() : base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40; // dmg reduction for melee dmg
			case EDamageType.Crush: return 40; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
			default: return 70; // dmg reduction for rest resists
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
		get { return 300000; }
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
						truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
		if (IsAlive && keyName == AbilityConstants.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
    public override void OnAttackEnemy(AttackData ad)
    {
		if(ad != null && GrandSummonerGovannonBrain.Stage2==true)
        {
			if(Util.Chance(35))//30% chance to make a bleed
            {
				if (!ad.Target.effectListComponent.ContainsEffectForEffectType(EEffect.Bleed))
					CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
        }
        base.OnAttackEnemy(ad);
    }
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
		{
			if (npc != null)
			{
				if (npc.IsAlive && (npc.Brain is SummonedDemonBrain || npc.Brain is SummonedSacrificeBrain || npc.Brain is ShadeOfAelfgarBrain))
					npc.RemoveFromWorld();
			}
		}
		base.Die(killer);
    }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18801);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		Faction = FactionMgr.GetFactionByID(206);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		GrandSummonerGovannonBrain.SpawnSacrifices1 = false;
		GrandSummonerGovannonBrain.Stage2 = false;

		GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
		template.AddNPCEquipment(EInventorySlot.TorsoArmor, 86, 43, 0, 0); //Slot,model,color,effect,extension
		template.AddNPCEquipment(EInventorySlot.ArmsArmor, 88, 43);
		template.AddNPCEquipment(EInventorySlot.LegsArmor, 87, 43);
		template.AddNPCEquipment(EInventorySlot.HandsArmor, 89, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.FeetArmor, 90, 43, 0, 0);
		template.AddNPCEquipment(EInventorySlot.Cloak, 57, 65, 0, 0);
		template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 442, 0, 0, 0);
		Inventory = template.CloseTemplate();
		SwitchWeapon(EActiveWeaponSlot.TwoHanded);

		GrandSummonerGovannonBrain sbrain = new GrandSummonerGovannonBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		GameNpc[] npcs;

		npcs = WorldMgr.GetNPCsByNameFromRegion("Grand Summoner Govannon", 248, (ERealm)0);
		if (npcs.Length == 0)
		{
			log.Warn("Grand Summoner Govannon not found, creating it...");

			log.Warn("Initializing Grand Summoner Govannon...");
			GrandSummonerGovannon OF = new GrandSummonerGovannon();
			OF.Name = "Grand Summoner Govannon";
			OF.Model = 61;
			OF.Realm = 0;
			OF.Level = 80;
			OF.Size = 65;
			OF.CurrentRegionID = 248;//OF summoners hall

			OF.Strength = 5;
			OF.Intelligence = 200;
			OF.Piety = 200;
			OF.Dexterity = 200;
			OF.Constitution = 100;
			OF.Quickness = 125;
			OF.Empathy = 300;
			OF.BodyType = (ushort)EBodyType.Humanoid;
			OF.MeleeDamageType = EDamageType.Crush;
			OF.Faction = FactionMgr.GetFactionByID(206);
			OF.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

			OF.X = 34577;
			OF.Y = 31371;
			OF.Z = 15998;
			OF.MaxDistance = 2000;
			OF.TetherRange = 1300;
			OF.MaxSpeedBase = 250;
			OF.Heading = 19;
			OF.IsCloakHoodUp = true;

			GrandSummonerGovannonBrain ubrain = new GrandSummonerGovannonBrain();
			ubrain.AggroLevel = 100;
			ubrain.AggroRange = 600;
			OF.SetOwnBrain(ubrain);
			OF.AddToWorld();
			OF.Brain.Start();
			OF.SaveIntoDatabase();
		}
		else
			log.Warn("Grand Summoner Govannon exist ingame, remove it and restart server if you want to add by script code.");
	}
	private Spell m_Bleed;
	private Spell Bleed
	{
		get
		{
			if (m_Bleed == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 2130;
				spell.Icon = 3411;
				spell.TooltipId = 3411;
				spell.Damage = 85;
				spell.Name = "Demon's Scar";
				spell.Description = "Does 85 damage to a target every 3 seconds for 30 seconds.";
				spell.Message1 = "You are bleeding! ";
				spell.Message2 = "{0} is bleeding! ";
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 250;
				spell.SpellID = 11762;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.StyleBleeding.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Bleed = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
			}
			return m_Bleed;
		}
	}
}
#endregion Grand Summoner Govannon

#region Summoner Sacrifice
public class SummonedSacrifice : GameNpc
{
	public override int MaxHealth
	{
		get { return 6000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 500;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 35;// dmg reduction for melee dmg
			case EDamageType.Crush: return 35;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 35;// dmg reduction for melee dmg
			default: return 55;// dmg reduction for rest resists
		}
	}
	public static int SacrificeKilledCount = 0;
    public override void Die(GameObject killer)
    {
        base.Die(killer);
    }
    public override bool AddToWorld()
	{
		Model = 122;
		Name = "summoned sacrifice";
		SacrificeKilledCount = 0;
		RespawnInterval = -1;
		Size = 45;
		Level = (byte)Util.Random(62, 68);
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		SummonedSacrificeBrain sacrifice = new SummonedSacrificeBrain();
		SetOwnBrain(sacrifice);
		base.AddToWorld();
		return true;
	}
}
#endregion Summoner Sacrifice

#region Summoned Demon
public class SummonedDemon : GameNpc
{
	public override int MaxHealth
	{
		get { return 6000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 500;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 35;// dmg reduction for melee dmg
			case EDamageType.Crush: return 35;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 35;// dmg reduction for melee dmg
			default: return 55;// dmg reduction for rest resists
		}
	}
	public static int SummonedDemonCount = 0;
	public override void Die(GameObject killer)
	{
		base.Die(killer);
	}
	public override bool AddToWorld()
	{
		Model = 253;
		Name = "summoned demon";
		SummonedDemonCount = 0;
		RespawnInterval = -1;
		Size = 30;
		Level = (byte)Util.Random(62, 68);
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		SummonedDemonBrain sacrifice = new SummonedDemonBrain();
		SetOwnBrain(sacrifice);
		base.AddToWorld();
		return true;
	}
}
#endregion Summoned Demon

#region Shade of Aelfgar
public class ShadeOfAelfgar : GameEpicNPC
{
	public override int MaxHealth
	{
		get { return 10000; }
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 35;// dmg reduction for melee dmg
			case EDamageType.Crush: return 35;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 35;// dmg reduction for melee dmg
			default: return 55;// dmg reduction for rest resists
		}
	}
	public static int ShadeOfAelfgarCount = 0;
	public override void Die(GameObject killer)
	{
		++ShadeOfAelfgarCount;
		base.Die(killer);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(18803);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		ShadeOfAelfgarBrain.RandomTarget = null;
		ShadeOfAelfgarBrain.CanPort = false;
		ShadeOfAelfgarCount = 0;
		RespawnInterval = -1;
		Faction = FactionMgr.GetFactionByID(187);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
		ShadeOfAelfgarBrain sacrifice = new ShadeOfAelfgarBrain();
		SetOwnBrain(sacrifice);
		base.AddToWorld();
		return true;
	}
	public override void OnAttackEnemy(AttackData ad)
	{
		if (Util.Chance(30))//30% chance to make a bleed
			CastSpell(AelfgarStun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		base.OnAttackEnemy(ad);
	}
	private Spell m_AelfgarStun;
	public Spell AelfgarStun
	{
		get
		{
			if (m_AelfgarStun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(20, 35);
				spell.ClientEffect = 3379;
				spell.Icon = 3379;
				spell.TooltipId = 3379;
				spell.Duration = 5;
				spell.DamageType = (int)EDamageType.Spirit;
				spell.Name = "Aelfgar's Shout";
				spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
				spell.Message1 = "You are stunned!";
				spell.Message2 = "{0} is stunned!";
				spell.Message3 = "You recover from the stun.";
				spell.Message4 = "{0} recovers from the stun.";
				spell.Range = 350;
				spell.Radius = 500;
				spell.SpellID = 11764;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.Type = ESpellType.Stun.ToString();
				m_AelfgarStun = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AelfgarStun);
			}
			return m_AelfgarStun;
		}
	}
}
#endregion Shade of Aelfgar