using System;

namespace DOL.GS.Spells
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SpellHandlerAttribute : Attribute
    {
        public eSpellType SpellType { get; }

        public SpellHandlerAttribute(eSpellType spellType)
        {
            SpellType = spellType;
        }
    }
}
