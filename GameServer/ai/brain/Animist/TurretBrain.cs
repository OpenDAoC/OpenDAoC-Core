/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS;

namespace DOL.AI.Brain
{
    public class TurretBrain : ControlledNpcBrain
    {
        protected readonly List<GameLiving> m_defensiveSpellTargets;

        public TurretBrain(GameLiving owner) : base(owner)
        {
            m_defensiveSpellTargets = new();
        }

        public List<GameLiving> DefensiveSpellTargets => m_defensiveSpellTargets;
        public override int AggroRange => ((TurretPet) Body).TurretSpell.Range;

        public override void Think()
        {
            GamePlayer playerowner = GetPlayerOwner();

            if (!playerowner.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID), out long lastUpdate))
                playerowner.Client.GameObjectUpdateArray.TryAdd(new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID), lastUpdate);

            if (playerowner != null && (GameLoop.GameLoopTime - playerowner.Client.GameObjectUpdateArray[new Tuple<ushort, ushort>(Body.CurrentRegionID, (ushort)Body.ObjectID)]) > ThinkInterval)
                playerowner.Out.SendObjectUpdate(Body);

            if (AggressionState == eAggressionState.Aggressive)
                CheckProximityAggro();

            if (!CheckSpells(eCheckSpellType.Defensive))
                CheckSpells(eCheckSpellType.Offensive);
        }

        public override bool CheckSpells(eCheckSpellType type)
        {
            if (Body == null || AggressionState == eAggressionState.Passive || ((TurretPet)Body).TurretSpell == null)
                return false;

            Spell spell = ((TurretPet)Body).TurretSpell;

            if (Body.GetSkillDisabledDuration(spell) != 0)
                return false;

            bool casted = false;

            switch (type)
            {
                case eCheckSpellType.Defensive:
                    casted = CheckDefensiveSpells(spell);
                    break;
                case eCheckSpellType.Offensive:
                    casted = CheckOffensiveSpells(spell);
                    break;
            }

            return casted /*|| Body.IsCasting*/;
        }

        protected override bool CheckDefensiveSpells(Spell spell)
        {
            switch ((eSpellType)spell.SpellType)
            {
                case eSpellType.HeatColdMatterBuff:
                case eSpellType.BodySpiritEnergyBuff:
                case eSpellType.ArmorAbsorptionBuff:
                case eSpellType.AblativeArmor:
                    return TrustCast(spell, eCheckSpellType.Defensive, GetDefensiveTarget(spell));
            }

            return false;
        }

        protected override bool CheckOffensiveSpells(Spell spell)
        {
            switch ((eSpellType)spell.SpellType)
            {
                case eSpellType.DirectDamage:
                case eSpellType.DamageSpeedDecrease:
                case eSpellType.SpeedDecrease:
                case eSpellType.Taunt:
                case eSpellType.MeleeDamageDebuff:
                    return TrustCast(spell, eCheckSpellType.Offensive, CalculateNextAttackTarget());
            }

            return false;
        }

        protected virtual bool TrustCast(Spell spell, eCheckSpellType type, GameLiving target)
        {
            if (spell.IsPBAoE)
                return Body.CastSpell(spell, m_mobSpellLine);

            if (target != null)
            {
                Body.TargetObject = target;
                Body.StopAttack();
                return Body.CastSpell(spell, m_mobSpellLine, false);
            }

            return false;
        }

        private GameLiving GetDefensiveTarget(Spell spell)
        {
            // Clear the current list of invalid or already buffed targets before checking nearby players and NPCs.
            for (int i = DefensiveSpellTargets.Count - 1; i >= 0; i--)
            {
                GameLiving living = DefensiveSpellTargets[i];

                if (GameServer.ServerRules.IsAllowedToAttack(Body, living, true) || !living.IsAlive || LivingHasEffect(living, spell) || !Body.IsWithinRadius(living, (ushort)spell.Range))
                    DefensiveSpellTargets.RemoveAt(i);
            }

            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)spell.Range, !Body.CurrentRegion.IsDungeon))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Body, player, true) || !player.IsAlive || LivingHasEffect(player, spell))
                    continue;

                if (player == GetPlayerOwner())
                    return player;

                if (!DefensiveSpellTargets.Contains(player))
                    DefensiveSpellTargets.Add(player);
            }

            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)spell.Range, !Body.CurrentRegion.IsDungeon))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Body, npc, true) || !npc.IsAlive || LivingHasEffect(npc, spell))
                    continue;

                if (npc == Body || npc == GetLivingOwner())
                    return npc;

                if (!DefensiveSpellTargets.Contains(npc))
                    DefensiveSpellTargets.Add(npc);
            }

            return DefensiveSpellTargets.Any() ? DefensiveSpellTargets[Util.Random(DefensiveSpellTargets.Count - 1)] : null;
        }

        public override bool Stop()
        {
            ClearAggroList();
            DefensiveSpellTargets.Clear();
            return base.Stop();
        }

        #region AI

        public override void FollowOwner() { }

        public override void Follow(GameObject target) { }

        protected override void OnFollowLostTarget(GameObject target) { }

        public override void Goto(GameObject target) { }

        public override void ComeHere() { }

        public override void Stay() { }

        #endregion
    }
}