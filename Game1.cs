﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using System;
using TeamProject3.Scene;

namespace TeamProject3
{
    public class Game1 : Core
    {
        private TitleScene _titleScene;
        private SplashScene _splashScene;
        private bool _splashScreenShown = false;
        private bool _titleScreenShown = false;
        private Nez.Scene.SceneResolutionPolicy _sceneResolutionPolicy;
        private VirtualButton _startButton = new VirtualButton();
        private const int _screenWidth = 960;
        private const int _screenHeight = 540;

        public Game1() : base(_screenWidth, _screenHeight,
            windowTitle: "Team Project 2", isFullScreen: false)
        {
            _sceneResolutionPolicy = Nez.Scene.SceneResolutionPolicy.ShowAll;
            ExitOnEscapeKeypress = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            ParticleSystem.Initialize();

            // TODO: Add your initialization logic here
            _splashScene = new SplashScene();
            _splashScene.SetDesignResolution(_screenWidth, _screenHeight,
                _sceneResolutionPolicy);
            _splashScene.ViewportCenter = new Vector2(
                GraphicsDevice.Viewport.Width / 2,
                GraphicsDevice.Viewport.Height / 2);

            Scene = _splashScene;

            _startButton.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));

            Core.DebugRenderEnabled = true;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_splashScreenShown)
            {
                Core.Schedule(2.0f, timer =>
                {
                    _titleScene = new TitleScene();
                    _titleScene.ViewportCenter = new Vector2(
                        GraphicsDevice.Viewport.Width / 2,
                        GraphicsDevice.Viewport.Height / 2);
                    
                    var transition = new FadeTransition(() => _titleScene);
                    transition.FadeToColor = Color.Black;
                    transition.OnTransitionCompleted = () =>
                    {
                        _titleScreenShown = true;
                    };
                    StartSceneTransition(transition);
                });

                _splashScreenShown = true;
            }

            LoadGameScene();
        }

        private void LoadGameScene()
        {
            if (!_titleScreenShown) return;

            if (_startButton.IsPressed)
            {
                var transition = new WindTransition(()
                    => new GameScene(new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height)));
                transition.Duration = 5.0f;
                StartSceneTransition(transition);
                _startButton.Deregister();
                _titleScreenShown = false;
            }
        }
    }
}
