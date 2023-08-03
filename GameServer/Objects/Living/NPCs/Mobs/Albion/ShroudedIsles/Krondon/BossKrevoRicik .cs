﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class BossKrevoRicik : GameEpicBoss
	{
		public BossKrevoRicik() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Krevo Ricik Initializing...");
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
			Model = 919;
			Level = (byte)(UtilCollection.Random(72, 75));
			Name = "Krevo Ricik";
			Size = 120;

			Strength = 280;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			KrevoRicikBrain sbrain = new KrevoRicikBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNpc add in GetNPCsInRadius(4000))
			{
				if (add == null) continue;
				if (add.IsAlive && add.Brain is KrevoAddBrain)
					add.Die(this);
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class KrevoRicikBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KrevoRicikBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad != null && ad.Attacker is GamePlayer && ad.Damage > 0)
            {
				if(UtilCollection.Chance(10))
					ad.Attacker.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z + 400, Body.Heading);
				if (UtilCollection.Chance(15))
					SpawnGhost();
            }
            base.OnAttackedByEnemy(ad);
        }
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				if (!RemoveAdds)
				{
					foreach (GameNpc add in Body.GetNPCsInRadius(4000))
					{
						if (add == null) continue;
						if (add.IsAlive && add.Brain is KrevoAddBrain)
							add.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
				RemoveAdds = false;
			base.Think();
		}
		public void SpawnGhost()
        {
			foreach (GameNpc add in Body.GetNPCsInRadius(4000))
			{
				if (add.Brain is KrevoAddBrain)
				{
					return;
				}
			}
			KrevolAdd npc = new KrevolAdd();
			npc.X = Body.X + UtilCollection.Random(-100, 100);
			npc.Y = Body.Y + UtilCollection.Random(-100, 100);
			npc.Z = Body.Z;
			npc.Heading = Body.Heading;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.RespawnInterval = -1;
			npc.AddToWorld();
		}
	}
}
namespace DOL.GS
{
	public class KrevolAdd : GameEpicNpc
	{
		public KrevolAdd() : base() { }

		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 30;// dmg reduction for melee dmg
				case EDamageType.Crush: return 30;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 30;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 120;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 400;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 4000; }
		}
		public override void Die(GameObject killer)
		{
			base.Die(killer);
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 120; }
		public override bool AddToWorld()
		{
			Model = 902;
			Level = (byte)(UtilCollection.Random(62, 64));
			Name = "forgoten ghost";
			Size = (byte)(UtilCollection.Random(50, 70));
			MaxSpeedBase = 250;
			RespawnInterval = ServerProperties.ServerProperties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			KrevoAddBrain sbrain = new KrevoAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Explode),30000); //30 seconds until this will explode and deal heavy 
			}
			return success;
		}
		private int Explode(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				SetGroundTarget(X, Y, Z);
				CastSpell(KrevoAddBomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(KillBomb), 500);
			}
			return 0;
		}
		private int KillBomb(ECSGameTimer timer)
		{
			if (IsAlive)
				Die(this);
			return 0;
		}
		private Spell m_KrevoAddBomb;
		private Spell KrevoAddBomb
		{
			get
			{
				if (m_KrevoAddBomb == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 6159;
					spell.Icon = 6159;
					spell.TooltipId = 6159;
					spell.Damage = 800;
					spell.Name = "Dark Explosion";
					spell.Range = 1500;
					spell.Radius = 700;
					spell.SpellID = 11890;
					spell.Target = ESpellTarget.Area.ToString();
					spell.Type = ESpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)EDamageType.Matter;
					m_KrevoAddBomb = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KrevoAddBomb);
				}
				return m_KrevoAddBomb;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class KrevoAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public KrevoAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}