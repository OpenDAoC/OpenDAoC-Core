using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_ForestheartAmbusher : TimedRealmAbility
    {
        public AtlasOF_ForestheartAmbusher(DBAbility dba, int level) : base(dba, level)
        {
        }

        public const int duration = 180000; // 180 seconds - 3 minutes

        // public const int duration = 30000; // 30 seconds

        public override int MaxLevel => 1;

        public override int CostForUpgrade(int level)
        {
            return 10;
        }

        public override int GetReUseDelay(int level)
        {
            return 1800; // 30 mins
        } 

        public override ushort Icon => 4268;

        public override void AddDelve(ref MiniDelveWriter w)
        {
            w.AddKeyValuePair("Name", Name);
            if (Icon > 0)
                w.AddKeyValuePair("icon", Icon);
        }

        private GamePlayer m_player;

        protected virtual void CreateSpell()
        {
            new AtlasOF_ForestheartAmbusherECSEffect(new ECSGameEffectInitParams(m_player, duration, Level));
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_player = p;
            GamePlayer m_caster = living as GamePlayer;

            if (m_caster == null)
                return;
            
            Region rgn = WorldMgr.GetRegion(m_caster.CurrentRegion.ID);
            
            if (rgn?.GetZone(m_caster.GroundTarget.X, m_caster.GroundTarget.Y) == null)
            {
                m_caster.MessageFromControlled(LanguageMgr.GetTranslation(m_caster.Client, "SummonAnimistFnF.CheckBeginCast.NoGroundTarget"), eChatType.CT_SpellResisted);
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
                delveInfoList.Add("Summons ground-targetted 100% pet for 3 minutes. Automatically acquires targets that enter its aggression radius.");
                delveInfoList.Add("Casting time: instant");
                return delveInfoList;
            }
        }
    }
}