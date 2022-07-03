using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class Vagdush : GameNPC
	{
		public Vagdush() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			VagdushBrain sbrain = new VagdushBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			switch (Util.Random(1, 2))
			{
				case 1:
					SpawnPoint.X = 421759;
					SpawnPoint.Y = 650509;
					SpawnPoint.Z = 3933;
					Heading = 3842;
					break;
				case 2:
					SpawnPoint.X = 421716;
					SpawnPoint.Y = 658478;
					SpawnPoint.Z = 4196;
					Heading = 2164;
					break;
			}
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class VagdushBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public VagdushBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool CallforHelp = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				CallforHelp = false;
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
				Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			}

			if (HasAggro && Body.TargetObject != null)
			{
				if (!CallforHelp)
				{
					if (Body.HealthPercent <= 10)
					{
						BroadcastMessage("The " + Body.Name + " calls for help!");
						foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
						{
							if (npc != null && npc.IsAlive && npc.PackageID == "VagdushBaf")
								AddAggroListTo(npc.Brain as StandardMobBrain);
						}
						CallforHelp = true;
					}				
				}
				GameLiving target = Body.TargetObject as GameLiving;
				if(!target.IsWithinRadius(Body,Body.AttackRange) && target.IsAlive && target != null)
                {
					Body.MaxSpeedBase = 0;
					if (!Body.IsCasting && Util.Chance(100))
					{
						if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.Disease))
							Body.CastSpell(VagdushDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						else
							Body.CastSpell(VagdushDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					}
				}
				else
                {
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12742);
					Body.MaxSpeedBase = npcTemplate.MaxSpeed;
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_VagdushDisease;
		private Spell VagdushDisease
		{
			get
			{
				if (m_VagdushDisease == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.RecastDelay = 0;
					spell.ClientEffect = 731;
					spell.Icon = 731;
					spell.TooltipId = 731;
					spell.Name = "Persistent Disease";
					spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
					spell.Message1 = "You are diseased!";
					spell.Message2 = "{0} is diseased!";
					spell.Message3 = "You look healthy.";
					spell.Message4 = "{0} looks healthy again.";
					spell.Range = 1500;
					spell.Duration = 60;
					spell.SpellID = 11986;
					spell.Target = "Enemy";
					spell.Type = "Disease";
					spell.DamageType = (int)eDamageType.Body; //Energy DMG Type
					m_VagdushDisease = new Spell(spell, 10);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagdushDisease);
				}
				return m_VagdushDisease;
			}
		}
		private Spell m_VagdushDD;
		private Spell VagdushDD
		{
			get
			{
				if (m_VagdushDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 754;
					spell.Icon = 754;
					spell.Name = "Vagdush Blast";
					spell.Damage = 50;
					spell.Range = 1500;
					spell.SpellID = 11987;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Matter;
					m_VagdushDD = new Spell(spell, 10);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagdushDD);
				}
				return m_VagdushDD;
			}
		}
		#endregion
	}
}
