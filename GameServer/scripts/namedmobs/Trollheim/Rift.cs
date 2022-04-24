using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;

namespace DOL.GS
{
	public class Rift : GameEpicBoss
	{
		public Rift() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Rift Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 70;// dmg reduction for melee dmg
				case eDamageType.Crush: return 70;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 70;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
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
			return 600;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.40;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is MorkenhetBrain)
					{
						npc.RemoveFromWorld();
					}
				}
			}
			base.Die(killer);
        }
        public override bool AddToWorld()
		{
			Name = "Trollkarl Bonchar";
			Model = 874;
			Level = 65;
			Size = 81;
			MaxSpeedBase = 250;
			Strength = 550;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 100;
			Piety = 150;
			Intelligence = 150;
			Empathy = 300;
			MaxDistance = 3500;
			TetherRange = 3500;
			MeleeDamageType = eDamageType.Crush;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			RiftBrain.IsValkyn = false;

			RiftBrain sbrain = new RiftBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class RiftBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RiftBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public static bool IsValkyn = false;
		public static bool IsRift = false;
		public void ChangeAppearance()
        {
			if (!HasAggro && Body.IsAlive && IsValkyn == false)
			{
				Body.Name = "Trollkarl Bonchar";
				Body.Model = 874;
				Body.Level = 65;
				Body.Size = 81;
				Body.MaxSpeedBase = 250;
				Body.Strength = 550;
				Body.Dexterity = 150;
				Body.Constitution = 100;
				Body.Quickness = 100;
				Body.Piety = 150;
				Body.Intelligence = 150;
				Body.Empathy = 300;
				Body.MeleeDamageType = eDamageType.Crush;
				Body.Flags = 0;
				IsValkyn = true;
			}
			if(HasAggro && Body.IsAlive && IsRift==false)
            {
				Body.Name = "The Rift";
				Body.Model = 2049;
				Body.Level = 70;
				Body.Size = 80;
				Body.MaxSpeedBase = 280;
				Body.Strength = 650;
				Body.Dexterity = 150;
				Body.Constitution = 100;
				Body.Quickness = 100;
				Body.Piety = 150;
				Body.Intelligence = 150;
				Body.Empathy = 300;
				Body.MeleeDamageType = eDamageType.Energy;
				Body.Flags = GameNPC.eFlags.DONTSHOWNAME;
				IsRift = true;
            }
		}
		public override void Think()
		{
			if(Body.IsAlive)
				ChangeAppearance();

			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
				IsRift = false;
				SpawnMoreAdds = false;
				foreach(GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
					if (npc != null)
                    {
						if(npc.IsAlive && npc.Brain is MorkenhetBrain)
                        {
							npc.RemoveFromWorld();
                        }
                    }
                }
			}
			if (Body.IsAlive && HasAggro)
			{
				IsValkyn = false;								
				if(IsRift)
                {
					Body.CastSpell(RiftDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if (SpawnMoreAdds == false)
					{
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdds), Util.Random(15000, 20000));//15-20s spawn add
						SpawnMoreAdds = true;
					}
				}
			}
			base.Think();
		}
		public static bool SpawnMoreAdds = false;
		public int SpawnAdds(ECSGameTimer timer)
        {
			if (Body.IsAlive && HasAggro && IsRift)
			{
				Morkenhet Add1 = new Morkenhet();
				Add1.X = Body.X + Util.Random(-100, 100);
				Add1.Y = Body.Y + Util.Random(-100, 100);
				Add1.Z = Body.Z;
				Add1.CurrentRegion = Body.CurrentRegion;
				Add1.Heading = Body.Heading;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
			}
			SpawnMoreAdds = false;
			return 0;
		}
		private Spell m_RiftDD;
		private Spell RiftDD
		{
			get
			{
				if (m_RiftDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(6, 10);
					spell.ClientEffect = 3510;
					spell.Icon = 3510;
					spell.TooltipId = 3510;
					spell.Name = "Rift Strike";
					spell.Damage = 300;
					spell.Range = 1500;
					spell.SpellID = 11852;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Energy;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_RiftDD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RiftDD);
				}
				return m_RiftDD;
			}
		}
	}
}
//////////////////////////////////////////////////////////////////////////////////////////Rift Adds/////////////////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class MorkenhetBrain : StandardMobBrain
	{
		public MorkenhetBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
namespace DOL.GS
{
	public class Morkenhet : GameNPC
	{
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 35; // dmg reduction for melee dmg
				case eDamageType.Crush: return 35; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
				default: return 35; // dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 600;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
        public override short Strength { get => base.Strength; set => base.Strength = 350; }
        public override int MaxHealth
		{
			get { return 5000; }
		}
		public override bool AddToWorld()
		{
			Model = 929;
			Name = "morkenhet";
			Strength = 550;
			Dexterity = 200;
			Quickness = 100;
			Constitution = 100;
			RespawnInterval = -1;
			MaxSpeedBase = 225;

			Size = (byte)Util.Random(50, 60);
			Level = (byte)Util.Random(58, 62);
			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			MorkenhetBrain add = new MorkenhetBrain();
			SetOwnBrain(add);
			base.AddToWorld();
			return true;
		}
	}
}