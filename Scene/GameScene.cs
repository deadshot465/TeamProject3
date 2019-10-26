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
    public class GameScene : Nez.Scene
    {
        private Entity _playerCharacterEntity;
        private List<Entity> _playerProjectiles = new List<Entity>();
        private Entity _playerGuardProjectile;
        private SpriteAnimator _playerAnimator;
        private VirtualButton _playerAttackButtonZ = new VirtualButton();
        private VirtualButton _playerAttackButtonX = new VirtualButton();
        private VirtualButton _playerAttackButtonC = new VirtualButton();
        private VirtualButton _playerAttackButtonV = new VirtualButton();

        private bool _guarding = false;

        public GameScene()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            var playerTexture = Content.Load<Texture2D>("player_revised");
            var playerAtlas = Sprite.SpritesFromAtlas(playerTexture, 128, 128);
            _playerCharacterEntity = CreateEntity("sample-player");
            _playerAnimator = _playerCharacterEntity.AddComponent(new SpriteAnimator());
            _playerAnimator.AddAnimation("running",
                new SpriteAnimation(playerAtlas.ToArray()[0..3], 30.0f));
            _playerAnimator.AddAnimation("guard",
                new SpriteAnimation(playerAtlas.ToArray()[3..6], 30.0f));
            _playerAnimator.AddAnimation("parry",
                new SpriteAnimation(playerAtlas.ToArray()[6..9], 30.0f));
            _playerAnimator.AddAnimation("damage",
                new SpriteAnimation(playerAtlas.ToArray()[9..12], 30.0f));
            _playerAnimator.AddAnimation("prepare",
                new SpriteAnimation(playerAtlas.ToArray()[12..15], 30.0f));

            _playerCharacterEntity.Position = new Vector2(250, 250);

            _playerGuardProjectile = CreateEntity("guard-matrix");
            _playerGuardProjectile.SetParent(_playerCharacterEntity);
            _playerGuardProjectile.AddComponent(
                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Electrons));
            _playerGuardProjectile.SetEnabled(false);

            _playerAttackButtonZ.AddKeyboardKey(Keys.Z);
            _playerAttackButtonX.AddKeyboardKey(Keys.X);
            _playerAttackButtonC.AddKeyboardKey(Keys.C);
            _playerAttackButtonV.AddKeyboardKey(Keys.V);

            _playerAnimator.OnAnimationCompletedEvent += animationName =>
            {
                _playerAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
            };
        }

        public override void OnStart()
        {
            base.OnStart();

            _playerAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
        }

        public override void Unload()
        {
            base.Unload();
        }

        public override void Update()
        {
            base.Update();
            ClearColor = Color.Black;

            if (_playerAttackButtonZ.IsPressed)
            {
                var entity = CreateEntity("projectile");
                entity.SetParent(_playerCharacterEntity);
                entity.AddComponent(ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Fire));
                _playerProjectiles.Add(entity);
                _playerAnimator.Play("parry", SpriteAnimator.LoopMode.Once);
            }

            if (_playerAttackButtonX.IsPressed)
            {
                var entity = CreateEntity("projectile");
                entity.SetParent(_playerCharacterEntity);
                entity.AddComponent(ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Comet));
                _playerProjectiles.Add(entity);
                _playerAnimator.Play("parry", SpriteAnimator.LoopMode.Once);
            }

            if (_playerAttackButtonC.IsPressed)
            {
                var entity = CreateEntity("projectile");
                entity.SetParent(_playerCharacterEntity);
                entity.AddComponent(ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.BlueFlame));
                _playerProjectiles.Add(entity);
                _playerAnimator.Play("parry", SpriteAnimator.LoopMode.Once);
            }

            if (_playerAttackButtonV.IsDown && !_guarding)
            {
                _playerGuardProjectile.SetEnabled(true);
                _playerAnimator.Play("guard", SpriteAnimator.LoopMode.Loop);
                _guarding = true;
            }
            else if (_playerAttackButtonV.IsReleased && _guarding)
            {
                _playerGuardProjectile.SetEnabled(false);
                _playerAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
                _guarding = false;
            }

            _playerProjectiles.ForEach(entity =>
            {
                entity.Position = new Vector2(entity.Position.X + 10, entity.Position.Y);
            });

            _playerProjectiles.Where(entity => entity.Position.X > 1200).ToList()
                .ForEach(entity =>
                {
                    entity.RemoveAllComponents();
                });

            _playerProjectiles.RemoveAll(entity => entity.Position.X > 1200);
        }
    }
}
