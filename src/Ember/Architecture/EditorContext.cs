using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Ember.Architecture.Style;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using IfritParticles;
using IfritParticles.Modifiers;
using IfritParticles.Interpolators;
using IfritParticles.Profiles;

namespace Ember.Architecture;

public sealed class EditorContext : IDisposable
{
    private readonly Dictionary<object, bool> _locks = [];
    private readonly Dictionary<string, Texture2D> _textureCache = [];
    private readonly Game _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _contentManager;
    private readonly string _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    private float _baseFontSize = 16.0f;
    private float _fontScaleMain = 1.0f;

    private bool _shouldExit;
    private PendingAction _pendingAction = PendingAction.None;
    private string _pendingProjectName;
    private string _pendingProjectDirectory;
    private bool _pendingCreateProjectDirectory;
    private string _pendingProjectFilePath;

    // The particle shader effect (loaded once, shared by all effects)
    private Effect _particleEffect;

    public ParticleSystem ParticleSystem { get; private set; }
    public ParticleEmitter SelectedEmitter { get; private set; }
    public int SelectedEmitterIndex { get; private set; } = -1;

    public IModifier SelectedModifier { get; private set; }
    public int SelectedModifierIndex { get; private set; } = -1;

    public IInterpolator SelectedInterpolator { get; private set; }
    public int SelectedInterpolatorIndex { get; private set; } = -1;

    public string ProjectName { get; private set; } = string.Empty;
    public string ProjectDirectory { get; private set; } = string.Empty;
    public string ProjectFilePath { get; private set; } = string.Empty;

    public string LastUsedTextureDirectory { get; set; } = string.Empty;
    public string LastUsedProjectDirectory { get; set; } = string.Empty;

    private const int MaxRecentFiles = 10;
    private static readonly string RecentFilesPath = Path.Combine(GetAppDataDirectory(), "recent.json");

