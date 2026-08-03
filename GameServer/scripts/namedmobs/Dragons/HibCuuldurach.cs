namespace DOL.GS
{
    public class HibCuuldurach : GameDragon
    {
        protected override DragonConfig Config => Definition;

        internal static readonly DragonConfig Definition = new()
        {
            NpcTemplateId = 678903,
            FactionId = 83,
            MeleeDamageType = eDamageType.Slash,
            SpawnPoint = new Point3D(408646, 706432, 2965),
            SpawnHeading = 1764,
            SpellKeyPrefix = "Cuuldurach",

            SpellDamageType = eDamageType.Spirit,
            ResistDebuffSpellType = eSpellType.SpiritResistDebuff,
            ResistDebuffDescription = "Decreases a target's given resistance to Spirit magic by 50%",
            GlareSpellName = "Cuuldurach's Glare",
            BreathSpellName = "Cuuldurach's Breath",
            SpellClientEffect = 5702,
            StunClientEffect = 5703,
            ResistDebuffClientEffect = 4576,
            BreathClientEffect = 5702,
            RoamGlareSpellId = 11959,
            GlareSpellId = 11960,
            BreathSpellId = 11961,
            StunSpellId = 11962,
            ResistDebuffSpellId = 11963,

            GlareTexts =
            [
                "{0} shouts, 'I will crush your bones {1}!'",
                "{0} shouts, 'Your end is near little {1}. I will taste your flesh.'",
                "{0} shouts, '{1} like you should not enter my domain. Your corpse will rest at my lair.'",
                "{0} shouts, 'Tasty poor {1}. I will drain your last life essence from your body.'"
            ],
            BreathTexts =
            [
                "You feel a rush of air flow past you as {0} inhales deeply!",
                "{0} takes another powerful breath as he prepares to unleash a raging inferno upon you!",
                "{0} bellows in rage and glares at all of the creatures attacking him.",
                "{0} noticeably winces from his wounds as he attempts to prepare for yet another life-threatening attack!"
            ],
            RoamStartText = "{0} bellows from the skies, 'Let all who intrude into my domain pay heed. I will seek you out and cast you into the arms of Death if you remain here!'",
            StunTelegraphText = "{0} roars horrifyingly!",
            ThrowText = "{0} begins flapping his wings violently. You struggle to hold your footing on the ground!",
            EnemyKilledTaunt = "{0} laughs at the {1} who has fallen beneath his crushing blow.",
            DeathAnnounces =
            [
                "The hills seem to weep for the loss of their king."
            ],

            RoamPath =
            [
                (408646, 706432, 2965),
                (399021, 704912, 6212),
                (391823, 706981, 6212),
                (379666, 707613, 6212),
                (374210, 703692, 6212),
                (369800, 698565, 6212),
                (376500, 693899, 6212),
                (382065, 695219, 6212),
                (383638, 677035, 6212),
                (391481, 681660, 6810),
                (384378, 684504, 6226),
                (376941, 691151, 6226),
                (373055, 684792, 7197),
                (371289, 666663, 7197),
                (361740, 659874, 7197),
                (367670, 653364, 7197),
                (374128, 652093, 8016),
                (392383, 658971, 8016),
                (399312, 670926, 8016),
                (399806, 678950, 6685),
                (394874, 680283, 6685),
                (399038, 686435, 6685),
                (410606, 672288, 6685),
                (406201, 657594, 8321),
                (411408, 655769, 8321),
                (411061, 673862, 6722),
                (409199, 679881, 6722),
                (409781, 696669, 7148),
                (408646, 706432, 2965)
            ],
            ThrowDestinations =
            [
                new() { X = 408807, Y = 706640, Z = 4315, Heading = 1588 },
                new() { X = 404579, Y = 699656, Z = 4683, Heading = 1840 },
                new() { X = 410650, Y = 698271, Z = 4758, Heading = 2890 },
                new() { X = 402790, Y = 707787, Z = 4083, Heading = 2628 },
                new() { X = 407532, Y = 695634, Z = 4533, Heading = 281 }
            ],
            MessengerSpawnPoint = new Point3D(408752, 706546, 2974),
            MessengerPaths =
            [
                [(407371, 704161, 2760), (405306, 701159, 3491), (404443, 699515, 3783)],
                [(411517, 704273, 2759), (410828, 699897, 3490), (410713, 698129, 3619)],
                [(410511, 709059, 2760), (405922, 709976, 2735), (403053, 707551, 2474)],
                [(405898, 707838, 2760), (403716, 705502, 2660), (401242, 704226, 3277)]
            ],
            AddReturnPaths =
            [
                [(404443, 699515, 3783), (405306, 701159, 3491), (407371, 704161, 2760), (408646, 706432, 2965)],
                [(410713, 698129, 3619), (410828, 699897, 3490), (411424, 704307, 2758), (408646, 706432, 2965)],
                [(403053, 707551, 2474), (405922, 709976, 2735), (410511, 709059, 2760), (408646, 706432, 2965)],
                [(401242, 704226, 3277), (403716, 705502, 2660), (405898, 707838, 2760), (408646, 706432, 2965)]
            ],

            MessengerName = "Cuuldurach's messenger",
            MessengerModel = 2389,
            MessengerSize = 50,
            AddVariants =
            [
                new() { Name = "glimmer geist", Model = 2388, MinSize = 50, MaxSize = 55 },
                new() { Name = "glimmer knight", Model = 2390, MinSize = 50, MaxSize = 55 },
                new() { Name = "glimmer deathwatcher", Model = 2389, MinSize = 50, MaxSize = 55 }
            ],

            CreateMessenger = pathIndex => new CuuldurachMessenger { PathIndex = pathIndex },
            CreateAdd = pathIndex => new CuuldurachSpawnedAdd { PathIndex = pathIndex },

            BountyPointsReward = 0,
            CurrencyItemTemplateId = "dragonscales",
            CurrencyItemCount = 10
        };
    }

    public class CuuldurachMessenger : DragonMessenger
    {
        public CuuldurachMessenger() : base(HibCuuldurach.Definition) { }
    }

    public class CuuldurachSpawnedAdd : DragonAdd
    {
        public CuuldurachSpawnedAdd() : base(HibCuuldurach.Definition) { }
    }
}
