using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

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
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        

        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override short MaxSpeedBase => (short) (191 + Level * 2);
        public override int MeleeAttackRange => 180;
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165038);
            LoadTemplate(npcTemplate);
            TetherRange = 2000;
            RoamingRange = 400;
            PrincessNahemahBrain sBrain = new PrincessNahemahBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            PrincessNahemahBrain.spawnMinions = true;
            // demon
            BodyType = 2;


            // Custom Respawn +/- 20% 4h
            int baseRespawnMS = 14400000; 
            int maxOffsetMS = 2880000; 
            Random rnd = new Random();
            int randomOffset = rnd.Next(maxOffsetMS * 2) - maxOffsetMS;
            RespawnInterval = baseRespawnMS + randomOffset;


            Faction = FactionMgr.GetFactionByID(191);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);

            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc.Brain is NahemahMinionBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
            bool canReportNews = true;
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("daemon_blood_seal");
            int itemCount = 50;
            string message_currency = "Princess Nahemah drops " + itemCount + " " + template.Name + ".";
            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                DbInventoryItem item = GameInventoryItem.Create(template);
                item.Count = itemCount;
                if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
                {
                    player.Out.SendMessage(message_currency, eChatType.CT_Loot, eChatLoc.CL_ChatWindow);
                    InventoryLogging.LogInventoryAction(player, player, eInventoryActionType.Other, template, itemCount);
                }
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (!canReportNews || GameServer.ServerRules.CanGenerateNews(player) != false) continue;
                if (player.Client.Account.PrivLevel == (int) ePrivLevel.Player)
                    canReportNews = false;
            }

            if (canReportNews)
            {
                ReportNews(killer);
            }
        }
        private void ReportNews(GameObject killer)
        {
            //int numPlayers = AwardHLKillPoint();
            String message = String.Format("{0} has been slain!", Name/*, numPlayers*/);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);
        }
        /*private int AwardHLKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                count++;
            }
            return count;
        }*/
    }
}

namespace DOL.AI.Brain
{
    public class PrincessNahemahBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool spawnMinions = true;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                spawnMinions = true;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc.Brain is NahemahMinionBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
                RemoveAdds = false;
            base.Think();
        }
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (spawnMinions)
            {
                Spawn(); // spawn minions
                spawnMinions = false; // check to avoid spawning adds multiple times

                foreach (GameNPC mob_c in Body.GetNPCsInRadius(2000))
                {
                    if (mob_c?.Brain is NahemahMinionBrain && mob_c.IsAlive && mob_c.IsAvailableToJoinFight)
                    {
                        AddAggroListTo(mob_c.Brain as NahemahMinionBrain);
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
            get { return 550; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162189);
            LoadTemplate(npcTemplate);
            RoamingRange = 350;
            RespawnInterval = -1;
            TetherRange = 2000;
            Realm = eRealm.None;
            NahemahMinionBrain adds = new NahemahMinionBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);

            base.AddToWorld();
            return true;
        }
        public override bool CanDropLoot => false;
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
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 310;
                    spell.Icon = 310;
                    spell.Damage = 170;
                    spell.Name = "Maelstrom";
                    spell.Range = 1500;
                    spell.Radius = 350;
                    spell.SpellID = 99998;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "DirectDamage";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_fireDDSpell = new Spell(spell, 50);
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public NahemahMinionBrain()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }
    }
}
