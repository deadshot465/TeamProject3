using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nez;
using Nez.Particles;
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
        private const float _animationFramerate = 20.0f;

        private enum FireInput
        {
            Z, X, C, V
        }
        private Dictionary<FireInput, VirtualButton> _playerAttackButtons
            = new Dictionary<FireInput, VirtualButton>();

        private bool _guarding = false;

        public GameScene()
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            SetupCharacterAnimation();

            _playerGuardProjectile = CreateEntity("guard-matrix");
            _playerGuardProjectile.SetParent(_playerCharacterEntity);
            _playerGuardProjectile.AddComponent(
                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Electrons));
            _playerGuardProjectile.SetEnabled(false);

            _playerAttackButtons.Add(FireInput.Z, new VirtualButton());
            _playerAttackButtons.Add(FireInput.X, new VirtualButton());
            _playerAttackButtons.Add(FireInput.C, new VirtualButton());
            _playerAttackButtons.Add(FireInput.V, new VirtualButton());

            _playerAttackButtons[FireInput.Z].AddKeyboardKey(Keys.Z);
            _playerAttackButtons[FireInput.X].AddKeyboardKey(Keys.X);
            _playerAttackButtons[FireInput.C].AddKeyboardKey(Keys.C);
            _playerAttackButtons[FireInput.V].AddKeyboardKey(Keys.V);
        }

        private void SetupCharacterAnimation()
        {
            var playerTexture = Content.Load<Texture2D>("player_revised");
            var playerAtlas = Sprite.SpritesFromAtlas(playerTexture, 128, 128);
            _playerCharacterEntity = CreateEntity("sample-player");
            _playerAnimator = _playerCharacterEntity.AddComponent(new SpriteAnimator());
            _playerAnimator.AddAnimation("running",
                new SpriteAnimation(playerAtlas.ToArray()[0..3], _animationFramerate));
            _playerAnimator.AddAnimation("guard",
                new SpriteAnimation(playerAtlas.ToArray()[3..6], _animationFramerate));
            _playerAnimator.AddAnimation("parry",
                new SpriteAnimation(playerAtlas.ToArray()[6..9], _animationFramerate));
            _playerAnimator.AddAnimation("damage",
                new SpriteAnimation(playerAtlas.ToArray()[9..12], _animationFramerate));
            _playerAnimator.AddAnimation("prepare",
                new SpriteAnimation(playerAtlas.ToArray()[12..15], _animationFramerate));

            _playerCharacterEntity.Position = new Vector2(250, 250);

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

            var pressedButtons = _playerAttackButtons
                .Values
                .Where(button => button.IsPressed);

            if (pressedButtons.ToList().Count > 0)
            {
                foreach (var button in pressedButtons)
                {
                    var entity = CreateEntity("projectile");
                    entity.Position = _playerCharacterEntity.Position;
                    entity.AddComponent(SelectEmitterType(button));
                    entity.AddComponent(new ProjectileMover());
                    entity.AddComponent(new ProjectileController(new Vector2(175.0f, 0.0f)));

                    var collider = entity.AddComponent<CircleCollider>();
                    Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
                    Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);

                    //_playerProjectiles.Add(entity);
                    _playerAnimator.Play("parry", SpriteAnimator.LoopMode.Once);
                }
            }

            if (_playerAttackButtons[FireInput.V].IsDown && !_guarding)
            {
                _playerGuardProjectile.SetEnabled(true);
                _playerAnimator.Play("guard", SpriteAnimator.LoopMode.Loop);
                _guarding = true;
            }
            else if (_playerAttackButtons[FireInput.V].IsReleased && _guarding)
            {
                _playerGuardProjectile.SetEnabled(false);
                _playerAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
                _guarding = false;
            }

            _playerProjectiles.Where(entity => entity.Position.X > 1200).ToList()
                .ForEach(entity =>
                {
                    entity.RemoveAllComponents();
                    //entity.Destroy();
                });
        }

        private ParticleEmitter SelectEmitterType(VirtualButton button) =>
            button switch
            {
                var x when x == _playerAttackButtons[FireInput.Z] =>
                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Fire),
                var x when x == _playerAttackButtons[FireInput.X] =>
                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Comet),
                var x when x == _playerAttackButtons[FireInput.C] =>
                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.BlueFlame),
                _ => ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.WinnerStars)
            };
    }
}
