using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamProject3
{
    public class Player : Component, IUpdatable
    {
        private SubpixelVector2 _subpixelVector = new SubpixelVector2();
        private Vector2 _startPosition = Vector2.Zero;
        private SpriteAnimator _spriteAnimator;
        private Mover _mover;
        private float _animationFramerate = 0.0f;
        private BoxCollider _collider;
        private float _speed = 0.0f;
        private bool _isJumping = false;
        private const float _gravity = 1000f;

        private enum Input
        {
            Attack, Flee, Jump
        }

        private enum Movement
        {
            Move
        }

        private Dictionary<Input, VirtualButton> _inputKeyMappings =
            new Dictionary<Input, VirtualButton>();

        private Dictionary<Input, Tuple<Keys, Buttons>> _inputKeys =
            new Dictionary<Input, Tuple<Keys, Buttons>>();

        private Dictionary<Movement, VirtualIntegerAxis> _inputMovementMappings =
            new Dictionary<Movement, VirtualIntegerAxis>();

        private Dictionary<Movement, Tuple<Keys, Keys>> _inputMovements =
            new Dictionary<Movement, Tuple<Keys, Keys>>();

        public Player(float speed, Vector2 startPosition, float animationFramerate)
        {
            _speed = speed;
            _startPosition = startPosition;
            _animationFramerate = animationFramerate;

            _inputKeys.Add(Input.Attack, new Tuple<Keys, Buttons>(Keys.Z, Buttons.A));
            _inputKeys.Add(Input.Flee, new Tuple<Keys, Buttons>(Keys.C, Buttons.X));
            _inputKeys.Add(Input.Jump, new Tuple<Keys, Buttons>(Keys.X, Buttons.B));

            _inputMovements.Add(Movement.Move, new Tuple<Keys, Keys>(Keys.Left, Keys.Right));
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var texture = Entity.Scene.Content.Load<Texture2D>("player_revised");
            var spriteAtlas = Sprite.SpritesFromAtlas(texture, 128, 128);
            _spriteAnimator = Entity.AddComponent<SpriteAnimator>();
            _spriteAnimator.AddAnimation("running",
                new SpriteAnimation(spriteAtlas.ToArray()[0..3], _animationFramerate));
            _spriteAnimator.AddAnimation("guard",
                new SpriteAnimation(spriteAtlas.ToArray()[3..6], _animationFramerate));
            _spriteAnimator.AddAnimation("parry",
                new SpriteAnimation(spriteAtlas.ToArray()[6..9], _animationFramerate));
            _spriteAnimator.AddAnimation("damage",
                new SpriteAnimation(spriteAtlas.ToArray()[9..12], _animationFramerate));
            _spriteAnimator.AddAnimation("prepare",
                new SpriteAnimation(spriteAtlas.ToArray()[12..15], _animationFramerate));

            _collider = Entity.AddComponent<BoxCollider>();
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, 1);

            _mover = Entity.AddComponent<Mover>();

            Entity.Position = _startPosition;

            _spriteAnimator.OnAnimationCompletedEvent += animationName =>
            {
                _spriteAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
            };

            SetupKeyboardInputs();

            _spriteAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();

            _inputKeyMappings.ToList().ForEach(mapping =>
            {
                mapping.Value.Deregister();
            });
        }

        void IUpdatable.Update()
        {
            var moveDirection = new Vector2(_inputMovementMappings[Movement.Move].Value, 0);
            var velocity = Vector2.Zero;

            if (moveDirection.X < 0)
            {
                velocity.X = -_speed;
                _spriteAnimator.FlipX = true;
            }
            else if (moveDirection.X > 0)
            {
                velocity.X = _speed;
                _spriteAnimator.FlipX = false;
            }
            else
            {
                velocity.X = 0;
            }

            if (_inputKeyMappings[Input.Jump].IsPressed && !_isJumping)
            {
                velocity.Y = -Mathf.Sqrt(2f * 100 * _gravity);
                _isJumping = true;
            }

            velocity.Y += _gravity * Time.DeltaTime;

            var movement = velocity * Time.DeltaTime;

            var res = _mover.CalculateMovement(ref movement, out var result);
            _subpixelVector.Update(ref movement);
            _mover.ApplyMovement(movement);

            Console.WriteLine(_collider.CollidesWithAny(out var collisionResult));
        }

        private void SetupKeyboardInputs()
        {
            _inputKeyMappings.Add(Input.Attack, new VirtualButton());
            _inputKeyMappings.Add(Input.Flee, new VirtualButton());
            _inputKeyMappings.Add(Input.Jump, new VirtualButton());
            _inputMovementMappings.Add(Movement.Move, new VirtualIntegerAxis());

            AddInputButton(Input.Attack);
            AddInputButton(Input.Jump);
            AddInputButton(Input.Flee);

            _inputMovementMappings[Movement.Move].Nodes
                .Add(new VirtualAxis.GamePadDpadLeftRight());
            _inputMovementMappings[Movement.Move].Nodes
                .Add(new VirtualAxis.GamePadLeftStickX());

            AddInputAxis(Movement.Move);    
        }

        private void AddInputButton(Input input)
        {
            _inputKeyMappings[input].Nodes
                .Add(new VirtualButton.KeyboardKey(_inputKeys[input].Item1));
            _inputKeyMappings[input].Nodes
                .Add(new VirtualButton.GamePadButton(0, _inputKeys[input].Item2));
        }

        private void AddInputAxis(Movement movement)
        {
            _inputMovementMappings[movement].Nodes
                .Add(new VirtualAxis
                .KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer,
                _inputMovements[movement].Item1,
                _inputMovements[movement].Item2));
        }

        private void HandleInput()
        {
            
        }
    }
}
