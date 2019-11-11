﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Sprites;
using Nez.Textures;
using Nez.Tweens;
using System.Collections.Generic;

namespace TeamProject3
{
    public class BossSettings
    {
        public readonly Vector2 Speed = new Vector2(150);
        public readonly float AnimationFramerate = 20.0f;
        public readonly float ProjectileVelocity = 350.0f;
        public readonly string AnimationFileName = "monster_dknight1";
        public readonly int AnimationFrameWidth = 94;
        public readonly int AnimationFrameHeight = 100;
                
        public readonly int PunchLoops = 2;
        public readonly float PunchDuration = 0.5f;
                
        public readonly EaseType DashEaseType = EaseType.BackIn;
        public readonly float DashDuration = 1.0f;
                
        public readonly Vector2 FirstStepJumpOffset = new Vector2(-175, -200);
        public readonly EaseType FirstStepJumpEaseType = EaseType.CircIn;
        public readonly float FirstStepJumpDuration = 1.5f;
        public readonly Vector2 SecondStepJumpOffset = new Vector2(-175, 200);
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
        private BoxCollider _collider;
        private int _currentAttack = 0;
        private int _currentAttackTick = 0;
        private readonly float _projectileVelocity;
        private float _bossDirection = 0;

        public float Width => _spriteAnimator.Width;
        public float Height => _spriteAnimator.Height;

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
        private List<AttackHandler> _bossPhaseHandlers = new List<AttackHandler>();

        private delegate void MovePhaseHandler(bool movePhase, int? phase);
        private List<MovePhaseHandler> _movePhaseHandlers = new List<MovePhaseHandler>();

        public Vector2 Speed { get; private set; }

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

            _bossPhaseHandlers.Add(StageOneAttack);
            _bossPhaseHandlers.Add(StageTwoAttack);
            _bossPhaseHandlers.Add(StageThreeAttack);
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
            _spriteAnimator.FlipX = (_bossDirection < 0) ? true : false;

            if (!_timerStarted)
            {
                _nextAttackDuration = Random.Range(2.5f, 3.5f);
                _timerStarted = true;
            }

            if (_elapsedTime > _nextAttackDuration && !_attackStarted)
            {
                _attackStarted = true;

                _bossPhaseHandlers[(int)_currentStage].Invoke();
            }

            _elapsedTime += Time.DeltaTime;
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

            _spriteAnimator.AddAnimation("WalkDown", new SpriteAnimation(
                spriteAtlas.ToArray()[0..3], _animationFramerate));
            _spriteAnimator.AddAnimation("WalkLeft", new SpriteAnimation(
                spriteAtlas.ToArray()[3..6], _animationFramerate));
            _spriteAnimator.AddAnimation("WalkRight", new SpriteAnimation(
                spriteAtlas.ToArray()[6..9], _animationFramerate));
            _spriteAnimator.AddAnimation("WalkUp", new SpriteAnimation(
                spriteAtlas.ToArray()[9..12], _animationFramerate));

            _mover = Entity.AddComponent<Mover>();
            //_collider = Entity.GetComponent<BoxCollider>();

            Entity.Position = _startPosition;

            _spriteAnimator.Play("WalkRight", SpriteAnimator.LoopMode.Loop);
        }

        public override void OnRemovedFromEntity()
        {
            base.OnRemovedFromEntity();
        }

