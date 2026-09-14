using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CardBase.Scripts.PlayerScripts;

public static class AbilityKeyBindings
{
    public const int SlotCount = 4;
    public const string SettingsPath = "user://ability_key_bindings.txt";

    public static readonly string[] AbilityActions = { "Ability1", "Ability2", "Ability3", "Ability4" };

    private static readonly Key[] DefaultKeys =
    {
        (Key)49,
        (Key)50,
        (Key)51,
        (Key)52
    };

    private static readonly HashSet<Key> BlockedKeys = new()
    {
        (Key)65,
        (Key)68,
        (Key)83,
        (Key)87
    };

    private readonly struct AbilityBinding
    {
        public AbilityBinding(Key key)
        {
            Key = key;
            MouseButton = (MouseButton)0;
            IsMouseButton = false;
        }

        public AbilityBinding(MouseButton mouseButton)
        {
            Key = (Key)0;
            MouseButton = mouseButton;
            IsMouseButton = true;
        }

        public Key Key { get; }
        public MouseButton MouseButton { get; }
        public bool IsMouseButton { get; }
    }

    public static void ApplySavedBindings()
    {
        for (var slot = 0; slot < SlotCount; slot++)
        {
            SetBinding(slot, new AbilityBinding(DefaultKeys[slot]));
        }

        if (!FileAccess.FileExists(SettingsPath))
        {
            return;
        }

        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return;
        }

