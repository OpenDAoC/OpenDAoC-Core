using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
	public class Ick : GameNPC
	{
		public Ick() : base() { }

		public override bool AddToWorld()
		{
			foreach(GameNPC npc in GetNPCsInRadius(5000))
            {
				if (npc != null && npc.IsAlive && npc.Brain is IckAddBrain)
					npc.RemoveFromWorld();
            }
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162371);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			IckBrain sbrain = new IckBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			SpawnWorms();
            base.Die(killer);
        }
		private void SpawnWorms()
        {
			for (int i = 0; i < 10; i++)
			{
				IckAdd npc = new IckAdd();
				npc.X = X + Util.Random(-100, 100);
				npc.Y = Y + Util.Random(-100, 100);
				npc.Z = Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0)
				Health += ad.Damage;
			base.DealDamage(ad);
		}
	}
}
namespace DOL.AI.Brain
{
	public class IckBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public IckBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool InitlifeLeechForm = false;
		private bool lifeLeechForm = false;
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
				if(!InitlifeLeechForm)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(LifeLeech), 20000);
					InitlifeLeechForm = true;
                }
				if(lifeLeechForm && !Body.IsCasting)
                {
					Body.CastSpell(IckDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
            }
			base.Think();
		}
		private int LifeLeech(ECSGameTimer timer)
        {
			if (HasAggro && Body.TargetObject != null)
			{
				BroadcastMessage(String.Format("{0} grows in size as he steals {1}'s life energy!",Body.Name,Body.TargetObject.Name));
				lifeLeechForm = true;
				Body.Size = 50;				
			}
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(EndLifeLeech), 20000);
			return 0;
        }
		private int EndLifeLeech(ECSGameTimer timer)
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162371);
			BroadcastMessage(String.Format("{0}'s stolen life energy fades and he returns to normal.",Body.Name));
			if (HasAggro && Body.TargetObject != null)
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(LifeLeech), 20000);
			Body.Size = 20;
			lifeLeechForm = false;
			return 0;
		}
		private Spell m_IckDD;
		private Spell IckDD
		{
			get
			{
				if (m_IckDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = Util.Random(5, 8);
					spell.ClientEffect = 581;
					spell.Icon = 581;
					spell.Damage = 80;
					spell.DamageType = (int)eDamageType.Body;
					spell.Name = "LifeDrain";
					spell.Range = 1500;
					spell.SpellID = 11945;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_IckDD = new Spell(spell, 20);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IckDD);
				}
				return m_IckDD;
			}
		}
	}
}

namespace DOL.GS
{
	public class IckAdd : GameNPC
	{
		public IckAdd() : base() { }

		public override bool AddToWorld()
		{
			Name = "Ick worm";
			Level = (byte)Util.Random(17, 19);
			Model = 458;
			Size = 17;
			IckAddBrain sbrain = new IckAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class IckAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public IckAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}