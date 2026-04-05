// Copyright (c) Christopher Whitley and Contributors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Ember.Architecture;
using Ember.Architecture.Style;
using Ember.Architecture.Views;
using Ember.Graphics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ifrit;


namespace Ember;

public class EmberEditor : Game
{
    private static EmberEditor s_instance;
    private static readonly CompositeFormat s_windowTitle = CompositeFormat.Parse("Ember: {0} | {1:F3} ms/Frame | {2:F1} FPS {3}");
    private static readonly string s_version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    private static float s_frameRate;

    private readonly GraphicsDeviceManager _graphics;

    // Input
    private static MouseState s_previousMouseState;
    private static MouseState s_currentMouseState;

    private MainView _mainView;
    private EditorContext _context;

    public EmberEditor()
    {
        s_instance = this;
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        _context.CenterParticleEffect();
    }

    protected override unsafe void Initialize()
    {
        base.Initialize();


        // Initialize ImGui
        ImGuiRenderer.Initialize(this);

        // Enable docking
        ImGuiIOPtr ioPtr = ImGui.GetIO();
        ioPtr.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        Fonts.Load();

        _context = new(this);
        _context.ApplyTheme<CatppuccinFrappeTheme>();

        // Load the particle shader
        _context.LoadParticleShader();

        _mainView = new MainView(_context);

    }

    protected override void LoadContent()
    {
    }

    protected override void Update(GameTime gameTime)
    {
        s_previousMouseState = s_currentMouseState;
        s_currentMouseState = Mouse.GetState();

        if (_context.IsProjectPaused)
        {
            return;
        }
        // Emit particles on click
        if (_context.ParticleEffect is ParticleEffect particleEffect)
        {
            ImGuiIOPtr ioPtr = ImGui.GetIO();

            // Only emit if the click happens somewhere that ImGui is not
            // actively capturing mouse inputs
            if (!ioPtr.WantCaptureMouse && s_currentMouseState.LeftButton == ButtonState.Pressed)
            {
                XnaVec2 mousePos = s_currentMouseState.Position.ToVector2();
                particleEffect.WorldPosition = new Vector3(mousePos.X, mousePos.Y, 0f);
                particleEffect.Trigger();
            }

            // Update the particle effect
            particleEffect.Update(gameTime);
        }
    }

    protected override unsafe void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_context.ClearColor);

        if (_context.ParticleEffect is ParticleEffect particleEffect)
        {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;

            // Set up orthographic projection for 2D particle rendering
            particleEffect.View = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);
            particleEffect.Projection = Matrix.CreateOrthographic(w, h, 0.01f, 10f);

            // Set texture if available
            if (_context.ParticleTexture != null)
            {
                particleEffect.Texture = _context.ParticleTexture;
            }

            // Set blend state for alpha blending
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;

            particleEffect.Draw();
        }

        ImGuiRenderer.BeforeLayout(gameTime);
        _mainView.Draw();
        ImGuiRenderer.AfterLayout();

        s_frameRate = ImGui.GetIO().Framerate;


        Window.Title = string.Format(CultureInfo.InvariantCulture, s_windowTitle, s_version, 1000.0f / s_frameRate, s_frameRate, _context.HasUnsavedChanges ? "*" : string.Empty);
    }
}
