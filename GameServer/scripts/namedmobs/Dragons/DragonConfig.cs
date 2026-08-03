using System;

namespace DOL.GS
{
    public sealed class DragonConfig
    {
        public sealed class ThrowDestination
        {
            public int X { get; init; }
            public int Y { get; init; }
            public int Z { get; init; }
            public ushort Heading { get; init; }
        }

        public sealed class AddVariant
        {
            public string Name { get; init; }
            public ushort Model { get; init; }
            public byte MinSize { get; init; }
            public byte MaxSize { get; init; }
        }

        public int NpcTemplateId { get; init; }
        public int FactionId { get; init; }
        public eDamageType MeleeDamageType { get; init; }
        public Point3D SpawnPoint { get; init; }
        public ushort SpawnHeading { get; init; }
        public string SpellKeyPrefix { get; init; }

        public int BaseMaxHealth { get; init; } = 300000;

        public eDamageType SpellDamageType { get; init; }
        public eSpellType ResistDebuffSpellType { get; init; }
        public string ResistDebuffDescription { get; init; }
        public string GlareSpellName { get; init; }
        public string BreathSpellName { get; init; }
        public string StunSpellName { get; init; } = "Dragon's Stun";
        public string ResistDebuffSpellName { get; init; } = "Dragon's Breath";
        public int SpellClientEffect { get; init; }
        public int StunClientEffect { get; init; }
        public int ResistDebuffClientEffect { get; init; }
        public int BreathClientEffect { get; init; }
        public int GlareClientEffect { get; init; } = 5714;
        public int GlareDragonEffect { get; init; } = 14264;
        public int BreathMarkEffect { get; init; } = 13656;
        public int GlareMarkEffect { get; init; } = 5704;
        public int WaveEffect { get; init; } = 6072;
        public int BreathConeRange { get; init; } = 2000;
        public int BreathConeArc { get; init; } = 120;
        public int RoamGlareSpellId { get; init; }
        public int GlareSpellId { get; init; }
        public int BreathSpellId { get; init; }
        public int StunSpellId { get; init; }
        public int ResistDebuffSpellId { get; init; }

        public string[] GlareTexts { get; init; }
        public string[] BreathTexts { get; init; }
        public string GlareTelegraphText { get; init; } = "{0} stares at {1} and prepares a massive attack.";
        public string BreathTelegraphText { get; init; } = "{0} draws in a deep breath, fixing its gaze upon {1}!";
        public string BreathAnchorText { get; init; } = "{0} fixes its deadly gaze upon YOU! Lead its breath away from your allies!";
        public string MessengerWaveTelegraphText { get; init; } = "{0} roars a summons, and the ground trembles near its lair!";
        public string RoamStartText { get; init; }
        public string StunTelegraphText { get; init; }
        public string ThrowText { get; init; }
        public string EnemyKilledTaunt { get; init; }
        public string[] DeathAnnounces { get; init; }

        public (int X, int Y, int Z)[] RoamPath { get; init; }
        public ThrowDestination[] ThrowDestinations { get; init; }
        public Point3D MessengerSpawnPoint { get; init; }
        public (int X, int Y, int Z)[][] MessengerPaths { get; init; }
        public (int X, int Y, int Z)[][] AddReturnPaths { get; init; }

        public string MessengerName { get; init; }
        public ushort MessengerModel { get; init; }
        public byte MessengerSize { get; init; }
        public AddVariant[] AddVariants { get; init; }

        public int GlareCooldownMin { get; init; } = 40000;
        public int GlareCooldownMax { get; init; } = 60000;
        public int StunCooldownMin { get; init; } = 120000;
        public int StunCooldownMax { get; init; } = 180000;
        public int ThrowCooldownMin { get; init; } = 60000;
        public int ThrowCooldownMax { get; init; } = 80000;
        public int MessengerWaveCooldownMin { get; init; } = 80000;
        public int MessengerWaveCooldownMax { get; init; } = 90000;
        public int RoamGlareCooldownMin { get; init; } = 5000;
        public int RoamGlareCooldownMax { get; init; } = 8000;

        /// <summary>
        /// Roster members above the scaling baseline needed for one extra messenger. 0 disables the scaling.
        /// </summary>
        public int PlayersPerMessenger { get; init; } = 4;
        public int MessengerWaveMaxCount { get; init; } = 15;

        public int BountyPointsReward { get; init; }
        public string CurrencyItemTemplateId { get; init; }
        public int CurrencyItemCount { get; init; }

        public Func<int, DragonMessenger> CreateMessenger { get; init; }
        public Func<int, DragonAdd> CreateAdd { get; init; }
    }
}
