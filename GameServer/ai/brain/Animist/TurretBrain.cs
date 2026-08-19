using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class TurretBrain : ControlledMobBrain
    {
        public TurretBrain(GameLiving owner) : base(owner) { }

        public override int AggroRange
        {
            get
            {
                TurretPet body = Body as TurretPet;
                Spell spell = body.TurretSpell;
                return spell.IsPBAoE ? spell.Radius : spell.CalculateEffectiveRange(body);
            }
        }

        public override bool CheckSpells(eCheckSpellType type)
        {
            if (Body == null || AggressionState is eAggressionState.Passive)
                return false;

            Spell spell = (Body as TurretPet).TurretSpell;

            if (spell == null || Body.GetSkillDisabledDuration(spell) != 0)
                return false;

            switch (type)
            {
                case eCheckSpellType.Defensive:
                {
                    if (spell.IsHarmful)
                        return false;

                    GameLiving target = FindTargetForDefensiveSpell(spell);
                    return TrustCast(spell, eCheckSpellType.Defensive, target, false);
                }
                case eCheckSpellType.Offensive:
                {
                    if (!spell.IsHarmful)
                        return false;

                    GameLiving target = CalculateNextAttackTarget();
                    return TrustCast(spell, eCheckSpellType.Offensive, target, true);
                }
            }

            return false;
        }

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            List<GameLiving> targets = GameLoop.GetListForTick<GameLiving>();
            ushort spellRange = (ushort) spell.CalculateEffectiveRange(Body);

            foreach (GamePlayer player in Body.GetPlayersInRadius(spellRange))
            {
                if (!CanSpellStillBeCastOnTarget(spell, player))
                    continue;

                if (player == GetPlayerOwner())
                    return player;

                targets.Add(player);
            }

            foreach (GameNPC npc in Body.GetNPCsInRadius(spellRange))
            {
                if (!CanSpellStillBeCastOnTarget(spell, npc))
                    continue;

                if (npc == Body || npc == GetLivingOwner())
                    return npc;

                targets.Add(npc);
            }

            return targets.Count != 0 ? targets[Util.Random(targets.Count - 1)] : null;
        }

        protected virtual bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target, bool checkLos)
        {
            if (spell.IsPBAoE)
                return Body.CastSpell(spell, m_mobSpellLine);

            if (target == null)
            {
                Body.TargetObject = null;
                return false;
            }

            Body.TargetObject = target;
            return Body.CastSpell(spell, m_mobSpellLine, checkLos);
        }

        #region AI

        public override void FollowOwner() { }

        public override void Follow(GameObject target) { }

        public override void Goto(GameObject target) { }

        public override void ComeHere() { }

        public override void Stay() { }

        #endregion
    }
}
