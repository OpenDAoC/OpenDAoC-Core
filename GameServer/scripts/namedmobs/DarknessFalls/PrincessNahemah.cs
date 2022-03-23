using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
    public class PrincessNahemah : GameEpicBoss
    {
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Princess Nahemah initialized..");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
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

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165038);
            LoadTemplate(npcTemplate);
            MaxDistance = 1500;
            TetherRange = 2000;
            RoamingRange = 400;
            PrincessNahemahBrain sBrain = new PrincessNahemahBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            PrincessNahemahBrain.spawnMinions = true;

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer)
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);
                }
            }

            base.Die(killer);

            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc.Brain is NahemahMinionBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class PrincessNahemahBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool spawnMinions = true;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                spawnMinions = true;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is NahemahMinionBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }

            base.Think();
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (spawnMinions)
            {
                Spawn(); // spawn minions
                spawnMinions = false; // check to avoid spawning adds multiple times

                foreach (GameNPC mob_c in Body.GetNPCsInRadius(2000, false))
                {
                    if (mob_c?.Brain is NahemahMinionBrain && mob_c.IsAlive && mob_c.IsAvailable)
                    {
                        AddAggroListTo(mob_c.Brain as StandardMobBrain);
                    }
                }
            }

            base.OnAttackedByEnemy(ad);
        }

        private void Spawn()
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is NahemahMinionBrain)
                {
                    return;
                }
            }

            var amount = Util.Random(5, 10);

            for (int i = 0; i < amount; i++) // Spawn x minions
            {
                NahemahMinion Add = new NahemahMinion();
                Add.X = Body.X + Util.Random(100, 350);
                Add.Y = Body.Y + Util.Random(100, 350);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.IsWorthReward = false;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }

            spawnMinions = false;
        }
    }
}

namespace DOL.GS
{
    public class NahemahMinion : GameNPC
    {
        public override int MaxHealth
        {
            get { return 450 * Constitution / 100; }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162189);
            LoadTemplate(npcTemplate);
            RoamingRange = 350;
            RespawnInterval = -1;
            MaxDistance = 1500;
            TetherRange = 2000;
            IsWorthReward = false; // worth no reward
            Realm = eRealm.None;
            NahemahMinionBrain adds = new NahemahMinionBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

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

        public override void OnAttackEnemy(AttackData ad)
        {
            if (Util.Chance(10))
            {
                CastSpell(FireDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            base.OnAttackEnemy(ad);
        }

        #region fire aoe dd

        /// <summary>
        /// The Bomb spell.
        /// and assign the spell to m_breathSpell.
        /// </summary>
        ///
        /// 
        protected Spell m_fireDDSpell;

        /// <summary>
        /// The Bomb spell.
        /// </summary>
        protected Spell FireDD
        {
            get
            {
                if (m_fireDDSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 310;
                    spell.Icon = 310;
                    spell.Damage = 170;
                    spell.Name = "Maelstrom";
                    spell.Range = 1500;
                    spell.Radius = 350;
                    spell.SpellID = 99998;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_fireDDSpell = new Spell(spell, 50);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_fireDDSpell);
                }

                return m_fireDDSpell;
            }
        }

        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class NahemahMinionBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public NahemahMinionBrain()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }
    }
}