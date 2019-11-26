using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Farseer;
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
        private const int _animationWidth = 300;
        private const int _animationHeight = 300;
        private bool _animationStarted = false;
        private bool _comboStarted = false;
        private bool _movementStarted = false;
        private bool _waitForNextInput = false;
        private bool _inputTimerStarted = false;
        private float _comboTimer = 0.0f;
        private float _inputTimer = 0.0f;
        private int _comboIndex = 0;

        private BoxCollider _collider;
        private FSRigidBody _rigidBody;
        public Fixture PlayerFixture { get; private set; }

        private float _speed = 0.0f;
        private bool _isJumping = false;
        private Vector2 _gravity;
        private Vector2 _velocity = Vector2.Zero;
        private float _finalVelocity;
        private const float _jumpHeight = 400.0f;
        private const float _scale = 2.0f;

        public float Width => _spriteAnimator.Width;
        public float Height => _spriteAnimator.Height;

        public Fixture GroundFixture { get; set; }
        public Fixture BossFixture { get; set; }

        private enum Input
        {
            Attack, Flee, Jump,
            //AttackTwo, AttackThree
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

        private string[] _attackStrings = new[]
        {
            "attack_1", "attack_2", "attack_3"
        };

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

            var texture = Entity.Scene.Content.Load<Texture2D>("player");
            var spriteAtlas = Sprite
                .SpritesFromAtlas(texture, _animationWidth, _animationHeight);
            _spriteAnimator = Entity.AddComponent<SpriteAnimator>();
            _spriteAnimator.AddAnimation("idle",
                new SpriteAnimation(spriteAtlas.ToArray()[0..10], _animationFramerate * 0.5f));
            _spriteAnimator.AddAnimation("running",
                new SpriteAnimation(spriteAtlas.ToArray()[10..20], _animationFramerate * 0.5f));
            _spriteAnimator.AddAnimation("jump",
                new SpriteAnimation(spriteAtlas.ToArray()[20..25], _animationFramerate * 0.75f));
            _spriteAnimator.AddAnimation("flee",
                new SpriteAnimation(spriteAtlas.ToArray()[30..35], _animationFramerate * 0.45f));
            _spriteAnimator.AddAnimation("attack_1",
                new SpriteAnimation(spriteAtlas.ToArray()[40..45], _animationFramerate * 0.75f));
            _spriteAnimator.AddAnimation("attack_2",
                new SpriteAnimation(spriteAtlas.ToArray()[50..55], _animationFramerate * 0.75f));
            _spriteAnimator.AddAnimation("attack_3",
                new SpriteAnimation(spriteAtlas.ToArray()[60..65], _animationFramerate * 0.75f));

            //_collider = Entity.GetComponent<BoxCollider>();

            _mover = Entity.AddComponent<Mover>();

            Entity.Position = _startPosition;

            (_rigidBody, PlayerFixture) = Helper.CreateFarseerFixture(ref Entity,
                BodyType.Dynamic,
                -1.0f, _animationWidth / 4, _animationHeight / 2);
            _rigidBody.SetInertia(0.0f).SetFixedRotation(true);

            _gravity = FSConvert.ToDisplayUnits(_rigidBody.Body.World.Gravity);
            _finalVelocity = -Mathf.Sqrt(2.0f * _jumpHeight * _gravity.Y);

            SetupKeyboardInputs();

            _spriteAnimator.Play("idle", SpriteAnimator.LoopMode.Loop);
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

            if (!_animationStarted && !_comboStarted)
            {
                if (moveDirection.X != 0 && _spriteAnimator.CurrentAnimationName != "running")
                    _spriteAnimator.Play("running", SpriteAnimator.LoopMode.Loop);
                else if (moveDirection.X == 0 &&
                    _spriteAnimator.CurrentAnimationName != "idle")
                    _spriteAnimator.Play("idle", SpriteAnimator.LoopMode.Loop);
            }

            if (moveDirection.X < 0)
            {
                _velocity.X = -_speed;
                _spriteAnimator.FlipX = true;
            }
            else if (moveDirection.X > 0)
            {
                _velocity.X = _speed;
                _spriteAnimator.FlipX = false;
            }
            else
            {
                _velocity.X = 0;
            }

            if (_inputKeyMappings[Input.Jump].IsPressed && !_isJumping)
            {
                _velocity.Y = _finalVelocity;
                _isJumping = true;
            }

            if (!_isJumping) _velocity.Y = 0;
            else _velocity.Y += _gravity.Y * Time.DeltaTime * _scale;

            var movement = _velocity * Time.DeltaTime;
            var res = _mover.CalculateMovement(ref movement, out var result);
            _subpixelVector.Update(ref movement);
            _mover.ApplyMovement(movement);

            FSCollisions.CollideFixtures(PlayerFixture, GroundFixture, out var fsResult);

            if (fsResult.Normal.Y < 0)
            {
                _isJumping = false;
            }

            if (_inputTimerStarted)
            {
                if (_inputTimer > 0.2f)
                    _inputTimerStarted = false;
                _inputTimer += Time.DeltaTime;
            }
            else
            {
                HandleKeyboardInputs();
            }
            _rigidBody.SetIsAwake(true).SetIsSleepingAllowed(false);
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

        private void HandleKeyboardInputs()
        {
            if (_waitForNextInput)
            {
                if (_comboTimer > 0.5f)
                {
                    _comboTimer = 0.0f;
                    _spriteAnimator.OnAnimationCompletedEvent -= OnAttackFinished;
                    _comboIndex = 0;
                    _animationStarted = false;
                    _comboStarted = false;
                    _waitForNextInput = false;
                    _movementStarted = false;
                }
                _comboTimer += Time.DeltaTime;
            }

            if (!_movementStarted)
            {
                if (_inputKeyMappings[Input.Attack].IsReleased)
                {
                    if (!_comboStarted)
                    {
                        _comboStarted = true;
                        _animationStarted = true;
                        _spriteAnimator.Play(_attackStrings[_comboIndex], SpriteAnimator.LoopMode.ClampForever);
                        _spriteAnimator.OnAnimationCompletedEvent += OnAttackFinished;
                    }
                    else
                    {
                        if (_waitForNextInput)
                        {
                            _waitForNextInput = false;
                            _comboTimer = 0.0f;
                            _spriteAnimator.Play(_attackStrings[_comboIndex], SpriteAnimator.LoopMode.ClampForever);
                        }
                    }
                    _inputTimerStarted = true;
                }
                else if (_inputKeyMappings[Input.Jump].IsReleased)
                {
                    _spriteAnimator.Play("jump", SpriteAnimator.LoopMode.Once);
                    _animationStarted = true;
                    _movementStarted = true;
                    if (!_comboStarted)
                        _spriteAnimator.OnAnimationCompletedEvent += OnAnimationFinished;
                    _inputTimerStarted = true;
                }
                else if (_inputKeyMappings[Input.Flee].IsReleased)
                {
                    _spriteAnimator.Play("flee", SpriteAnimator.LoopMode.Once);
                    _animationStarted = true;
                    _movementStarted = true;
                    var tween = Entity.TweenPositionTo(
                        new Vector2(400 * (_spriteAnimator.FlipX ? -1 : 1), 0), 0.25f);
                    tween.SetFrom(Entity.Position)
                        .SetIsRelative()
                        .SetEaseType(Nez.Tweens.EaseType.Linear)
                        .Start();
                    if (!_comboStarted)
                        _spriteAnimator.OnAnimationCompletedEvent += OnAnimationFinished;
                    _inputTimerStarted = true;
                }
            }
        }

        private void OnAttackFinished(string animationName)
        {
            _waitForNextInput = true;
            _comboTimer = 0.0f;
            _comboIndex = (_comboIndex + 1) % 3; ;
        }

        private void OnAnimationFinished(string animationName)
        {
            _animationStarted = false;
            _movementStarted = false;
            _spriteAnimator.OnAnimationCompletedEvent -= OnAnimationFinished;
        }
    }
}
