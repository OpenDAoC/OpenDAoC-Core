using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class Ruckus : GameNPC
	{
		public Ruckus() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165469);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RuckusBrain sbrain = new RuckusBrain();
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
	public class RuckusBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public RuckusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool PrepareStun = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if (Util.Chance(25) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) 
					&& !target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun) && target.IsAlive && target != null && !PrepareStun)
                {
					BroadcastMessage(String.Format("Ruckus begins saving energy for a stunning blow.\nRuckus attacks begin to stun his opponent with next blow.​"));
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastStun), 2000);
					PrepareStun = true;
                }
				if (!Body.effectListComponent.ContainsEffectForEffectType(eEffect.DamageAdd) && !Body.IsCasting)
					Body.CastSpell(RuckusDA, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.Think();
		}
		private int CastStun(ECSGameTimer timer)
        {
			if (HasAggro && Body.TargetObject != null)		
				Body.CastSpell(Ruckus_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetStun), 20000);
			return 0;
        }
		private int ResetStun(ECSGameTimer timer)
		{
			PrepareStun = false;
			return 0;
		}
		#region Spells
		private Spell m_RuckusDA;
		private Spell RuckusDA
		{
			get
			{
				if (m_RuckusDA == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 10;
					spell.ClientEffect = 18;
					spell.Icon = 18;
					spell.Damage = 10;
					spell.Duration = 10;
					spell.DamageType = (int)eDamageType.Matter;
					spell.Name = "Earthen Fury";
					spell.Range = 1000;
					spell.SpellID = 11942;
					spell.Target = "Self";
					spell.Type = eSpellType.DamageAdd.ToString();
					spell.Uninterruptible = true;
					m_RuckusDA = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RuckusDA);
				}
				return m_RuckusDA;
			}
		}
		private Spell m_Ruckus_stun;
		private Spell Ruckus_stun
		{
			get
			{
				if (m_Ruckus_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2165;
					spell.Icon = 2132;
					spell.TooltipId = 2132;
					spell.Duration = 4;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 400;
					spell.SpellID = 11943;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					m_Ruckus_stun = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Ruckus_stun);
				}
				return m_Ruckus_stun;
			}
		}
        #endregion
    }
}

