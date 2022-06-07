using System;
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
		}
		public static bool point1check = false;
		public static bool point2check = false;
		public static bool point3check = false;
		public static bool point4check = false;
		public static bool point5check = false;
		public static bool point6check = false;
        public static bool walkback = false;
        public override void Think()
		{
			Point3D point1 = new Point3D(783055, 882613, 4613);
			Point3D point2 = new Point3D(781504, 886149, 4613);
			Point3D point3 = new Point3D(788057, 899051, 4613);
			Point3D point4 = new Point3D(797231, 909562, 4613);
			Point3D point5 = new Point3D(791084, 894015, 4613);
			Point3D point6 = new Point3D(788652, 887943, 4613);
            Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
            #region WalkPoints
            if (!Body.InCombat && !HasAggro)
            {

                if (!Body.IsWithinRadius(point1, 30) && point1check == false)
                {
                    Body.WalkTo(point1, 200);
                }
                else
                {
                    point1check = true;
                    walkback = false;
                    if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
                    {
                        Body.WalkTo(point2, 200);
                    }
                    else
                    {
                        point2check = true;
                        if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
                            point3check == false)
                        {
                            Body.WalkTo(point3, 200);
                        }
                        else
                        {
                            point3check = true;
                            if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
                                point3check == true && point4check == false)
                            {
                                Body.WalkTo(point4, 200);
                            }
                            else
                            {
                                point4check = true;
                                if (!Body.IsWithinRadius(point5, 30) && point1check == true &&
                                    point2check == true && point3check == true && point4check == true &&
                                    point5check == false)
                                {
                                    Body.WalkTo(point5, 200);
                                }
                                else
                                {
                                    point5check = true;
                                    if (!Body.IsWithinRadius(point6, 30) && point1check == true &&
                                        point2check == true && point3check == true && point4check == true &&
                                        point5check == true && point6check == false)
                                    {
                                        Body.WalkTo(point6, 200);
                                    }
                                    else
                                    {
                                        point6check = true;
                                        if (!Body.IsWithinRadius(spawn, 30) && point1check == true &&
                                            point2check == true && point3check == true && point4check == true &&
                                            point5check == true && point6check == true && walkback == false)
                                        {
                                            Body.WalkTo(spawn, 200);
                                        }
                                        else
                                        {
                                            walkback = true;
                                            point1check = false;
                                            point2check = false;
                                            point3check = false;
                                            point4check = false;
                                            point5check = false;
                                            point6check = false;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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

