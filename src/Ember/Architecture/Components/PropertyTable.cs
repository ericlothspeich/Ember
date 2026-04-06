using System;
using System.IO;
using System.Text;
using Ember.Graphics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IfritParticles;
using static Hexa.NET.ImGui.ImGui;

namespace Ember.Architecture.Components;

public static class PropertyTable
{
    public static bool BeginPropertyTable(ReadOnlySpan<byte> id)
    {
        if (BeginTable(id, columns: 2, ImGuiTableFlags.SizingFixedSame))
        {
            TableSetupColumn("##property-label"u8, ImGuiTableColumnFlags.WidthFixed, -1, 0);
            TableSetupColumn("##property-value"u8, ImGuiTableColumnFlags.WidthStretch, -1, 1);
            return true;
        }

        return false;
    }

    public static void EndPropertyTable() => EndTable();

    private static void Label(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip)
    {
        TableNextColumn();
        AlignTextToFramePadding();
        Text(label);
        if (IsItemHovered(ImGuiHoveredFlags.DelayNormal))
        {
            SetTooltip(tooltip);
        }
    }

    public static bool InputTextProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref string value)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        if (InputText("##value"u8, ref value, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.EscapeClearsAll))
        {
            changed = true;
        }

        PopID();
        return changed;
    }

    public static bool CheckboxProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref bool value)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        if (Checkbox("##value", ref value))
        {
            changed = true;
        }
        PopID();
        return changed;
    }

    public static bool DragIntProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref int value, int step, int min, int max)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        if (DragInt("##value"u8, ref value, step, min, max))
        {
            changed = true;
        }

        PopID();
        return changed;
    }

    public static bool InputIntProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref int value)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        if (InputInt("##value"u8, ref value, 0, 0))
        {
            changed = true;
        }

        PopID();
        return changed;
    }

    public static bool InputRectProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref XnaRect value)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        int[] components = [value.X, value.Y, value.Width, value.Height];
        if (InputInt4("##value"u8, ref components[0]))
        {
            value.X = components[0];
            value.Y = components[1];
            value.Width = components[2];
            value.Height = components[3];
            changed = true;
        }

        PopID();
        return changed;

    }

    public static bool DragFloatProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref float value, float step, float min, float max)
    {
        bool changed = false;

        TableNextRow();
        PushID(label);
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        if (DragFloat("##value"u8, ref value, step, min, max, "%.2f"u8))
        {
            changed = true;
        }

        PopID();
        return changed;
    }

    public static bool DragVector2Property(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref XnaVec2 value, float step, float min, float max)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();

        ImGuiStylePtr stylePtr = GetStyle();
        float availWidth = GetContentRegionAvail().X;
        float itemSpacingWidth = stylePtr.ItemSpacing.X;
        float dragWidth = (availWidth - itemSpacingWidth) * 0.5f;

        SetNextItemWidth(dragWidth);
        if (DragFloat("##value-x"u8, ref value.X, step, min, max, "X: %.2F"))
        {
            changed = true;
        }

        SameLine();
        SetNextItemWidth(dragWidth);
        if (DragFloat("##value-y"u8, ref value.Y, step, min, max, "Y: %.2F"))
        {
            changed = true;
        }

        PopID();
        return changed;
    }

    /// <summary>
    /// Color property that works with HSL stored as Vector3 (H 0-360, S 0-1, L 0-1).
    /// Uses Ifrit's static HslColor class for conversions.
    /// </summary>
    public static bool Color3VectorProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ref XnaVec3 value)
    {
        bool changed = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();

        XnaColor rgbColor = HslColor.ToRgb(value);
        SysVec4 color = new(rgbColor.R / 255.0f, rgbColor.G / 255.0f, rgbColor.B / 255.0f, 1.0f);

        float availWidth = GetContentRegionAvail().X;
        SysVec2 buttonSize = new(availWidth, GetFrameHeight());

        if (ColorButton("##color-button"u8, color, ImGuiColorEditFlags.None, buttonSize))
        {
            OpenPopup("##color-picker");
        }

        if (BeginPopup("##color-picker"))
        {
            SysVec3 rgb = new SysVec3(color.X, color.Y, color.Z);
            if (ColorPicker3("##value", ref rgb))
            {
                XnaColor newRgb = new XnaColor(rgb.X, rgb.Y, rgb.Z);
                Vector3 newHsl = HslColor.FromRgb(newRgb);
                value = newHsl;
                changed = true;
            }

            EndPopup();
        }

        PopID();
        return changed;
    }

    /// <summary>
    /// Button property that displays a label/tooltip row with a button in the value column.
    /// </summary>
    public static bool ButtonProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, string buttonText)
    {
        bool clicked = false;

        PushID(label);

        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();

        if (Button(Encoding.UTF8.GetBytes(buttonText ?? ""), -SysVec2.UnitX))
        {
            clicked = true;
        }

        PopID();
        return clicked;
    }

    public static bool ButtonProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip)
    {
        bool clicked = false;

        PushID(label);

        TableNextRow();
        TableNextColumn();
        TableNextColumn();

        if (Button(label, -SysVec2.UnitX))
        {
            clicked = true;
        }
        if (IsItemHovered(ImGuiHoveredFlags.DelayNormal))
        {
            SetTooltip(tooltip);
        }

        PopID();
        return clicked;

    }

    public static bool BeginComboProperty(ReadOnlySpan<byte> label, ReadOnlySpan<byte> tooltip, ReadOnlySpan<byte> preview)
    {
        PushID(label);
        TableNextRow();
        Label(label, tooltip);
        TableNextColumn();
        SetNextItemWidth(-1);
        bool opened = BeginCombo("##value"u8, preview);
        if (!opened)
        {
            PopID();
        }
        return opened;
    }

    public static bool ComboItem(ReadOnlySpan<byte> itemLabel, ReadOnlySpan<byte> itemToolTip, bool isSelected)
    {
        bool clicked = Selectable(itemLabel, isSelected);

        if (IsItemHovered(ImGuiHoveredFlags.DelayNormal))
        {
            SetTooltip(itemToolTip);
        }

        if (isSelected)
        {
            SetItemDefaultFocus();
        }

        return clicked;
    }

    public static void EndComboProperty()
    {
        EndCombo();
        PopID();
    }
}
