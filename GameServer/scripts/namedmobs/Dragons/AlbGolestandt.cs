namespace DOL.GS
{
    public class AlbGolestandt : GameDragon
    {
        protected override DragonConfig Config => Definition;

        internal static readonly DragonConfig Definition = new()
        {
            NpcTemplateId = 60157497,
            FactionId = 31,
            MeleeDamageType = eDamageType.Crush,
            SpawnPoint = new Point3D(391344, 755419, 395),
            SpawnHeading = 2071,
            SpellKeyPrefix = "Golestandt",

            SpellDamageType = eDamageType.Heat,
            ResistDebuffSpellType = eSpellType.HeatResistDebuff,
            ResistDebuffDescription = "Decreases a target's given resistance to Heat magic by 50%",
            GlareSpellName = "Golestandt's Glare",
            BreathSpellName = "Golestandt's Breath",
            SpellClientEffect = 5700,
            StunClientEffect = 5703,
            ResistDebuffClientEffect = 777,
            BreathClientEffect = 5700,
            RoamGlareSpellId = 11955,
            GlareSpellId = 11956,
            BreathSpellId = 11957,
            StunSpellId = 11958,
            ResistDebuffSpellId = 11965,

            GlareTexts =
            [
                "{0} shouts, 'Foolish {1}! Your flesh will make a splendid meal.'",
                "{0} shouts, 'Perhaps your dark ages would end if {1}s like you continue to be weeded out!'",
                "{0} shouts, 'Meddle not in the affairs of dragons, {1}! Yes, you are indeed crunchy.'"
            ],
            BreathTexts =
            [
                "You feel a rush of air flow past you as {0} inhales deeply!",
                "{0} takes another powerful breath as he prepares to unleash a raging inferno upon you!",
                "{0} bellows in rage and glares at all of the creatures attacking him.",
                "{0} noticeably winces from his wounds as he attempts to prepare for yet another life-threatening attack!"
            ],
            RoamStartText = "The skies darken as {0} takes wing, and a voice explodes across the land, 'I will grind your bones and shred your flesh!'",
            StunTelegraphText = "{0} looks mindfully around.",
            ThrowText = "{0} begins flapping his wings violently. You struggle to hold your footing on the ground!",
            EnemyKilledTaunt = "{0} roars in triumph as another {1} falls before his might.",
            DeathAnnounces =
            [
                "The earth lurches beneath your feet as {0} staggers and topples to the ground."
            ],

            RoamPath =
            [
                (391344, 755419, 395),
                (385865, 756961, 3504),
                (378547, 755862, 3504),
                (373114, 749008, 3504),
                (365764, 745172, 3504),
                (365007, 734622, 3504),
                (366398, 727898, 3504),
                (364666, 722970, 3504),
                (365500, 718003, 3504),
                (362982, 714084, 3504),
                (363536, 706078, 3504),
                (374879, 705288, 3504),
                (382939, 704836, 4649),
                (388354, 708784, 4649),
                (392940, 712391, 3723),
                (395754, 717498, 3507),
                (395476, 722965, 3507),
                (394829, 726232, 3507),
                (393783, 743566, 3512),
                (381718, 739900, 3512),
                (371903, 718204, 3512),
                (380357, 716827, 3512),
                (388960, 725072, 3512),
                (394914, 726548, 3512),
                (397830, 713380, 3512),
                (407425, 720655, 3512),
                (408918, 742335, 3512),
                (397944, 754701, 3512),
                (391344, 755419, 395)
            ],
            ThrowDestinations =
            [
                new() { X = 391348, Y = 755751, Z = 1815, Heading = 2069 },
                new() { X = 398605, Y = 754458, Z = 1404, Heading = 1042 },
                new() { X = 392450, Y = 743176, Z = 1404, Heading = 4063 },
                new() { X = 383669, Y = 758112, Z = 1847, Heading = 3003 },
                new() { X = 401432, Y = 755310, Z = 1728, Heading = 1065 }
            ],
            MessengerSpawnPoint = new Point3D(391345, 755661, 410),
            MessengerPaths =
            [
                [(391353, 752390, 200), (391185, 749212, 363), (393537, 748310, 563)],
                [(394175, 755677, 200), (395993, 753955, 200), (397102, 752316, 448)],
                [(390959, 758732, 431), (394489, 758450, 411), (397504, 756902, 324)],
                [(388541, 754932, 204), (386798, 756434, 323), (385011, 757680, 523)]
            ],
            AddReturnPaths =
            [
                [(393690, 747560, 585), (391348, 749033, 374), (391351, 755567, 412)],
                [(397218, 752345, 444), (394848, 755457, 200), (391445, 755608, 410)],
                [(397504, 756902, 324), (390933, 758814, 446), (391331, 755730, 398)],
                [(384804, 757887, 537), (388423, 754910, 212), (391103, 755534, 380)]
            ],

            MessengerName = "Golestandt's messenger",
            MessengerModel = 2386,
            MessengerSize = 80,
            AddVariants =
            [
                new() { Name = "granite giant stonelord", Model = 2386, MinSize = 150, MaxSize = 170 },
                new() { Name = "granite giant pounder", Model = 2386, MinSize = 130, MaxSize = 150 },
                new() { Name = "granite giant outlooker", Model = 2386, MinSize = 130, MaxSize = 140 }
            ],

            CreateMessenger = pathIndex => new GolestandtMessenger { PathIndex = pathIndex },
            CreateAdd = pathIndex => new GolestandtSpawnedAdd { PathIndex = pathIndex },

            BountyPointsReward = 0,
            CurrencyItemTemplateId = "dragonscales",
            CurrencyItemCount = 10
        };
    }

    public class GolestandtMessenger : DragonMessenger
    {
        public GolestandtMessenger() : base(AlbGolestandt.Definition) { }
    }

    public class GolestandtSpawnedAdd : DragonAdd
    {
        public GolestandtSpawnedAdd() : base(AlbGolestandt.Definition) { }
    }
}
