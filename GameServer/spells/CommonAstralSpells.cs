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
    [SpellHandler(eSpellType.DexStrConQuiTap)]
    public class DexStrConQuiTap : SpellHandler
    {
        private IList<eProperty> m_stats;
        public IList<eProperty> Stats
        {
            get { return m_stats; }
            set { m_stats = value; }
        }

        public DexStrConQuiTap(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Stats = new List<eProperty>();
            Stats.Add(eProperty.Dexterity);
            Stats.Add(eProperty.Strength);
            Stats.Add(eProperty.Constitution);
            Stats.Add(eProperty.Quickness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            foreach (eProperty property in Stats)
            {
                m_caster.BaseBuffBonusCategory[property] += (int)m_spell.Value;
                Target.DebuffCategory[property] -= (int)m_spell.Value;
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            foreach (eProperty property in Stats)
            {
                Target.DebuffCategory[property] += (int)m_spell.Value;
                m_caster.BaseBuffBonusCategory[property] -= (int)m_spell.Value;
            }
            return base.OnEffectExpires(effect, noMessages);
        }
    }

    /// <summary>
    /// A proc to lower target's ArmorFactor and ArmorAbsorption.
    /// </summary>
    [SpellHandler(eSpellType.ArmorReducingEffectiveness)]
    public class ArmorReducingEffectiveness : DualStatDebuff
    {
        public override eProperty Property1 { get { return eProperty.ArmorFactor; } }
        public override eProperty Property2 { get { return eProperty.ArmorAbsorption; } }
        public ArmorReducingEffectiveness(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Summons a Elemental that only follows the caster.
    /// </summary>
    [SpellHandler(eSpellType.SummonHealingElemental)]
    public class SummonHealingElemental : MasterlevelHandling
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        GameNPC summoned = null;
        GameSpellEffect beffect = null;
        public SummonHealingElemental(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)  {}

        public override void ApplyEffectOnTarget(GameLiving target)
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
                MessageToCaster("NPC template " + Spell.LifeDrainReturn + " not found!", eChatType.CT_System);
                return;
            }

            Point2D summonloc;
            beffect = CreateSpellEffect(target, CasterEffectiveness);
            {
                summonloc = target.GetPointFromHeading(target.Heading, 64);

                BrittleBrain controlledBrain = new BrittleBrain(player);
                controlledBrain.IsMainPet = false;
                summoned = new GameNPC(template);
                summoned.SetOwnBrain(controlledBrain);
                summoned.X = summonloc.X;
                summoned.Y = summonloc.Y;
                summoned.Z = target.Z;
                summoned.CurrentRegion = target.CurrentRegion;
                summoned.Heading = (ushort)((target.Heading + 2048) % 4096);
                summoned.Realm = target.Realm;
                summoned.Level = Caster.Level;
                summoned.Size = 50;
                summoned.AddToWorld();
                controlledBrain.AggressionState = eAggressionState.Passive;
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
    [SpellHandler(eSpellType.SummonElemental)]
    public class SummonElemental : SummonSpellHandler
    {
        private ISpellHandler _trap;

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            //Set pet infos & Brain
            base.ApplyEffectOnTarget(target);
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

        protected void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if (args == null || args.AttackData == null)
                return;

            if (_trap == null)
            {
                _trap = MakeTrap();
            }
            if (Util.Chance(99))
            {
                _trap.StartSpell(args.AttackData.Target);
            }
        }
        // Creates the trap(spell)
        private ISpellHandler MakeTrap()
        {
            DbSpell dbs = new DbSpell();
            dbs.Name = "irritatin wisp";
            dbs.Icon = 4107;
            dbs.ClientEffect = 5435;
            dbs.DamageType = 15;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = eSpellType.DirectDamage.ToString();
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

        public SummonElemental(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    } 
}

namespace DOL.GS
{
    public class SummonHealingElementalPet : GameSummonedPet
    {
        public override int MaxHealth
        {
            get { return Level * 10; }
        }
        public override void OnAttackedByEnemy(AttackData ad) { }
        public SummonHealingElementalPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
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