        for (var slot = 0; slot < SlotCount && !file.EofReached(); slot++)
        {
            var line = file.GetLine().Trim();
            if (!TryParseBinding(line, out var binding) || !IsAllowedBinding(binding))
            {
                continue;
            }

            SetBinding(slot, binding);
        }
    }

    public static void ResetToDefaults()
    {
        for (var slot = 0; slot < SlotCount; slot++)
        {
            SetBinding(slot, new AbilityBinding(DefaultKeys[slot]));
        }

        SaveCurrentBindings();
    }

    public static bool TrySetKeyForSlot(int slot, InputEventKey keyEvent, out string error)
    {
        error = string.Empty;
        if (slot is < 0 or >= SlotCount)
        {
            error = "Unknown ability slot.";
            return false;
        }

        if (keyEvent.CtrlPressed)
        {
            error = "Ctrl cannot be used for ability bindings.";
            return false;
        }

        var key = GetEventKey(keyEvent);
        if (!IsAllowedKey(key))
        {
            error = "Choose a key other than Ctrl, W, A, S, or D.";
            return false;
        }

        var binding = new AbilityBinding(key);
        var usedSlot = GetSlotUsingBinding(binding);
        if (usedSlot >= 0 && usedSlot != slot)
        {
            error = $"That key is already used by Ability {usedSlot + 1}.";
            return false;
        }

        SetBinding(slot, binding);
        SaveCurrentBindings();
        return true;
    }

    public static bool TrySetMouseButtonForSlot(int slot, InputEventMouseButton mouseEvent, out string error)
    {
        error = string.Empty;
        if (slot is < 0 or >= SlotCount)
        {
            error = "Unknown ability slot.";
            return false;
        }

        if (mouseEvent.CtrlPressed)
        {
            error = "Ctrl cannot be used for ability bindings.";
            return false;
        }

        var binding = new AbilityBinding(mouseEvent.ButtonIndex);
        if (!IsAllowedBinding(binding))
        {
            error = "Choose a mouse click button.";
            return false;
        }

        var usedSlot = GetSlotUsingBinding(binding);
        if (usedSlot >= 0 && usedSlot != slot)
        {
            error = $"That mouse button is already used by Ability {usedSlot + 1}.";
            return false;
        }

        SetBinding(slot, binding);
        SaveCurrentBindings();
        return true;
    }

    public static Key GetKeyForSlot(int slot)
    {
        if (slot is < 0 or >= SlotCount)
        {
            return (Key)0;
        }

        return GetFirstKeyboardKey(AbilityActions[slot]) ?? DefaultKeys[slot];
    }

    public static string GetBindingDisplayName(int slot)
    {
        if (slot is < 0 or >= SlotCount)
        {
            return string.Empty;
        }

        var binding = GetBindingForSlot(slot);
        if (binding.IsMouseButton)
        {
            return GetMouseButtonDisplayName(binding.MouseButton);
        }

        var key = binding.Key;
        var displayName = OS.GetKeycodeString(key);
        return string.IsNullOrWhiteSpace(displayName) ? ((long)key).ToString() : displayName;
    }

    private static int GetSlotUsingBinding(AbilityBinding binding)
    {
        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (GetBindings(AbilityActions[slot]).Any(existingBinding => AreSameBinding(existingBinding, binding)))
            {
                return slot;
            }
        }

        return -1;
    }

    private static bool IsAllowedKey(Key key)
    {
        if (key == (Key)0 || BlockedKeys.Contains(key))
        {
            return false;
        }

        var displayName = OS.GetKeycodeString(key);
        return !displayName.Contains("Ctrl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedBinding(AbilityBinding binding)
    {
        return binding.IsMouseButton ? IsAllowedMouseButton(binding.MouseButton) : IsAllowedKey(binding.Key);
    }

    private static bool IsAllowedMouseButton(MouseButton mouseButton)
    {
        var button = (long)mouseButton;
        return button is 1 or 2 or 3 or 8 or 9;
    }

    private static bool AreSameBinding(AbilityBinding first, AbilityBinding second)
    {
        if (first.IsMouseButton != second.IsMouseButton)
        {
            return false;
        }

        return first.IsMouseButton ? first.MouseButton == second.MouseButton : first.Key == second.Key;
    }

    private static void SetBinding(int slot, AbilityBinding binding)
    {
        var action = AbilityActions[slot];
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        InputMap.ActionEraseEvents(action);
        if (binding.IsMouseButton)
        {
            InputMap.ActionAddEvent(action, new InputEventMouseButton
            {
                ButtonIndex = binding.MouseButton
            });
            return;
        }

        InputMap.ActionAddEvent(action, new InputEventKey
        {
            PhysicalKeycode = binding.Key
        });
    }

    private static void SaveCurrentBindings()
    {
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            return;
        }

        for (var slot = 0; slot < SlotCount; slot++)
        {
            file.StoreLine(SerializeBinding(GetBindingForSlot(slot)));
        }
    }

    private static AbilityBinding GetBindingForSlot(int slot)
    {
        foreach (var binding in GetBindings(AbilityActions[slot]))
        {
            if (IsAllowedBinding(binding))
            {
                return binding;
            }
        }

        return new AbilityBinding(DefaultKeys[slot]);
    }

    private static Key? GetFirstKeyboardKey(string action)
    {
        foreach (var binding in GetBindings(action))
        {
            if (!binding.IsMouseButton && binding.Key != (Key)0)
            {
                return binding.Key;
            }
        }

        return null;
    }

    private static IEnumerable<AbilityBinding> GetBindings(string action)
    {
        if (!InputMap.HasAction(action))
        {
            yield break;
        }

        foreach (var inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventKey keyEvent)
            {
                var key = GetEventKey(keyEvent);
                if (key != (Key)0)
                {
                    yield return new AbilityBinding(key);
                }
            }
            else if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex != (MouseButton)0)
            {
                yield return new AbilityBinding(mouseEvent.ButtonIndex);
            }
        }
    }

    private static Key GetEventKey(InputEventKey keyEvent)
    {
        return keyEvent.PhysicalKeycode != (Key)0 ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
    }

    private static bool TryParseBinding(string value, out AbilityBinding binding)
    {
        binding = new AbilityBinding((Key)0);
        if (value.StartsWith("key:", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(value["key:".Length..], out var keyValue))
        {
            binding = new AbilityBinding((Key)keyValue);
            return true;
        }

        if (value.StartsWith("mouse:", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(value["mouse:".Length..], out var mouseValue))
        {
            binding = new AbilityBinding((MouseButton)mouseValue);
            return true;
        }

        if (long.TryParse(value, out var legacyKeyValue))
        {
            binding = new AbilityBinding((Key)legacyKeyValue);
            return true;
        }

        return false;
    }

    private static string SerializeBinding(AbilityBinding binding)
    {
        return binding.IsMouseButton
            ? $"mouse:{(long)binding.MouseButton}"
            : $"key:{(long)binding.Key}";
    }

    private static string GetMouseButtonDisplayName(MouseButton mouseButton)
    {
        return (long)mouseButton switch
        {
            1 => "Mouse Left",
            2 => "Mouse Right",
            3 => "Mouse Middle",
            8 => "Mouse Button 4",
            9 => "Mouse Button 5",
            _ => $"Mouse {(long)mouseButton}"
        };
    }
}
