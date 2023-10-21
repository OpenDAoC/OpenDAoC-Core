using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;

namespace Core.GS;

#region Beran Supply Master
public class BeranSupplyMaster : GameEpicBoss
{
	public BeranSupplyMaster() : base() { }

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Beran the Supply Master Initializing...");
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
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158369);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		BeranSupplyMasterBrain sbrain = new BeranSupplyMasterBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    public override void Die(GameObject killer)
    {
		foreach (GameNpc npc in GetNPCsInRadius(2500))
		{
			if (npc != null)
			{
				if (npc.IsAlive && npc.Name.ToLower() == "onstal hyrde" && npc.RespawnInterval == -1)
					npc.Die(npc);
			}
		}
		base.Die(killer);
    }
}
#endregion Beran Supply Master

#region Barrel Explosion Mob
public class BarrelExplosive : GameNpc
{
	public BarrelExplosive() : base() { }

    public override void StartAttack(GameObject target)
    {
    }
    public override int MaxHealth
	{
		get { return 20000; }
	}
	public override bool AddToWorld()
	{
		Model = 665;
		Name = "Explosion";
		Dexterity = 200;
		Piety = 200;
		Intelligence = 200;
		Empathy = 200;
		Level = 80;
		RespawnInterval = -1;
		Size = 100;
		Flags ^= ENpcFlags.DONTSHOWNAME;
		Flags ^= ENpcFlags.CANTTARGET;
		Flags ^= ENpcFlags.STATUE;

		Faction = FactionMgr.GetFactionByID(8);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

		BarrelExplosiveBrain sbrain = new BarrelExplosiveBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Explode), 8000); //8 seconds until this will explode and deal heavy heat dmg
		}
		return success;
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(this, this, 5976, 0, false, 0x01);

			return 2400;
		}

		return 0;
	}
	
	protected int Explode(EcsGameTimer timer)
	{
		if (IsAlive)
		{
			SetGroundTarget(X, Y, Z);			
			CastSpell(Barrel_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(KillBomb), 500);
		}
		return 0;
	}
	public int KillBomb(EcsGameTimer timer)
	{
		if (IsAlive)
			RemoveFromWorld();
		return 0;
	}
	private Spell m_Barrel_aoe;
	private Spell Barrel_aoe
	{
		get
		{
			if (m_Barrel_aoe == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 2308;
				spell.Icon = 2308;
				spell.TooltipId = 2308;
				spell.Damage = 1200;
				spell.Name = "Explosion";
				spell.Radius = 1000;
				spell.Range = 1000;
				spell.SpellID = 11880;
				spell.Target = ESpellTarget.AREA.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Barrel_aoe = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Barrel_aoe);
			}
			return m_Barrel_aoe;
		}
	}
}
#endregion Barrel Explosion Mob