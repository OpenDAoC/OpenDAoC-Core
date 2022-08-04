using System;
using System.Linq;
using System.Threading.Tasks;
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
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Die(GameObject killer) //on kill generate orbs
        {
            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is FornShardBrain)
                    npc.RemoveFromWorld();
            }
            BroadcastMessage(String.Format("The frosty glows in {0}'s eyes abruptly blinks out. {0}'s form slowly fades into the ice. The shard swiftly evaporate leaving no trace of their corporeal existence behind!", Name));
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds


            FornfrusenenBrain sbrain = new FornfrusenenBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }
        #region Show Effects
        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                Parallel.ForEach(GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
                {
                    if (player == null) return;
                    player.Out.SendSpellEffectAnimation(this, this, 6160, 0, false, 0x01);//left hand glow
                    player.Out.SendSpellEffectAnimation(this, this, 6161, 0, false, 0x01);//right hand glow
                });

                return 3000;
            }
            return 0;
        }
        
        #endregion

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
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool SpamMessage = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if(ad != null && ad.Attacker != null && ad.Attacker.IsAlive && !SpamMessage)
            {
                BroadcastMessage(String.Format("{0} awakens from its peaceful slumber and emerges from this ice walls and hisses \"I know your name {1}, take a good look at your surroundings! Within this ice is where you'll be entombed for all eternity! Hahahahaha\"", Body.Name, ad.Attacker.Name));
                SpamMessage = true;            
            }
            base.OnAttackedByEnemy(ad);
        }
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                FornInCombat = false;
                SpamMessage = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc != null && npc.IsAlive)
                        {
                            if (npc.Brain is FornShardBrain)
                                npc.RemoveFromWorld(); //remove adds here
                        }
                    }
                    RemoveAdds = true;
                }
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
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
            for (int i = 0; i < Util.Random(2, 3); i++)
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
#region Forn Shards
namespace DOL.GS
{
    public class FornfrusenenShard : GameNPC
    {
        public FornfrusenenShard() : base()
        {
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            Point3D point = new Point3D(49617, 32874, 10859);
            if (source is GamePlayer || source is GamePet)
            {
                if (!source.IsWithinRadius(point, 400)) //take no damage
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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 80;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
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
            get { return 50000; }
        }

        public override void Die(GameObject killer)
        {
            foreach (GameNPC boss in GetNPCsInRadius(3000))
            {
                if (boss != null && boss.IsAlive && boss.Brain is FornfrusenenBrain)
                {
                    if (boss.HealthPercent <= 100 && boss.HealthPercent > 35) //dont dmg boss if is less than 35%
                        boss.Health -= boss.MaxHealth / 4; //deal dmg to boss if this is killed
                }
            }
            base.Die(killer);
        }
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
        public override short Strength { get => base.Strength; set => base.Strength = 120; }
        #endregion
        public override bool AddToWorld()
        {
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            Name = "Fornfrusenen Shard";
            Level = 75;
            Model = 920;
            Realm = 0;
            Size = (byte) Util.Random(30, 40);
            MeleeDamageType = eDamageType.Cold;

            RespawnInterval = -1;
            MaxSpeedBase = 200; 

            FornShardBrain sbrain = new FornShardBrain();
            SetOwnBrain(sbrain);
            base.AddToWorld();
            return true;
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (Util.Chance(20))
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                    CastSpell(FornShardDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
        }
        private Spell m_FornShardDD;
        public Spell FornShardDD
        {
            get
            {
                if (m_FornShardDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Power = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 14323;
                    spell.Icon = 11266;
                    spell.Damage = 300;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.Name = "Frost Shock";
                    spell.Range = 500;
                    spell.Radius = 300;
                    spell.SpellID = 11924;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_FornShardDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FornShardDD);
                }
                return m_FornShardDD;
            }
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
            AggroLevel = 100; 
            AggroRange = 800;
            ThinkInterval = 1000;
        }
        public override void Think()
        {
            if (HasAggro && Body.TargetObject != null)
            {
                Point3D point = new Point3D(49617, 32874, 10859);
                GameLiving target = Body.TargetObject as GameLiving;
                if (target != null && target.IsAlive)
                {
                    if (!target.IsWithinRadius(point, 400) && !Body.IsWithinRadius(point, 400))
                        Body.MaxSpeedBase = 0;
                    if (target.IsWithinRadius(point, 400))
                        Body.MaxSpeedBase = 200;
                }
            }
            base.Think();
        }
    }
}
#endregion