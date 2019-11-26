using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Farseer;
using Nez.Sprites;
using System;

namespace TeamProject3.Scene
{
    public class GameScene : Nez.Scene
    {
        public class BackgroundElement
        {
            public Texture2D ElementTexture { get; set; }
            public Entity ElementEntity { get; set; }
            public SpriteRenderer ElementSprite { get; set; }
        }

        private const float _animationFramerate = 20.0f;
        private const float _projectileVelocity = 350.0f;
        private Vector2 _startPosition = new Vector2(Helper.ScreenWidth / 2, 100.0f);

        private RenderLayerRenderer _defaultRenderer;
        private RenderLayerRenderer _effectRenderer;
        private RenderLayerRenderer _staticSpriteRenderer;

        private BackgroundElement[] _backgroundElements = new BackgroundElement[4];
        private BackgroundElement _stageElement = new BackgroundElement();
        private Fixture _stageFixture;

        private FollowCamera _followCamera;

        public Vector2 Viewport { get; set; } = Vector2.Zero;

        public GameScene(Vector2 viewportCenter)
        {
            Viewport = viewportCenter;
        }

        public Entity PlayerEntity;
        public Entity BossEntity;

        private ulong _count = 0;

        public override void Initialize()
        {
            base.Initialize();

            SetupRendererAndPhysicalWorld();
            CreateEntities();

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

            if (Math.Abs(PlayerEntity.Position.X - BossEntity.Position.X) < 210)
            {
                //var hit = Physics.Linecast(PlayerEntity.Position, BossEntity.Position);
                //if (hit.Collider != null)
                //{
                //    Console.WriteLine($"Collision Detected!\tCount: {_count++}");
                //}
            }
        }

        private void SetColliderFlags<T>(ref T collider) where T : Collider
        {
            Flags.SetFlagExclusive(ref collider.CollidesWithLayers, 0);
            Flags.SetFlagExclusive(ref collider.PhysicsLayer, 1);
        }

        private void SetupRendererAndPhysicalWorld()
        {
            _staticSpriteRenderer = AddRenderer(new RenderLayerRenderer(0, new[] { 1000, 990, 980, 970, 960 }));
            _defaultRenderer = AddRenderer(new RenderLayerRenderer(1, new[] { 0 }));
            _effectRenderer = AddRenderer(new RenderLayerRenderer(2, new[] { -5 }));

            var world = GetOrCreateSceneComponent<FSWorld>();

#if DEBUG
            var debugView = CreateEntity("debug-view").AddComponent(new FSDebugView(world));
            debugView.AppendFlags(FSDebugView.DebugViewFlags.AABB);
            debugView.AppendFlags(FSDebugView.DebugViewFlags.ContactPoints);
            debugView.AppendFlags(FSDebugView.DebugViewFlags.CenterOfMass);
#endif
        }

        private void CreateEntities()
        {
            // The background will be drawn first.
            // Farther pillars will be drawn next.
            // Closer pillars will be then drawn next.
            // Clouds will be drawn in the foreground.
            SetupBackgrounds(new[]
            {
                new Tuple<string, string>("1sky", "sky-entity"),
                new Tuple<string, string>("2pillar", "far-pillar-entity"),
                new Tuple<string, string>("3pillar", "close-pillar-entity"),
                new Tuple<string, string>("4cloud", "cloud-entity")
            }, new[]
            {
                1000, 990, 980, 970
            });

            PlayerEntity = CreateEntity("player-entity");
            PlayerEntity
                .AddComponent(new Player(400.0f, _startPosition, _animationFramerate));
            //_followCamera = Camera.Entity.AddComponent(new FollowCamera(PlayerEntity));
            Camera.Entity.AddComponent<CameraShake>();

            BossEntity = CreateEntity("boss-entity");
            BossEntity.AddComponent(new Boss(new Vector2(_startPosition.X + 400,
                _startPosition.Y), BossSettings.ImportBossSettings()));

            var playerComponent = PlayerEntity.GetComponent<Player>();
            var bossComponent = BossEntity.GetComponent<Boss>();
            playerComponent.GroundFixture = _stageFixture;
            bossComponent.GroundFixture = _stageFixture;
            playerComponent.BossFixture = bossComponent.BossFixture;
            bossComponent.PlayerFixture = playerComponent.PlayerFixture;

            //var collider = _groundEntity.AddComponent<BoxCollider>();
        }

        private void SetupBackgrounds(Tuple<string, string>[] names, int[] renderLayer)
        {
            int i = 0;

            foreach (var name in names)
            {
                var (fileName, entityName) = name;

                _backgroundElements[i] = new BackgroundElement
                {
                    ElementTexture =
                    Content.Load<Texture2D>(fileName),
                    ElementEntity =
                    CreateEntity(entityName)
                };
                _backgroundElements[i].ElementEntity.SetPosition(
                    Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);
                _backgroundElements[i].ElementSprite =
                    _backgroundElements[i].ElementEntity
                    .AddComponent(new SpriteRenderer(_backgroundElements[i].ElementTexture));
                _backgroundElements[i].ElementSprite.SetRenderLayer(renderLayer[i]);
                
                i++;
            }

            // Setting up the main stage platform.
            _stageElement.ElementTexture = Content.Load<Texture2D>("5stage");
            _stageElement.ElementEntity = CreateEntity("stage-entity");
            _stageElement.ElementSprite = _stageElement.ElementEntity
                .AddComponent(new SpriteRenderer(_stageElement.ElementTexture));
            _stageElement.ElementEntity
                .SetPosition(Helper.ScreenWidth / 2,
                Helper.ScreenHeight / 2);
            _stageElement.ElementSprite
                .SetRenderLayer(960);
            var stageRigidBody = _stageElement.ElementEntity
                .AddComponent<FSRigidBody>()
                .SetBodyType(BodyType.Static)
                .SetInertia(0.0f)
                .SetIsAwake(true)
                .SetIsSleepingAllowed(false)
                .SetFixedRotation(true);
            var vertices = new Vertices();
            var x1 = FSConvert.ToSimUnits(-744);
            var x2 = FSConvert.ToSimUnits(744);
            var y1 = FSConvert.ToSimUnits(324);
            var y2 = FSConvert.ToSimUnits(540);
            vertices.Add(new Vector2(x1, y1));
            vertices.Add(new Vector2(x2, y1));
            vertices.Add(new Vector2(x1, y2));
            vertices.Add(new Vector2(x2, y2));

            _stageFixture = stageRigidBody.Body
                .CreateFixture(new PolygonShape(vertices, 100000.0f));
        }
    }
}
