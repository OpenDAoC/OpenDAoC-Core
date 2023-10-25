using System.Reflection;
using Core.GS.GameLoop;
using Core.GS.Scripts;
using Core.GS.Skills;
using Core.GS.Spells;
using log4net;

namespace Core.GS.AI;

public class BomberBrain : ControlledNpcBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private Spell _spell;
    private SpellLine _spellLine;
    private long _expireTime = GameLoopMgr.GameLoopTime + 60 * 1000;

    public BomberBrain(GameLiving owner, Spell spell, SpellLine spellLine) : base(owner)
    {
        _spell = spell;
        _spellLine = spellLine;
    }

    public override int ThinkInterval => 300;

    protected override bool CheckDefensiveSpells(Spell spell)
    {
        return true;
    }

    protected override bool CheckOffensiveSpells(Spell spell)
    {
        return true;
    }

    public override void Think()
    {
        if (GameLoopMgr.GameLoopTime >= _expireTime)
            Body.Delete();

        if (Body.IsWithinRadius(Body.TargetObject, 150))
            DeliverPayload();
    }

    private void DeliverPayload()
    {
        Spell subSpell = SkillBase.GetSpellByID(_spell.SubSpellID);

        if (subSpell == null)
        {
            if (log.IsErrorEnabled && subSpell == null)
                log.Error("Bomber SubspellID for Bomber SpellID: " + _spell.ID + " is not implemented yet");

            Body.Health = 0;
            Body.Delete();
            return;
        }

        subSpell.Level = _spell.Level;

        if (Body.IsWithinRadius(Body.TargetObject, 350))
        {
            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(m_owner, subSpell, SkillBase.GetSpellLine(_spellLine.KeyName));
            spellHandler.StartSpell(Body.TargetObject as GameLiving);
        }

        Body.Health = 0;
        Body.Delete();
    }

    public override void FollowOwner() { }
    public override void UpdatePetWindow() { }
}