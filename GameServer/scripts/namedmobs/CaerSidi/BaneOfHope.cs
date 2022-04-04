using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class BaneOfHope : GameEpicBoss
    {
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 65; // dmg reduction for melee dmg
                case eDamageType.Crush: return 65; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 65; // dmg reduction for melee dmg
                default: return 55; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * 30;
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == "CCImmunity")
                return true;

            return base.HasAbility(keyName);
        }
        public override short MaxSpeedBase
        {
            get => (short) (191 + (Level * 2));
            set => m_maxSpeedBase = value;
        }
        public override int MaxHealth => 20000;
        public override int AttackRange
        {
            get => 180;
            set { }
        }
        public override bool AddToWorld()
        {
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158245);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;

            BaneOfHopeBrain adds = new BaneOfHopeBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            //MoveTo(CurrentRegionID, 31154, 30913, 13950, 3043);
            base.Die(killer);
        }       
    }
}
namespace DOL.AI.Brain
{
    public class BaneOfHopeBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public BaneOfHopeBrain()
            :base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public static GameLiving TeleportTarget = null;
        public static bool CanPoison = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if(ad != null && ad.Attacker != null && Body.TargetObject != ad.Attacker && CanPoison==false)
            {          
                GameLiving target = Body.TargetObject as GameLiving;
                if (Util.Chance(25))
                {
                    log.Warn("ad.attacker=" + ad.Attacker.Name);
                    GameObject oldTarget = Body.TargetObject;
                    Body.TurnTo(ad.Attacker);
                    Body.TargetObject = ad.Attacker;
                    TeleportTarget = ad.Attacker;
                    if (ad.Attacker.IsAlive)
                    {
                        Body.CastSpell(BaneOfHope_Aoe_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                        new RegionTimer(Body, new RegionTimerCallback(TeleportEnemy), 4500);
                        CanPoison = true;
                    }                  
                    if (oldTarget != null) Body.TargetObject = oldTarget;
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public int TeleportEnemy(RegionTimer timer)
        {
            if (TeleportTarget != null)
            {
                switch (Util.Random(1, 3))
                {
                    case 1:
                        {
                            TeleportTarget.MoveTo(Body.CurrentRegionID, 34496, 30879, 14551, 1045);
                            CanPoison = false;
                            TeleportTarget = null;
                        }
                        break;
                    case 2:
                        {
                            TeleportTarget.MoveTo(Body.CurrentRegionID, 37377, 30154, 13973, 978);
                            CanPoison = false;
                            TeleportTarget = null;
                        }
                        break;
                    case 3:
                        {
                            TeleportTarget.MoveTo(Body.CurrentRegionID, 38292, 31794, 13940, 986);
                            CanPoison = false;
                            TeleportTarget = null;
                        }
                        break;
                }
            }
            return 0;
        }
        public override void Think()
        {
            base.Think();
        }
        private Spell m_BaneOfHope_Aoe_Dot;
        public Spell BaneOfHope_Aoe_Dot
        {
            get
            {
                if (m_BaneOfHope_Aoe_Dot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 4;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.Damage = 350;
                    spell.Name = "Essense of Souls";
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Radius = 1500;
                    spell.Duration = 45;
                    spell.Frequency = 40; //dot tick every 4s
                    spell.SpellID = 11783;
                    spell.Target = "Enemy";
                    spell.Type = "DamageOverTime";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                    m_BaneOfHope_Aoe_Dot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BaneOfHope_Aoe_Dot);
                }

                return m_BaneOfHope_Aoe_Dot;
            }
        }
    }
}