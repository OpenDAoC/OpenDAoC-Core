using System.Linq;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
	public class Dooben : GameNPC
	{
		public Dooben() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12676);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			//RespawnInterval = Util.Random(3600000, 7200000);

			DoobenBrain sbrain = new DoobenBrain();
			if (NPCTemplate != null)
			{
				sbrain.AggroLevel = NPCTemplate.AggroLevel;
				sbrain.AggroRange = NPCTemplate.AggroRange;
			}
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
		
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(45))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
					CastSpell(DoobenDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		private Spell m_DoobenDD;
		public Spell DoobenDD
		{
			get
			{
				if (m_DoobenDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 127;
					spell.Icon = 127;
					spell.Damage = 25;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Sand Strike";
					spell.Range = 350;
					spell.SpellID = 11988;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					m_DoobenDD = new Spell(spell, 10);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DoobenDD);
				}
				return m_DoobenDD;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class DoobenBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DoobenBrain() : base()
		{
			ThinkInterval = 1500;
		}
		private bool NotInCombat = false;
		public override void Think()
		{
			if (!HasAggressionTable())
            {
				if (NotInCombat == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
					NotInCombat = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
				NotInCombat = false;

			base.Think();
		}
		#region Show Effects
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (Body.IsAlive && !HasAggro)
			{
				Parallel.ForEach(Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
				{
					player?.Out.SendSpellEffectAnimation(Body, Body, 479, 0, false, 0x01);
				});

				return 1600;
			}
			return 0;
		}
		
		#endregion
	}
}



