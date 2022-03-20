using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class CryptLord : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CryptLord()
            : base()
        {
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
            get
            {
                return 20000;
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
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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

        public override void WalkToSpawn()
        {
            if (this.CurrentRegionID == 60)//if region is caer sidi
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
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            CryptLordBrain adds = new CryptLordBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Crypt Lord", 60, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Crypt Lord  not found, creating it...");

                log.Warn("Initializing Crypt Lord...");
                CryptLord CO = new CryptLord();
                CO.Name = "Crypt Lord";
                CO.Model = 927;
                CO.Realm = 0;
                CO.Level = 81;
                CO.Size = 150;
                CO.CurrentRegionID = 60;//caer sidi

                CO.Strength = 500;
                CO.Intelligence = 220;
                CO.Piety = 220;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

                CO.X = 24906;
                CO.Y = 40138;
                CO.Z = 15372;
                CO.MaxDistance = 4500;
                CO.TetherRange = 4700;
                CO.MaxSpeedBase = 300;
                CO.Heading = 3035;

                CryptLordBrain ubrain = new CryptLordBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 400;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Crypt Lord exist ingame, remove it and restart server if you want to add by script code.");
        }

        public override void Die(GameObject killer)//on kill generate orbs
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);//5k orbs for every player in group
                }
            }
            
            base.Die(killer);
        }

    }
}
namespace DOL.AI.Brain
{
    public class CryptLordBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
            point1.X = 28461; point1.Y = 40166; point1.Z = 15373;
            Point3D point2 = new Point3D();
            point2.X = 28494; point2.Y = 43144; point2.Z = 15373;
            Point3D point3 = new Point3D();
            point3.X = 26751; point3.Y = 43111; point3.Z = 15373;
            Point3D point4 = new Point3D();
            point4.X = 26741; point4.Y = 40147; point4.Z = 15373;
            Point3D spawn = new Point3D();
            spawn.X = 24891; spawn.Y = 40139; spawn.Z = 15372;

            if (!Body.InCombat && !HasAggro)
            {
                if (Body.CurrentRegionID == 60)//caer sidi
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
                            if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true && point3check == false)
                            {
                                Body.WalkTo(point3, 100);
                            }
                            else
                            {
                                point3check = true;
                                if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true && point3check == true && point4check == false)
                                {
                                    Body.WalkTo(point4, 100);
                                }
                                else
                                {
                                    point4check = true;
                                    if (!Body.IsWithinRadius(spawn, 30) && point1check == true && point2check == true && point3check == true && point4check == true && walkback == false)
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
                else//not sidi
                {
                    //mob will not roam
                }
            }
        }

        public void BafMobAggro()//if baf mob aggro and boss is near it will pull boss+ rest of mobs
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if(npc != null)
                {
                    if(npc.IsAlive)
                    {
                        if(npc.PackageID=="CryptLordBaf")
                        {
                            if(npc.InCombat && npc.TargetObject != null)
                            {
                                GameLiving target = npc.TargetObject as GameLiving;
                                if (Body.IsAlive)
                                {
                                    if (npc.IsWithinRadius(Body, 800))//the range that mob will bring Boss and rest mobs
                                    {
                                        AddToAggroList(target, 100);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void SetMobstats()
        {
            if(Body.TargetObject != null && (Body.InCombat || HasAggro || Body.AttackState == true))//if in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf")
                        {
                            if (BafMobs == true && npc.TargetObject == Body.TargetObject)
                            {
                                npc.MaxDistance = 10000;//set mob distance to make it reach target
                                npc.TetherRange = 10000;//set tether to not return to home
                                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                                {
                                    npc.MaxSpeedBase = 300;//speed is is not near to reach target faster
                                }
                                else
                                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed;//return speed to normal
                            }
                        }
                    }
                }
            }
            else//if not in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CryptLordBaf")
                        {
                            if (BafMobs == false)
                            {
                                npc.MaxDistance = npc.NPCTemplate.MaxDistance;//return distance to normal
                                npc.TetherRange = npc.NPCTemplate.TetherRange;//return tether to normal
                                npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed;//return speed to normal
                            }
                        }
                    }
                }
            }
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                BafMobs = false;
                this.Body.Health = this.Body.MaxHealth;;
            }
            if (Body.IsOutOfTetherRange)
            {              
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if(Body.InCombat || HasAggro || Body.AttackState == true)//bring mobs from rooms if mobs got set PackageID="CryptLordBaf"
            {
                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "CryptLordBaf")
                            {
                                AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with CryptLordBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
            }
            SetMobstats();//setting mob distance+tether+speed
            LordPath();//boss path
            BafMobAggro();//if npc with set packageid aggro near boss, then boss will aggro + his friends
            base.Think();
        }
    }
}