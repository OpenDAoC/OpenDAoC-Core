using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Fornfrusenen : GameEpicBoss
    {
        public Fornfrusenen() : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 800;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }

        public override int MaxHealth
        {
            get { return 20000; }
        }

        public override void Die(GameObject killer) //on kill generate orbs
        {
            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is FornShardBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }

            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161047);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            MaxSpeedBase = 0;
            RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            AbilityBonus[(int) eProperty.Resist_Body] = 15;
            AbilityBonus[(int) eProperty.Resist_Heat] = 15;
            AbilityBonus[(int) eProperty.Resist_Cold] = 15;
            AbilityBonus[(int) eProperty.Resist_Matter] = 15;
            AbilityBonus[(int) eProperty.Resist_Energy] = 15;
            AbilityBonus[(int) eProperty.Resist_Spirit] = 15;
            AbilityBonus[(int) eProperty.Resist_Slash] = 25;
            AbilityBonus[(int) eProperty.Resist_Crush] = 25;
            AbilityBonus[(int) eProperty.Resist_Thrust] = 25;

            FornfrusenenBrain sbrain = new FornfrusenenBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Fornfrusenen", 160, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Fornfrusenen  not found, creating it...");

                log.Warn("Initializing Fornfrusenen ...");
                Fornfrusenen TG = new Fornfrusenen();
                TG.Name = "Fornfrusenen";
                TG.Model = 920;
                TG.Realm = 0;
                TG.Level = 75;
                TG.Size = 60;
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

                TG.MaxSpeedBase = 0; //boss does not move
                TG.X = 54583;
                TG.Y = 37745;
                TG.Z = 11435;
                FornfrusenenBrain ubrain = new FornfrusenenBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Fornfrusenen exist ingame, remove it and restart server if you want to add by script code.");
        }

        //boss does not move so he will not take damage if enemys hit him from far away
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (!source.IsWithinRadius(this, 200)) //take no damage
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else //take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class FornfrusenenBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public FornfrusenenBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                FornInCombat = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is FornShardBrain)
                            {
                                npc.RemoveFromWorld(); //remove adds here
                            }
                        }
                    }
                }
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

            if (Body.InCombat && HasAggro)
            {
                if (FornInCombat == false)
                {
                    SpawnShards(); //spawn adds here
                    FornInCombat = true;
                }
            }

            base.Think();
        }

        public static bool FornInCombat = false;

        public void SpawnShards()
        {
            for (int i = 0; i < Util.Random(6, 10); i++)
            {
                FornfrusenenShard Add = new FornfrusenenShard();
                Add.X = Body.X + Util.Random(-100, 100);
                Add.Y = Body.Y + Util.Random(-100, 100);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }
    }
}

////////////////////////////////////////////Shards-adds///////////////////////////////
namespace DOL.GS
{
    public class FornfrusenenShard : GameNPC
    {
        public FornfrusenenShard() : base()
        {
        }

        public static GameNPC Boss = null;

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsByNameFromRegion("Fornfrusenen", 160, eRealm.None))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is FornfrusenenBrain)
                            {
                                Boss = npc; //pick boss here
                            }
                        }
                    }
                }

                if (!source.IsWithinRadius(Boss, 200)) //take no damage if is out of boss
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else //take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 60;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override void Die(GameObject killer)
        {
            foreach (GameNPC boss in GetNPCsInRadius(3000))
            {
                if (boss != null)
                {
                    if (boss.IsAlive)
                    {
                        if (boss.Brain is FornfrusenenBrain)
                        {
                            if (boss.HealthPercent <= 100 &&
                                boss.HealthPercent > 35) //dont dmg boss if is less than 35%
                            {
                                boss.Health -= boss.MaxHealth / 10; //deal dmg to boss if this is killed
                            }
                        }
                    }
                }
            }

            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            Name = "Fornfrusenen Shard";
            Level = 75;
            Model = 126;
            Realm = 0;
            Size = (byte) Util.Random(20, 30);
            MeleeDamageType = eDamageType.Cold;

            AbilityBonus[(int) eProperty.Resist_Body] = 15;
            AbilityBonus[(int) eProperty.Resist_Heat] = 15;
            AbilityBonus[(int) eProperty.Resist_Cold] = 15;
            AbilityBonus[(int) eProperty.Resist_Matter] = 15;
            AbilityBonus[(int) eProperty.Resist_Energy] = 15;
            AbilityBonus[(int) eProperty.Resist_Spirit] = 15;
            AbilityBonus[(int) eProperty.Resist_Slash] = 25;
            AbilityBonus[(int) eProperty.Resist_Crush] = 25;
            AbilityBonus[(int) eProperty.Resist_Thrust] = 25;

            Strength = 70;
            Quickness = 125;
            Constitution = 100;
            Dexterity = 200;
            RespawnInterval = -1;
            MaxSpeedBase = 120; //very slow

            FornShardBrain sbrain = new FornShardBrain();
            SetOwnBrain(sbrain);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class FornShardBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public FornShardBrain()
            : base()
        {
            AggroLevel = 0; //neutral
            AggroRange = 0;
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            base.Think();
        }
    }
}