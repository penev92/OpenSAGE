﻿using System.Collections.Generic;
using System.IO;
using OpenSage.Content;
using OpenSage.Data;
using OpenSage.Graphics;
using OpenSage.Graphics.Animation;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.ParticleSystems;
using OpenSage.Gui.Wnd;
using OpenSage.Input;
using OpenSage.Logic.Object;
using OpenSage.LowLevel;
using OpenSage.LowLevel.Graphics3D;
using OpenSage.Scripting;

namespace OpenSage
{
    public sealed class Game : DisposableBase
    {
        private readonly FileSystem _fileSystem;
        private readonly GameTimer _gameTimer;

        private readonly Dictionary<string, HostCursor> _cachedCursors;
        private HostCursor _currentCursor;

        private Scene _scene;

        public ContentManager ContentManager { get; private set; }

        public GraphicsDevice GraphicsDevice { get; }
        internal SwapChain SwapChain { get; private set; }

        internal List<GameSystem> GameSystems { get; }

        /// <summary>
        /// Gets the graphics system.
        /// </summary>
        public GraphicsSystem Graphics { get; }

        /// <summary>
        /// Gets the GUI system.
        /// </summary>
        public WndSystem Wnd { get; }

        /// <summary>
        /// Gets the input system.
        /// </summary>
        public InputSystem Input { get; }

        /// <summary>
        /// Gets the scripting system.
        /// </summary>
        public ScriptingSystem Scripting { get; }

        public int FrameCount { get; private set; }

        public GameTime UpdateTime { get; private set; }

        public bool IsActive { get; set; }

        private HostView _hostView;
        internal HostView HostView
        {
            get => _hostView;
            set
            {
                _hostView = value;

                if (value != null)
                {
                    Input.MessageBuffer.SetHostView(value);

                    SetSwapChain(value.SwapChain);
                    ResetElapsedTime();

                    _hostView.SetCursor(_currentCursor);
                }
                else
                {
                    Input.MessageBuffer.SetHostView(null);

                    Scene = null;
                    ContentManager.Unload();

                    SetSwapChain(null);
                }
            }
        }

        public SageGame SageGame { get; }

        public Game(GraphicsDevice graphicsDevice, FileSystem fileSystem, SageGame sageGame)
        {
            GraphicsDevice = graphicsDevice;
            SageGame = sageGame;

            _fileSystem = fileSystem;

            _gameTimer = AddDisposable(new GameTimer());
            _gameTimer.Start();

            _cachedCursors = new Dictionary<string, HostCursor>();

            ContentManager = AddDisposable(new ContentManager(
                _fileSystem, 
                graphicsDevice,
                sageGame));

            ContentManager.IniDataContext.LoadIniFile(@"Data\INI\GameData.ini");
            ContentManager.IniDataContext.LoadIniFile(@"Data\INI\Mouse.ini");

            GameSystems = new List<GameSystem>();

            Input = AddDisposable(new InputSystem(this));

            AddDisposable(new AnimationSystem(this));
            AddDisposable(new ObjectSystem(this));
            AddDisposable(new ParticleSystemSystem(this));
            AddDisposable(new UpdateableSystem(this));

            Wnd = AddDisposable(new WndSystem(this));

            Graphics = AddDisposable(new GraphicsSystem(this));

            Scripting = AddDisposable(new ScriptingSystem(this));

            GameSystems.ForEach(gs => gs.Initialize());

            SetCursor("Arrow");
        }

        public void SetSwapChain(SwapChain swapChain)
        {
            SwapChain = swapChain;
            if (Scene != null)
            {
                Scene.Camera.SetSwapChain(swapChain);
            }
            foreach (var gameSystem in GameSystems)
            {
                gameSystem.OnSwapChainChanged();
            }
        }

        public void ResetElapsedTime()
        {
            _gameTimer.Reset();
        }

        public void Activate()
        {
            // TODO: Unpause everything.
        }

        public void Deactivate()
        {
            // TODO: Pause everything.
        }

        public Scene Scene
        {
            get { return _scene; }
            set
            {
                var oldScene = _scene;
                if (oldScene == value)
                {
                    return;
                }

                if (oldScene != null)
                {
                    RemoveComponentsRecursive(oldScene.Entities);
                    oldScene.Game = null;
                }

                _scene = value;

                if (_scene != null)
                {
                    _scene.Game = this;

                    _scene.Camera.SetSwapChain(SwapChain);

                    if (_scene.CameraController == null)
                    {
                        _scene.CameraController = new RtsCameraController(ContentManager);
                    }

                    AddComponentsRecursive(_scene.Entities);
                }
            }
        }

        private void RemoveComponentsRecursive(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                RemoveComponentsRecursive(entity);
            }
        }

        internal void RemoveComponentsRecursive(Entity entity)
        {
            OnEntityComponentsRemoved(entity.Components);

            RemoveComponentsRecursive(entity.GetChildren());
        }

        private void AddComponentsRecursive(IEnumerable<Entity> entities)
        {
            foreach (var entity in entities)
            {
                AddComponentsRecursive(entity);
            }
        }

        internal void AddComponentsRecursive(Entity entity)
        {
            OnEntityComponentsAdded(entity.Components);

            AddComponentsRecursive(entity.GetChildren());
        }

        // Needed by Data Viewer.
        public void SetCursor(HostCursor cursor)
        {
            _currentCursor = cursor;

            HostView?.SetCursor(cursor);
        }

        public void SetCursor(string cursorName)
        {
            if (!_cachedCursors.TryGetValue(cursorName, out var cursor))
            {
                var mouseCursor = ContentManager.IniDataContext.MouseCursors.Find(x => x.Name == cursorName);

                var cursorFileName = mouseCursor.Image;
                if (string.IsNullOrEmpty(Path.GetExtension(cursorFileName)))
                {
                    cursorFileName += ".ani";
                }

                var aniFilePath = Path.Combine(_fileSystem.RootDirectory, "Data", "Cursors", cursorFileName);

                _cachedCursors[cursorName] = cursor = AddDisposable(new HostCursor(aniFilePath));
            }

            SetCursor(cursor);
        }

        public void Tick()
        {
            _gameTimer.Update();

            var gameTime = _gameTimer.CurrentGameTime;

            UpdateTime = gameTime;

            Update(gameTime);
            Draw(gameTime);

            FrameCount += 1;
        }

        private void Update(GameTime gameTime)
        {
            foreach (var gameSystem in GameSystems)
                gameSystem.Update(gameTime);
        }

        private void Draw(GameTime gameTime)
        {
            foreach (var gameSystem in GameSystems)
                gameSystem.Draw(gameTime);
        }

        internal void OnEntityComponentsAdded(IEnumerable<EntityComponent> components)
        {
            foreach (var component in components)
            {
                component.Initialize();

                foreach (var system in GameSystems)
                    system.OnEntityComponentAdded(component);
            }
        }

        internal void OnEntityComponentsRemoved(IEnumerable<EntityComponent> components)
        {
            foreach (var component in components)
            {
                component.Uninitialize();

                foreach (var system in GameSystems)
                    system.OnEntityComponentRemoved(component);
            }
        }
    }
}
