using System;
using System.Collections.Generic;
using System.Threading;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PropertyCalc;

public sealed class MultiplicativePropertiesHybrid : IMultiplicativeProperties
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, PropertyEntry> _properties = new();

    public void Set(int index, ECSGameEffect effect, double value)
    {
        EffectKey key = GetKey(effect);

        lock (_lock)
        {
            if (!_properties.TryGetValue(index, out PropertyEntry entry))
                _properties[index] = entry = new();

            entry.Values[key] = value;
            entry.CalculateCachedValue();
        }
    }

    public void Set(int index, IGameEffect effect, double value)
    {
        EffectKey key = GetKey(effect);

        lock (_lock)
        {
            if (!_properties.TryGetValue(index, out PropertyEntry entry))
                _properties[index] = entry = new();

            entry.Values[key] = value;
            entry.CalculateCachedValue();
        }
    }

    public void Remove(int index, ECSGameEffect effect)
    {
        EffectKey key = GetKey(effect);

        lock (_lock)
        {
            if (!_properties.TryGetValue(index, out PropertyEntry entry))
                return;

            if (!entry.Values.Remove(key))
                return;

            if (entry.Values.Count == 0)
                _properties.Remove(index);
            else
                entry.CalculateCachedValue();
        }
    }

    public void Remove(int index, IGameEffect effect)
    {
        EffectKey key = GetKey(effect);

        lock (_lock)
        {
            if (!_properties.TryGetValue(index, out PropertyEntry entry))
                return;

            if (!entry.Values.Remove(key))
                return;

            if (entry.Values.Count == 0)
                _properties.Remove(index);
            else
                entry.CalculateCachedValue();
        }
    }

    public double Get(int index)
    {
        return _properties.TryGetValue(index, out PropertyEntry entry) ? entry.CachedValue : 1.0;
    }

    private static EffectKey GetKey(ECSGameEffect effect)
    {
        if (effect.SpellHandler != null)
            return new EffectKey(EffectKeyType.Spell, effect.SpellHandler.Spell.ID);
        else
            return new EffectKey(EffectKeyType.Ability, effect.Icon);
    }

    private static EffectKey GetKey(IGameEffect effect)
    {
        return new EffectKey(EffectKeyType.Legacy, effect.Icon);
    }

    private sealed class PropertyEntry
    {
        public double CachedValue { get; private set; } = 1.0;
        public Dictionary<EffectKey, double> Values { get; private set; } = new();

        public void CalculateCachedValue()
        {
            if (Values.Count == 0)
            {
                CachedValue = 1.0;
                return;
            }

            double result = 1.0;

            foreach (double value in Values.Values)
                result *= value;

            CachedValue = result;
        }
    }

    private readonly struct EffectKey : IEquatable<EffectKey>
    {
        private readonly EffectKeyType Tag;
        private readonly int Id;

        public EffectKey(EffectKeyType tag, int id)
        {
            Tag = tag;
            Id = id;
        }

        public bool Equals(EffectKey other)
        {
            return Tag == other.Tag && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is EffectKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tag, Id);
        }

        public override string ToString()
        {
            return $"{Tag}:{Id}";
        }
    }

    private enum EffectKeyType
    {
        Spell,
        Ability,
        Legacy
    }
} 
