using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class WatcherRylie : GameNPC
	{
		public WatcherRylie() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167795);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Faction = FactionMgr.GetFactionByID(79);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(79));

			WatcherRylieBrain sbrain = new WatcherRylieBrain();
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
	public class WatcherRylieBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public WatcherRylieBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
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
			if (Body.TargetObject != null && HasAggro)
			{
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "RylieBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				GameLiving target = Body.TargetObject as GameLiving;
				if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity))
					Body.CastSpell(Rylie_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				else
					Body.CastSpell(RylieDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.Think();
		}
        #region Spells
        private Spell m_RylieDD;
		private Spell RylieDD
		{
			get
			{
				if (m_RylieDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(5, 7);
					spell.ClientEffect = 4111;
					spell.Icon = 4111;
					spell.Damage = 80;
					spell.DamageType = (int)eDamageType.Energy;
					spell.Name = "Energy Blast";
					spell.Range = 1500;
					spell.SpellID = 11949;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_RylieDD = new Spell(spell, 15);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RylieDD);
				}
				return m_RylieDD;
			}
		}
		private Spell m_Rylie_stun;
		private Spell Rylie_stun
		{
			get
			{
				if (m_Rylie_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 0;
					spell.ClientEffect = 4125;
					spell.Icon = 4125;
					spell.TooltipId = 4125;
					spell.Duration = 5;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 1500;
					spell.SpellID = 11950;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					spell.DamageType = (int)eDamageType.Energy;
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_Rylie_stun = new Spell(spell, 15);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Rylie_stun);
				}
				return m_Rylie_stun;
			}
		}
		#endregion
	}
}
