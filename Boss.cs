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
        public readonly Vector2 SecondStepJumpOffset = new Vector2(175, 372.5f);
        public readonly EaseType SecondStepJumpEaseType = EaseType.BackIn;
        public readonly float SecondStepJumpDuration = 1.0f;
        public readonly Vector2 ThirdStepJumpOffset = new Vector2(-75f, -172.5f);
        public readonly EaseType ThirdStepJumpEaseType = EaseType.Linear;
        public readonly float ThirdStepJumpDuration = 0.5f;

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
        private const float _stageOneHp = 50.0f;
        private const float _stageTwoHp = 50.0f;
        private const float _stageThreeHp = 100.0f;
        private bool _isAttackPaused = false;

        private FSRigidBody _rigidBody;

        private BossSettings _bossSettings;

        private enum BossStage
        {
            One, Two, Three, End
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
        private List<Entity> _judgeEntities = new List<Entity>();
        private Action<string> _attackFinishAction;

        public Vector2 Speed { get; private set; }
        public Fixture GroundFixture { get; set; }
        public Fixture LeftWallFixture { get; set; }
        public Fixture PlayerFixture { get; set; }
        public Fixture BossFixture { get; private set; }
        public float Width => _spriteAnimator.Width;
        public float Height => _spriteAnimator.Height;
        public BoxCollider Collider { get; private set; }
        public float Hp { get; set; } = _stageOneHp;
        public bool FadeFlag { get; private set; } = false;
        public bool FadeFinished { get; set; } = false;
        public bool CanAttack { get; set; } = false;
        public bool IsDead { get; private set; } = false;

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
                () => MoveToNextPhase(true, (int)BossAttacks.Dash),
                () => MoveToNextPhase(true, (int)BossAttacks.JumpAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.ShieldAttack),
                () => MoveToNextPhase(true, (int)BossAttacks.FullscreenAttack)
            };

            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[1]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[0]);
            _movePhaseHandlers.Add(actions[2]);

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
            HandleMoveStage();

            if (IsDead) return;

            var playEntity = Entity.Scene.FindEntity("player-entity");
            _bossDirection = (playEntity.Position.X < Entity.Position.X) ? -1.0f : 1.0f;
            _spriteAnimator.FlipX = (_bossDirection < 0) ? false : true;

            if (_currentStage != BossStage.End)
            {
                if (!_timerStarted)
                {
                    _nextAttackDuration = Nez.Random.Range(1.0f, 2.0f);
                    _timerStarted = true;
                    return;
                }
                else
                    _elapsedTime += Time.DeltaTime;

                if (_elapsedTime > _nextAttackDuration && !_attackStarted)
                {
                    _attackStarted = true;
                    _elapsedTime = 0.0f;

                    _bossPhaseHandlers[(int)_currentStage].Invoke(_currentStage switch
                    {
                        BossStage.One => 6,
                        BossStage.Two => 16,
                        BossStage.Three => 37,
                        _ => 0
                    });
                }
            }

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
            _spriteAnimator.AddAnimation("Shielding", new SpriteAnimation(
                spriteAtlas.ToArray()[40..50], _animationFramerate));
            _spriteAnimator.AddAnimation("HideSword", new SpriteAnimation(
                spriteAtlas.ToArray()[50..60], _animationFramerate));
            _spriteAnimator.AddAnimation("Attack", new SpriteAnimation(
                spriteAtlas.ToArray()[20..40], _animationFramerate));

            _mover = Entity.AddComponent<Mover>();
            Collider = Entity.AddComponent(
                new BoxCollider(Width / 2, Height));

            Entity.Position = _startPosition;

            (_rigidBody, BossFixture) = Helper.CreateFarseerFixture(ref Entity,
                BodyType.Dynamic, -1.0f,
                Width / 4, Height / 2);
            _rigidBody.SetInertia(0.0f).SetFixedRotation(true).SetIgnoreGravity(true);
            BossFixture.CollisionGroup = -1;

            _spriteAnimator.Play("Idle", SpriteAnimator.LoopMode.Loop);

            //MoveToNextStage(BossStage.End);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();
        }

        private void ChainAttack()
        {
            if (_isAttackPaused) return;
            _spriteAnimator.Play("RaiseSword", SpriteAnimator.LoopMode.ClampForever);

            Core.Schedule(0.25f, timer =>
            {
                _spriteAnimator.Play("PutdownSword", SpriteAnimator.LoopMode.ClampForever);
                _spriteAnimator.OnAnimationCompletedEvent += _attackFinishAction;
                HandleAttack();
            });
        }

        private void RangeAttack()
        {
            if (_isAttackPaused) return;
            _spriteAnimator.Play("RaiseSword", SpriteAnimator.LoopMode.ClampForever);

            var entity = Entity.Scene.CreateEntity("projectile");
            entity.Position = new Vector2(Entity.Position.X + (150 * _bossDirection), Entity.Position.Y - 200);
            var emitter = entity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.Charge));
            emitter.SetRenderLayer(-5);
            entity.AddComponent<ProjectileMover>();

            if (!FadeFlag && !FadeFinished) FadeFlag = true;

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

                var (bulletRigidBody, bulletFixture) = Helper.CreateFarseerFixture(ref entity, BodyType.Kinematic, 0.0f, 75.0f, 75.0f);
                bulletRigidBody.SetIsBullet(true)
                .SetIgnoreGravity(true)
                .SetLinearVelocity(new Vector2(3.0f * _bossDirection, 0.0f));
                bulletFixture.IgnoreCollisionWith(BossFixture);

                entity.AddComponent(new ProjectileController(
                    new Vector2(_projectileVelocity * _bossDirection, 0),
                    bulletRigidBody, bulletFixture, true));

                if (FadeFlag && FadeFinished) FadeFlag = false;
            });
        }

        private void Dash()
        {
            if (_isAttackPaused) return;
            var entity = Entity.Scene.CreateEntity("projectile");
            //entity.Position = new Vector2(Entity.Position.X + (100 * _bossDirection), Entity.Position.Y);
            entity.SetParent(Entity);
            entity.SetLocalPosition(new Vector2(0.0f, -200.0f));
            var emitter = entity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.DashReady));
            emitter.SetRenderLayer(-5);

            if (!FadeFlag && !FadeFinished) FadeFlag = true;

            Core.Schedule(2.0f, timer =>
            {
                _spriteAnimator.Play("Move", SpriteAnimator.LoopMode.Loop);

                var tween = Entity.TweenPositionTo(new Vector2(350 * _bossDirection, 0), _bossSettings.DashDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(_bossSettings.DashEaseType)
                .SetCompletionHandler(_tween =>
                {
                    entity.Destroy();
                    HandleAttack();
                    MoveToNextPhase();
                });

                tween.Start();

                if (FadeFlag && FadeFinished) FadeFlag = false;
            });
        }

        private void JumpAttack()
        {
            if (_isAttackPaused) return;
            var firstOffset = _bossSettings.FirstStepJumpOffset;
            firstOffset.X *= _bossDirection;
            var secondOffset = _bossSettings.SecondStepJumpOffset;
            secondOffset.X *= _bossDirection;
            var thirdOffset = _bossSettings.ThirdStepJumpOffset;
            thirdOffset.X *= _bossDirection;

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
                    .SetCompletionHandler(_secondTween =>
                    {
                        var entity = Entity.Scene.CreateEntity("projectile-1");
                        entity.Position = new Vector2(Entity.Position.X + (100 * _bossDirection), Entity.Position.Y + 50);
                        var _emitter = entity.AddComponent(ParticleSystem
                            .CreateEmitter(ParticleSystem.ParticleType.Charge));
                        _emitter.SetRenderLayer(-5);
                        entity.AddComponent<ProjectileMover>();

                        var clonedEntity = entity.Clone(new Vector2(
                            Entity.Position.X + (100 * -(_bossDirection)), Entity.Position.Y + 50));
                        clonedEntity.AttachToScene(Entity.Scene);

                        var (bulletRigidBody1, bulletFixture1) = Helper.CreateFarseerFixture(ref entity, BodyType.Kinematic, 0.0f, 50.0f, 50.0f);
                        bulletRigidBody1.SetIsBullet(true)
                        .SetIgnoreGravity(true)
                        .SetLinearVelocity(new Vector2(5.0f * _bossDirection, 0.0f));
                        bulletFixture1.IgnoreCollisionWith(BossFixture);

                        var (bulletRigidBody2, bulletFixture2) = Helper.CreateFarseerFixture(ref clonedEntity, BodyType.Kinematic, 0.0f, 50.0f, 50.0f);
                        bulletRigidBody2.SetIsBullet(true)
                        .SetIgnoreGravity(true)
                        .SetLinearVelocity(new Vector2(5.0f * -(_bossDirection), 0.0f));
                        bulletFixture2.IgnoreCollisionWith(BossFixture);

                        entity.AddComponent(new ProjectileController(
                            new Vector2(_projectileVelocity * _bossDirection, 0),
                            bulletRigidBody1, bulletFixture1, true));

                        clonedEntity.AddComponent(new ProjectileController(
                            new Vector2(_projectileVelocity * _bossDirection, 0),
                            bulletRigidBody2, bulletFixture2, true));

                        var thirdTween = Entity
                        .TweenPositionTo(thirdOffset, _bossSettings.ThirdStepJumpDuration)
                        .SetFrom(Entity.Position)
                        .SetIsRelative()
                        .SetEaseType(_bossSettings.ThirdStepJumpEaseType)
                        .SetCompletionHandler(_thirdTween =>
                        {
                            HandleAttack();
                            Core.Schedule(0.5f, timer => MoveToNextPhase());
                        });

                        thirdTween.Start();
                    });

                    secondTween.Start();
                });

            _spriteAnimator.Play("Move", SpriteAnimator.LoopMode.Loop);

            tween.Start();
        }

        private void FullscreenAttack()
        {
            if (_currentStage == BossStage.End) return;
            var tween = Entity.TweenPositionTo(new Vector2(Helper.ScreenWidth / 2, 150.0f), 2.0f)
                .SetFrom(Entity.Position)
                .SetEaseType(EaseType.ExpoInOut)
                .SetCompletionHandler(_tween =>
                {
                    _spriteAnimator.Play("HideSword", SpriteAnimator.LoopMode.ClampForever);

                    Core.Schedule(0.5f, timer =>
                    {
                        var entity = Entity.Scene.CreateEntity("admonishment");
                        entity.SetParent(Entity);
                        entity.SetLocalPosition(new Vector2(0.0f, 100.0f));
                        var emitter = entity.AddComponent(ParticleSystem
                            .CreateEmitter(ParticleSystem.ParticleType.Admonishment));
                        emitter.SetRenderLayer(-5);

                        Core.Schedule(1.0f, _timer =>
                        {
                            entity.Destroy();

                            const float padding = 300.0f;
                            for (float i = Helper.ScreenWidth / 2; i > 0; i -= padding)
                            {
                                _judgeEntities.Add(Entity.Scene.CreateEntity(Nez.Utils.RandomString(10)));
                                var judgeLeftEntity = _judgeEntities.Last();
                                judgeLeftEntity.Position = new Vector2(
                                    i, Entity.Position.Y);
                                var judgeLeftEmitter = judgeLeftEntity.AddComponent(
                                    ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Judgement));
                                judgeLeftEmitter.SetRenderLayer(-5);
                                judgeLeftEmitter.AddComponent<ProjectileMover>();

                                var (bulletRigidBody, bulletFixture) = Helper.CreateFarseerFixture(ref judgeLeftEntity, BodyType.Kinematic, 0.0f, 50.0f, 50.0f);
                                bulletRigidBody.SetIsBullet(true).SetIgnoreGravity(true);
                                bulletFixture.IgnoreCollisionWith(BossFixture);

                                judgeLeftEntity.AddComponent(new ProjectileController(
                                new Vector2(_projectileVelocity * _bossDirection, 0),
                                bulletRigidBody, bulletFixture, true));

                                if (i != Helper.ScreenWidth / 2)
                                {
                                    _judgeEntities.Add(Entity.Scene.CreateEntity(Nez.Utils.RandomString(10)));
                                    var judgeRightEntity = _judgeEntities.Last();
                                    judgeRightEntity.Position = new Vector2(
                                    Helper.ScreenWidth / 2 + (Helper.ScreenWidth / 2 - i), Entity.Position.Y);

                                    var judgeRightEmitter = judgeRightEntity.AddComponent(
                                    ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Judgement));
                                    judgeRightEmitter.SetRenderLayer(-5);
                                    judgeRightEmitter.AddComponent<ProjectileMover>();

                                    var (bulletRigidBodyRight, bulletFixtureRight) = Helper.CreateFarseerFixture(ref judgeRightEntity, BodyType.Kinematic, 0.0f, 50.0f, 50.0f);
                                    bulletRigidBodyRight.SetIsBullet(true).SetIgnoreGravity(true);
                                    bulletFixtureRight.IgnoreCollisionWith(BossFixture);

                                    judgeRightEntity.AddComponent(new ProjectileController(
                                    new Vector2(_projectileVelocity * _bossDirection, 0),
                                    bulletRigidBodyRight, bulletFixtureRight, true));

                                    _judgeEntities.Add(judgeRightEntity);
                                }
                            }

                            Core.Schedule(1.0f, __timer =>
                            {
                                _judgeEntities.ForEach(_entity =>
                                {
                                    if (_entity.GetComponent<FSRigidBody>() != null)
                                    {
                                        _entity.GetComponent<FSRigidBody>()
                                        .SetLinearVelocity(new Vector2(0.0f, 3.0f));
                                    }
                                });

                                Core.Schedule(1.0f, ___timer =>
                                {
                                    _spriteAnimator.Play("Idle", SpriteAnimator.LoopMode.Loop);
                                    var secondTween = Entity.TweenPositionTo(new Vector2(Helper.ScreenWidth / 2, _startPosition.Y), 3.0f);
                                    secondTween.SetFrom(Entity.Position)
                                    .SetEaseType(EaseType.Linear)
                                    .SetCompletionHandler(_secondTween =>
                                    {
                                        Core.Schedule(1.0f, timer => MoveToNextPhase());
                                    })
                                    .Start();
                                });
                            });
                        });
                    });
                });

            tween.Start();
        }

        private void ShieldAttack()
        {
            if (_isAttackPaused) return;
            var tween = Entity
                .TweenPositionTo(new Vector2(50 * _bossDirection, 0), 0.5f)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(EaseType.ExpoIn)
                .SetCompletionHandler(_tween =>
                {
                    HandleAttack();
                    MoveToNextPhase();
                });

            _spriteAnimator.Play("Shielding", SpriteAnimator.LoopMode.Once);
            
            tween.Start();
        }

        private void MoveToNextPhase(bool movePhase = false, int? phase = null)
        {
            if (_currentStage == BossStage.End) return;
            if (!movePhase)
            {
                _attackStarted = false;
                _timerStarted = false;
                _elapsedTime = 0.0f;
            }
            
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

                        Hp = _stageTwoHp;
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

                        Hp = _stageThreeHp;
                        Core.Schedule(2.0f, timer =>
                        {
                            var entity = Entity.Scene.CreateEntity("infinity");
                            entity.SetParent(Entity);
                            var emitter = entity.AddComponent(
                                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.Infinity));
                            emitter.SetRenderLayer(-5);
                            entity.AddComponent<ProjectileMover>();
                        });
                        break;
                    }
                case BossStage.End:
                    HandleDeath();
                    break;
                default:
                    break;
            }

            _currentStage = stage;
        }

        private void StageAttack(int patterns)
        {
            if (_currentStage == BossStage.End || _isAttackPaused) return;
            _movePhaseHandlers[_currentAttackTick].Invoke();

            _attackHandlers[_currentAttack].Invoke();
            _currentAttackTick = (_currentAttackTick + 1) % patterns;
        }

        private void HandleAttack()
        {
            if (_currentStage == BossStage.End || _isAttackPaused) return;
            if (!CanAttack) return;
            var playerEntity = Entity.Scene.FindEntity("player-entity");
            if (playerEntity != null)
            {
                if (!playerEntity.GetComponent<Player>().IsInvincible)
                {
                    playerEntity.GetComponent<Player>().Hp -= 10;
                    System.Console.WriteLine($"Player Hp: {playerEntity.GetComponent<Player>().Hp}");
                }
            }
        }

        private void HandleMoveStage()
        {
            if (Hp > 0 || _currentStage == BossStage.End) return;
            _isAttackPaused = true;
            
            if (_currentStage != BossStage.Three) FullscreenAttack();
            MoveToNextStage(_currentStage switch
            {
                BossStage.One => BossStage.Two,
                BossStage.Two => BossStage.Three,
                BossStage.Three => BossStage.End
            });
            Core.Schedule(5.0f, timer => _isAttackPaused = false);
        }

        private void HandleDeath()
        {
            _isAttackPaused = true;
            var infinityEntity = Entity.Scene.FindEntity("infinity");
            if (infinityEntity != null) infinityEntity.Destroy();

            var tween = Entity.TweenPositionTo(
                new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2 - 150.0f), 3.0f);
            _spriteAnimator.Play("Idle", SpriteAnimator.LoopMode.Loop);
            _spriteAnimator.Speed *= 0.75f;
            tween.SetDelay(2.0f)
                .SetEaseType(EaseType.Linear)
                .SetFrom(Entity.Position)
                .SetCompletionHandler(_tween =>
                {
                    Core.Schedule(2.0f, timer =>
                    {
                        var entity = Entity.Scene.CreateEntity("the-end");
                        entity.SetParent(Entity);
                        var emitter = entity.AddComponent(
                            ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.TheEnd));
                        emitter.SetRenderLayer(-5);
                        entity.AddComponent<ProjectileMover>();
                        var emitterTween = emitter.TweenColorTo(Color.DarkRed, 5.0f);
                        emitterTween.SetEaseType(EaseType.Linear)
                        .SetFrom(emitter.Color)
                        .SetCompletionHandler(emitterTimer =>
                        {
                            var secondEntity = Entity.Scene.CreateEntity("new-beginning");
                            secondEntity.SetParent(Entity);
                            var secondEmitter = secondEntity.AddComponent(
                                ParticleSystem.CreateEmitter(ParticleSystem.ParticleType.FullscreenFeather));
                            secondEmitter.SetRenderLayer(-5);
                            secondEmitter.AddComponent<ProjectileMover>();
                            entity.Destroy();

                            var lastTween = _spriteAnimator.TweenColorTo(Color.Transparent, 5.0f);
                            lastTween.SetFrom(_spriteAnimator.Color)
                            .SetCompletionHandler(lastTweenTimer =>
                            {
                                secondEntity.Destroy();
                                Core.Schedule(2.0f, endTimer => IsDead = true);
                            }).Start();

                        }).Start();
                    });
                }).Start();
        }
    }
}
