using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Juggernaut : TimedRealmAbility
    {
        public AtlasOF_Juggernaut(DBAbility dba, int level) : base(dba, level)
        {
        }

        public const int duration = 240000; // 240 seconds - 4 minutes

        // public const int duration = 30000; // 30 seconds

        public override int MaxLevel => 1;

        public override int CostForUpgrade(int level)
        {
            return 14;
        }

        public override int GetReUseDelay(int level)
        {
            return 1800; // 30 mins
        } 

        public override ushort Icon => 4261;

        public override void AddDelve(ref MiniDelveWriter w)
        {
            w.AddKeyValuePair("Name", Name);
            if (Icon > 0)
                w.AddKeyValuePair("icon", Icon);
        }

        private GamePlayer m_player;

        protected virtual void CreateSpell()
        {
            new AtlasOF_JuggernautECSEffect(new ECSGameEffectInitParams(m_player, duration, Level));
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_player = p;
            GamePlayer m_caster = living as GamePlayer;

            if (m_caster == null)
                return;

            if (m_caster.ControlledBrain != null)
            {
                m_caster.Out.SendMessage(
                    LanguageMgr.GetTranslation((m_caster).Client, "Summon.CheckBeginCast.AlreadyHaveaPet"),
                    eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            CreateSpell();
            DisableSkill(living);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Summons a pet of your level (i.e. a 100% pet) that lasts for 4 minutes.");
                delveInfoList.Add("Casting time: instant");
                return delveInfoList;
            }
        }
    }
}