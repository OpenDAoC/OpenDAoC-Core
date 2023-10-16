using System;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
    //no shared timer
    #region Stormlord-2
    [SpellHandler("VacuumVortex")]
    public class VacuumVortexSpell : SpellHandler
    {
        /// <summary>
        /// Execute direct damage spell
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override IList<GameLiving> SelectTargets(GameObject CasterTarget)
        {
            
            var list = new List<GameLiving>(8);
            foreach (GameNpc storms in Caster.GetNPCsInRadius(350))
            {
                if ((storms is GameStorm) && (GameServer.ServerRules.IsSameRealm(storms, Caster, true))) list.Add(storms);
            }
            return list;
        }
        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnDirectEffect(GameLiving target)
        {
            //base.OnDirectEffect(target, effectiveness);
            var targets = SelectTargets(Caster);

            if (targets == null) return;

            foreach (GameStorm targetStorm in targets)
            {
                if (targetStorm.Movable)
                {
                    int range = Util.Random(0, 750);
                    double angle = Util.RandomDouble() * 2 * Math.PI;
                    targetStorm.WalkTo(new Point3D(targetStorm.X + (int) (range * Math.Cos(angle)), targetStorm.Y + (int) (range * Math.Sin(angle)), targetStorm.Z), targetStorm.MaxSpeed);
                }
            }
        }

        public VacuumVortexSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
#endregion
}