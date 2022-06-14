using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

#region Tabor
namespace DOL.GS
{
	public class Tabor : GameNPC
	{
		public Tabor() : base() { }

		public override bool AddToWorld()
		{
			foreach(GameNPC npc in GetNPCsInRadius(5000))
            {
				if (npc != null && npc.IsAlive && npc.Brain is TaborGhostBrain)
					npc.RemoveFromWorld();
            }
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166738);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 315, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Slash;

			TaborBrain sbrain = new TaborBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		public override void Die(GameObject killer)
        {
			BroadcastMessage(String.Format("As {0} falls to the ground, you feel a breeze in the air.\nA swirl of dirt covers the area.", Name));
			SpawnSwirlDirt();
            base.Die(killer);
        }
		private void SpawnSwirlDirt()
        {
			SwirlDirt npc = new SwirlDirt();
			npc.X = 37256;
			npc.Y = 32460;
			npc.Z = 14437;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
    }
}
namespace DOL.AI.Brain
{
	public class TaborBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public TaborBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
        public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if(!Tabor_Dot.TargetHasEffect(target) && Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (!Tabor_Dot2.TargetHasEffect(target) && Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_Dot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.Think();
		}
		#region Spells
		private Spell m_Tabor_DD;
		private Spell Tabor_DD
		{
			get
			{
				if (m_Tabor_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(10,15);
					spell.ClientEffect = 5087;
					spell.Icon = 5087;
					spell.TooltipId = 5087;
					spell.Damage = 100;
					spell.Name = "Earth Blast";
					spell.Range = 1500;
					spell.SpellID = 11931;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_DD = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD);
				}
				return m_Tabor_DD;
			}
		}
		private Spell m_Tabor_DD2;
		private Spell Tabor_DD2
		{
			get
			{
				if (m_Tabor_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(15, 20);
					spell.ClientEffect = 5087;
					spell.Icon = 5087;
					spell.TooltipId = 5087;
					spell.Damage = 80;
					spell.Name = "Earth Blast";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11932;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_DD2 = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD2);
				}
				return m_Tabor_DD2;
			}
		}
		private Spell m_Tabor_Dot;
		private Spell Tabor_Dot
		{
			get
			{
				if (m_Tabor_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.Name = "Poison";
					spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 3411;
					spell.Range = 1500;
					spell.Damage = 25;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11933;
					spell.Target = "Enemy";
					spell.SpellGroup = 1802;
					spell.EffectGroup = 1502;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_Dot = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot);
				}
				return m_Tabor_Dot;
			}
		}
		private Spell m_Tabor_Dot2;
		private Spell Tabor_Dot2
		{
			get
			{
				if (m_Tabor_Dot2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3475;
					spell.Icon = 4431;
					spell.Name = "Acid";
					spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 4431;
					spell.Range = 1500;
					spell.Damage = 25;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11934;
					spell.Target = "Enemy";
					spell.SpellGroup = 1803;
					spell.EffectGroup = 1503;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Tabor_Dot2 = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot2);
				}
				return m_Tabor_Dot2;
			}
		}
		#endregion
	}
}
#endregion

