﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
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
							truc.Out.SendMessage(Name + " is immune to damage form this distance!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
			foreach (GameNPC npc in GetNPCsInRadius(8000))
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
			foreach (GameNPC npc in GetNPCsInRadius(8000))
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
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
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
					npc.Flags = eFlags.GHOST;
					npc.Heading = player.Heading;
					npc.CurrentRegion = CurrentRegion;
					npc.AddToWorld();
					BroadcastMessage(String.Format("Perhaps your pathetic gods will grant you another life, {0}. In the meantime, Hibernia shall defeat Midgard, and your spirit shall help!",player.Name));
				}
			}
			base.EnemyKilled(enemy);
        }
    }
}
namespace DOL.AI.Brain
{
	public class OonaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OonaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (Body.IsAlive)
			{
				if (!Body.Spells.Contains(OonaDD))
					Body.Spells.Add(OonaDD);
				if (!Body.Spells.Contains(OonaBolt))
					Body.Spells.Add(OonaBolt);
			}
			if (!CheckProximityAggro())
			{
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.TargetObject != null && HasAggro)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is OonaUndeadSoldierBrain brain)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
					}
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is OonaUndeadAddBrain brain)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
					}
				}
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
								if (Body.GetSkillDisabledDuration(OonaBolt) == 0)
									Body.CastSpell(OonaBolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								else
									Body.CastSpell(OonaDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
						}
					}
				}
			}
			base.Think();
		}

		#region Spells
		private Spell m_OonaDD;
		private Spell OonaDD
		{
			get
			{
				if (m_OonaDD == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4111;
					spell.Icon = 4111;
					spell.Damage = 330;
					spell.DamageType = (int)EDamageType.Heat;
					spell.Name = "Aurora Blast";
					spell.Range = 1650;
					spell.SpellID = 12004;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();					
					m_OonaDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OonaDD);
				}
				return m_OonaDD;
			}
		}
		
		private Spell m_OonaBolt;
		private Spell OonaBolt
		{
			get
			{
				if (m_OonaBolt == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(15, 20);
					spell.ClientEffect = 4559;
					spell.Icon = 4559;
					spell.Damage = 200;
					spell.DamageType = (int)EDamageType.Cold;
					spell.Name = "Bolt of Uncreation";
					spell.Range = 1800;
					spell.SpellID = 12005;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = ESpellType.Bolt.ToString();
					m_OonaBolt = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OonaBolt);
				}
				return m_OonaBolt;
			}
		}
		#endregion
	}
}
#region Oona's Undead Soldiers
namespace DOL.GS
{
	public class OonaUndeadSoldier : GameNPC
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
}
namespace DOL.AI.Brain
{
	public class OonaUndeadSoldierBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OonaUndeadSoldierBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion

#region Oona's Undead adds
namespace DOL.GS
{
	public class OonaUndeadAdd: GameNPC
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
}
namespace DOL.AI.Brain
{
	public class OonaUndeadAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public OonaUndeadAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion