using DOL.AI.Brain;
using DOL.Language;

namespace DOL.GS.Keeps
{
    public class GuardTemplateMgr
    {
        public static void RefreshTemplate(GameKeepGuard guard)
        {
            SetGuardRealm(guard);
            SetGuardGuild(guard);
            SetGuardRespawn(guard);
            SetGuardGender(guard);
            SetGuardModel(guard);
            SetGuardName(guard);
            SetBlockEvadeParryChance(guard);
            SetGuardBrain(guard);
            SetGuardSpeed(guard);
            SetGuardLevel(guard);
            SetGuardResists(guard);
            SetGuardStats(guard);
            SetGuardAggression(guard);
            SetGuardSpell(guard);
            ClothingMgr.EquipGuard(guard);
            ClothingMgr.SetEmblem(guard);
        }

        private static void SetGuardSpell(GameKeepGuard guard)
        {
            if (guard.Spells.Count > 0)
                guard.Spells.Clear();

            if (guard is GuardHealer)
            {
                if (guard.IsPortalKeepGuard || guard.Level == 255)
                {
                    switch (guard.Realm)
                    {
                        case eRealm.None:
                        case eRealm.Albion:
                            guard.Spells.Add(SpellMgr.AlbLordHealSpell);
                            break;
                        case eRealm.Midgard:
                            guard.Spells.Add(SpellMgr.MidLordHealSpell);
                            break;
                        case eRealm.Hibernia:
                            guard.Spells.Add(SpellMgr.HibLordHealSpell);
                            break;
                    }
                }
                else
                {
                    switch (guard.Realm)
                    {
                        case eRealm.None:
                        case eRealm.Albion:
                            guard.Spells.Add(SpellMgr.AlbGuardHealSmallSpell);
                            break;
                        case eRealm.Midgard:
                            guard.Spells.Add(SpellMgr.MidGuardHealSmallSpell);
                            break;
                        case eRealm.Hibernia:
                            guard.Spells.Add(SpellMgr.HibGuardHealSmallSpell);
                            break;
                    }
                }
            }
            if (guard is GuardCaster)
            {
                if (guard.IsPortalKeepGuard || guard.Level == 255)
                {
                    switch (guard.Realm)
                    {
                        case eRealm.None:
                        case eRealm.Albion:
                            guard.Spells.Add(SpellMgr.AlbGuardBoltSpellPortalKeep);
                            guard.Spells.Add(SkillBase.GetSpellByID(10));
                            break;
                        case eRealm.Midgard:
                            guard.Spells.Add(SpellMgr.MidGuardBoltSpellPortalKeep);
                            guard.Spells.Add(SkillBase.GetSpellByID(2669));
                            break;
                        case eRealm.Hibernia:
                            guard.Spells.Add(SpellMgr.HibGuardBoltSpellPortalKeep);
                            guard.Spells.Add(SkillBase.GetSpellByID(4310));
                            break;
                    }
                }
                else
                {
                    switch (guard.Realm)
                    {
                        case eRealm.None:
                        case eRealm.Albion:
                            if (guard.Level < 6)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(301));
                                guard.Spells.Add(SkillBase.GetSpellByID(3));
                            }
                            else if (guard.Level >= 6 && guard.Level <= 10)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(303));
                                guard.Spells.Add(SkillBase.GetSpellByID(4));
                            }
                            else if (guard.Level >= 11 && guard.Level <= 15)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(305));
                                guard.Spells.Add(SkillBase.GetSpellByID(5));
                            }
                            else if (guard.Level >= 16 && guard.Level <= 20)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(306));
                                guard.Spells.Add(SkillBase.GetSpellByID(6));
                            }
                            else if (guard.Level >= 21 && guard.Level <= 25)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(307));
                                guard.Spells.Add(SkillBase.GetSpellByID(7));
                            }
                            else if (guard.Level >= 26 && guard.Level <= 30)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(307));
                                guard.Spells.Add(SkillBase.GetSpellByID(8));
                            }
                            else if (guard.Level >= 31 && guard.Level <= 35)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(308));
                                guard.Spells.Add(SkillBase.GetSpellByID(8));
                            }
                            else if (guard.Level >= 36 && guard.Level <= 40)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(309));
                                guard.Spells.Add(SkillBase.GetSpellByID(3));
                            }
                            else if (guard.Level >= 41 && guard.Level <= 45)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(309));
                                guard.Spells.Add(SkillBase.GetSpellByID(9));
                            }
                            else
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(310));
                                guard.Spells.Add(SkillBase.GetSpellByID(10));
                            }

                            break;
                        case eRealm.Midgard:
                            if (guard.Level < 6)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2503));
                                guard.Spells.Add(SkillBase.GetSpellByID(2662));
                            }
                            else if (guard.Level >= 6 && guard.Level <= 10)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2505));
                                guard.Spells.Add(SkillBase.GetSpellByID(2663));
                            }
                            else if (guard.Level >= 11 && guard.Level <= 15)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2507));
                                guard.Spells.Add(SkillBase.GetSpellByID(2664));
                            }
                            else if (guard.Level >= 16 && guard.Level <= 20)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2508));
                                guard.Spells.Add(SkillBase.GetSpellByID(2665));
                            }
                            else if (guard.Level >= 21 && guard.Level <= 25)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2508));
                                guard.Spells.Add(SkillBase.GetSpellByID(2666));
                            }
                            else if (guard.Level >= 26 && guard.Level <= 30)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2509));
                                guard.Spells.Add(SkillBase.GetSpellByID(2667));
                            }
                            else if (guard.Level >= 31 && guard.Level <= 35)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2510));
                                guard.Spells.Add(SkillBase.GetSpellByID(2668));
                            }
                            else if (guard.Level >= 36 && guard.Level <= 40)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2510));
                                guard.Spells.Add(SkillBase.GetSpellByID(2669));
                            }
                            else if (guard.Level >= 41 && guard.Level <= 45)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2511));
                                guard.Spells.Add(SkillBase.GetSpellByID(2669));
                            }
                            else
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(2512));
                                guard.Spells.Add(SkillBase.GetSpellByID(2670));
                            }
                            break;
                        case eRealm.Hibernia:
                            if (guard.Level < 6)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4103));
                                guard.Spells.Add(SkillBase.GetSpellByID(4302));
                            }
                            else if (guard.Level >= 6 && guard.Level <= 10)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4105));
                                guard.Spells.Add(SkillBase.GetSpellByID(4304));
                            }
                            else if (guard.Level >= 11 && guard.Level <= 15)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4106));
                                guard.Spells.Add(SkillBase.GetSpellByID(4305));
                            }
                            else if (guard.Level >= 16 && guard.Level <= 20)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4107));
                                guard.Spells.Add(SkillBase.GetSpellByID(4306));
                            }
                            else if (guard.Level >= 21 && guard.Level <= 25)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4108));
                                guard.Spells.Add(SkillBase.GetSpellByID(4307));
                            }
                            else if (guard.Level >= 26 && guard.Level <= 30)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4108));
                                guard.Spells.Add(SkillBase.GetSpellByID(4307));
                            }
                            else if (guard.Level >= 31 && guard.Level <= 35)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4109));
                                guard.Spells.Add(SkillBase.GetSpellByID(4308));
                            }
                            else if (guard.Level >= 36 && guard.Level <= 40)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4109));
                                guard.Spells.Add(SkillBase.GetSpellByID(4308));
                            }
                            else if (guard.Level >= 41 && guard.Level <= 45)
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4109));
                                guard.Spells.Add(SkillBase.GetSpellByID(4309));
                            }
                            else
                            {
                                guard.Spells.Add(SkillBase.GetSpellByID(4111));
                                guard.Spells.Add(SkillBase.GetSpellByID(4310));
                            }
                            break;
                    }
                }
            }
        }

        private static void SetGuardRealm(GameKeepGuard guard)
        {
            if (guard.Component != null)
            {
                if (guard.Component.Keep != null)
					guard.Realm = guard.Component.Keep.Realm;

                if (guard.Realm != eRealm.None)
                {
                    guard.ModelRealm = guard.Realm;
                }
                else
                {
                    guard.ModelRealm = (eRealm)Util.Random(1, 3);
                }
            }
            else
            {
                guard.Realm = guard.CurrentZone.Realm;
                guard.ModelRealm = guard.Realm;
            }
        }

        private static void SetGuardGuild(GameKeepGuard guard)
        {
            if (guard.Component == null)
            {
                guard.GuildName = "";
            }
            else if (guard.Component.Keep == null || guard.Component.Keep.Guild == null)
            {
                guard.GuildName = "";
            }
            else
            {
                guard.GuildName = guard.Component.Keep.Guild.Name;
            }
        }

        private static void SetGuardRespawn(GameKeepGuard guard)
        {
            if (guard is GuardFighterRK)
            {
                guard.RespawnInterval = Util.Random(30, 35) * 60 * 1000; // 60-100 minutes spawn
            }
            else if (guard is FrontierHastener)
            {
                guard.RespawnInterval = 5000; // 5 seconds
            }
            else if (guard is GateKeeperIn || guard is GateKeeperOut)
            {
                guard.RespawnInterval = 5000; // 5 seconds
            }
            else if (guard is GuardLord)
            {
                if (guard.Component != null && guard.Component.Keep != null)
                {
                    guard.RespawnInterval = guard.Component.Keep.LordRespawnTime;
                }
                else
                {
                    guard.RespawnInterval = 5000;
                }
            }
            else if (guard is MissionMaster)
            {
                guard.RespawnInterval = 10000; // 10 seconds
            }
            else
            {
                if (guard.Component != null && guard.Component.Keep != null)
                {
                    if (!guard.Component.Keep.IsRelic)
                        guard.RespawnInterval = Util.Random(13, 17) * 60 * 1000; // 8 to 10 mn
                    else
                        guard.RespawnInterval = Util.Random(21, 25) * 60 * 1000; // 21 to 25 mn
                }
                else
                    guard.RespawnInterval = Util.Random(13, 17) * 60 * 1000;
            }
        }

        private static void SetGuardAggression(GameKeepGuard guard)
        {
            if (guard is GuardFighterRK)
            {
                (guard.Brain as KeepGuardBrain).SetAggression(99, 1800);
            }
            else if (guard is GuardStaticCaster)
            {
                (guard.Brain as KeepGuardBrain).SetAggression(99, 1850);
            }
            else if (guard is GuardStaticArcher)
            {
                (guard.Brain as KeepGuardBrain).SetAggression(99, 2100);
            }
        }

        public static void SetGuardLevel(GameKeepGuard guard)
        {
            if (guard is GuardFighterRK)
            {
                guard.Level = 66;
            }
            else
            {
                if (guard.Component != null && guard.Component.Keep != null)
                {
                    guard.Component.Keep.SetGuardLevel(guard);
                }
            }
        }

        private static void SetGuardGender(GameKeepGuard guard)
        {
            //portal keep guards are always male
            if (guard is GuardFighterRK)
            {
                guard.Gender = eGender.Male;
            }
            if (guard is GuardCorspeSummoner)
            {
                guard.Gender = eGender.Male;
            }
            else if (guard.IsPortalKeepGuard || guard.Level == 255)
            {
                guard.Gender = eGender.Male;
            }
            else
            {
                if (Util.Chance(50))
                {
                    guard.Gender = eGender.Male;
                }
                else
                {
                    guard.Gender = eGender.Female;
                }
            }
        }


        #region Hastener Models
        public static ushort AlbionHastener = 244;
        public static ushort MidgardHastener = 22;
        public static ushort HiberniaHastener = 1910;
        #endregion

        #region AlbionClassModels
        public static ushort BritonMale = 32;
        public static ushort BritonFemale = 35;
        public static ushort HighlanderMale = 39;
        public static ushort HighlanderFemale = 43;
        public static ushort SaracenMale = 48;
        public static ushort SaracenFemale = 52;
        public static ushort AvalonianMale = 61;
        public static ushort AvalonianFemale = 65;
        public static ushort IcconuMale = 716;
        public static ushort IcconuFemale = 724;
        // public static ushort HalfOgreMale = 1008;
        // public static ushort HalfOgreFemale = 1020;
        // public static ushort MinotaurMaleAlb = 1395;
        #endregion

        #region MidgardClassModels
        public static ushort TrollMale = 137;
        public static ushort TrollFemale = 145;
        public static ushort NorseMale = 503;
        public static ushort NorseFemale = 507;
        public static ushort KoboldMale = 169;
        public static ushort KoboldFemale = 177;
        public static ushort DwarfMale = 185;
        public static ushort DwarfFemale = 193;
        public static ushort ValkynMale = 773;
        public static ushort ValkynFemale = 781;
        // public static ushort FrostalfMale = 1051;
        // public static ushort FrostalfFemale = 1063;
        // public static ushort MinotaurMaleMid = 1407;
        #endregion

        #region HiberniaClassModels
        public static ushort FirbolgMale = 286;
        public static ushort FirbolgFemale = 294;
        public static ushort CeltMale = 302;
        public static ushort CeltFemale = 310;
        public static ushort LurikeenMale = 318;
        public static ushort LurikeenFemale = 326;
        public static ushort ElfMale = 334;
        public static ushort ElfFemale = 342;
        // public static ushort SharMale = 1075;
        // public static ushort SharFemale = 1087;
        public static ushort SylvianMale = 700;
        public static ushort SylvianFemale = 708;
        // public static ushort MinotaurMaleHib = 1419;
        #endregion

        /// <summary>
        /// Sets a guards model
        /// </summary>
        /// <param name="guard">The guard object</param>
        private static void SetGuardModel(GameKeepGuard guard)
        {
            if (!ServerProperties.Properties.AUTOMODEL_GUARDS_LOADED_FROM_DB && !guard.LoadedFromScript)
            {
                return;
            }
            if (guard is FrontierHastener || guard is GateKeeperIn || guard is GateKeeperOut)
            {
                switch (guard.Realm)
                {
                    case eRealm.None:
                    case eRealm.Albion:
                        {
                            guard.Model = AlbionHastener;
                            guard.Size = 45;
                            break;
                        }
                    case eRealm.Midgard:
                        {
                            guard.Model = MidgardHastener;
                            guard.Size = 50;
                            guard.Flags ^= GameNPC.eFlags.GHOST;
                            break;
                        }
                    case eRealm.Hibernia:
                        {
                            guard.Model = HiberniaHastener;
                            guard.Size = 45;
                            break;
                        }
                }
                return;
            }

            switch (guard.ModelRealm)
            {
                #region None
                case eRealm.None:
                #endregion

                #region Albion
                case eRealm.Albion:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = SaracenMale; break;//Saracen Male
                                    case 1: guard.Model = HighlanderMale; break;//Highlander Male
                                    case 2: guard.Model = BritonMale; break;//Briton Male
                                    case 3: guard.Model = IcconuMale; break;//Icconu Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = SaracenFemale; break;//Saracen Female
                                    case 1: guard.Model = HighlanderFemale; break;//Highlander Female
                                    case 2: guard.Model = BritonFemale; break;//Briton Female
                                    case 3: guard.Model = IcconuFemale; break;//Icconu Female
                                }
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = AvalonianMale; break;//Avalonian Male
                                    case 1: guard.Model = BritonMale; break;//Briton Male
                                    // case 2: guard.Model = HalfOgreMale; break;//Half Ogre Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = AvalonianFemale; break;//Avalonian Female
                                    case 1: guard.Model = BritonFemale; break;//Briton Female
                                    // case 2: guard.Model = HalfOgreFemale; break;//Half Ogre Female
                                }
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 4))
                                {
                                    case 0: guard.Model = HighlanderMale; break;//Highlander Male
                                    case 1: guard.Model = BritonMale; break;//Briton Male
                                    case 2: guard.Model = SaracenMale; break;//Saracen Male
                                    case 3: guard.Model = AvalonianMale; break;//Avalonian Male
                                    // case 5: guard.Model = HalfOgreMale; break;//Half Ogre Male
                                    case 4: guard.Model = IcconuMale; break;//Icconu Male
                                    // case 6: guard.Model = MinotaurMaleAlb; break;//Minotuar
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 4))
                                {
                                    case 0: guard.Model = HighlanderFemale; break;//Highlander Female
                                    case 1: guard.Model = BritonFemale; break;//Briton Female
                                    case 2: guard.Model = SaracenFemale; break;//Saracen Female
                                    case 3: guard.Model = AvalonianFemale; break;//Avalonian Female
                                    // case 5: guard.Model = HalfOgreFemale; break;//Half Ogre Female
                                    case 4: guard.Model = IcconuFemale; break;//Icconu Female
                                }
                            }
                        }
                        else if (guard is GuardFighterRK)
                        {

                            switch (Util.Random(0, 2))
                            {
                                case 0: guard.Model = HighlanderMale; break;//Highlander Male
                                case 1: guard.Model = BritonMale; break;//Briton Male
                                case 2: guard.Model = AvalonianMale; break;//Avalonian Male
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = HighlanderMale; break;//Highlander Male
                                    case 1: guard.Model = BritonMale; break;//Briton Male
                                    case 2: guard.Model = AvalonianMale; break;//Avalonian Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = HighlanderFemale; break;//Highlander Female
                                    case 1: guard.Model = BritonFemale; break;//Briton Female
                                    case 2: guard.Model = AvalonianFemale; break;//Avalonian Female
                                }
                            }
                        }
                        else if (guard is GuardLord || guard is MissionMaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = HighlanderMale; break;//Highlander Male
                                    case 1: guard.Model = BritonMale; break;//Briton Male
                                    case 2: guard.Model = AvalonianMale; break;//Avalonian Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = HighlanderFemale; break;//Highlander Female
                                    case 1: guard.Model = BritonFemale; break;//Briton Female
                                    case 2: guard.Model = AvalonianFemale; break;//Avalonian Female
                                }
                            }
                        }
                        else if (guard is GuardStealther)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = SaracenMale; break;//Saracen Male
                                    case 1: guard.Model = BritonMale; break;//Briton Male
                                    case 2: guard.Model = IcconuMale; break;//Icconu Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = SaracenFemale; break;//Saracen Female
                                    case 1: guard.Model = BritonFemale; break;//Briton Female
                                    case 2: guard.Model = IcconuFemale; break;//Icconu Female
                                }
                            }
                        }
                        break;
                    }
                #endregion

                #region Midgard
                case eRealm.Midgard:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = NorseMale; break;//Norse Male
                                    case 1: guard.Model = KoboldMale; break;//Kobold Male
                                    case 2: guard.Model = DwarfMale; break;//Dwarf Male
                                    case 3: guard.Model = ValkynMale; break;//Valkyn Male
                                    // case 4: guard.Model = FrostalfMale; break;//Frostalf Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = NorseFemale; break;//Norse Female
                                    case 1: guard.Model = KoboldFemale; break;//Kobold Female
                                    case 2: guard.Model = DwarfFemale; break;//Dwarf Female
                                    case 3: guard.Model = ValkynFemale; break;//Valkyn Female
                                    // case 4: guard.Model = FrostalfFemale; break;//Frostalf Female
                                }
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = KoboldMale; break;//Kobold Male
                                    case 1: guard.Model = NorseMale; break;//Norse Male
                                    case 2: guard.Model = DwarfMale; break;//Dwarf Male
                                    // case 3: guard.Model = FrostalfMale; break;//Frostalf Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = KoboldFemale; break;//Kobold Female
                                    case 1: guard.Model = NorseFemale; break;//Norse Female
                                    case 2: guard.Model = DwarfFemale; break;//Dwarf Female
                                    // case 3: guard.Model = FrostalfFemale; break;//Frostalf Female
                                }
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 4))
                                {
                                    case 0: guard.Model = TrollMale; break;//Troll Male
                                    case 1: guard.Model = NorseMale; break;//Norse Male
                                    case 2: guard.Model = DwarfMale; break;//Dwarf Male
                                    case 3: guard.Model = KoboldMale; break;//Kobold Male
                                    case 4: guard.Model = ValkynMale; break;//Valkyn Male
                                    // case 5: guard.Model = MinotaurMaleMid; break;//Minotaur
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 4))
                                {
                                    case 0: guard.Model = TrollFemale; break;//Troll Female
                                    case 1: guard.Model = NorseFemale; break;//Norse Female
                                    case 2: guard.Model = DwarfFemale; break;//Dwarf Female
                                    case 3: guard.Model = KoboldFemale; break;//Kobold Female
                                    case 4: guard.Model = ValkynFemale; break;//Valkyn Female
                                }
                            }
                        }
                        else if (guard is GuardFighterRK)
                        {

                            switch (Util.Random(0, 4))
                            {
                                case 0: guard.Model = TrollMale; break;//Troll Male
                                case 1: guard.Model = NorseMale; break;//Norse Male
                                case 2: guard.Model = DwarfMale; break;//Dwarf Male
                                case 3: guard.Model = KoboldMale; break;//Kobold Male
                                case 4: guard.Model = ValkynMale; break;//Valkyn Male
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = DwarfMale; break;//Dwarf Male
                                    case 1: guard.Model = NorseMale; break;//Norse Male
                                    // case 2: guard.Model = FrostalfMale; break;//Frostalf Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = DwarfFemale; break;//Dwarf Female
                                    case 1: guard.Model = NorseFemale; break;//Norse Female
                                    // case 2: guard.Model = FrostalfFemale; break;//Frostalf Female
                                }
                            }
                        }
                        else if (guard is GuardLord || guard is MissionMaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = DwarfMale; break;//Dwarf Male
                                    case 1: guard.Model = NorseMale; break;//Norse Male
                                    case 2: guard.Model = TrollMale; break;//Troll Male
                                    case 3: guard.Model = KoboldMale; break;//Kobold Male
                                    // case 4: guard.Model = MinotaurMaleMid; break;//Minotaur
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = DwarfFemale; break;//Dwarf Female
                                    case 1: guard.Model = NorseFemale; break;//Norse Female
                                    case 2: guard.Model = TrollFemale; break;//Troll Female
                                    case 3: guard.Model = KoboldFemale; break;//Kobold Female
                                }
                            }
                        }
                        else if (guard is GuardStealther)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = KoboldMale; break;//Kobold Male
                                    case 1: guard.Model = NorseMale; break;//Norse Male
                                    case 2: guard.Model = ValkynMale; break;//Valkyn Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = KoboldFemale; break;//Kobold Female
                                    case 1: guard.Model = NorseFemale; break;//Norse Female
                                    case 2: guard.Model = ValkynFemale; break;//Valkyn Female
                                }
                            }
                        }
                        break;
                    }
                #endregion

                #region Hibernia
                case eRealm.Hibernia:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = LurikeenMale; break;//Lurikeen Male
                                    case 1: guard.Model = ElfMale; break;//Elf Male
                                    case 2: guard.Model = CeltMale; break;//Celt Male
                                    // case 3: guard.Model = SharMale; break;//Shar Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = LurikeenFemale; break;//Lurikeen Female
                                    case 1: guard.Model = ElfFemale; break;//Elf Female
                                    case 2: guard.Model = CeltFemale; break;//Celt Female
                                    // case 3: guard.Model = SharFemale; break;//Shar Female
                                }
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = ElfMale; break;//Elf Male
                                    case 1: guard.Model = LurikeenMale; break;//Lurikeen Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = ElfFemale; break;//Elf Female
                                    case 1: guard.Model = LurikeenFemale; break;//Lurikeen Female
                                }
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = FirbolgMale; break;//Firbolg Male
                                    case 1: guard.Model = LurikeenMale; break;//Lurikeen Male
                                    case 2: guard.Model = CeltMale; break;//Celt Male
                                    // case 3: guard.Model = SharMale; break;//Shar Male
                                    // case 4: guard.Model = MinotaurMaleHib; break;//Minotaur
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = FirbolgFemale; break;//Firbolg Female
                                    case 1: guard.Model = LurikeenFemale; break;//Lurikeen Female
                                    case 2: guard.Model = CeltFemale; break;//Celt Female
                                    // case 3: guard.Model = SharFemale; break;//Shar Female
                                }
                            }
                        }
                        else if (guard is GuardFighterRK)
                        {

                            switch (Util.Random(0, 2))
                            {
                                case 0: guard.Model = FirbolgMale; break;//Firbolg Male
                                case 1: guard.Model = LurikeenMale; break;//Lurikeen Male
                                case 2: guard.Model = CeltMale; break;//Celt Male
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = CeltMale; break;//Celt Male
                                    case 1: guard.Model = FirbolgMale; break;//Firbolg Male
                                    case 2: guard.Model = SylvianMale; break;//Sylvian Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 2))
                                {
                                    case 0: guard.Model = CeltFemale; break;//Celt Female
                                    case 1: guard.Model = FirbolgFemale; break;//Firbolg Female
                                    case 2: guard.Model = SylvianFemale; break;//Sylvian Female
                                }
                            }
                        }
                        else if (guard is GuardLord || guard is MissionMaster)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = CeltMale; break;//Celt Male
                                    case 1: guard.Model = FirbolgMale; break;//Firbolg Male
                                    case 2: guard.Model = LurikeenMale; break;//Lurikeen Male
                                    case 3: guard.Model = ElfMale; break;//Elf Male
                                    // case 4: guard.Model = MinotaurMaleHib; break;//Minotaur
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 3))
                                {
                                    case 0: guard.Model = CeltFemale; break;//Celt Female
                                    case 1: guard.Model = FirbolgFemale; break;//Firbolg Female
                                    case 2: guard.Model = LurikeenFemale; break;//Lurikeen Female
                                    case 3: guard.Model = ElfFemale; break;//Elf Female
                                }
                            }
                        }
                        else if (guard is GuardStealther)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = ElfMale; break;//Elf Male
                                    case 1: guard.Model = LurikeenMale; break;//Lurikeen Male
                                }
                            }
                            else
                            {
                                switch (Util.Random(0, 1))
                                {
                                    case 0: guard.Model = ElfFemale; break;//Elf Female
                                    case 1: guard.Model = LurikeenFemale; break;//Lurikeen Female
                                }
                            }
                        }
                        break;
                    }
                #endregion
            }
        }

        /// <summary>
        /// Gets short name of keeps
        /// </summary>
        /// <param name="KeepName">Complete name of the Keep</param>
        public static string GetKeepShortName(string KeepName)
        {
            string ShortName;
            if (KeepName.StartsWith("Caer"))//Albion
            {
                ShortName = KeepName.Substring(5);
            }
            else if (KeepName.StartsWith("Fort"))
            {
                ShortName = KeepName.Substring(5);
            }
            else if (KeepName.StartsWith("Château"))
            {
                ShortName = KeepName.Substring(8);
            }
            else if (KeepName.StartsWith("Dun"))//Hibernia
            {
                if (KeepName == "Dun nGed")
                {
                    ShortName = "nGed";
                }
                else if (KeepName == "Dun da Behn")
                {
                    ShortName = "Behn";
                }
                else
                {
                    ShortName = KeepName.Substring(4);
                }
            }
            else//Midgard
            {
                if (KeepName.Contains(" "))
                    ShortName = KeepName.Substring(0, KeepName.IndexOf(" ", 0));
                else
                    ShortName = KeepName;
            }
            return ShortName;
        }

        /// <summary>
        /// Sets a guards name
        /// </summary>
        /// <param name="guard">The guard object</param>
        private static void SetGuardName(GameKeepGuard guard)
        {
            if (guard is GuardCorspeSummoner)
            {
                guard.Name = "Corpse Summoner";
                guard.TranslationId = "SetGuardName.CorpseSummoner";
                return;
            }
            if (guard is FrontierHastener)
            {
                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Hastener");
                guard.TranslationId = "SetGuardName.Hastener";
                return;
            }
            if (guard is GateKeeperIn || guard is GateKeeperOut)
            {
                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Gatekeeper");
                guard.TranslationId = "SetGuardName.Gatekeeper";
                return;
            }
            if (guard is GuardLord)
            {
                if (guard.Component == null)
                {
                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Commander", guard.CurrentZone.Description);
                    guard.TranslationId = "SetGuardName.Commander";
                    return;
                }
                else if (guard.IsTowerGuard)
                {
                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.TowerCaptain");
                    guard.TranslationId = "SetGuardName.TowerCaptain";
                    return;
                }
            }
            switch (guard.ModelRealm)
            {
                #region None

                case eRealm.None:

                #endregion None

                #region Albion

                case eRealm.Albion:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.BowmanCommander");
                                guard.TranslationId = "SetGuardName.BowmanCommander";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Scout");
                                guard.TranslationId = "SetGuardName.Scout";
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.MasterWizard");
                                guard.TranslationId = "SetGuardName.MasterWizard";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Wizard");
                                guard.TranslationId = "SetGuardName.Wizard";
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.KnightCommander");
                                guard.TranslationId = "SetGuardName.KnightCommander";
                            }
                            else
                            {
                                if (guard.Gender == eGender.Male)
                                {
                                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Armsman");
                                    guard.TranslationId = "SetGuardName.Armsman";
                                }
                                else
                                {
                                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Armswoman");
                                    guard.TranslationId = "SetGuardName.Armswoman";
                                }
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Cleric");
                            guard.TranslationId = "SetGuardName.Cleric";
                        }
                        else if (guard is GuardLord && guard.Component != null && guard.Component.Keep != null)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Lord", GetKeepShortName(guard.Component.Keep.Name));
                                guard.TranslationId = "SetGuardName.Lord";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Lady", GetKeepShortName(guard.Component.Keep.Name));
                                guard.TranslationId = "SetGuardName.Lady";
                            }
                        }
                        else if (guard is GuardStealther)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Infiltrator");
                            guard.TranslationId = "SetGuardName.Infiltrator";
                        }
                        else if (guard is MissionMaster)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.CaptainCommander");
                            guard.TranslationId = "SetGuardName.CaptainCommander";
                        }
                        break;
                    }

                #endregion Albion

                #region Midgard

                case eRealm.Midgard:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.NordicHunter");
                                guard.TranslationId = "SetGuardName.NordicHunter";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Hunter");
                                guard.TranslationId = "SetGuardName.Hunter";
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.MasterRunes");
                                guard.TranslationId = "SetGuardName.MasterRunes";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Runemaster");
                                guard.TranslationId = "SetGuardName.Runemaster";
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.NordicJarl");
                                guard.TranslationId = "SetGuardName.NordicJarl";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Huscarl");
                                guard.TranslationId = "SetGuardName.Huscarl";
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Healer");
                            guard.TranslationId = "SetGuardName.Healer";
                        }
                        else if (guard is GuardLord)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Jarl", GetKeepShortName(guard.Component.Keep.Name));
                            guard.TranslationId = "SetGuardName.Jarl";
                        }
                        else if (guard is GuardStealther)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Shadowblade");
                            guard.TranslationId = "SetGuardName.Shadowblade";
                        }
                        else if (guard is MissionMaster)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.HersirCommander");
                            guard.TranslationId = "SetGuardName.HersirCommander";
                        }
                        break;
                    }
                #endregion

                #region Hibernia

                case eRealm.Hibernia:
                    {
                        if (guard is GuardArcher)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.MasterRanger");
                                guard.TranslationId = "SetGuardName.MasterRanger";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Ranger");
                                guard.TranslationId = "SetGuardName.Ranger";
                            }
                        }
                        else if (guard is GuardCaster)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.MasterEldritch");
                                guard.TranslationId = "SetGuardName.MasterEldritch";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Eldritch");
                                guard.TranslationId = "SetGuardName.Eldritch";
                            }
                        }
                        else if (guard is GuardFighter)
                        {
                            if (guard.IsPortalKeepGuard || guard.Level == 255)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Champion");
                                guard.TranslationId = "SetGuardName.Champion";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Guardian");
                                guard.TranslationId = "SetGuardName.Guardian";
                            }
                        }
                        else if (guard is GuardHealer)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Druid");
                            guard.TranslationId = "SetGuardName.Druid";
                        }
                        else if (guard is GuardLord)
                        {
                            if (guard.Gender == eGender.Male)
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Chieftain", GetKeepShortName(guard.Component.Keep.Name));
                                guard.TranslationId = "SetGuardName.Chieftain";
                            }
                            else
                            {
                                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Chieftess", GetKeepShortName(guard.Component.Keep.Name));
                                guard.TranslationId = "SetGuardName.Chieftess";
                            }
                        }
                        else if (guard is GuardStealther)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Nightshade");
                            guard.TranslationId = "SetGuardName.Nightshade";
                        }
                        else if (guard is MissionMaster)
                        {
                            guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.ChampionCommander");
                            guard.TranslationId = "SetGuardName.ChampionCommander";
                        }
                        break;
                    }

                #endregion
            }

            if (guard.Realm == eRealm.None)
            {
                guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Renegade", guard.Name);
            }
        }

        /// <summary>
        /// Sets a guards Block, Parry and Evade change
        /// </summary>
        /// <param name="guard">The guard object</param>
        private static void SetBlockEvadeParryChance(GameKeepGuard guard)
        {
            guard.BlockChance = 0;
            guard.EvadeChance = 0;
            guard.ParryChance = 0;

            if (guard is GuardLord || guard is MissionMaster)
            {
                guard.BlockChance = 15;
                guard.ParryChance = 15;

                if (guard.ModelRealm != eRealm.Albion)
                {
                    guard.EvadeChance = 10;
                    guard.ParryChance = 5;
                }
            }
            else if (guard is GuardStealther)
            {
                guard.EvadeChance = 30;
            }
            else if (guard is GuardFighterRK)
            {
                guard.BlockChance = 20;
                guard.ParryChance = 20;
            }
            else if (guard is GuardFighter)
            {
                guard.BlockChance = 10;
                guard.ParryChance = 10;

                if (guard.ModelRealm != eRealm.Albion)
                {
                    guard.EvadeChance = 5;
                    guard.ParryChance = 5;
                }
            }
            else if (guard is GuardHealer)
            {
                guard.BlockChance = 5;
            }
            else if (guard is GuardArcher)
            {
                if (guard.ModelRealm == eRealm.Albion)
                {
                    guard.BlockChance = 10;
                    guard.EvadeChance = 5;
                }
                else
                {
                    guard.EvadeChance = 15;
                }
            }
        }

        /// <summary>
        /// Sets the guards brain
        /// </summary>
        /// <param name="guard">The guard object</param>
        public static void SetGuardBrain(GameKeepGuard guard)
        {
            if (guard.Brain is KeepGuardBrain == false)
            {
                KeepGuardBrain brain = new KeepGuardBrain();
                if (guard is GuardFighterRK)
                    brain = new KeepGuardBrain();
                else if (guard is GuardCaster)
                    brain = new CasterBrain();
                else if (guard is GuardHealer)
                    brain = new HealerBrain();
                else if (guard is GuardLord)
                    brain = new LordBrain();
                else if (guard is GuardCorspeSummoner)
                    brain = new CorspeSummonerBrain();
                guard.AddBrain(brain);
                brain.guard = guard;
            }

            if (guard is MissionMaster)
            {
                (guard.Brain as KeepGuardBrain).SetAggression(90, 400);
            }
        }

        /// <summary>
        /// Sets the guards speed
        /// </summary>
        /// <param name="guard">The guard object</param>
        public static void SetGuardSpeed(GameKeepGuard guard)
        {
            if (guard.IsPortalKeepGuard || guard.Level == 255)
            {
                guard.MaxSpeedBase = 575;
            }
            if (((guard is GuardLord) && guard.Component != null) || guard is GuardStaticArcher || guard is GuardStaticCaster)
            {
                guard.MaxSpeedBase = 0;
            }
            else if (guard.Level < 85)
            {
                if (guard.Realm == eRealm.None)
                {
                    guard.MaxSpeedBase = 200;
                }
                else if (guard.Level < 50)
                {
                    guard.MaxSpeedBase = 210;
                }
                else
                {
                    guard.MaxSpeedBase = 250;
                }
            }
            else
            {
                guard.MaxSpeedBase = 575;
            }
            if (guard is GuardFighterRK)
            {
                guard.MaxSpeedBase = 375;
            }
        }

        /// <summary>
        /// Sets a guards resists
        /// </summary>
        /// <param name="guard">The guard object</param>
        private static void SetGuardResists(GameKeepGuard guard)
        {
            for (int i = (int)eProperty.Resist_First; i <= (int)eProperty.Resist_Last; i++)
            {
                if (guard is GuardLord)
                {
                    guard.BaseBuffBonusCategory[i] = 26;
                }
                else if (guard is GuardFighterRK)
                {
                    guard.BaseBuffBonusCategory[i] = 26;
                }
                else if (guard.Level < 50)
                {
                    guard.BaseBuffBonusCategory[i] = guard.Level / 2 + 1;
                }
                else if (guard.IsPortalKeepGuard || guard.Level == 255)
                {
                    guard.BaseBuffBonusCategory[i] = 75;
                }
                else
                {
                    if (guard is GuardFighter)
                    {
                        guard.BaseBuffBonusCategory[i] = 23;
                    }
                    else if (guard is GuardHealer)
                    {
                        guard.BaseBuffBonusCategory[i] = 17;
                    }
                    else if (guard is GuardCaster)
                    {
                        guard.BaseBuffBonusCategory[i] = 8;
                    }
                    else if (guard is GuardArcher)
                    {
                        guard.BaseBuffBonusCategory[i] = 14;
                    }
                    else if (guard is GuardStealther)
                    {
                        guard.BaseBuffBonusCategory[i] = 10;
                    }
                    else
                        guard.BaseBuffBonusCategory[i] = 13;// 26 changé pour baissé un peu la difficulté.
                }
            }
        }

        /// <summary>
        /// Sets a guards stats
        /// </summary>
        /// <param name="guard">The guard object</param>
        private static void SetGuardStats(GameKeepGuard guard)
        {
            if (guard is GuardLord)
            {
                guard.Strength = (short)(20 + (guard.Level * 8));
                guard.Dexterity = (short)(guard.Level * 2);
                guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON);
                guard.Quickness = 60;
            }
            else if (guard is GuardFighterRK)
            {
                guard.Strength = (short)(20 + (guard.Level * 9));
                guard.Dexterity = (short)(guard.Level * 2);
                guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON);
                guard.Quickness = 60;
            }
            else if (guard is GuardCaster)
            {
                guard.Strength = (short)(20 + (guard.Level * 4));
                //guard.Strength = (short)(20 + (guard.Level * 6));
                guard.Dexterity = (short)(guard.Level);
                guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON - 5);
                guard.Quickness = 40;
            }
            else if (guard.IsPortalKeepGuard || guard.Level == 255)
            {
                guard.Strength = (short)(20 + (guard.Level / 4));
                guard.Dexterity = (short)(guard.Level);
                guard.Constitution = (short)(guard.Level);
                guard.Quickness = (short)(guard.Level / 2);
                guard.Intelligence = (short)(guard.Level);
                guard.Empathy = (short)(guard.Level);
                guard.Piety = (short)(guard.Level);
                guard.Charisma = (short)(guard.Level);
            }
            else
            {
                guard.Strength = (short)(20 + (guard.Level * 5));
                //guard.Strength = (short)(20 + (guard.Level * 7));
                guard.Dexterity = (short)(guard.Level);
                guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON);
                guard.Quickness = 40;
            }
        }
    }
}
