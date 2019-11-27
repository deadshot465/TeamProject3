using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using Microsoft.Xna.Framework;
using Nez;
using Nez.Farseer;

namespace TeamProject3
{
    public class ProjectileController : Component, IUpdatable
    {
        ProjectileMover _mover;
        FSRigidBody _rigidBody;
        Fixture _fixture;
        bool _isBossProjectile = false;
        bool _immutable = false;

        public Vector2 Velocity { get; set; }

        public ProjectileController(Vector2 velocity,
            FSRigidBody rigidBody, Fixture fixture,
            bool isBossProjectile = false,
            bool immutable = false)
        {
            Velocity = velocity;
            _rigidBody = rigidBody;
            _fixture = fixture;
            _isBossProjectile = isBossProjectile;
            _immutable = immutable;
        }


        void IUpdatable.Update()
        {
            if (_mover.Move(Velocity * Time.DeltaTime) ||
                Entity.Position.X > 6000 ||
                Entity.Position.X < -6000 ||
                Entity.Position.Y > 3000)
            {
                Entity.Destroy();
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _mover = Entity.GetComponent<ProjectileMover>();

            _fixture.OnCollision += CollisionHandler;
        }

        private bool CollisionHandler(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var playerEntity = Entity.Scene.FindEntity("player-entity");
            var invincible = playerEntity.GetComponent<Player>().IsInvincible;

            if (_isBossProjectile)
            {
                if (playerEntity != null)
                {
                    if (!invincible)
                    {
                        playerEntity.GetComponent<Player>().Hp -= 10;
                        System.Console.WriteLine($"Player Hp: {playerEntity.GetComponent<Player>().Hp}");
                    }
                }
            }
            if (!Entity.IsDestroyed && !invincible)
                Entity.Destroy();
            return true;
        }
    }
}
