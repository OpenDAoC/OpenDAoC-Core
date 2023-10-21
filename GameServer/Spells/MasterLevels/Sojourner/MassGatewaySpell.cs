using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer
    #region Sojourner-10
    [SpellHandler("Groupport")]
    public class MassGatewaySpell : MasterLevelSpellHandling
    {
        public MassGatewaySpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is GamePlayer && Caster.CurrentRegionID == 51 && ((GamePlayer)Caster).BindRegion == 51)
            {
                if (Caster.CurrentRegionID == 51)
                {
                    MessageToCaster("You can't use this Ability here", EChatType.CT_SpellResisted);
                    return false;
                }
                else
                {
                    MessageToCaster("Bind in another Region to use this Ability", EChatType.CT_SpellResisted);
                    return false;
                }
            }
            return base.CheckBeginCast(selectedTarget);
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;
            if ((player != null) && (player.Group != null))
            {
                if (player.Group.IsGroupInCombat())
                {
                    player.Out.SendMessage("You can't teleport a group that is in combat!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
                }
                else
                {
                    foreach (GamePlayer pl in player.Group.GetPlayersInTheGroup())
                    {
                        if (pl != null)
                        {
                            SendEffectAnimation(pl, 0, false, 1);
                            pl.MoveTo((ushort)player.BindRegion, player.BindXpos, player.BindYpos, player.BindZpos, (ushort)player.BindHeading);
                        }
                    }
                }
            }
            else
            {
                player.Out.SendMessage("You are not a part of a group!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
            }
        }
    }
    #endregion
}
