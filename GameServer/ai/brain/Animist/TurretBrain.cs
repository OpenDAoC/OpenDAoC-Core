using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class TurretBrain : ControlledMobBrain
    {
        protected readonly List<GameLiving> _defensiveSpellTargets;
        protected virtual bool CheckLosBeforeCastingDefensiveSpells => false;
        protected virtual bool CheckLosBeforeCastingOffensiveSpells => true;

        public TurretBrain(GameLiving owner) : base(owner)
        {
            _defensiveSpellTargets = new();
        }

        public override int AggroRange => ((TurretPet) Body).TurretSpell.Range;

        public override void Think()
        {
            if (AggressionState == eAggressionState.Aggressive)
                CheckProximityAggro();

            if (!CheckSpells(eCheckSpellType.Defensive))
                CheckSpells(eCheckSpellType.Offensive);
        }

        public override bool CheckSpells(eCheckSpellType type)
        {
            if (Body == null || AggressionState == eAggressionState.Passive)
                return false;

            Spell spell = ((TurretPet) Body).TurretSpell;

            if (spell == null || Body.GetSkillDisabledDuration(spell) != 0)
                return false;

            switch (type)
            {
                case eCheckSpellType.Defensive:
                {
                    switch (spell.SpellType)
                    {
                        case eSpellType.HeatColdMatterBuff:
                        case eSpellType.BodySpiritEnergyBuff:
                        case eSpellType.ArmorAbsorptionBuff:
                        case eSpellType.AblativeArmor:
                        {
                            GameLiving target = FindTargetForDefensiveSpell(spell);

                            if (target != null)
                                return TrustCast(spell, eCheckSpellType.Defensive, FindTargetForDefensiveSpell(spell), CheckLosBeforeCastingDefensiveSpells);

                            break;
                        }
                    }

                    return false;
                }
                case eCheckSpellType.Offensive:
                {
                    switch (spell.SpellType)
                    {
                        case eSpellType.DirectDamage:
                        case eSpellType.DamageSpeedDecrease:
                        case eSpellType.SpeedDecrease:
                        case eSpellType.Taunt:
                        case eSpellType.MeleeDamageDebuff:
                        {
                            GameLiving target = CalculateNextAttackTarget();

                            if (target != null)
                                return TrustCast(spell, eCheckSpellType.Offensive, target, CheckLosBeforeCastingOffensiveSpells);

                            break;
                        }
                    }

                    return false;
                }
                default:
                    return false;
            }
        }

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            // Clear the current list of invalid or already buffed targets before checking nearby players and NPCs.
            for (int i = _defensiveSpellTargets.Count - 1; i >= 0; i--)
            {
                GameLiving living = _defensiveSpellTargets[i];

                if (GameServer.ServerRules.IsAllowedToAttack(Body, living, true) || !living.IsAlive || LivingHasEffect(living, spell) || !Body.IsWithinRadius(living, (ushort)spell.Range))
                    _defensiveSpellTargets.RemoveAt(i);
            }

            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Body, player, true) || !player.IsAlive || LivingHasEffect(player, spell))
                    continue;

                if (player == GetPlayerOwner())
                    return player;

                if (!_defensiveSpellTargets.Contains(player))
                    _defensiveSpellTargets.Add(player);
            }

            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort) spell.Range))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Body, npc, true) || !npc.IsAlive || LivingHasEffect(npc, spell))
                    continue;

                if (npc == Body || npc == GetLivingOwner())
                    return npc;

                if (!_defensiveSpellTargets.Contains(npc))
                    _defensiveSpellTargets.Add(npc);
            }

            return _defensiveSpellTargets.Count != 0 ? _defensiveSpellTargets[Util.Random(_defensiveSpellTargets.Count - 1)] : null;
        }

        protected virtual bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target, bool checkLos)
        {
            if (spell.IsPBAoE)
                return Body.CastSpell(spell, m_mobSpellLine);

            if (target != null)
            {
                Body.TargetObject = target;
                Body.StopAttack();
                return Body.CastSpell(spell, m_mobSpellLine, checkLos);
            }

            return false;
        }

        public override bool Stop()
        {
            ClearAggroList();
            _defensiveSpellTargets.Clear();
            return base.Stop();
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
