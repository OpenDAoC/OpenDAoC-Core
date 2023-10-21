using System.Reflection;
using Core.AI;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.PlayerClass;
using Core.GS.ServerProperties;
using log4net;

namespace Core.GS
{
    public class Doppelganger : GameSummoner
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static new readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        override public int PetSummonThreshold { get { return 50; } }

        override public NpcTemplate PetTemplate { get { return m_petTemplate; } }
        static private NpcTemplate m_petTemplate = null;

        override public byte PetLevel { get { return 50; } }
        override public byte PetSize { get { return 50; } }

        public Doppelganger() : base() { }
        public Doppelganger(ABrain defaultBrain) : base(defaultBrain) { }
        public Doppelganger(INpcTemplate template) : base(template) { }

        static Doppelganger()
        {
           DbNpcTemplate chthonian = CoreDb<DbNpcTemplate>.SelectObject(DB.Column("Name").IsEqualTo("chthonian crawler"));
            if (chthonian != null)
                m_petTemplate = new NpcTemplate(chthonian);
        }

        /// <summary>
        /// Realm point value of this living
        /// </summary>
        public override int RealmPointsValue
        {
            get { return Properties.DOPPELGANGER_REALM_POINTS; }
        }

        /// <summary>
        /// Bounty point value of this living
        /// </summary>
        public override int BountyPointsValue
        {
            get { return Properties.DOPPELGANGER_BOUNTY_POINTS; }
        }

        protected const ushort doppelModel = 2248;

        /// <summary>
        /// Gets/sets the object health
        /// </summary>
        public override int Health
        {
            get { return base.Health; }
            set
            {
                base.Health = value;

                if (value >= MaxHealth)
                {
                    if (Model == doppelModel)
                        Disguise();
                }
                else if (value <= MaxHealth >> 1 && Model != doppelModel)
                {
                    Model = doppelModel;
                    Name = "doppelganger";
                    Inventory = new GameNpcInventory(GameNpcInventoryTemplate.EmptyTemplate);
                    BroadcastLivingEquipmentUpdate();
                }
            }
        }

        /// <summary>
        /// Load a npc from the npc template
        /// </summary>
        /// <param name="obj">template to load from</param>
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            Disguise();
        }

        /// <summary>
        /// Starts a melee or ranged attack on a given target.
        /// </summary>
        /// <param name="target">The object to attack.</param>
        public override void StartAttack(GameObject target)
        {
            // Don't allow ranged attacks
            if (ActiveWeaponSlot == EActiveWeaponSlot.Distance)
            {
                bool standard = Inventory.GetItem(EInventorySlot.RightHandWeapon) != null;
                bool twoHanded = Inventory.GetItem(EInventorySlot.TwoHandWeapon) != null;

                if (standard && twoHanded)
                {
                    if (Util.Random(1) < 1)
                        SwitchWeapon(EActiveWeaponSlot.Standard);
                    else
                        SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                }
                else if (twoHanded)
                    SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                else
                    SwitchWeapon(EActiveWeaponSlot.Standard);
            }

            base.StartAttack(target);
        }

        /// <summary>
        /// Disguise the doppelganger as an invader
        /// </summary>
        protected void Disguise()
        {
            if (Util.Chance(50))
                Gender = EGender.Male;
            else
                Gender = EGender.Female;

            IPlayerClass playerClass = new DefaultPlayerClass();

            switch (Util.Random(2))
            {
                case 0: // Albion
                    Name = $"Albion {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Albion_Archer.CloneTemplate();
                            SwitchWeapon(EActiveWeaponSlot.Distance);
                            playerClass = new ClassScout();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Albion_Caster.CloneTemplate();
                            playerClass = new ClassTheurgist();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Albion_Fighter.CloneTemplate();
                            playerClass = new ClassArmsman();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Albion_Healer.CloneTemplate();
                            playerClass = new ClassCleric();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Albion_Stealther.CloneTemplate();
                            playerClass = new ClassInfiltrator();
                            break;
                    }
                    break;
                case 1: // Hibernia
                    Name = $"Hibernia {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Hibernia_Archer.CloneTemplate();
                            SwitchWeapon(EActiveWeaponSlot.Distance);
                            playerClass = new ClassRanger();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Hibernia_Caster.CloneTemplate();
                            playerClass = new ClassEldritch();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Hibernia_Fighter.CloneTemplate();
                            playerClass = new ClassArmsman();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Hibernia_Healer.CloneTemplate();
                            playerClass = new ClassDruid();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Hibernia_Stealther.CloneTemplate();
                            playerClass = new ClassNightshade();
                            break;
                    }
                    break;
                case 2: // Midgard
                    Name = $"Midgard {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Midgard_Archer.CloneTemplate();
                            SwitchWeapon(EActiveWeaponSlot.Distance);
                            playerClass = new ClassHunter();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Midgard_Caster.CloneTemplate();
                            playerClass = new ClassRunemaster();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Midgard_Fighter.CloneTemplate();
                            playerClass = new ClassWarrior();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Midgard_Healer.CloneTemplate();
                            playerClass = new ClassHealer();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Midgard_Stealther.CloneTemplate();
                            playerClass = new ClassShadowblade();
                            break;
                    }
                    break;
            }

            var possibleRaces = playerClass.EligibleRaces;
            var indexPick = Util.Random(0, possibleRaces.Count - 1);
            Model = (ushort)possibleRaces[indexPick].GetModel(Gender);

            bool distance = Inventory.GetItem(EInventorySlot.DistanceWeapon) != null;
            bool standard = Inventory.GetItem(EInventorySlot.RightHandWeapon) != null;
            bool twoHanded = Inventory.GetItem(EInventorySlot.TwoHandWeapon) != null;

            if (distance)
                SwitchWeapon(EActiveWeaponSlot.Distance);
            else if (standard && twoHanded)
            {
                if (Util.Random(1) < 1)
                    SwitchWeapon(EActiveWeaponSlot.Standard);
                else
                    SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            }
            else if (twoHanded)
                SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            else
                SwitchWeapon(EActiveWeaponSlot.Standard);
            
        }
    }
}
