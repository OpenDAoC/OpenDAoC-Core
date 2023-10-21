namespace Core.GS.SpellEffects
{
    public interface IEffectComponent
    {
        ESpellEffect Type { get; set; }
        ushort SpellEffectId { get; set; }
    }
}