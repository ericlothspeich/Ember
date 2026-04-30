// Copyright (c) Christopher Whitley and Contributors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Hexa.NET.ImGui;

namespace Ember.Architecture;

public static partial class Fonts
{
    private static readonly uint[] s_excludeRange = [0xE000, 0xF8FF, 0];
    private static readonly byte[] s_mainFontBytes = LoadEmbeddedTtf("JetBrainsMono-Regular.ttf");
    private static readonly byte[] s_iconFontBytes = LoadEmbeddedTtf("fa-solid-900.ttf");
    private static ImFontPtr s_mainFontPtr;

    public static unsafe void Load()
    {
        ImGuiIOPtr ioPtr = ImGui.GetIO();

        ioPtr.Fonts.Clear();

        fixed (uint* iconExcludeRangePtr = s_excludeRange)
        fixed (byte* mainFontDataPtr = s_mainFontBytes)
        fixed (byte* iconFontDataPtr = s_iconFontBytes)
        {
            // Load the base font, excluding the icon range
            ImFontConfigPtr baseFontConfigPtr = ImGui.ImFontConfig();
            baseFontConfigPtr.GlyphExcludeRanges = iconExcludeRangePtr;
            baseFontConfigPtr.FontDataOwnedByAtlas = false;
            s_mainFontPtr = ioPtr.Fonts.AddFontFromMemoryTTF(mainFontDataPtr, s_mainFontBytes.Length, 0.0f, baseFontConfigPtr);

            // Load the font with icons, which will use the excluded range
            // on merge. I dunno, ImGui magic
            ImFontConfigPtr iconFontConfigPtr = ImGui.ImFontConfig();
            iconFontConfigPtr.MergeMode = true;
            iconFontConfigPtr.FontDataOwnedByAtlas = false;
            ioPtr.Fonts.AddFontFromMemoryTTF(iconFontDataPtr, s_iconFontBytes.Length, 0.0f, iconFontConfigPtr);
        }

    }

    // Pinned-object-heap allocation keeps the pointer stable for the atlas's lifetime.
    private static byte[] LoadEmbeddedTtf(string name)
    {
        using Stream stream = typeof(Fonts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded font resource '{name}' not found.");
        byte[] buffer = GC.AllocateUninitializedArray<byte>((int)stream.Length, pinned: true);
        stream.ReadExactly(buffer);
        return buffer;
    }
}
