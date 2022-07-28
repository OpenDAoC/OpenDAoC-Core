using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class DraargusMighty : GameEpicBoss
	{
		public DraargusMighty() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Dra'argus the Mighty Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20; // dmg reduction for melee dmg
				case eDamageType.Crush: return 20; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
				default: return 30; // dmg reduction for rest resists
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override void StartAttack(GameObject target)
		{
			if (DraugynSphere.SphereCount > 0)
				return;
			else
				base.StartAttack(target);
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
			if (DraugynSphere.SphereCount > 0 && IsAlive && keyName == GS.Abilities.DamageImmunity)
				return true;
			return base.HasAbility(keyName);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160055);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(9);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(9));
			CreateSphere();

			DraargusMightyBrain sbrain = new DraargusMightyBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public void CreateSphere()
        {
			if (DraugynSphere.SphereCount == 0)
			{
				DraugynSphere Add = new DraugynSphere();
				Add.X = 26766;
				Add.Y = 37124;
				Add.Z = 9027;
				Add.CurrentRegion = CurrentRegion;
				Add.Heading = 966;
				Add.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class DraargusMightyBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DraargusMightyBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.DamageReturn))
				{
					Body.CastSpell(FireDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.Think();
		}
		private Spell m_FireDS;
		private Spell FireDS
		{
			get
			{
				if (m_FireDS == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.ClientEffect = 57;
					spell.Icon = 57;
					spell.Damage = 120;
					spell.Duration = 60;
					spell.Name = "Dra'argus Shield";
					spell.TooltipId = 57;
					spell.SpellID = 11800;
					spell.Target = "Self";
					spell.Type = "DamageShield";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_FireDS = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireDS);
				}
				return m_FireDS;
			}
		}
	}
}
////////////////////////////////////////////////////////////////////////////////Sphere////////////////////////////////////////
namespace DOL.GS
{
	public class DraugynSphere : GameEpicNPC
	{
		public DraugynSphere() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Drau'gyn Sphere Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 50;// dmg reduction for rest resists
			}
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.15;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public static int SphereCount = 0;
		public static bool IsSphereDead = false;
        public override void Die(GameObject killer)
        {
			--SphereCount;
			IsSphereDead = true;
            base.Die(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160133);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			IsSphereDead = false;

			Faction = FactionMgr.GetFactionByID(9);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(9));
			MaxSpeedBase = 0;
			++SphereCount;
			RespawnInterval = -1;

			DraugynSphereBrain sbrain = new DraugynSphereBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if(success)
            {
				 new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 55, 0, false, 0x01);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1500);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1500);
			return 0;
		}
		public override void StartAttack(GameObject target)
        {
        }
    }
}
namespace DOL.AI.Brain
{
	public class DraugynSphereBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DraugynSphereBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro && Body.IsAlive)
			{
				Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
				Body.CastSpell(Sphere_pbaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

				foreach(GamePlayer player in Body.GetPlayersInRadius(300))
                {
					if(player != null)
                    {
						if(player.IsAlive && AggroTable.ContainsKey(player) && player.Client.Account.PrivLevel == 1)
                        {
							if(!player.IsWithinRadius(Body,200))
                            {
								player.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z, Body.Heading);
                            }
                        }
                    }
                }
			}
			base.Think();
		}
		private Spell m_Sphere_pbaoe;
		private Spell Sphere_pbaoe
		{
			get
			{
				if (m_Sphere_pbaoe == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 4;
					spell.ClientEffect = 368;
					spell.Icon = 368;
					spell.TooltipId = 368;
					spell.Damage = 300;
					spell.Name = "Sphere Explosion";
					spell.Range = 500;
					spell.Radius = 500;
					spell.SpellID = 11799;
					spell.Target = "Area";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Sphere_pbaoe = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Sphere_pbaoe);
				}
				return m_Sphere_pbaoe;
			}
		}
	}
}


