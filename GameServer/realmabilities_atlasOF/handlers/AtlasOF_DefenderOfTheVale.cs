using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_DefenderOfTheVale : TimedRealmAbility
    {
        public AtlasOF_DefenderOfTheVale(DBAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        
        public override bool CheckRequirement(GamePlayer player) { return true; }
        
        public override int CostForUpgrade(int level) { return 10; }
        
         
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Self");
            list.Add("Duration: 60 sec");
            list.Add("Casting time: instant");
        }
        
        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_value = 0;
        private GamePlayer m_player;

        public virtual void CreateSpell()
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Defender Of The Vale";
            m_dbspell.Icon = 7160;
            m_dbspell.ClientEffect = 11253;
            m_dbspell.Damage = 50;
            m_dbspell.DamageType = 11;
            m_dbspell.Target = "Group";
            m_dbspell.Radius = 1500;
            m_dbspell.Type = eSpellType.AblativeArmor.ToString();
            m_dbspell.Value = 500;
            m_dbspell.Duration = 60;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0; // stacks with other damage adds
            m_dbspell.Range = 0;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
            {
                m_player = p;
            }
            CreateSpell();
            CastSpell(living);
            DisableSkill(living);
        }

        protected void CastSpell(GameLiving target)
        {
            if (target.IsAlive && m_spell != null)
            {
                List<GameLiving> targets = new List<GameLiving>();
                if (target.Group != null)
                {
                    foreach (var living in target.Group.GetMembersInTheGroup())
                    {
                        if (target.GetDistance(new Point2D(living.X, living.Y))  > m_spell.Radius) 
                            targets.Add(living);
                    }
                }
                else 
                    targets.Add(target);

                foreach (var castTarget in targets)
                {
                    ISpellHandler dd = ScriptMgr.CreateSpellHandler(target, m_spell, m_spellline);
                    dd.StartSpell(castTarget);
                }
            }
        }
        
        public override IList<string> DelveInfo
        {
            get
            {
                IList<string> list = base.DelveInfo;
                list.Add("Defender of the Vale (Valewalker)");
                list.Add("Target: Group");
                list.Add("Applies hit point buffer that absorbs 50% damage for up to 500 hit points to group members in a 1500 radius.");
                
                list.Add("");
                list.Add("Buffer Value: 500");
                list.Add("Absorption Amount: 50%");

                return list;
            }
        }
    }
}
