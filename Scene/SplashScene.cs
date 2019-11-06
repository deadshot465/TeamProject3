using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class SplashScene :Nez.Scene
    {
        private Entity _splashScreenEntity;
        public Vector2 ViewportCenter { get; set; } = Vector2.Zero;

        public SplashScene()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            var splashScreen = Content.Load<Texture2D>("splash_screen");
            _splashScreenEntity = CreateEntity("splash-screen");
            _splashScreenEntity.AddComponent(new SpriteRenderer(splashScreen));
        }

        public override void OnStart()
        {
            base.OnStart();
        }

        public override void Unload()
        {
            base.Unload();
        }

        public override void Update()
        {
            base.Update();
            if (_splashScreenEntity.Position != ViewportCenter)
                _splashScreenEntity.Position = ViewportCenter;
        }
    }
}
