using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class CryptLord : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 200000; }
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

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

        public override void WalkToSpawn()
        {
            if (CurrentRegionID == 60) //if region is caer sidi
            {
                if (IsAlive)
                    return;
            }
            base.WalkToSpawn();
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159518);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CryptLordBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
        }

        public static bool BafMobs = false;
        public static bool point1check = false;
        public static bool point2check = false;
        public static bool point3check = false;
        public static bool point4check = false;
        public static bool walkback = false;

        public void LordPath()
        {
            Point3D point1 = new Point3D();
            point1.X = 28461;
            point1.Y = 40166;
            point1.Z = 15373;
            Point3D point2 = new Point3D();
            point2.X = 28494;
            point2.Y = 43144;
            point2.Z = 15373;
            Point3D point3 = new Point3D();
            point3.X = 26751;
            point3.Y = 43111;
            point3.Z = 15373;
            Point3D point4 = new Point3D();
            point4.X = 26741;
            point4.Y = 40147;
            point4.Z = 15373;
            Point3D spawn = new Point3D();
            spawn.X = 24891;
            spawn.Y = 40139;
            spawn.Z = 15372;

            if (!Body.InCombat && !HasAggro)
            {
                if (!Body.IsWithinRadius(point1, 30) && point1check == false)
                {
                    Body.WalkTo(point1, 100);
                }
                else
                {
                    point1check = true;
                    walkback = false;
                    if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
                    {
                        Body.WalkTo(point2, 100);
                    }
                    else
                    {
                        point2check = true;
                        if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
                            point3check == false)
                        {
                            Body.WalkTo(point3, 100);
                        }
                        else
                        {
                            point3check = true;
                            if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
                                point3check == true && point4check == false)
                            {
                                Body.WalkTo(point4, 100);
                            }
                            else
                            {
                                point4check = true;
                                if (!Body.IsWithinRadius(spawn, 30) && point1check == true && point2check == true &&
                                    point3check == true && point4check == true && walkback == false)
                                {
                                    Body.WalkTo(spawn, 100);
                                }
                                else
                                {
                                    walkback = true;
                                    point1check = false;
                                    point2check = false;
                                    point3check = false;
                                    point4check = false;
                                }
                            }
                        }
                    }
                }
            }
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
                                npc.MaxDistance = 10000; //set mob distance to make it reach target
                                npc.TetherRange = 10000; //set tether to not return to home
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
                                npc.MaxDistance = npc.NPCTemplate.MaxDistance; //return distance to normal
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
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf" && AggroTable.Count > 0 && npc.Brain is StandardMobBrain brain)
                        {
                            if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                                brain.AddToAggroList(target, 10);
                        }
                            //AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                    }
                }
            }

            SetMobstats(); //setting mob distance+tether+speed
            LordPath(); //boss path
            BafMobAggro(); //if npc with set packageid aggro near boss, then boss will aggro + his friends
            base.Think();
        }
    }
}