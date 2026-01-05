using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.Spells;

namespace DOL.GS.Commands
{
    [CmdAttribute("&buff", ePrivLevel.Player, "Buff the target", "/buff <(buffList) | all | help> [playerName | npcName]")]
    public class BuffCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const int RANGE = 1500;

        private static Dictionary<string, eSpellType> _buffLookupTable;

        static BuffCommandHandler()
        {
            // Doesn't handle Necromancer buffs.
            _buffLookupTable = new()
            {
                {"str", eSpellType.StrengthBuff},
                {"con", eSpellType.ConstitutionBuff},
                {"dex", eSpellType.DexterityBuff},
                {"sc", eSpellType.StrengthConstitutionBuff},
                {"dq", eSpellType.DexterityQuicknessBuff},
                {"acu", eSpellType.AcuityBuff},
                {"af", eSpellType.BaseArmorFactorBuff},
                {"saf", eSpellType.SpecArmorFactorBuff},
                {"paf", eSpellType.PaladinArmorFactorBuff},
                {"abs", eSpellType.ArmorAbsorptionBuff},
                {"cel", eSpellType.CelerityBuff},
                {"has", eSpellType.CombatSpeedBuff},
                {"da", eSpellType.DamageAdd},
                {"ds", eSpellType.DamageShield},
                {"dam", eSpellType.MeleeDamageBuff},
                {"hreg", eSpellType.HealthRegenBuff},
                {"hot", eSpellType.HealOverTime},
                {"preg", eSpellType.PowerRegenBuff},
                {"pot", eSpellType.PowerOverTime},
                {"ereg", eSpellType.EnduranceRegenBuff},
                {"fat", eSpellType.FatigueConsumptionBuff},
                {"mez", eSpellType.MesmerizeDurationBuff},
                {"bt", eSpellType.Bladeturn},
                {"dproc", eSpellType.DefensiveProc}, // Won't work well if the player has more than one.
                {"oproc", eSpellType.OffensiveProc}, // Won't work well if the player has more than one.
                {"hcm", eSpellType.HeatColdMatterBuff},
                {"cold", eSpellType.ColdResistBuff},
                {"matt", eSpellType.MatterResistBuff},
                {"heat", eSpellType.HeatResistBuff},
                {"bse", eSpellType.BodySpiritEnergyBuff},
                {"body", eSpellType.BodyResistBuff},
                {"spir", eSpellType.SpiritResistBuff},
                {"ener", eSpellType.EnergyResistBuff}
            };
        }

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "buff"))
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            if (!IsInAllowedArea())
            {
                ChatUtil.SendSystemMessage(client.Player, $"This command cannot be used here. You must be within 2500 units of a friendly keep, or within a main city, border keep, or a housing area.");
                return;
            }

            string targetName = null;
            GameLiving target;

            if (args.Length < 3)
            {
                if (args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    StringBuilder stringBuilder = new();

                    foreach (var pair in _buffLookupTable)
                        stringBuilder.Append($"{pair.Key}  ({pair.Value})\n");

                    ChatUtil.SendSystemMessage(client, stringBuilder.ToString());
                    ChatUtil.SendSystemMessage(client, "This command expects a buff list.\nUse these shortcuts separated by a single comma to build one.");
                    return;
                }
                else
                {
                    target = client.Player.TargetObject as GameLiving;

                    if (target != null)
                        targetName = target.Name;
                }
            }
            else
            {
                targetName = args[2];
                target = ClientService.Instance.GetPlayerByPartialName(targetName, out _);

                if (target == null)
                {
                    foreach (GameNPC npcInRadius in client.Player.GetNPCsInRadius(RANGE))
                    {
                        if (!npcInRadius.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        target = npcInRadius;
                        break;
                    }
                }

                if (target != null)
                {
                    client.Out.SendChangeTarget(target);
                    client.Player.TargetObject = target;
                }
            }

            if (target == null || !client.Player.IsWithinRadius(target, RANGE) || GameServer.ServerRules.IsAllowedToAttack(client.Player, target, true))
            {
                if (string.IsNullOrEmpty(targetName))
                    ChatUtil.SendSystemMessage(client.Player, $"You need a target!");
                else
                    ChatUtil.SendSystemMessage(client.Player, $"You don't see {targetName} around here!");

                return;
            }

            // Cast group buffs only if the target is in the same group or if we're targeting ourselves.
            bool sameGroup = client.Player.Group == null ? client.Player == target : client.Player.Group == target.Group;
            bool isOwnPet = target is GameNPC targetNpc && targetNpc.Brain is ControlledMobBrain brain && brain.GetPlayerOwner() == client.Player;
            string[] buffKeys = args[1].Split(',');
            bool isCastingEveryBuff = buffKeys[0].Equals("all", StringComparison.OrdinalIgnoreCase);

            if (isCastingEveryBuff)
                buffKeys = _buffLookupTable.Keys.ToArray();

            List<(Spell, SpellLine)> buffsToCast = new(buffKeys.Length);
            List<(Skill, Skill)> useableSkills = client.Player.GetAllUsableSkills();
            List<(SpellLine, List<Skill>)> useableLists = client.Player.GetAllUsableListSpells();

            foreach (string buffKey in buffKeys)
            {
                (Spell spell, SpellLine spellLine) strongestSpell = default;

                if (!_buffLookupTable.TryGetValue(buffKey, out eSpellType buffType))
                {
                    ChatUtil.SendSystemMessage(client.Player, $"\"{buffKey}\" is not a valid shortcut.");
                    continue;
                }

                foreach ((Skill, Skill) useableSkill in useableSkills)
                {
                    Spell useableSpell = useableSkill.Item1 as Spell;

                    if (CheckSpell(useableSpell, strongestSpell.spell, buffType))
                        strongestSpell = (useableSpell, useableSkill.Item2 as SpellLine);
                }

                foreach ((SpellLine, List<Skill>) useableList in useableLists)
                {
                    foreach (Skill useableSkill in useableList.Item2)
                    {
                        Spell useableSpell = useableSkill as Spell;

                        if (CheckSpell(useableSpell, strongestSpell.spell, buffType))
                            strongestSpell = (useableSpell, useableList.Item1);
                    }
                }

                if (strongestSpell.spell == null)
                {
                    if (!isCastingEveryBuff)
                        ChatUtil.SendSystemMessage(client.Player, $"\"{buffKey}\" doesn't match any buff you can cast.");
                }
                else
                    buffsToCast.Add(strongestSpell);
            }

            if (buffsToCast.Count == 0)
                return;

            BuffCommandSpell buffCommandSpell = CreateBuffCommandSpell(buffsToCast);
            SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            client.Player.CastSpell(buffCommandSpell, spellLine);

            bool IsInAllowedArea()
            {
                if ((ePrivLevel) client.Account.PrivLevel > ePrivLevel.Player)
                    return true;

                // Camelot, Tir na Nog, Jordheim.
                if (client.Player.CurrentRegionID is 10 or 101 or 201)
                    return true;

                if (client.Player.CurrentRegion.HousingEnabled)
                    return true;

                foreach (AbstractArea area in client.Player.CurrentAreas.OfType<AbstractArea>().ToList())
                {
                    switch (area.Description)
                    {
                        case "Castle Sauvage":
                        case "Snowdonia Fortress":
                        case "Svasud Faste":
                        case "Vindsaul Faste":
                        case "Druim Ligen":
                        case "Druim Cain":
                            return true;
                    }
                }

                // The radius should be large enough for players to be able to cast from outside.
                AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, 2500);

                if (keep != null && keep.IsPortalKeep)
                    return true;

                return false;
            }

            bool CheckSpell(Spell spell, Spell strongestSpell, eSpellType buffType)
            {
                // Ignore pulsing spells, spells with a cooldown, non-concentration spells with a short duration (5 minutes).
                if (spell == null ||
                    spell.SpellType != buffType ||
                    spell.IsPulsing ||
                    spell.RecastDelay > 0 ||
                    (!spell.IsConcentration && spell.Duration <= 300000))
                {
                    return false;
                }

                switch (spell.Target)
                {
                    case eSpellTarget.SELF:
                    {
                        if (client.Player != target)
                            return false;

                        break;
                    }
                    case eSpellTarget.GROUP:
                    {
                        if (!sameGroup)
                            return false;

                        break;
                    }
                    case eSpellTarget.PET:
                    {
                        if (!isOwnPet)
                            return false;

                        break;
                    }
                    case eSpellTarget.REALM:
                        break;
                    default:
                        return false;
                }

                return strongestSpell == null || spell.Value > strongestSpell.Value || spell.Damage > strongestSpell.Damage;
            }

            BuffCommandSpell CreateBuffCommandSpell(List<(Spell, SpellLine)> buffsToCast)
            {
                DbSpell dbSpell = new()
                {
                    Name = "Buff Command",
                    ClientEffect = 11325, // A random ID. Maybe use one that matches the player's realm if we want to be fancy?
                    DamageType = (int) eDamageType.Natural,
                    Target = eSpellTarget.REALM.ToString(),
                    Type = eSpellType.BuffCommand.ToString(),
                    CastTime = 3,
                    Range = RANGE
                };

                return new BuffCommandSpell(dbSpell, 0, buffsToCast);
            }
        }
    }

    public class BuffCommandSpell : Spell
    {
        public List<(Spell, SpellLine)> BuffsToCast { get; }

        public BuffCommandSpell(DbSpell dbspell, int requiredLevel, List<(Spell, SpellLine)> buffsToCast) : base(dbspell, requiredLevel)
        {
            BuffsToCast = buffsToCast;
        }
    }

    [SpellHandlerAttribute(eSpellType.BuffCommand)]
    public class BuffCommandSpellHandler : SingleStatBuff
    {
        public BuffCommandSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override eProperty Property1 => eProperty.Undefined;

        public override void OnDirectEffect(GameLiving target)
        {
            BuffCommandSpell buffCommandSpell = Spell as BuffCommandSpell;

            if (buffCommandSpell == null)
                return;

            foreach ((Spell spell, SpellLine spellLine) in buffCommandSpell.BuffsToCast)
            {
                Spell clonedSpell = spell.Clone() as Spell;
                clonedSpell.CastTime = 0;
                SpellHandler spellHandler = ScriptMgr.CreateSpellHandler(Caster, clonedSpell, spellLine) as SpellHandler;
                spellHandler.Target = target;
                spellHandler.Tick();
            }
        }
    }
}
