﻿using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Farseer;
using Nez.Sprites;

namespace TeamProject3.Scene
{
    public class GameScene : Nez.Scene
    {
        public Entity PlayerEntity;
        public Entity BossEntity;
        private Entity _groundEntity;
        private const float _animationFramerate = 20.0f;
        private const float _projectileVelocity = 350.0f;
        private Vector2 _startPosition = new Vector2(250.0f, 100.0f);

        private RenderLayerRenderer _defaultRenderer;
        private RenderLayerRenderer _effectRenderer;

        private FollowCamera _followCamera;

        public Vector2 Viewport { get; set; } = Vector2.Zero;

        public GameScene(Vector2 viewportCenter)
        {
            Viewport = viewportCenter;
        }

        private ulong _count = 0;

        public override void Initialize()
        {
            base.Initialize();

            _defaultRenderer = AddRenderer(new RenderLayerRenderer(0, new[] { 0 }));
            _effectRenderer = AddRenderer(new RenderLayerRenderer(1, new[] { -5 }));

            var world = GetOrCreateSceneComponent<FSWorld>();

            var debugView = CreateEntity("debug-view").AddComponent(new FSDebugView(world));
            debugView.AppendFlags(FSDebugView.DebugViewFlags.AABB);
            debugView.AppendFlags(FSDebugView.DebugViewFlags.ContactPoints);
            debugView.AppendFlags(FSDebugView.DebugViewFlags.CenterOfMass);

            PlayerEntity = CreateEntity("player-entity");
            PlayerEntity
                .AddComponent(new Player(400.0f, _startPosition, _animationFramerate));
            _followCamera = Camera.Entity.AddComponent(new FollowCamera(PlayerEntity));
            Camera.Entity.AddComponent<CameraShake>();

            BossEntity = CreateEntity("boss-entity");
            BossEntity.AddComponent(new Boss(new Vector2(_startPosition.X + 400,
                _startPosition.Y), BossSettings.ImportBossSettings()));

            var groundTexture = Content.Load<Texture2D>("sample_ground");
            _groundEntity = CreateEntity("ground");
            _groundEntity.Position = new Vector2(960.0f / 2.0f, 395.0f);
            _groundEntity.AddComponent(new SpriteRenderer(groundTexture));
            var groundRigidBody = _groundEntity.AddComponent<FSRigidBody>()
                .SetBodyType(BodyType.Static);
            var vertices = new Vertices();
            var x1 = FSConvert.ToSimUnits(-480);
            var x2 = FSConvert.ToSimUnits(480);
            var y1 = FSConvert.ToSimUnits(-145);
            var y2 = FSConvert.ToSimUnits(145);
            vertices.Add(new Vector2(x1, y1));
            vertices.Add(new Vector2(x2, y1));
            vertices.Add(new Vector2(x1, y2));
            vertices.Add(new Vector2(x2, y2));
            var fixture = groundRigidBody.Body.CreateFixture(new PolygonShape(vertices, 1.0f));

            var playerComponent = PlayerEntity.GetComponent<Player>();
            var bossComponent = BossEntity.GetComponent<Boss>();
            playerComponent.GroundFixture = fixture;
            bossComponent.GroundFixture = fixture;
            playerComponent.BossFixture = bossComponent.BossFixture;
            bossComponent.PlayerFixture = playerComponent.PlayerFixture;

            //var collider = _groundEntity.AddComponent<BoxCollider>();

            BossSettings.ExportBossSettings();
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
            ClearColor = Color.Black;

            _groundEntity.Position = new Vector2(Viewport.X / 2, 395.0f);
 
            if (Input.IsKeyPressed(Keys.Q))
            {
                _followCamera.Camera.ZoomOut(0.5f);
                Core.Schedule(2.0f, timer =>
                {
                    _followCamera.Camera.ZoomIn(0.5f);
                });
            }

            if (Input.IsKeyPressed(Keys.S))
            {
                Camera.Entity.GetComponent<CameraShake>().Shake();
            }

            var world = GetSceneComponent<FSWorld>().World;
            world.Step(Time.DeltaTime);

            var playerComponent = PlayerEntity.GetComponent<Player>();
            var bossComponent = BossEntity.GetComponent<Boss>();
            bossComponent.PlayerFixture = playerComponent.PlayerFixture;
            playerComponent.BossFixture = bossComponent.BossFixture;

            if (System.Math.Abs(PlayerEntity.Position.X - BossEntity.Position.X) < 210)
            {
                var hit = Physics.Linecast(PlayerEntity.Position, BossEntity.Position);
                if (hit.Collider != null)
                {
                    System.Console.WriteLine($"Collision Detected!\tCount: {_count++}");
                }
            }
        }

        private void SetColliderFlags<T>(ref T collider) where T : Collider
        {
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);
        }
    }
}
