﻿using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Dex/Qui/Str/Con stat specline debuff and transfers them to the caster.
    /// </summary>
    [SpellHandlerAttribute("DexStrConQuiTap")]
    public class DexStrConQuiTapHandler : SpellHandler
    {
        private IList<EProperty> m_stats;
        public IList<EProperty> Stats
        {
            get { return m_stats; }
            set { m_stats = value; }
        }

        public DexStrConQuiTapHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Stats = new List<EProperty>();
            Stats.Add(EProperty.Dexterity);
            Stats.Add(EProperty.Strength);
            Stats.Add(EProperty.Constitution);
            Stats.Add(EProperty.Quickness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            foreach (EProperty property in Stats)
            {
                m_caster.BaseBuffBonusCategory[(int)property] += (int)m_spell.Value;
                Target.DebuffCategory[(int)property] -= (int)m_spell.Value;
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            foreach (EProperty property in Stats)
            {
                Target.DebuffCategory[(int)property] += (int)m_spell.Value;
                m_caster.BaseBuffBonusCategory[(int)property] -= (int)m_spell.Value;
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }

    /// <summary>
    /// A proc to lower target's ArmorFactor and ArmorAbsorption.
    /// </summary>
    [SpellHandlerAttribute("ArmorReducingEffectiveness")]
    public class ArmorReducingHandler : DualStatDebuffHandler
    {
        public override EProperty Property1 { get { return EProperty.ArmorFactor; } }
        public override EProperty Property2 { get { return EProperty.ArmorAbsorption; } }
        public ArmorReducingHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Summons a Elemental that only follows the caster.
    /// </summary>
    [SpellHandler("SummonHealingElemental")]
    public class SummonHealingElementalHandler : MasterLevelHandling
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        GameNpc summoned = null;
        GameSpellEffect beffect = null;
        public SummonHealingElementalHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)  {}

        /// <summary>
        /// Apply effect on target or do spell action if non duration spell
        /// </summary>
        /// <param name="target">target that gets the effect</param>
        /// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = Caster as GamePlayer;
            if (player == null)
            {
                return;
            }

            INpcTemplate template = NpcTemplateMgr.GetTemplate(Spell.LifeDrainReturn);
            if (template == null)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("NPC template {0} not found! Spell: {1}", Spell.LifeDrainReturn, Spell.ToString());
                MessageToCaster("NPC template " + Spell.LifeDrainReturn + " not found!", EChatType.CT_System);
                return;
            }

            Point2D summonloc;
            beffect = CreateSpellEffect(target, effectiveness);
            {
                summonloc = target.GetPointFromHeading(target.Heading, 64);

                BrittleBrain controlledBrain = new BrittleBrain(player);
                controlledBrain.IsMainPet = false;
                summoned = new GameNpc(template);
                summoned.SetOwnBrain(controlledBrain);
                summoned.X = summonloc.X;
                summoned.Y = summonloc.Y;
                summoned.Z = target.Z;
                summoned.CurrentRegion = target.CurrentRegion;
                summoned.Heading = (ushort)((target.Heading + 2048) % 4096);
                summoned.Realm = target.Realm;
                summoned.CurrentSpeed = 0;
                summoned.Level = Caster.Level;
                summoned.Size = 50;
                summoned.AddToWorld();
                controlledBrain.AggressionState = EAggressionState.Passive;
                beffect.Start(Caster);
            }
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (summoned != null)
            {
                summoned.Health = 0; // to send proper remove packet
                summoned.Delete();
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }

    /// <summary>
    /// Summons a Elemental that follows the target and attacks the target.
    /// </summary>
    [SpellHandler("SummonElemental")]
    public class SummonElementalHandler : SummonHandler
    {
        private ISpellHandler _trap;

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            //Set pet infos & Brain
            base.ApplyEffectOnTarget(target, effectiveness);
            ProcPetBrain petBrain = (ProcPetBrain)m_pet.Brain;
            m_pet.Level = Caster.Level;
            m_pet.Strength = 0;
            petBrain.AddToAggroList(target, 1);
            petBrain.Think();
        }

        protected override GameSummonedPet GetGamePet(INpcTemplate template) { return new SummonElementalPet(template); }
        protected override IControlledBrain GetPetBrain(GameLiving owner) { return new ProcPetBrain(owner); }
        protected override void SetBrainToOwner(IControlledBrain brain) { }
        protected override void AddHandlers() { GameEventMgr.AddHandler(m_pet, GameLivingEvent.AttackFinished, EventHandler); }

        protected void EventHandler(CoreEvent e, object sender, EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if (args == null || args.AttackData == null)
                return;

            if (_trap == null)
            {
                _trap = MakeTrap();
            }
            if (UtilCollection.Chance(99))
            {
                _trap.StartSpell(args.AttackData.Target);
            }
        }
        // Creates the trap(spell)
        private ISpellHandler MakeTrap()
        {
            DBSpell dbs = new DBSpell();
            dbs.Name = "irritatin wisp";
            dbs.Icon = 4107;
            dbs.ClientEffect = 5435;
            dbs.DamageType = 15;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.DirectDamage.ToString();
            dbs.Damage = 80;
            dbs.Value = 0;
            dbs.Duration = 0;
            dbs.Frequency = 0;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = 1500;
            Spell s = new Spell(dbs, 50);
            SpellLine sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            return ScriptMgr.CreateSpellHandler(m_pet, s, sl);
        }

        public SummonElementalHandler(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    } 
}

namespace DOL.GS
{
    public class HealingElementalPet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public HealingElementalPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }

    public class SummonElementalPet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public SummonElementalPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}