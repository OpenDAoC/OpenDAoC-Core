using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Spells;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class AttackData
    {
        public GameLiving Attacker { get; set; }
        public GameLiving Target { get; set; }
        public GameLiving OriginalTarget { get; set; } // Non-null if the attack was redirected to a different target.
        public eDamageType DamageType { get; set; }
        public eAttackType AttackType { get; set; } = eAttackType.Unknown;
        public eAttackResult AttackResult { get; set; } = eAttackResult.Any;
        public int Damage { get; set; }
        public int StyleDamage { get; set; }
        public int CriticalDamage { get; set; }
        public int CriticalChance { get; set; }
        public DbInventoryItem Weapon { get; set; }
        public int Interval { get; set; }
        public bool IsOffHand { get; set; }
        public Style Style { get; set; }
        public List<ISpellHandler> StyleEffects { get; set; } = [];
        public bool CausesCombat { get; set; } = true;
        public double ParryChance { get; set; }
        public double EvadeChance { get; set; }
        public double BlockChance { get; set; }
        public double MissChance { get; set; }
        public double DefensePenetration { get; set; }
        public eArmorSlot ArmorHitLocation { get; set; } = eArmorSlot.NOTSET;
        public ISpellHandler SpellHandler { get; set; }
        public bool IsSpellResisted { get; set; }
        public int Modifier { get; set; } // Resisted damage.
        public int AnimationId { get; set; }
        // Temporary property set by `AttackComponent.BroadcastAttackMessageToOtherPlayers`.
        // Used by pets in `GameNPC.OnAttackedByEnemy` for convenience.
        public string BroadcastMessage { get; set; }

        public bool IsMeleeAttack => AttackType is eAttackType.MeleeOneHand or eAttackType.MeleeTwoHand or eAttackType.MeleeDualWield;

        public bool IsHit => AttackResult switch
        {
            eAttackResult.HitUnstyled or
            eAttackResult.HitStyle or
            eAttackResult.Missed or
            eAttackResult.Blocked or
            eAttackResult.Evaded or
            eAttackResult.Fumbled or
            eAttackResult.Parried => true,
            _ => false
        };

        public bool GeneratesAggro => SpellHandler == null || SpellHandler.Spell.SpellType is not eSpellType.Amnesia || IsSpellResisted;

        public AttackData() { }

        public static eAttackType GetAttackType(DbInventoryItem weapon, bool dualWield, GameLiving attacker)
        {
            if (dualWield && (attacker is not GamePlayer playerAttacker || (eCharacterClass) playerAttacker.CharacterClass.ID is not eCharacterClass.Savage))
                return eAttackType.MeleeDualWield;
            else if (weapon == null)
                return eAttackType.MeleeOneHand;

            eAttackType attackType = weapon.SlotPosition switch
            {
                Slot.TWOHAND => eAttackType.MeleeTwoHand,
                Slot.RANGED => eAttackType.Ranged,
                _ => eAttackType.MeleeOneHand,
            };

            if (attacker is GamePlayer)
            {
                // This should the only important one to check.
                if (attackType is eAttackType.MeleeTwoHand)
                {
                    if (weapon.Item_Type is not Slot.TWOHAND)
                        attackType = eAttackType.MeleeOneHand;
                }
                else if (attackType is eAttackType.Ranged)
                {
                    if (weapon.Item_Type is not Slot.RANGED)
                        attackType = eAttackType.MeleeOneHand;
                }
            }

            return attackType;
        }

        public enum eAttackType : int
        {
            Unknown = 0,
            MeleeOneHand = 1,
            MeleeDualWield = 2,
            MeleeTwoHand = 3,
            Ranged = 4,
            Spell = 5
        }
    }
}
