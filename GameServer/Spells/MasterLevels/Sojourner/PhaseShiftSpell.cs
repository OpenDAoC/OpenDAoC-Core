using System;
using Core.Events;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("Phaseshift")]
    public class PhaseShiftSpell : MasterLevelSpellHandling
    {
        private int endurance;

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            endurance = (Caster.MaxEndurance * 50) / 100;

            if (Caster.Endurance < endurance)
            {
                MessageToCaster("You need 50% endurance for this spell!!", EChatType.CT_System);
                return false;
            }

            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameEventMgr.AddHandler(Caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            Caster.Endurance -= endurance;
        }

        private void OnAttack(CoreEvent e, object sender, EventArgs arguments)
        {
            GameLiving living = sender as GameLiving;
            if (living == null) return;
            AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
            AttackData ad = null;
            if (attackedByEnemy != null)
                ad = attackedByEnemy.AttackData;

            if (ad.Attacker is GamePlayer)
            {
                ad.Damage = 0;
                ad.CriticalDamage = 0;
                GamePlayer player = ad.Attacker as GamePlayer;
                player.Out.SendMessage(living.Name + " is Phaseshifted and can't be attacked!", EChatType.CT_Missed,
                    EChatLoc.CL_SystemWindow);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(OnAttack));
            return base.OnEffectExpires(effect, noMessages);
        }

        public override bool HasPositiveEffect
        {
            get { return false; }
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        // constructor
        public PhaseShiftSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}