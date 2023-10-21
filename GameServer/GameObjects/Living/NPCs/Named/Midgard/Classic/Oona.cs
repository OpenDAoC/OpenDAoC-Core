using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Oona
public class Oona : GameEpicNPC
{
	public Oona() : base() { }
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 20;// dmg reduction for melee dmg
			case EDamageType.Crush: return 20;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
			default: return 20;// dmg reduction for rest resists
		}
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
			if (!source.IsWithinRadius(spawn, TetherRange))//dont take any dmg 
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
						truc.Out.SendMessage(Name + " is immune to damage form this distance!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
		get { return 10000; }
	}
	public override bool AddToWorld()
	{
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc.Brain is OonaBrain)
				return false;
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164669);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		OonaBrain sbrain = new OonaBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
    public override void StartAttack(GameObject target)
    {
    }
    public override void Die(GameObject killer)
	{
		foreach (GameNpc npc in GetNPCsInRadius(8000))
		{
			if (npc != null && npc.IsAlive && npc.Brain is OonaUndeadAddBrain)
				npc.Die(this);
		}
		base.Die(killer);
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(4500))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public override void EnemyKilled(GameLiving enemy)
    {
		GamePlayer player = enemy as GamePlayer;
		if (enemy is GamePlayer)
		{
			if (player != null)
			{
				OonaUndeadAdd npc = new OonaUndeadAdd();
				npc.Name = "undead " + player.RaceName;
				if (player.Race == (int)ERace.Dwarf && player.Gender == EGender.Male)
					npc.Model = 185;
				if (player.Race == (int)ERace.Dwarf && player.Gender == EGender.Female)
					npc.Model = 194;
				if (player.Race == (int)ERace.Norseman && player.Gender == EGender.Male)
					npc.Model = 153;
				if (player.Race == (int)ERace.Norseman && player.Gender == EGender.Female)
					npc.Model = 162;
				if (player.Race == (int)ERace.Kobold && player.Gender == EGender.Male)
					npc.Model = 169;
				if (player.Race == (int)ERace.Kobold && player.Gender == EGender.Female)
					npc.Model = 178;
				if (player.Race == (int)ERace.Troll && player.Gender == EGender.Male)
					npc.Model = 137;
				if (player.Race == (int)ERace.Troll && player.Gender == EGender.Female)
					npc.Model = 146;
				if (player.Race == (int)ERace.Valkyn && player.Gender == EGender.Male)
					npc.Model = 773;
				if (player.Race == (int)ERace.Valkyn && player.Gender == EGender.Female)
					npc.Model = 782;
				npc.Gender = player.Gender;
				npc.X = player.X;
				npc.Y = player.Y;
				npc.Z = player.Z;
				npc.Flags = ENpcFlags.GHOST;
				npc.Heading = player.Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
				BroadcastMessage(String.Format("Perhaps your pathetic gods will grant you another life, {0}. In the meantime, Hibernia shall defeat Midgard, and your spirit shall help!",player.Name));
			}
		}
		base.EnemyKilled(enemy);
    }
}
#endregion Oona

#region Oona's Undead Soldiers
public class OonaUndeadSoldier : GameNpc
{
	public OonaUndeadSoldier() : base() { }
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167424);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		OonaUndeadSoldierBrain sbrain = new OonaUndeadSoldierBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion Oona's Undead Soldiers

#region Oona's Undead adds
public class OonaUndeadAdd: GameNpc
{
	public OonaUndeadAdd() : base() { }
	public override bool AddToWorld()
	{
		Level = (byte)Util.Random(36,38);
		Size = 50;
		MaxSpeedBase = 225;
		RoamingRange = 200;

		OonaUndeadAddBrain sbrain = new OonaUndeadAddBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		base.AddToWorld();
		return true;
	}
}
#endregion Oona's Undead adds