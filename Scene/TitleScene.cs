using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TeamProject3.Scene
{
    public class TitleScene : Nez.Scene
    {
        private List<Vector2> _brickPositions = new List<Vector2>();
        private Entity _titleAppearEntity;
        private Entity _titleDisappearEntity;
        private List<Entity> _brickEntities = new List<Entity>();
        private int _enabledCount = 0;
        private float _elapsedTime = 0.0f;

        public bool IsBrickLoaded { get; private set; } = false;
        public Vector2 ViewportCenter { get; set; } = Vector2.Zero;

        public TitleScene() : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            var titleAppear = Content.Load<Texture2D>("sample_title");
            _titleAppearEntity = CreateEntity("sample-title-appear");
            _titleAppearEntity.AddComponent(new SpriteRenderer(titleAppear));
            _titleAppearEntity.SetEnabled(false);

            var titleDisappear = Content.Load<Texture2D>("sample_title_disappear");
            _titleDisappearEntity = CreateEntity("sample-title-disappear");
            _titleDisappearEntity.AddComponent(new SpriteRenderer(titleDisappear));
            _titleDisappearEntity.SetEnabled(false);

            var brick = Content.Load<Texture2D>("sample_brick");

            for (var i = 513; i >= 0; i -= 27)
            {
                for (var j = 0; j < 960; j += 48)
                {
                    _brickPositions.Add(new Vector2(j + 24.0f, i + 13.5f));
                }
            }

            _brickPositions.Shuffle();

            for (var i = 0; i < _brickPositions.Count; i++)
            {
                _brickEntities.Add(CreateEntity($"sample-brick{i}"));
                _brickEntities[i].AddComponent(new SpriteRenderer(brick));
                _brickEntities[i].Position = _brickPositions[i];
                _brickEntities[i].SetEnabled(false);
            }
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
            _elapsedTime += Time.DeltaTime;
            _titleAppearEntity.Position = ViewportCenter;
            _titleDisappearEntity.Position = ViewportCenter;

            if (_elapsedTime > 0.01f && !IsBrickLoaded)
            {
                _brickEntities[_enabledCount].SetEnabled(true);
                _enabledCount++;
                _elapsedTime = 0.0f;

                if (_brickEntities.All(entity => entity.Enabled))
                {
                    IsBrickLoaded = true;
                }
            }

            if (IsBrickLoaded)
            {
                _elapsedTime += Time.DeltaTime;
                bool toggle = (int)_elapsedTime % 2 == 0;
                _titleAppearEntity.SetEnabled(toggle ? true : false);
                _titleDisappearEntity.SetEnabled(toggle ? false : true);
            }
        }

        public void DisableAllBricks()
        {
            _brickEntities.ForEach(entity => entity.SetEnabled(false));
        }
    }
}
