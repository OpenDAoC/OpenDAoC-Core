using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Loken : GameEpicNPC
	{
		public Loken() : base() { }
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
				if (!source.IsWithinRadius(spawn, TetherRange))//dont take any dmg 
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
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
		public override double AttackDamage(InventoryItem weapon)
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
			get { return 10000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is LokenBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163372);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			SpawnWolfs();

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			LokenBrain sbrain = new LokenBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach(GameNPC npc in GetNPCsInRadius(8000))
            {
				if (npc != null && npc.IsAlive && npc.Brain is LokenWolfBrain)
					npc.Die(this);
            }
            base.Die(killer);
        }
        private void SpawnWolfs()
		{
			Point3D spawn = new Point3D(636780, 762427, 4597);
			for (int i = 0; i < 2; i++)
			{
				LokenWolf npc = new LokenWolf();
				npc.X = spawn.X + Util.Random(-100, 100);
				npc.Y = spawn.Y + Util.Random(-100, 100);
				npc.Z = spawn.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class LokenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LokenBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		private bool SpawnWolf = false;
		public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(LokenDD))
					Body.Spells.Add(LokenDD);
				if (!Body.Spells.Contains(LokenBolt))
					Body.Spells.Add(LokenBolt);
			}
			if(!HasAggressionTable())
            {
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163372);
				Body.MaxSpeedBase = npcTemplate.MaxSpeed;
				if (LokenWolf.WolfsCount < 2 && !SpawnWolf)
                {
					SpawnWolfs();
					SpawnWolf = true;
                }
            }
			if(HasAggro && Body.TargetObject != null)
            {
				SpawnWolf = false;
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is LokenWolfBrain brian)
					{
						if (!brian.HasAggro && brian != null && target != null && target.IsAlive)
							brian.AddToAggroList(target, 10);
					}
				}
				if(Util.Chance(100) && !Body.IsCasting)
					Body.CastSpell(LokenDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			if (Body.IsOutOfTetherRange && Body.TargetObject != null)
			{
				Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
				GameLiving target = Body.TargetObject as GameLiving;
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163372);
				if (target != null)
				{
					if (!target.IsWithinRadius(spawn, Body.TetherRange))
					{
						Body.MaxSpeedBase = 0;
					}
					else
						Body.MaxSpeedBase = npcTemplate.MaxSpeed;
				}
			}
			base.Think();
		}
		private void SpawnWolfs()
		{
			Point3D spawn = new Point3D(636780, 762427, 4597);
			for (int i = 0; i < 2; i++)
			{
				if (LokenWolf.WolfsCount < 2)
				{
					LokenWolf npc = new LokenWolf();
					npc.X = spawn.X + Util.Random(-100, 100);
					npc.Y = spawn.Y + Util.Random(-100, 100);
					npc.Z = spawn.Z;
					npc.Heading = Body.Heading;
					npc.CurrentRegion = Body.CurrentRegion;
					npc.AddToWorld();
				}
			}
		}
		#region Spells
		private Spell m_LokenDD;
		private Spell LokenDD
		{
			get
			{
				if (m_LokenDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 280;
					spell.Duration = 30;
					spell.Value = 20;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Description = "Damages the target and lowers their resistance to Heat by 20%";
					spell.Name = "Searing Blast";
					spell.Range = 1500;
					spell.SpellID = 12001;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageWithDebuff.ToString();
					m_LokenDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenDD);
				}
				return m_LokenDD;
			}
		}
		private Spell m_LokenDD2;
		private Spell LokenDD2
		{
			get
			{
				if (m_LokenDD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(5,10);
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.Damage = 280;
					spell.Duration = 30;
					spell.Value = 20;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Description = "Damages the target and lowers their resistance to Heat by 20%";
					spell.Name = "Searing Blast";
					spell.Range = 1500;
					spell.SpellID = 12002;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = eSpellType.DirectDamageWithDebuff.ToString();
					m_LokenDD2 = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenDD2);
				}
				return m_LokenDD2;
			}
		}
		private Spell m_LokenBolt;
		private Spell LokenBolt
		{
			get
			{
				if (m_LokenBolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = Util.Random(15,20);
					spell.ClientEffect = 378;
					spell.Icon = 378;
					spell.Damage = 200;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Flame Spear";
					spell.Range = 1800;
					spell.SpellID = 12003;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					m_LokenBolt = new Spell(spell, 60);
					spell.Uninterruptible = true;
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenBolt);
				}
				return m_LokenBolt;
			}
		}
		#endregion
	}
}

#region Loken wolfs
namespace DOL.GS
{
	public class LokenWolf : GameNPC
	{
		public LokenWolf() : base() { }
		#region Stats
		public override short Constitution { get => base.Constitution; set => base.Constitution = 200; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 150; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 100; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Loken's companion";
			Level = 50;
			Model = 459;
			Size = 45;
			LokenWolfBrain sbrain = new LokenWolfBrain();
			SetOwnBrain(sbrain);
			++WolfsCount;
			MaxSpeedBase = 225;
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public static int WolfsCount = 0;
        public override void Die(GameObject killer)
        {
			--WolfsCount;
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class LokenWolfBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LokenWolfBrain() : base()
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