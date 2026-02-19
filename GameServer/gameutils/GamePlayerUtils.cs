using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Styles;
using DOL.Language;

namespace DOL.GS
{
    /// <summary>
    /// GamePlayer Utils Extension Class
    /// </summary>
    public static class GamePlayerUtils
    {
        #region Spot and Area Description / Translation
        /// <summary>
        /// Get Spot Description Checking Any Area with Description or Zone Description
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="spot"></param>
        /// <returns></returns>
        public static string GetSpotDescription(this Region reg, IPoint3D spot)
        {
            return reg.GetSpotDescription(spot.X, spot.Y, spot.Z);
        }
        
        /// <summary>
        /// Get Spot Description Checking Any Area with Description or Zone Description
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static string GetSpotDescription(this Region reg, int x, int y, int z)
        {
            if (reg != null)
            {
                var area = reg.GetAreasOfSpot(x, y, z).OfType<AbstractArea>().FirstOrDefault(a => a.DisplayMessage && !string.IsNullOrEmpty(a.Description));
                
                if (area != null)
                    return area.Description;
                
                var zone = reg.GetZone(x, y);
                
                if (zone != null)
                    return zone.Description;
                
                return reg.Description;
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Get Spot Description Checking Any Area with Description or Zone Description and Try Translating it
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="client"></param>
        /// <param name="spot"></param>
        /// <returns></returns>
        public static string GetTranslatedSpotDescription(this Region reg, GameClient client, IPoint3D spot)
        {
            return reg.GetTranslatedSpotDescription(client, spot.X, spot.Y, spot.Z);
        }
        
        /// <summary>
        /// Get Spot Description Checking Any Area with Description or Zone Description and Try Translating it
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="client"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static string GetTranslatedSpotDescription(this Region reg, GameClient client, int x, int y, int z)
        {
            if (reg != null)
            {
                var area = reg.GetAreasOfSpot(x, y, z).OfType<AbstractArea>().FirstOrDefault(a => a.DisplayMessage);
                
                // Try Translate Area First
                if (area != null)
                {
                    var lng = LanguageMgr.GetTranslation(client, area) as DbLanguageArea;
                    
                    if (lng != null && !string.IsNullOrEmpty(lng.ScreenDescription))
                        return lng.ScreenDescription;
                            
                    return area.Description;
                }
                
                var zone = reg.GetZone(x, y);
                
                // Try Translate Zone
                if (zone != null)
                {
                    var lng = LanguageMgr.GetTranslation(client, zone) as DbLanguageZone;
                    if (lng != null)
                        return lng.ScreenDescription;
                    
                    return zone.Description;
                }
                
                return reg.Description;
            }
            
            return string.Empty;			
        }
        
        /// <summary>
        /// Get Player Spot Description Checking Any Area with Description or Zone Description and Try Translating it
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetTranslatedSpotDescription(this GamePlayer player)
        {
            return player.GetTranslatedSpotDescription(player.CurrentRegion, player.X, player.Y, player.Z);
        }
        
        /// <summary>
        /// Get Player Spot Description Checking Any Area with Description or Zone Description and Try Translating it
        /// </summary>
        /// <param name="player"></param>
        /// <param name="region"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static string GetTranslatedSpotDescription(this GamePlayer player, Region region, int x, int y, int z)
        {
            return player.Client.GetTranslatedSpotDescription(region, x, y, z);
        }
        
        /// <summary>
        /// Get Client Spot Description Checking Any Area with Description or Zone Description and Try Translating it
        /// </summary>
        /// <param name="client"></param>
        /// <param name="region"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static string GetTranslatedSpotDescription(this GameClient client, Region region, int x, int y, int z)
        {
            return region.GetTranslatedSpotDescription(client, x, y, z);
        }
        
        /// <summary>
        /// Get Player Spot Description Checking Any Area with Description or Zone Description 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetSpotDescription(this GamePlayer player)
        {
            return player.GetTranslatedSpotDescription();
        }
        
        /// <summary>
        /// Get Player's Bind Spot Description Checking Any Area with Description or Zone Description 
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetBindSpotDescription(this GamePlayer player)
        {
            return player.GetTranslatedSpotDescription(WorldMgr.GetRegion((ushort)player.BindRegion), player.BindXpos, player.BindYpos, player.BindZpos);
        }
        #endregion
        
        #region player skills / bonuses
        /// <summary>
        /// Updates all disabled skills to player
        /// </summary>
        public static void UpdateDisabledSkills(this GamePlayer player)
        {
            player.Out.SendDisableSkill(player.GetAllUsableSkills().Select(skt => skt.Item1).Where(sk => !(sk is Specialization))
                     .Union(player.GetAllUsableListSpells().SelectMany(sl => sl.Item2))
                     .Select(sk => new Tuple<Skill, int>(sk, player.GetSkillDisabledDuration(sk))).ToArray());
        }

        /// <summary>
        /// Reset all disabled skills to player
        /// </summary>
        public static void ResetDisabledSkills(this GamePlayer player)
        {
            foreach (Skill skl in player.GetAllDisabledSkills())
            {
                player.RemoveDisabledSkill(skl);
            }
            
            player.UpdateDisabledSkills();
        }

        /// <summary>
        /// Delve Player Bonuses for Info Window
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static ICollection<string> GetBonusesInfo(this GamePlayer player)
        {
            var info = new List<string>();
            
            /*
            <Begin Info: Bonuses (snapshot)>
            Resistances
             Crush: +25%/+0%
             Slash: +28%/+0%
             Thrust: +28%/+0%
             Heat: +25%/+0%
             Cold: +25%/+0%
             Matter: +26%/+0%
             Body: +31%/+0%
             Spirit: +21%/+0%
             Energy: +26%/+0%

            Special Item Bonuses
             +20% to Power Pool
             +3% to all Melee Damage
             +6% to all Stat Buff Spells
             +23% to all Heal Spells
             +3% to Melee Combat Speed
             +10% to Casting Speed

            Realm Rank Bonuses
             +7 to ALL Specs

            Relic Bonuses
             +20% to all Melee Damage

            Outpost Bonuses
             none

            <End Info>
             */

            //AbilityBonus[(int)((eProperty)updateResists[i])]
            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Resist"));
            info.Add(string.Format(" {2}:   {0:+0;-0}%/\t{1:+0;-0}%", player.GetModified(eProperty.Resist_Crush) - player.AbilityBonus[eProperty.Resist_Crush], player.AbilityBonus[eProperty.Resist_Crush], SkillBase.GetPropertyName(eProperty.Resist_Crush)));
            info.Add(string.Format(" {2}:    {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Slash) - player.AbilityBonus[eProperty.Resist_Slash], player.AbilityBonus[eProperty.Resist_Slash], SkillBase.GetPropertyName(eProperty.Resist_Slash)));
            info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Thrust) - player.AbilityBonus[eProperty.Resist_Thrust], player.AbilityBonus[eProperty.Resist_Thrust], SkillBase.GetPropertyName(eProperty.Resist_Thrust)));
            info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Heat) - player.AbilityBonus[eProperty.Resist_Heat], player.AbilityBonus[eProperty.Resist_Heat], SkillBase.GetPropertyName(eProperty.Resist_Heat)));
            info.Add(string.Format(" {2}:      {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Cold) - player.AbilityBonus[eProperty.Resist_Cold], player.AbilityBonus[eProperty.Resist_Cold], SkillBase.GetPropertyName(eProperty.Resist_Cold)));
            info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Matter) - player.AbilityBonus[eProperty.Resist_Matter], player.AbilityBonus[eProperty.Resist_Matter], SkillBase.GetPropertyName(eProperty.Resist_Matter)));
            info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Body) - player.AbilityBonus[eProperty.Resist_Body], player.AbilityBonus[eProperty.Resist_Body], SkillBase.GetPropertyName(eProperty.Resist_Body)));
            info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Spirit) - player.AbilityBonus[eProperty.Resist_Spirit], player.AbilityBonus[eProperty.Resist_Spirit], SkillBase.GetPropertyName(eProperty.Resist_Spirit)));
            info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Energy) - player.AbilityBonus[eProperty.Resist_Energy], player.AbilityBonus[eProperty.Resist_Energy], SkillBase.GetPropertyName(eProperty.Resist_Energy)));
            info.Add(string.Format(" {2}: {0:+0;-0}%/{1:+0;-0}%", player.GetModified(eProperty.Resist_Natural) - player.AbilityBonus[eProperty.Resist_Natural], player.AbilityBonus[eProperty.Resist_Natural], SkillBase.GetPropertyName(eProperty.Resist_Natural)));

            info.Add(" ");
            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Special"));

            //This is an Array of the bonuses that show up in the Bonus Snapshot on Live, the only ones that really need to be there.
            int[] bonusToBeDisplayed = [9, 10, 150, 151, 153, 154, 155, 173, 174, 179, 180, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 210, 247, 248, 251, 252, 253, 254];
            foreach (eProperty property in Enum.GetValues<eProperty>())
            {
                if ((player.ItemBonus[property] > 0) && (Array.BinarySearch(bonusToBeDisplayed, (int) property) >= 0)) //Tiny edit here to add the binary serach to weed out the non essential bonuses
                {
                    if (player.ItemBonus[property] != 0)
                    {
                        //LIFEFLIGHT Add, to correct power pool from showing too much
                        //This is where we need to correct the display, make it cut off at the cap if
                        //Same with hits and hits cap
                        if (property == eProperty.PowerPool)
                        {
                            int powercap = player.ItemBonus[eProperty.PowerPoolCapBonus];
                            if (powercap > 50)
                            {
                                powercap = 50;
                            }
                            int powerpool = player.ItemBonus[eProperty.PowerPool];
                            if (powerpool > 26)
                                powerpool = 26;
                            if (powerpool > powercap + 25)
                            {
                                int tempbonus = powercap + 25;
                                info.Add(ItemBonusDescription(tempbonus, (int) property));
                            }
                            else
                            {
                                int tempbonus = powerpool;
                                info.Add(ItemBonusDescription(tempbonus, (int) property));
                            }


                        }
                        else if (property == eProperty.MaxHealth)
                        {
                            int hitscap = player.ItemBonus[eProperty.MaxHealthCapBonus];
                            if (hitscap > 200)
                            {
                                hitscap = 200;
                            }
                            int hits = player.ItemBonus[eProperty.MaxHealth];
                            if (hits > hitscap + 200)
                            {
                                int tempbonus = hitscap + 200;
                                info.Add(ItemBonusDescription(tempbonus, (int) property));
                            }
                            else
                            {
                                int tempbonus = hits;
                                info.Add(ItemBonusDescription(tempbonus, (int) property));
                            }
                        }
                        else
                        {
                            info.Add(ItemBonusDescription(player.ItemBonus[property], (int) property));
                        }
                    }
                }
            }

            info.Add(" ");
            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Realm"));
            if (player.RealmLevel > 10)
                info.Add(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Specs", player.RealmLevel / 10)));
            else
                info.Add(" none");
            info.Add(" ");
            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Relic"));

            int meleeRelicBonus = (int) Math.Round((RelicMgr.GetRelicBonusModifier(player, eRelicType.Strength) - 1.0) * 100);
            int magicRelicBonus = (int) Math.Round((RelicMgr.GetRelicBonusModifier(player, eRelicType.Magic) - 1.0) * 100);

            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Melee", meleeRelicBonus));
            info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Magic", magicRelicBonus));

            // info.Add(" ");
            // info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Outpost"));
            // info.Add("TODO, this needs to be written");

            return info;
        }
        
        /// <summary>
        /// Helper For Bonus Description
        /// </summary>
        /// <param name="iBonus"></param>
        /// <param name="iBonusType"></param>
        /// <returns></returns>
        private static string ItemBonusDescription(int iBonus, int iBonusType)
        {
            //This displays the bonuses just like the Live servers. there is a check against the pts/% differences
            string str = ((iBonusType == 9) | (iBonusType == 150) | (iBonusType == 210) | (iBonusType == 10) | (iBonusType == 151) | (iBonusType == 186)) ? "pts of " : "% to ";  //150(Health Regen) 151(PowerRegen) and 186(Style reductions) need the prefix of "pts of " to be correct

            //we need to only display the effective bonus, cut off caps

            //Lifeflight add

            //iBonusTypes that cap at 10
            //SpellRange, ArcheryRange, MeleeSpeed, MeleeDamage, RangedDamage, ArcherySpeed,
            //CastingSpeed, ResistPierce, SpellDamage, StyleDamage
            if (iBonusType == (int)eProperty.SpellRange || iBonusType == (int)eProperty.ArcheryRange || iBonusType == (int)eProperty.MeleeSpeed
                || iBonusType == (int)eProperty.MeleeDamage || iBonusType == (int)eProperty.RangedDamage || iBonusType == (int)eProperty.ArcherySpeed
                || iBonusType == (int)eProperty.CastingSpeed || iBonusType == (int)eProperty.ResistPierce || iBonusType == (int)eProperty.SpellDamage || iBonusType == (int)eProperty.StyleDamage)
            {
                if (iBonus > 10)
                    iBonus = 10;
            }

            //cap at 25 with no chance of going over
            //DebuffEffectivness, BuffEffectiveness, HealingEffectiveness
            //SpellDuration
            if (iBonusType == (int)eProperty.DebuffEffectiveness || iBonusType == (int)eProperty.BuffEffectiveness || iBonusType == (int)eProperty.HealingEffectiveness || iBonusType == (int)eProperty.SpellDuration || iBonusType == (int)eProperty.ArcaneSyphon)
            {
                if (iBonus > 25)
                    iBonus = 25;
            }
            //hitscap, caps at 200
            if (iBonusType == (int)eProperty.MaxHealthCapBonus)
            {
                if (iBonus > 200)
                    iBonus = 200;
            }
            //cap at 25, but can get overcaps
            //PowerPool
            //This will need to be done in the method that calls this method.
            //if (iBonusType == (int)eProperty.PowerPool)
            //{
            //    if (iBonus > 25)
            //        iBonus = 50;
            //}
            //caps at 50
            //PowerPoolCapBonus
            if (iBonusType == (int)eProperty.PowerPoolCapBonus)
            {
                if (iBonus > 50)
                    iBonus = 50;
            }

            return string.Format("+{0}{1}{2}", iBonus, str, SkillBase.GetPropertyName(((eProperty)iBonusType)));
        }
        #endregion

        public static List<(SpellLine, List<Skill>)> UpdateUsableListSpells(GamePlayer player, List<(SpellLine, List<Skill>)> usableListSpells)
        {
            // Map existing Tuples by SpellLine ID to reuse the inner List<Skill> objects.
            Dictionary<int, (SpellLine, List<Skill>)> existingMap = new(usableListSpells.Count);

            foreach (var item in usableListSpells)
            {
                // If duplicates exist (shouldn't happen), last one wins.
                if (item != default)
                    existingMap[item.Item1.ID] = item;
            }

            // Use two buckets to preserve the Base -> Spec order.
            List<(SpellLine, List<Skill>)> newBase = new();
            List<(SpellLine, List<Skill>)> newSpec = new();

            foreach (Specialization spec in player.GetSpecList())
            {
                if (spec.HybridSpellList)
                    continue;

                var spellsMap = spec.GetLinesSpellsForLiving(player);

                foreach (SpellLine sl in spec.GetSpellLinesForLiving(player))
                {
                    List<Skill> innerList;
                    (SpellLine, List<Skill>) tuple;

                    if (existingMap.TryGetValue(sl.ID, out var existingTuple))
                    {
                        innerList = existingTuple.Item2;
                        innerList.Clear();

                        // If the SpellLine object instance is identical, reuse the Tuple. Otherwise, create a new Tuple with the new SpellLine but the old List.
                        tuple = existingTuple.Item1 == sl ? existingTuple : new(sl, innerList);
                    }
                    else
                    {
                        innerList = new();
                        tuple = new(sl, innerList);
                    }

                    // Populate the inner list with skills.
                    SpellLine key = spellsMap.Keys.FirstOrDefault(k => k.ID == sl.ID);

                    if (key != null && spellsMap.TryGetValue(key, out List<Skill> spellsInLine))
                        innerList.AddRange(spellsInLine);

                    if (sl.IsBaseLine)
                        newBase.Add(tuple);
                    else
                        newSpec.Add(tuple);
                }
            }

            usableListSpells.Clear();
            usableListSpells.AddRange(newBase);
            usableListSpells.AddRange(newSpec);
            return usableListSpells;
        }

        public static List<(Skill, Skill)> UpdateUsableSkills(GamePlayer player, List<(Skill, Skill)> usableSkills)
        {
            int count = usableSkills.Count;

            // Map existing indices.
            // - Specs, Abilities, Styles: Map by unique ID.
            // - Spells: Map by SpellLine ID using a Queue to preserve slot order.

            Dictionary<SkillKey, int> idMap = new(count);
            Dictionary<int, Queue<int>> spellSlots = new();
            Dictionary<int, Queue<int>> songSlots = new();

            for (int i = 0; i < count; i++)
            {
                var item = usableSkills[i];
                Skill skill = item.Item1;
                Skill parent = item.Item2; // Usually the SpellLine or Spec.

                if (skill is Spell spell)
                {
                    if (parent is SpellLine spellLine)
                    {
                        var targetDict = spell.NeedInstrument ? songSlots : spellSlots;

                        if (!targetDict.TryGetValue(spellLine.ID, out Queue<int> queue))
                        {
                            queue = new();
                            targetDict[spellLine.ID] = queue;
                        }

                        queue.Enqueue(i);
                    }
                }
                else
                {
                    // Specs, Abilities, Styles -> Strict ID matching.
                    SkillKey key = SkillKey.GetKey(skill);

                    if (!idMap.ContainsKey(key))
                        idMap[key] = i;
                }
            }

            bool[] visited = ArrayPool<bool>.Shared.Rent(count);

            try
            {
                Array.Clear(visited, 0, count);

                List<(Skill, Skill)> newSpecs = new();
                List<(Skill, Skill)> newOthers = new();

                IList<Specialization> specs = player.GetSpecList();
                List<(Specialization spec, IDictionary<SpellLine, List<Skill>> spellLines)> hybridSpellLists = new(specs.Count);

                // Order matters.

                // Specs.
                foreach (Specialization spec in specs)
                {
                    if (spec.HybridSpellList)
                        hybridSpellLists.Add((spec, spec.GetLinesSpellsForLiving(player)));

                    if (!spec.Trainable)
                        continue;

                    if (idMap.TryGetValue(SkillKey.GetKey(spec), out int index))
                        UpdateAt(usableSkills, visited, index, spec, spec);
                    else
                        newSpecs.Add(new(spec, spec));
                }

                // Abilities.
                foreach (Specialization spec in specs)
                {
                    foreach (Ability ability in spec.GetAbilitiesForLiving(player))
                    {
                        // Retrieve the actual ability from the player to preserve any customizations.
                        Ability ab = player.GetAbility(ability.KeyName) ?? ability;

                        if (idMap.TryGetValue(SkillKey.GetKey(ab), out int index))
                            UpdateAt(usableSkills, visited, index, ab, spec);
                        else
                            newOthers.Add(new(ab, spec));
                    }
                }

                // Hybrid spells (no songs).
                foreach (var (spec, spellLines) in hybridSpellLists)
                {
                    foreach (var pair in spellLines)
                    {
                        spellSlots.TryGetValue(pair.Key.ID, out Queue<int> availableIndices);

                        foreach (Skill skill in pair.Value)
                        {
                            if (skill is Spell spell && !spell.NeedInstrument)
                            {
                                if (availableIndices != null && availableIndices.Count > 0)
                                {
                                    int index = availableIndices.Dequeue();
                                    UpdateAt(usableSkills, visited, index, spell, pair.Key);
                                }
                                else
                                    newOthers.Add(new(spell, pair.Key));
                            }
                        }
                    }
                }

                // Songs.
                foreach (var (spec, spellLines) in hybridSpellLists)
                {
                    foreach (var pair in spellLines)
                    {
                        songSlots.TryGetValue(pair.Key.ID, out Queue<int> availableIndices);

                        foreach (Skill skill in pair.Value)
                        {
                            if (skill is Spell spell && spell.NeedInstrument)
                            {
                                if (availableIndices != null && availableIndices.Count > 0)
                                {
                                    int index = availableIndices.Dequeue();
                                    UpdateAt(usableSkills, visited, index, spell, pair.Key);
                                }
                                else
                                    newOthers.Add(new(spell, pair.Key));
                            }
                        }
                    }
                }

                // Styles.
                foreach (Specialization spec in specs)
                {
                    foreach (Style style in spec.GetStylesForLiving(player))
                    {
                        if (idMap.TryGetValue(SkillKey.GetKey(style), out int index))
                            UpdateAt(usableSkills, visited, index, style, spec);
                        else
                            newOthers.Add(new(style, spec));
                    }
                }

                // Remove any skill that wasn't visited (upgraded or kept).
                int writeIndex = 0;
                int specBlockEnd = 0;

                for (int i = 0; i < count; i++)
                {
                    if (visited[i])
                    {
                        if (i != writeIndex)
                            usableSkills[writeIndex] = usableSkills[i];

                        if (usableSkills[writeIndex].Item1 is Specialization)
                            specBlockEnd++;

                        writeIndex++;
                    }
                }

                if (writeIndex < count)
                    usableSkills.RemoveRange(writeIndex, count - writeIndex);

                if (newSpecs.Count > 0)
                    usableSkills.InsertRange(specBlockEnd, newSpecs);

                if (newOthers.Count > 0)
                    usableSkills.AddRange(newOthers);

                return usableSkills;
            }
            finally
            {
                ArrayPool<bool>.Shared.Return(visited);
            }

            static void UpdateAt(List<(Skill, Skill)> list, bool[] visited, int index, Skill primary, Skill secondary)
            {
                visited[index] = true;
                var existing = list[index];

                if (existing.Item1 != primary || existing.Item2 != secondary)
                    list[index] = new(primary, secondary);
            }
        }

        private readonly struct SkillKey : IEquatable<SkillKey>
        {
            private readonly int _id;
            private readonly byte _type;

            public SkillKey(int id, byte type)
            {
                _id = id;
                _type = type;
            }

            public override int GetHashCode()
            {
                return (_id << 5) ^ _type;
            }

            public override bool Equals(object obj)
            {
                return obj is SkillKey other && Equals(other);
            }

            public bool Equals(SkillKey other)
            {
                return _id == other._id && _type == other._type;
            }

            public static SkillKey GetKey(Skill skill)
            {
                if (skill is Specialization spec)
                    return new(spec.ID, 1);

                if (skill is Ability ability)
                    return new(ability.ID, 2);

                if (skill is Style style)
                    return new(style.ID, 4);

                return new(skill.ID, 0);
            }
        }
    }
}
