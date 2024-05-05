using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{
    /// <summary>
    /// The spell used for the Personal Bind Recall Stone.
    /// </summary>
    [SpellHandlerAttribute("GatewayPersonalBind")]
    public class GatewayPersonalBind : SpellHandler
    {
        public GatewayPersonalBind(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }

        /// <summary>
        /// Can this spell be queued with other spells?
        /// </summary>
        public override bool CanQueue => false;

        /// <summary>
        /// Whether this spell can be cast on the selected target at all.
        /// </summary>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (Caster is not GamePlayer player)
                return false;

            if ((((player.CurrentZone != null && player.CurrentZone.IsRvR) || (player.CurrentRegion != null && player.CurrentRegion.IsInstance)) && GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvE) ||
                (player.CurrentRegion.ID == 497 && player.Client.Account.PrivLevel == 1)) // Jail.
            {
                // Actual live message is: You can't use that item!
                player.Out.SendMessage("You can't use that here!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.IsMoving)
            {
                SendMovingMessage(player);
                return false;
            }

            if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
            {
                SendInCombatMessage(player);
                return false;
            }

            return true;
        }

        public override bool CheckEndCast(GameLiving target)
        {
            if (Caster is not GamePlayer player)
                return false;

            if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
            {
                SendInCombatMessage(player);
                return false;
            }

            if (player.IsMoving)
            {
                SendMovingMessage(player);
                return false;
            }

            return base.CheckEndCast(target);
        }

        /// <summary>
        /// Always a constant casting time
        /// </summary>
        public override int CalculateCastingTime()
        {
            return m_spell.CastTime;
        }

        /// <summary>
        /// Apply the effect.
        /// </summary>
        public override void ApplyEffectOnTarget(GameLiving target)
        {
            if (target is not GamePlayer player)
                return;

            SendEffectAnimation(player, 0, false, 1);
            UniPortalEffect effect = new(this, 1000);
            effect.Start(player);
            player.MoveToBind();
        }

        public override void CasterMoves()
        {
            InterruptCasting();

            if (Caster is GamePlayer playerCaster)
                playerCaster.Out.SendMessage(LanguageMgr.GetTranslation(playerCaster.Client, "SpellHandler.CasterMove"), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                List<string> list = [$"  {Spell.Description}"];
                return list;
            }
        }

        private static void SendInCombatMessage(GamePlayer player)
        {
            player.Out.SendMessage("You have been in combat recently and cannot use this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private static void SendMovingMessage(GamePlayer player)
        {
            player.Out.SendMessage("You must be standing still to use this item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
