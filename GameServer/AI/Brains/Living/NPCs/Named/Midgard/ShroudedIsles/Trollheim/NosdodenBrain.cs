using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Styles;

namespace Core.GS.AI.Brains;

#region Nosdoden
public class NosdodenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public NosdodenBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
	}
	public static bool IsPulled = false;
	private bool SpawnAdds1 = false;
	private bool SpawnAdds2 = false;
	private bool SpawnAdds3 = false;
	private bool SpawnAdds4 = false;
	private bool SpawnAdds5 = false;
	private bool SpawnAdds6 = false;
	private bool SpawnAdds7 = false;
	private bool SpawnAdds8 = false;
	private bool SpawnAdds9 = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	#region Worm Dot
	public static bool CanCast2 = false;
	public static bool StartCastDOT = false;
	public static GamePlayer randomtarget2 = null;
	public static GamePlayer RandomTarget2
	{
		get { return randomtarget2; }
		set { randomtarget2 = value; }
	}
	List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
	public int PickRandomTarget2(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DOT.Contains(player))
						{
							Enemys_To_DOT.Add(player);
						}
					}
				}
			}
			if (Enemys_To_DOT.Count > 0)
			{
				if (CanCast2 == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
					RandomTarget2 = Target;//set random target to static RandomTarget
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDOT), 2000);
					CanCast2 = true;
				}
			}
		}
		return 0;
	}
	public int CastDOT(EcsGameTimer timer)
	{
		if (HasAggro && RandomTarget2 != null)
		{
			GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
			if (RandomTarget2 != null && RandomTarget2.IsAlive)
			{
				Body.TargetObject = RandomTarget2;
				Body.TurnTo(RandomTarget2);
				Body.CastSpell(NosdodenDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDOT), 5000);
		}
		return 0;
	}
	public int ResetDOT(EcsGameTimer timer)
	{
		Enemys_To_DOT.Clear();
		RandomTarget2 = null;
		CanCast2 = false;
		StartCastDOT = false;
		return 0;
	}
	#endregion
	#region Worm DD
	public static bool CanCast = false;
	public static bool StartCastDD = false;
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	List<GamePlayer> Enemys_To_DD = new List<GamePlayer>();
	public int PickRandomTarget(EcsGameTimer timer)
	{
		if (HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Enemys_To_DD.Contains(player))
						{
							Enemys_To_DD.Add(player);
						}
					}
				}
			}
			if (Enemys_To_DD.Count > 0)
			{
				if (CanCast == false)
				{
					GamePlayer Target = (GamePlayer)Enemys_To_DD[Util.Random(0, Enemys_To_DD.Count - 1)];//pick random target from list
					RandomTarget = Target;//set random target to static RandomTarget
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDD), 5000);
					BroadcastMessage(String.Format(Body.Name + " starts casting void magic at " + RandomTarget.Name + "."));
					CanCast = true;
				}
			}
		}
		return 0;
	}
	public int CastDD(EcsGameTimer timer)
	{
		if (HasAggro && RandomTarget != null)
		{
			GameLiving oldTarget = Body.TargetObject as GameLiving;//old target
			if (RandomTarget != null && RandomTarget.IsAlive)
			{
				Body.TargetObject = RandomTarget;
				Body.TurnTo(RandomTarget);
				Body.CastSpell(NosdodenDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDD), 5000);
		}
		return 0;
	}
	public int ResetDD(EcsGameTimer timer)
	{
		Enemys_To_DD.Clear();
		RandomTarget = null;
		CanCast = false;
		StartCastDD = false;
		return 0;
	}
	#endregion
	public static GameNpc spiritmob = null;
	public static GameNpc SpiritMob
	{
		get { return spiritmob; }
		set { spiritmob = value; }
	}
	public static GamePlayer playerrezzed = null;
	public static GamePlayer PlayerRezzed
	{
		get { return playerrezzed; }
		set { playerrezzed = value; }
	}
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
            #region Checks
            StartCastDOT = false;
			StartCastDD = false;
			CanCast2 = false;
			CanCast = false;
			RandomTarget = null;
			RandomTarget2 = null;
			CanKillSpirit = false;
			SpawnAdds1 = false;
			SpawnAdds2 = false;
			SpawnAdds3 = false;
			SpawnAdds4 = false;
			SpawnAdds5 = false;
			SpawnAdds6 = false;
			SpawnAdds7 = false;
			SpawnAdds8 = false;
			SpawnAdds9 = false;
            #endregion
            if (Enemys_To_DD.Count > 0)
				Enemys_To_DD.Clear();
			if (Enemys_To_DOT.Count > 0)
				Enemys_To_DOT.Clear();
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is NosdodenGhostAddBrain)
						npc.Die(Body);
				}
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is NosdodenSummonedAddsBrain)
						npc.RemoveFromWorld();
				}
				RemoveAdds = true;
			}
		}
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
            #region Summon Adds
            if (Body.HealthPercent <= 90 && SpawnAdds1==false)
            {
				SpawnEssences();
				SpawnAdds1 = true;
            }
			if (Body.HealthPercent <= 80 && SpawnAdds2 == false)
			{
				SpawnEssences();
				SpawnAdds2 = true;
			}
			if (Body.HealthPercent <= 70 && SpawnAdds3 == false)
			{
				SpawnEssences();
				SpawnAdds3 = true;
			}
			if (Body.HealthPercent <= 60 && SpawnAdds4 == false)
			{
				SpawnEssences();
				SpawnAdds4 = true;
			}
			if (Body.HealthPercent <= 50 && SpawnAdds5 == false)
			{
				SpawnEssences();
				SpawnAdds5 = true;
			}
			if (Body.HealthPercent <= 40 && SpawnAdds6 == false)
			{
				SpawnEssences();
				SpawnAdds6 = true;
			}
			if (Body.HealthPercent <= 30 && SpawnAdds7 == false)
			{
				SpawnEssences();
				SpawnAdds7 = true;
			}
			if (Body.HealthPercent <= 20 && SpawnAdds8 == false)
			{
				SpawnEssences();
				SpawnAdds8 = true;
			}
			if (Body.HealthPercent <= 10 && SpawnAdds9 == false)
			{
				SpawnEssences();
				SpawnAdds9 = true;
			}
            #endregion
            if (StartCastDOT == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget2), Util.Random(20000, 30000));
				StartCastDOT = true;
			}
			if (StartCastDD == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomTarget), Util.Random(35000, 45000));
				StartCastDD = true;
			}

			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				if (player != null && player.IsAlive)
				{
					foreach (GameNpc ghosts in Body.GetNPCsInRadius(5000))
					{
						if (ghosts != null && ghosts.IsAlive && ghosts.Brain is NosdodenGhostAddBrain)
						{
							if (Regex.IsMatch(ghosts.Name, string.Format(@"\b{0}\b", player.Name)))//check if spirit name contains exact player name
							{
								SpiritMob = ghosts;
								PlayerRezzed = player;
							}
						}
					}
				}
			}
			if(CanKillSpirit == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(KillSpirit), 2000);
				CanKillSpirit = true;
            }
		}
		base.Think();
	}
	private bool CanKillSpirit = false;
	private int KillSpirit(EcsGameTimer timer)
	{
		if (Body.IsAlive && SpiritMob != null && SpiritMob.IsAlive && PlayerRezzed != null && PlayerRezzed.IsAlive)
		{
			BroadcastMessage(String.Format("Life essense returned back to " + PlayerRezzed.Name + "."));
			SpiritMob.Die(Body);
		}
		CanKillSpirit = false;
		return 0;
	}
	private void SpawnEssences()
	{
		for (int i = 0; i < Util.Random(12, 18); i++)
		{
			NosdodenSummonedAdds add = new NosdodenSummonedAdds();
			add.X = Body.X + Util.Random(-200, 200);
			add.Y = Body.Y + Util.Random(-200, 200);
			add.Z = Body.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.AddToWorld();
		}
	}
    #region Spells
    private Spell m_NosdodenDot;
	private Spell NosdodenDot
	{
		get
		{
			if (m_NosdodenDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4099;
				spell.Icon = 4099;
				spell.TooltipId = 4099;
				spell.Name = "Nosdoden's Venom";
				spell.Description = "Inflicts 150 damage to the target every 4 sec for 60 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.Damage = 150;
				spell.Duration = 60;
				spell.Frequency = 40;
				spell.Range = 1800;
				spell.Radius = 500;
				spell.SpellID = 11856;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_NosdodenDot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NosdodenDot);
			}
			return m_NosdodenDot;
		}
	}
	private Spell m_NosdodenDD;
	private Spell NosdodenDD
	{
		get
		{
			if (m_NosdodenDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4568;
				spell.Icon = 4568;
				spell.TooltipId = 4568;
				spell.Name = "Call of Void";
				spell.Damage = 1100;
				spell.Range = 1800;
				spell.Radius = 550;
				spell.SpellID = 11857;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Cold;
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_NosdodenDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NosdodenDD);
			}
			return m_NosdodenDD;
		}
	}
	#endregion
}
#endregion Nosdoden

