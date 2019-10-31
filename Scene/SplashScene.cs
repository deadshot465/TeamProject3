using System;
using System.Collections.Generic;
using System.Text;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
            _splashScreenEntity.Position = ViewportCenter;
        }
    }
}
