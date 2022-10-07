using System;
using DOL.AI.Brain;
using DOL.Language;
using System.Collections;
using System.Linq;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class GameGuard : GameNPC
    {
        public GameGuard()
            : base()
        {
            m_ownBrain = new GuardBrain();
            m_ownBrain.Body = this;
        }

        public GameGuard(INpcTemplate template) : base(template)
        {
            m_ownBrain = new GuardBrain();
            m_ownBrain.Body = this;
        }

        public override bool IsStealthed
        {
            get
            {
                return (Flags & eFlags.STEALTH) != 0;
            }
        }

        public override void ProcessDeath(GameObject killer)
        {
            Console.WriteLine($"killer near zone? {ConquestService.ConquestManager.IsPlayerNearConquestObjective(killer as GamePlayer)}");
            if (killer is GamePlayer p && ConquestService.ConquestManager.IsPlayerNearConquestObjective(p))
            {
                ConquestService.ConquestManager.AddContributors(this.XPGainers.Keys.OfType<GamePlayer>().ToList());
            }

            base.ProcessDeath(killer);
        }

        public override void DropLoot(GameObject killer)
        {
            //Guards dont drop loot when they die
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = new ArrayList(4);
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GetExamineMessages.Examine", 
                                                GetName(0, true, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false)));
            return list;
        }

        public void StartAttack(GameObject attackTarget)
        {
            attackComponent.StartAttack(attackTarget);

            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
            {
                if (player != null)
                    switch (Realm)
                    {
                        case eRealm.Albion:
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.Albion.StartAttackSay"), eChatType.CT_System, eChatLoc.CL_SystemWindow); break;
                        case eRealm.Midgard:
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.Midgard.StartAttackSay"), eChatType.CT_System, eChatLoc.CL_SystemWindow); break;
                        case eRealm.Hibernia:
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.Hibernia.StartAttackSay"), eChatType.CT_System, eChatLoc.CL_SystemWindow); break;
                    }
            }
        }
    }
}