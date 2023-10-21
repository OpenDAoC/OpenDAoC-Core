using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;
using Core.GS;

namespace Core.GS
{
	public class Loken : GameEpicNPC
	{
		public Loken() : base() { }
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
			foreach(GameNpc npc in GetNPCsInRadius(8000))
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


#region Loken wolfs
namespace Core.GS
{
	public class LokenWolf : GameNpc
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
#endregion