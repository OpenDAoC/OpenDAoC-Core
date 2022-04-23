using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_Juggernaut : TimedRealmAbility
    {
		public AtlasOF_Juggernaut(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        private int m_duration = 240; // in seconds - 4mins
        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 14; }
		public override int GetReUseDelay(int level) { return 1800; } // 30 mins
		
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer m_caster = living as GamePlayer;
			if (m_caster == null || m_caster.castingComponent == null )
				return;
			
			if (m_caster.ControlledBrain != null){
				m_caster.Out.SendMessage(LanguageMgr.GetTranslation((m_caster).Client, "Summon.CheckBeginCast.AlreadyHaveaPet"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
				return;
			}
			
			SpellLine RAspellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            Spell Juggernaut = SkillBase.GetSpellByID(90801);
            

            if (Juggernaut != null)
            {
	            m_caster.CastSpell(Juggernaut, RAspellLine);
            }
            
            new ECSGameTimer(m_caster, KillJuggernaut, m_duration);
		}
        
        private int KillJuggernaut(ECSGameTimer timer)
		{
			GamePlayer m_caster = timer.TimerOwner as GamePlayer;

			m_caster?.ControlledBrain.Body.Die(null);

			return 0;
		}
	}
}
