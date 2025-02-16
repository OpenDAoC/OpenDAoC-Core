﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System.Reflection;
using DOL.AI;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS
{
    public class Doppelganger : GameSummoner
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

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
           DbNpcTemplate chthonian = DOLDB<DbNpcTemplate>.SelectObject(DB.Column("Name").IsEqualTo("chthonian crawler"));
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
                    Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
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
            if (ActiveWeaponSlot == eActiveWeaponSlot.Distance)
            {
                bool standard = Inventory.GetItem(eInventorySlot.RightHandWeapon) != null;
                bool twoHanded = Inventory.GetItem(eInventorySlot.TwoHandWeapon) != null;

                if (standard && twoHanded)
                {
                    if (Util.Random(1) < 1)
                        SwitchWeapon(eActiveWeaponSlot.Standard);
                    else
                        SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                }
                else if (twoHanded)
                    SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                else
                    SwitchWeapon(eActiveWeaponSlot.Standard);
            }

            base.StartAttack(target);
        }

        /// <summary>
        /// Disguise the doppelganger as an invader
        /// </summary>
        protected void Disguise()
        {
            if (Util.Chance(50))
                Gender = eGender.Male;
            else
                Gender = eGender.Female;

            ICharacterClass characterClass = new DefaultCharacterClass();

            switch (Util.Random(2))
            {
                case 0: // Albion
                    Name = $"Albion {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Albion_Archer.CloneTemplate();
                            SwitchWeapon(eActiveWeaponSlot.Distance);
                            characterClass = new ClassScout();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Albion_Caster.CloneTemplate();
                            characterClass = new ClassTheurgist();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Albion_Fighter.CloneTemplate();
                            characterClass = new ClassArmsman();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Albion_Healer.CloneTemplate();
                            characterClass = new ClassCleric();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Albion_Stealther.CloneTemplate();
                            characterClass = new ClassInfiltrator();
                            break;
                    }
                    break;
                case 1: // Hibernia
                    Name = $"Hibernia {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Hibernia_Archer.CloneTemplate();
                            SwitchWeapon(eActiveWeaponSlot.Distance);
                            characterClass = new ClassRanger();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Hibernia_Caster.CloneTemplate();
                            characterClass = new ClassEldritch();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Hibernia_Fighter.CloneTemplate();
                            characterClass = new ClassArmsman();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Hibernia_Healer.CloneTemplate();
                            characterClass = new ClassDruid();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Hibernia_Stealther.CloneTemplate();
                            characterClass = new ClassNightshade();
                            break;
                    }
                    break;
                case 2: // Midgard
                    Name = $"Midgard {LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GamePlayer.RealmTitle.Invader")}";

                    switch (Util.Random(4))
                    {
                        case 0: // Archer
                            Inventory = ClothingMgr.Midgard_Archer.CloneTemplate();
                            SwitchWeapon(eActiveWeaponSlot.Distance);
                            characterClass = new ClassHunter();
                            break;
                        case 1: // Caster
                            Inventory = ClothingMgr.Midgard_Caster.CloneTemplate();
                            characterClass = new ClassRunemaster();
                            break;
                        case 2: // Fighter
                            Inventory = ClothingMgr.Midgard_Fighter.CloneTemplate();
                            characterClass = new ClassWarrior();
                            break;
                        case 3: // GuardHealer
                            Inventory = ClothingMgr.Midgard_Healer.CloneTemplate();
                            characterClass = new ClassHealer();
                            break;
                        case 4: // Stealther
                            Inventory = ClothingMgr.Midgard_Stealther.CloneTemplate();
                            characterClass = new ClassShadowblade();
                            break;
                    }
                    break;
            }

            var possibleRaces = characterClass.EligibleRaces;
            var indexPick = Util.Random(0, possibleRaces.Count - 1);
            Model = (ushort)possibleRaces[indexPick].GetModel(Gender);

            bool distance = Inventory.GetItem(eInventorySlot.DistanceWeapon) != null;
            bool standard = Inventory.GetItem(eInventorySlot.RightHandWeapon) != null;
            bool twoHanded = Inventory.GetItem(eInventorySlot.TwoHandWeapon) != null;

            if (distance)
                SwitchWeapon(eActiveWeaponSlot.Distance);
            else if (standard && twoHanded)
            {
                if (Util.Random(1) < 1)
                    SwitchWeapon(eActiveWeaponSlot.Standard);
                else
                    SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            }
            else if (twoHanded)
                SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            else
                SwitchWeapon(eActiveWeaponSlot.Standard);
            
        }
    }
}
