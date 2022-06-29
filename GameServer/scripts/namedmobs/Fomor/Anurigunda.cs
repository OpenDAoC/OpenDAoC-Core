using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;
using System.Collections.Generic;

namespace DOL.GS
{
	public class Anurigunda : GameEpicBoss
	{
		public Anurigunda() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Anurigunda Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
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
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is AnurigundaAddsBrain)
					{
						npc.RemoveFromWorld();
					}
				}
			}
			base.Die(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157942);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Level = Convert.ToByte(npcTemplate.Level);
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(82);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));

			AnurigundaBrain sbrain = new AnurigundaBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class AnurigundaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AnurigundaBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		private bool RemoveAdds = false;
		public void BlockEntrance()
        {
			Point3D entrance = new Point3D(31234, 33215, 15842);
			foreach(GamePlayer player in Body.GetPlayersInRadius(Body.CurrentRegionID))
            {
				if (player == null) continue;
				if(player.IsAlive && player.Client.Account.PrivLevel == (uint)ePrivLevel.Player)
                {
					if(player.IsWithinRadius(entrance,400))
                    {
						player.MoveTo(180, 30652, 33089, 16124, 3169);
                    }
                }
            }
        }
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
				Adds1 = false;
				Adds2 = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is AnurigundaAddsBrain)
							{
								npc.RemoveFromWorld();
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if(Body.IsAlive)
            {
				BlockEntrance();
            }
			if (Body.IsAlive && HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (IsPulled == false)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "AnurigundaBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain);
							}
						}
					}
					IsPulled = true;
				}
				if(Body.TargetObject != null)
                {
					Body.CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				SpawnFomorians();
			}
			base.Think();
		}
		public static bool Adds1 = false;
		public static bool Adds2 = false;
		public void SpawnFomorians()
        {
			if (Body.HealthPercent <= 40 && Adds1 == false)
			{
				for (int i = 0; i < 4; i++)
				{
					AnurigundaAdds Add1 = new AnurigundaAdds();
					Add1.X = Body.X + Util.Random(-100, 100);
					Add1.Y = Body.Y + Util.Random(-100, 100);
					Add1.Z = Body.Z;
					Add1.CurrentRegion = Body.CurrentRegion;
					Add1.Heading = Body.Heading;
					Add1.RespawnInterval = -1;
					Add1.AddToWorld();
				}
				Adds1 = true;
			}
			if (Body.HealthPercent <= 20 && Adds2 == false)
			{
				for (int i = 0; i < 5; i++)
				{
					AnurigundaAdds Add1 = new AnurigundaAdds();
					Add1.X = Body.X + Util.Random(-100, 100);
					Add1.Y = Body.Y + Util.Random(-100, 100);
					Add1.Z = Body.Z;
					Add1.CurrentRegion = Body.CurrentRegion;
					Add1.Heading = Body.Heading;
					Add1.RespawnInterval = -1;
					Add1.AddToWorld();
				}
				Adds2 = true;
			}
		}
		private Spell m_FireGroundDD;
		private Spell FireGroundDD
		{
			get
			{
				if (m_FireGroundDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(10,15);
					spell.ClientEffect = 77;
					spell.Icon = 77;
					spell.TooltipId = 77;
					spell.Damage = 45;
					spell.Duration = 60;
					spell.Frequency = 20;
					spell.Name = "Touch of Flames";
					spell.Description = "Inflicts 45 damage to the target every 2 sec for 60 seconds.";
					spell.Message1 = "You are covered in lava!";
					spell.Message2 = "{0} is covered in lava!";
					spell.Message3 = "The lava hardens and falls away.";
					spell.Message4 = "The lava falls from {0}'s skin.";
					spell.Range = 0;
					spell.Radius = 350;
					spell.SpellID = 11839;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_FireGroundDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireGroundDD);
				}
				return m_FireGroundDD;
			}
		}
	}
}
////////////////////////////////////////////////////////////////adds/////////////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class AnurigundaAddsBrain : StandardMobBrain
	{
		public AnurigundaAddsBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 800;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
			}
			base.Think();
		}
	}
}
namespace DOL.GS
{
	public class AnurigundaAdds : GameNPC
	{
		public override int MaxHealth
		{
			get { return 5000; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 35;// dmg reduction for melee dmg
				case eDamageType.Crush: return 35;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 35;// dmg reduction for melee dmg
				default: return 25;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 70;
		}
		List<int> Id_npctemplates = new List<int>()
		{
			60160948,60160946,60160979,60161009
		};

        public override bool AddToWorld()
		{
			int idtemplate = Id_npctemplates[Util.Random(0, Id_npctemplates.Count - 1)];
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(idtemplate);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = -1;

			Faction = FactionMgr.GetFactionByID(82);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(82));
			AnurigundaAddsBrain adds = new AnurigundaAddsBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
	}
}