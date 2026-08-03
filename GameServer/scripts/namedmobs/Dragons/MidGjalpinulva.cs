namespace DOL.GS
{
    public class MidGjalpinulva : GameDragon
    {
        protected override DragonConfig Config => Definition;

        internal static readonly DragonConfig Definition = new()
        {
            NpcTemplateId = 694189,
            FactionId = 781,
            MeleeDamageType = eDamageType.Crush,
            SpawnPoint = new Point3D(708888, 1021439, 3014),
            SpawnHeading = 2531,
            SpellKeyPrefix = "Gjalpinulva",

            SpellDamageType = eDamageType.Cold,
            ResistDebuffSpellType = eSpellType.ColdResistDebuff,
            ResistDebuffDescription = "Decreases a target's given resistance to Cold magic by 50%",
            GlareSpellName = "Gjalpinulva's Glare",
            BreathSpellName = "Gjalpinulva's Breath",
            SpellClientEffect = 5701,
            StunClientEffect = 5703,
            ResistDebuffClientEffect = 2976,
            BreathClientEffect = 5701,
            RoamGlareSpellId = 11954,
            GlareSpellId = 11953,
            BreathSpellId = 11952,
            StunSpellId = 11951,
            ResistDebuffSpellId = 11964,

            GlareTexts =
            [
                "{0} shouts, 'Odin will have to do without your aid at Ragnarök, {1}!'",
                "{0} shouts, 'There shall be no valkyries bearing you this day, {1}!'",
                "{0} shouts, 'May your corpse rot on Nastrand, {1}!'",
                "{0} shouts, 'My aunt has a wonderful place reserved for {1}s like you in Niflheim!'"
            ],
            BreathTexts =
            [
                "You feel a rush of air flow past you as {0} inhales deeply!",
                "{0} takes another powerful breath as she prepares to unleash a raging blizzard upon you!",
                "{0} bellows in rage and glares at all of the creatures attacking her.",
                "{0} noticeably winces from her wounds as she attempts to prepare for yet another life-threatening attack!"
            ],
            RoamStartText = "A booming voice echoes through the canyons as {0} rises from her lair, 'I grow restless. Who has dared to enter my domain? I shall freeze their flesh and grind their bones to dust!'",
            StunTelegraphText = "{0} looks mindfully around.",
            ThrowText = "{0} begins flapping her wings violently. You struggle to hold your footing on the ground!",
            EnemyKilledTaunt = "{0} shouts, 'Your soul now belongs to me, {1}!'",
            DeathAnnounces =
            [
                "A soul-piercing howl echoes throughout the land, and then all is quiet."
            ],

            RoamPath =
            [
                (708888, 1021439, 3014),
                (712650, 1016043, 5106),
                (710579, 1007943, 5106),
                (703830, 998367, 5106),
                (695888, 990438, 5106),
                (695600, 979446, 5106),
                (701990, 980841, 5106),
                (709579, 986573, 5106),
                (714571, 984901, 5106),
                (719998, 983284, 5106),
                (721001, 993999, 5106),
                (720992, 999819, 5106),
                (728387, 1010676, 5106),
                (737301, 1010536, 5106),
                (736273, 1000467, 5106),
                (729920, 999398, 5106),
                (727483, 987398, 5106),
                (722107, 982002, 5106),
                (722974, 978111, 5106),
                (731811, 979376, 6057),
                (741124, 981185, 6057),
                (745175, 992884, 6057),
                (746278, 1001302, 5341),
                (746067, 1006105, 5341),
                (747528, 1010486, 5341),
                (747080, 1023245, 5341),
                (727530, 1027210, 5341),
                (715303, 1025848, 5341),
                (708888, 1021439, 3014)
            ],
            ThrowDestinations =
            [
                new() { X = 708632, Y = 1021688, Z = 3721, Heading = 2499 },
                new() { X = 713073, Y = 1015679, Z = 3833, Heading = 441 },
                new() { X = 713388, Y = 1024499, Z = 3833, Heading = 1372 },
                new() { X = 705812, Y = 1024952, Z = 3833, Heading = 2573 },
                new() { X = 706019, Y = 1018867, Z = 3833, Heading = 3521 }
            ],
            MessengerSpawnPoint = new Point3D(708770, 1021639, 3030),
            MessengerPaths =
            [
                [(710329, 1019375, 2824), (710514, 1016616, 2893), (710434, 1013684, 2783)],
                [(706980, 1019434, 2824), (702629, 1021259, 2800), (699391, 1019292, 2681)],
                [(710841, 1023038, 2824), (714212, 1025142, 2782)],
                [(706824, 1023914, 2759), (708924, 1025927, 2817), (712828, 1026645, 2824)]
            ],
            AddReturnPaths =
            [
                [(710646, 1016748, 2918), (710546, 1018812, 2824), (708814, 1021611, 3028)],
                [(702705, 1020839, 2818), (707015, 1019589, 2824), (708814, 1021611, 3028)],
                [(712485, 1024302, 2824), (708814, 1021611, 3028)],
                [(709203, 1025740, 2824), (706957, 1024039, 2745), (708814, 1021611, 3028)]
            ],

            MessengerName = "Gjalpinulva's messenger",
            MessengerModel = 626,
            MessengerSize = 50,
            AddVariants =
            [
                new() { Name = "drakulv executioner", Model = 625, MinSize = 130, MaxSize = 150 },
                new() { Name = "drakulv disciple", Model = 617, MinSize = 120, MaxSize = 140 },
                new() { Name = "drakulv soultrapper", Model = 624, MinSize = 100, MaxSize = 120 }
            ],

            CreateMessenger = pathIndex => new GjalpinulvaMessenger { PathIndex = pathIndex },
            CreateAdd = pathIndex => new GjalpinulvaSpawnedAdd { PathIndex = pathIndex },

            BountyPointsReward = 0,
            CurrencyItemTemplateId = "dragonscales",
            CurrencyItemCount = 10
        };
    }

    public class GjalpinulvaMessenger : DragonMessenger
    {
        public GjalpinulvaMessenger() : base(MidGjalpinulva.Definition) { }
    }

    public class GjalpinulvaSpawnedAdd : DragonAdd
    {
        public GjalpinulvaSpawnedAdd() : base(MidGjalpinulva.Definition) { }
    }
}
