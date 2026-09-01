using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    [SkillHandlerAttribute(Abilities.Distraction)]
    public class DistractionAbilityHandler : IAbilityActionHandler
    {
        private const int REUSE_TIMER = 10000;
        private const int DURATION = 6000;
        private const int RANGE = 750;
        private const ushort RADIUS = 400;

        public void Execute(Ability ab, GamePlayer player)
        {
            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseDead"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsMezzed)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseMezzed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsStunned)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStunned"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsSitting)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.CannotUseStanding"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            GroundTarget groundTarget = player.GroundTarget;

            if (!groundTarget.IsValid)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNull"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!player.GroundTargetInView)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInView"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (groundTarget.GetDistance(player) > RANGE)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInSpellRange"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            foreach (GameNPC npc in player.CurrentRegion.GetNPCsInRadius(groundTarget, RADIUS))
            {
                if (!GameServer.ServerRules.IsAllowedToAttack(player, npc, true) ||
                    npc.Brain is IControlledBrain ||
                    npc.attackComponent.AttackState ||
                    npc.castingComponent.IsCasting)
                {
                    continue;
                }

                npc.TurnTo(groundTarget.X, groundTarget.Y, DURATION);
            }

            player.DisableSkill(ab, REUSE_TIMER);
        }
    }
}
