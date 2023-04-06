using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_RuneOfDecimation : TimedRealmAbility
    {
        public AtlasOF_RuneOfDecimation(DBAbility dba, int level) : base(dba, level) { }

        public const int m_duration = 480000; // 8 minutes
        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        public override int CostForUpgrade(int level) { return 14; }

        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private GamePlayer m_player;

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Ground");
            list.Add("Duration: 8 minutes");
            list.Add("Range: 1500 units");
            list.Add("Casting time: instant");
        }

        public virtual void CreateSpell()
        {
            m_dbspell = new();
            m_dbspell.Name = "Rune Of Decimation";
            m_dbspell.Icon = 4254;
            m_dbspell.ClientEffect = 7153;
            m_dbspell.Damage = 650;
            m_dbspell.DamageType = (int)eDamageType.Energy;
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.DirectDamage.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = m_duration;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.Range = 350;
            m_spell = new Spell(m_dbspell, 0); // Make spell level 0 so it bypasses the spec level adjustment code.
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living is GamePlayer p)
                m_player = p;

            CreateSpell();

            if (m_spell == null)
                return;

            GamePlayer caster = living as GamePlayer;

            // OF trap drops at caster's feet, not at GT.
            /*if ( caster.GroundTarget == null || !caster.IsWithinRadius( caster.GroundTarget, 1500 ) )
            {
                caster.Out.SendMessage("You groundtarget is too far away to use this ability!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!caster.GroundTargetInView)
            {
                caster.Out.SendMessage("Your groundtarget is not in view!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }*/

            if (caster.IsCasting)
            {
                caster.Out.SendMessage("You are already casting an ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            GameNPC trap = new();
            trap.Model = 488;
            trap.Name = "nothing";
            trap.GuildName = m_player.Name + "";
            trap.Realm = m_player.Realm;
            trap.Size = 1;
            trap.Level = m_player.Level;
            trap.CurrentRegion = m_player.CurrentRegion;
            trap.X = m_player.X;
            trap.Y = m_player.Y;
            trap.Z = m_player.Z;
            trap.ObjectState = GameObject.eObjectState.Active;
            trap.AddToWorld();

            SpellHandler tmpHandler = new(m_player, new Spell(m_spell, eSpellType.DirectDamage), m_spellline);

            foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
            {
                if (player == caster)
                    player.MessageToSelf("You cast " + Name + "!", eChatType.CT_Spell);
                else
                    player.MessageFromArea(caster, caster.Name + " casts a spell!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

                player.Out.SendObjectCreate(trap);
            }

            new AtlasOF_RuneOfDecimationECSEffect(new ECSGameEffectInitParams(trap, m_duration, 1, tmpHandler));
            DisableSkill(living);
        }
    }
}
