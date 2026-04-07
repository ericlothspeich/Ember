using System;
using Hexa.NET.ImGui;
using IfritParticles;
using static Hexa.NET.ImGui.ImGui;

namespace Ember.Architecture.Components;

public static class CurveEditor
{
    public static bool Draw(string id, ParticleCurve curve, float width, float height)
    {
        bool modified = false;

        PushID(id);

        SysVec2 canvasPos = GetCursorScreenPos();
        InvisibleButton("##canvas", new SysVec2(width, height));
        bool isActive = IsItemActive();

        ImDrawListPtr drawList = GetWindowDrawList();

        // Background
        uint bgColor = ImGui.ColorConvertFloat4ToU32(new SysVec4(0.15f, 0.15f, 0.15f, 1f));
        drawList.AddRectFilled(canvasPos, new SysVec2(canvasPos.X + width, canvasPos.Y + height), bgColor);

        // Grid lines at 25% intervals
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new SysVec4(0.25f, 0.25f, 0.25f, 1f));
        for (int i = 1; i < 4; i++)
        {
            float frac = i * 0.25f;
            float gx = canvasPos.X + frac * width;
            float gy = canvasPos.Y + frac * height;
            drawList.AddLine(new SysVec2(gx, canvasPos.Y), new SysVec2(gx, canvasPos.Y + height), gridColor);
            drawList.AddLine(new SysVec2(canvasPos.X, gy), new SysVec2(canvasPos.X + width, gy), gridColor);
        }

        // Curve polyline
        uint curveColor = ImGui.ColorConvertFloat4ToU32(new SysVec4(1f, 1f, 1f, 1f));
        for (int i = 0; i < 255; i++)
        {
            float x0 = canvasPos.X + (i / 255f) * width;
            float y0 = canvasPos.Y + (1f - curve.Values[i]) * height;
            float x1 = canvasPos.X + ((i + 1) / 255f) * width;
            float y1 = canvasPos.Y + (1f - curve.Values[i + 1]) * height;
            drawList.AddLine(new SysVec2(x0, y0), new SysVec2(x1, y1), curveColor);
        }

        // Start and end dots
        uint dotColor = ImGui.ColorConvertFloat4ToU32(new SysVec4(1f, 1f, 1f, 1f));
        float startY = canvasPos.Y + (1f - curve.Values[0]) * height;
        float endY = canvasPos.Y + (1f - curve.Values[255]) * height;
        drawList.AddCircleFilled(new SysVec2(canvasPos.X, startY), 5f, dotColor);
        drawList.AddCircleFilled(new SysVec2(canvasPos.X + width, endY), 5f, dotColor);

        // Border
        uint borderColor = ImGui.ColorConvertFloat4ToU32(new SysVec4(0.4f, 0.4f, 0.4f, 1f));
        drawList.AddRect(canvasPos, new SysVec2(canvasPos.X + width, canvasPos.Y + height), borderColor);

        // Freehand drawing
        if (isActive)
        {
            SysVec2 mousePos = GetIO().MousePos;
            SysVec2 mouseDelta = GetIO().MouseDelta;

            float currRelX = (mousePos.X - canvasPos.X) / width;
            float currRelY = 1f - (mousePos.Y - canvasPos.Y) / height;

            float prevRelX = ((mousePos.X - mouseDelta.X) - canvasPos.X) / width;
            float prevRelY = 1f - ((mousePos.Y - mouseDelta.Y) - canvasPos.Y) / height;

            int currIdx = Math.Clamp((int)(currRelX * 255), 0, 255);
            int prevIdx = Math.Clamp((int)(prevRelX * 255), 0, 255);

            float currVal = Math.Clamp(currRelY, 0f, 1f);
            float prevVal = Math.Clamp(prevRelY, 0f, 1f);

            int minIdx = Math.Min(prevIdx, currIdx);
            int maxIdx = Math.Max(prevIdx, currIdx);

            if (minIdx == maxIdx)
            {
                curve.Values[currIdx] = currVal;
            }
            else
            {
                for (int i = minIdx; i <= maxIdx; i++)
                {
                    float t = (float)(i - prevIdx) / (currIdx - prevIdx);
                    curve.Values[i] = Math.Clamp(prevVal + t * (currVal - prevVal), 0f, 1f);
                }
            }

            modified = true;
        }

        PopID();
        return modified;
    }

    public static bool DrawPresets(string id, ParticleCurve curve)
    {
        bool applied = false;

        PushID(id);

        ImGuiStylePtr style = GetStyle();
        float availWidth = GetContentRegionAvail().X;
        float buttonWidth = (availWidth - style.ItemSpacing.X * 5f) / 6f;
        SysVec2 buttonSize = new SysVec2(buttonWidth, 0);

        if (Button("Linear"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.Linear().Values, curve.Values, 256);
            applied = true;
        }

        SameLine();
        if (Button("Ease In"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.EaseIn().Values, curve.Values, 256);
            applied = true;
        }

        SameLine();
        if (Button("Ease Out"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.EaseOut().Values, curve.Values, 256);
            applied = true;
        }

        SameLine();
        if (Button("In/Out"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.EaseInOut().Values, curve.Values, 256);
            applied = true;
        }

        SameLine();
        if (Button("Bell"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.Bell().Values, curve.Values, 256);
            applied = true;
        }

        SameLine();
        if (Button("Flat"u8, buttonSize))
        {
            Array.Copy(ParticleCurve.Constant(1f).Values, curve.Values, 256);
            applied = true;
        }

        // Second row: Flip and Smooth
        float buttonWidth2 = (availWidth - style.ItemSpacing.X) / 2f;
        SysVec2 buttonSize2 = new SysVec2(buttonWidth2, 0);

        if (Button("Flip"u8, buttonSize2))
        {
            // Invert values vertically (1 becomes 0, 0 becomes 1)
            for (int i = 0; i < 256; i++)
                curve.Values[i] = 1f - curve.Values[i];
            applied = true;
        }

        SameLine();
        if (Button("Smooth"u8, buttonSize2))
        {
            // Average each value with its neighbors (3-tap box filter)
            float[] temp = new float[256];
            temp[0] = curve.Values[0];
            temp[255] = curve.Values[255];
            for (int i = 1; i < 255; i++)
                temp[i] = (curve.Values[i - 1] + curve.Values[i] + curve.Values[i + 1]) / 3f;
            Array.Copy(temp, curve.Values, 256);
            applied = true;
        }

        PopID();
        return applied;
    }

    /// <summary>
    /// If the curve starts higher than it ends but startValue &lt; endValue (or vice versa),
    /// swaps the start and end values to match the curve direction, then normalizes
    /// the curve to always ramp from low to high internally.
    /// Returns true if values were swapped.
    /// </summary>
    public static bool NormalizeDirection(ParticleCurve curve, ref float startValue, ref float endValue)
    {
        bool curveDescends = curve.Values[0] > curve.Values[255];
        bool valuesAscend = startValue < endValue;

        if (curveDescends == valuesAscend && startValue != endValue)
        {
            // Swap start and end values
            float temp = startValue;
            startValue = endValue;
            endValue = temp;
            return true;
        }
        return false;
    }
}
