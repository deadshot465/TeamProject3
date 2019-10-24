using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using TeamProject3.Scene;

namespace TeamProject3
{
    public class Game1 : Core
    {
        private TitleScene _titleScene;
        private bool _transitioned = false;
        public Game1() : base(960, 540, windowTitle: "Team Project 2")
        {
            
        }

        protected override void Initialize()
        {
            base.Initialize();

            // TODO: Add your initialization logic here
            _titleScene = new TitleScene();

            _titleScene.ViewportCenter = new Vector2(GraphicsDevice.Viewport.Width / 2,
                GraphicsDevice.Viewport.Height / 2);

            Scene = _titleScene;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_titleScene.IsBrickLoaded && !_transitioned)
            {
                var transition = new TextureWipeTransition();
                StartSceneTransition(transition);
                _transitioned = true;
            }
        }
    }
}
