﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class EpicRotoddjur : GameEpicNpc
	{
		public EpicRotoddjur() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Rotoddjur Initializing...");
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is RotoddjurAddBrain)
					{
						npc.RemoveFromWorld();
					}
				}
			}
			base.Die(killer);
        }
        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165428);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			RotoddjurBrain sbrain = new RotoddjurBrain();
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
	public class RotoddjurBrain : StandardMobBrain
	{
		public RotoddjurBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 500;
		}
		public static bool IsPulled = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if(!CheckProximityAggro())
            {
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
				if (!RemoveAdds)
				{
					foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.Brain is RotoddjurAddBrain)
							{
								npc.RemoveFromWorld();
							}
						}
					}
					RemoveAdds = true;
				}
			}
			if(HasAggro && Body.TargetObject != null)
            {
				RemoveAdds = false;
				GameLiving target = Body.TargetObject as GameLiving;
				if (UtilCollection.Chance(25) && target != null)
				{
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
					{
						Body.CastSpell(RotodddjurDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
				if(IsPulled==false)
                {
					SpawnMushrooms();
					IsPulled = true;
                }
			}
			base.Think();
		}
		public void SpawnMushrooms()
		{
			for (int i = 0; i < UtilCollection.Random(4, 5); i++)
			{
				RotoddjurAdd add = new RotoddjurAdd();
				add.X = Body.X + UtilCollection.Random(-100, 100);
				add.Y = Body.Y + UtilCollection.Random(-100, 100);
				add.Z = Body.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.RespawnInterval = -1;
				add.AddToWorld();
			}
		}
		private Spell m_RotodddjurDot;
		private Spell RotodddjurDot
		{
			get
			{
				if (m_RotodddjurDot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 3475;
					spell.Icon = 3475;
					spell.TooltipId = 3475;
					spell.Name = "Orm Poison";
					spell.Description = "Inflicts 70 damage to the target every 3 sec for 30 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.Damage = 80;
					spell.Duration = 30;
					spell.Frequency = 30;
					spell.Range = 500;
					spell.SpellID = 11855;
					spell.Target = ESpellTarget.Enemy.ToString();
					spell.Type = ESpellType.DamageOverTime.ToString();
					spell.DamageType = (int)EDamageType.Body;
					spell.Uninterruptible = true;
					m_RotodddjurDot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RotodddjurDot);
				}
				return m_RotodddjurDot;
			}
		}
	}
}
/////////////////////////////////////////////////////////////////adds///////////////////////////////////////////////////
namespace DOL.AI.Brain
{
	public class RotoddjurAddBrain : StandardMobBrain
	{
		public RotoddjurAddBrain()
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
	public class RotoddjurAdd : GameNpc
	{
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 35; // dmg reduction for melee dmg
				case EDamageType.Crush: return 35; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
				default: return 35; // dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.35;
		}
		public override short Strength { get => base.Strength; set => base.Strength = 350; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override int MaxHealth
		{
			get { return 3000; }
		}
		public override bool AddToWorld()
		{
			Model = 820;
			Name = "Rotoddjur's Servant";
			RespawnInterval = -1;
			MaxSpeedBase = 225;

			Size = (byte)UtilCollection.Random(40, 60);
			Level = (byte)UtilCollection.Random(62, 66);
			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			RotoddjurAddBrain add = new RotoddjurAddBrain();
			SetOwnBrain(add);
			base.AddToWorld();
			return true;
		}
	}
}