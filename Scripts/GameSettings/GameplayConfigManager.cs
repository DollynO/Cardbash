using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Items;
using Godot;

namespace CardBase.Scripts.GameSettings;

public enum GameplayConfigKind
{
    Ability,
    Item,
}

public sealed class GameplayConfigDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("abilities")]
    public Dictionary<string, GameplayConfigEntry> Abilities { get; set; } = new();

    [JsonPropertyName("items")]
    public Dictionary<string, GameplayConfigEntry> Items { get; set; } = new();
}

public sealed class GameplayConfigEntry
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("iconPath")]
    public string IconPath { get; set; }

    [JsonPropertyName("cooldown")]
    public double? Cooldown { get; set; }

    [JsonPropertyName("baseDamage")]
    public double? BaseDamage { get; set; }

    [JsonPropertyName("damageType")]
    public int? DamageType { get; set; }

    [JsonPropertyName("maxStack")]
    public int? MaxStack { get; set; }

    [JsonPropertyName("baseAilmentChance")]
    public double? BaseAilmentChance { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, double> Params { get; set; } = new();

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(DisplayName)
               && string.IsNullOrWhiteSpace(Description)
               && string.IsNullOrWhiteSpace(IconPath)
               && !Cooldown.HasValue
               && !BaseDamage.HasValue
               && !DamageType.HasValue
               && !MaxStack.HasValue
               && !BaseAilmentChance.HasValue
               && (Params == null || Params.Count == 0);
    }
}

public static class GameplayConfigManager
{
    private const string BaseConfigPath = "res://Config/card_base_config.json";
    private const string CustomConfigPath = "user://card_custom_config.json";
    private const double DiffTolerance = 0.0001;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static GameplayConfigDocument BaseConfig { get; private set; }
    public static GameplayConfigDocument CustomConfig { get; private set; }
    public static GameplayConfigDocument ActiveConfig { get; private set; }
    public static string ActiveHash { get; private set; } = string.Empty;

    public static void LoadHostConfig()
    {
        BaseConfig = LoadDocument(BaseConfigPath) ?? new GameplayConfigDocument();
        CustomConfig = LoadDocument(CustomConfigPath) ?? new GameplayConfigDocument();
        ActiveConfig = Merge(BaseConfig, CustomConfig);
        ActiveHash = ComputeHash(Serialize(ActiveConfig));
    }

    public static void EnsureLoaded()
    {
        if (ActiveConfig != null)
        {
            return;
        }

        LoadHostConfig();
    }

    public static bool LoadSyncedConfig(string json, string expectedHash, out string error)
    {
        error = string.Empty;
        var hash = ComputeHash(json);
        if (!string.IsNullOrWhiteSpace(expectedHash) && hash != expectedHash)
        {
            error = $"Gameplay config hash mismatch. Expected {expectedHash}, got {hash}.";
            return false;
        }

        var document = Deserialize(json);
        if (document == null)
        {
            error = "Failed to parse gameplay config JSON.";
            return false;
        }

        BaseConfig ??= LoadDocument(BaseConfigPath) ?? new GameplayConfigDocument();
        CustomConfig = new GameplayConfigDocument();
        ActiveConfig = document;
        ActiveHash = hash;
        return true;
    }

    public static string GetMergedJson()
    {
        EnsureLoaded();
        return Serialize(ActiveConfig);
    }

    public static string GetMergedHash()
    {
        return ComputeHash(GetMergedJson());
    }

    public static IReadOnlyDictionary<string, GameplayConfigEntry> GetEntries(GameplayConfigKind kind)
    {
        EnsureLoaded();
        return GetMutableEntries(ActiveConfig, kind);
    }

    public static GameplayConfigEntry GetEntry(GameplayConfigKind kind, string guid)
    {
        EnsureLoaded();
        return GetMutableEntries(ActiveConfig, kind).GetValueOrDefault(guid);
    }

    public static void ResetEntry(GameplayConfigKind kind, string guid)
    {
        EnsureLoaded();
        var activeEntries = GetMutableEntries(ActiveConfig, kind);
        var baseEntries = GetMutableEntries(BaseConfig, kind);
        if (baseEntries.TryGetValue(guid, out var baseEntry))
        {
            activeEntries[guid] = Clone(baseEntry);
        }
        else
        {
            activeEntries.Remove(guid);
        }
    }

