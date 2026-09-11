using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public partial class StatblockComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public bool ReplicateStats { get; set; } = true;

    public StatblockComponent()
    {
        Name = nameof(StatblockComponent);
    }

    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
    }

    public Dictionary ReplicatedCurrent = new();

    private readonly StatBlock _stats = new();

    public static readonly IReadOnlyDictionary<DamageType, StatType> DamageBonusStats =
        new System.Collections.Generic.Dictionary<DamageType, StatType>
        {
            [DamageType.Physical] = StatType.DmgPhysicalBonus,
            [DamageType.Poison] = StatType.DmgPoisonBonus,
            [DamageType.Fire] = StatType.DmgFireBonus,
            [DamageType.Ice] = StatType.DmgIceBonus,
            [DamageType.Lightning] = StatType.DmgLightningBonus,
            [DamageType.Darkness] = StatType.DmgDarknessBonus,
            [DamageType.Holy] = StatType.DmgHolyBonus,
        };
    
    public static readonly IReadOnlyDictionary<StatType, (DamageType, DamageType)> DamageConversionStats =
        new System.Collections.Generic.Dictionary<StatType, (DamageType, DamageType)>
        {
            [StatType.DmgConvertFireFrost] = (DamageType.Fire, DamageType.Ice),
            [StatType.DmgConvertFireLightning] = (DamageType.Fire, DamageType.Lightning),
            [StatType.DmgConvertFrostFire] = (DamageType.Ice, DamageType.Fire),
            [StatType.DmgConvertFrostLightning] = (DamageType.Ice, DamageType.Lightning),
            [StatType.DmgConvertLightningFrost] = (DamageType.Lightning, DamageType.Ice),
            [StatType.DmgConvertLightningFire] = (DamageType.Lightning, DamageType.Fire),
            [StatType.DmgConvertFireDarkness] = (DamageType.Fire, DamageType.Darkness),
            [StatType.DmgConvertFireHoly] = (DamageType.Fire, DamageType.Holy),
            [StatType.DmgConvertFrostDarkness] = (DamageType.Ice, DamageType.Darkness),
            [StatType.DmgConvertFrostHoly] = (DamageType.Ice, DamageType.Holy),
            [StatType.DmgConvertLightningDarkness] = (DamageType.Lightning, DamageType.Darkness),
            [StatType.DmgConvertLightningHoly] = (DamageType.Lightning, DamageType.Holy),
        };


    public List<DamageModifier> GetDamageModifiers()
    {
        var modifiers = new List<DamageModifier>();

        foreach (var (damageType, statType) in DamageBonusStats)
        {
            AddDamageModifier(modifiers, damageType, GetStat(statType));
        }

        foreach (var conv in DamageConversionStats)
        {
            
            AddDamageConverterModifier(modifiers, conv.Value.Item1, conv.Value.Item2, GetStat(conv.Key));
        }

        return modifiers;
    }

    private static void AddDamageModifier(List<DamageModifier> modifiers, DamageType damageType, float value)
    {
        if (Math.Abs(value) < 0.0001f)
        {
            return;
        }

        modifiers.Add(new DamageModifier
        {
            TargetDamageType = damageType,
            OutputDamageType = damageType,
            Type = DamageModifierType.Modifier,
            Value = value,
        });
    }

    private static void AddDamageConverterModifier(List<DamageModifier> modifiers, DamageType sourceType,
        DamageType targetType, float value)
    {
        if (Math.Abs(value) < 0.0001f)
        {
            return;
        }
        
        modifiers.Add(new DamageModifier()
        {
            TargetDamageType = sourceType,
            OutputDamageType = targetType,
            Type = DamageModifierType.Conversion,
            Value = value,
        });
    }

    public override void _Ready()
    {
        Name = nameof(StatblockComponent);
    }

    public void Define(StatType stat, float baseValue, float? minValue = null, float? maxValue = null)
    {
        var dict = new Godot.Collections.Dictionary<int, float>()
        {
            {(int)stat, baseValue}
        };

        if (!Multiplayer.IsServer())
        {
            if (!ReplicateStats)
            {
                updateStat(dict);
            }

            return;
        }

        _stats.Define(stat, baseValue, minValue, maxValue);
        SyncStats(dict);
    }

    public float GetStat(StatType stat)
    {
        if (!ReplicatedCurrent.ContainsKey((int)stat)) return 0;

        return (float)ReplicatedCurrent[(int)stat];
    }

    public void AddDamageConverter(DamageType source, DamageType target, float convertValue)
    {
        
    }
    
    public void AddModifiers(StatModifier modifier)
    {
        AddModifiers(new[] { modifier });
    }

    public void AddModifiers(IEnumerable<StatModifier> modifiers)
    {
        if (!Multiplayer.IsServer()) return;

        var dict = new Godot.Collections.Dictionary<int, float>();
        foreach (var modifier in modifiers)
        {
            var affectedKeys = _stats.AddSourceMods(
                modifier.SourceId,
                modifier.Stat,
                new[] { (modifier.Op, modifier.Value) });

            foreach (var (statType, value) in affectedKeys)
            {
                dict[(int)statType] = value;
            }
        }

        SyncStats(dict);
    }

    public void RemoveModifierSource(string sourceId)
    {
        if (!Multiplayer.IsServer()) return;

        var affectedKeys = _stats.RemoveSource(sourceId);
        var dict = new Godot.Collections.Dictionary<int, float>(affectedKeys.ToDictionary(kvp => (int)kvp.Key, kvp => kvp.Value));
        SyncStats(dict);
    }

    private void SyncStats(Godot.Collections.Dictionary<int, float> stats)
    {
        if (ReplicateStats)
        {
            Rpc(MethodName.updateStat, stats);
        }
        else
        {
            updateStat(stats);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void updateStat(Godot.Collections.Dictionary<int, float> stats)
    {
        foreach (var (key, value) in stats)
        {
            if (ReplicatedCurrent.ContainsKey(key))
            {
                ReplicatedCurrent[key] = value;
            }
            else
            {
                ReplicatedCurrent.Add(key, value);
            }
        }
    }

    public string GetStatDebugText()
    {
        var sb = new StringBuilder();
        foreach (var value in Enum.GetValues<StatType>())
        {
            if (ReplicatedCurrent.ContainsKey((int)value))
            {
                sb.AppendLine($"{value} : {ReplicatedCurrent[(int)value]}");
            }
        }

        return sb.ToString();
    }
}
