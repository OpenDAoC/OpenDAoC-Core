using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.GS
{
    public class CryptLord : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const short PATROL_SPEED = 100;

        // Patrol route, starting at the spawn point.
        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (24891, 40139, 15372),
            (28461, 40166, 15373),
            (28494, 43144, 15373),
            (26751, 43111, 15373),
            (26741, 40147, 15373)
        ];

        public CryptLord()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public virtual int COifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }



        public override int MaxHealth
        {
            get { return 100000; }
        }

        public override int MeleeAttackRange => 350;

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159518);
            LoadTemplate(npcTemplate);
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(64);
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            CryptLordBrain adds = new CryptLordBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        //public override bool IsVisibleToPlayers => true;
    }
}

namespace DOL.AI.Brain
{
    public class CryptLordBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CryptLordBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
        }

        public void BafMobAggro() //if baf mob aggro and boss is near it will pull boss+ rest of mobs
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "CryptLordBaf")
                {
                    if (npc.InCombat && npc.TargetObject != null)
                    {
                        GameLiving target = npc.TargetObject as GameLiving;
                        if (Body.IsAlive && target != null && target.IsAlive)
                        {
                            if (npc.IsWithinRadius(Body, 800)) //the range that mob will bring Boss and rest mobs
                                AddToAggroList(target, 100);
                        }
                    }
                }
            }
        }

        public void SetMobstats()
        {
            if (Body.TargetObject != null && HasAggro) //if in combat
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(10000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf")
                        {
                            if (npc.TargetObject == Body.TargetObject && npc.NPCTemplate != null)//check if npc got NpcTemplate!
                            {
                                npc.TetherRange = 0; //set tether to not return to home
                                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                                    npc.MaxSpeedBase = 300; //speed is is not near to reach target faster
                                else
                                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                            }
                        }
                    }
                }
            }
            else //if not in combat
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(10000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf" && npc.NPCTemplate != null)//check if npc got NpcTemplate!
                        {
                            if (!HasAggro)
                            {
                                npc.TetherRange = npc.NPCTemplate.TetherRange; //return tether to normal
                                npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                            }
                        }
                    }
                }
            }
        }

        public override void Think()
        {
            if(Body.IsMoving)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            AddToAggroList(player, 10);//aggro players if roaming
                        }
                    }
                   /* if(player == null || !player.IsAlive || player.Client.Account.PrivLevel != 1)
                    {
                        if(AggroTable.Count>0)
                        {
                            ClearAggroList();//clear list if it contain any aggroed players
                        }
                    }*/
                }
            }
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.TargetObject != null) //bring mobs from rooms if mobs got set PackageID="CryptLordBaf"
            {
                GameLiving target = Body.TargetObject as GameLiving;
                foreach (GameNPC npc in Body.GetNPCsInRadius(10000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf" && HasAggro && npc.Brain is StandardMobBrain brain)
                        {
                            if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
                        }
                            //AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                    }
                }
            }

            SetMobstats(); //setting mob distance+tether+speed
            BafMobAggro(); //if npc with set packageid aggro near boss, then boss will aggro + his friends
            base.Think();
        }
    }
}