#region Nosdoden Ghost add
public class NosdodenGhostAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public NosdodenGhostAddBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
	}
	#region Mob Class Berserker
	private protected bool CanWalkBerserker = false;

	public static int TauntBerserkerID = 202;
	public static int TauntBerserkerClassID = 31; 
	public static Style tauntBerserker = SkillBase.GetStyleByID(TauntBerserkerID, TauntBerserkerClassID);

	public static int BackBerserkerID = 195;
	public static int BackBerserkerClassID = 31;
	public static Style BackBerserker = SkillBase.GetStyleByID(BackBerserkerID, BackBerserkerClassID);

	public static int AfterEvadeBerserkerID = 198;
	public static int AfterEvadeBerserkerClassID = 31;
	public static Style AfterEvadeBerserker = SkillBase.GetStyleByID(AfterEvadeBerserkerID, AfterEvadeBerserkerClassID);

	public static int EvadeFollowUpBerserkerID = 203;
	public static int EvadeFollowUpBerserkerClassID = 31;
	public static Style EvadeFollowUpBerserker = SkillBase.GetStyleByID(EvadeFollowUpBerserkerID, EvadeFollowUpBerserkerClassID);
	public void IsBerserker()
    {
		if (Body.PackageID == "NosdodenGhostBerserker")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.Standard);
			Body.VisibleActiveWeaponSlots = 16;
			Body.EvadeChance = 60;
			if (Body.IsAlive)
			{
				if (!Body.Styles.Contains(tauntBerserker))
					Body.Styles.Add(tauntBerserker);
				if (!Body.Styles.Contains(AfterEvadeBerserker))
					Body.Styles.Add(AfterEvadeBerserker);
				if (!Body.Styles.Contains(EvadeFollowUpBerserker))
					Body.Styles.Add(EvadeFollowUpBerserker);
				if (!Body.Styles.Contains(BackBerserker))
					Body.Styles.Add(BackBerserker);
			}
			if (!CheckProximityAggro())
			{
				CanWalkBerserker = false;
			}
			if (Body.InCombat && HasAggro)
            {
				if (Body.TargetObject != null)
                {
					GameLiving target = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if (angle >= 160 && angle <= 200)
                    {
						Body.Quickness = 100;
						Body.Strength = 220;
						Body.styleComponent.NextCombatStyle = BackBerserker;
					}
					else
                    {
						Body.Quickness = 100;
						Body.Strength = 180;
					}
					if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
					{
						if (CanWalkBerserker == false)
						{
							new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
							CanWalkBerserker = true;
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
					{
						CanWalkBerserker = false;//reset flag so can slam again
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Warrior
	private protected bool CanWalkWarrior = false;

	public static int TauntSwordWarriorID = 157;
	public static int TauntSwordWarriorClassID = 22;
	public static Style tauntSwordWarrior = SkillBase.GetStyleByID(TauntSwordWarriorID, TauntSwordWarriorClassID);

	public static int TauntHammerWarriorID = 167;
	public static int TauntHammerWarriorClassID = 22;
	public static Style tauntHammerWarrior = SkillBase.GetStyleByID(TauntHammerWarriorID, TauntHammerWarriorClassID);

	public static int TauntAxeWarriorID = 178;
	public static int TauntAxeWarriorClassID = 22;
	public static Style tauntAxeWarrior = SkillBase.GetStyleByID(TauntAxeWarriorID, TauntAxeWarriorClassID);

	public static int SlamWarriorID = 228;
	public static int SlamWarriorClassID = 22;
	public static Style slamWarrior = SkillBase.GetStyleByID(SlamWarriorID, SlamWarriorClassID);
	public void IsWarrior()
	{
		if (Body.PackageID == "NosdodenGhostWarrior")
		{
			if(Body.IsAlive && !HasAggro)
            {
				Body.ParryChance = 15;
				Body.BlockChance = 60;
				Body.SwitchWeapon(EActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
			}
			if (!CheckProximityAggro())
			{
				CanWalkWarrior = false;
			}
			if(Body.IsAlive)
            {
				if (!Body.Styles.Contains(slamWarrior))
					Body.Styles.Add(slamWarrior);
				if (!Body.Styles.Contains(Taunt2h))
					Body.Styles.Add(Taunt2h);
				if (!Body.Styles.Contains(Back2h))
					Body.Styles.Add(Back2h);
            }
			if (Body.InCombat && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					GameLiving target = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if (angle >= 160 && angle <= 200)
					{
						Body.Strength = 250;
						Body.ParryChance = 60;
						Body.BlockChance = 0;
						Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						Body.VisibleActiveWeaponSlots = 34;
						Body.styleComponent.NextCombatStyle = Back2h;
						Body.styleComponent.NextCombatBackupStyle = Taunt2h;
					}
					else
					{
						Body.Strength = 180;
						Body.Quickness = 100;
						Body.ParryChance = 15;
						Body.BlockChance = 60;
						Body.SwitchWeapon(EActiveWeaponSlot.Standard);
						Body.VisibleActiveWeaponSlots = 16;
						foreach (Style styles in Body.Styles)
						{
							if (styles != null)
							{
								if (styles.ID == 157 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntSwordWarrior;

								if (styles.ID == 178 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntAxeWarrior;

								if (styles.ID == 167 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntHammerWarrior;
							}
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
					{
						Body.Strength = 180;
						Body.Quickness = 100;
						Body.SwitchWeapon(EActiveWeaponSlot.Standard);
						Body.VisibleActiveWeaponSlots = 16;
						Body.ParryChance = 15;
						Body.BlockChance = 60;
						Body.styleComponent.NextCombatStyle = slamWarrior;
					}
					if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
					{
						if (CanWalkWarrior == false)
						{
							new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
							CanWalkWarrior = true;
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
					{
						CanWalkWarrior = false;//reset flag so can slam again
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Savage
	public static int TauntSavageID = 372;
	public static int TauntSavageClassID = 32;
	public static Style tauntSavage = SkillBase.GetStyleByID(TauntSavageID, TauntSavageClassID);

	public static int BackSavageID = 373;
	public static int BackSavageClassID = 32;
	public static Style BackSavage = SkillBase.GetStyleByID(BackSavageID, BackSavageClassID);

	public void IsSavage()
	{
		if (Body.PackageID == "NosdodenGhostSavage")
		{
			Body.EvadeChance = 50;
			Body.ParryChance = 15;
			Body.SwitchWeapon(EActiveWeaponSlot.Standard);
			Body.VisibleActiveWeaponSlots = 16;
			if (Body.InCombat && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (Util.Chance(35))
						Body.CastSpell(Savage_dps_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

					GameLiving target = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if (angle >= 160 && angle <= 200)
					{
						Body.SwitchWeapon(EActiveWeaponSlot.Standard);
						Body.VisibleActiveWeaponSlots = 16;
						foreach (Style styles in Body.Styles)
						{
							if (styles != null)
							{
								if (styles.ID == 373 && styles.ClassID == 32)
								{
									Body.Quickness = 80;
									Body.Strength = 200;
									Body.SwitchWeapon(EActiveWeaponSlot.Standard);
									Body.VisibleActiveWeaponSlots = 16;
									Body.styleComponent.NextCombatStyle = BackSavage;
								}
								else if (styles.ID == 372 && styles.ClassID == 32)
								{
									Body.Quickness = 80;
									Body.Strength = 170;
									Body.SwitchWeapon(EActiveWeaponSlot.Standard);
									Body.VisibleActiveWeaponSlots = 16;
									Body.styleComponent.NextCombatBackupStyle = tauntSavage;
								}
							}
						}
						if(!Body.Styles.Contains(BackSavage) && !Body.Styles.Contains(tauntSavage))
                        {
							Body.Strength = 250;
							Body.Quickness = 50;
							Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
							Body.VisibleActiveWeaponSlots = 34;
							Body.styleComponent.NextCombatStyle = Taunt2h;
						}
					}
					else
                    {
						foreach (Style styles in Body.Styles)
                        {
							if (styles != null)
                            {
								if (styles.ID == 372 && styles.ClassID == 32)
								{
									Body.Strength = 170;
									Body.Quickness = 80;
									Body.SwitchWeapon(EActiveWeaponSlot.Standard);
									Body.VisibleActiveWeaponSlots = 16;
									Body.styleComponent.NextCombatStyle = tauntSavage;
								}
								else
								{
									if (styles.ID == 103 && styles.ClassID == 1)
									{
										Body.Strength = 250;
										Body.Quickness = 50;
										Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
										Body.VisibleActiveWeaponSlots = 34;
										Body.styleComponent.NextCombatStyle = Taunt2h;
									}
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Thane
	private protected bool CanWalkThane = false;
	public void IsThane()
	{
		if (Body.PackageID == "NosdodenGhostThane")
		{
			if (Body.IsAlive && !HasAggro)
			{
				Body.ParryChance = 15;
				Body.BlockChance = 60;
				Body.SwitchWeapon(EActiveWeaponSlot.Standard);
				Body.VisibleActiveWeaponSlots = 16;
			}
			if (!CheckProximityAggro())
				CanWalkThane = false;

			if (Body.IsAlive)
			{
				if (!Body.Styles.Contains(slamWarrior))
					Body.Styles.Add(slamWarrior);
				if (!Body.Styles.Contains(Taunt2h))
					Body.Styles.Add(Taunt2h);
				if (!Body.Styles.Contains(Back2h))
					Body.Styles.Add(Back2h);
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
					{
						if (Body.attackComponent.AttackState)
							Body.attackComponent.StopAttack();
						if (Body.IsMoving)
							Body.StopFollowing();
						Body.TurnTo(Body.TargetObject);
						Body.CastSpell(InstantThaneDD_casting, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
					}

					if(Util.Chance(15) && Body.IsWithinRadius(Body.TargetObject,Body.AttackRange))
						Body.CastSpell(InstantThaneDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (Util.Chance(15) && Body.IsWithinRadius(Body.TargetObject, Body.AttackRange))
						Body.CastSpell(InstantThaneDD_pbaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					
					GameLiving target = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if (angle >= 160 && angle <= 200)
					{
						Body.Strength = 220;
						Body.Quickness = 60;
						Body.ParryChance = 50;
						Body.BlockChance = 0;
						Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						Body.VisibleActiveWeaponSlots = 34;
						Body.styleComponent.NextCombatStyle = Back2h;
						Body.styleComponent.NextCombatBackupStyle = Taunt2h;
					}
					else
					{
						Body.Strength = 160;
						Body.Quickness = 100;
						Body.ParryChance = 15;
						Body.BlockChance = 50;
						Body.SwitchWeapon(EActiveWeaponSlot.Standard);
						Body.VisibleActiveWeaponSlots = 16;
						foreach (Style styles in Body.Styles)
						{
							if (styles != null)
							{
								if (styles.ID == 157 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntSwordWarrior;

								if (styles.ID == 178 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntAxeWarrior;

								if (styles.ID == 167 && styles.ClassID == 22)
									Body.styleComponent.NextCombatStyle = tauntHammerWarrior;
							}
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
					{
						Body.Strength = 160;
						Body.Quickness = 100;
						Body.SwitchWeapon(EActiveWeaponSlot.Standard);
						Body.VisibleActiveWeaponSlots = 16;
						Body.ParryChance = 15;
						Body.BlockChance = 50;
						Body.styleComponent.NextCombatStyle = slamWarrior;
					}
					if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
					{
						if (CanWalkThane == false)
						{
							new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
							CanWalkThane = true;
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
						CanWalkThane = false;//reset flag so can slam again
				}
			}
		}
	}
	#endregion
	#region Mob Class Skald
	public static int AfterParry2hID = 108;
	public static int AfterParry2hClassID = 1;
	public static Style AfterParry2h = SkillBase.GetStyleByID(AfterParry2hID, AfterParry2hClassID);

	public static int Taunt2hID = 103;
	public static int Taunt2hClassID = 1;
	public static Style Taunt2h = SkillBase.GetStyleByID(Taunt2hID, Taunt2hClassID);

	public static int Back2hID = 113;
	public static int Back2hClassID = 1;
	public static Style Back2h = SkillBase.GetStyleByID(Back2hID, Back2hClassID);
	public void IsSkald()
	{
		if (Body.PackageID == "NosdodenGhostSkald")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			Body.VisibleActiveWeaponSlots = 34;
			Body.ParryChance = 50;
			if(Body.IsAlive)
            {
				if (!Body.Styles.Contains(Taunt2h))
					Body.Styles.Add(Taunt2h);
				if (!Body.Styles.Contains(AfterParry2h))
					Body.Styles.Add(AfterParry2h);
            }
			if(!CheckProximityAggro())
            {
				lock (Body.effectListComponent.EffectsLock)
				{
					var effects = Body.effectListComponent.GetAllPulseEffects();
					for (int i = 0; i < effects.Count; i++)
					{
						EcsPulseEffect effect = effects[i];
						if (effect == null)
							continue;

						if (effect == null)
							continue;
						if (effect.SpellHandler.Spell.Pulse == 1)
						{
							EffectService.RequestCancelConcEffect(effect);//cancel here all pulse effect
						}
					}
				}
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (Util.Chance(30))
						Body.CastSpell(Skald_DA, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (Util.Chance(35) && Body.IsWithinRadius(Body.TargetObject, 700))
						Body.CastSpell(InstantSkaldDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (Util.Chance(35) && Body.IsWithinRadius(Body.TargetObject, 700))
						Body.CastSpell(InstantSkaldDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

					if (Util.Chance(100))
					{
						Body.Quickness = 80;
						Body.Strength = 220;
						Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
						Body.VisibleActiveWeaponSlots = 34;
						Body.styleComponent.NextCombatStyle = AfterParry2h;
						Body.styleComponent.NextCombatBackupStyle = Taunt2h;
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Hunter
	private protected bool Switch_to_Ranged = false;

	public static int TauntSpearHuntID = 217;
	public static int TauntSpearHuntClassID = 25;
	public static Style TauntSpearHunt = SkillBase.GetStyleByID(TauntSpearHuntID, TauntSpearHuntClassID);

	public static int BackSpearHuntID = 218;
	public static int BackSpearHuntClassID = 25;
	public static Style BackSpearHunt = SkillBase.GetStyleByID(BackSpearHuntID, BackSpearHuntClassID);
	public void IsHunter()
	{
		if (Body.PackageID == "NosdodenGhostHunter")
		{
			Body.EvadeChance = 40;
			if (Body.IsAlive)
			{
				if (!Body.Styles.Contains(Taunt2h))
					Body.Styles.Add(Taunt2h);
			}
			if (!CheckProximityAggro())
			{
				Body.SwitchWeapon(EActiveWeaponSlot.Distance);
				Body.VisibleActiveWeaponSlots = 51;
				CanCreateHunterPet = false;
				Switch_to_Ranged = false;
				foreach(GameNpc npc in Body.GetNPCsInRadius(5000))
                {
					if(npc != null)
                    {
						if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "GhostHunterPet" && npc.Brain is StandardMobBrain brain && !brain.HasAggro)
							npc.Die(npc);
                    }
                }
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					CreateHunterPet();
					if (!Body.IsWithinRadius(Body.TargetObject, 200))
					{
						if (Body.IsMoving)
							Body.StopFollowing();
						if (Switch_to_Ranged == false)
						{
							Body.SwitchWeapon(EActiveWeaponSlot.Distance);
							Body.VisibleActiveWeaponSlots = 51;
							Body.Strength = 220;
							Switch_to_Ranged = true;
						}
					}
					if (Body.IsWithinRadius(Body.TargetObject, 200))
					{
						Switch_to_Ranged = false;
						GameLiving target = Body.TargetObject as GameLiving;
						float angle = Body.TargetObject.GetAngle(Body);
						if (angle >= 160 && angle <= 200)
						{
							foreach (Style styles in Body.Styles)
							{
								if (styles != null)
								{
									if (styles.ID == 218 && styles.ClassID == 25)
									{
										Body.Quickness = 60;
										Body.Strength = 220;
										Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
										Body.VisibleActiveWeaponSlots = 34;
										Body.styleComponent.NextCombatStyle = BackSpearHunt;
									}
									else if (styles.ID == 217 && styles.ClassID == 25)
									{
										Body.Quickness = 60;
										Body.Strength = 170;
										Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
										Body.VisibleActiveWeaponSlots = 34;
										Body.styleComponent.NextCombatBackupStyle = TauntSpearHunt;
									}
								}
							}
							if (!Body.Styles.Contains(BackSpearHunt) && !Body.Styles.Contains(TauntSpearHunt))
							{
								Body.Quickness = 60;
								Body.Strength = 170;
								Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
								Body.VisibleActiveWeaponSlots = 34;
								Body.styleComponent.NextCombatStyle = Taunt2h;
							}
						}
						else
						{
							foreach (Style styles in Body.Styles)
							{
								if (styles != null)
								{
									if (styles.ID == 217 && styles.ClassID == 25)
									{
										Body.Quickness = 60;
										Body.Strength = 170;
										Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
										Body.VisibleActiveWeaponSlots = 34;
										Body.styleComponent.NextCombatStyle = TauntSpearHunt;
									}
								}
							}
							if (!Body.Styles.Contains(BackSpearHunt) && !Body.Styles.Contains(TauntSpearHunt))
							{
								Body.Quickness = 60;
								Body.Strength = 170;
								Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
								Body.VisibleActiveWeaponSlots = 34;
								Body.styleComponent.NextCombatStyle = Taunt2h;
							}
						}
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Shadowblade
	private protected bool CanWalkShadowblade = false;

	public static int AnyTimerSBID = 342;
	public static int AnyTimerSBClassID = 23;
	public static Style AnyTimerSB = SkillBase.GetStyleByID(AnyTimerSBID, AnyTimerSBClassID);

	public static int AnyTimerFollowUpSBID = 344;
	public static int AnyTimerFollowUpSBClassID = 23;
	public static Style AnyTimerFollowUpSB = SkillBase.GetStyleByID(AnyTimerFollowUpSBID, AnyTimerFollowUpSBClassID);
	public void IsShadowblade()
	{
		if (Body.PackageID == "NosdodenGhostShadowblade")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.Standard);
			Body.VisibleActiveWeaponSlots = 16;
			Body.EvadeChance = 60;
			if (Body.IsAlive)
			{
				if (!Body.Styles.Contains(AnyTimerSB))
					Body.Styles.Add(AnyTimerSB);
				if (!Body.Styles.Contains(AfterEvadeBerserker))
					Body.Styles.Add(AfterEvadeBerserker);
				if (!Body.Styles.Contains(EvadeFollowUpBerserker))
					Body.Styles.Add(EvadeFollowUpBerserker);
				if (!Body.Styles.Contains(BackBerserker))
					Body.Styles.Add(BackBerserker);
			}
			if (!CheckProximityAggro())
			{
				CanWalkShadowblade = false;
			}
			if (Body.InCombat && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					GameLiving target = Body.TargetObject as GameLiving;
					float angle = Body.TargetObject.GetAngle(Body);
					if (angle >= 160 && angle <= 200)
					{
						Body.Quickness = 100;
						Body.Strength = 180;
						Body.styleComponent.NextCombatStyle = BackBerserker;
					}
					else
					{
						Body.Quickness = 100;
						Body.Strength = 150;
					}
					if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
					{
						if (CanWalkShadowblade == false)
						{
							new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
							CanWalkShadowblade = true;
						}
					}
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
					{
						CanWalkShadowblade = false;//reset flag so can slam again
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Runemaster
	public void IsRunemaster()
	{
		if (Body.PackageID == "NosdodenGhostRunemaster")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			Body.VisibleActiveWeaponSlots = 34;
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Rune_Bolt))
					Body.Spells.Add(Rune_Bolt);
				if (!Body.Spells.Contains(Rune_DD))
					Body.Spells.Add(Rune_DD);
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving)
					{						
						foreach(Spell spells in Body.Spells)
                        {
							if(spells != null)
                            {
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject,spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject,spells.Range - 50,5000);

								Body.TurnTo(Body.TargetObject);
								if (Util.Chance(100))
								{
									if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Rune_Bolt) == 0)
										Body.CastSpell(Rune_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
									else
										Body.CastSpell(Rune_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
								}
							}
                        }
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Spiritmaster
	public void IsSpiritmaster()
	{
		if (Body.PackageID == "NosdodenGhostSpiritmaster")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			Body.VisibleActiveWeaponSlots = 34;
			if(!CheckProximityAggro())
            {
			}
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Spirit_DD))
					Body.Spells.Add(Spirit_DD);
				if (!Body.Spells.Contains(Spirit_Mezz))
					Body.Spells.Add(Spirit_Mezz);
			}
			if (HasAggro)
			{
				SummonSpiritChampion();
				foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is GhostSpiritChampionBrain brain && npc.PackageID == Convert.ToString(Body.ObjectID))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

								Body.TurnTo(Body.TargetObject);
								if (Util.Chance(100))
								{
									if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Spirit_Mezz) == 0)
										Body.CastSpell(Spirit_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
									else
										Body.CastSpell(Spirit_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Bonedancer
	public void IsBonedancer()
	{
		if (Body.PackageID == "NosdodenGhostBonedancer")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			Body.VisibleActiveWeaponSlots = 34;
			if (!CheckProximityAggro())
			{
			}
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Bone_DD))
					Body.Spells.Add(Bone_DD);
				if (!Body.Spells.Contains(Bone_DD2))
					Body.Spells.Add(Bone_DD2);
			}
			if (HasAggro)
			{
				SummonSkeletalCommander();
				foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is GhostSkeletalCommanderBrain brain && npc.PackageID == Convert.ToString(Body.ObjectID))
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

								Body.TurnTo(Body.TargetObject);
								if (Util.Chance(100))
								{
									if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Bone_DD) == 0)
										Body.CastSpell(Bone_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
									else
										Body.CastSpell(Bone_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Healer
	private protected GamePlayer randomhealertarget = null;
	private protected GamePlayer RandomHealerTarget
	{
		get { return randomhealertarget; }
		set { randomhealertarget = value; }
	}
	private protected List<GamePlayer> HealerEnemys_To_Mezz = new List<GamePlayer>();
	private protected void PickTargetToMezz()
    {
		if(HasAggro)
        {
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!HealerEnemys_To_Mezz.Contains(player) && (!player.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity) || !player.effectListComponent.ContainsEffectForEffectType(EEffect.Mez)))
							HealerEnemys_To_Mezz.Add(player);
					}
				}
			}
			if (HealerEnemys_To_Mezz.Count > 0)
			{
				if (Body.GetSkillDisabledDuration(Healer_Mezz) == 0)
				{
					GamePlayer Target = HealerEnemys_To_Mezz[Util.Random(0, HealerEnemys_To_Mezz.Count - 1)];//pick random target from list
					RandomHealerTarget = Target;
					if (RandomHealerTarget != null && RandomHealerTarget.IsAlive
						&& (!RandomHealerTarget.effectListComponent.ContainsEffectForEffectType(EEffect.Mez) || !RandomHealerTarget.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity)))
					{
						Body.TargetObject = RandomHealerTarget;
						Body.TurnTo(RandomHealerTarget);
						Body.CastSpell(Healer_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);						
					}
				}
			}
		}
    }
	public void IsHealer()
	{
		if (Body.PackageID == "NosdodenGhostHealer")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.Standard);
			Body.VisibleActiveWeaponSlots = 16;
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Healer_Heal))
					Body.Spells.Add(Healer_Heal);
				if (!Body.Spells.Contains(Healer_Mezz))
					Body.Spells.Add(Healer_Mezz);
				if (!Body.Spells.Contains(Healer_Amnesia))
					Body.Spells.Add(Healer_Amnesia);
			}
			if(!CheckProximityAggro())
            {
				RandomHealerTarget = null;
				if (HealerEnemys_To_Mezz.Count > 0)
					HealerEnemys_To_Mezz.Clear();

				foreach(GameNpc npc in Body.GetNPCsInRadius((ushort)Healer_Heal.Range))
                {
					if(npc != null)
                    {
						if(npc.IsAlive && npc.Faction == Body.Faction && npc.HealthPercent < 100)
                        {
							Body.TargetObject = npc;
							if(npc != Body)
								Body.TurnTo(npc);
							Body.CastSpell(Healer_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
						}
                    }							
                }
            }
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					GameLiving oldtarget = Body.TargetObject as GameLiving;
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

								if (Util.Chance(100))
								{
									if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Healer_Mezz) == 0)
										PickTargetToMezz();
									else
									{
										foreach (GameNpc npc in Body.GetNPCsInRadius((ushort)Healer_Heal.Range))
										{
											if (npc != null)
											{
												if (npc.IsAlive && npc.Faction == Body.Faction)
												{
													if (npc.HealthPercent < 100)
													{
														Body.TargetObject = npc;
														if(npc != Body)
															Body.TurnTo(npc);
														Body.CastSpell(Healer_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
													}
													if (npc.HealthPercent == 100)
													{
														if (oldtarget != null && oldtarget != Body && !HealerEnemys_To_Mezz.Contains(Body.TargetObject as GamePlayer))
														{
															Body.TargetObject = CalculateNextAttackTarget();
															Body.TurnTo(Body.TargetObject);
															Body.CastSpell(Healer_Amnesia, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
	#region Mob Class Shaman 
	public void IsShaman()
	{
		if (Body.PackageID == "NosdodenGhostShaman")
		{
			Body.SwitchWeapon(EActiveWeaponSlot.Standard);
			Body.VisibleActiveWeaponSlots = 16;
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(Shamy_Bolt))
					Body.Spells.Add(Shamy_Bolt);
				if (!Body.Spells.Contains(Shamy_DD))
					Body.Spells.Add(Shamy_DD);
				if (!Body.Spells.Contains(Shamy_AoeDot))
					Body.Spells.Add(Shamy_AoeDot);
				if (!Body.Spells.Contains(Shamy_InstaAoeDisease))
					Body.Spells.Add(Shamy_InstaAoeDisease);
			}
			if (HasAggro)
			{
				if (Body.TargetObject != null)
				{
					if (!Body.IsCasting && !Body.IsMoving)
					{
						foreach (Spell spells in Body.Spells)
						{
							if (spells != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, spells.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, spells.Range - 50, 5000);

								Body.TurnTo(Body.TargetObject);
								if (Util.Chance(100))
								{
									if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_Bolt) == 0)
										Body.CastSpell(Shamy_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else if(spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_AoeDot) == 0)
										Body.CastSpell(Shamy_AoeDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else if (spells.HasRecastDelay && Body.GetSkillDisabledDuration(Shamy_InstaAoeDisease) == 0)
										Body.CastSpell(Shamy_InstaAoeDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									else 
										Body.CastSpell(Shamy_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
	public override void Think()
	{
		if(Body.IsAlive)
        {
			IsBerserker();
			IsWarrior();
			IsSavage();
			IsThane();
			IsSkald();
			IsHunter();
			IsShadowblade();
			IsRunemaster();
			IsSpiritmaster();
			IsBonedancer();
			IsHealer();
			IsShaman();
        }
		base.Think();
	}
    public int WalkBack(EcsGameTimer timer)
	{
		if (Body.InCombat && HasAggro && Body.TargetObject != null)
		{
			if (Body.TargetObject is GameLiving)
			{
				GameLiving living = Body.TargetObject as GameLiving;
				float angle = living.GetAngle(Body);
				Point2D positionalPoint;
				positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (180 * (4096.0 / 360.0))), 65);
				//Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
				Body.X = positionalPoint.X;
				Body.Y = positionalPoint.Y;
				Body.Z = living.Z;
				Body.Heading = 1250;
			}
		}
		return 0;
	}
	#region Spells
	#region Spells Thane
	private Spell m_InstantThaneDD;
	public Spell InstantThaneDD
	{
		get
		{
			if (m_InstantThaneDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3510;
				spell.Icon = 3510;
				spell.Damage = 120;
				spell.DamageType = (int)EDamageType.Energy;
				spell.Name = "Toothgnasher's Ram";
				spell.Range = 1500;
				spell.SpellID = 11869;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_InstantThaneDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD);
			}
			return m_InstantThaneDD;
		}
	}
	private Spell m_InstantThaneDD_pbaoe;
	public Spell InstantThaneDD_pbaoe
	{
		get
		{
			if (m_InstantThaneDD_pbaoe == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3528;
				spell.Icon = 35280;
				spell.Damage = 120;
				spell.DamageType = (int)EDamageType.Energy;
				spell.Name = "Greater Thunder Roar";
				spell.Range = 0;
				spell.Radius = 350;
				spell.SpellID = 11870;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_InstantThaneDD_pbaoe = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD_pbaoe);
			}
			return m_InstantThaneDD_pbaoe;
		}
	}
	private Spell m_InstantThaneDD_casting;
	public Spell InstantThaneDD_casting
	{
		get
		{
			if (m_InstantThaneDD_casting == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3510;
				spell.Icon = 3510;
				spell.Damage = 300;
				spell.DamageType = (int)EDamageType.Energy;
				spell.Name = "Thor's Full Lightning";
				spell.Range = 1500;
				spell.SpellID = 11871;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_InstantThaneDD_casting = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantThaneDD_casting);
			}
			return m_InstantThaneDD_casting;
		}
	}
	#endregion
	#region Spells Skald
	private Spell m_InstantSkaldDD;
	public Spell InstantSkaldDD
	{
		get
		{
			if (m_InstantSkaldDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 15;
				spell.ClientEffect = 3628;
				spell.Icon = 3628;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Battle Roar";
				spell.Range = 700;
				spell.SpellID = 11872;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_InstantSkaldDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantSkaldDD);
			}
			return m_InstantSkaldDD;
		}
	}
	private Spell m_InstantSkaldDD2;
	public Spell InstantSkaldDD2
	{
		get
		{
			if (m_InstantSkaldDD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 15;
				spell.ClientEffect = 3624;
				spell.Icon = 3624;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Battle Roar";
				spell.Range = 700;
				spell.SpellID = 11873;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				m_InstantSkaldDD2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_InstantSkaldDD2);
			}
			return m_InstantSkaldDD2;
		}
	}
	private Spell m_Skald_DA;
	public Spell Skald_DA
	{
		get
		{
			if (m_Skald_DA == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 8;
				spell.Duration = 5;
				spell.Frequency = 50;
				spell.Pulse = 1;
				spell.ClientEffect = 3607;
				spell.Icon = 3607;
				spell.Damage = 10;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Chant of Blood";
				spell.Range = 700;
				spell.SpellID = 11875;
				spell.Target = "Self";
				spell.Type = ESpellType.DamageAdd.ToString();
				m_Skald_DA = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Skald_DA);
			}
			return m_Skald_DA;
		}
	}
	#endregion
	#region Spells Savage
	private Spell m_Savage_dps_Buff;
	private Spell Savage_dps_Buff
	{
		get
		{
			if (m_Savage_dps_Buff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 30;
				spell.Duration = 20;
				spell.ClientEffect = 10541;
				spell.Icon = 10541;
				spell.Name = "Savage Blows";
				spell.Message2 = "{0} takes on a feral aura.";
				spell.TooltipId = 10541;
				spell.Range = 0;
				spell.Value = 25;
				spell.SpellID = 11874;
				spell.Target = "Self";
				spell.Type = ESpellType.SavageDPSBuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Savage_dps_Buff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Savage_dps_Buff);
			}
			return m_Savage_dps_Buff;
		}
	}

	#endregion
	#region Hunter Pet summon
	private protected bool CanCreateHunterPet = false;
	public void CreateHunterPet()
    {
		if(CanCreateHunterPet==false && Body.PackageID == "NosdodenGhostHunter")
        {
			GameLiving ptarget = CalculateNextAttackTarget();
			GameNpc pet = new GameNpc();
			pet.Name = "Hunter's Avatar";
			pet.Model = 648;
			pet.Size = 60;
			pet.Level = 50;
			pet.Strength = 150;
			pet.Quickness = 80;
			pet.MaxSpeedBase = 225;
			pet.Health = 2500;
			pet.X = Body.X;
			pet.Y = Body.Y;
			pet.Z = Body.Z;
			pet.Heading = Body.Heading;
			pet.CurrentRegionID = Body.CurrentRegionID;
			pet.PackageID = "GhostHunterPet";
			pet.RespawnInterval = -1;
			StandardMobBrain sbrain = new StandardMobBrain();
			pet.SetOwnBrain(sbrain);
			sbrain.AggroRange = 500;
			sbrain.AggroLevel = 100;
			if (ptarget != null)
			{
				sbrain.AddToAggroList(ptarget, 10);
				pet.StartAttack(ptarget);
			}
			pet.AddToWorld();
			CanCreateHunterPet = true;
        }
    }
	#endregion
	#region Spells Runemaster
	private Spell m_Rune_DD;
	private Spell Rune_DD
	{
		get
		{
			if (m_Rune_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 2570;
				spell.Icon = 2570;
				spell.TooltipId = 2570;
				spell.Damage = 300;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Greater Rune of Shadow";
				spell.Range = 1500;
				spell.SpellID = 11877;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				m_Rune_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Rune_DD);
			}
			return m_Rune_DD;
		}
	}
	private Spell m_Rune_Bolt;
	private Spell Rune_Bolt
	{
		get
		{
			if (m_Rune_Bolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 2970;
				spell.Icon = 2970;
				spell.TooltipId = 2970;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Sigil of Undoing";
				spell.Range = 1800;
				spell.SpellID = 11878;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Bolt.ToString();
				spell.Uninterruptible = true;
				m_Rune_Bolt = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Rune_Bolt);
			}
			return m_Rune_Bolt;
		}
	}
	#endregion
	#region Spells Spiritmaster and Summon Pet
	private Spell m_Spirit_DD;
	private Spell Spirit_DD
	{
		get
		{
			if (m_Spirit_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 2610;
				spell.Icon = 2610;
				spell.TooltipId = 2610;
				spell.Damage = 320;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Extinguish Lifeforce";
				spell.Range = 1500;
				spell.SpellID = 11879;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				m_Spirit_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Spirit_DD);
			}
			return m_Spirit_DD;
		}
	}
	private Spell m_Spirit_Mezz;
	private Spell Spirit_Mezz
	{
		get
		{
			if (m_Spirit_Mezz == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 30;
				spell.ClientEffect = 2643;
				spell.Icon = 2643;
				spell.TooltipId = 2643;
				spell.Duration = 35;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
				spell.Name = "Umbral Shroud";
				spell.Range = 1500;
				spell.Radius = 450;
				spell.SpellID = 11880;
				spell.Target = "Enemy";
				spell.Type = "Mesmerize";
				spell.Uninterruptible = true;
				m_Spirit_Mezz = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Spirit_Mezz);
			}
			return m_Spirit_Mezz;
		}
	}	
	public void SummonSpiritChampion()
    {
		foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(Body.ObjectID) && npc.Brain is GhostSpiritChampionBrain)
				return;
		}
		if (Body.PackageID == "NosdodenGhostSpiritmaster")
		{
			GhostSpiritChampion pet = new GhostSpiritChampion();
			pet.X = Body.X;
			pet.Y = Body.Y-100;
			pet.Z = Body.Z;
			pet.Heading = Body.Heading;
			pet.CurrentRegionID = Body.CurrentRegionID;
			pet.Faction = Body.Faction;
			pet.PackageID = Convert.ToString(Body.ObjectID);
			pet.RespawnInterval = -1;
			GhostSpiritChampionBrain sbrain = new GhostSpiritChampionBrain();
			pet.SetOwnBrain(sbrain);
			sbrain.AggroRange = 500;
			sbrain.AggroLevel = 100;
			pet.AddToWorld();
			pet.Brain.Start();
		}
	}
	#endregion
	#region Spells Bonedancer and Summon Skeletal Commander
	private Spell m_Bone_DD2;
	private Spell Bone_DD2
	{
		get
		{
			if (m_Bone_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 10029;
				spell.Icon = 10029;
				spell.TooltipId = 10029;
				spell.Damage = 320;
				spell.Value = 35;
				spell.Duration = 30;
				spell.LifeDrainReturn = 90;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Description = "Target is damaged for 179 and also moves 35% slower for the spell duration.";
				spell.Name = "Crystallize Skeleton";
				spell.Range = 1500;
				spell.SpellID = 11882;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageSpeedDecreaseNoVariance.ToString();
				m_Bone_DD2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bone_DD2);
			}
			return m_Bone_DD2;
		}
	}
	private Spell m_Bone_DD;
	private Spell Bone_DD
	{
		get
		{
			if (m_Bone_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 4;
				spell.ClientEffect = 10081;
				spell.Icon = 10081;
				spell.TooltipId = 10081;
				spell.Damage = 250;
				spell.Value = -90;
				spell.LifeDrainReturn = 90;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "Pulverize Skeleton";
				spell.Range = 1500;
				spell.SpellID = 11881;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Lifedrain.ToString();
				m_Bone_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bone_DD);
			}
			return m_Bone_DD;
		}
	}
	public void SummonSkeletalCommander()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == Convert.ToString(Body.ObjectID) && npc.Brain is GhostSkeletalCommanderBrain)
				return;
		}
		if (Body.PackageID == "NosdodenGhostBonedancer")
		{
			GhostSkeletalCommander pet = new GhostSkeletalCommander();
			pet.X = Body.X;
			pet.Y = Body.Y - 100;
			pet.Z = Body.Z;
			pet.Heading = Body.Heading;
			pet.CurrentRegionID = Body.CurrentRegionID;
			pet.Faction = Body.Faction;
			pet.PackageID = Convert.ToString(Body.ObjectID);
			pet.RespawnInterval = -1;
			GhostSkeletalCommanderBrain sbrain = new GhostSkeletalCommanderBrain();
			pet.SetOwnBrain(sbrain);
			sbrain.AggroRange = 500;
			sbrain.AggroLevel = 100;
			pet.AddToWorld();
			pet.Brain.Start();
		}
	}
	#endregion
	#region Spells Healer
	private Spell m_Healer_Heal;
	private Spell Healer_Heal
	{
		get
		{
			if (m_Healer_Heal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3058;
				spell.Icon = 3058;
				spell.TooltipId = 3058;
				spell.Value = 400;
				spell.Name = "Heal";
				spell.Range = 2500;
				spell.SpellID = 11885;
				spell.Target = "Realm";
				spell.Type = "Heal";
				m_Healer_Heal = new Spell(spell, 70);
				spell.Uninterruptible = true;
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Heal);
			}
			return m_Healer_Heal;
		}
	}
	private Spell m_Healer_Mezz;
	private Spell Healer_Mezz
	{
		get
		{
			if (m_Healer_Mezz == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3371;
				spell.Icon = 3371;
				spell.TooltipId = 3371;
				spell.Duration = 65;
				spell.Name = "Tranquilize Area";
				spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
				spell.Range = 1500;
				spell.Radius = 400;
				spell.SpellID = 11886;
				spell.Target = "Enemy";
				spell.Type = "Mesmerize";
				spell.DamageType = (int)EDamageType.Body;
				m_Healer_Mezz = new Spell(spell, 70);
				spell.Uninterruptible = true;
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Mezz);
			}
			return m_Healer_Mezz;
		}
	}
	private Spell m_Healer_Amnesia;
	private Spell Healer_Amnesia
	{
		get
		{
			if (m_Healer_Amnesia == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;					
				spell.CastTime = 2;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3315;
				spell.Icon = 3315;
				spell.TooltipId = 3315;
				spell.Name = "Wake Oblivious";
				spell.AmnesiaChance = 100;
				spell.Message2 = "{0} forgets what they were doing!";
				spell.Range = 2300;
				spell.Radius = 350;
				spell.SpellID = 11887;
				spell.Target = "Enemy";
				spell.Type = "Amnesia";
				m_Healer_Amnesia = new Spell(spell, 44);
				spell.Uninterruptible = true;
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Healer_Amnesia);
			}
			return m_Healer_Amnesia;
		}
	}
	#endregion
	#region Spells Shaman
	private Spell m_Shamy_Bolt;
	private Spell Shamy_Bolt
	{
		get
		{
			if (m_Shamy_Bolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3470;
				spell.Icon = 3470;
				spell.TooltipId = 3470;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Fungal Spine";
				spell.Range = 1800;
				spell.SpellID = 11888;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Bolt.ToString();
				spell.Uninterruptible = true;
				m_Shamy_Bolt = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_Bolt);
			}
			return m_Shamy_Bolt;
		}
	}
	private Spell m_Shamy_DD;
	private Spell Shamy_DD
	{
		get
		{
			if (m_Shamy_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3494;
				spell.Icon = 3494;
				spell.TooltipId = 3494;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Fungal Mucus";
				spell.Range = 1500;
				spell.SpellID = 11890;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				m_Shamy_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_DD);
			}
			return m_Shamy_DD;
		}
	}
	private Spell m_Shamy_InstaAoeDisease;
	private Spell Shamy_InstaAoeDisease
	{
		get
		{
			if (m_Shamy_InstaAoeDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 30;
				spell.ClientEffect = 3425;
				spell.Icon = 3425;
				spell.TooltipId = 3425;
				spell.Duration = 120;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.Name = "Plague Spores";
				spell.Range = 1500;
				spell.Radius = 400;
				spell.SpellID = 11891;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Disease.ToString();
				spell.Uninterruptible = true;
				m_Shamy_InstaAoeDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_InstaAoeDisease);
			}
			return m_Shamy_InstaAoeDisease;
		}
	}
	private Spell m_Shamy_AoeDot;
	private Spell Shamy_AoeDot
	{
		get
		{
			if (m_Shamy_AoeDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 15;
				spell.ClientEffect = 3475;
				spell.Icon = 3475;
				spell.TooltipId = 3475;
				spell.Damage = 83;
				spell.Duration = 24;
				spell.Frequency = 40;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Description = "Inflicts 83 damage to the target every 4 sec for 24 seconds";
				spell.Message1 = "Your body is covered with painful sores!";
				spell.Message2 = "{0}'s skin erupts in open wounds!";
				spell.Message3 = "The destructive energy wounding you fades.";
				spell.Message4 = "The destructive energy around {0} fades.";
				spell.Name = "Fungal Spine";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11889;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				m_Shamy_AoeDot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Shamy_AoeDot);
			}
			return m_Shamy_AoeDot;
		}
	}
	#endregion
	#endregion
}
#endregion Nosoden Ghost add

#region Spiritmaster Pet
public class GhostSpiritChampionBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public GhostSpiritChampionBrain()
	{
		AggroLevel = 100;
		AggroRange = 450;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			foreach (GameNpc bone in Body.GetNPCsInRadius(5000))
			{
				if (bone != null)
				{
					if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain && bone.PackageID == "NosdodenGhostSpiritmaster" && Body.PackageID == Convert.ToString(bone.ObjectID))
						Body.Follow(bone, 100, 5000);
				}
			}
		}
		if (HasAggro)
		{
			foreach (GameNpc bone in Body.GetNPCsInRadius(5000))
			{
				if (bone != null)
				{
					if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain && bone.PackageID == "NosdodenGhostSpiritmaster" && Body.PackageID == Convert.ToString(bone.ObjectID))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (target != null)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(target, 100);
						}
					}
				}
			}
		}
		base.Think();
	}
}
#endregion Spiritmaster Pet

#region Skeletal Commander
public class GhostSkeletalCommanderBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public GhostSkeletalCommanderBrain()
	{
		AggroLevel = 100;
		AggroRange = 450;
	}
	public override void Think()
	{
		if(!CheckProximityAggro())
		{
			CanSummonBonemender=false;
			foreach(GameNpc bone in Body.GetNPCsInRadius(5000))
			{
				if(bone != null)
				{
					if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain && bone.PackageID == "NosdodenGhostBonedancer" && Body.PackageID == Convert.ToString(bone.ObjectID))
						Body.Follow(bone, 100, 5000);
				}
			}
		}
		if(HasAggro)
		{
			foreach (GameNpc bone in Body.GetNPCsInRadius(5000))
			{
				if (bone != null)
				{
					if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain && bone.PackageID == "NosdodenGhostBonedancer" && Body.PackageID == Convert.ToString(bone.ObjectID))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (target != null)
						{
							if (!brain.HasAggro)
								brain.AddToAggroList(target, 100);
						}
					}
				}
			}
		}
		if (Body.IsAlive)
			SummonBonemender();
		base.Think();
	}
	private protected bool CanSummonBonemender = false;
	private protected void SummonBonemender()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
		{
			if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is SkeletalCommanderHealerBrain && npc.PackageID == Body.PackageID)
				return;
		}
		SkeletalCommanderHealer pet = new SkeletalCommanderHealer();
		pet.X = Body.X;
		pet.Y = Body.Y - 50;
		pet.Z = Body.Z;
		pet.PackageID = Body.PackageID;
		pet.Heading = Body.Heading;
		pet.Faction = Body.Faction;
		pet.CurrentRegionID = Body.CurrentRegionID;
		pet.Faction = Body.Faction;
		pet.AddToWorld();
	}
}
#endregion Skeletal Commander

#region Bonemender
public class SkeletalCommanderHealerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public SkeletalCommanderHealerBrain()
	{
		AggroLevel = 0;
		AggroRange = 450;
	}
	public override void Think()
	{
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(Pet_Heal))
				Body.Spells.Add(Pet_Heal);
			foreach (GameNpc commander in Body.GetNPCsInRadius(5000))
            {
				foreach (GameNpc bone in Body.GetNPCsInRadius(5000))
				{
					if (commander != null && bone != null)
					{
						if (commander.IsAlive && commander.Brain is GhostSkeletalCommanderBrain brain && commander.PackageID == Body.PackageID)
						{
							if (bone.IsAlive && bone.Brain is NosdodenGhostAddBrain brain2 && bone.PackageID== "NosdodenGhostBonedancer" && bone.ObjectID == Convert.ToInt16(Body.PackageID))
							{
								if (!Body.IsCasting && !Body.IsMoving)
								{
									foreach (Spell spells in Body.Spells)
									{
										if (spells != null)
										{
											if (Body.IsMoving && Body.IsCasting)
												Body.StopFollowing();
											else
												Body.Follow(commander, 100, 5000);

											if (Util.Chance(100))
											{
												if (commander.HealthPercent < 100)
												{
													if (Body.TargetObject != commander)
														Body.TargetObject = commander;
													Body.TurnTo(commander);
													Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
												}
												else if (Body.HealthPercent < 100)
												{
													if (Body.TargetObject != Body)
														Body.TargetObject = Body;
													Body.TurnTo(Body);
													Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
												}
												else if (bone.HealthPercent < 100)
												{
													if (Body.TargetObject != bone)
														Body.TargetObject = bone;
													Body.TurnTo(bone);
													Body.CastSpell(Pet_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
												}
											}
										}
									}
								}
							}
						}
					}
				}
            }
        }
		base.Think();
	}
	private Spell m_Pet_Heal;
	private Spell Pet_Heal
	{
		get
		{
			if (m_Pet_Heal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3058;
				spell.Icon = 3058;
				spell.TooltipId = 3058;
				spell.Value = 250;
				spell.Name = "Heal";
				spell.Range = 1500;
				spell.SpellID = 11883;
				spell.Target = "Realm";
				spell.Type = "Heal";
				m_Pet_Heal = new Spell(spell, 70);
				spell.Uninterruptible = true;
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Pet_Heal);
			}
			return m_Pet_Heal;
		}
	}
}
#endregion Bonemender

#region Nosdoden adds
public class NosdodenSummonedAddsBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public NosdodenSummonedAddsBrain()
	{
		AggroLevel = 100;
		AggroRange = 1500;
	}
	public override void Think()
	{
		if (Body.IsAlive)
		{				
		}
		base.Think();
	}
}
#endregion Nosdoden adds