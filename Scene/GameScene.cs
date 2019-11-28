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
using System.Collections;

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
        private Vector2 _startPosition = new Vector2(Helper.ScreenWidth / 2, Helper.ScreenHeight / 2);

        private RenderLayerRenderer _defaultRenderer;
        private RenderLayerRenderer _effectRenderer;
        private RenderLayerRenderer _staticSpriteRenderer;
        private RenderLayerRenderer _uiRenderer;

        private BackgroundElement[] _backgroundElements = new BackgroundElement[4];
        private BackgroundElement _stageElement = new BackgroundElement();
        private BackgroundElement _shadeElement = new BackgroundElement();
        private BackgroundElement _hiddenStageElement = new BackgroundElement();
        private Fixture _stageFixture;

        private Entity _playerUiEntity;
        private Entity _bossUiEntity;
        private SpriteRenderer _playerHpSprite;
        private SpriteRenderer _bossHpSprite;

        private float PlayerHp => PlayerEntity.GetComponent<Player>().Hp;
        private float BossHp => BossEntity.GetComponent<Boss>().Hp;

        private FollowCamera _followCamera;

        public Vector2 Viewport { get; set; } = Vector2.Zero;
        public Entity PlayerEntity;
        public Entity BossEntity;
        public bool IsPlayerAlive
        {
            get
            {
                if (PlayerEntity != null && PlayerEntity.GetComponent<Player>() != null)
                {
                    return PlayerEntity.GetComponent<Player>().Hp > 0;
                }
                return true;
            }
        }
        public bool IsBossDead
        {
            get
            {
                if (BossEntity != null && BossEntity.GetComponent<Boss>() != null)
                {
                    return BossEntity.GetComponent<Boss>().IsDead;
                }
                return true;
            }
        }

        private ulong _count = 0;

        public GameScene(Vector2 viewportCenter)
        {
            Viewport = viewportCenter;
        }

        public override void Initialize()
        {
            base.Initialize();

            SetupRendererAndPhysicalWorld();
            CreateEntities();
            LoadUi();

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

            if (Math.Abs(PlayerEntity.Position.X - BossEntity.Position.X) < 210 &&
                Math.Abs(PlayerEntity.Position.Y - BossEntity.Position.Y) < 175)
            {
                var hit = Physics.Linecast(PlayerEntity.Position, BossEntity.Position);
                if (hit.Collider != null)
                {
                    PlayerEntity.GetComponent<Player>().CanAttack = true;
                    BossEntity.GetComponent<Boss>().CanAttack = true;
                }
            }
            else
            {
                PlayerEntity.GetComponent<Player>().CanAttack = false;
                BossEntity.GetComponent<Boss>().CanAttack = false;
            }

            if (bossComponent.FadeFlag && !bossComponent.FadeFinished)
            {
                Core.StartCoroutine(ShadeFadeIn());
            }
            else if (!bossComponent.FadeFlag && bossComponent.FadeFinished)
            {
                Core.StartCoroutine(ShadeFadeOut());
            }
        }

        private IEnumerator ShadeFadeIn()
        {
            if (BossEntity.GetComponent<Boss>().FadeFinished) yield break;
            for (float i = 0.0f; i <= 0.5f; i += Time.DeltaTime)
            {
                _shadeElement.ElementSprite
                    .SetColor(new Color(0, 0, 0, i));
                yield return null;
            }
            BossEntity.GetComponent<Boss>().FadeFinished = true;
            yield break;
        }

        private IEnumerator ShadeFadeOut()
        {
            if (!BossEntity.GetComponent<Boss>().FadeFinished) yield break;
            for (float i = 0.5f; i >= 0.0f; i -= Time.DeltaTime)
            {
                _shadeElement.ElementSprite
                    .SetColor(new Color(0, 0, 0, i));
                yield return null;
            }
            BossEntity.GetComponent<Boss>().FadeFinished = false;
            yield break;
        }

        private void SetupRendererAndPhysicalWorld()
        {
            _staticSpriteRenderer =
                AddRenderer(new RenderLayerRenderer(0,
                new[] { 1000, 990, 980, 970, 960, 950 }));
            _defaultRenderer = AddRenderer(new RenderLayerRenderer(1, new[] { 0 }));
            _effectRenderer = AddRenderer(new RenderLayerRenderer(2, new[] { -5 }));
            _uiRenderer = AddRenderer(new RenderLayerRenderer(2, new[] { -20, -15, -10 }));

            var world = GetOrCreateSceneComponent<FSWorld>();

#if DEBUG
            //var debugView = CreateEntity("debug-view").AddComponent(new FSDebugView(world));
            //debugView.AppendFlags(FSDebugView.DebugViewFlags.AABB);
            //debugView.AppendFlags(FSDebugView.DebugViewFlags.ContactPoints);
            //debugView.AppendFlags(FSDebugView.DebugViewFlags.CenterOfMass);
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
        }

        private void SetupBackgrounds(Tuple<string, string>[] names, int[] renderLayer)
        {
            #region Static Background Sprites
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
            #endregion

            #region Main Stage Platform
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

            var stageSecondEntity = CreateEntity("stage-second-entity");
            var stageSecondTexture = Graphics.CreateSingleColorTexture(1488, 216, Color.Transparent);
            stageSecondEntity.SetParent(_stageElement.ElementEntity);
            stageSecondEntity.SetLocalPosition(new Vector2(0.0f, 432.0f));
            stageSecondEntity.AddComponent(new SpriteRenderer(stageSecondTexture));
            stageSecondEntity.AddComponent<BoxCollider>();
            
            #endregion

            //var leftWallEntity = CreateEntity("left-wall-entity");
            //leftWallEntity.Position = new Vector2(Helper.ScreenWidth / 4 - 384, Helper.ScreenHeight / 2);
            //leftWallEntity.AddComponent(new SpriteRenderer(
            //    Graphics.CreateSingleColorTexture(216, Helper.ScreenHeight, Color.Transparent)));
            //_leftWallRigidBody = leftWallEntity.AddComponent<FSRigidBody>()
            //    .SetBodyType(BodyType.Static)
            //    .SetFixedRotation(true)
            //    .SetInertia(0.0f)
            //    .SetIsAwake(true).SetIsSleepingAllowed(false);
            //var leftWallVertices = new Vertices();
            //var x3 = FSConvert.ToSimUnits(-960);
            //var x4 = FSConvert.ToSimUnits(-744);
            //var y3 = FSConvert.ToSimUnits(-540);
            //var y4 = FSConvert.ToSimUnits(540);
            //leftWallVertices.Add(new Vector2(x3, y3));
            //leftWallVertices.Add(new Vector2(x4, y3));
            //leftWallVertices.Add(new Vector2(x3, y4));
            //leftWallVertices.Add(new Vector2(x4, y4));
            //_leftWallFixture =
            //    _leftWallRigidBody.Body
            //    .CreateFixture(new PolygonShape(leftWallVertices, 100000.0f));
            //leftWallEntity.AddComponent<BoxCollider>();

            #region Black Shade Preparation
            // Create a black shade.
            _shadeElement.ElementTexture = Graphics
                .CreateSingleColorTexture(Helper.ScreenWidth, Helper.ScreenHeight, Color.Black);
            _shadeElement.ElementEntity = CreateEntity("shade-entity");
            _shadeElement.ElementSprite = _shadeElement.ElementEntity
                .AddComponent(new SpriteRenderer(_shadeElement.ElementTexture));
            _shadeElement.ElementEntity
                .SetPosition(Helper.ScreenWidth / 2,
                Helper.ScreenHeight / 2);
            _shadeElement.ElementSprite
                .SetRenderLayer(950);
            _shadeElement.ElementSprite.SetColor(Color.TransparentBlack);
            #endregion
        }

        private void LoadUi()
        {
            _playerUiEntity = CreateEntity("player-ui");
            _bossUiEntity = CreateEntity("boss-ui");
            var _playerGaugeTexture = Content.Load<Texture2D>("UI_player1");
            var _playerHpTexture = Content.Load<Texture2D>("UI_player2");
            _playerUiEntity.Position =
                new Vector2(Helper.ScreenWidth / 6, Helper.ScreenHeight / 10);
            _playerUiEntity.SetUpdateOrder(int.MaxValue);
            
            _playerUiEntity.AddComponent(
                new SpriteRenderer(_playerGaugeTexture)).SetRenderLayer(-10);
            _playerHpSprite = _playerUiEntity.AddComponent(
                new SpriteRenderer(_playerHpTexture));
            _playerHpSprite.SetRenderLayer(-15);
        }
    }
}
