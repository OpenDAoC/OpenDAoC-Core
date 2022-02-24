using System;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;

// Boss Mechanics
// Changes form every ~20 seconds
// Preceded by "Hurionthex casts a spell!"
// Has 3 different forms he randomly switches between. He remains in a form for 20 seconds.
// He then returns to his base form for 20 seconds, accompanied by system message:
// "Hurionthex returns to his natural form."
// Each state change is random, so he may change to the same form repeatedly.
// Form change accompanied by message, "A ring of magical energy emanates from Hurionthex."
// Spell animation same as ice wizard PBAOE.


namespace DOL.GS
{
    public class Hurionthex : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected String m_ChangeForm; // Message broadcast prior to changing forms.
        protected String m_BaseForm; // Message broadcast upon returning to base form.
        protected String m_PlayerKill; // Message upon killing a player

        public Hurionthex()
            : base()
        {
            m_ChangeForm = "A ring of magical energy emanates from Hurionthex.";
            m_BaseForm = "Hurionthex returns to his natural form.";
            m_PlayerKill = "";
        }


        public virtual int HurionDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * (Strength * HurionDifficulty) / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000 * HurionDifficulty / 100;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000 * HurionDifficulty / 100;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85 * HurionDifficulty / 100;
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Hurionthex", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Hurionthex not found, creating it...");

                log.Warn("Initializing Hurionthex...");
                Hurionthex Hurion = new Hurionthex();
                Hurion.Name = "Hurionthex";
                Hurion.Model = 889;
                Hurion.Gender = 0;
                Hurion.ExamineArticle = "";
                Hurion.MessageArticle = "";
                Hurion.Realm = 0;
                Hurion.Level = 81;
                Hurion.Size = 170;
                Hurion.CurrentRegionID = 191; // Galladoria
                Hurion.Strength = 500;
                Hurion.Intelligence = 220;
                Hurion.Piety = 220;
                Hurion.Dexterity = 200;
                Hurion.Constitution = 200;
                Hurion.Quickness = 125;
                Hurion.BodyType = 5; // Giant
                Hurion.MeleeDamageType = eDamageType.Crush;
                Hurion.RoamingRange = 0;
                Hurion.Faction = FactionMgr.GetFactionByID(96);
                Hurion.Faction.AddEnemyFaction(FactionMgr.GetFactionByID(89));
                Hurion.Faction.AddEnemyFaction(FactionMgr.GetFactionByID(98));
                Hurion.Faction.AddEnemyFaction(FactionMgr.GetFactionByID(111));
                Hurion.Faction.AddEnemyFaction(FactionMgr.GetFactionByID(112));
                Hurion.Faction.AddEnemyFaction(FactionMgr.GetFactionByID(115));

                Hurion.X = 55672;
                Hurion.Y = 43536;
                Hurion.Z = 12417;
                Hurion.MaxDistance = 2000;
                Hurion.MaxSpeedBase = 300;
                Hurion.Heading = 1035;

                HurionthexBrain ubrain = new HurionthexBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                Hurion.SetOwnBrain(ubrain);
                Hurion.AddToWorld();
                Hurion.Brain.Start();
                Hurion.SaveIntoDatabase();
            }
            else
                log.Warn("Hurionthex already exists in-game! Remove it and restart the server if you want to add any scripts.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class HurionthexBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public HurionthexBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 750;
        }
        
        private static int m_FormChangeTimer = 20000;
        private static volatile Timer m_timer = null;
        
        private int m_stage = 3;

        /// <summary>
        /// The current "stage" of the encounter. Hurionthex's forms are treated as stages.
        /// </summary>
        public int Stage
        {
            get { return m_stage; }
            set { if (value >= 0 && value <= 3) m_stage = value; }
        }
        
        public static bool IsBaseForm = false;
        public static bool IsSaiyanForm = false;
        public static bool IsTreantForm = false;
        public static bool IsGranidonForm = false;
        
        /// <summary>
        /// This method determines who to attack.
        /// </summary>
        public override void AttackMostWanted()
        {
            if (ECS.Debug.Diagnostics.AggroDebugEnabled)
            {
                PrintAggroTable();
            }

            Body.TargetObject = CalculateNextAttackTarget();

            if (Body.TargetObject != null)
            {
                if (!CheckSpells(eCheckSpellType.Offensive))
                {
                    Body.StartAttack(Body.TargetObject);
                }
            }
            base.AttackMostWanted();
        }

        //todo = Randomization of chosen form
        //todo = Timer for switching between forms
        //todo = Add spell casting upon expiration
        //todo = Add animations between switched forms
        
        #region Base Form
        // Base Form: Sylvan
        // Considered regular form, hits for ~450 against leather
        // DS, DA active (~35)

        /// <summary>
        /// Defines the attributes to revert to each time Hurionthex reverts to his base form.
        /// </summary>
        public void FormBase(GameNPC Hurionthex)
        {
            Hurionthex Base = new Hurionthex();
            Base.X = Hurionthex.X;
            Base.Y = Hurionthex.Y;
            Base.Z = Hurionthex.Z;
            Base.CurrentRegion = Hurionthex.CurrentRegion;
            Base.Heading = Hurionthex.Heading;
            Base.Level = Hurionthex.Level;
            Base.Realm = Hurionthex.Realm;
            Base.Model = 889;
            Base.Size = 170;
            Base.MeleeDamageType = eDamageType.Crush;
            Base.BodyType = 5; // Giant
            Base.RoamingRange = Hurionthex.RoamingRange;
            Base.MaxDistance = 2000;
            Base.Health = Hurionthex.Health;
            Base.MaxSpeedBase = Hurionthex.MaxSpeedBase;
            Base.Strength = Hurionthex.Strength;
            Base.Dexterity = Hurionthex.Dexterity;
            Base.Quickness = Hurionthex.Quickness;
            Base.Intelligence = Hurionthex.Intelligence;
            Base.Empathy = Hurionthex.Empathy;
            Base.Piety = Hurionthex.Piety;
            Base.Charisma = Hurionthex.Charisma;
        }
        
        #endregion Base Form
        
        #region Treant Form
        // Treant form
        // Behaviors: DA, DS, hits for 1000 (vs leather)

        /// <summary>
        /// Defines the attributes to change with Hurionthex when he changes to his tank (treant) form.
        /// </summary>
        public void FormTreant(GameNPC Hurionthex)
        {
            Hurionthex Treant = new Hurionthex();
            Treant.X = Hurionthex.X;
            Treant.Y = Hurionthex.Y;
            Treant.Z = Hurionthex.Z;
            Treant.CurrentRegion = Hurionthex.CurrentRegion;
            Treant.Heading = Hurionthex.Heading;
            Treant.Level = Hurionthex.Level;
            Treant.Realm = Hurionthex.Realm;
            Treant.Model = 140;
            Treant.Size = 150;
            Treant.AttackRange = 450;
            Treant.MeleeDamageType = eDamageType.Spirit;
            Treant.BodyType = 10; // Plant
            Treant.RoamingRange = Hurionthex.RoamingRange;
            Treant.MaxDistance = 2000;
            Treant.Health = Hurionthex.Health;
            Treant.MaxSpeedBase = Hurionthex.MaxSpeedBase;
            Treant.Strength = 1000;
            Treant.Constitution = 1000;
            Treant.Dexterity = Hurionthex.Dexterity;
            Treant.Quickness = Hurionthex.Quickness;
            Treant.Intelligence = Hurionthex.Intelligence;
            Treant.Empathy = Hurionthex.Empathy;
            Treant.Piety = Hurionthex.Piety;
            Treant.Charisma = Hurionthex.Charisma;
        }
        
        #endregion Treant Form

        #region Saiyan Form
        // "Saiyan" form
        // Behaviors: DS, DA, hits for ~260 (vs leather), attacks very fast
        
        /// <summary>
        /// Defines the attributes to change with Hurionthex when he changes to his attack (Saiyan) form.
        /// </summary>
        public void FormSaiyan(GameNPC Hurionthex)
        {
            Hurionthex Saiyan = new Hurionthex();
            Saiyan.X = Hurionthex.X;
            Saiyan.Y = Hurionthex.Y;
            Saiyan.Z = Hurionthex.Z;
            Saiyan.CurrentRegion = Hurionthex.CurrentRegion;
            Saiyan.Heading = Hurionthex.Heading;
            Saiyan.Level = Hurionthex.Level;
            Saiyan.Realm = Hurionthex.Realm;
            Saiyan.Model = 844;
            Saiyan.Size = 150;
            Saiyan.AttackRange = 450;
            Saiyan.MeleeDamageType = eDamageType.Spirit;
            Saiyan.BodyType = 1; // Animal
            Saiyan.RoamingRange = Hurionthex.RoamingRange;
            Saiyan.MaxDistance = 2000;
            Saiyan.Health = Hurionthex.Health;
            Saiyan.MaxSpeedBase = Hurionthex.MaxSpeedBase;
            Saiyan.Strength = 250;
            Saiyan.Constitution = Hurionthex.Constitution;
            Saiyan.Dexterity = 500;
            Saiyan.Quickness = 500;
            Saiyan.Intelligence = Hurionthex.Intelligence;
            Saiyan.Empathy = Hurionthex.Empathy;
            Saiyan.Piety = Hurionthex.Piety;
            Saiyan.Charisma = Hurionthex.Charisma;
        }
        
        #endregion Saiyan Form

        #region Granidan Form
        // Granidon form
        // Behaviors: DA, DS, 50-minute disease (Black Plague), hits for ~750 (vs leather)
        
        /// <summary>
        /// Defines the attributes to change with Hurionthex when he changes to his hybrid (Granidon) form.
        /// </summary>
        public void FormGranidon(GameNPC Hurionthex)
        {
            Hurionthex Granidon = new Hurionthex();
            Granidon.X = Hurionthex.X;
            Granidon.Y = Hurionthex.Y;
            Granidon.Z = Hurionthex.Z;
            Granidon.CurrentRegion = Hurionthex.CurrentRegion;
            Granidon.Heading = Hurionthex.Heading;
            Granidon.Level = Hurionthex.Level;
            Granidon.Realm = Hurionthex.Realm;
            Granidon.Model = 925;
            Granidon.Size = 150;
            Granidon.AttackRange = 450;
            Granidon.MeleeDamageType = eDamageType.Spirit;
            Granidon.RoamingRange = Hurionthex.RoamingRange;
            Granidon.MaxDistance = 2000;
            Granidon.Health = Hurionthex.Health;
            Granidon.MaxSpeedBase = Hurionthex.MaxSpeedBase;
            Granidon.Strength = 750;
            Granidon.Constitution = Hurionthex.Constitution;
            Granidon.Dexterity = Hurionthex.Dexterity;
            Granidon.Quickness = Hurionthex.Quickness;
            Granidon.Intelligence = 1000;
            Granidon.Empathy = 1000;
            Granidon.Piety = 1000;
            Granidon.Charisma = 1000;
        }

        #endregion Granidan Form
        
        /// <summary>
        /// Handles how the form changes. If not in base form, change to base, otherwise randomly change to another of three forms: Treant, Saiyan, Granidon.
        /// </summary>
        public void ChangeForm()
        {
            if (IsBaseForm == false)
            {
                FormBase(Body);
                return;
            }
            ChangeForm();
        }
        
        /// <summary>
        /// The timer to handle how often Huriothex's form changes occur.
        /// </summary>
        private void FormTimer(object state)
        {
            ChangeForm();
            if (m_timer != null)
                m_timer.Change(m_FormChangeTimer, Timeout.Infinite);
            return;
        }
        
        public int CastBlackPlague(RegionTimer timer)
        {
            Body.CastSpell(BlackPlague, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        
        private Spell m_BlackPlague;

        private Spell BlackPlague
        {
            get
            {
                if (BlackPlague == null)
                {
                    DBSpell BlackPlague = new DBSpell();
                    BlackPlague.AllowAdd = false;
                    BlackPlague.CastTime = 2000;
                    BlackPlague.Concentration = 0;
                    BlackPlague.MoveCast = true;
                    BlackPlague.Duration = 3000; // 50-minute timer
                    BlackPlague.Damage = 0;
                    BlackPlague.DamageType = 10; // Body
                    BlackPlague.Value = -55; // Reduce strength by 55
                    BlackPlague.Frequency = 0;
                    BlackPlague.Name = "Black Plague";
                    BlackPlague.Description =
                        "Inflicts a wasting disease on the target that slows target by 15%, reduces its strength by 55, and inhibits healing by 50%.";
                    BlackPlague.Range = 100;
                    BlackPlague.SpellID = 681001;
                    BlackPlague.EffectGroup = 0;
                    BlackPlague.ClientEffect = 10111;
                    BlackPlague.Icon = 1467;
                    BlackPlague.Target = "Enemy";
                    BlackPlague.Message1 = "You are diseased!";
                    BlackPlague.Message2 = "{0} is diseased!";
                    BlackPlague.Message3 = "You look healthy.";
                    BlackPlague.Message4 = "{0} looks healthy again.";
                    BlackPlague.Type = eSpellType.Disease.ToString();
                    BlackPlague.PackageID = "Galla_Hurionthex";
                }
                return m_BlackPlague;
            }
        }

        //Todo = Add DA, DS spells
        
        public override void Think()
        {
            // Reset boss encounter in the event of a party wipe or people running away
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
                IsBaseForm = true;
                IsSaiyanForm = false;
                IsTreantForm = false;
                IsGranidonForm = false;
            }

            if (Body.InCombat && HasAggro)
            {
                if (Body.TargetObject != null)
                {
                    //todo = Change switch case to form, make it dependent on timer trigger
                    switch (Stage)
                    {
                        case 0:
                        {
                            IsBaseForm = true;
                            FormBase(Body);
                            
                            break;
                        }
                        case 1:
                        {
                            IsSaiyanForm = true;
                            FormSaiyan(Body);
                            
                            break;
                        }
                        case 2:
                        {
                            IsTreantForm = true;
                            FormTreant(Body);
                            
                            break;
                        }
                        case 3:
                        {
                            IsGranidonForm = true;
                            FormGranidon(Body);
                            
                            break;
                        }
                    }
                }
            }
            
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(5) && Body.TargetObject != null)
                {
                    if (BlackPlague.TargetHasEffect(Body.TargetObject) == false &&
                        Body.TargetObject.IsVisibleTo(Body) && IsSaiyanForm == true)
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastBlackPlague), 2000);
                    }
                }
            }
            base.Think();
        }
    }
}