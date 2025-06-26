using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.Styles;
using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.Combat;
using DOL.GS.Interfaces.Character;
using DOL.GS.Interfaces.Items;
using DOL.Database;
using Moq;

namespace Tests.Helpers
{
    /// <summary>
    /// Helper class for creating common mock objects in tests
    /// </summary>
    public static class MockHelper
    {
        /// <summary>
        /// Create a mock character with common properties
        /// </summary>
        public static Mock<GamePlayer> CreateMockCharacter(int level = 50, eCharacterClass? characterClass = null)
        {
            var mock = new Mock<GamePlayer>();
            mock.Setup(x => x.Level).Returns((byte)level);
            
            if (characterClass.HasValue)
            {
                // CharacterClass is of type ICharacterClass, so we need to mock that
                var classMock = new Mock<DOL.GS.ICharacterClass>();
                classMock.Setup(x => x.ID).Returns(Convert.ToInt32(characterClass.Value));
                mock.Setup(x => x.CharacterClass).Returns(classMock.Object);
            }
            
            // Setup common properties that might be accessed
            mock.Setup(x => x.GetModified(It.IsAny<eProperty>())).Returns(0);
            
            return mock;
        }

        /// <summary>
        /// Create a mock weapon with common properties
        /// </summary>
        public static Mock<DbInventoryItem> CreateMockWeapon(int dps = 165, int speed = 37, eDamageType damageType = eDamageType.Slash)
        {
            var mock = new Mock<DbInventoryItem>();
            mock.Setup(w => w.DPS_AF).Returns(dps);
            mock.Setup(w => w.SPD_ABS).Returns(speed);
            mock.Setup(w => w.Type_Damage).Returns((int)damageType);
            
            return mock;
        }

        /// <summary>
        /// Create a mock armor item
        /// </summary>
        public static Mock<DbInventoryItem> CreateMockArmor(int armorFactor = 100, int absorb = 10)
        {
            var mock = new Mock<DbInventoryItem>();
            mock.Setup(a => a.DPS_AF).Returns(armorFactor);
            // Absorb would be set via bonus or other mechanism in actual game
            
            return mock;
        }

        /// <summary>
        /// Create a mock shield
        /// </summary>
        public static Mock<DbInventoryItem> CreateMockShield(int shieldSize = 2)
        {
            var mock = new Mock<DbInventoryItem>();
            mock.Setup(s => s.Type_Damage).Returns(shieldSize); // Shield size stored here
            mock.Setup(s => s.Object_Type).Returns((int)eObjectType.Shield);
            
            return mock;
        }

        /// <summary>
        /// Create a mock style
        /// </summary>
        public static Mock<Style> CreateMockStyle(int bonusToHit = 0, int bonusToDefense = 0)
        {
            var mock = new Mock<Style>();
            // Style properties would be set through the underlying DbStyle
            
            return mock;
        }

        /// <summary>
        /// Create a mock ammo
        /// </summary>
        public static Mock<DbInventoryItem> CreateMockAmmo(eObjectType ammoType = eObjectType.Arrow, int quality = 100)
        {
            var mock = new Mock<DbInventoryItem>();
            mock.Setup(a => a.Object_Type).Returns((int)ammoType);
            mock.Setup(a => a.Quality).Returns(quality);
            
            return mock;
        }

        /// <summary>
        /// Helper to create attack data with common defaults
        /// </summary>
        public static DOL.GS.AttackData CreateAttackData(GameLiving attacker = null, GameLiving target = null)
        {
            return new DOL.GS.AttackData
            {
                Attacker = attacker,
                Target = target,
                AttackType = DOL.GS.AttackData.eAttackType.MeleeOneHand,
                AttackResult = eAttackResult.Any,
                ArmorHitLocation = DOL.GS.eArmorSlot.NOTSET
            };
        }
    }
} 