    public static void ResetAll()
    {
        EnsureLoaded();
        ActiveConfig = Clone(BaseConfig);
        CustomConfig = new GameplayConfigDocument();
        ActiveHash = ComputeHash(Serialize(ActiveConfig));
    }

    public static void SaveActiveAsCustomConfig()
    {
        EnsureLoaded();
        CustomConfig = Diff(BaseConfig, ActiveConfig);
        var customJson = Serialize(CustomConfig);
        using var file = Godot.FileAccess.Open(CustomConfigPath, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(customJson);
        ActiveHash = ComputeHash(Serialize(ActiveConfig));
    }

    public static void ApplyToAbility(Ability ability)
    {
        if (ability == null)
        {
            return;
        }

        EnsureLoaded();
        if (ActiveConfig.Abilities.TryGetValue(ability.GUID, out var entry))
        {
            ability.ApplyGameplayConfig(entry);
        }
    }

    public static void ApplyToItem(Item item)
    {
        if (item == null)
        {
            return;
        }

        EnsureLoaded();
        if (ActiveConfig.Items.TryGetValue(item.GUID, out var entry))
        {
            item.ApplyGameplayConfig(entry);
        }
    }

    public static float GetAbilityParam(string guid, string paramName, float fallback)
    {
        return GetParam(GameplayConfigKind.Ability, guid, paramName, fallback);
    }

    public static int GetAbilityParam(string guid, string paramName, int fallback)
    {
        return Mathf.RoundToInt(GetAbilityParam(guid, paramName, (float)fallback));
    }

    public static float GetItemParam(string guid, string paramName, float fallback)
    {
        return GetParam(GameplayConfigKind.Item, guid, paramName, fallback);
    }

    public static int GetItemParam(string guid, string paramName, int fallback)
    {
        return Mathf.RoundToInt(GetItemParam(guid, paramName, (float)fallback));
    }

    public static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    public static string Serialize(GameplayConfigDocument document)
    {
        return JsonSerializer.Serialize(document ?? new GameplayConfigDocument(), JsonOptions);
    }

    private static float GetParam(GameplayConfigKind kind, string guid, string paramName, float fallback)
    {
        EnsureLoaded();
        var entries = GetMutableEntries(ActiveConfig, kind);
        if (entries.TryGetValue(guid, out var entry)
            && entry.Params != null
            && entry.Params.TryGetValue(paramName, out var value))
        {
            return (float)value;
        }

        return fallback;
    }

    private static GameplayConfigDocument LoadDocument(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return null;
        }

        return Deserialize(file.GetAsText());
    }

