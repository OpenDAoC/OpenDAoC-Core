using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class GiantSporiteCluster : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GiantSporiteCluster()
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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override int AttackRange
        {
            get { return 250; }
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
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            int damageDealt = damageAmount + criticalAmount;
            foreach (GameNPC copy in GetNPCsInRadius(10000))
            {
                if (copy != null)
                {
                    if (copy is GSCAdds && copy.IsAlive)
                    {
                        copy.Health = Health;
                    }
                }
            }
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC copy in GetNPCsInRadius(10000))
            {
                if (copy != null)
                {
                    if (copy.IsAlive && copy is GSCAdds)
                    {
                        copy.RemoveFromWorld();
                    }
                }
            }
            base.Die(killer);
        }
        public void Spawn()
        {
            foreach (GameNPC npc in GetNPCsInRadius(4000))
            {
                if (npc.Brain is GSCAddsBrain)
                {
                    return;
                }
            }
            for (int i = 0; i < 7; i++)
            {
                GSCAdds Add = new GSCAdds();
                Add.X = X + Util.Random(-50, 80);
                Add.Y = Y + Util.Random(-50, 80);
                Add.Z = Z;
                Add.CurrentRegion = CurrentRegion;
                Add.Heading = Heading;
                Add.AddToWorld();
            }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161336);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            Spawn();

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            GiantSporiteClusterBrain sBrain = new GiantSporiteClusterBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}

public class GiantSporiteClusterBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GiantSporiteClusterBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
    }

    public override void Think()
    {
        if (!HasAggressionTable())
        {
            //set state to RETURN TO SPAWN
            FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            if (Util.Chance(5) && Body.TargetObject != null)
            {
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastAOEDD), 3000);
            }
            foreach (GameNPC copy in Body.GetNPCsInRadius(5000))
            {
                if (copy != null)
                {
                    if (copy.IsAlive && copy.Brain is GSCAddsBrain brain)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.Think();
    }
    public int CastAOEDD(ECSGameTimer timer)
    {
        Body.CastSpell(GSCAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        return 0;
    }

    private Spell m_GSCAoe;
    private Spell GSCAoe
    {
        get
        {
            if (m_GSCAoe == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 25;
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.Damage = 200;
                spell.Name = "Xaga Staff Bomb";
                spell.TooltipId = 4568;
                spell.Radius = 200;
                spell.Range = 600;
                spell.SpellID = 11709;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) eDamageType.Cold;
                m_GSCAoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GSCAoe);
            }

            return m_GSCAoe;
        }
    }
}
namespace DOL.GS
{
    public class GSCAdds : GameEpicBoss
    {
        public GSCAdds() : base()
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
            get { return 250; }
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
            return 300;
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            int damageDealt = damageAmount + criticalAmount;
            foreach (GameNPC copy in GetNPCsInRadius(10000))
            {
                if (copy != null)
                {
                    if ((copy is GSCAdds || copy is GiantSporiteCluster) && this != copy && copy.IsAlive)
                    {
                        copy.Health = Health;
                    }
                }
            }
        }
        public override void Die(GameObject killer)
        {
            foreach(GameNPC copy in GetNPCsInRadius(10000))
            {
                if (copy != null)
                {
                    if (this != copy && copy is GSCAdds && copy.IsAlive)
                    {
                        copy.RemoveFromWorld();
                    }
                }
            }
            foreach (GameNPC boss in GetNPCsInRadius(10000))
            {
                if (boss != null)
                {
                    if (this != boss && boss is GiantSporiteCluster && boss.IsAlive)
                    {
                        boss.Die(boss);
                    }
                }
            }
            base.Die(killer);
        }
        public override short Strength { get => base.Strength; set => base.Strength = 50; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
        public override bool AddToWorld()
        {
            Model = 906;
            Name = "Giant Sporite Cluster";
            RespawnInterval = -1;
            MaxDistance = 0;
            TetherRange = 0;
            Size = (byte)Util.Random(45, 55);
            Level = 79;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BodyType = 8;
            Realm = eRealm.None;
            GSCAddsBrain adds = new GSCAddsBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class GSCAddsBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GSCAddsBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
        }
        public override void Think()
        {
            if(!HasAggressionTable())
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            if(HasAggro && Body.TargetObject != null)
            {
                foreach(GameNPC copy in Body.GetNPCsInRadius(5000))
                {
                    if(copy != null)
                    {
                        if(copy.IsAlive && Body != copy && copy.Brain is GSCAddsBrain brain)
                        {
                            GameLiving target = Body.TargetObject as GameLiving;
                            if (!brain.HasAggro)
                                brain.AddToAggroList(target, 10);
                        }
                    }
                }
                foreach (GameNPC boss in Body.GetNPCsInRadius(5000))
                {
                    if (boss != null)
                    {
                        if (boss.IsAlive && boss.Brain is GiantSporiteClusterBrain brain1)
                        {
                            GameLiving target = Body.TargetObject as GameLiving;
                            if (!brain1.HasAggro)
                                brain1.AddToAggroList(target, 10);
                        }
                    }
                }
            }
            base.Think();
        }
    }
}