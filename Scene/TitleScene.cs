using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class TitleScene : Nez.Scene
    {
        private Entity _titleEntity;

        public Vector2 ViewportCenter { get; set; } = Vector2.Zero;

        public TitleScene() : base()
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();

            var title = Content.Load<Texture2D>("sample_titlescreen");
            _titleEntity = CreateEntity("sample-titlescreen");
            _titleEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _titleEntity.AddComponent(new SpriteRenderer(title));
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
        }
    }
}