    private static GameplayConfigDocument Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GameplayConfigDocument>(json, JsonOptions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Invalid gameplay config JSON: {e.Message}");
            return null;
        }
    }

    private static GameplayConfigDocument Merge(GameplayConfigDocument baseConfig, GameplayConfigDocument customConfig)
    {
        var merged = Clone(baseConfig ?? new GameplayConfigDocument());
        MergeEntries(merged.Abilities, customConfig?.Abilities);
        MergeEntries(merged.Items, customConfig?.Items);
        return merged;
    }

    private static void MergeEntries(Dictionary<string, GameplayConfigEntry> target, Dictionary<string, GameplayConfigEntry> overrides)
    {
        if (overrides == null)
        {
            return;
        }

        foreach (var (guid, overrideEntry) in overrides)
        {
            if (!target.TryGetValue(guid, out var targetEntry))
            {
                target[guid] = Clone(overrideEntry);
                continue;
            }

            ApplyOverride(targetEntry, overrideEntry);
        }
    }

    private static void ApplyOverride(GameplayConfigEntry target, GameplayConfigEntry source)
    {
        if (!string.IsNullOrWhiteSpace(source.DisplayName)) target.DisplayName = source.DisplayName;
        if (!string.IsNullOrWhiteSpace(source.Description)) target.Description = source.Description;
        if (!string.IsNullOrWhiteSpace(source.IconPath)) target.IconPath = source.IconPath;
        if (source.Cooldown.HasValue) target.Cooldown = source.Cooldown;
        if (source.BaseDamage.HasValue) target.BaseDamage = source.BaseDamage;
        if (source.DamageType.HasValue) target.DamageType = source.DamageType;
        if (source.MaxStack.HasValue) target.MaxStack = source.MaxStack;
        if (source.BaseAilmentChance.HasValue) target.BaseAilmentChance = source.BaseAilmentChance;
        if (source.Params == null)
        {
            return;
        }

        target.Params ??= new Dictionary<string, double>();
        foreach (var (key, value) in source.Params)
        {
            target.Params[key] = value;
        }
    }

    private static GameplayConfigDocument Diff(GameplayConfigDocument baseConfig, GameplayConfigDocument activeConfig)
    {
        var diff = new GameplayConfigDocument { Version = activeConfig?.Version ?? 1 };
        DiffEntries(baseConfig?.Abilities, activeConfig?.Abilities, diff.Abilities);
        DiffEntries(baseConfig?.Items, activeConfig?.Items, diff.Items);
        return diff;
    }

    private static void DiffEntries(
        Dictionary<string, GameplayConfigEntry> baseEntries,
        Dictionary<string, GameplayConfigEntry> activeEntries,
        Dictionary<string, GameplayConfigEntry> diffEntries)
    {
        if (activeEntries == null)
        {
            return;
        }

        baseEntries ??= new Dictionary<string, GameplayConfigEntry>();
        foreach (var (guid, activeEntry) in activeEntries)
        {
            baseEntries.TryGetValue(guid, out var baseEntry);
            var diff = DiffEntry(baseEntry, activeEntry);
            if (!diff.IsEmpty())
            {
                diffEntries[guid] = diff;
            }
        }
    }

    private static GameplayConfigEntry DiffEntry(GameplayConfigEntry baseEntry, GameplayConfigEntry activeEntry)
    {
        var diff = new GameplayConfigEntry();
        if (!SameString(baseEntry?.DisplayName, activeEntry.DisplayName)) diff.DisplayName = activeEntry.DisplayName;
        if (!SameString(baseEntry?.Description, activeEntry.Description)) diff.Description = activeEntry.Description;
        if (!SameString(baseEntry?.IconPath, activeEntry.IconPath)) diff.IconPath = activeEntry.IconPath;
        if (!SameNullable(baseEntry?.Cooldown, activeEntry.Cooldown)) diff.Cooldown = activeEntry.Cooldown;
        if (!SameNullable(baseEntry?.BaseDamage, activeEntry.BaseDamage)) diff.BaseDamage = activeEntry.BaseDamage;
        if (baseEntry?.DamageType != activeEntry.DamageType) diff.DamageType = activeEntry.DamageType;
        if (baseEntry?.MaxStack != activeEntry.MaxStack) diff.MaxStack = activeEntry.MaxStack;
        if (!SameNullable(baseEntry?.BaseAilmentChance, activeEntry.BaseAilmentChance)) diff.BaseAilmentChance = activeEntry.BaseAilmentChance;

        var baseParams = baseEntry?.Params ?? new Dictionary<string, double>();
        foreach (var (key, value) in activeEntry.Params ?? new Dictionary<string, double>())
        {
            if (!baseParams.TryGetValue(key, out var baseValue) || Math.Abs(baseValue - value) > DiffTolerance)
            {
                diff.Params[key] = value;
            }
        }

        return diff;
    }

    private static bool SameNullable(double? left, double? right)
    {
        if (!left.HasValue || !right.HasValue)
        {
            return left.HasValue == right.HasValue;
        }

        return Math.Abs(left.Value - right.Value) <= DiffTolerance;
    }

    private static bool SameString(string left, string right)
    {
        return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
    }

    private static Dictionary<string, GameplayConfigEntry> GetMutableEntries(GameplayConfigDocument document, GameplayConfigKind kind)
    {
        return kind == GameplayConfigKind.Ability ? document.Abilities : document.Items;
    }

    private static T Clone<T>(T value)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions);
    }
}
