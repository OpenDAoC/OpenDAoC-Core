using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.Spells
{


    //http://www.camelotherald.com/masterlevels/ma.php?ml=Convoker
    //no shared timer
    #region Convoker-1
    [SpellHandlerAttribute("SummonWood")]
    public class SummonWoodSpellHandler : SummonItemSpellHandler
    {
        public SummonWoodSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("heartwood_bounty_point_wooden_boards");
            if (template != null)
            {
                items.Add(GameInventoryItem.Create(template));
                foreach (DbInventoryItem item in items)
                {
                    if (item.IsStackable)
                    {
                        item.Count = 100;
                    }
                }
            }
        }
    }




    #endregion

    //no shared timer
    #region Convoker-2
    [SpellHandlerAttribute("PrescienceNode")]
    public class PrescienceNodeSpellHandler : FontSpellHandler
    {
        // constructor
        public PrescienceNodeSpellHandler(GameLiving caster, Spell spell, SpellLine line)
: base(caster, spell, line)
        {
            ApplyOnNPC = false;
            ApplyOnCombat = true;

            //Construct a new font.
            font = new GameFont();
            font.Model = 2584;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the font spell
            dbs = new DbSpell();
            dbs.SpellID = 999999;
            dbs.Name = spell.Name;
            dbs.Icon = 7312;
            dbs.ClientEffect = 7312;
            dbs.Damage = 0;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 1000;
            dbs.Type = eSpellType.Prescience.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = 2;
            dbs.Frequency = 0;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 1000;
            s = new Spell(dbs, 50);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            fontSpell = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    [SpellHandlerAttribute("Prescience")]
    public class PrescienceSpellHandler : SpellHandler
    {
        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return true;
        }

        public override bool HasPositiveEffect
        {
            get { return false; }
        }


        public PrescienceSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }





    #endregion

    //no shared timer
    #region Convoker-3
    [SpellHandlerAttribute("PowerTrap")]
    public class PowerTrapSpellHandler : MineSpellHandler
    {
        // constructor
        public PowerTrapSpellHandler(GameLiving caster, Spell spell, SpellLine line)
: base(caster, spell, line)
        {
            //Construct a new mine.
            mine = new GameMine();
            mine.Model = 2590;
            mine.Name = spell.Name;
            mine.Realm = caster.Realm;
            mine.X = caster.X;
            mine.Y = caster.Y;
            mine.Z = caster.Z;
            mine.CurrentRegionID = caster.CurrentRegionID;
            mine.Heading = caster.Heading;
            mine.Owner = (GamePlayer)caster;

            // Construct the mine spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7313;
            dbs.ClientEffect = 7313;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = eSpellType.PowerRend.ToString();
            dbs.Value = spell.Damage;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            trap = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }




    #endregion

    //no shared timer
    #region Convoker-4
    [SpellHandlerAttribute("SpeedWrapWard")]
    public class SpeedWrapWardSpellHandler : FontSpellHandler
    {
        // constructor
        public SpeedWrapWardSpellHandler(GameLiving caster, Spell spell, SpellLine line)
: base(caster, spell, line)
        {
            ApplyOnCombat = true;
            Friendly = false;

            //Construct a new mine.
            font = new GameFont();
            font.Model = 2586;
            font.Name = spell.Name;
            font.Realm = caster.Realm;
            font.X = caster.X;
            font.Y = caster.Y;
            font.Z = caster.Z;
            font.CurrentRegionID = caster.CurrentRegionID;
            font.Heading = caster.Heading;
            font.Owner = (GamePlayer)caster;

            // Construct the mine spell
            dbs = new DbSpell();
            dbs.SpellID = 99998;
            dbs.Name = spell.Name;
            dbs.Icon = 7237;
            dbs.ClientEffect = 7237;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = eSpellType.SpeedWrap.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 2000;
            dbs.SpellGroup = 9;
            s = new Spell(dbs, 50);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            fontSpell = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
    [SpellHandlerAttribute("SpeedWrap")]
    public class SpeedWrapSpellHandler : SpellHandler
    {
        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            new SpeedWarpECSGameEffect(initParams);
        }
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }
        public SpeedWrapSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    public class SpeedWarpECSGameEffect : ECSGameSpellEffect
    {
        public SpeedWarpECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        public override void OnStartEffect()
        {
            base.OnStartEffect();
            if (Owner.IsMezzed || Owner.IsStunned)
                return;

            if (Owner is GamePlayer player)
                player.Out.SendUpdateMaxSpeed();

            if (Owner is GameNPC npc)
            {
                short maxSpeed = npc.MaxSpeed;
                if (npc.CurrentSpeed > maxSpeed)
                    npc.CurrentSpeed = maxSpeed;
            }
        }
        public override void OnStopEffect()
        {
            if (Owner.IsMezzed || Owner.IsStunned)
                return;

            if (Owner is GamePlayer player)
                player.Out.SendUpdateMaxSpeed();

            if (Owner is GameNPC npc)
            {
                short maxSpeed = npc.MaxSpeed;
                if (npc.CurrentSpeed > maxSpeed)
                    npc.CurrentSpeed = maxSpeed;
            }
        }
    }




    #endregion

    //shared timer 1
    #region Convoker-5
    [SpellHandlerAttribute("SummonWarcrystal")]
    public class SummonWarcrystalSpellHandler : SummonItemSpellHandler
    {
        public SummonWarcrystalSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            string ammo = "";
            switch (Util.Random(1, 2))
            {
                case 1:
                    ammo = "mystic_ammo_heat";
                    break;
                case 2:
                    ammo = "mystic_ammo_cold";
                    break;
            }
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(ammo);
            if (template != null)
            {
                items.Add(GameInventoryItem.Create(template));
                foreach (DbInventoryItem item in items)
                {
                    if (item.IsStackable)
                    {
                        item.Count = 1;
                    }
                }

            }
        }
    }




    #endregion

    //shared timer 1
    #region Convoker-6
    [SpellHandlerAttribute("Battlewarder")]
    public class BattlewarderSpellHandler : SpellHandler
    {
        public GameNPC warder;
        BattleWarderECSGameEffect effect;

        public BattlewarderSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
    : base(caster, spell, spellLine)
        {
        }









        /// <summary>
        /// Execute battle warder summon spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }
        public override bool IsOverwritable(ECSGameSpellEffect compare)
        {
            return false;
        }


        public override void CreateECSPulseEffect(GameLiving target, double effectiveness)
        {
            int freq = Spell != null ? Spell.Frequency : 0;

            {
                if (warder == null)
                {
                    warder = new GameNPC();
                    //Fill the object variables
                    warder.CurrentRegion = Caster.CurrentRegion;
                    warder.Heading = (ushort)((Caster.Heading + 2048) % 4096);
                    warder.Level = 70;
                    warder.Realm = Caster.Realm;
                    warder.Name = "Battle Warder";
                    warder.Model = 993;
                    warder.CurrentSpeed = 0;
                    warder.MaxSpeedBase = 0;
                    warder.GuildName = "";
                    warder.Size = 50;
                }
                if (effect == null)
                {
                    effect = new BattleWarderECSGameEffect(target, this, CalculateEffectDuration(target, effectiveness), freq, effectiveness, Spell.Icon);
                }
            }
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!base.CheckBeginCast(selectedTarget)) return false;
            if (!(m_caster.GroundTarget != null && m_caster.GroundTargetInView))
            {
                MessageToCaster("Your area target is out of range.  Set a closer ground position.", eChatType.CT_SpellResisted);
                return false;
            }
            return true;

        }

        public override void FocusSpellAction(bool moving = false)
        {
            base.FocusSpellAction(moving);
            effect.OnStopEffect();
            effect.CancelEffect = true;
        }
    }
    public class BattleWarderECSGameEffect : ECSPulseEffect
    {
        public BattleWarderECSGameEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base(owner, handler, duration, pulseFreq, effectiveness, icon, cancelEffect)
        {
        }

        // Event : Battle warder has died
        private void BattleWarderDie(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC kWarder = sender as GameNPC;
            if (kWarder == null) return;
            if (e == GameLivingEvent.Dying)
            {
                ((GamePlayer)Owner).Out.SendMessage("Your Battle Warder has fallen!", eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                OnStopEffect();
            }
        }

        public override void OnStartEffect()
        {
            base.OnStartEffect();

            if (Owner == null || !Owner.IsAlive)
                return;

            if ((Owner is GamePlayer casterPlayer && SpellHandler is BattlewarderSpellHandler bwSpellHanlder))
            {
                if (casterPlayer.GroundTarget != null && casterPlayer.GroundTargetInView)
                {
                    GameEventMgr.AddHandler(bwSpellHanlder.warder, GameLivingEvent.Dying, new DOLEventHandler(BattleWarderDie));
                    bwSpellHanlder.warder.X = casterPlayer.GroundTarget.X;
                    bwSpellHanlder.warder.Y = casterPlayer.GroundTarget.Y;
                    bwSpellHanlder.warder.Z = casterPlayer.GroundTarget.Z;
                    bwSpellHanlder.warder.AddBrain(new MLBrain());
                    bwSpellHanlder.warder.AddToWorld();
                }
                else
                {
                    ((GamePlayer)Owner).Out.SendMessage("Your area target is out of range.  Set a closer ground position.", eChatType.CT_SpellExpires, eChatLoc.CL_SystemWindow);
                    OnStopEffect();
                }
            }
        }
        public override void OnStopEffect()
        {
            base.OnStopEffect();
            if (SpellHandler is BattlewarderSpellHandler bwSpellHanlder && bwSpellHanlder.warder != null)
            {
                GameEventMgr.RemoveHandler(bwSpellHanlder.warder, GameLivingEvent.Dying, new DOLEventHandler(BattleWarderDie));
                bwSpellHanlder.warder.RemoveBrain(bwSpellHanlder.warder.Brain);
                bwSpellHanlder.warder.Health = 0;
                bwSpellHanlder.warder.Delete();
            }
        }
    }




    #endregion

    //no shared timer
    #region Convoker-7
    [SpellHandlerAttribute("DissonanceTrap")]
    public class DissonanceTrapSpellHandler : MineSpellHandler
    {
        // constructor
        public DissonanceTrapSpellHandler(GameLiving caster, Spell spell, SpellLine line)
: base(caster, spell, line)
        {
            //Construct a new mine.
            mine = new GameMine();
            mine.Model = 2588;
            mine.Name = spell.Name;
            mine.Realm = caster.Realm;
            mine.X = caster.X;
            mine.Y = caster.Y;
            mine.Z = caster.Z;
            mine.CurrentRegionID = caster.CurrentRegionID;
            mine.Heading = caster.Heading;
            mine.Owner = (GamePlayer)caster;

            // Construct the mine spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7255;
            dbs.ClientEffect = 7255;
            dbs.Damage = -40;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = eSpellType.DirectDamage.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            trap = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }




    #endregion

    //no shared timer
    #region Convoker-8
    [SpellHandler("BrittleGuard")]
    public class BrittleGuardSpellHandler : MasterlevelHandling
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public BrittleGuardSpellHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {

        }
        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            new BrittleGuardECSGameEffect(initParams);
        }








        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }
    }

    public class BrittleGuardECSGameEffect : ECSGameSpellEffect
    {
        GameNPC summoned = null;
        public BrittleGuardECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        public override void OnStartEffect()
        {
            base.OnStartEffect();

            GamePlayer player = Caster as GamePlayer;
            if (player == null)
            {
                return;
            }

            INpcTemplate template = NpcTemplateMgr.GetTemplate(SpellHandler.Spell.LifeDrainReturn);
            if (template == null)
            {

                ((GamePlayer)Owner).Out.SendMessage("NPC template " + SpellHandler.Spell.LifeDrainReturn + " not found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Point2D summonloc;

            summonloc = player.GetPointFromHeading(Caster.Heading, 64);

            BrittleBrain controlledBrain = new BrittleBrain(player);
            controlledBrain.IsMainPet = false;
            //summoned = new GameNPC();
            summoned = new GameSummonedPet(template);

            summoned.SetOwnBrain(controlledBrain);
            summoned.X = summonloc.X;
            summoned.Y = summonloc.Y;
            summoned.Z = player.Z;
            summoned.CurrentRegion = player.CurrentRegion;
            summoned.Heading = (ushort)((player.Heading + 2048) % 4096);
            summoned.Realm = player.Realm;
            summoned.CurrentSpeed = 0;
            summoned.Level = 1;
            summoned.Size = 10;
            summoned.AddToWorld();
            controlledBrain.AggressionState = eAggressionState.Passive;
            GameEventMgr.AddHandler(summoned, GameLivingEvent.Dying, new DOLEventHandler(GuardDie));

        }
        public override void OnStopEffect()
        {
            base.OnStopEffect();
            if (summoned != null)
            {
                summoned.Health = 0; // to send proper remove packet
                ECSGameAbilityEffect effect = EffectListService.GetAbilityEffectOnTarget(Owner, eEffect.Intercept);
                if (effect is InterceptECSGameEffect interceptEffect && interceptEffect.InterceptSource == summoned)
                {
                    EffectService.RequestImmediateCancelEffect(effect);
                }
                summoned.Delete();
            }
        }

        private void GuardDie(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC bguard = sender as GameNPC;
            if (bguard == summoned)
            {
                GameEventMgr.RemoveHandler(summoned, GameLivingEvent.Dying, new DOLEventHandler(GuardDie));
                EffectService.RequestImmediateCancelEffect(this);
            }
        }
    }




    #endregion

    //no shared timer
    #region Convoker-9
    [SpellHandlerAttribute("SummonMastery")]
    public class Convoker9Handler : MasterlevelHandling
    {


        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            new SummonMasteryECSGameEffect(initParams);
        }

        public Convoker9Handler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        { }
    }
    public class SummonMasteryECSGameEffect : ECSGameSpellEffect
    {
        private GameNPC m_living;
        private GamePlayer m_player;
        public SummonMasteryECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        public override void OnStartEffect()
        {
            foreach (JuggernautEffect jg in Owner.EffectList.GetAllOfType<JuggernautEffect>())
            {
                if (jg != null)
                {
                    ((GamePlayer)Owner).Out.SendMessage("Your Pet already has an ability of this type active", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    EffectService.RequestImmediateCancelEffect(this);
                    return;
                }
            }

            // Add byNefa 04.02.2011 13:35
            // Check if Necro try to use ML9 Convoker at own Pet
            if (m_player != null && m_player.CharacterClass.ID == (int)eCharacterClass.Necromancer)
            { // Caster is a Necro
                NecromancerPet necroPet = Owner as NecromancerPet;
                if (necroPet == null || necroPet.Owner == m_player)
                { // Caster is a Nekro and his Target is his Own Pet
                    ((GamePlayer)Owner).Out.SendMessage("You cant use this ability on your own Pet", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    EffectService.RequestImmediateCancelEffect(this);
                    return;
                }
            }
            base.OnStartEffect();
            m_living = SpellHandler.Caster.ControlledBrain.Body;
            m_living.Level += 20;
            m_living.BaseBuffBonusCategory[(int)eProperty.MeleeDamage] += 275;
            m_living.BaseBuffBonusCategory[(int)eProperty.ArmorAbsorption] += 75;
            m_living.Size += 40;

        }
        public override void OnStopEffect()
        {
            base.OnStopEffect();
            if (m_living != null)
            {
                m_living.Level -= 20;
                m_living.BaseBuffBonusCategory[(int)eProperty.MeleeDamage] -= 275;
                m_living.BaseBuffBonusCategory[(int)eProperty.ArmorAbsorption] -= 75;
                m_living.Size -= 40;
            }
        }
    }





    #endregion


    //no shared timer
    #region Convoker-10
    [SpellHandlerAttribute("SummonTitan")]
    public class Convoker10SpellHandler : MasterlevelHandling
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Convoker10SpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!CheckCastLocation())
                return false;
            return base.CheckBeginCast(selectedTarget);
        }







        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }


        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            new SummonTitanECSGameEffect(initParams);
        }



        private bool CheckCastLocation()
        {

            if (Spell.Target == eSpellTarget.AREA)
            {
                if (Caster.GroundTargetInView && Caster.GroundTarget != null)
                {

                }
                else
                {
                    if (Caster.GroundTarget == null)
                    {
                        MessageToCaster("You must set a groundtarget!", eChatType.CT_SpellResisted);
                        return false;
                    }
                    else
                    {
                        MessageToCaster("Your area target is not in view.", eChatType.CT_SpellResisted);
                        return false;
                    }
                }
            }
            return true;
        }

    }

    public class SummonTitanECSGameEffect : ECSGameSpellEffect
    {
        GameNPC summoned = null;
        ECSGameTimer m_growTimer;
        private const int C_GROWTIMER = 2000;

        public SummonTitanECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        public override void OnStartEffect()
        {
            GamePlayer player = Caster as GamePlayer;
            if (player == null)
            {
                return;
            }
            int x = Caster.X;
            int y = Caster.Y;
            int z = Caster.Z;
            if (SpellHandler.Spell.Target == eSpellTarget.AREA)
            {
                x = Caster.GroundTarget.X;
                y = Caster.GroundTarget.Y;
                z = Caster.GroundTarget.Z;
            }


            INpcTemplate template = NpcTemplateMgr.GetTemplate(SpellHandler.Spell.LifeDrainReturn);
            if (template == null)
            {

                ((GamePlayer)Owner).Out.SendMessage("NPC template " + SpellHandler.Spell.LifeDrainReturn + " not found!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            TitanBrain controlledBrain = new TitanBrain(player);
            controlledBrain.IsMainPet = false;
            controlledBrain.WalkState = eWalkState.Stay;
            summoned = new GameNPC(template);
            summoned.SetOwnBrain(controlledBrain);

            summoned.X = x;
            summoned.Y = y;
            summoned.Z = z;
            summoned.CurrentRegion = player.CurrentRegion;
            summoned.Heading = (ushort)((player.Heading + 2048) % 4096);
            summoned.Realm = player.Realm;
            summoned.CurrentSpeed = 0;
            summoned.Size = 10;
            summoned.Level = 100;
            summoned.Flags |= GameNPC.eFlags.PEACE;
            summoned.AddToWorld();
            controlledBrain.AggressionState = eAggressionState.Aggressive;
            m_growTimer = new ECSGameTimer((GameObject)Owner, new ECSGameTimer.ECSTimerCallback(TitanGrows), C_GROWTIMER);

        }

        // Make titan growing, and activate it on completition
        private int TitanGrows(ECSGameTimer timer)
        {
            if (summoned != null && summoned.Size != 60)
            {
                summoned.Size += 10;
                return C_GROWTIMER;
            }
            else
            {
                summoned.Flags = 0;
                m_growTimer.Stop();
                m_growTimer = null;
            }
            return 0;
        }
        public override void OnStopEffect()
        {
            base.OnStopEffect();
            if (summoned != null)
            {
                summoned.Health = 0; // to send proper remove packet
                summoned.Delete();
            }
        }
    }


    #endregion
}



#region BrittleBrain
namespace DOL.AI.Brain
{
    public class BrittleBrain : ControlledNpcBrain
    {
        public BrittleBrain(GameLiving owner)
            : base(owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
        }

        public override void FollowOwner()
        {
            Body.StopAttack();
            Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
        }
    }
}




#endregion

#region Titanbrain

namespace DOL.AI.Brain
{
    public class TitanBrain : ControlledNpcBrain
    {
        private GameLiving m_target;

        public TitanBrain(GameLiving owner)
            : base(owner)
        {
        }

        public GameLiving Target
        {
            get { return m_target; }
            set { m_target = value; }
        }



        #region AI

        public override bool Start()
        {
            if (!base.Start()) return false;
            return true;
        }

        public override bool Stop()
        {
            if (!base.Stop()) return false;
            return true;
        }

        private IList FindTarget()
        {
            ArrayList list = new ArrayList();

            foreach (GamePlayer o in Body.GetPlayersInRadius((ushort)Body.AttackRange))
            {
                GamePlayer p = o as GamePlayer;

                if (GameServer.ServerRules.IsAllowedToAttack(Body, p, true))
                    list.Add(p);
            }
            return list;
        }

        public override void Think()
        {
            if (Body.TargetObject is GameNPC)
                Body.TargetObject = null;

            if (Body.attackComponent.AttackState)
                return;

            IList enemies = new ArrayList();
            if (Target == null)
                enemies = FindTarget();
            else if (!Body.IsWithinRadius(Target, Body.AttackRange))
                enemies = FindTarget();
            else if (!Target.IsAlive)
                enemies = FindTarget();
            if (enemies.Count > 0 && Target == null)
            {
                //pick a random target...
                int targetnum = Util.Random(0, enemies.Count - 1);

                //Choose a random target.
                Target = enemies[targetnum] as GameLiving;
            }
            else if (enemies.Count < 1)
            {
                WalkState = eWalkState.Stay;
                enemies = FindTarget();
            }

            if (Target != null)
            {
                if (!Target.IsAlive)
                {
                    Target = null;
                }
                else if (Body.IsWithinRadius(Target, Body.AttackRange))
                {
                    Body.TargetObject = Target;
                    Goto(Target);
                    Body.StartAttack(Target);
                }
                else
                {
                    Target = null;
                }
            }
        }


        #endregion
    }
}




#endregion

#region MLBrain
public class MLBrain : GuardBrain
{
    public MLBrain() : base() { }

    public override int AggroRange
    {
        get { return 400; }
    }
    protected override void CheckNPCAggro()
    {
        //Check if we are already attacking, return if yes
        if (Body.attackComponent.AttackState)
            return;

        foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
        {
            if (AggroTable.ContainsKey(npc))
                continue; // add only new npcs
            if ((npc.Flags & GameNPC.eFlags.FLYING) != 0)
                continue; // let's not try to attack flying mobs
            if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
                continue;
            if (!npc.IsWithinRadius(Body, AggroRange))
                continue;

            if (!(npc.Brain is IControlledBrain || npc is GameGuard))
                continue;

            AddToAggroList(npc, npc.Level << 1);
            return;
        }
    }
}


#endregion
