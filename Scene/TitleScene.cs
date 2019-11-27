using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class TitleScene : Nez.Scene
    {
        private Entity _titleEntity;
        private Entity _pressSpaceKeyEntity;
        private SpriteRenderer _pressSpaceKeySprite;
        private float _timer = 0.0f;

        public Vector2 ViewportCenter { get; set; } = Vector2.Zero;

        public TitleScene() : base()
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();

            var title = Content.Load<Texture2D>("title");
            _titleEntity = CreateEntity("title");
            _titleEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _titleEntity.AddComponent(new SpriteRenderer(title));

            var pressSpaceKey = Content.Load<Texture2D>("press_space_key");
            _pressSpaceKeyEntity = CreateEntity("press-space-key");
            _pressSpaceKeyEntity.Position =
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
            _pressSpaceKeySprite =
                _pressSpaceKeyEntity.AddComponent(new SpriteRenderer(pressSpaceKey));
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

            if (_timer > 2.0f)
            {
                _pressSpaceKeySprite.Enabled = !_pressSpaceKeySprite.Enabled;
                _timer = 0.0f;
            }

            _timer += Time.DeltaTime;
        }
    }
}
