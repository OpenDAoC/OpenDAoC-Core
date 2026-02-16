using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    // Command /buff [target|class] [buffs...]
    // The command only works when not in a rvr zone or near a portal keep / NF village, also works at entrance of darkness falls
    [CmdAttribute("&buff", ePrivLevel.Player, "Buff target or class", "/buff <target | class> <buffs...>")]
    public class BuffCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private const int RANGE = 1500;
        private static Dictionary<string, eSpellType> _buffLookupTable;

        static BuffCommandHandler()
        {
            _buffLookupTable = new(StringComparer.OrdinalIgnoreCase)
                {
                    {"str", eSpellType.StrengthBuff},
                    {"con", eSpellType.ConstitutionBuff},
                    {"dex", eSpellType.DexterityBuff},
                    {"sc", eSpellType.StrengthConstitutionBuff},
                    {"dq", eSpellType.DexterityQuicknessBuff},
                    {"acu", eSpellType.AcuityBuff},
                    {"af", eSpellType.BaseArmorFactorBuff},
                    {"saf", eSpellType.SpecArmorFactorBuff},
                    {"haste", eSpellType.CombatSpeedBuff},
                    {"da", eSpellType.DamageAdd},
                    {"ds", eSpellType.DamageShield},
                    {"abs", eSpellType.ArmorAbsorptionBuff},
                    {"reg", eSpellType.HealthRegenBuff},
                    {"end", eSpellType.EnduranceRegenBuff},
                    {"pom", eSpellType.PowerRegenBuff},
                    {"piercing", eSpellType.BodyResistBuff}
                };
        }

        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "buff"))
                return;

            // Syntax Check
            if (args.Length < 3)
            {
                if (args.Length == 2 && args[1].Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    StringBuilder sb = new();
                    foreach (var pair in _buffLookupTable) sb.Append($"{pair.Key} ");
                    ChatUtil.SendSystemMessage(client, "Available shortcuts: \n" + sb.ToString());
                    return;
                }
                DisplaySyntax(client);
                ChatUtil.SendSystemMessage(client, "Example: /buff target <buffs (dq, sc, acu, dex, con, str, af, saf, haste, da, ds, abs, reg, end, pom, piercing)>");
                ChatUtil.SendSystemMessage(client, "Example: /buff <class (while grouped)> <buffs (dq, sc, acu, dex, con, str, af, saf, haste, da, ds, abs, reg, end, pom, piercing)>");
                ChatUtil.SendSystemMessage(client, "Example target/pet/self: /buff target dex con saf will buff base dex, base con and spec af to your target");
                ChatUtil.SendSystemMessage(client, "Example class, only works while grouped: /buff shaman dex con will buff base dex and base con to every shaman in your group");
                ChatUtil.SendSystemMessage(client, "Add the 'low' prefix to any buff name to use the 2nd highest buff tier (ie: lowdxq, lowsc, lowstr, lowdex, etc)");
                return;
            }

            // Area Check (Original Logic)
            if (!IsInAllowedArea(client))
            {
                ChatUtil.SendSystemMessage(client.Player, $"This command cannot be used in a frontier area.");
                return;
            }

            string selector = args[1].ToLower();

            // Allow comma separated OR space separated buffs
            // If user types: /buff target str con dex -> args length is 5
            // If user types: /buff target str,con,dex -> args length is 3
            List<string> rawBuffKeys = new List<string>();
            if (args[2].Contains(","))
            {
                rawBuffKeys.AddRange(args[2].Split(','));
            }
            else
            {
                for (int i = 2; i < args.Length; i++) rawBuffKeys.Add(args[i]);
            }

            List<GameLiving> targets = new();

            // 1. Target Identification Logic
            if (selector == "target")
            {
                GameLiving t = client.Player.TargetObject as GameLiving;
                if (t == null)
                {
                    client.Player.Out.SendMessage("You must select a target for this spell.", eChatType.CT_SpellResisted, eChatLoc.CL_ChatWindow);
                    return;
                }

                if (client.Player.IsWithinRadius(t, RANGE))
                {
                    targets.Add(t);
                }
                else
                {
                    client.Player.Out.SendMessage("Target is too far away.", eChatType.CT_SpellResisted, eChatLoc.CL_ChatWindow);
                    return;
                }
            }
            else if (selector == "self")
            {
                GameLiving t = client.Player;
                targets.Add(t);
            }
            else
            {
                // Class Logic
                if (client.Player.Group == null)
                {
                    ChatUtil.SendSystemMessage(client.Player, "You are not in a group.");
                    return;
                }

                foreach (GamePlayer p in client.Player.Group.GetMembersInTheGroup())
                {
                    if (p == null || p.CharacterClass == null) continue;

                    if (p.CharacterClass.Name.Equals(selector, StringComparison.OrdinalIgnoreCase))
                    {
                        if (client.Player.IsWithinRadius(p, RANGE))
                        {
                            targets.Add(p);
                        }
                    }
                }
            }

            if (targets.Count == 0)
            {
                ChatUtil.SendSystemMessage(client.Player, "No valid targets found (wrong class name or out of range).");
                return;
            }

            // 2. Prepare Skills (Get all available spells)
            List<(Skill, Skill)> useableSkills = client.Player.GetAllUsableSkills();
            List<(SpellLine, List<Skill>)> useableLists = client.Player.GetAllUsableListSpells();

            bool isCastingEveryBuff = rawBuffKeys.Count > 0 && rawBuffKeys[0].Equals("all", StringComparison.OrdinalIgnoreCase);
            if (isCastingEveryBuff)
            {
                rawBuffKeys = _buffLookupTable.Keys.ToList();
            }

            // 3. Process each target
            foreach (GameLiving currentTarget in targets)
            {
                List<(Spell, SpellLine)> buffsToCast = new();
                bool sameGroup = client.Player.Group == null ? client.Player == currentTarget : client.Player.Group == currentTarget.Group;
                bool isOwnPet = currentTarget is GameNPC targetNpc && targetNpc.Brain is ControlledMobBrain brain && brain.GetPlayerOwner() == client.Player;

                foreach (string rawKey in rawBuffKeys)
                {
                    string key = rawKey.Trim().ToLower();
                    if (string.IsNullOrEmpty(key)) continue;

                    bool useLowTier = key.StartsWith("low");
                    string lookupKey = useLowTier ? key.Substring(3) : key;

                    if (!_buffLookupTable.TryGetValue(lookupKey, out eSpellType buffType))
                    {
                        if (!isCastingEveryBuff)
                            ChatUtil.SendSystemMessage(client.Player, $"\"{key}\" is not a valid shortcut.");
                        continue;
                    }

                    // Find all matching spells for this type
                    bool isSpecAFSearch = lookupKey == "saf";
                    List<(Spell spell, SpellLine line)> possibleSpells = new();
                    bool IsMatchingType(Spell s)
                    {
                        if (s.SpellType == buffType) return true;
                        if (isSpecAFSearch && s.SpellType == eSpellType.PaladinArmorFactorBuff) return true;
                        return false;
                    }

                    // Check Single Skills
                    foreach ((Skill skill, Skill line) in useableSkills)
                    {
                        Spell s = skill as Spell;
                        if (s != null && IsMatchingType(s) && CheckSpell(s, s.SpellType, client.Player, currentTarget, sameGroup, isOwnPet))
                        {
                            possibleSpells.Add((s, line as SpellLine));
                        }
                    }

                    // Check List Skills
                    foreach ((SpellLine line, List<Skill> skills) in useableLists)
                    {
                        foreach (Skill skill in skills)
                        {
                            Spell s = skill as Spell;
                            if (s != null && IsMatchingType(s) && CheckSpell(s, s.SpellType, client.Player, currentTarget, sameGroup, isOwnPet))
                            {
                                possibleSpells.Add((s, line));
                            }
                        }
                    }

                    // Select the best (or 2nd best) spell
                    if (possibleSpells.Count > 0)
                    {
                        // Order by Level descending (High to Low)
                        var sortedSpells = possibleSpells.OrderByDescending(x => x.spell.Level).ToList();

                        if (useLowTier && sortedSpells.Count > 1)
                        {
                            // User requested "low", give them the 2nd best
                            buffsToCast.Add(sortedSpells[1]);
                        }
                        else
                        {
                            // Give the best
                            buffsToCast.Add(sortedSpells[0]);
                        }
                    }
                    else
                    {
                        if (!isCastingEveryBuff)
                            ChatUtil.SendSystemMessage(client.Player, $"You have no spell for \"{lookupKey}\" that works on {currentTarget.Name}.");
                    }
                }

                if (buffsToCast.Count > 0)
                {
                    BuffCommandSpell buffCommandSpell = CreateBuffCommandSpell(buffsToCast);
                    SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);

                    GameLiving originalTarget = client.Player.TargetObject as GameLiving;
                    client.Player.TargetObject = currentTarget;
                    client.Player.CastSpell(buffCommandSpell, spellLine);
                }
            }
        }

        bool IsInAllowedArea(GameClient client)
        {
            // Admins allow always
            if ((ePrivLevel)client.Account.PrivLevel > ePrivLevel.Player)
                return true;

            GamePlayer player = client.Player;

            // If zone is not frontier we allow everywhere
            if (!player.CurrentRegion.IsRvR)
                return true;

            if (player.CurrentRegionID == 163)
            {
                foreach (AbstractArea area in player.CurrentAreas.OfType<AbstractArea>())
                {
                    switch (area.Description)
                    {
                        // Borderkeeps & Relic Towns
                        case "Castle Sauvage":
                        case "Snowdonia Fortress":
                        case "Svasud Faste":
                        case "Vindsaul Faste":
                        case "Druim Ligen":
                        case "Druim Cain":
                        case "Crair Treflan":
                        case "Magh Tuireadh":
                        case "Godrborg":
                        case "Rensamark":
                        case "Catterick Hamlet":
                        case "Dinas Emrys":
                            return true;
                    }
                }
            }
            else
            {
                // Battlegrounds
                DbBattleground bg = GameServer.KeepManager.GetBattleground(player.CurrentRegionID);
                if (bg != null)
                {
                    // Allow at portal keeps
                    AbstractGameKeep bgKeep = GameServer.KeepManager.GetClosestKeepToSpot(player.CurrentRegionID, player, 2500);
                    if (bgKeep != null && bgKeep.IsPortalKeep)
                        return true;
                }

                // Enable buffing near df entrance
                foreach (GameObject obj in player.GetNPCsInRadius(2000))
                {
                    if (obj is GameNPC npc && npc.GuildName == "Darkness Falls Explorer")
                    {
                        return true; 
                    }
                }
            }
            return false;
        }

        // Updated signature to handle context (caster, target, group status)
        bool CheckSpell(Spell spell, eSpellType buffType, GamePlayer caster, GameLiving target, bool sameGroup, bool isOwnPet)
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
                        if (caster != target) return false;
                        break;
                    }
                case eSpellTarget.GROUP:
                    {
                        if (!sameGroup) return false;
                        break;
                    }
                case eSpellTarget.PET:
                    {
                        if (!isOwnPet) return false;
                        break;
                    }
                case eSpellTarget.REALM:
                    break;
                default:
                    return false;
            }

            return true;
        }

        BuffCommandSpell CreateBuffCommandSpell(List<(Spell, SpellLine)> buffsToCast)
        {
            DbSpell dbSpell = new()
            {
                Name = "Buff Command",
                ClientEffect = 11325,
                DamageType = (int)eDamageType.Natural,
                Target = eSpellTarget.REALM.ToString(),
                Type = eSpellType.BuffCommand.ToString(),
                CastTime = 0, // Make it instant to allow smooth multi-target buffing
                Range = RANGE
            };

            return new BuffCommandSpell(dbSpell, 0, buffsToCast);
        }
    }

    // --- Spell & Handler Classes ---

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

        // FIX: Implements the abstract Property1 required by SingleStatBuff
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
                if (spellHandler != null)
                {
                    spellHandler.Target = target;
                    // Tick() applies the spell immediately without casting bar
                    spellHandler.Tick();
                }
            }
        }
    }
}