    private static string GetAppDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "Ember");
        // Windows: AppData/Roaming/Ember, Linux: ~/.config/Ember
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ember");
    }

    public List<string> RecentFiles { get; private set; } = new List<string>();

    public void AddRecentFile(string filePath)
    {
        RecentFiles.Remove(filePath);
        RecentFiles.Insert(0, filePath);
        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        SaveRecentFiles();
    }

    private void LoadRecentFiles()
    {
        try
        {
            if (File.Exists(RecentFilesPath))
            {
                string json = File.ReadAllText(RecentFilesPath);
                RecentFiles = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                RecentFiles = RecentFiles.Where(File.Exists).ToList();
            }
        }
        catch { RecentFiles = new List<string>(); }
    }

    public void ClearRecentFiles()
    {
        RecentFiles.Clear();
        SaveRecentFiles();
    }

    private void SaveRecentFiles()
    {
        try
        {
            string dir = Path.GetDirectoryName(RecentFilesPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(RecentFilesPath, JsonSerializer.Serialize(RecentFiles));
        }
        catch { }
    }

    public bool HasUnsavedChanges { get; set; }
    public bool IsProjectOpen => ParticleSystem != null;
    public bool IsProjectPaused { get; private set; } = false;
    public bool IsSavePromptPending => _pendingAction != PendingAction.None;

    /// <summary>The shared texture atlas assigned to the particle effect.</summary>
    public Texture2D ParticleTexture { get; set; }

    public float BaseFontSize
    {
        get => _baseFontSize;
        set
        {
            if (Math.Abs(_baseFontSize - value) > 0.0001f)
            {
                _baseFontSize = Math.Clamp(value, 8.0f, 72.0f);
                ApplyFontSettings();
            }
        }
    }

    public float FontScaleMain
    {
        get => _fontScaleMain;
        set
        {
            if (Math.Abs(_fontScaleMain - value) > 0.0001f)
            {
                _fontScaleMain = Math.Clamp(value, 0.5f, 4.0f);
                ApplyFontSettings();
            }
        }
    }

    public float EffectiveFontSize => _baseFontSize * _fontScaleMain;

    public XnaColor ClearColor { get; set; } = XnaColor.Black;

    public ITheme CurrentTheme { get; set; }

    public string Version => _version;

    public Game Game => _game;

    public bool IsDisposed { get; private set; }

    public EditorContext(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        _game = game;
        _graphicsDevice = game.GraphicsDevice;
        _contentManager = game.Content;
        _game.Exiting += OnExiting;
        ApplyTheme<CatppuccinFrappeTheme>();
        ApplyFontSettings();
        LoadRecentFiles();
    }

    ~EditorContext() => Dispose(false);

    /// <summary>
    /// Loads the Particle shader effect. Uses a dedicated ContentManager so that the
    /// shared shader is not affected when _contentManager.Unload() is called on project close.
    /// </summary>
    public void LoadParticleShader()
    {
        if (_particleEffect != null)
            return;

        // Use a separate ContentManager so unloading project content doesn't dispose the shader
        var shaderContent = new ContentManager(_game.Services, "Content");
        _particleEffect = shaderContent.Load<Effect>("Effects/Particle");
    }

    public Effect GetParticleShader() => _particleEffect;

    private void OnExiting(object sender, EventArgs e)
    {
        if (!_shouldExit && HasUnsavedChanges)
        {
            _pendingAction = PendingAction.Exit;
            ((ExitingEventArgs)e).Cancel = true;
        }
    }

    public void RequestExit()
    {
        if (HasUnsavedChanges)
        {
            _pendingAction = PendingAction.Exit;
        }
        else
        {
            _game.Exit();
        }
    }

    public void RequestCreateProject(string projectName, string projectDirectory, bool createProjectDirectory)
    {
        if (HasUnsavedChanges)
        {
            _pendingAction = PendingAction.CreateProject;
            _pendingProjectName = projectName;
            _pendingProjectDirectory = projectDirectory;
            _pendingCreateProjectDirectory = createProjectDirectory;
        }
        else
        {
            CreateProject(projectName, projectDirectory, createProjectDirectory);
        }
    }

    public void RequestOpenProject(string filePath)
    {
        if (HasUnsavedChanges)
        {
            _pendingAction = PendingAction.OpenProject;
            _pendingProjectFilePath = filePath;
        }
        else
        {
            OpenProject(filePath);
        }
    }

    public void ConfirmPendingAction(bool save)
    {
        if (save)
        {
            SaveProject();
        }

        ExecutePendingAction();
    }

    public void CancelPendingAction()
    {
        _pendingAction = PendingAction.None;
        _pendingProjectName = null;
        _pendingProjectDirectory = null;
        _pendingCreateProjectDirectory = false;
        _pendingProjectFilePath = null;
    }

    private void ExecutePendingAction()
    {
        switch (_pendingAction)
        {
            case PendingAction.Exit:
                _shouldExit = true;
                _game.Exit();
                break;
            case PendingAction.CreateProject:
                CreateProject(_pendingProjectName, _pendingProjectDirectory, _pendingCreateProjectDirectory);
                break;
            case PendingAction.OpenProject:
                OpenProject(_pendingProjectFilePath);
                break;
        }

        CancelPendingAction();
    }

    public void CenterParticleSystem()
    {
        if (ParticleSystem == null)
        {
            return;
        }

        ParticleSystem.WorldPosition = Vector3.Zero;
    }

    internal string GenerateEmitterName()
    {
        int index = 0;
        while (true)
        {
            string name = "Emitter" + index;
            bool exists = false;
            for (int i = 0; i < ParticleSystem.Emitters.Count; i++)
            {
                if (ParticleSystem.Emitters[i].Name == name)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists) return name;
            index++;
        }
    }

    public void AddEmitter()
    {
        if (ParticleSystem == null)
        {
            return;
        }

        var emitter = new ParticleEmitter(1000)
        {
            Profile = new PointProfile(),
            Lifetime = 1f,
            SpeedMin = 50f,
            SpeedMax = 100f,
            ColorStartMin = new Vector3(0f, 0f, 1f),
            ColorStartMax = new Vector3(0f, 0f, 1f),
            OpacityMin = 1f,
            OpacityMax = 1f,
            ScaleMin = new Vector2(0f, 0f),
            ScaleMax = new Vector2(0.25f, 0.25f),
            Quantity = 1,
            AutoTrigger = ParticleSystem.AutoTrigger,
            AutoTriggerFrequency = 0.01f,
            Name = GenerateEmitterName()
        };

        // Inherit texture settings from existing texture
        if (ParticleTexture != null)
        {
            emitter.TextureSize = new Microsoft.Xna.Framework.Vector2(ParticleTexture.Width, ParticleTexture.Height);
        }

        int index = ParticleSystem.Emitters.Count;
        ParticleSystem.Emitters.Add(emitter);
        TrackLock(emitter);
        SelectEmitter(index);
        HasUnsavedChanges = true;
    }

    public void SelectEmitter(int index)
    {
        if (ParticleSystem == null || index < 0 || index >= ParticleSystem.Emitters.Count)
        {
            SelectedEmitter = null;
            SelectedEmitterIndex = -1;
        }
        else
        {
            SelectedEmitter = ParticleSystem.Emitters[index];
            SelectedEmitterIndex = index;
        }

        // Select the first modifier of this emitter (if there is one)
        SelectModifier(0);
    }

    public void RemoveEmitter(int index)
    {
        if (ParticleSystem == null || index < 0 || index >= ParticleSystem.Emitters.Count)
        {
            return;
        }

        ParticleEmitter emitter = ParticleSystem.Emitters[index];
        ParticleSystem.Emitters.RemoveAt(index);
        UntrackLock(emitter);

        // Update selection if we removed the selected emitter
        if (emitter == SelectedEmitter)
        {
            int newIndex = Math.Max(0, index - 1);
            SelectEmitter(newIndex);
        }

        HasUnsavedChanges = true;
    }

    public void ReorderEmitters(int fromIndex, int toIndex)
    {
        if (ParticleSystem == null || fromIndex < 0 || fromIndex >= ParticleSystem.Emitters.Count || toIndex < 0 || toIndex >= ParticleSystem.Emitters.Count)
        {
            return;
        }

        ParticleEmitter moving = ParticleSystem.Emitters[fromIndex];
        ParticleSystem.Emitters.RemoveAt(fromIndex);
        ParticleSystem.Emitters.Insert(toIndex, moving);

        // Maintain current selection by re-selecting at the potentially new index
        if (SelectedEmitter != null)
        {
            int currentIndex = ParticleSystem.Emitters.IndexOf(SelectedEmitter);
            SelectEmitter(currentIndex);
        }

        HasUnsavedChanges = true;
    }

    public void AddModifier(Type modifierType)
    {
        if (SelectedEmitter == null)
        {
            return;
        }

        IModifier modifier = CreateModifier(modifierType);
        int index = SelectedEmitter.Modifiers.Count;
        SelectedEmitter.Modifiers.Add(modifier);
        TrackLock(modifier);
        SelectModifier(index);
        HasUnsavedChanges = true;
    }

    public void SelectModifier(int index)
    {
        if (SelectedEmitter == null || index < 0 || index >= SelectedEmitter.Modifiers.Count)
        {
            SelectedModifier = null;
            SelectedModifierIndex = -1;
        }
        else
        {
            SelectedModifier = SelectedEmitter.Modifiers[index];
            SelectedModifierIndex = index;
        }

        // Select the first interpolator of this modifier (if there is one)
        SelectInterpolator(0);
    }

    public void RemoveModifier(int index)
    {
        if (SelectedEmitter == null || index < 0 || index >= SelectedEmitter.Modifiers.Count)
        {
            return;
        }

        IModifier modifier = SelectedEmitter.Modifiers[index];
        SelectedEmitter.Modifiers.RemoveAt(index);
        UntrackLock(modifier);

        // Update selection if we removed the selected modifier
        if (modifier == SelectedModifier)
        {
            int newIndex = Math.Max(0, index - 1);
            SelectModifier(newIndex);
        }

        HasUnsavedChanges = true;
    }

    public void ReorderModifiers(int fromIndex, int toIndex)
    {
        if (SelectedEmitter == null || fromIndex < 0 || fromIndex >= SelectedEmitter.Modifiers.Count || toIndex < 0 || toIndex >= SelectedEmitter.Modifiers.Count)
        {
            return;
        }

        IModifier moving = SelectedEmitter.Modifiers[fromIndex];
        SelectedEmitter.Modifiers.RemoveAt(fromIndex);
        SelectedEmitter.Modifiers.Insert(toIndex, moving);

        // Maintain current selection by re-selecting at the potentially new index
        if (SelectedModifier != null)
        {
            int currentIndex = SelectedEmitter.Modifiers.IndexOf(SelectedModifier);
            SelectModifier(currentIndex);
        }

        HasUnsavedChanges = true;
    }

    public bool SupportsInterpolators(IModifier modifier) => modifier is AgeModifier || modifier is VelocityModifier;

    public List<IInterpolator> GetCurrentInterpolators()
    {
        return SelectedModifier switch
        {
            AgeModifier age => age.Interpolators,
            VelocityModifier velocity => velocity.Interpolators,
            _ => null
        };
    }


    private IModifier CreateModifier(Type modifierType)
    {
        if (modifierType == typeof(RectangleLoopContainerModifier))
        {
            return new RectangleLoopContainerModifier() { Width = 100, Height = 100 };
        }

        if (modifierType == typeof(RectangleContainerModifier))
        {
            return new RectangleContainerModifier { Width = 100, Height = 100 };
        }

        if (modifierType == typeof(LinearGravityModifier))
        {
            return new LinearGravityModifier() { Direction = Vector3.UnitY, Strength = 100.0f };
        }

        if (modifierType == typeof(VortexModifier))
        {
            return new VortexModifier() { };
        }

        if (modifierType == typeof(NoiseModifier))
        {
            return new NoiseModifier() { Strength = 50f, Frequency = 1f, ScrollSpeed = 1f, Octaves = 1 };
        }

        if (modifierType == typeof(OpacityFastFadeModifier))
        {
            return new OpacityFastFadeModifier();
        }

        if (modifierType == typeof(AgeModifier))
        {
            return new AgeModifier() { Interpolators = { new ScaleInterpolator() { StartValue = Vector2.Zero, EndValue = Vector2.One } } };
        }

        if (modifierType == typeof(CircleContainerModifier))
        {
            return new CircleContainerModifier() { Radius = 100.0f };
        }

        if (modifierType == typeof(DragModifier))
        {
            return new DragModifier();
        }

        if (modifierType == typeof(RotationModifier))
        {
            return new RotationModifier() { RotationRate = MathF.PI / 4.0f };
        }

        if (modifierType == typeof(VelocityColorModifier))
        {
            return new VelocityColorModifier() { VelocityThreshold = 100.0f, StationaryColor = new Vector3(0, 0, 1.0f), VelocityColor = new Vector3(0, 1.0f, 0.5f) };
        }

        if (modifierType == typeof(VelocityModifier))
        {
            return new VelocityModifier() { VelocityThreshold = 100.0f, Interpolators = { new ScaleInterpolator() { StartValue = Vector2.Zero, EndValue = Vector2.One } } };
        }

        throw new InvalidOperationException($"Unknown modifier type '{modifierType.Name}'");
    }

    public void AddInterpolator(Type interpolatorType)
    {
        List<IInterpolator> interpolators = GetCurrentInterpolators();
        if (interpolators == null)
        {
            return;
        }

        IInterpolator interpolator = CreateInterpolator(interpolatorType);
        int index = interpolators.Count;
        interpolators.Add(interpolator);
        TrackLock(interpolator);
        SelectInterpolator(index);
        HasUnsavedChanges = true;
    }

    public void SelectInterpolator(int index)
    {
        List<IInterpolator> interpolators = GetCurrentInterpolators();
        if (interpolators == null || index < 0 || index >= interpolators.Count)
        {
            SelectedInterpolator = null;
            SelectedInterpolatorIndex = -1;
        }
        else
        {
            SelectedInterpolator = interpolators[index];
            SelectedInterpolatorIndex = index;
        }
    }

    public void RemoveInterpolator(int index)
    {
        List<IInterpolator> interpolators = GetCurrentInterpolators();
        if (interpolators == null || index < 0 || index >= interpolators.Count)
        {
            return;
        }

        IInterpolator interpolator = interpolators[index];
        interpolators.RemoveAt(index);
        UntrackLock(interpolator);

        // Update selection if we removed the selected interpolator
        if (interpolator == SelectedInterpolator)
        {
            int newIndex = Math.Max(0, index - 1);
            SelectInterpolator(newIndex);
        }

        HasUnsavedChanges = true;
    }

    public void ReorderInterpolators(int fromIndex, int toIndex)
    {
        List<IInterpolator> interpolators = GetCurrentInterpolators();
        if (interpolators == null || fromIndex < 0 || fromIndex >= interpolators.Count || toIndex < 0 || toIndex >= interpolators.Count)
        {
            return;
        }

        IInterpolator moving = interpolators[fromIndex];
        interpolators.RemoveAt(fromIndex);
        interpolators.Insert(toIndex, moving);

        // Maintain current selection by re-selecting at the potentially new index
        if (SelectedInterpolator != null)
        {
            int currentIndex = interpolators.IndexOf(SelectedInterpolator);
            SelectInterpolator(currentIndex);
        }

        HasUnsavedChanges = true;
    }

    private static IInterpolator CreateInterpolator(Type interpolatorType)
    {
        if (interpolatorType == typeof(ColorInterpolator))
        {
            return new ColorInterpolator() { StartValue = new Vector3(0.0f, 0.0f, 0.0f), EndValue = new Vector3(0.0f, 0.0f, 1.0f) };
        }

        if (interpolatorType == typeof(HueInterpolator))
        {
            return new HueInterpolator() { StartValue = 0.0f, EndValue = 1.0f };
        }

        if (interpolatorType == typeof(OpacityInterpolator))
        {
            return new OpacityInterpolator() { StartValue = 0.0f, EndValue = 1.0f };
        }

        if (interpolatorType == typeof(RotationInterpolator))
        {
            return new RotationInterpolator() { StartValue = 0.0f, EndValue = MathF.PI / 2.0f };
        }

        if (interpolatorType == typeof(ScaleInterpolator))
        {
            return new ScaleInterpolator() { StartValue = Vector2.One, EndValue = Vector2.Zero };
        }

        if (interpolatorType == typeof(VelocityInterpolator))
        {
            return new VelocityInterpolator() { StartValue = Vector2.Zero, EndValue = Vector2.One };
        }

        throw new InvalidOperationException($"Unknown interpolator type '{interpolatorType.Name}'");
    }
    public void ClearSelection()
    {
        SelectEmitter(-1);
    }

    public bool IsLocked(ParticleEmitter emitter) => _locks.GetValueOrDefault(emitter, false);
    public bool IsLocked(IModifier modifier) => _locks.GetValueOrDefault(modifier, false);
    public bool IsLocked(IInterpolator interpolator) => _locks.GetValueOrDefault(interpolator, false);
    public bool ToggleLock(ParticleEmitter emitter) => _locks[emitter] = !IsLocked(emitter);
    public bool ToggleLock(IModifier modifier) => _locks[modifier] = !IsLocked(modifier);
    public bool ToggleLock(IInterpolator interpolator) => _locks[interpolator] = !IsLocked(interpolator);
    public void TrackLock(ParticleEmitter emitter) => _locks[emitter] = false;
    public void TrackLock(IModifier modifier) => _locks[modifier] = false;
    public void TrackLock(IInterpolator interpolator) => _locks[interpolator] = false;
    public void UntrackLock(IModifier emitter) => _locks.Remove(emitter);
    public void UntrackLock(ParticleEmitter modifier) => _locks.Remove(modifier);
    public void UntrackLock(IInterpolator parameter) => _locks.Remove(parameter);
    public void ClearLocks() => _locks.Clear();

    public string GetWorkingDirectory() => Directory.GetCurrentDirectory();
    public void SetWorkingDirectory(string directory) => Directory.SetCurrentDirectory(directory);

    public string GetRelativePath(string filePath) => Path.GetRelativePath(GetWorkingDirectory(), filePath);

    public bool TextureExists(string relativePath) => _textureCache.ContainsKey(relativePath);

    public void AddTexture(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        string relativePath = GetRelativePath(filePath);

        if (TextureExists(relativePath))
        {
            return;
        }

        LoadTexture(filePath, relativePath);
    }

    private Texture2D LoadTexture(string absolutePath, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(absolutePath);
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        Texture2D texture = Texture2D.FromFile(_graphicsDevice, absolutePath);
        texture.Name = relativePath;

        // Premultiply alpha to match content pipeline behavior
        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            float a = pixels[i].A / 255f;
            pixels[i] = new Color(
                (byte)(pixels[i].R * a),
                (byte)(pixels[i].G * a),
                (byte)(pixels[i].B * a),
                pixels[i].A);
        }
        texture.SetData(pixels);

        _textureCache[relativePath] = texture;

        return texture;
    }

    public Texture2D GetTexture(string relativePath)
    {
        if (_textureCache.TryGetValue(relativePath, out Texture2D texture))
        {
            return texture;
        }

        return null;
    }

    public IEnumerable<string> GetTextureNames() => _textureCache.Keys;

    public void ClearTextures()
    {
        foreach (var kvp in _textureCache)
        {
            kvp.Value.Dispose();
        }

        _textureCache.Clear();
    }

    public void CreateProject(string projectName, string projectDirectory, bool createProjectDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectName);
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);

        CloseProject();

        ProjectName = projectName;
        ProjectDirectory = projectDirectory;

        if (createProjectDirectory)
        {
            ProjectDirectory = Path.Combine(ProjectDirectory, ProjectName);
        }

        ProjectFilePath = Path.Combine(ProjectDirectory, ProjectName);
        ProjectFilePath = Path.ChangeExtension(ProjectFilePath, ".ember");

        Directory.CreateDirectory(ProjectDirectory);

        SetWorkingDirectory(ProjectDirectory);
        _contentManager.RootDirectory = ProjectDirectory;

        LastUsedTextureDirectory = ProjectDirectory;
        LastUsedProjectDirectory = ProjectDirectory;

        ParticleSystem = new ParticleSystem(_graphicsDevice, _particleEffect.Clone());
        ParticleSystem.Name = ProjectName;
        ParticleSystem.ForceAutoTrigger = true;

        CenterParticleSystem();

        HasUnsavedChanges = true;
        SaveProject();
        AddRecentFile(ProjectFilePath);
    }

    public void OpenProject(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        CloseProject();

        ProjectName = Path.GetFileNameWithoutExtension(filePath);
        ProjectDirectory = Path.GetDirectoryName(filePath);
        ProjectFilePath = filePath;

        SetWorkingDirectory(ProjectDirectory);
        _contentManager.RootDirectory = ProjectDirectory;

        LastUsedTextureDirectory = ProjectDirectory;
        LastUsedProjectDirectory = ProjectDirectory;

        // Load ember data from file
        EmberData data = EmberLoader.Load(ProjectFilePath);

        // Reconstruct ParticleSystem
        ParticleSystem = new ParticleSystem(_graphicsDevice, _particleEffect.Clone());
        ParticleSystem.Name = data.Name;
        ParticleSystem.AutoTrigger = data.AutoTrigger;
        ParticleSystem.AutoTriggerFrequency = data.AutoTriggerFrequency;
        ParticleSystem.ForceAutoTrigger = true;

        // Load textures and create emitters
        foreach (EmberEmitterData emitterData in data.Emitters)
        {
            // Load texture if specified
            int atlasWidth = 1;
            int atlasHeight = 1;
            if (!string.IsNullOrEmpty(emitterData.TextureName))
            {
                string absoluteTexturePath = Path.Combine(ProjectDirectory, emitterData.TextureName);
                if (File.Exists(absoluteTexturePath))
                {
                    AddTexture(absoluteTexturePath);
                    Texture2D tex = GetTexture(emitterData.TextureName);
                    if (tex != null)
                    {
                        atlasWidth = tex.Width;
                        atlasHeight = tex.Height;
                        ParticleTexture = tex;
                        ParticleSystem.Texture = tex;
                    }
                }
            }

            ParticleEmitter emitter = EmberLoader.CreateEmitter(emitterData, data, atlasWidth, atlasHeight);
            emitter.Name = emitterData.Name ?? nameof(ParticleEmitter);
            ParticleSystem.Emitters.Add(emitter);
            TrackLock(emitter);

            // Track locks for modifiers and interpolators
            foreach (IModifier mod in emitter.Modifiers)
            {
                TrackLock(mod);
                if (mod is AgeModifier ageMod)
                {
                    foreach (IInterpolator interp in ageMod.Interpolators)
                        TrackLock(interp);
                }
                else if (mod is VelocityModifier velMod)
                {
                    foreach (IInterpolator interp in velMod.Interpolators)
                        TrackLock(interp);
                }
            }
        }

        CenterParticleSystem();
        AddRecentFile(filePath);

        HasUnsavedChanges = false;
    }

    public void SaveProject()
    {
        if (ParticleSystem == null)
        {
            return;
        }

        var context = new EmberWriteContext
        {
            EffectName = ParticleSystem.Name ?? ProjectName,
            AutoTrigger = ParticleSystem.AutoTrigger,
            AutoTriggerFrequency = ParticleSystem.AutoTriggerFrequency,
        };

        foreach (ParticleEmitter emitter in ParticleSystem.Emitters)
        {
            var emitterData = new EmitterWriteData
            {
                Emitter = emitter,
                Name = emitter.Name,
            };

            // Try to find the texture name and bounds for this emitter
            if (ParticleTexture != null)
            {
                emitterData.TextureName = ParticleTexture.Name;
                emitterData.TextureBoundsX = (int)(emitter.UVOffset.X * ParticleTexture.Width);
                emitterData.TextureBoundsY = (int)(emitter.UVOffset.Y * ParticleTexture.Height);
                emitterData.TextureBoundsWidth = (int)(emitter.UVScale.X * ParticleTexture.Width);
                emitterData.TextureBoundsHeight = (int)(emitter.UVScale.Y * ParticleTexture.Height);
            }

            context.Emitters.Add(emitterData);
        }

        EmberWriter.Save(ProjectFilePath, context);

        HasUnsavedChanges = false;
    }

    public void CloseProject()
    {
        if (ParticleSystem != null)
        {
            ParticleSystem.Dispose();
            ParticleSystem = null;
        }

        ParticleTexture = null;
        _contentManager.Unload();
        ClearTextures();
        ClearSelection();
        ClearLocks();

        ProjectName = string.Empty;
        ProjectDirectory = string.Empty;
        ProjectFilePath = string.Empty;
        HasUnsavedChanges = false;

        GC.Collect();
    }

    public void PauseProject(bool pause) => IsProjectPaused = pause;

    private void ApplyFontSettings()
    {
        ImGuiStylePtr stylePtr = ImGui.GetStyle();
        stylePtr.FontSizeBase = _baseFontSize;
        stylePtr.FontScaleMain = _fontScaleMain;
        stylePtr.FontScaleDpi = 1.0f;

        // Temporary hack from ImGui demo until font atlas rebuilding is finalized
        stylePtr.NextFrameFontSizeBase = _baseFontSize;
    }

    public void ApplyTheme<T>() where T : ITheme
    {
        CurrentTheme = Activator.CreateInstance(typeof(T)) as ITheme;
        Theme.Apply(CurrentTheme);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        if (disposing)
        {
            ClearTextures();
        }

        IsDisposed = true;
    }
}