#region Ghost of Tabor
namespace DOL.GS
{
	public class TaborGhost : GameNPC
	{
		public TaborGhost() : base() { }
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		public override bool AddToWorld()
		{
			BroadcastMessage("Ghost of Tabor says, \"You thought the fight was over did you ? \"");
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161293);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 445, 0, 0);
			template.AddNPCEquipment(eInventorySlot.DistanceWeapon, 471, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			VisibleActiveWeaponSlots = 16;
			MeleeDamageType = eDamageType.Slash;

			RespawnInterval = -1;
			TaborGhostBrain sbrain = new TaborGhostBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			if(killer != null)
				BroadcastMessage(String.Format("{0} says, \"I will return some day.Be warned!\"",Name));
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class TaborGhostBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public TaborGhostBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				if (!Tabor_Dot.TargetHasEffect(target) && Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (!Tabor_Dot2.TargetHasEffect(target) && Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_Dot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(15) && !Body.IsCasting)
					Body.CastSpell(Tabor_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			}
			base.Think();
		}
		#region Spells
		private Spell m_Tabor_DD;
		private Spell Tabor_DD
		{
			get
			{
				if (m_Tabor_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(10, 15);
					spell.ClientEffect = 5087;
					spell.Icon = 5087;
					spell.TooltipId = 5087;
					spell.Damage = 100;
					spell.Name = "Earth Blast";
					spell.Range = 1500;
					spell.SpellID = 11938;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_DD = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD);
				}
				return m_Tabor_DD;
			}
		}
		private Spell m_Tabor_DD2;
		private Spell Tabor_DD2
		{
			get
			{
				if (m_Tabor_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = Util.Random(15, 20);
					spell.ClientEffect = 5087;
					spell.Icon = 5087;
					spell.TooltipId = 5087;
					spell.Damage = 80;
					spell.Name = "Earth Blast";
					spell.Range = 1500;
					spell.Radius = 350;
					spell.SpellID = 11937;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_DD2 = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_DD2);
				}
				return m_Tabor_DD2;
			}
		}
		private Spell m_Tabor_Dot;
		private Spell Tabor_Dot
		{
			get
			{
				if (m_Tabor_Dot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3411;
					spell.Icon = 3411;
					spell.Name = "Poison";
					spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 3411;
					spell.Range = 1500;
					spell.Damage = 25;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11936;
					spell.Target = "Enemy";
					spell.SpellGroup = 1802;
					spell.EffectGroup = 1502;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Matter;
					m_Tabor_Dot = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot);
				}
				return m_Tabor_Dot;
			}
		}
		private Spell m_Tabor_Dot2;
		private Spell Tabor_Dot2
		{
			get
			{
				if (m_Tabor_Dot2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 20;
					spell.ClientEffect = 3475;
					spell.Icon = 4431;
					spell.Name = "Acid";
					spell.Description = "Inflicts 25 damage to the target every 4 sec for 20 seconds";
					spell.Message1 = "An acidic cloud surrounds you!";
					spell.Message2 = "{0} is surrounded by an acidic cloud!";
					spell.Message3 = "The acidic mist around you dissipates.";
					spell.Message4 = "The acidic mist around {0} dissipates.";
					spell.TooltipId = 4431;
					spell.Range = 1500;
					spell.Damage = 25;
					spell.Duration = 20;
					spell.Frequency = 40;
					spell.SpellID = 11935;
					spell.Target = "Enemy";
					spell.SpellGroup = 1803;
					spell.EffectGroup = 1503;
					spell.Type = eSpellType.DamageOverTime.ToString();
					spell.Uninterruptible = true;
					spell.DamageType = (int)eDamageType.Body;
					m_Tabor_Dot2 = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Tabor_Dot2);
				}
				return m_Tabor_Dot2;
			}
		}
		#endregion
	}
}
#endregion

#region Swirl of Dirt
namespace DOL.GS
{
	public class SwirlDirt : GameNPC
	{
		public SwirlDirt() : base() { }

		public override bool AddToWorld()
		{
			Name = "Swirl of Dirt";
			Level = 50;
			Model = 665;
			Size = 70;
			Flags = (GameNPC.eFlags)28;

			SwirlDirtBrain sbrain = new SwirlDirtBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1000);
			}
			return success;
		}
		#region Show Effects
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 6072, 0, false, 0x01);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(SpawnGhostTabor), 1000);
			}
			return 0;
		}
		protected int RemoveMob(ECSGameTimer timer)
		{
			if (IsAlive)
				RemoveFromWorld();
			return 0;
		}
		private int SpawnGhostTabor(ECSGameTimer timer)
        {
			SpawnGhostOfTabor();
			return 0;
        }
		private void SpawnGhostOfTabor()
		{
			foreach (GameNPC mob in GetNPCsInRadius(5000))
			{
				if (mob.Brain is TaborGhostBrain)
					return;
			}
			TaborGhost npc = new TaborGhost();
			npc.X = 37256;
			npc.Y = 32460;
			npc.Z = 14437;
			npc.Heading = Heading;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
			new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveMob), 500);
		}
		#endregion
	}
}
namespace DOL.AI.Brain
{
	public class SwirlDirtBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SwirlDirtBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 0;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion