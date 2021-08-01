namespace DOL.GS.SpellEffects
{
    public interface IEffectComponent
    {
        eSpellEffect Type { get; set; }
        ushort SpellEffectId { get; set; }
    }
}