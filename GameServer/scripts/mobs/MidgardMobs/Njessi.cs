using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Njessi : GameNPC
	{
		public Njessi() : base() { }

        public override bool IsVisibleToPlayers => true; //mob brain will work if there are 0 players around

        public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164504);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

            NjessiBrain sbrain = new NjessiBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void WalkToSpawn()
        {
            if (IsAlive)
                return;
            base.WalkToSpawn();
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (Util.Chance(10) && !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.DamageOverTime))
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_NjessiDD = new Spell(spell, 20);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NjessiDD);
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DamageOverTime.ToString();
                    spell.DamageType = (int)eDamageType.Body;
                    spell.Uninterruptible = true;
                    m_NjessiPoison = new Spell(spell, 20);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NjessiPoison);
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
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public NjessiBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
            
            _roamingPathPoints.Add(new Point3D(783055, 882613, 4613));
            _roamingPathPoints.Add(new Point3D(781504, 886149, 4613));
            _roamingPathPoints.Add(new Point3D(788057, 899051, 4613));
            _roamingPathPoints.Add(new Point3D(797231, 909562, 4613));
            _roamingPathPoints.Add(new Point3D(791084, 894015, 4613));
            _roamingPathPoints.Add(new Point3D(788652, 887943, 4613));
		}
		
		private List<Point3D> _roamingPathPoints = new List<Point3D>();
        private int _lastRoamIndex = 0;
        
        public override void Think()
		{
			
            Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
            #region WalkPoints
            if (!Body.InCombat && !HasAggro)
            {

	            if (Body.IsWithinRadius(_roamingPathPoints[_lastRoamIndex], 100))
	            {
		            _lastRoamIndex++;
	            }

	            if (_lastRoamIndex >= _roamingPathPoints.Count)
	            {
		            _lastRoamIndex = 0;
	            }
	            else if(!Body.IsMoving) Body.WalkTo(_roamingPathPoints[_lastRoamIndex], 120);
                
            }
            #endregion
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null && player.IsAlive && !AggroTable.ContainsKey(player) && player.Client.Account.PrivLevel == 1)
                        AggroTable.Add(player, 10);
                }
                foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
                {
                    if (npc != null && npc.IsAlive && npc.Realm != Body.Realm && !AggroTable.ContainsKey(npc))
                        AggroTable.Add(npc, 10);
                }
            }
            base.Think();
		}
	}
}

