using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS.Spells
{
    public abstract class SpellDelve
    {
        private static FrozenDictionary<eDamageType, int> _damageTypeToIdMap =
            new Dictionary<eDamageType, int>()
        {
            {eDamageType.Crush, 1 },
            {eDamageType.Slash, 2 },
            {eDamageType.Thrust, 3 },
            {eDamageType.Heat, 10 },
            {eDamageType.Spirit, 11 },
            {eDamageType.Cold, 12 },
            {eDamageType.Matter, 13 },
            {eDamageType.Body, 16 },
            {eDamageType.Energy, 20 }
        }.ToFrozenDictionary();

        public static string GetSpellString(ISpellHandler spellHandler)
        {
            Spell spell = spellHandler.Spell;

            return ClientDelve.Create("Spell")
                .AddElement("Function", "light")
                .AddElement("Index", unchecked((ushort) spell.InternalID))
                .AddElement("Name", spell.Name)
                .AddElementIf(spell.CastTime >= 2000, "cast_timer", spell.CastTime - 2000)
                .AddElementIf(spell.CastTime == 0, "instant", "1")
                .AddElement("damage_type", GetMagicTypeId(spell))
                .AddElement("level", spell.Level)
                .AddElement("power_cost", spell.Power)
                .AddElement("cost_type", GetCostTypeId(spellHandler.CostType))
                .AddElement("range", spell.Range)
                .AddElement("duration", spell.Duration / 1000.0)
                .AddElement("dur_type", GetDurationType(spell))
                .AddElement("timer_value", spell.RecastDelay / 1000.0)
                .AddElement("target", GetSpellTargetType(spell.Target))
                .AddElement("radius", spell.Radius)
                .AddElement("concentration_points", spell.Concentration)
                .AddElement("frequency", spell.Frequency)
                .AddElement("delve_string", GetSpellDescription(spellHandler, spell))
                .Finalize();
        }

        public static string GetSongString(ISpellHandler spellHandler)
        {
            Spell spell = spellHandler.Spell;

            return ClientDelve.Create("Song")
                .AddElement("Index", unchecked((short) spell.InternalID))
                .AddElement("effect", unchecked((short) spell.InternalID))
                .AddElement("Name", spell.Name)
                .Finalize();
        }

        public static string GetStyleString(GameClient client, Style style)
        {
            List<Style> allStyles = null;

            foreach (var spec in client.Player.GetSpecList())
            {
                allStyles ??= new();
                allStyles.AddRange(spec.PretendStylesForLiving(client.Player, GamePlayer.MAX_LEVEL));
            }

            if (allStyles == null)
                return string.Empty;

            List<object> followupStyles = null;

            foreach (var s in allStyles)
            {
                if (s.OpeningRequirementType is Style.eOpening.Offensive &&
                    s.AttackResultRequirement is Style.eAttackResultRequirement.Style &&
                    s.OpeningRequirementValue == style.ID)
                {
                    followupStyles ??= new();
                    followupStyles.Add(s.Name);
                }
            }

            string openingStyleName = null;

            if (style.OpeningRequirementType is Style.eOpening.Offensive && style.AttackResultRequirement is Style.eAttackResultRequirement.Style)
            {
                Style openingStyle = allStyles.FirstOrDefault(s => s.ID == style.OpeningRequirementValue);

                if (openingStyle != null)
                    openingStyleName = openingStyle.Name;
            }

            StyleProcInfo selectedProc = null;

            if (style.Procs != null && style.Procs.Count > 0)
            {
                selectedProc = style.Procs.FirstOrDefault(p => p.ClassId == client.Player.CharacterClass.ID);
                selectedProc ??= style.Procs.FirstOrDefault(p => p.ClassId == 0);
            }

            // Build the delve string using the fluent API
            return ClientDelve.Create("Style")
                .AddElement("Index", unchecked((short) style.InternalID))
                .AddElementIf(openingStyleName != null, "OpeningStyle", openingStyleName)
                .AddElementIf(followupStyles != null, "FollowupStyle", followupStyles)
                .AddElement("Name", style.Name)
                .AddElement("Icon", style.Icon)
                .AddElement("Level", style.Level)
                .AddElement("Fatigue", style.EnduranceCost)
                .AddElement("DefensiveMod", style.BonusToDefense)
                .AddElement("AttackMod", style.BonusToHit)
                .AddElement("OpeningDamage", style.GrowthRate * 200)
                .AddElement("OpeningType", (int) style.OpeningRequirementType)
                .AddElement("OpeningResult", (int) style.AttackResultRequirement)
                .AddElement("Hidden", style.StealthRequirement)
                .AddElementIf(style.OpeningRequirementType is Style.eOpening.Positional, "OpeningNumber", style.OpeningRequirementValue)
                .AddElementIf(style.WeaponTypeRequirement > 0, "Weapon", style.GetRequiredWeaponName())
                .AddElementIf(selectedProc != null, "SpecialNumber", selectedProc?.Spell.InternalID)
                .AddElementIf(selectedProc != null, "SpecialType", 1)
                .Finalize();
        }

        private static string GetSpellDescription(ISpellHandler spellHandler, Spell spell)
        {
            if (spell.SubSpellID == 0)
                return spellHandler.ShortDescription;

            Spell subSpell = SkillBase.GetSpellByID(spell.SubSpellID);

            if (subSpell == null)
                return spellHandler.ShortDescription;

            ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(spellHandler.Caster, subSpell, null);

            if (subSpellHandler == null)
                return spellHandler.ShortDescription;

            StringBuilder builder = new();
            builder.Append($"{spellHandler.ShortDescription}\n\n");

            foreach (string line in subSpellHandler.DelveInfo)
                builder.Append($"{line}\n");

            return builder.ToString();
        }

        private static int GetSpellTargetType(eSpellTarget spellTarget)
        {
            return spellTarget switch
            {
                eSpellTarget.REALM => 7,
                eSpellTarget.SELF => 0,
                eSpellTarget.ENEMY or eSpellTarget.CONE => 1,
                eSpellTarget.PET or eSpellTarget.CONTROLLED => 6,
                eSpellTarget.GROUP => 3,
                eSpellTarget.AREA => 9,
                eSpellTarget.CORPSE => 8,
                _ => 0,
            };
        }

        private static int GetDurationType(Spell spell)
        {
            if (spell.Duration > 0)
                return 2;

            if (spell.Concentration > 0)
                return 4;

            return 0;
        }

        private static int GetCostTypeId(SpellCostType costType)
        {
            return costType switch
            {
                SpellCostType.Health => 2,
                SpellCostType.Endurance => 3,
                _ => 0,
            };
        }

        private static int GetMagicTypeId(Spell spell)
        {
            return _damageTypeToIdMap.TryGetValue(spell.DamageType, out int damageTypeId) ? damageTypeId : 0;
        }
    }
}
