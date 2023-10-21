using System.Collections;
using Core.Database;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class OfRaBrilliantAuraAbility : TimedRealmAbility, ISpellCastingAbilityHandler
{
    public OfRaBrilliantAuraAbility(DbAbility dba, int level) : base(dba, level) { }

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

        GamePlayer player = living as GamePlayer;

        if (player == null)
            return;
        if (m_handler == null) m_handler = CreateSpell(player);

        ArrayList targets = new ArrayList();
        if (player.Group == null)
            targets.Add(player);
        else
        {
            foreach (GamePlayer grpplayer in player.Group.GetPlayersInTheGroup())
            {
                if (player.IsWithinRadius(grpplayer, m_range, true) && grpplayer.IsAlive)
                    targets.Add(grpplayer);
            }
        }

        SendCastMessage(player);

        bool AtLeastOneEffectRemoved = false;
        foreach (GamePlayer target in targets)
        {
            new StatBuffEcsSpellEffect(new EcsGameEffectInitParams(target, 30000, 1, m_handler));
            foreach (GamePlayer pl in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                pl.Out.SendSpellEffectAnimation(target,target,4317,20000,false,1);
                pl.Out.SendSpellEffectAnimation(target,target,5208,20000,false,1);
            }
        }

        DisableSkill(living);
    }

    public virtual SpellHandler CreateSpell(GameLiving caster)
    {
        m_dbspell = new DbSpell();
        m_dbspell.Name = "Brilliant Aura of Deflection";
        m_dbspell.Icon = 7149;
        m_dbspell.ClientEffect = 7009;
        m_dbspell.Damage = 0;
        m_dbspell.DamageType = 0;
        m_dbspell.Target = "Realm";
        m_dbspell.Radius = 0;
        m_dbspell.Type = ESpellType.AllMagicResistBuff.ToString();
        m_dbspell.Value = 36;
        m_dbspell.Duration = 30;
        m_dbspell.Pulse = 0;
        m_dbspell.PulsePower = 0;
        m_dbspell.Power = 0;
        m_dbspell.CastTime = 0;
        m_dbspell.EffectGroup = 0;
        m_dbspell.Frequency = 0;
        m_dbspell.Range = 1500;
        m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
        m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        return new SpellHandler(caster, m_spell, m_spellline);
    }
    
    private DbSpell m_dbspell;
    private Spell m_spell = null;
    private SpellLine m_spellline;

}
