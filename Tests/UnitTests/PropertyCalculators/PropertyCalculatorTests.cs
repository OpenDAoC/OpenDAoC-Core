using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using DOL.GS.Interfaces.PropertyCalculators;
using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.Character;

namespace DOL.Tests.Unit.PropertyCalculators
{
    /// <summary>
    /// Unit tests for the Property Calculator System based on game rules documentation
    /// Tests cover calculator types, stacking rules, buff/debuff interactions,
    /// and multiplicative modifiers
    /// </summary>
    [TestFixture]
    public class PropertyCalculatorTests
    {
        private IPropertyCalculatorRegistry _registry;
        private Mock<IPropertySource> _propertySourceMock;
        
        [SetUp]
        public void Setup()
        {
            _registry = new PropertyCalculatorRegistry();
            _propertySourceMock = new Mock<IPropertySource>();
            
            // Register default calculators
            RegisterDefaultCalculators();
        }

        #region Armor Factor Calculator Tests
        
        [Test]
        public void ArmorFactor_ShouldIncludeBaseItemAndBuffs()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.ArmorFactor)).Returns(500);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.ArmorFactor)).Returns(100);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.ArmorFactor)).Returns(50);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.ArmorFactor)).Returns(0);
            
            var calculator = new ArmorFactorCalculator();
            
            // Act
            var totalAF = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalAF.Should().Be(650); // 500 + 100 + 50
        }

        [Test]
        public void ArmorFactor_ShouldApplyDebuffsSubtractively()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.ArmorFactor)).Returns(500);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.ArmorFactor)).Returns(100);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.ArmorFactor)).Returns(50);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.ArmorFactor)).Returns(75);
            
            var calculator = new ArmorFactorCalculator();
            
            // Act
            var totalAF = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalAF.Should().Be(575); // 500 + 100 + 50 - 75
        }

        #endregion

        #region Resistance Calculator Tests
        
        [Test]
        [TestCase(Property.Resist_Body)]
        [TestCase(Property.Resist_Cold)]
        [TestCase(Property.Resist_Energy)]
        [TestCase(Property.Resist_Heat)]
        [TestCase(Property.Resist_Matter)]
        [TestCase(Property.Resist_Spirit)]
        public void Resistances_ShouldStackAdditively(Property resistType)
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(resistType)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(resistType)).Returns(26);
            _propertySourceMock.Setup(x => x.GetBuffBonus(resistType)).Returns(24);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(resistType)).Returns(10);
            
            var calculator = new ResistanceCalculator(resistType);
            
            // Act
            var totalResist = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalResist.Should().Be(40); // 0 + 26 + 24 - 10
        }

        [Test]
        public void Resistances_ShouldBeCappedAt70()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.Resist_Body)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.Resist_Body)).Returns(50);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.Resist_Body)).Returns(50);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.Resist_Body)).Returns(0);
            
            var calculator = new ResistanceCalculator(Property.Resist_Body);
            
            // Act
            var totalResist = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalResist.Should().Be(70); // Capped at 70
        }

        #endregion

        #region Stat Calculator Tests
        
        [Test]
        [TestCase(Property.Strength)]
        [TestCase(Property.Constitution)]
        [TestCase(Property.Dexterity)]
        [TestCase(Property.Quickness)]
        [TestCase(Property.Intelligence)]
        public void Stats_ShouldIncludeAllSources(Property stat)
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(stat)).Returns(75);
            _propertySourceMock.Setup(x => x.GetItemBonus(stat)).Returns(35);
            _propertySourceMock.Setup(x => x.GetBuffBonus(stat)).Returns(60);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(stat)).Returns(0);
            
            var calculator = new StatCalculator(stat);
            
            // Act
            var totalStat = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalStat.Should().Be(170); // 75 + 35 + 60
        }

        [Test]
        public void Stats_DebuffsShouldBeHalved()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.Strength)).Returns(75);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.Strength)).Returns(0);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.Strength)).Returns(0);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.Strength)).Returns(50);
            
            var calculator = new StatCalculator(Property.Strength);
            
            // Act
            var totalStat = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalStat.Should().Be(50); // 75 - (50 / 2)
        }

        [Test]
        public void Stats_ShouldApplyConstitutionLostAtDeath()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.Constitution)).Returns(100);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.Constitution)).Returns(0);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.Constitution)).Returns(0);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.Constitution)).Returns(0);
            _propertySourceMock.Setup(x => x.GetModifiers(Property.Constitution))
                .Returns(new List<IPropertyModifier>
                {
                    new ConstitutionDeathPenalty { Value = 5 }
                });
            
            var calculator = new StatCalculator(Property.Constitution);
            
            // Act
            var totalStat = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalStat.Should().Be(95); // 100 - 5
        }

        #endregion

        #region Speed Calculator Tests
        
        [Test]
        public void MeleeSpeed_ShouldStackWithDiminishingReturns()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.MeleeSpeed)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.MeleeSpeed)).Returns(10); // 10% from items
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.MeleeSpeed)).Returns(20); // 20% from buff
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.MeleeSpeed)).Returns(0);
            
            var calculator = new MeleeSpeedCalculator();
            
            // Act
            var totalSpeed = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            // Total = 1 - ((1 - 0.1) * (1 - 0.2)) = 1 - (0.9 * 0.8) = 1 - 0.72 = 0.28 = 28%
            totalSpeed.Should().Be(28);
        }

        [Test]
        public void CastingSpeed_ShouldBeCappedAt50Percent()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.CastingSpeed)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.CastingSpeed)).Returns(25);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.CastingSpeed)).Returns(40);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.CastingSpeed)).Returns(0);
            
            var calculator = new CastingSpeedCalculator();
            
            // Act
            var totalSpeed = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalSpeed.Should().Be(50); // Capped at 50%
        }

        #endregion

        #region Buff/Debuff Stacking Tests
        
        [Test]
        public void Buffs_ShouldUseHighestValuePerCategory()
        {
            // Arrange
            var source = new MockPropertySource();
            
            // Add multiple buffs in same category
            source.AddBuff(new PropertyBuff 
            { 
                Property = Property.Strength, 
                Value = 30, 
                Category = BuffCategory.Base 
            });
            source.AddBuff(new PropertyBuff 
            { 
                Property = Property.Strength, 
                Value = 50, 
                Category = BuffCategory.Base 
            });
            
            // Add buff in different category
            source.AddBuff(new PropertyBuff 
            { 
                Property = Property.Strength, 
                Value = 20, 
                Category = BuffCategory.Spec 
            });
            
            // Act
            var buffBonus = source.GetBuffBonus(Property.Strength);
            
            // Assert
            buffBonus.Should().Be(70); // 50 (highest base) + 20 (spec)
        }

        [Test]
        public void ItemBonuses_ShouldStackAdditively()
        {
            // Arrange
            var source = new MockPropertySource();
            
            source.AddItem(new MockItem 
            { 
                Bonuses = new Dictionary<Property, int> 
                { 
                    { Property.Strength, 15 } 
                } 
            });
            source.AddItem(new MockItem 
            { 
                Bonuses = new Dictionary<Property, int> 
                { 
                    { Property.Strength, 20 } 
                } 
            });
            
            // Act
            var itemBonus = source.GetItemBonus(Property.Strength);
            
            // Assert
            itemBonus.Should().Be(35); // 15 + 20
        }

        #endregion

        #region Multiplicative Modifier Tests
        
        [Test]
        public void DamageModifiers_ShouldApplyMultiplicatively()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.MeleeDamage)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.MeleeDamage)).Returns(10); // +10%
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.MeleeDamage)).Returns(15); // +15%
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.MeleeDamage)).Returns(0);
            _propertySourceMock.Setup(x => x.GetModifiers(Property.MeleeDamage))
                .Returns(new List<IPropertyModifier>
                {
                    new DamageModifier { Type = ModifierType.Multiplicative, Value = 110 }, // 1.1x
                    new DamageModifier { Type = ModifierType.Multiplicative, Value = 120 }  // 1.2x
                });
            
            var calculator = new MeleeDamageCalculator();
            
            // Act
            var totalDamage = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            // Base: 25% (10 + 15)
            // Multiplicative: 1.1 * 1.2 = 1.32
            // Total: 25 * 1.32 = 33%
            totalDamage.Should().Be(33);
        }

        #endregion

        #region Property Capping Tests
        
        [Test]
        public void CriticalHitChance_ShouldBeCapped()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.CriticalMeleeHit)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.CriticalMeleeHit)).Returns(30);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.CriticalMeleeHit)).Returns(20);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.CriticalMeleeHit)).Returns(0);
            
            var calculator = new CriticalHitCalculator();
            
            // Act
            var critChance = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            critChance.Should().Be(50); // Often capped at 50%
        }

        [Test]
        public void PowerRegeneration_ShouldHaveMinimumValue()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.PowerRegen)).Returns(1);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.PowerRegen)).Returns(0);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.PowerRegen)).Returns(0);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.PowerRegen)).Returns(5);
            
            var calculator = new PowerRegenCalculator();
            
            // Act
            var powerRegen = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            powerRegen.Should().Be(0); // Cannot go negative
        }

        #endregion

        #region Realm Bonus Tests
        
        [Test]
        public void RealmBonuses_ShouldApplyToRelevantProperties()
        {
            // Arrange
            _propertySourceMock.Setup(x => x.GetBase(Property.MeleeDamage)).Returns(0);
            _propertySourceMock.Setup(x => x.GetItemBonus(Property.MeleeDamage)).Returns(10);
            _propertySourceMock.Setup(x => x.GetBuffBonus(Property.MeleeDamage)).Returns(0);
            _propertySourceMock.Setup(x => x.GetDebuffPenalty(Property.MeleeDamage)).Returns(0);
            _propertySourceMock.Setup(x => x.GetModifiers(Property.MeleeDamage))
                .Returns(new List<IPropertyModifier>
                {
                    new RealmBonus { Type = ModifierType.Additive, Value = 10 } // +10% from relics
                });
            
            var calculator = new MeleeDamageCalculator();
            
            // Act
            var totalDamage = calculator.Calculate(_propertySourceMock.Object);
            
            // Assert
            totalDamage.Should().Be(20); // 10 + 10
        }

        #endregion

        #region Helper Methods
        
        private void RegisterDefaultCalculators()
        {
            _registry.Register(Property.ArmorFactor, new ArmorFactorCalculator());
            _registry.Register(Property.Strength, new StatCalculator(Property.Strength));
            _registry.Register(Property.Constitution, new StatCalculator(Property.Constitution));
            _registry.Register(Property.MeleeSpeed, new MeleeSpeedCalculator());
            _registry.Register(Property.CastingSpeed, new CastingSpeedCalculator());
            _registry.Register(Property.MeleeDamage, new MeleeDamageCalculator());
            
            foreach (var resist in new[] { Property.Resist_Body, Property.Resist_Cold, 
                                           Property.Resist_Energy, Property.Resist_Heat, 
                                           Property.Resist_Matter, Property.Resist_Spirit })
            {
                _registry.Register(resist, new ResistanceCalculator(resist));
            }
        }
        
        #endregion
    }

    #region Mock Implementations
    
    public class MockPropertySource : IPropertySource
    {
        private readonly Dictionary<Property, int> _baseValues = new();
        private readonly Dictionary<Property, int> _itemBonuses = new();
        private readonly Dictionary<Property, int> _buffBonuses = new();
        private readonly Dictionary<Property, int> _debuffPenalties = new();
        private readonly Dictionary<Property, List<IPropertyModifier>> _modifiers = new();

        public int GetBase(Property property) => _baseValues.GetValueOrDefault(property, 0);
        public int GetItemBonus(Property property) => _itemBonuses.GetValueOrDefault(property, 0);
        public int GetBuffBonus(Property property) => _buffBonuses.GetValueOrDefault(property, 0);
        public int GetDebuffPenalty(Property property) => _debuffPenalties.GetValueOrDefault(property, 0);
        
        public IEnumerable<IPropertyModifier> GetModifiers(Property property)
        {
            return _modifiers.GetValueOrDefault(property, new List<IPropertyModifier>());
        }

        public void SetBase(Property property, int value) => _baseValues[property] = value;
        public void SetItemBonus(Property property, int value) => _itemBonuses[property] = value;
        public void SetBuffBonus(Property property, int value) => _buffBonuses[property] = value;
        public void SetDebuffPenalty(Property property, int value) => _debuffPenalties[property] = value;
        public void AddModifier(Property property, IPropertyModifier modifier)
        {
            if (!_modifiers.ContainsKey(property))
                _modifiers[property] = new List<IPropertyModifier>();
            _modifiers[property].Add(modifier);
        }
    }
    
    public class PropertyBuff
    {
        public Property Property { get; set; }
        public int Value { get; set; }
        public BuffCategory Category { get; set; }
    }
    
    public class MockItem : IItem
    {
        public string TemplateID { get; set; } = "test_item";
        public string Name { get; set; } = "Test Item";
        public int Level { get; set; } = 50;
        public int BonusLevel { get; set; } = 0;
        public int Quality { get; set; } = 100;
        public int Condition { get; set; } = 100;
        public int Durability { get; set; } = 100;
        public int MaxCondition { get; set; } = 100;
        public ItemType Type { get; set; } = ItemType.Weapon;
        public Dictionary<Property, int> Bonuses { get; set; } = new();
        public bool IsUnique { get; set; } = false;
    }
    
    public class ConstitutionDeathPenalty : IPropertyModifier
    {
        public Property Target => Property.Constitution;
        public int Value { get; set; } = -15;
        public ModifierType Type => ModifierType.Additive;
        public object Source => "Death Penalty";
    }
    
    public class DamageModifier : IPropertyModifier
    {
        public Property Target => Property.MeleeDamage;
        public int Value { get; set; } = 10;
        public ModifierType Type => ModifierType.Additive;
        public object Source => "Damage Buff";
    }
    
    public class RealmBonus : IPropertyModifier
    {
        public Property Target => Property.HitPoints;
        public int Value { get; set; } = 200;
        public ModifierType Type => ModifierType.Additive;
        public object Source => "Realm Rank";
    }
    
    #endregion

    #region Calculator Implementations
    
    public class ArmorFactorCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.ArmorFactor;
        
        public int Calculate(IPropertySource source)
        {
            return source.GetBase(TargetProperty) +
                   source.GetItemBonus(TargetProperty) +
                   source.GetBuffBonus(TargetProperty) -
                   source.GetDebuffPenalty(TargetProperty);
        }
    }
    
    public class ResistanceCalculator : IPropertyCalculator
    {
        private readonly Property _resistType;
        
        public ResistanceCalculator(Property resistType)
        {
            _resistType = resistType;
        }
        
        public Property TargetProperty => _resistType;
        
        public int Calculate(IPropertySource source)
        {
            var total = source.GetBase(TargetProperty) +
                       source.GetItemBonus(TargetProperty) +
                       source.GetBuffBonus(TargetProperty) -
                       source.GetDebuffPenalty(TargetProperty);
                       
            return Math.Min(total, 70); // Cap at 70%
        }
    }
    
    public class StatCalculator : IPropertyCalculator
    {
        private readonly Property _stat;
        
        public StatCalculator(Property stat)
        {
            _stat = stat;
        }
        
        public Property TargetProperty => _stat;
        
        public int Calculate(IPropertySource source)
        {
            var total = source.GetBase(TargetProperty) +
                       source.GetItemBonus(TargetProperty) +
                       source.GetBuffBonus(TargetProperty) -
                       source.GetDebuffPenalty(TargetProperty) / 2; // Debuffs are halved
                       
            // Apply special modifiers (e.g., constitution lost at death)
            foreach (var modifier in source.GetModifiers(TargetProperty))
            {
                if (modifier.Type == ModifierType.Subtractive)
                    total -= modifier.Value;
            }
            
            return total;
        }
    }
    
    public class MeleeSpeedCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.MeleeSpeed;
        
        public int Calculate(IPropertySource source)
        {
            var itemBonus = source.GetItemBonus(TargetProperty) / 100.0;
            var buffBonus = source.GetBuffBonus(TargetProperty) / 100.0;
            
            // Multiplicative stacking with diminishing returns
            var totalReduction = 1 - ((1 - itemBonus) * (1 - buffBonus));
            
            return (int)(totalReduction * 100);
        }
    }
    
    public class CastingSpeedCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.CastingSpeed;
        
        public int Calculate(IPropertySource source)
        {
            var total = source.GetItemBonus(TargetProperty) +
                       source.GetBuffBonus(TargetProperty);
                       
            return Math.Min(total, 50); // Cap at 50%
        }
    }
    
    public class MeleeDamageCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.MeleeDamage;
        
        public int Calculate(IPropertySource source)
        {
            var additive = source.GetItemBonus(TargetProperty) +
                          source.GetBuffBonus(TargetProperty);
                          
            var multiplicative = 1.0;
            foreach (var modifier in source.GetModifiers(TargetProperty))
            {
                if (modifier.Type == ModifierType.Multiplicative)
                    multiplicative *= modifier.Value / 100.0;
                else if (modifier.Type == ModifierType.Additive)
                    additive += modifier.Value;
            }
            
            return (int)(additive * multiplicative);
        }
    }
    
    public class CriticalHitCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.CriticalMeleeHit;
        
        public int Calculate(IPropertySource source)
        {
            var total = source.GetItemBonus(TargetProperty) +
                       source.GetBuffBonus(TargetProperty);
                       
            return Math.Min(total, 50); // Cap at 50%
        }
    }
    
    public class PowerRegenCalculator : IPropertyCalculator
    {
        public Property TargetProperty => Property.PowerRegen;
        
        public int Calculate(IPropertySource source)
        {
            var total = source.GetBase(TargetProperty) +
                       source.GetItemBonus(TargetProperty) +
                       source.GetBuffBonus(TargetProperty) -
                       source.GetDebuffPenalty(TargetProperty);
                       
            return Math.Max(total, 0); // Cannot go negative
        }
    }
    
    #endregion

    #region Enums
    
    public enum BuffCategory
    {
        Base,
        Spec,
        Other,
        Realm
    }
    
    #endregion
} 