using System;
using System.IO;
using Ember.Architecture.Components;
using Ember.Architecture.PopupModals;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IfritParticles;
using IfritParticles.Profiles;
using static Hexa.NET.ImGui.ImGui;

namespace Ember.Architecture.Views;

public sealed class ParticlePoolView
{
    public const string ViewName = "Particle Effect";

    private readonly EditorContext _context;

    private int _emitterDragFromIndex = -1;
    private int _emitterDragToIndex = -1;
    private bool _selectTexture;


    public ParticlePoolView(EditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public void Draw()
    {
        if (_context.ParticlePool == null)
        {
            return;
        }
        if (Begin(ViewName))
        {
            DrawParticlePoolProperties();
            DrawParticleEmitterList();
            DrawSelectedEmitterProperties();
            DrawSelectedEmitterProfile();
            DrawSelectedEmitterReleaseParameters();
        }
        End();

        DrawSelectTexturePopup();
    }

    private void DrawParticlePoolProperties()
    {
        if (CollapsingHeader("Particle Effect Properties"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGuiChildFlags childFlags = ImGuiChildFlags.Borders
                                         | ImGuiChildFlags.AutoResizeY;

            if (BeginChild("##particle-effect-properties"u8, SysVec2.Zero, childFlags))
            {
                if (BeginTable("##particle-effect-properties-table"u8, columns: 2, ImGuiTableFlags.SizingStretchProp))
                {
                    TableSetupColumn("##particle-effect-property-label-column"u8, ImGuiTableColumnFlags.WidthStretch, 1.0f);
                    TableSetupColumn("##particle-effect-property-value-column"u8, ImGuiTableColumnFlags.WidthStretch, 1.0f);

                    // Auto trigger property
                    TableNextRow();
                    TableNextColumn();
                    AlignTextToFramePadding();
                    Text("Auto Trigger"u8);

                    if (IsItemHovered(ImGuiHoveredFlags.DelayNormal))
                    {
                        SetTooltip("Indicates whether this particle effect will automatically trigger its emitters"u8);
                    }

                    TableNextColumn();
                    bool autoTrigger = _context.ParticlePool.AutoTrigger;
                    if (Checkbox("##particle-effect-auto-trigger"u8, ref autoTrigger))
                    {
                        _context.ParticlePool.AutoTrigger = autoTrigger;
                        // Propagate to all emitters
                        foreach (var emitter in _context.ParticlePool.Emitters)
                        {
                            emitter.AutoTrigger = autoTrigger;
                        }
                        _context.HasUnsavedChanges = true;
                    }

                    // Auto Trigger Frequency
                    TableNextRow();
                    TableNextColumn();
                    AlignTextToFramePadding();
                    Text("Auto Trigger Frequency"u8);

                    if (IsItemHovered(ImGuiHoveredFlags.DelayNormal))
                    {
                        SetTooltip("The frequency, in seconds, at which this particle effect automatically triggers emitters"u8);
                    }

                    TableNextColumn();
                    BeginDisabled(!_context.ParticlePool.AutoTrigger);
                    SetNextItemWidth(-1);
                    float frequency = _context.ParticlePool.AutoTriggerFrequency;
                    if (DragFloat("##particle-effect-auto-trigger-frequency"u8, ref frequency, 0.1f, 0.1f, float.MaxValue, "%.2f"u8))
                    {
                        _context.ParticlePool.AutoTriggerFrequency = frequency;
                        // Propagate to all emitters
                        foreach (var emitter in _context.ParticlePool.Emitters)
                        {
                            emitter.AutoTriggerFrequency = frequency;
                        }
                        _context.HasUnsavedChanges = true;
                    }
                    EndDisabled();
                    EndTable();
                }
            }
            EndChild();
        }
    }

    private unsafe void DrawParticleEmitterList()
    {
        if (CollapsingHeader("Particle Emitters"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGuiChildFlags childFlags = ImGuiChildFlags.Borders
                                         | ImGuiChildFlags.AutoResizeY;

            SysVec2 childWindowSize = new SysVec2(0.0f, 300.0f);

            if (Button("Add New Emitter"u8, new SysVec2(-1, 0)))
            {
                _context.AddEmitter();
            }

            // If there are no emitters, just display a child window with the
            // the text stating so and return back
            if (_context.ParticlePool.Emitters.Count == 0)
            {
                if (BeginChild("##particle-emitter-list-child-window"u8, childWindowSize, childFlags))
                {
                    TextDisabled("No particle emitters added"u8);
                }
                EndChild();
                return;
            }

            if (BeginChild("##particle-emitter-list-child-window"u8, childWindowSize, childFlags))
            {
                float iconColumnWidth = 20.0f;
                ImGuiTableFlags tableFlags = ImGuiTableFlags.ScrollY
                                             | ImGuiTableFlags.RowBg
                                             | ImGuiTableFlags.SizingStretchProp;

                if (BeginTable("##particle-emitter-list"u8, columns: 4, tableFlags))
                {
                    TableSetupColumn("##particle-emitter-list-name-column"u8, ImGuiTableColumnFlags.WidthStretch, 1.0f);
                    TableSetupColumn("##particle-emitter-list-lock-column"u8, ImGuiTableColumnFlags.WidthFixed, iconColumnWidth);
                    TableSetupColumn("##particle-emitter-list-visibility-column"u8, ImGuiTableColumnFlags.WidthFixed, iconColumnWidth);
                    TableSetupColumn("##particle-emitter-list-delete-column"u8, ImGuiTableColumnFlags.WidthFixed, iconColumnWidth);

                    for (int i = 0; i < _context.ParticlePool.Emitters.Count; i++)
                    {
                        TableNextRow();
                        PushID(i);

                        ParticleEmitter emitter = _context.ParticlePool.Emitters[i];
                        bool isLocked = _context.IsLocked(emitter);
                        bool isSelected = emitter == _context.SelectedEmitter;
                        uint buttonColor = isSelected ? GetColorU32(ImGuiCol.Button) : GetColorU32(SysVec4.Zero);
                        uint buttonHoverColor = GetColorU32(ImGuiCol.ButtonHovered);

                        // Name Column
                        TableNextColumn();
                        SysVec2 nameButtonSize = new SysVec2(-1, GetFrameHeight());
                        PushStyleColor(ImGuiCol.Button, buttonColor);
                        PushStyleColor(ImGuiCol.ButtonHovered, buttonHoverColor);
                        PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new SysVec2(0.0f, 0.5f));
                        if (Button(emitter.Name, nameButtonSize))
                        {
                            _context.SelectEmitter(i);
                        }

                        if (BeginDragDropSource(ImGuiDragDropFlags.None))
                        {
                            int* indexPtr = &i;
                            SetDragDropPayload("emitter-reorder-payload"u8, &i, sizeof(int));
                            Text($"Moving: {emitter.Name}");
                            EndDragDropSource();
                        }

                        if (BeginDragDropTarget())
                        {
                            ImGuiPayloadPtr payloadPtr = AcceptDragDropPayload("emitter-reorder-payload"u8);
                            if (!payloadPtr.IsNull)
                            {
                                _emitterDragFromIndex = *(int*)payloadPtr.Data;
                                _emitterDragToIndex = i;
                            }

                            EndDragDropTarget();
                        }

                        PopStyleColor(2);
                        PopStyleVar();

                        // Lock column
                        TableNextColumn();
                        Text(isLocked ? Fonts.LockIcon : Fonts.UnlockedIcon);
                        if (IsItemHovered() && IsItemClicked(ImGuiMouseButton.Left))
                        {
                            _context.ToggleLock(emitter);
                        }

                        // Visibility Column
                        TableNextColumn();
                        BeginDisabled(isLocked);
                        Text(emitter.Visible ? Fonts.VisibleIcon : Fonts.NotVisibleIcon);
                        if (IsItemHovered() && IsItemClicked(ImGuiMouseButton.Left))
                        {
                            emitter.Visible = !emitter.Visible;
                            _context.HasUnsavedChanges = true;
                        }
                        EndDisabled();

                        // Delete column
                        TableNextColumn();
                        BeginDisabled(isLocked);
                        Text(Fonts.DeleteIcon);
                        if (IsItemHovered() && IsItemClicked(ImGuiMouseButton.Left))
                        {
                            _context.RemoveEmitter(i);
                        }
                        EndDisabled();

                        // Reorder emitters if a drag/drop occurred
                        if (_emitterDragFromIndex != -1 && _emitterDragToIndex != -1 && _emitterDragFromIndex != _emitterDragToIndex)
                        {
                            _context.ReorderEmitters(_emitterDragFromIndex, _emitterDragToIndex);
                            _emitterDragFromIndex = -1;
                            _emitterDragToIndex = -1;
                        }

                        PopID();
                    }
                    EndTable();
                }
            }
            EndChild();
        }
    }

    private void DrawSelectedEmitterProperties()
    {
        if (CollapsingHeader("Emitter Properties"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (_context.SelectedEmitter is not ParticleEmitter emitter)
            {
                return;
            }

            ImGuiChildFlags childFlags = ImGuiChildFlags.Borders
                                         | ImGuiChildFlags.AutoResizeY;

            if (BeginChild("##selected-emitter-properties-child-window"u8, SysVec2.Zero, childFlags))
            {
                BeginDisabled(_context.IsLocked(emitter));
                if (PropertyTable.BeginPropertyTable("##selected-emitter-properties-table"u8))
                {
                    // Name property
                    string emitterName = emitter.Name;
                    if (PropertyTable.InputTextProperty("Name"u8, "The display name of the selected emitter"u8, ref emitterName))
                    {
                        emitter.Name = emitterName;
                        _context.HasUnsavedChanges = true;
                    }

                    // Texture Property
                    string textureName = _context.ParticleTexture != null
                        ? Path.GetFileName(_context.ParticleTexture.Name ?? "Texture")
                        : null;
                    if (PropertyTable.ButtonProperty(
                        "Texture"u8,
                        "The texture used by the selected emitter"u8,
                        textureName ?? "Select Texture"))
                    {
                        _selectTexture = true;
                    }

                    if (_context.ParticleTexture != null)
                    {
                        // Source Rectangle (derived from UV)
                        Texture2D tex = _context.ParticleTexture;
                        int boundsX = (int)(emitter.UVOffset.X * tex.Width);
                        int boundsY = (int)(emitter.UVOffset.Y * tex.Height);
                        int boundsW = (int)(emitter.UVScale.X * tex.Width);
                        int boundsH = (int)(emitter.UVScale.Y * tex.Height);
                        XnaRect sourceRectangle = new XnaRect(boundsX, boundsY, boundsW, boundsH);
                        if (PropertyTable.InputRectProperty("Source Rectangle"u8, "The rectangular bounds within the texture to render"u8, ref sourceRectangle))
                        {
                            emitter.UVOffset = new Vector2((float)sourceRectangle.X / tex.Width, (float)sourceRectangle.Y / tex.Height);
                            emitter.UVScale = new Vector2((float)sourceRectangle.Width / tex.Width, (float)sourceRectangle.Height / tex.Height);
                            emitter.TextureSize = new Vector2(sourceRectangle.Width, sourceRectangle.Height);
                            _context.HasUnsavedChanges = true;
                        }

                        // Reset source rectangle
                        if (PropertyTable.ButtonProperty("Reset Source Rectangle"u8, "Resets the source rectangle back to the bounds of the texture"u8))
                        {
                            emitter.UVOffset = Vector2.Zero;
                            emitter.UVScale = Vector2.One;
                            emitter.TextureSize = new Vector2(tex.Width, tex.Height);
                            _context.HasUnsavedChanges = true;
                        }
                    }

                    int emitterCapacity = emitter.Capacity;
                    if (PropertyTable.InputIntProperty("Capacity"u8, "The maximum number of particles that this emitter can have active at a given time"u8, ref emitterCapacity))
                    {
                        emitter.Capacity = emitterCapacity;
                        _context.HasUnsavedChanges = true;
                    }

                    // Lifetime Property
                    float emitterLifetime = emitter.Lifetime;
                    if (PropertyTable.DragFloatProperty("Lifetime"u8, "The amount of time, in seconds, that each particle released from this emitter will live"u8, ref emitterLifetime, 0.1f, 0.0f, float.MaxValue))
                    {
                        emitter.Lifetime = emitterLifetime;
                        _context.HasUnsavedChanges = true;
                    }

                    // Prewarm
                    float prewarm = emitter.PrewarmSeconds;
                    if (PropertyTable.DragFloatProperty("Prewarm"u8, "Seconds to pre-simulate on start. Effect appears already running."u8, ref prewarm, 0.1f, 0f, 30f))
                    {
                        emitter.PrewarmSeconds = prewarm;
                        _context.HasUnsavedChanges = true;
                    }

                    // Offset Property
                    XnaVec2 emitterOffset = emitter.Offset;
                    if (PropertyTable.DragVector2Property("Offset"u8, "The position offset applied to this emitter from the effect position"u8, ref emitterOffset, 1.0f, float.MinValue, float.MaxValue))
                    {
                        emitter.Offset = emitterOffset;
                        _context.HasUnsavedChanges = true;
                    }

                    // Blend Mode
                    ReadOnlySpan<byte> blendPreview = emitter.BlendMode == ParticleBlendMode.Additive ? "Additive"u8 : "Alpha Blend"u8;
                    if (PropertyTable.BeginComboProperty("Blend Mode"u8, "The blending mode used when rendering particles"u8, blendPreview))
                    {
                        if (PropertyTable.ComboItem("Alpha Blend"u8, "Standard transparency blending"u8, emitter.BlendMode == ParticleBlendMode.AlphaBlend))
                        {
                            emitter.BlendMode = ParticleBlendMode.AlphaBlend;
                            _context.HasUnsavedChanges = true;
                        }
                        if (PropertyTable.ComboItem("Additive"u8, "Colors add together, creates glow and fire effects"u8, emitter.BlendMode == ParticleBlendMode.Additive))
                        {
                            emitter.BlendMode = ParticleBlendMode.Additive;
                            _context.HasUnsavedChanges = true;
                        }
                        PropertyTable.EndComboProperty();
                    }

                    PropertyTable.EndPropertyTable();
                }
                EndDisabled();
            }
            EndChild();
        }
    }

    private void DrawSelectedEmitterProfile()
    {
        if (CollapsingHeader("Emitter Profile"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGuiChildFlags childFlags = ImGuiChildFlags.Borders
                                         | ImGuiChildFlags.AutoResizeY;

            if (_context.SelectedEmitter is not ParticleEmitter emitter)
            {
                return;
            }

            if (BeginChild("##selected-emitter-profile-child-window"u8, SysVec2.Zero, childFlags))
            {
                BeginDisabled(_context.IsLocked(emitter));

                if (PropertyTable.BeginPropertyTable("##selected-emitter-profile-properties-table"u8))
                {
                    ReadOnlySpan<byte> profileTypePreview = emitter.Profile switch
                    {
                        BoxFillProfile => "Box Fill"u8,
                        BoxUniformProfile => "Box Uniform"u8,
                        BoxProfile => "Box"u8,
                        CircleProfile => "Circle"u8,
                        LineProfile => "Line"u8,
                        PointProfile => "Point"u8,
                        RingProfile => "Ring"u8,
                        SprayProfile => "Spray"u8,
                        _ => "Unknown"u8
                    };

                    if (PropertyTable.BeginComboProperty("Profile Type"u8, "Profiles define the emission pattern, such as points, lines, rings, or areas, from which particles originate"u8, profileTypePreview))
                    {
                        if (PropertyTable.ComboItem("Box Fill"u8, "Randomly distributes particles throughout a rectangular area"u8, emitter.Profile is BoxFillProfile))
                        {
                            emitter.Profile = new BoxFillProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Box"u8, "Distributes particles along the edges of a rectangular boundary"u8, emitter.Profile is BoxProfile && emitter.Profile is not BoxFillProfile))
                        {
                            emitter.Profile = new BoxProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Box Uniform"u8, "Distributes particles along the edges of a rectangular boundary with uniform density"u8, emitter.Profile is BoxUniformProfile))
                        {
                            emitter.Profile = new BoxUniformProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Circle"u8, "Distributes particles throughout a circular area with controllable radiation patterns"u8, emitter.Profile is CircleProfile))
                        {
                            emitter.Profile = new CircleProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Line"u8, "Distributes particles uniformly along a line segment with random headings"u8, emitter.Profile is LineProfile))
                        {
                            emitter.Profile = new LineProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Point"u8, "Emits all particles from a single point with random headings"u8, emitter.Profile is PointProfile))
                        {
                            emitter.Profile = new PointProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Ring"u8, "Distributes particles along the perimeter of a circle with controllable radiation patterns"u8, emitter.Profile is RingProfile))
                        {
                            emitter.Profile = new RingProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        if (PropertyTable.ComboItem("Spray"u8, "Emits particles from a single point in a directional cone pattern"u8, emitter.Profile is SprayProfile))
                        {
                            emitter.Profile = new SprayProfile();
                            _context.HasUnsavedChanges = true;
                        }

                        PropertyTable.EndComboProperty();
                    }

                    // Profile Properties Row(s)
                    switch (emitter.Profile)
                    {
                        case BoxFillProfile boxFill:
                            float boxFillWidth = boxFill.Width;
                            if (PropertyTable.DragFloatProperty("Width"u8, "The width of the rectangular area"u8, ref boxFillWidth, 0.1f, 0.0f, float.MaxValue))
                            {
                                boxFill.Width = boxFillWidth;
                                _context.HasUnsavedChanges = true;
                            }

                            float boxFillHeight = boxFill.Height;
                            if (PropertyTable.DragFloatProperty("Height"u8, "The height of the rectangular area"u8, ref boxFillHeight, 0.1f, 0.0f, float.MaxValue))
                            {
                                boxFill.Height = boxFillHeight;
                                _context.HasUnsavedChanges = true;
                            }
                            break;

                        case BoxUniformProfile boxUniform:
                            float boxUniformWidth = boxUniform.Width;
                            if (PropertyTable.DragFloatProperty("Width"u8, "The width of the rectangular perimeter"u8, ref boxUniformWidth, 0.1f, 0.0f, float.MaxValue))
                            {
                                boxUniform.Width = boxUniformWidth;
                                _context.HasUnsavedChanges = true;
                            }

                            float boxUniformHeight = boxUniform.Height;
                            if (PropertyTable.DragFloatProperty("Height"u8, "The height of the rectangular perimeter"u8, ref boxUniformHeight, 0.1f, 0.0f, float.MaxValue))
                            {
                                boxUniform.Height = boxUniformHeight;
                                _context.HasUnsavedChanges = true;
                            }
                            break;

                        case BoxProfile box:
                            float boxWidth = box.Width;
                            if (PropertyTable.DragFloatProperty("Width"u8, "The width of the rectangular perimeter"u8, ref boxWidth, 0.1f, 0.0f, float.MaxValue))
                            {
                                box.Width = boxWidth;
                                _context.HasUnsavedChanges = true;
                            }

                            float boxHeight = box.Height;
                            if (PropertyTable.DragFloatProperty("Height"u8, "The height of the rectangular perimeter"u8, ref boxHeight, 0.1f, 0.0f, float.MaxValue))
                            {
                                box.Height = boxHeight;
                                _context.HasUnsavedChanges = true;
                            }
                            break;

                        case CircleProfile circle:
                            float circleRadius = circle.Radius;
                            if (PropertyTable.DragFloatProperty("Radius"u8, "The radius of the circular area"u8, ref circleRadius, 0.1f, 0.0f, float.MaxValue))
                            {
                                circle.Radius = circleRadius;
                                _context.HasUnsavedChanges = true;
                            }

                            ReadOnlySpan<byte> circleRadiatePreview = circle.Radiate switch
                            {
                                CircleRadiation.None => "None"u8,
                                CircleRadiation.In => "In"u8,
                                CircleRadiation.Out => "Out"u8,
                                _ => throw new InvalidOperationException($"Unknown circle radiation '{circle.Radiate}")
                            };
                            if (PropertyTable.BeginComboProperty("Radiate"u8, "The radiation mode that determines how particle headings are calculate"u8, circleRadiatePreview))
                            {
                                if (PropertyTable.ComboItem("None"u8, "Particles move toward the center of the circle"u8, circle.Radiate == CircleRadiation.None))
                                {
                                    circle.Radiate = CircleRadiation.None;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("In"u8, "Particles move in random directions unrelated to their positions"u8, circle.Radiate == CircleRadiation.In))
                                {
                                    circle.Radiate = CircleRadiation.In;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("Out"u8, "Particles move away from the center of the circle"u8, circle.Radiate == CircleRadiation.Out))
                                {
                                    circle.Radiate = CircleRadiation.Out;
                                    _context.HasUnsavedChanges = true;
                                }

                                PropertyTable.EndComboProperty();
                            }
                            break;

                        case LineProfile line:
                            XnaVec2 lineAxis = line.Axis;
                            if (PropertyTable.DragVector2Property("Axis"u8, "The direction vector of the line axis"u8, ref lineAxis, 1f, -1f, 1f))
                            {
                                line.Axis = lineAxis;
                                _context.HasUnsavedChanges = true;
                            }

                            float lineLength = line.Length;
                            if (PropertyTable.DragFloatProperty("Length"u8, "The length of the line segment"u8, ref lineLength, 0.1f, 0.0f, float.MaxValue))
                            {
                                line.Length = lineLength;
                                _context.HasUnsavedChanges = true;
                            }


                            ReadOnlySpan<byte> lineRadiatePreview = line.Radiate switch
                            {
                                LineRadiation.None => "None"u8,
                                LineRadiation.Directional => "Directional"u8,
                                LineRadiation.PerpendicularUp => "Perpendicular Up"u8,
                                LineRadiation.PerpendicularDown => "Perpendicular Down"u8,
                                _ => throw new InvalidOperationException($"Unknown line radiation '{line.Radiate}")
                            };
                            if (PropertyTable.BeginComboProperty("Radiate"u8, "Determines the initial particle headings when radiating from the line axis"u8, lineRadiatePreview))
                            {
                                if (PropertyTable.ComboItem("None"u8, "The initial heading of particles is completely random and has no relationship to their position along the line"u8, line.Radiate == LineRadiation.None))
                                {
                                    line.Radiate = LineRadiation.None;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("Directional"u8, "All particles are given the same initial heading as specified by the Direction vector"u8, line.Radiate == LineRadiation.Directional))
                                {
                                    line.Radiate = LineRadiation.Directional;
                                    line.Direction = XnaVec2.UnitY;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("Perpendicular Up"u8, "All particles are given initial headings perpendicular to the line's axis, pointing upward in screen coordinates (negative Y direction)"u8, line.Radiate == LineRadiation.PerpendicularUp))
                                {
                                    line.Radiate = LineRadiation.PerpendicularUp;
                                    line.Direction = XnaVec2.Zero;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("Perpendicular Down"u8, "All particles are given initial headings perpendicular to the line's axis, pointing downward in screen coordinates (positive Y direction)"u8, line.Radiate == LineRadiation.PerpendicularDown))
                                {
                                    line.Radiate = LineRadiation.PerpendicularDown;
                                    line.Direction = XnaVec2.Zero;
                                    _context.HasUnsavedChanges = true;
                                }

                                PropertyTable.EndComboProperty();
                            }

                            if (line.Radiate == LineRadiation.Directional)
                            {
                                XnaVec2 lineDirection = line.Direction;
                                if (PropertyTable.DragVector2Property("Direction"u8, ""u8, ref lineDirection, 0.1f, -1.0f, 1.0f))
                                {
                                    line.Direction = lineDirection;
                                    _context.HasUnsavedChanges = true;
                                }
                            }
                            break;

                        case PointProfile point:
                            // No additional properties
                            break;

                        case RingProfile ring:
                            float ringRadius = ring.Radius;
                            if (PropertyTable.DragFloatProperty("Radius"u8, "The radius if the ring."u8, ref ringRadius, 0.1f, 0.0f, float.MaxValue))
                            {
                                ring.Radius = ringRadius;
                                _context.HasUnsavedChanges = true;
                            }

                            ReadOnlySpan<byte> ringRadiatePreview = ring.Radiate switch
                            {
                                CircleRadiation.None => "None"u8,
                                CircleRadiation.In => "In"u8,
                                CircleRadiation.Out => "Out"u8,
                                _ => throw new InvalidOperationException($"Unknown circle radiation '{ring.Radiate}")
                            };
                            if (PropertyTable.BeginComboProperty("Radiate"u8, "The radiation mode that determines how particle headings are calculate"u8, ringRadiatePreview))
                            {
                                if (PropertyTable.ComboItem("None"u8, "Particles move in random directions unrelated to their positions"u8, ring.Radiate == CircleRadiation.None))
                                {
                                    ring.Radiate = CircleRadiation.None;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("In"u8, "Particles move toward the center of the circle"u8, ring.Radiate == CircleRadiation.In))
                                {
                                    ring.Radiate = CircleRadiation.In;
                                    _context.HasUnsavedChanges = true;
                                }

                                if (PropertyTable.ComboItem("Out"u8, "Particles move away from the center of the circle"u8, ring.Radiate == CircleRadiation.Out))
                                {
                                    ring.Radiate = CircleRadiation.Out;
                                    _context.HasUnsavedChanges = true;
                                }

                                PropertyTable.EndComboProperty();
                            }
                            break;

                        case SprayProfile spray:
                            XnaVec3 sprayDirection = spray.Direction;
                            if (PropertyTable.DragVector3Property("Direction"u8, "The central direction vector of the spray"u8, ref sprayDirection, 0.1f, float.MinValue, float.MaxValue))
                            {
                                spray.Direction = sprayDirection;
                                _context.HasUnsavedChanges = true;
                            }

                            float spraySpread = spray.Spread;
                            if (PropertyTable.DragFloatProperty("Spread"u8, "The angular spread of the spray cone (in radians)"u8, ref spraySpread, 0.1f, 0.0f, float.MaxValue))
                            {
                                spray.Spread = spraySpread;
                                _context.HasUnsavedChanges = true;
                            }
                            break;
                    }
                    EndTable();
                }

                EndDisabled();
            }
            EndChild();
        }
    }

    private void DrawSelectedEmitterReleaseParameters()
    {
        if (CollapsingHeader("Emitter Release Parameters"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (_context.SelectedEmitter is not ParticleEmitter emitter)
            {
                return;
            }

            ImGuiChildFlags childFlags = ImGuiChildFlags.Borders
                                         | ImGuiChildFlags.AutoResizeY;

            if (BeginChild("##selected-emitter-release-parameters-child-window"u8, SysVec2.Zero, childFlags))
            {
                BeginDisabled(_context.IsLocked(emitter));

                if (PropertyTable.BeginPropertyTable("##selected-emitter-release-parameters-table"u8))
                {
                    // Quantity
                    int quantity = emitter.Quantity;
                    if (PropertyTable.InputIntProperty("Quantity"u8, "The number of particles released per emission"u8, ref quantity))
                    {
                        emitter.Quantity = quantity;
                        _context.HasUnsavedChanges = true;
                    }

                    // Speed Min/Max
                    float speedMin = emitter.SpeedMin;
                    if (PropertyTable.DragFloatProperty("Speed Min"u8, "The minimum initial speed of particles when released"u8, ref speedMin, 0.1f, 0.0f, float.MaxValue))
                    {
                        emitter.SpeedMin = speedMin;
                        _context.HasUnsavedChanges = true;
                    }

                    float speedMax = emitter.SpeedMax;
                    if (PropertyTable.DragFloatProperty("Speed Max"u8, "The maximum initial speed of particles when released"u8, ref speedMax, 0.1f, 0.0f, float.MaxValue))
                    {
                        emitter.SpeedMax = speedMax;
                        _context.HasUnsavedChanges = true;
                    }

                    // Color Min/Max (HSL as Vector3)
                    XnaVec3 colorMin = emitter.ColorStartMin;
                    if (PropertyTable.Color3VectorProperty("Color Min"u8, "The minimum HSL color of particles"u8, ref colorMin))
                    {
                        emitter.ColorStartMin = colorMin;
                        _context.HasUnsavedChanges = true;
                    }

                    XnaVec3 colorMax = emitter.ColorStartMax;
                    if (PropertyTable.Color3VectorProperty("Color Max"u8, "The maximum HSL color of particles"u8, ref colorMax))
                    {
                        emitter.ColorStartMax = colorMax;
                        _context.HasUnsavedChanges = true;
                    }

                    // Opacity Min/Max
                    float opacityMin = emitter.OpacityMin;
                    if (PropertyTable.DragFloatProperty("Opacity Min"u8, "The minimum transparency of particles (0.0 = transparent, 1.0 = opaque)"u8, ref opacityMin, 0.01f, 0.0f, 1.0f))
                    {
                        emitter.OpacityMin = opacityMin;
                        _context.HasUnsavedChanges = true;
                    }

                    float opacityMax = emitter.OpacityMax;
                    if (PropertyTable.DragFloatProperty("Opacity Max"u8, "The maximum transparency of particles (0.0 = transparent, 1.0 = opaque)"u8, ref opacityMax, 0.01f, 0.0f, 1.0f))
                    {
                        emitter.OpacityMax = opacityMax;
                        _context.HasUnsavedChanges = true;
                    }

                    // Scale Min/Max
                    XnaVec2 scaleMin = emitter.ScaleMin;
                    if (PropertyTable.DragVector2Property("Scale Min"u8, "The minimum size multiplier of particles"u8, ref scaleMin, 0.01f, 0.0f, float.MaxValue))
                    {
                        emitter.ScaleMin = scaleMin;
                        _context.HasUnsavedChanges = true;
                    }

                    XnaVec2 scaleMax = emitter.ScaleMax;
                    if (PropertyTable.DragVector2Property("Scale Max"u8, "The maximum size multiplier of particles"u8, ref scaleMax, 0.01f, 0.0f, float.MaxValue))
                    {
                        emitter.ScaleMax = scaleMax;
                        _context.HasUnsavedChanges = true;
                    }

                    // Rotation Min/Max
                    float rotationMin = emitter.RotationMin;
                    if (PropertyTable.DragFloatProperty("Rotation Min"u8, "The minimum initial rotation angle of particles in radians"u8, ref rotationMin, 0.01f, -MathF.PI * 2.0f, MathF.PI * 2.0f))
                    {
                        emitter.RotationMin = rotationMin;
                        _context.HasUnsavedChanges = true;
                    }

                    float rotationMax = emitter.RotationMax;
                    if (PropertyTable.DragFloatProperty("Rotation Max"u8, "The maximum initial rotation angle of particles in radians"u8, ref rotationMax, 0.01f, -MathF.PI * 2.0f, MathF.PI * 2.0f))
                    {
                        emitter.RotationMax = rotationMax;
                        _context.HasUnsavedChanges = true;
                    }

                    // Mass Min/Max
                    float massMin = emitter.MassMin;
                    if (PropertyTable.DragFloatProperty("Mass Min"u8, "The minimum mass of particles (affects physics interactions)"u8, ref massMin, 0.1f, 0.0f, float.MaxValue))
                    {
                        emitter.MassMin = massMin;
                        _context.HasUnsavedChanges = true;
                    }

                    float massMax = emitter.MassMax;
                    if (PropertyTable.DragFloatProperty("Mass Max"u8, "The maximum mass of particles (affects physics interactions)"u8, ref massMax, 0.1f, 0.0f, float.MaxValue))
                    {
                        emitter.MassMax = massMax;
                        _context.HasUnsavedChanges = true;
                    }

                    PropertyTable.EndPropertyTable();
                }

                EndDisabled();
            }
            EndChild();
        }
    }

    private void DrawSelectTexturePopup()
    {
        // I really don't like this way of signalling to open the popup modal.
        // I'd like to find a better approach than storing a state from when
        // select texture is clicked in the main menu, then checking state here
        // and telling the popup to open and then changing state to false,
        // but because the modal needs to be **opened** and **rendered** outside
        // the scope of the child, here we are.
        if (_selectTexture)
        {
            OpenPopup("select-texture"u8);
            _selectTexture = false;
        }

        ImGuiViewportPtr viewportPtr = GetMainViewport();
        SysVec2 workCenter = viewportPtr.WorkPos + (viewportPtr.WorkSize * 0.5f);

        SetNextWindowPos(workCenter, ImGuiCond.Always, new SysVec2(0.5f));
        SetNextWindowSize(viewportPtr.WorkSize * 0.9f, ImGuiCond.Appearing);
        SetNextWindowSizeConstraints(new SysVec2(600, 500), viewportPtr.WorkSize * 0.9f);

        ImGuiWindowFlags modalFlags = ImGuiWindowFlags.Modal
                                      | ImGuiWindowFlags.NoMove
                                      | ImGuiWindowFlags.NoTitleBar;

        if (BeginPopupModal("select-texture"u8, modalFlags))
        {
            FileDialog dialog = FileDialog.GetFileDialog(this, _context.LastUsedTextureDirectory, ".png");
            if (dialog.Draw())
            {
                string filePath = dialog.SelectedItem.FullName;
                string relativePath = _context.GetRelativePath(filePath);

                _context.LastUsedTextureDirectory = Path.GetDirectoryName(filePath);
                _context.AddTexture(filePath);
                AssignTextureToSelectedEmitter(relativePath);
            }
            EndPopup();
        }
    }

    private void AssignTextureToSelectedEmitter(string relativePath)
    {
        ParticleEmitter emitter = _context.SelectedEmitter;
        if (emitter == null)
        {
            return;
        }

        Texture2D texture = _context.GetTexture(relativePath);
        if (texture != null)
        {
            // Set UV to cover full texture
            emitter.UVOffset = Vector2.Zero;
            emitter.UVScale = Vector2.One;
            emitter.TextureSize = new Vector2(texture.Width, texture.Height);

            // Store as the particle texture
            _context.ParticleTexture = texture;
            _context.ParticlePool.Texture = texture;

            _context.HasUnsavedChanges = true;
        }
    }
}
