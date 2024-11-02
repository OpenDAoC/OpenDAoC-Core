using System.Collections;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class GameGuard : GameNPC
    {
        public GameGuard() : base()
        {
            m_ownBrain = new GuardBrain();
            m_ownBrain.Body = this;
        }

        public GameGuard(INpcTemplate template) : base(template)
        {
            m_ownBrain = new GuardBrain();
            m_ownBrain.Body = this;
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = new ArrayList(4);
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GetExamineMessages.Examine", 
                                                GetName(0, true, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false)));
            return list;
        }

        public override void FireAmbientSentence(eAmbientTrigger trigger, GameObject living)
        {
            if (trigger != eAmbientTrigger.aggroing)
            {
                base.FireAmbientSentence(trigger, living);
                return;
            }

            string translatableObject = null;

            switch (Realm)
            {
                case eRealm.Albion:
                    translatableObject = "GameGuard.Albion.StartAttackSay";
                    break;
                case eRealm.Midgard:
                    translatableObject = "GameGuard.Midgard.StartAttackSay";
                    break;
                case eRealm.Hibernia:
                    translatableObject = "GameGuard.Hibernia.StartAttackSay";
                    break;
            }

            if (translatableObject == null)
                return;

            Message.MessageToArea(this, $"{Name} says, \"{LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, translatableObject)}\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow, 512, null);
        }
    }
}
