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
using IfritParticles;


namespace Ember;

public class EmberEditor : Game
{
    private static EmberEditor s_instance;
    private static readonly CompositeFormat s_windowTitle = CompositeFormat.Parse("Ember: {0} | {1:F1} FPS | P:{2} {3}");
    private static readonly string s_version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    private static float s_frameRate;

    private readonly GraphicsDeviceManager _graphics;

    // Input
    private static MouseState s_previousMouseState;
    private static MouseState s_currentMouseState;
    private bool _previousFKeyState;
    private bool _previousSaveKeyState;

    // Orbit camera
    private Quaternion _camRotation = Quaternion.Identity;
    private float _camDistance;
    private Vector3 _camTarget;
    private float _defaultCamDistance;
    // Scroll inertia
    private float _orbitVelX;
    private float _orbitVelY;
    private float _panVelX;
    private float _panVelY;
    private float _zoomVel;
    private const float ScrollDecay = 0.75f;

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
        _context.CenterParticlePool();
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

        // Cmd+S (Mac) / Ctrl+S (Win/Linux): save
        KeyboardState kb = Keyboard.GetState();
        bool modHeld = OperatingSystem.IsMacOS()
            ? kb.IsKeyDown(Keys.LeftWindows) || kb.IsKeyDown(Keys.RightWindows)
            : kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
        if (modHeld && kb.IsKeyDown(Keys.S) && !_previousSaveKeyState)
        {
            _context.SaveProject();
        }
        _previousSaveKeyState = modHeld && kb.IsKeyDown(Keys.S);

        // F key: reset camera (edge-triggered)
        bool fDown = kb.IsKeyDown(Keys.F) && !ImGui.GetIO().WantCaptureKeyboard && !modHeld;
        if (fDown && !_previousFKeyState)
        {
            _camRotation = Quaternion.Identity;
            _camDistance = _defaultCamDistance > 0f ? _defaultCamDistance : 1f;
            _camTarget = Vector3.Zero;
            _orbitVelX = 0f; _orbitVelY = 0f;
            _panVelX = 0f; _panVelY = 0f; _zoomVel = 0f;
        }
        _previousFKeyState = fDown;

        if (_context.IsProjectPaused)
        {
            return;
        }
        if (_context.ParticlePool is ParticlePool particleEffect)
        {
            ImGuiIOPtr ioPtr = ImGui.GetIO();

            bool mouseInClient = s_currentMouseState.X >= 0 && s_currentMouseState.Y >= 0
                && s_currentMouseState.X < GraphicsDevice.Viewport.Width
                && s_currentMouseState.Y < GraphicsDevice.Viewport.Height;

            if (IsActive && mouseInClient && !ioPtr.WantCaptureMouse)
            {
                float dx = s_currentMouseState.X - s_previousMouseState.X;
                float dy = s_currentMouseState.Y - s_previousMouseState.Y;
                bool altHeld = kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt);
                bool ctrlHeld = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);

