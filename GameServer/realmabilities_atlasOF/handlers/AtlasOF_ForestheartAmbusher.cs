using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_ForestheartAmbusher : TimedRealmAbility
    {
        public AtlasOF_ForestheartAmbusher(DBAbility dba, int level) : base(dba, level) { }

        public const int duration = 180000; // 180 seconds - 3 minutes

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

        private GamePlayer _caster;

        protected virtual void CreateSpell()
        {
            new AtlasOF_ForestheartAmbusherECSEffect(new ECSGameEffectInitParams(_caster, duration, Level));
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            _caster = living as GamePlayer;

            if (_caster == null)
                return;
            
            Region rgn = WorldMgr.GetRegion(_caster.CurrentRegion.ID);
            
            if (rgn?.GetZone(_caster.GroundTarget.X, _caster.GroundTarget.Y) == null)
            {
                _caster.MessageToSelf(LanguageMgr.GetTranslation(_caster.Client, "SummonAnimistFnF.CheckBeginCast.NoGroundTarget"), eChatType.CT_SpellResisted);
                return;
            }

            if (!_caster.GroundTargetInView)
            {
				_caster.MessageToSelf(LanguageMgr.GetTranslation(_caster.Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInView"), eChatType.CT_SpellResisted);
				return;
            }

            if (!_caster.IsWithinRadius(_caster.GroundTarget, 2000))
            {
				_caster.MessageToSelf(LanguageMgr.GetTranslation(_caster.Client, "SummonAnimistPet.CheckBeginCast.GroundTargetNotInSpellRange"), eChatType.CT_SpellResisted);
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