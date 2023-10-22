using System;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

[SpellHandler("Decoy")]
public class DecoySpell : SpellHandler
{
    private GameDecoy decoy;
    private GameSpellEffect m_effect;

    /// <summary>
    /// Execute Decoy summon spell
    /// </summary>
    /// <param name="target"></param>
    public override void FinishSpellCast(GameLiving target)
    {
        m_caster.Mana -= PowerCost(target);
        base.FinishSpellCast(target);
    }

    public override bool IsOverwritable(EcsGameSpellEffect compare)
    {
        return false;
    }

    public override void ApplyEffectOnTarget(GameLiving target)
    {
        GameSpellEffect neweffect = CreateSpellEffect(target, Effectiveness);
        decoy.AddToWorld();
        neweffect.Start(decoy);
    }

    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);
        m_effect = effect;
        if (effect.Owner == null || !effect.Owner.IsAlive)
            return;
        GameEventMgr.AddHandler(decoy, GameLivingEvent.Dying, new CoreEventHandler(DecoyDied));
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        GameEventMgr.RemoveHandler(decoy, GameLivingEvent.Dying, new CoreEventHandler(DecoyDied));
        if (decoy != null)
        {
            decoy.Health = 0;
            decoy.Delete();
        }

        return base.OnEffectExpires(effect, noMessages);
    }

    private void DecoyDied(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc kDecoy = sender as GameNpc;
        if (kDecoy == null) return;
        if (e == GameLivingEvent.Dying)
        {
            MessageToCaster("Your Decoy has fallen!", EChatType.CT_SpellExpires);
            OnEffectExpires(m_effect, true);
            return;
        }
    }

    public DecoySpell(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line)
    {
        Random m_rnd = new Random();
        decoy = new GameDecoy();
        //Fill the object variables
        decoy.CurrentRegion = caster.CurrentRegion;
        decoy.Heading = (ushort)((caster.Heading + 2048) % 4096);
        decoy.Level = 50;
        decoy.Realm = caster.Realm;
        decoy.X = caster.X;
        decoy.Y = caster.Y;
        decoy.Z = caster.Z;
        string TemplateId = "";
        switch (caster.Realm)
        {
            case ERealm.Albion:
                decoy.Name = "Avalonian Unicorn Knight";
                decoy.Model = (ushort)m_rnd.Next(61, 68);
                TemplateId = "e3ead77b-22a7-4b7d-a415-92a29295dcf7";
                break;
            case ERealm.Midgard:
                decoy.Name = "Kobold Elding Herra";
                decoy.Model = (ushort)m_rnd.Next(169, 184);
                TemplateId = "ee137bff-e83d-4423-8305-8defa2cbcd7a";
                break;
            case ERealm.Hibernia:
                decoy.Name = "Elf Gilded Spear";
                decoy.Model = (ushort)m_rnd.Next(334, 349);
                TemplateId = "a4c798a2-186a-4bda-99ff-ccef228cb745";
                break;
        }

        GameNpcInventoryTemplate load = new GameNpcInventoryTemplate();
        if (load.LoadFromDatabase(TemplateId))
        {
            decoy.EquipmentTemplateID = TemplateId;
            decoy.Inventory = load;
            decoy.BroadcastLivingEquipmentUpdate();
        }

        decoy.CurrentSpeed = 0;
        decoy.GuildName = "";
    }
}