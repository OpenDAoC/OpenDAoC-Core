using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

#region Amalgamate Parthanan
namespace Core.GS;

public class AmalgamateParthanan : GameNpc
{
	public AmalgamateParthanan() : base() { }
    #region Immune to specific dammage/range attack
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			GameLiving target = source as GameLiving;
			if (target == null || target.ActiveWeapon == null) return;
			if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
				damageType == EDamageType.Energy || damageType == EDamageType.Heat
				|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit || target.ActiveWeapon.Object_Type == (int)EObjectType.RecurvedBow || target.ActiveWeapon.Object_Type == (int)EObjectType.Fired)
			{
				GamePlayer truc;
				if (source is GamePlayer)
					truc = (source as GamePlayer);
				else
					truc = ((source as GameSummonedPet).Owner as GamePlayer);
				if (truc != null)
					truc.Out.SendMessage(Name + " is immune to this form of attack!", EChatType.CT_SpellResisted, EChatLoc.CL_ChatWindow);
				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
		if (source is GameNpc)//for charmed pets or other faction mobs
		{
			GameNpc npc = source as GameNpc;
			if (npc.ActiveWeapon != null && npc.ActiveWeaponSlot == EActiveWeaponSlot.Distance)
			{
				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
				damageType == EDamageType.Energy || damageType == EDamageType.Heat
				|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit)
			{
				base.TakeDamage(source, damageType, 0, 0);
				return;
			}
			else //take dmg
			{
				base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			}
		}
	}
	#endregion

	public override void StartAttack(GameObject target)//dont attack in initial phase after spawn
	{
		#region Lough Derg
		if (PackageID == "ParthananBossLoughDerg")
		{
			if (ParthananFarmController1Brain.SacrificeParthanan1)
				return;
			else
				base.StartAttack(target);
		}
		#endregion
		#region Connacht
		if (PackageID == "ParthananBossConnacht")
		{
			if (ParthananFarmController2Brain.SacrificeParthanan2)
				return;
			else
				base.StartAttack(target);
		}
		//2nd farm
		if (PackageID == "ParthananBossConnacht2")
		{
			if (ParthananFarmController2bBrain.SacrificeParthanan2b)
				return;
			else
				base.StartAttack(target);
		}
		#endregion
		#region Lough Gur
		if (PackageID == "ParthananBossLoughGur")
		{
			if (ParthananFarmController3Brain.SacrificeParthanan3)
				return;
			else
				base.StartAttack(target);
		}
		//2nd farm
		if (PackageID == "ParthananBossLoughGur2")
		{
			if (ParthananFarmController3bBrain.SacrificeParthanan3b)
				return;
			else
				base.StartAttack(target);
		}
        #endregion
    }
    public override bool HasAbility(string keyName)//immune to cc and dmg(in certain situation only)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;
        #region Lough Derg
        if (ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg" && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
        #endregion
        #region Connacht
        if (ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht" && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
		if (ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2" && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
        #endregion
        #region Lough Gur
        if (ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur" && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
		if (ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2" && IsAlive && keyName == GS.Abilities.DamageImmunity)
			return true;
        #endregion
        return base.HasAbility(keyName);
	}

	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157792);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		AmalgamateParthananBrain sbrain = new AmalgamateParthananBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		RespawnInterval = -1;
		bool success = base.AddToWorld();
		if (success)
		{
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
		}
		return success;
	}
	public override int MaxHealth
	{
		get { return 3000; }
	}
	public override void Die(GameObject killer)
    {
		#region Lough Derg
		if (PackageID == "ParthananBossLoughDerg")
		{
			ParthananFarmController1Brain.LoughDergBoss = 0;
			ParthananFarmController1Brain.BossIsUP = false;
			ParthananFarmController1Brain.ParthansCanDie = false;
			if (ParthananFarmController1Brain.MinParthAround.Count > 0)
				ParthananFarmController1Brain.MinParthAround.Clear();
			ParthananFarmController1Brain.MobsToKillLoughDerg = Util.Random(60, 120);
		}
        #endregion
        #region Connacht
        if (PackageID == "ParthananBossConnacht")
		{
			ParthananFarmController2Brain.ConnachtBoss = 0;
			ParthananFarmController2Brain.BossIsUP2 = false;
			ParthananFarmController2Brain.ParthansCanDie2 = false;
			if (ParthananFarmController2Brain.MinParthAround2.Count > 0)
				ParthananFarmController2Brain.MinParthAround2.Clear();
			ParthananFarmController2Brain.MobsToKillConnacht = Util.Random(60, 120);
		}
		//2nd farm
		if (PackageID == "ParthananBossConnacht2")
		{
			ParthananFarmController2bBrain.Connacht2Boss = 0;
			ParthananFarmController2bBrain.BossIsUP2b = false;
			ParthananFarmController2bBrain.ParthansCanDie2b = false;
			if (ParthananFarmController2bBrain.MinParthAround2b.Count > 0)
				ParthananFarmController2bBrain.MinParthAround2b.Clear();
			ParthananFarmController2bBrain.MobsToKillConnacht2 = Util.Random(60, 80);
		}
        #endregion
        #region Lough Gur
        if (PackageID == "ParthananBossLoughGur")
		{
			ParthananFarmController3Brain.LoughGurBoss = 0;
			ParthananFarmController3Brain.BossIsUP3 = false;
			ParthananFarmController3Brain.ParthansCanDie3 = false;
			if (ParthananFarmController3Brain.MinParthAround3.Count > 0)
				ParthananFarmController3Brain.MinParthAround3.Clear();
			ParthananFarmController3Brain.MobsToKillLoughGur = Util.Random(60, 120);
		}
		//2nd farm
		if (PackageID == "ParthananBossLoughGur2")
		{
			ParthananFarmController3bBrain.LoughGur2Boss = 0;
			ParthananFarmController3bBrain.BossIsUP3b = false;
			ParthananFarmController3bBrain.ParthansCanDie3b = false;
			if (ParthananFarmController3bBrain.MinParthAround3b.Count > 0)
				ParthananFarmController3bBrain.MinParthAround3b.Clear();
			ParthananFarmController3bBrain.MobsToKillLoughGur2 = Util.Random(60, 120);
		}
        #endregion
        base.Die(killer);
    }
    #region Effects
    protected int Show_Effect(EcsGameTimer timer)
	{
        #region Lough Derg
        if (IsAlive && ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2909, 1);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 1500);
		}
		if (IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
        #endregion
        #region Connacht
        if (IsAlive && ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2909, 1);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 1500);
		}
		if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);

		//2nd farm
		if (IsAlive && ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2909, 1);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 1500);
		}
		if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
		#endregion
		#region Lough Gur
		if (IsAlive && ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2909, 1);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 1500);
		}
		if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossLoughGur")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);

		//2nd farm
		if (IsAlive && ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellCastAnimation(this, 2909, 1);
			}
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoCast), 1500);
		}
		if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
		#endregion
		return 0;
	}
	protected int DoCast(EcsGameTimer timer)
	{
        #region Lough Derg
        if (IsAlive && ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1500);
		if(IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
        #endregion
        #region Connacht
        if (IsAlive && ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1500);
		if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);

		//2nd farm
		if (IsAlive && ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1500);
		if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
		#endregion
		#region Lough Gur
		if (IsAlive && ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1500);
		if (IsAlive && !ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);

		//2nd farm
		if (IsAlive && ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 1500);
		if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
			new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(DoEndCast), 100);
		#endregion
		return 0;
	}
	protected int DoEndCast(EcsGameTimer timer)
	{
        #region Lough Derg
        if (IsAlive && !ParthananFarmController1Brain.SacrificeParthanan1 && PackageID == "ParthananBossLoughDerg")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
			}
		}
        #endregion
        #region Connacht
        if (IsAlive && !ParthananFarmController2Brain.SacrificeParthanan2 && PackageID == "ParthananBossConnacht")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
			}
		}
		//2nd farm
		if (IsAlive && !ParthananFarmController2bBrain.SacrificeParthanan2b && PackageID == "ParthananBossConnacht2")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
			}
		}
		#endregion
		#region Lough Gur
		if (IsAlive && !ParthananFarmController3Brain.SacrificeParthanan3 && PackageID == "ParthananBossLoughGur")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
			}
		}
		//2nd farm
		if (IsAlive && !ParthananFarmController3bBrain.SacrificeParthanan3b && PackageID == "ParthananBossLoughGur2")
		{
			foreach (GamePlayer player in GetPlayersInRadius(10000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
			}
		}
		#endregion
		return 0;
	}
	#endregion
}
#endregion

