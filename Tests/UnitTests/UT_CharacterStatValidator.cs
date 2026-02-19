using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_CharacterStatValidator
    {
        [Test]
        public void Validate_NullArguments_ThrowsArgumentNullException()
        {
            int points;
            Dictionary<eStat, int> raceStats = new();
            Dictionary<eStat, int> charStats = new();

            Assert.Throws<ArgumentNullException>(() => CharacterStatValidator.Validate(null, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out points));
            Assert.Throws<ArgumentNullException>(() => CharacterStatValidator.Validate(charStats, null, eStat.STR, eStat.DEX, eStat.CON, 1, out points));
        }

        [Test]
        public void Validate_MissingStatInCharacter_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new();

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_StatBelowRaceBase_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 49 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_Level1_Exact30Points_ReturnsTrue()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 60 },
                { eStat.DEX, 60 },
                { eStat.CON, 60 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_Level1_SecondCostTierAppliedCorrectly_ReturnsTrue()
        {
            Dictionary<eStat, int> raceStats = new() 
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 }
            };

            Dictionary<eStat, int> charStats = new() 
            {
                { eStat.STR, 66 },
                { eStat.DEX, 57 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_Over30Points_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 81 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.GreaterThan(30));
        }

        [Test]
        public void Validate_Under30Points_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 55 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.LessThan(30));
        }

        [Test]
        public void Validate_Level6_CalculatesCorrectly()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 61 },
                { eStat.DEX, 61 },
                { eStat.CON, 61 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 6, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_LevelBasedMinimumNotMet_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 50 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 10, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_FirstCostThresholdBoundary_AppliesIncreasedCost()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 61 },
                { eStat.DEX, 59 },
                { eStat.CON, 59 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_SecondCostTierBoundary_CalculatesCorrectly()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 65 },
                { eStat.DEX, 60 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_Level7_AutoStatProgressionAccumulatesCorrectly()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 62 },
                { eStat.DEX, 61 },
                { eStat.CON, 61 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 7, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_EmptyRaceStats_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new();
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 60 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_AllStatsAtMinimum_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.CON, 50 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_SameStatUsedForMultipleRoles_DoesNotBypassMinimum_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 51 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.STR, eStat.CON, 8, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_NoMatchingRoleStatsInRaceStats_ReturnsFalse()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.INT, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.INT, 50 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 6, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_CharacterHasExtraStats_IgnoresNonRaceStats()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 60 },
                { eStat.DEX, 50 },
                { eStat.CON, 60 },
                { eStat.INT, 99 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(20), "Stats not present in raceStats influenced the point calculation.");
        }

        [Test]
        public void Validate_Level9_CalculatesModuloCadenceCorrectly()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.DEX, 50 },
                { eStat.QUI, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 64 },
                { eStat.DEX, 62 },
                { eStat.QUI, 62 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.QUI, 9, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }

        [Test]
        public void Validate_SameStatAssignedToAllRoles_FailsValidation()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 53 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.STR, eStat.STR, 6, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(0));
        }

        [Test]
        public void Validate_HighStatValue_TriggersThirdCostTierCorrectly()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 70 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.DEX, eStat.CON, 1, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(35));
        }

        [Test]
        public void Validate_DuplicateRoleStat_DoesNotDoubleCountPoints()
        {
            Dictionary<eStat, int> raceStats = new() { { eStat.STR, 50 } };
            Dictionary<eStat, int> charStats = new() { { eStat.STR, 68 } };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.STR, eStat.STR, 10, out int points);

            Assert.That(result, Is.False);
            Assert.That(points, Is.EqualTo(8));
        }

        [Test]
        public void Validate_DuplicateRoleStat_WithOtherStats_ReturnsTrue()
        {
            Dictionary<eStat, int> raceStats = new()
            {
                { eStat.STR, 50 },
                { eStat.CON, 50 }
            };

            Dictionary<eStat, int> charStats = new()
            {
                { eStat.STR, 78 },
                { eStat.CON, 51 }
            };

            bool result = CharacterStatValidator.Validate(charStats, raceStats, eStat.STR, eStat.STR, eStat.STR, 10, out int points);

            Assert.That(result, Is.True);
            Assert.That(points, Is.EqualTo(30));
        }
    }
}
