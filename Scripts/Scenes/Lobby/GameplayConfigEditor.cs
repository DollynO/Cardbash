using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.GameSettings;
using Godot;

public partial class GameplayConfigEditor : PanelContainer
{
    private readonly List<FieldBinding> _bindings = new();
    private TabContainer _tabs;
    private Label _statusLabel;

    [Signal]
    public delegate void ConfigSavedEventHandler();

    public override void _Ready()
    {
        Build();
    }

    private void Build()
    {
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = 40;
        OffsetTop = 40;
        OffsetRight = -40;
        OffsetBottom = -40;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        var header = new HBoxContainer();
        root.AddChild(header);

        var title = new Label
        {
            Text = "Custom Card Config",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        header.AddChild(title);

        var resetAll = new Button { Text = "Reset All To Base" };
        resetAll.Pressed += OnResetAllPressed;
        header.AddChild(resetAll);

        var save = new Button { Text = "Save Custom Config" };
        save.Pressed += OnSavePressed;
        header.AddChild(save);

        var close = new Button { Text = "Close" };
        close.Pressed += () => Visible = false;
        header.AddChild(close);

        var hint = new Label
        {
            Text = "Only changed values are saved to user://card_custom_config.json. Game start syncs the merged config to clients.",
        };
        hint.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(hint);

        _tabs = new TabContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(_tabs);

        _statusLabel = new Label();
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_statusLabel);

        RebuildTabs();
    }

    public void Open()
    {
        GameplayConfigManager.LoadHostConfig();
        RebuildTabs();
        Visible = true;
    }

    private void RebuildTabs()
    {
        if (_tabs == null)
        {
            return;
        }

        _bindings.Clear();
        foreach (var child in _tabs.GetChildren().ToList())
        {
            _tabs.RemoveChild(child);
            child.QueueFree();
        }

        AddConfigTab("Abilities", GameplayConfigKind.Ability);
        AddConfigTab("Items", GameplayConfigKind.Item);
        _statusLabel.Text = string.Empty;
    }

    private void AddConfigTab(string title, GameplayConfigKind kind)
    {
        var scroll = new ScrollContainer
        {
            Name = title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        var grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        scroll.AddChild(grid);

        foreach (var (guid, entry) in GameplayConfigManager.GetEntries(kind)
                     .OrderBy(kvp => kvp.Value.DisplayName ?? kvp.Key))
        {
            grid.AddChild(CreateConfigCard(kind, guid, entry));
        }

        _tabs.AddChild(scroll);
    }

    private Control CreateConfigCard(GameplayConfigKind kind, string guid, GameplayConfigEntry entry)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var icon = new TextureRect
        {
            CustomMinimumSize = new Vector2(42, 42),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidth,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = string.IsNullOrWhiteSpace(entry.IconPath) ? null : IconLoader.Instance.LoadImage(entry.IconPath),
        };
        row.AddChild(icon);

        var info = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(140, 0),
        };
        row.AddChild(info);

        var name = new Label
        {
            Text = string.IsNullOrWhiteSpace(entry.DisplayName) ? guid : entry.DisplayName,
            ClipText = true,
        };
        name.AddThemeFontSizeOverride("font_size", 14);
        info.AddChild(name);

        var id = new Label
        {
            Text = guid.Length > 8 ? guid[^8..] : guid,
            TooltipText = guid,
        };
        id.AddThemeFontSizeOverride("font_size", 10);
        info.AddChild(id);

        var reset = new Button
        {
            Text = "Base",
            CustomMinimumSize = new Vector2(60, 24),
        };
        reset.Pressed += () =>
        {
            GameplayConfigManager.ResetEntry(kind, guid);
            RebuildTabs();
        };
        info.AddChild(reset);

        var fields = new GridContainer
        {
            Columns = 4,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        fields.AddThemeConstantOverride("h_separation", 4);
        fields.AddThemeConstantOverride("v_separation", 2);
        row.AddChild(fields);

        if (kind == GameplayConfigKind.Ability)
        {
            AddCommonField(fields, entry, CommonField.Cooldown, "CD", "Cooldown", entry.Cooldown ?? 1);
            AddCommonField(fields, entry, CommonField.BaseDamage, "DMG", "Base Damage", entry.BaseDamage ?? 0);
            AddDamageTypeField(fields, entry, entry.DamageType ?? 0);
            AddCommonField(fields, entry, CommonField.MaxStack, "Stack", "Maximum Stacks", entry.MaxStack ?? 1);
            AddCommonField(fields, entry, CommonField.BaseAilmentChance, "Ail", "Base Ailment Chance", entry.BaseAilmentChance ?? 0);
        }

        foreach (var param in (entry.Params ?? new Dictionary<string, double>()).OrderBy(kvp => kvp.Key))
        {
            AddParamField(fields, entry, param.Key, param.Value);
        }

        return panel;
    }

    private void AddCommonField(GridContainer parent, GameplayConfigEntry entry, CommonField field, string label, string tooltip, object value)
    {
        AddField(parent, label, tooltip, ValueToText(value), new FieldBinding
        {
            Entry = entry,
            CommonField = field,
        });
    }

    private void AddParamField(GridContainer parent, GameplayConfigEntry entry, string paramName, double value)
    {
        if (paramName == "movementMode")
        {
            AddMovementModeField(parent, entry, paramName, Mathf.RoundToInt(value));
            return;
        }

        AddField(parent, ShortLabel(paramName), FormatParamTooltip(paramName), FormatNumber(value), new FieldBinding
        {
            Entry = entry,
            ParamName = paramName,
        });
    }

