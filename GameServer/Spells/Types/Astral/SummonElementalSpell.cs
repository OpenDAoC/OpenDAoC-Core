using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS.Spells
{
    /// <summary>
    /// Summons a Elemental that follows the target and attacks the target.
    /// </summary>
    [SpellHandler("SummonElemental")]
    public class SummonElementalSpell : SummonSpellHandler
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

        protected override GameSummonedPet GetGamePet(INpcTemplate template) { return new ElementalPet(template); }
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

        public SummonElementalSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }
    } 
}