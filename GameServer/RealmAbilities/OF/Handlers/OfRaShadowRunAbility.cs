using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class OfRaShadowRunAbility : TimedRealmAbility
{
    public OfRaShadowRunAbility(DbAbility dba, int level) : base(dba, level) { }
    
    int m_duration = 30000; // 30s

    public override int MaxLevel { get { return 1; } }
    public override int CostForUpgrade(int level) { return 10; }
    public override int GetReUseDelay(int level) { return 1800; } // 30 mins

    public override void Execute(GameLiving living)
    {
        GamePlayer player = living as GamePlayer;
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

        DisableSkill(living);

        new OfRaShadowRunEcsEffect(new EcsGameEffectInitParams(player, m_duration, 1, CreateSpell(living)));
    }
    
    private SpellHandler CreateSpell(GameLiving owner)
    {
        DbSpell tmpSpell = new DbSpell();
        tmpSpell.Name = "Shadow Run";
        tmpSpell.Icon = 4278;
        tmpSpell.ClientEffect = 7014;
        tmpSpell.Damage = 0;
        tmpSpell.DamageType = 0;
        tmpSpell.Target = "Self";
        tmpSpell.Radius = 0;
        tmpSpell.Type = ESpellType.OffensiveProc.ToString();
        tmpSpell.Value = 0;
        tmpSpell.Duration = 30;
        tmpSpell.Pulse = 0;
        tmpSpell.PulsePower = 0;
        tmpSpell.Power = 0;
        tmpSpell.CastTime = 0;
        tmpSpell.EffectGroup = 0; // stacks with other damage adds
        tmpSpell.Range = 0;
        tmpSpell.Description = "Move at double your normal stealthed movement rate.";
        SpellLine spellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        SpellHandler tmpHandler = new SpellHandler(owner, new Spell(tmpSpell, 0) , spellLine); // make spell level 0 so it bypasses the spec level adjustment code
        return tmpHandler;
    }
    
    public override IList<string> DelveInfo
    {
        get
        {
            var delveInfoList = new List<string>();
            delveInfoList.Add("Move at double your normal stealthed movement rate.");
            delveInfoList.Add("Casting time: instant");

            return delveInfoList;
        }
    }
    
    public override void AddEffectsInfo(IList<string> list)
    {
        list.Add("Move at double your normal stealthed movement rate.");
        list.Add("Casting time: instant");
    }
}