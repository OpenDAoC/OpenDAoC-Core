using System.Collections;
using Core.AI.Brain;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    public class GameRealmGuard : GameNpc
    {
        public GameRealmGuard() : base()
        {
            m_ownBrain = new RealmGuardBrain();
            m_ownBrain.Body = this;
        }

        public GameRealmGuard(INpcTemplate template) : base(template)
        {
            m_ownBrain = new RealmGuardBrain();
            m_ownBrain.Body = this;
        }

        public override bool IsStealthed => (Flags & ENpcFlags.STEALTH) != 0;

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = new ArrayList(4);
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GetExamineMessages.Examine", 
                                                GetName(0, true, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false)));
            return list;
        }

        public override void FireAmbientSentence(EAmbientNpcTrigger trigger, GameObject living)
        {
            if (trigger != EAmbientNpcTrigger.aggroing)
            {
                base.FireAmbientSentence(trigger, living);
                return;
            }

            string translatableObject = null;

            switch (Realm)
            {
                case ERealm.Albion:
                    translatableObject = "GameGuard.Albion.StartAttackSay";
                    break;
                case ERealm.Midgard:
                    translatableObject = "GameGuard.Midgard.StartAttackSay";
                    break;
                case ERealm.Hibernia:
                    translatableObject = "GameGuard.Hibernia.StartAttackSay";
                    break;
            }

            if (translatableObject == null)
                return;

            MessageUtil.MessageToArea(this, $"{Name} says, \"{LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, translatableObject)}\"", EChatType.CT_Say, EChatLoc.CL_ChatWindow, 512, null);
        }
    }
}