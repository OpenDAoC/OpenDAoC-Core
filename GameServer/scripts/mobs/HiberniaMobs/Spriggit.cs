using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Spriggit : GameNPC
	{
		public Spriggit() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166496);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			SpriggitBrain sbrain = new SpriggitBrain();
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
	public class SpriggitBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SpriggitBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool mobHasAggro = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		public override void Think()
		{
			if(!HasAggressionTable())
            {
				mobHasAggro = false;
            }
			if (HasAggro && Body.TargetObject != null)
			{
				if(!mobHasAggro)
                {
					BroadcastMessage(String.Format("Spriggit crackles as he attacks {0}!",Body.TargetObject.Name));
					mobHasAggro = true;
                }
				GameLiving target = Body.TargetObject as GameLiving;
				if(target != null && target.IsAlive)
                {
					if(!target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity) && Util.Chance(25))
						Body.CastSpell(SpriggitRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
					if(Util.Chance(30))
						Body.CastSpell(SpriggitDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "SpriggitBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
		#region Spells
		private Spell m_SpriggitDD;
		private Spell SpriggitDD
		{
			get
			{
				if (m_SpriggitDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(10,15);
					spell.ClientEffect = 161;
					spell.Icon = 161;
					spell.Damage = 90;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Frost Blast";
					spell.Range = 1500;
					spell.SpellID = 11941;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_SpriggitDD = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpriggitDD);
				}
				return m_SpriggitDD;
			}
		}
		private Spell m_SpriggitRoot;
		private Spell SpriggitRoot
		{
			get
			{
				if (m_SpriggitRoot == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(25, 35);
					spell.ClientEffect = 5204;
					spell.Icon = 5204;
					spell.TooltipId = 5204;
					spell.Duration = 30;
					spell.Value = 99;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Root";
					spell.Range = 1500;
					spell.SpellID = 11942;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpeedDecrease.ToString();
					spell.Uninterruptible = true;
					m_SpriggitRoot = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpriggitRoot);
				}
				return m_SpriggitRoot;
			}
		}
		#endregion
	}
}
