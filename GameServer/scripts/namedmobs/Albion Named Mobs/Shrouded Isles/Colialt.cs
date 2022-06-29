using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Colialt : GameEpicBoss
	{
		public Colialt() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Colialt Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
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
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 40000; }
		}
		private bool CanSpawnZombies = false;
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000018);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			CanSpawnZombies = false;
			if(CanSpawnZombies == false)
            {
				SpawnZombies();
				CanSpawnZombies = true;
			}
			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			ColialtBrain sbrain = new ColialtBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is ColialtAddsBrain)
					npc.Die(this);
			}
			base.Die(killer);
        }
        private void SpawnZombies()
        {
			for(int i=0; i<Util.Random(10,15); i++)
            {
				ColialtAdds add = new ColialtAdds();
				add.X = X + Util.Random(-500, 500);
				add.Y = Y + Util.Random(-500, 500);
				add.Z = Z;
				add.Heading = Heading;
				add.CurrentRegion = CurrentRegion;
				add.AddToWorld();
			}
        }
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0 && ColialtBrain.ColialtPhase)
			{
				Health += ad.Damage;
			}
			base.DealDamage(ad);
		}
		public override void StartAttack(GameObject target)
        {
			if (ColialtBrain.ColialtPhase)
				return;
			else
				base.StartAttack(target);
        }
    }
}
namespace DOL.AI.Brain
{
	public class ColialtBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ColialtBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool ColialtPhase = false;
		private bool CanFollow = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				ColialtPhase = false;
				CanFollow = false;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				foreach(GameNPC npc in Body.GetNPCsInRadius(3000))
                {
					if(npc != null && npc.IsAlive && npc.Brain is ColialtAddsBrain brain)
                    {
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target != null)
							brain.AddToAggroList(target, 10);
                    }
                }
				if (Body.HealthPercent <= 30)
				{
					ColialtPhase = true;
					if (Body.attackComponent.AttackState && ColialtPhase)
						Body.attackComponent.NPCStopAttack();

					if (Body.TargetObject != null)
					{
						if (!Body.IsCasting && !Body.IsMoving && ColialtPhase)
						{
							CanFollow = false;
							if (ColialtLifeDrain != null)
							{
								if (Body.IsMoving && Body.TargetObject.IsWithinRadius(Body.TargetObject, ColialtLifeDrain.Range))
									Body.StopFollowing();
								else
									Body.Follow(Body.TargetObject, ColialtLifeDrain.Range - 50, 5000);

								Body.TurnTo(Body.TargetObject);
								Body.CastSpell(ColialtLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
							}
						}
					}
				}
				else
				{
					ColialtPhase = false;
					if (CanFollow == false)
					{
						Body.StopFollowing();//remove follow target/loc so he can follow actually again
						CanFollow = true;
					}
					Body.StopCurrentSpellcast();
					if(Body.TargetObject != null)
						Body.Follow(Body.TargetObject, 50, 3000);
				}
			}
			base.Think();
		}
		private Spell m_ColialtLifeDrain;
		private Spell ColialtLifeDrain
		{
			get
			{
				if (m_ColialtLifeDrain == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = 0;
					spell.ClientEffect = 14352;
					spell.Icon = 14352;
					spell.TooltipId = 14352;
					spell.Damage = 700;
					spell.Name = "Lifedrain";
					spell.Range = 1800;
					spell.SpellID = 11897;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Body;
					m_ColialtLifeDrain = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ColialtLifeDrain);
				}
				return m_ColialtLifeDrain;
			}
		}
	}
}
/////////////////////////////////////////////////////////////////Colialt Adds///////////////////////////////////////////////////////////
namespace DOL.GS
{
	public class ColialtAdds : GameNPC
	{
		public ColialtAdds() : base() { }
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
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override int MaxHealth
		{
			get { return 5000; }
		}
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public override bool AddToWorld()
		{
			Model = 921;
			Size = (byte)Util.Random(65, 75);
			Name = "ancient zombie";
			RespawnInterval = -1;
			Level = (byte)Util.Random(61, 66);
			MaxSpeedBase = 225;
			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			ColialtAddsBrain sbrain = new ColialtAddsBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class ColialtAddsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ColialtAddsBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
