using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Farseer;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tweens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamProject3
{
    public class BossSettings
    {
        public readonly Vector2 Speed = new Vector2(150);
        public readonly float AnimationFramerate = 20.0f;
        public readonly float ProjectileVelocity = 350.0f;
        public readonly string AnimationFileName = "boss1";
        public readonly int AnimationFrameWidth = 500;
        public readonly int AnimationFrameHeight = 500;
                
        public readonly int PunchLoops = 1;
        public readonly float PunchDuration = 1f;
                
        public readonly EaseType DashEaseType = EaseType.BackIn;
        public readonly float DashDuration = 1.0f;
                
        public readonly Vector2 FirstStepJumpOffset = new Vector2(175, -200);
        public readonly EaseType FirstStepJumpEaseType = EaseType.CircIn;
        public readonly float FirstStepJumpDuration = 1.5f;
        public readonly Vector2 SecondStepJumpOffset = new Vector2(175, 200);
        public readonly EaseType SecondStepJumpEaseType = EaseType.BackIn;
        public readonly float SecondStepJumpDuration = 1.0f;

        public BossSettings()
        {

        }

        public static void ExportBossSettings(string fileName = "boss1_settings")
        {
            var settings = new BossSettings();
            JsonExporter.WriteToJson(fileName, settings, true);
        }

        public static BossSettings ImportBossSettings(string fileName = "boss1_settings")
        {
            return JsonExporter.ReadFromJson(fileName, true);
        }
    }

    public class Boss : Component, ITriggerListener, IUpdatable
    {
        private SubpixelVector2 _subpixelVector = new SubpixelVector2();
        private Vector2 _startPosition = Vector2.Zero;
        private SpriteAnimator _spriteAnimator;
        private Mover _mover;
        private float _animationFramerate = 0.0f;
        private float _elapsedTime = 0.0f;
        private float _nextAttackDuration = 0.0f;
        private bool _timerStarted = false;
        private bool _attackStarted = false;
        private int _currentAttack = 0;
        private int _currentAttackTick = 0;
        private readonly float _projectileVelocity;
        private float _bossDirection = 0;

        private FSRigidBody _rigidBody;
        private FSRigidBody _battleRigidBody;
        public Fixture BossFixture { get; private set; }
        public Fixture BattleFixture { get; private set; }

        public float Width => _spriteAnimator.Width;
        public float Height => _spriteAnimator.Height;
        public BoxCollider Collider { get; private set; }

        private BossSettings _bossSettings;

        private enum BossStage
        {
            One, Two, Three
        }

        private enum BossAttacks
        {
            ChainAttack, RangeAttack, Dash,
            JumpAttack, FullscreenAttack, ShieldAttack
        }

        private BossStage _currentStage = BossStage.One;

        private delegate void AttackHandler();
        private List<AttackHandler> _attackHandlers = new List<AttackHandler>();

        private delegate void PhaseHandler(int patterns);
        private List<PhaseHandler> _bossPhaseHandlers = new List<PhaseHandler>();

        private List<Action> _movePhaseHandlers
            = new List<Action>();

        Action<string> _attackFinishAction;

        public Vector2 Speed { get; private set; }
        public Fixture GroundFixture { get; set; }
        public Fixture PlayerFixture { get; set; }

        public Boss(Vector2 startPosition, BossSettings bossSettings)
        {
            _bossSettings = bossSettings;

            Speed = _bossSettings.Speed;
            _animationFramerate = _bossSettings.AnimationFramerate;
            _startPosition = startPosition;
            _projectileVelocity = _bossSettings.ProjectileVelocity;

            _attackHandlers.Add(ChainAttack);
            _attackHandlers.Add(RangeAttack);
            _attackHandlers.Add(Dash);
            _attackHandlers.Add(JumpAttack);
            _attackHandlers.Add(FullscreenAttack);
            _attackHandlers.Add(ShieldAttack);

            _bossPhaseHandlers.Add(StageAttack);
            _bossPhaseHandlers.Add(StageAttack);
            _bossPhaseHandlers.Add(StageAttack);

            Action[] actions = new Action[]
            {
                () => MoveToNextPhase(true, (int)BossAttacks.ChainAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.RangeAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.Dash)
            };

            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[1]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[2]);

            MoveToNextStage(BossStage.Two);

            _attackFinishAction = animationName => MoveToNextPhase();
        }

        void ITriggerListener.OnTriggerEnter(Collider other, Collider local)
        {
            
        }

        void ITriggerListener.OnTriggerExit(Collider other, Collider local)
        {
            
        }

        void IUpdatable.Update()
        {
            var playEntity = Entity.Scene.FindEntity("player-entity");
            _bossDirection = (playEntity.Position.X < Entity.Position.X) ? -1.0f : 1.0f;
            _spriteAnimator.FlipX = (_bossDirection < 0) ? false : true;

            if (!_timerStarted)
            {
                _nextAttackDuration = Nez.Random.Range(2.5f, 3.5f);
                _timerStarted = true;
            }

            if (_elapsedTime > _nextAttackDuration && !_attackStarted)
            {
                _attackStarted = true;

                _bossPhaseHandlers[(int)_currentStage].Invoke(_currentStage switch
                {
                    BossStage.One => 6,
                    BossStage.Two => 16,
                    BossStage.Three => 37,
                    _ => 0
                });
            }

            _elapsedTime += Time.DeltaTime;
            _rigidBody.SetIsAwake(true).SetIsSleepingAllowed(false);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            var texture = Entity.Scene.Content.Load<Texture2D>(_bossSettings.AnimationFileName);
            var spriteAtlas = Sprite
                .SpritesFromAtlas(texture, _bossSettings.AnimationFrameWidth, _bossSettings.AnimationFrameHeight);
            _spriteAnimator = Entity.AddComponent<SpriteAnimator>();

            _spriteAnimator.AddAnimation("Idle", new SpriteAnimation(
                spriteAtlas.ToArray()[0..10], _animationFramerate));
            _spriteAnimator.AddAnimation("Move", new SpriteAnimation(
                spriteAtlas.ToArray()[10..20], _animationFramerate));
            _spriteAnimator.AddAnimation("RaiseSword", new SpriteAnimation(
                spriteAtlas.ToArray()[20..30], _animationFramerate));
            _spriteAnimator.AddAnimation("PutdownSword", new SpriteAnimation(
                spriteAtlas.ToArray()[30..40], _animationFramerate));
            _spriteAnimator.AddAnimation("Attack", new SpriteAnimation(
                spriteAtlas.ToArray()[20..40], _animationFramerate));

            _mover = Entity.AddComponent<Mover>();
            //Collider = Entity.AddComponent(
            //    new BoxCollider(Width / 2, Height));

            Entity.Position = _startPosition;

            (_rigidBody, BossFixture) = Helper.CreateFarseerFixture(ref Entity,
                BodyType.Dynamic, -1.0f,
                Width / 4, Height / 2);
            _rigidBody.SetInertia(0.0f).SetFixedRotation(true);
            BossFixture.CollisionGroup = -1;

            _spriteAnimator.Play("Idle", SpriteAnimator.LoopMode.Loop);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();
        }

        private void ChainAttack()
        {
            _spriteAnimator.Play("RaiseSword", SpriteAnimator.LoopMode.ClampForever);

            Core.Schedule(0.25f, timer =>
            {
                _spriteAnimator.Play("PutdownSword", SpriteAnimator.LoopMode.ClampForever);
                _spriteAnimator.OnAnimationCompletedEvent += _attackFinishAction;
            });
        }

        private void RangeAttack()
        {
            _spriteAnimator.Play("RaiseSword", SpriteAnimator.LoopMode.ClampForever);

            var entity = Entity.Scene.CreateEntity("projectile");
            entity.Position = new Vector2(Entity.Position.X + (150 * _bossDirection), Entity.Position.Y - 200);
            var emitter = entity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.Charge));
            emitter.SetRenderLayer(-5);
            entity.AddComponent<ProjectileMover>();
            //entity.AddComponent(new ProjectileController(Vector2.Zero));

            Core.Schedule(2f, timer => {
                
                entity.Destroy();

                _spriteAnimator.Play("PutdownSword", SpriteAnimator.LoopMode.ClampForever);
                _spriteAnimator.OnAnimationCompletedEvent += _attackFinishAction;

                entity = Entity.Scene.CreateEntity("projectile");
                entity.Position = new Vector2(Entity.Position.X + (100 * _bossDirection), Entity.Position.Y + 50);
                var _emitter = entity.AddComponent(ParticleSystem
                    .CreateEmitter(ParticleSystem.ParticleType.Cremation));
                _emitter.SetRenderLayer(-5);
                entity.AddComponent<ProjectileMover>();

                //var _collider = entity.AddComponent<CircleCollider>();
                //Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, 0);
                //Flags.SetFlagExclusive(ref _collider.PhysicsLayer, 1);

                var (bulletRigidBody, bulletFixture) = Helper.CreateFarseerFixture(ref entity, BodyType.Kinematic, 0.0f, 75.0f, 75.0f);
                bulletRigidBody.SetIsBullet(true)
                .SetIgnoreGravity(true)
                .SetLinearVelocity(new Vector2(3.0f * _bossDirection, 0.0f));
                bulletFixture.IgnoreCollisionWith(BossFixture);

                entity.AddComponent(new ProjectileController(
                    new Vector2(_projectileVelocity * _bossDirection, 0),
                    bulletRigidBody, bulletFixture));
            });
        }

        private void Dash()
        {
            var tween = Entity.TweenPositionTo(new Vector2(350 * _bossDirection, 0), _bossSettings.DashDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(_bossSettings.DashEaseType)
                .SetCompletionHandler(_tween =>
                {
                    MoveToNextPhase();
                });

            _spriteAnimator.Play("Move", SpriteAnimator.LoopMode.Loop);

            tween.Start();
        }

        private void JumpAttack()
        {
            var firstOffset = _bossSettings.FirstStepJumpOffset;
            firstOffset.X *= _bossDirection;
            var secondOffset = _bossSettings.SecondStepJumpOffset;
            secondOffset.X *= _bossDirection;

            var tween = Entity
                .TweenPositionTo(firstOffset, _bossSettings.FirstStepJumpDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(_bossSettings.FirstStepJumpEaseType)
                .SetCompletionHandler(_tween =>
                {
                    var secondTween = Entity
                    .TweenPositionTo(secondOffset, _bossSettings.SecondStepJumpDuration)
                    .SetFrom(Entity.Position)
                    .SetIsRelative()
                    .SetEaseType(_bossSettings.SecondStepJumpEaseType)
                    .SetCompletionHandler(_secondTween => MoveToNextPhase());

                    secondTween.Start();
                });

            _spriteAnimator.Play("Move", SpriteAnimator.LoopMode.Loop);

            tween.Start();
        }

        private void FullscreenAttack()
        {
            _spriteAnimator.Play("RaiseSword", SpriteAnimator.LoopMode.ClampForever);

            var leftEntity = Entity.Scene.CreateEntity("projectile");
            leftEntity.Position = Entity.Position;
            leftEntity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.BlueFlame));
            leftEntity.AddComponent<ProjectileMover>();
            //leftEntity.AddComponent(new ProjectileController(new Vector2(_projectileVelocity, 0)));

            var collider = leftEntity.AddComponent<CircleCollider>();
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);

            var rightEntity = leftEntity.Clone();
            rightEntity.Position = Entity.Position;
            rightEntity.GetComponent<ProjectileController>().Velocity *= -1;
            rightEntity.AttachToScene(Entity.Scene);

            Core.Schedule(2.0f, timer => MoveToNextPhase());
        }

        private void ShieldAttack()
        {
            var tween = Entity
                .TweenPositionTo(new Vector2(50 * _bossDirection, 0), 0.5f)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(EaseType.ExpoIn)
                .SetCompletionHandler(_tween => MoveToNextPhase());
            
            tween.Start();
        }

        private void MoveToNextPhase(bool movePhase = false, int? phase = null)
        {
            _attackStarted = false;
            _timerStarted = false;
            _elapsedTime = 0.0f;
            _spriteAnimator.Play("Idle", SpriteAnimator.LoopMode.Loop);
            _spriteAnimator.OnAnimationCompletedEvent -= _attackFinishAction;

            if (movePhase && phase.HasValue)
            {
                _currentAttack = phase.Value;
            }
        }

        private void MoveToNextStage(BossStage stage)
        {
            _movePhaseHandlers.Clear();
            _currentAttackTick = 0;

            Action[] actions = new Action[]
            {
                () => MoveToNextPhase(true, (int)BossAttacks.ChainAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.RangeAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.Dash),
                () => MoveToNextPhase(true, (int)BossAttacks.JumpAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.ShieldAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.FullscreenAttack)
            };

            switch (stage)
            {
                case BossStage.One:
                    break;
                case BossStage.Two:
                    {
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[4]);

                        break;
                    }
                case BossStage.Three:
                    {
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[5]);

                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[5]);

                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[0]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[1]);
                        _movePhaseHandlers.Add(actions[4]);
                        _movePhaseHandlers.Add(actions[3]);
                        _movePhaseHandlers.Add(actions[2]);
                        _movePhaseHandlers.Add(actions[5]);
                        break;
                    }
                default:
                    break;
            }

            _currentStage = stage;
        }

        private void StageAttack(int patterns)
        {
            _movePhaseHandlers[_currentAttackTick].Invoke();

            _attackHandlers[_currentAttack].Invoke();
            _currentAttackTick = (_currentAttackTick + 1) % patterns;
        }
    }
}
