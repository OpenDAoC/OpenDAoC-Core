using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Scripts;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

//ml5 in database Target shood be Group if PvP..Realm if RvR..Value = spell proc'd (a.k the 80value dd proc)

[SpellHandler("EssenceFlamesProc")]
public class EssenceFlamesProcSpell : OffensiveProcSpell
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Handler fired whenever effect target is attacked
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sender"></param>
    /// <param name="arguments"></param>
    protected override void EventHandler(CoreEvent e, object sender, EventArgs arguments)
    {
        AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
        if (args == null || args.AttackData == null)
        {
            return;
        }

        AttackData ad = args.AttackData;
        if (ad.AttackResult != EAttackResult.HitUnstyled && ad.AttackResult != EAttackResult.HitStyle)
            return;

        int baseChance = Spell.Frequency / 100;

        if (ad.IsMeleeAttack)
        {
            if (sender is GamePlayer)
            {
                GamePlayer player = (GamePlayer)sender;
                DbInventoryItem leftWeapon = player.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
                // if we can use left weapon, we have currently a weapon in left hand and we still have endurance,
                // we can assume that we are using the two weapons.
                if (player.attackComponent.CanUseLefthandedWeapon && leftWeapon != null &&
                    leftWeapon.Object_Type != (int)EObjectType.Shield)
                {
                    baseChance /= 2;
                }
            }
        }

        if (baseChance < 1)
            baseChance = 1;

        if (Util.Chance(baseChance))
        {
            ISpellHandler handler = ScriptMgr.CreateSpellHandler((GameLiving)sender, m_procSpell, m_procSpellLine);
            if (handler != null)
            {
                switch (m_procSpell.Target)
                {
                    case ESpellTarget.ENEMY:
                    {
                        handler.StartSpell(ad.Target);
                        break;
                    }
                    case ESpellTarget.SELF:
                    {
                        handler.StartSpell(ad.Attacker);
                        break;
                    }
                    case ESpellTarget.GROUP:
                    {
                        if (Caster is GamePlayer player)
                        {
                            if (player.Group != null)
                            {
                                foreach (GameLiving groupPlayer in player.Group.GetMembersInTheGroup())
                                {
                                    if (player.IsWithinRadius(groupPlayer, m_procSpell.Range))
                                    {
                                        handler.StartSpell(groupPlayer);
                                    }
                                }
                            }
                            else
                                handler.StartSpell(player);
                        }

                        break;
                    }
                    default:
                    {
                        log.Warn("Skipping " + m_procSpell.Target + " proc " + m_procSpell.Name + " on " +
                                 ad.Target.Name + "; Realm = " + ad.Target.Realm);
                        break;
                    }
                }
            }
        }
    }

    // constructor
    public EssenceFlamesProcSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}