    private void AddDamageTypeField(GridContainer parent, GameplayConfigEntry entry, int damageType)
    {
        var box = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(92, 0),
        };

        var label = new Label
        {
            Text = "Type",
            TooltipText = "Damage Type",
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        box.AddChild(label);

        var input = new OptionButton
        {
            TooltipText = "Damage Type",
            CustomMinimumSize = new Vector2(90, 26),
        };
        input.AddThemeFontSizeOverride("font_size", 12);

        foreach (var value in Enum.GetValues<DamageType>())
        {
            input.AddItem(value.ToString(), (int)value);
            if ((int)value == damageType)
            {
                input.Selected = input.ItemCount - 1;
            }
        }

        box.AddChild(input);
        _bindings.Add(new FieldBinding
        {
            Entry = entry,
            CommonField = CommonField.DamageType,
            OptionInput = input,
        });
        parent.AddChild(box);
    }

    private void AddMovementModeField(GridContainer parent, GameplayConfigEntry entry, string paramName, int movementMode)
    {
        var box = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(92, 0),
        };

        var label = new Label
        {
            Text = "Move",
            TooltipText = "Movement Mode",
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        box.AddChild(label);

        var input = new OptionButton
        {
            TooltipText = "Movement Mode",
            CustomMinimumSize = new Vector2(90, 26),
        };
        input.AddThemeFontSizeOverride("font_size", 12);

        foreach (var value in Enum.GetValues<MovementMode>())
        {
            input.AddItem(value.ToString(), (int)value);
            if ((int)value == movementMode)
            {
                input.Selected = input.ItemCount - 1;
            }
        }

        box.AddChild(input);
        _bindings.Add(new FieldBinding
        {
            Entry = entry,
            ParamName = paramName,
            OptionInput = input,
        });
        parent.AddChild(box);
    }

    private void AddField(GridContainer parent, string labelText, string tooltipText, string valueText, FieldBinding binding)
    {
        var box = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(72, 0),
        };

        var label = new Label
        {
            Text = labelText,
            TooltipText = tooltipText,
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        box.AddChild(label);

        var input = new LineEdit
        {
            Text = valueText,
            TooltipText = tooltipText,
            CustomMinimumSize = new Vector2(70, 26),
            SelectAllOnFocus = true,
        };
        input.AddThemeFontSizeOverride("font_size", 12);
        box.AddChild(input);

        binding.Input = input;
        _bindings.Add(binding);
        parent.AddChild(box);
    }

    private void OnSavePressed()
    {
        if (!ApplyInputs(out var error))
        {
            _statusLabel.Text = error;
            return;
        }

        GameplayConfigManager.SaveActiveAsCustomConfig();
        _statusLabel.Text = "Saved custom config.";
        EmitSignal(SignalName.ConfigSaved);
    }

    private void OnResetAllPressed()
    {
        GameplayConfigManager.ResetAll();
        GameplayConfigManager.SaveActiveAsCustomConfig();
        RebuildTabs();
        EmitSignal(SignalName.ConfigSaved);
    }

    private bool ApplyInputs(out string error)
    {
        error = string.Empty;
        foreach (var binding in _bindings)
        {
            if (binding.CommonField == CommonField.DamageType)
            {
                binding.Entry.DamageType = binding.OptionInput.GetSelectedId();
                continue;
            }

            if (binding.OptionInput != null)
            {
                binding.Entry.Params[binding.ParamName] = binding.OptionInput.GetSelectedId();
                continue;
            }

            var raw = binding.Input.Text.Trim();
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                error = $"Invalid number: {raw}";
                return false;
            }

            switch (binding.CommonField)
            {
                case CommonField.Cooldown:
                    binding.Entry.Cooldown = parsed;
                    break;
                case CommonField.BaseDamage:
                    binding.Entry.BaseDamage = parsed;
                    break;
                case CommonField.MaxStack:
                    binding.Entry.MaxStack = Mathf.RoundToInt(parsed);
                    break;
                case CommonField.BaseAilmentChance:
                    binding.Entry.BaseAilmentChance = parsed;
                    break;
                case CommonField.None:
                    binding.Entry.Params[binding.ParamName] = parsed;
                    break;
            }
        }

        return true;
    }

    private static string ShortLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var upper = new string(value.Where(char.IsUpper).ToArray());
        if (upper.Length >= 2)
        {
            return upper.Length > 5 ? upper[..5] : upper;
        }

        return value.Length > 8 ? value[..8] : value;
    }

    private static string FormatParamTooltip(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var words = new List<string>();
        var current = string.Empty;
        foreach (var c in value)
        {
            if (char.IsUpper(c) && current.Length > 0)
            {
                words.Add(current);
                current = string.Empty;
            }

            current += c;
        }

        if (current.Length > 0)
        {
            words.Add(current);
        }

        return string.Join(" ", words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    private static string ValueToText(object value)
    {
        return value switch
        {
            double d => FormatNumber(d),
            float f => FormatNumber(f),
            int i => i.ToString(CultureInfo.InvariantCulture),
            string s => s,
            _ => value.ToString(),
        };
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private sealed class FieldBinding
    {
        public GameplayConfigEntry Entry;
        public CommonField CommonField;
        public string ParamName;
        public LineEdit Input;
        public OptionButton OptionInput;
    }

    private enum CommonField
    {
        None,
        Cooldown,
        BaseDamage,
        DamageType,
        MaxStack,
        BaseAilmentChance,
    }
}
