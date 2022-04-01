using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class LichLordIlron : GameEpicBoss
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
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == "CCImmunity")
                return true;

            return base.HasAbility(keyName);
        }
        public override bool AddToWorld()
        {
            Level = 79;
            Gender = eGender.Neutral;
            BodyType = 11; // undead
            MaxDistance = 1500;
            TetherRange = 2000;
            RoamingRange = 400;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163266);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            LichLordIlronBrain sBrain = new LichLordIlronBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            LichLordIlronBrain.spawnimages = true;
            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);

            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc.Brain is IlronImagesBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Lich Lord Ilron NPC Initializing...");
        }
    }
}

namespace DOL.AI.Brain
{
    public class LichLordIlronBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool spawnimages = true;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                spawnimages = true;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is IlronImagesBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }

            base.Think();
        }


        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (spawnimages)
            {
                Spawn(); // spawn images
                spawnimages = false; // check to avoid spawning adds multiple times

                foreach (GameNPC mob_c in Body.GetNPCsInRadius(2000, false))
                {
                    if (mob_c?.Brain is IlronImagesBrain && mob_c.IsAlive && mob_c.IsAvailable)
                    {
                        AddAggroListTo(mob_c.Brain as StandardMobBrain);
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }

        public void Spawn()
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is IlronImagesBrain)
                {
                    return;
                }
            }

            for (int i = 0; i < 4; i++) // Spawn 5 images
            {
                IlronImages Add = new IlronImages();
                Add.X = Body.X + Util.Random(-100, 100);
                Add.Y = Body.Y + Util.Random(-100, 100);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.IsWorthReward = false;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }

            spawnimages = false;
        }
    }
}

namespace DOL.GS
{
    public class IlronImages : GameNPC
    {
        public override int MaxHealth
        {
            get { return 1000; }
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 25; // dmg reduction for melee dmg
                case eDamageType.Crush: return 25; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 700;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public override bool AddToWorld()
        {
            Model = 441;
            Name = "Lich Lord Ilron";
            Size = 130;
            Level = 70;
            RoamingRange = 350;
            RespawnInterval = -1;
            MaxDistance = 1500;
            TetherRange = 2000;
            IsWorthReward = false; // worth no reward
            Flags ^= eFlags.GHOST;
            Realm = eRealm.None;
            IlronImagesBrain adds = new IlronImagesBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }

        public override void DropLoot(GameObject killer) //no loot
        {
        }

        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }
    }
}

namespace DOL.AI.Brain
{
    public class IlronImagesBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public IlronImagesBrain()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }

        #region pbaoe mezz

        /// <summary>
        /// The Bomb spell. Override this property in your Aros Epic summonedGuard implementation
        /// and assign the spell to m_breathSpell.
        /// </summary>
        ///
        /// 
        protected Spell m_mezSpell;

        /// <summary>
        /// The Bomb spell.
        /// </summary>
        protected Spell Mezz
        {
            get
            {
                if (m_mezSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 1681;
                    spell.Icon = 1685;
                    spell.Damage = 0;
                    spell.Name = "Mesmerized";
                    spell.Range = 1500;
                    spell.Radius = 300;
                    spell.SpellID = 99999;
                    spell.Duration = 30;
                    spell.Target = "Enemy";
                    spell.Type = "Mesmerize";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Spirit; //Spirit DMG Type
                    m_mezSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
                }

                return m_mezSpell;
            }
        }

        #endregion

        public override void Think()
        {
            if (Util.Chance(3))
            {
                Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.Think();
        }
    }
}