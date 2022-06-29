using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Amphiptere : GameEpicBoss
	{
		public Amphiptere() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Amphiptere Initializing...");
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157842);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			AmphiptereBrain sbrain = new AmphiptereBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is AmphiptereAddsBrain)
					npc.Die(this);
			}
			base.Die(killer);
        }
        public override void OnAttackEnemy(AttackData ad)
        {
			if (Util.Chance(35))//cast nasty heat proc
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(HeatProc, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
        }
        private Spell m_HeatProc;
		private Spell HeatProc
		{
			get
			{
				if (m_HeatProc == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 4051;
					spell.Icon = 4051;
					spell.TooltipId = 4051;
					spell.Damage = 350;
					spell.Name = "Heat Proc";
					spell.Range = 350;
					spell.SpellID = 11906;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_HeatProc = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HeatProc);
				}
				return m_HeatProc;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class AmphiptereBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AmphiptereBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		private bool CanSpawnAdds = false;
		private bool RemoveAdds = false;
		public override void OnAttackedByEnemy(AttackData ad)
		{
			if(ad != null && ad.Damage > 0 && ad.Attacker != null && CanSpawnAdds == false && Util.Chance(20))
            {
				BroadcastMessage(String.Format("A blow knocks one of " + Body.Name + "'s tooths to the ground."));
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdd), 5000);
				CanSpawnAdds = true;
			}
			base.OnAttackedByEnemy(ad);
		}
		private int SpawnAdd(ECSGameTimer timer)
        {
			if (HasAggro && Body.IsAlive)
			{
				AmphiptereAdds add = new AmphiptereAdds();
				add.X = Body.X + Util.Random(-500, 500);
				add.Y = Body.Y + Util.Random(-500, 500);
				add.Z = Body.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpawnAdd), Util.Random(25000, 35000));
			return 0;
        }
		private int ResetSpawnAdd(ECSGameTimer timer)
        {
			CanSpawnAdds = false;
			return 0;
        }
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnAdds = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is AmphiptereAddsBrain)
							npc.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is AmphiptereAddsBrain brain)
						if (target != null && !brain.HasAggro)
							brain.AddToAggroList(target, 100);
				}				
				if (target != null)
				{
					if (Body.IsCasting)
					{
						if (Body.attackComponent.AttackState)
							Body.attackComponent.NPCStopAttack();
						if (Body.IsMoving)
							Body.StopFollowing();
					}
					Body.TurnTo(Body.TargetObject);
					if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
						Body.CastSpell(Amphiptere_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
						Body.CastSpell(AmphiptereDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					if (Util.Chance(25) && !Body.IsCasting && !Body.effectListComponent.ContainsEffectForEffectType(eEffect.Bladeturn))
						Body.CastSpell(Bubble, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
				}
			}
			base.Think();
		}
        #region Spells
        private Spell m_Amphiptere_Dot;
		private Spell Amphiptere_Dot
		{
			get
			{
				if (m_Amphiptere_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 562;
					spell.Icon = 562;
					spell.Name = "Amphiptere's Venom";
					spell.Description = "Inflicts 80 damage to the target every 4 sec for 40 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 562;
					spell.Range = 1500;
					spell.Damage = 80;
					spell.Duration = 40;
					spell.Frequency = 40;
					spell.SpellID = 11908;
					spell.Target = "Enemy";
					spell.SpellGroup = 1800;
					spell.EffectGroup = 1500;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Amphiptere_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Amphiptere_Dot);
				}
				return m_Amphiptere_Dot;
			}
		}
		private Spell m_AmphiptereDisease;
		private Spell AmphiptereDisease
		{
			get
			{
				if (m_AmphiptereDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 40;
					spell.ClientEffect = 731;
					spell.Icon = 731;
					spell.Name = "Amphiptere's Infection";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.TooltipId = 731;
					spell.Range = 1500;
					spell.Duration = 120;
					spell.SpellID = 11907;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_AmphiptereDisease = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AmphiptereDisease);
				}
				return m_AmphiptereDisease;
			}
		}
		private Spell m_Bubble;
		private Spell Bubble
		{
			get
			{
				if (m_Bubble == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 10;
					spell.Duration = 10;
					spell.ClientEffect = 5126;
					spell.Icon = 5126;
					spell.TooltipId = 5126;
					spell.Name = "Shield of Feathers";
					spell.Range = 0;
					spell.SpellID = 11909;
					spell.Target = "Self";
					spell.Type = eSpellType.Bladeturn.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Bubble = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bubble);
				}
				return m_Bubble;
			}
		}
        #endregion
    }
}
////////////////////////////////////////////////////////////////////////Zombie Adds//////////////////////////////////////////////////
namespace DOL.GS
{
	public class AmphiptereAdds : GameNPC
	{
		public AmphiptereAdds() : base() { }
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
			get { return 3000; }
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 120; }
		public override bool AddToWorld()
		{
			Model = 921;
			Size = (byte)Util.Random(65, 75);
			Name = "ancient zombie";
			RespawnInterval = -1;
			Level = (byte)Util.Random(61, 63);
			MaxSpeedBase = 225;
			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			AmphiptereAddsBrain sbrain = new AmphiptereAddsBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class AmphiptereAddsBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AmphiptereAddsBrain() : base()
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
