using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using System;
using TeamProject3.Scene;

namespace TeamProject3
{
    public class Game1 : Core
    {
        private TitleScene _titleScene;
        private bool _transitioned = false;
        private Nez.Scene.SceneResolutionPolicy _sceneResolutionPolicy;

        public Game1() : base(960, 540, windowTitle: "Team Project 2", isFullScreen: false)
        {
            _sceneResolutionPolicy = Nez.Scene.SceneResolutionPolicy.ShowAll;
            ExitOnEscapeKeypress = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            ParticleSystem.Initialize();

            // TODO: Add your initialization logic here
            _titleScene = new TitleScene();
            _titleScene.SetDesignResolution(960, 540, _sceneResolutionPolicy);

            _titleScene.ViewportCenter = new Vector2(GraphicsDevice.Viewport.Width / 2,
                GraphicsDevice.Viewport.Height / 2);

            Scene = _titleScene;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_titleScene.IsBrickLoaded && !_transitioned)
            {
                var transition = new TextureWipeTransition(() => new GameScene());
                StartSceneTransition(transition);
                transition.OnScreenObscured = () =>
                {
                    _titleScene.DisableAllBricks();
                };
                _transitioned = true;
            }
        }
    }
}