        private void ChainAttack()
        {
            var tween = Entity
                .TweenPositionTo(new Vector2(100 * _bossDirection, 0), _bossSettings.PunchDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(EaseType.Punch)
                .SetLoops(LoopType.RestartFromBeginning, _bossSettings.PunchLoops);

            tween.SetCompletionHandler(_tween =>
            {
                _bossDirection *= -1;
                _spriteAnimator.FlipX = !_spriteAnimator.FlipX;

                var secondTween = Entity.TweenPositionTo(new Vector2(100 * _bossDirection, 0), _bossSettings.PunchDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(EaseType.Punch)
                .SetLoops(LoopType.None, 1);

                secondTween.SetCompletionHandler(_secondTween =>
                {
                    _bossDirection *= -1;
                    _spriteAnimator.FlipX = !_spriteAnimator.FlipX;

                    MoveToNextPhase();

                }).Start();

            }).Start();
        }

        private void RangeAttack()
        {
            var entity = Entity.Scene.CreateEntity("projectile");
            entity.Position = Entity.Position;
            entity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.Sun));
            entity.AddComponent<ProjectileMover>();
            entity.AddComponent(new ProjectileController(
                new Vector2(_projectileVelocity * _bossDirection, 0)));

            var collider = entity.AddComponent<CircleCollider>();
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);

            MoveToNextPhase();
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

            tween.Start();
        }

        private void JumpAttack()
        {
            var tween = Entity
                .TweenPositionTo(_bossSettings.FirstStepJumpOffset, _bossSettings.FirstStepJumpDuration)
                .SetFrom(Entity.Position)
                .SetIsRelative()
                .SetEaseType(_bossSettings.FirstStepJumpEaseType)
                .SetCompletionHandler(_tween =>
                {
                    var secondTween = Entity
                    .TweenPositionTo(_bossSettings.SecondStepJumpOffset, _bossSettings.SecondStepJumpDuration)
                    .SetFrom(Entity.Position)
                    .SetIsRelative()
                    .SetEaseType(_bossSettings.SecondStepJumpEaseType)
                    .SetCompletionHandler(_secondTween => {

                        var thirdTween = Entity.TweenPositionTo(new Vector2(350, 0), 2.0f)
                        .SetFrom(Entity.Position)
                        .SetIsRelative()
                        .SetEaseType(EaseType.Linear)
                        .SetCompletionHandler(_thirdTween => MoveToNextPhase());

                        thirdTween.Start();
                    });

                    _spriteAnimator.Play("WalkLeft", SpriteAnimator.LoopMode.Loop);
                    secondTween.Start();
                });

            _spriteAnimator.Play("WalkDown", SpriteAnimator.LoopMode.Loop);
            tween.Start();
        }

        private void FullscreenAttack()
        {
            var leftEntity = Entity.Scene.CreateEntity("projectile");
            leftEntity.Position = Entity.Position;
            leftEntity.AddComponent(ParticleSystem
                .CreateEmitter(ParticleSystem.ParticleType.BlueFlame));
            leftEntity.AddComponent<ProjectileMover>();
            leftEntity.AddComponent(new ProjectileController(new Vector2(_projectileVelocity, 0)));

            var collider = leftEntity.AddComponent<CircleCollider>();
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);

            var rightEntity = leftEntity.Clone();
            rightEntity.Position = Entity.Position;
            rightEntity.GetComponent<ProjectileController>().Velocity *= -1;
            rightEntity.AttachToScene(Entity.Scene);

            MoveToNextPhase();
        }

        private void ShieldAttack()
        {

        }

        private void MoveToNextPhase(bool movePhase = false, int? phase = null)
        {
            _attackStarted = false;
            _timerStarted = false;
            _elapsedTime = 0.0f;

            if (movePhase && phase.HasValue)
            {
                _currentAttack = phase.Value;
            }
        }

        private void StageOneAttack()
        {
            if (_currentAttackTick == 2)
            {
                MoveToNextPhase(true, (int)BossAttacks.RangeAttack);
            }
            else if (_currentAttackTick == 5)
            {
                MoveToNextPhase(true, (int)BossAttacks.Dash);
            }
            else
                MoveToNextPhase(true, (int)BossAttacks.ChainAttack);

            _attackHandlers[_currentAttack].Invoke();
            _currentAttackTick = (_currentAttackTick + 1) % 6;
        }

        private void StageTwoAttack()
        {
        }

        private void StageThreeAttack()
        {

        }
    }
}
