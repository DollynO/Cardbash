using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardBase.Scripts.PlayerScripts;

public enum StatOp
{
    FlatAdd,
    PercentAdd,
    PercentMult
}

public enum StatType
{
    Life,
    Armor,
    EnergyShield,
    MovementSpeed,
    CritChance,
    CritBonus,
    Darkness,
    Blinding,
    CooldownReduction,

    DmgBonus,
    // typed damage bonus
    DmgLightningBonus,
    DmgIceBonus,
    DmgFireBonus,
    DmgHolyBonus,
    DmgDarknessBonus,
    DmgPhysicalBonus,
    DmgPoisonBonus,

    AddPullRadius,
    AddPullStrength,
}

public sealed class StatBlock
{
    private readonly Dictionary<StatType, float> _base = new();
    private readonly Dictionary<StatType, float> _current = new();
    private readonly Dictionary<StatType, (float Min, float Max)> _clamp = new();
    private readonly Dictionary<StatType, IEnumerable<StatType>> _linkedStats = new();

    private readonly Dictionary<string, Dictionary<StatType, List<(StatOp, float val)>>> _modsPerSourcePerStat = new();


    public void Define(StatType stat, float baseValue, float? min = null, float? max = null, IEnumerable<StatType> linkedStats = null)
    {
        _base[stat] = baseValue;
        if (min.HasValue || max.HasValue)
        {
            _clamp[stat] = (min ?? float.NegativeInfinity, max ?? float.PositiveInfinity);
        }

        if (linkedStats != null)
        {
            _linkedStats[stat] = linkedStats;
        }

        Recompute(stat);
    }

    public void SetBase(StatType stat, float v)
    {
        _base[stat] = v;
        Recompute(stat);
    }

    public float GetBase(StatType stat) => _base.GetValueOrDefault(stat, 0f);
    public float Get(StatType stat) => _current.GetValueOrDefault(stat, 0f);
    public IReadOnlyDictionary<StatType, float> Current => _current;

    public IEnumerable<KeyValuePair<StatType, float>> AddSourceMods(string sourceId, StatType stat, IEnumerable<(StatOp op, float val)> mods)
    {
        if (!_modsPerSourcePerStat.TryGetValue(sourceId, out var byStat))
        {
            _modsPerSourcePerStat[sourceId] = byStat = new();
        }
        if (!byStat.TryGetValue(stat, out var list))
        {
            byStat[stat] = list = new();
        }

        var valueTuples = mods.ToList();
        list.AddRange(valueTuples);
        Recompute(stat);

        var changedStats = new HashSet<StatType> { stat };
        if (_linkedStats.TryGetValue(stat, out var linkedStatList))
        {
            foreach (var linkedStat in linkedStatList)
            {
                AddSourceMods(sourceId, linkedStat, valueTuples.ToList());
                changedStats.Add(linkedStat);
            }
        }

        return _current.Where(kvp => changedStats.Contains(kvp.Key));
    }

    public IEnumerable<KeyValuePair<StatType, float>> RemoveSource(string sourceId)
    {
        if (!_modsPerSourcePerStat.ContainsKey(sourceId))
        {
            return Enumerable.Empty<KeyValuePair<StatType, float>>();
        }

        if (!_modsPerSourcePerStat.Remove(sourceId, out var byStat))
            return Enumerable.Empty<KeyValuePair<StatType, float>>();

        foreach (var stat in byStat.Keys)
        {
            Recompute(stat);
        }

        return _current.Where(kvp => byStat.Keys.Contains(kvp.Key));
    }

    private void Recompute(StatType stat)
    {
        var b = _base.GetValueOrDefault(stat, 0f);
        float flat = 0f, addPct = 0f, mult = 1f;

        foreach (var (_, byStat) in _modsPerSourcePerStat)
        {
            if (!byStat.TryGetValue(stat, out var list))
            {
                continue;
            }

            foreach (var (op, val) in list)
            {
                switch (op)
                {
                    case StatOp.FlatAdd: flat += val; break;
                    case StatOp.PercentAdd: addPct += val; break;
                    case StatOp.PercentMult: mult *= (1f + val); break;
                }
            }
        }

        var finalVal = (b + flat) * (1f + addPct) * mult;
        if (_clamp.TryGetValue(stat, out var clamp))
        {
            finalVal = Mathf.Clamp(finalVal, clamp.Min, clamp.Max);
        }

        _current[stat] = finalVal;
    }

    public void RecomputeAll()
    {
        foreach (var stat in _base.Keys) Recompute(stat);
    }
}

public sealed class StatModifier
{
    public readonly string Id;          // unique per application
    public readonly string SourceId;    // optional: group/removal
    public readonly StatType Stat;
    public readonly StatOp Op;
    public float Value;

    public StatModifier(string sourceId, StatType stat, StatOp op, float value)
    {
        Id = Guid.NewGuid().ToString("N");
        SourceId = sourceId ?? "";
        Stat = stat;
        Op = op;
        Value = value;
    }
}
