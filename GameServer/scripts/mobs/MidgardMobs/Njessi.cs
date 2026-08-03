using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.GS
{
	public class Njessi : GameNPC
	{
		public Njessi() : base() { }

        public override bool IsVisibleToPlayers => true; //mob brain will work if there are 0 players around

        private const short PATROL_SPEED = 120;

        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (783055, 882613, 4613),
            (781504, 886149, 4613),
            (788057, 899051, 4613),
            (797231, 909562, 4613),
            (791084, 894015, 4613),
            (788652, 887943, 4613)
        ];

        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164504);
			LoadTemplate(npcTemplate);
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            NjessiBrain sbrain = new NjessiBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (Util.Chance(10) && !ad.Target.IsPoisoned)
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                    CastSpell(NjessiPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            if (Util.Chance(30))
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                    CastSpell(NjessiDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
        }
        #region Spells
        private Spell m_NjessiDD;
        public Spell NjessiDD
        {
            get
            {
                if (m_NjessiDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Power = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 5700;
                    spell.Icon = 5700;
                    spell.Damage = 100;
                    spell.DamageType = (int)eDamageType.Heat;
                    spell.Name = "Flame Breath";
                    spell.Range = 500;
                    spell.Radius = 300;
                    spell.SpellID = 11933;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_NjessiDD = new Spell(spell, 20);
                }
                return m_NjessiDD;
            }
        }
        private Spell m_NjessiPoison;
        public Spell NjessiPoison
        {
            get
            {
                if (m_NjessiPoison == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 4099;
                    spell.Icon = 4099;
                    spell.TooltipId = 4099;
                    spell.Name = "Njessi Venom";
                    spell.Description = "Inflicts 25 damage to the target every 3 sec for 20 seconds";
                    spell.Message1 = "You are afflicted with a vicious poison!";
                    spell.Message2 = "{0} has been poisoned!";
                    spell.Message3 = "The poison has run its course.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.Damage = 25;
                    spell.Duration = 20;
                    spell.Frequency = 30;
                    spell.Range = 500;
                    spell.SpellID = 11934;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DamageOverTime.ToString();
                    spell.DamageType = (int)eDamageType.Body;
                    spell.Uninterruptible = true;
                    m_NjessiPoison = new Spell(spell, 20);
                }
                return m_NjessiPoison;
            }
        }
        #endregion
    }
}
namespace DOL.AI.Brain
{
	public class NjessiBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public NjessiBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}

        public override void Think()
		{
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                        AddToAggroList(player);
                }
                foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
                {
                    if (npc != null && npc.IsAlive && npc.Realm != Body.Realm)
                        AddToAggroList(npc);
                }
            }
            base.Think();
		}
	}
}