#region Parthanans
public class Parthanan : GameNpc
{
	public Parthanan() : base() { }

	public override bool AddToWorld()
	{

		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;

		ParthananBrain sbrain = new ParthananBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
	public override void Die(GameObject killer)
	{
        #region Lough Derg
        if (!ParthananFarmController1Brain.SacrificeParthanan1)
		{
			if (PackageID == "ParthananLoughDerg")
				++ParthananFarmController1Brain.ParthanansKilledFarm1;
		}
		else
        {
			if (PackageID == "ParthananLoughDerg")
			{
				if (ParthananFarmController1Brain.MinParthAround.Contains(this))
					ParthananFarmController1Brain.MinParthAround.Remove(this);
			}
		}
        #endregion
        #region Connacht
        if (!ParthananFarmController2Brain.SacrificeParthanan2)
		{
			if (PackageID == "ParthananConnacht")
				++ParthananFarmController2Brain.ParthanansKilledFarm2;
		}
		else
		{
			if (PackageID == "ParthananConnacht")
			{
				if (ParthananFarmController2Brain.MinParthAround2.Contains(this))
					ParthananFarmController2Brain.MinParthAround2.Remove(this);
			}
		}
		//2nd farm
		if (!ParthananFarmController2bBrain.SacrificeParthanan2b)
		{
			if (PackageID == "ParthananConnacht2")
				++ParthananFarmController2bBrain.ParthanansKilledFarm2b;
		}
		else
		{
			if (PackageID == "ParthananConnacht2")
			{
				if (ParthananFarmController2bBrain.MinParthAround2b.Contains(this))
					ParthananFarmController2bBrain.MinParthAround2b.Remove(this);
			}
		}
        #endregion
        #region Lough Gur
        if (!ParthananFarmController3Brain.SacrificeParthanan3)
		{
			if (PackageID == "ParthananLoughGur")
				++ParthananFarmController3Brain.ParthanansKilledFarm3;
		}
		else
		{
			if (PackageID == "ParthananLoughGur")
			{
				if (ParthananFarmController3Brain.MinParthAround3.Contains(this))
					ParthananFarmController3Brain.MinParthAround3.Remove(this);
			}
		}
		//2nd farm
		if (!ParthananFarmController3bBrain.SacrificeParthanan3b)
		{
			if (PackageID == "ParthananLoughGur2")
				++ParthananFarmController3bBrain.ParthanansKilledFarm3b;
		}
		else
		{
			if (PackageID == "ParthananLoughGur2")
			{
				if (ParthananFarmController3bBrain.MinParthAround3b.Contains(this))
					ParthananFarmController3bBrain.MinParthAround3b.Remove(this);
			}
		}
        #endregion
        base.Die(killer);
	}
}
#endregion

#region Parthanan Farm Controllers
#region Lough Derg
public class ParthananFarmController1 : GameNpc
{
	public ParthananFarmController1() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Parthanan Farm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		ParthananFarmController1Brain sbrain = new ParthananFarmController1Brain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion

#region Connacht
public class ParthananFarmController2 : GameNpc
{
	public ParthananFarmController2() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Parthanan Farm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		ParthananFarmController2Brain sbrain = new ParthananFarmController2Brain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}

/// <summary>
/// /////////////////////////////////////////////////////2nd farm
/// </summary>
public class ParthananFarmController2b : GameNpc
{
	public ParthananFarmController2b() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Parthanan Farm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		ParthananFarmController2bBrain sbrain = new ParthananFarmController2bBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion

#region Lough Gur
public class ParthananFarmController3 : GameNpc
{
	public ParthananFarmController3() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Parthanan Farm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		ParthananFarmController3Brain sbrain = new ParthananFarmController3Brain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}

///////////////////////////////////////////////////////// 2nd farm
public class ParthananFarmController3b : GameNpc
{
	public ParthananFarmController3b() : base()
	{
	}
	public override bool IsVisibleToPlayers => true;
	public override bool AddToWorld()
	{
		Name = "Parthanan Farm Controller";
		GuildName = "DO NOT REMOVE";
		Level = 50;
		Model = 665;
		RespawnInterval = 5000;
		Flags = (ENpcFlags)28;

		ParthananFarmController3bBrain sbrain = new ParthananFarmController3bBrain();
		SetOwnBrain(sbrain);
		base.AddToWorld();
		return true;
	}
}
#endregion
#endregion