                if (altHeld)
                {
                    // Alt + Ctrl + Left drag OR Alt + Right drag: dolly/zoom
                    bool zoomDrag = (ctrlHeld && s_currentMouseState.LeftButton == ButtonState.Pressed)
                        || s_currentMouseState.RightButton == ButtonState.Pressed;
                    if (zoomDrag && (dx != 0 || dy != 0))
                    {
                        _camDistance += dy * _camDistance * 0.005f;
                        if (_camDistance < 10f) _camDistance = 10f;
                    }
                    // Alt + Left drag (no Ctrl): orbit
                    else if (!ctrlHeld && s_currentMouseState.LeftButton == ButtonState.Pressed && (dx != 0 || dy != 0))
                    {
                        Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.Up, dx * 0.005f);
                        Vector3 right = Vector3.Transform(Vector3.Right, _camRotation);
                        Quaternion pitch = Quaternion.CreateFromAxisAngle(right, dy * 0.005f);
                        _camRotation = Quaternion.Normalize(pitch * yaw * _camRotation);
                    }
                }
                else
                {
                    // Left drag (no Alt): snap emitter to mouse position on the camera's near plane at Z=0
                    if (s_currentMouseState.LeftButton == ButtonState.Pressed)
                    {
                        int w = GraphicsDevice.Viewport.Width;
                        int h = GraphicsDevice.Viewport.Height;

                        // Normalized device coords (-1 to 1)
                        float ndcX = (2f * s_currentMouseState.X / w) - 1f;
                        float ndcY = 1f - (2f * s_currentMouseState.Y / h);

                        // Camera vectors
                        Quaternion invRot = Quaternion.Inverse(_camRotation);
                        Vector3 camForward = Vector3.Transform(Vector3.Forward, invRot);
                        Vector3 camRight = Vector3.Transform(Vector3.Right, invRot);
                        Vector3 camUp = Vector3.Transform(Vector3.Up, invRot);
                        Vector3 camPos = _camTarget - camForward * _camDistance;

                        // Ray from camera through mouse pixel
                        float fov = MathHelper.PiOver4;
                        float halfH = (float)Math.Tan(fov * 0.5f) * _camDistance;
                        float halfW = halfH * w / h;

                        // World position on the plane passing through _camTarget, perpendicular to camForward
                        Vector3 worldPos = _camTarget + camRight * (ndcX * halfW) + camUp * (ndcY * halfH);
                        particleEffect.WorldPosition = worldPos;
                    }
                }

                // Scroll input feeds velocities
                int scrollDeltaY = s_currentMouseState.ScrollWheelValue - s_previousMouseState.ScrollWheelValue;
                int scrollDeltaX = s_currentMouseState.HorizontalScrollWheelValue - s_previousMouseState.HorizontalScrollWheelValue;
                bool shiftHeld = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);

                if (scrollDeltaX != 0 || scrollDeltaY != 0)
                {
                    if (ctrlHeld)
                    {
                        _zoomVel += scrollDeltaY * 0.0003f;
                    }
                    else if (shiftHeld)
                    {
                        float panScale = _camDistance * 0.00015f;
                        _panVelX += scrollDeltaX * panScale;
                        _panVelY += scrollDeltaY * panScale;
                    }
                    else
                    {
                        _orbitVelX += scrollDeltaX * 0.0006f;
                        _orbitVelY += scrollDeltaY * 0.0006f;
                    }
                }
            }

            // Apply scroll inertia — orbit via quaternion
            if (_orbitVelX != 0f || _orbitVelY != 0f)
            {
                Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.Up, -_orbitVelX);
                Vector3 right = Vector3.Transform(Vector3.Right, _camRotation);
                Quaternion pitch = Quaternion.CreateFromAxisAngle(right, _orbitVelY);
                _camRotation = Quaternion.Normalize(pitch * yaw * _camRotation);
            }
            _camTarget.X += _panVelX;
            _camTarget.Y -= _panVelY;
            _camDistance *= 1f - _zoomVel;
            if (_camDistance < 10f) _camDistance = 10f;

            // Decay velocities
            _orbitVelX *= ScrollDecay;
            _orbitVelY *= ScrollDecay;
            _panVelX *= ScrollDecay;
            _panVelY *= ScrollDecay;
            _zoomVel *= ScrollDecay;

            particleEffect.Update(gameTime);
        }
    }

    protected override unsafe void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_context.ClearColor);

        if (_context.ParticlePool is ParticlePool particleEffect)
        {
            int w = GraphicsDevice.Viewport.Width;
            int h = GraphicsDevice.Viewport.Height;

            // Perspective projection with orbit camera
            float fov = MathHelper.PiOver4;

            // Initialize default distance on first frame
            if (_defaultCamDistance == 0f)
            {
                _defaultCamDistance = h * 0.5f / (float)Math.Tan(fov * 0.5f);
                _camDistance = _defaultCamDistance;
            }

            // Compute camera position from quaternion orbit
            Vector3 camOffset = Vector3.Transform(Vector3.Forward * _camDistance, Quaternion.Inverse(_camRotation));
            Vector3 camPos = _camTarget - camOffset;
            Vector3 camUp = Vector3.Transform(Vector3.Up, Quaternion.Inverse(_camRotation));

            particleEffect.View = Matrix.CreateLookAt(camPos, _camTarget, camUp);
            particleEffect.Projection = Matrix.CreatePerspectiveFieldOfView(fov, (float)w / h, 1f, _camDistance * 10f);

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

        // Reset camera button — only show when project is open
        if (_context.IsProjectOpen)
        {
            ImGui.SetNextWindowPos(new SysVec2(GraphicsDevice.Viewport.Width * 0.5f - 50, GraphicsDevice.Viewport.Height - 40));
            ImGui.SetNextWindowSize(new SysVec2(100, 0));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new SysVec2(4, 4));
            if (ImGui.Begin("##cam-reset"u8, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground))
            {
                if (ImGui.Button("Reset Cam"u8, new SysVec2(92, 0)))
                {
                    _camRotation = Quaternion.Identity;
                    _camDistance = _defaultCamDistance;
                    _camTarget = Vector3.Zero;
                    _orbitVelX = 0f; _orbitVelY = 0f;
                    _panVelX = 0f; _panVelY = 0f; _zoomVel = 0f;
                }
            }
            ImGui.End();
            ImGui.PopStyleVar();
        }

        _mainView.Draw();
        ImGuiRenderer.AfterLayout();

        s_frameRate = ImGui.GetIO().Framerate;


        int liveCount = (_context.ParticlePool as ParticlePool)?.TotalLiveCount ?? 0;
        Window.Title = string.Format(CultureInfo.InvariantCulture, s_windowTitle, s_version, s_frameRate, liveCount, _context.HasUnsavedChanges ? "*" : string.Empty);
    }
}
