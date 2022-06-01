using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Vortanos : GameEpicBoss
	{
		public Vortanos() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Vortanos Initializing...");
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
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167731);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			VortanosBrain sbrain = new VortanosBrain();
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
				if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
		public override void EnemyKilled(GameLiving enemy)
        {
			
            base.EnemyKilled(enemy);
        }
        public override void OnAttackEnemy(AttackData ad)
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
			{
				if(Util.Chance(50) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff))
					CastSpell(VortanosSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(50) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DexQuiDebuff))
					CastSpell(VortanosDebuffDQ, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		#region Spells
		private Spell m_VortanosSCDebuff;
		private Spell VortanosSCDebuff
		{
			get
			{
				if (m_VortanosSCDebuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.ClientEffect = 5408;
					spell.Icon = 5408;
					spell.Name = "S/C Debuff";
					spell.TooltipId = 5408;
					spell.Range = 1200;
					spell.Value = 65;
					spell.Duration = 60;
					spell.SpellID = 11917;
					spell.Target = "Enemy";
					spell.Type = "StrengthConstitutionDebuff";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_VortanosSCDebuff = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VortanosSCDebuff);
				}
				return m_VortanosSCDebuff;
			}
		}
		private Spell m_VortanosDebuffDQ;
		private Spell VortanosDebuffDQ
		{
			get
			{
				if (m_VortanosDebuffDQ == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 60;
					spell.Duration = 60;
					spell.Value = 65;
					spell.ClientEffect = 2627;
					spell.Icon = 2627;
					spell.TooltipId = 2627;
					spell.Name = "D/Q Debuff";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11918;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DexterityQuicknessDebuff.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_VortanosDebuffDQ = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VortanosDebuffDQ);
				}
				return m_VortanosDebuffDQ;
			}
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class VortanosBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public VortanosBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;		
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		private bool CanSpawnAdds = false;
		private bool NotInCombat = false;
		private bool InCombat1 = false;
		private bool SpamMess1 = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_SystemWindow);
			}
		}
        public override void OnAttackedByEnemy(AttackData ad)
        {
			if(ad != null && ad.Damage > 0 && !SpamMess1 && Util.Chance(25))
            {
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpamMessage), 1000);
				SpamMess1=true;
            }
            base.OnAttackedByEnemy(ad);
        }
		private int SpamMessage(ECSGameTimer timer)
        {
			if(HasAggro)
				BroadcastMessage(Body.Name + " says, \"The living can never conquer the eternal darkness of death incarnate!\"");

			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpamMessage), Util.Random(25000,45000));
			return 0;
        }
		private int ResetSpamMessage(ECSGameTimer timer)
		{
			SpamMess1 = false;
			return 0;
		}
		public override void Think()
		{
			if (Body.CurrentRegion.IsNightTime)
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			if (Body.CurrentRegion.IsNightTime == false)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					changed = false;
				}
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnAdds = false;
				SpamMess1 = false;
				foreach(GameNPC npc in Body.GetNPCsInRadius(5000))
                {
					if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain)
					{
						npc.RemoveFromWorld();
						if (!NotInCombat)
						{
							BroadcastMessage(Body.Name + " says, \"Sleep my unwilling prisoners, you are no longer needed here\"");
							NotInCombat = true;
						}
					}
                }
			}
			if(HasAggro && Body.TargetObject != null)
            {
				NotInCombat = false;
				if(!InCombat1)
                {
					BroadcastMessage(Body.Name + " says, \"Your flesh will be mine, one piece at a time!\"");
					InCombat1 = true;
                }
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain brain)
					{
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
				if (Util.Chance(35) && !Body.IsCasting)
					Body.CastSpell(Vortanos_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
				if (Util.Chance(35) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
					Body.CastSpell(Vortanos_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if(!CanSpawnAdds)
                {
					SpawnAdds();
					CanSpawnAdds = true;
                }					
			}
			base.Think();
		}
		private void SpawnAdds()
        {
			for(int i= 0; i < 2; i++)
            {
				VortanosAdd add = new VortanosAdd();
				add.Model = 1676;
				add.Name = "fell geist";
				add.Level = (byte)Util.Random(50, 56);
				add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
				add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
				add.Z = Body.SpawnPoint.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
			for (int i = 0; i < 4; i++)
			{
				VortanosAdd add = new VortanosAdd();
				add.Model = 921;
				add.Name = "ancient zombie";
				add.Level = (byte)Util.Random(54, 61);
				add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
				add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
				add.Z = Body.SpawnPoint.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
			for (int i = 0; i < 4; i++)
			{
				VortanosAdd add = new VortanosAdd();
				add.Model = 921;
				add.Name = "ghoul desecrator ";
				add.Level = (byte)Util.Random(49, 53);
				add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
				add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
				add.Z = Body.SpawnPoint.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.AddToWorld();
			}
		}
		#region Spells
		private Spell m_Vortanos_DD;
		private Spell Vortanos_DD
		{
			get
			{
				if (m_Vortanos_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = 10;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.TooltipId = 1609;
					spell.Damage = 450;
					spell.Name = "Fire Blast";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11915;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Vortanos_DD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vortanos_DD);
				}
				return m_Vortanos_DD;
			}
		}
		private Spell m_Vortanos_Dot;
		private Spell Vortanos_Dot
		{
			get
			{
				if (m_Vortanos_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 562;
					spell.Icon = 562;
					spell.Name = "Disintegrating Force";
					spell.Description = "Target takes 89 matter damage every 2.0 sec.";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 562;
					spell.Range = 1500;
					spell.Damage = 89;
					spell.Duration = 12;
					spell.Frequency = 20;
					spell.SpellID = 11916;
					spell.Target = "Enemy";
					spell.SpellGroup = 1800;
					spell.EffectGroup = 1500;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Vortanos_Dot = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vortanos_Dot);
				}
				return m_Vortanos_Dot;
			}
		}		
		#endregion
	}
}
//////////////////////////////////////////////////////////////////////////////adds/////////////////////////////////////////////////////////////
#region Vortanos Adds
namespace DOL.GS
{
	public class VortanosAdd : GameNPC
	{
		public VortanosAdd() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 10;// dmg reduction for melee dmg
				case eDamageType.Crush: return 10;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 10;// dmg reduction for melee dmg
				default: return 10;// dmg reduction for rest resists
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
			get { return 2000; }
		}
        #region Stats
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 120; }
        #endregion
		public override bool AddToWorld()
		{
			Size = (byte)Util.Random(50, 55);
			MaxSpeedBase = 225;
			RespawnInterval = -1;

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			VortanosAddBrain sbrain = new VortanosAddBrain();
			SetOwnBrain(sbrain);
			RoamingRange = 300;
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
		public override long ExperienceValue => 0;
		public override void DropLoot(GameObject killer)
		{
		}
	}
}
namespace DOL.AI.Brain
{
	public class VortanosAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public VortanosAddBrain() : base()
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
#endregion
