using System.Collections;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities;

public class AtlasOF_BrilliantAura : TimedRealmAbility, ISpellCastingAbilityHandler
{
    public AtlasOF_BrilliantAura(DBAbility dba, int level) : base(dba, level) { }

    private SpellHandler m_handler;
    
    public Spell Spell { get { return m_spell; } }
    public SpellLine SpellLine { get { return m_spellline; } }
    public Ability Ability { get { return this; } }

    public override int MaxLevel { get { return 1; } }
    public override int GetReUseDelay(int level) { return 1800; } // 30 min
    public override int CostForUpgrade(int level) { return 14; }

    int m_range = 1500;

    public override void Execute(GameLiving living)
    {
        if (CheckPreconditions(living, DEAD | SITTING)) return;
        if (m_handler == null) m_handler = CreateSpell(0);

        GamePlayer player = living as GamePlayer;

        if (player == null)
            return;

        ArrayList targets = new ArrayList();
        if (player.Group == null)
            targets.Add(player);
        else
        {
            foreach (GamePlayer grpplayer in player.Group.GetPlayersInTheGroup())
            {
                if (player.IsWithinRadius(grpplayer, m_range) && grpplayer.IsAlive)
                    targets.Add(grpplayer);
            }
        }

        SendCastMessage(player);

        bool AtLeastOneEffectRemoved = false;
        foreach (GamePlayer target in targets)
        {
            AtLeastOneEffectRemoved |= m_handler.StartSpell(target);
        }

        if (AtLeastOneEffectRemoved)
        {
            DisableSkill(living);
        }
    }

    private bool CastSpell(GameLiving living)
    {
        return true;
    }
    
    public virtual SpellHandler CreateSpell(double damage)
    {
        m_dbspell = new DBSpell();
        m_dbspell.Name = "Brilliant Aura of Deflection";
        m_dbspell.Icon = 14167;
        m_dbspell.ClientEffect = 14167;
        m_dbspell.Damage = 0;
        m_dbspell.DamageType = 0;
        m_dbspell.Target = "Group";
        m_dbspell.Radius = 0;
        m_dbspell.Type = eSpellType.AllMagicResistBuff.ToString();
        m_dbspell.Value = 36;
        m_dbspell.Duration = 30;
        m_dbspell.Pulse = 0;
        m_dbspell.PulsePower = 0;
        m_dbspell.Power = 0;
        m_dbspell.CastTime = 0;
        m_dbspell.EffectGroup = 0; // stacks with other damage adds
        m_dbspell.Range = 1500;
        m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
        m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        return new SpellHandler(this.m_activeLiving, m_spell, m_spellline);
    }
    
    private DBSpell m_dbspell;
    private Spell m_spell = null;
    private SpellLine m_spellline;

}
