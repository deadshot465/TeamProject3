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

        public Vector2 Velocity { get; set; }

        public ProjectileController(Vector2 velocity, FSRigidBody rigidBody, Fixture fixture)
        {
            Velocity = velocity;
            _rigidBody = rigidBody;
            _fixture = fixture;
        }


        void IUpdatable.Update()
        {
            if (_mover.Move(Velocity * Time.DeltaTime) ||
                Entity.Position.X > 12000 ||
                Entity.Position.X < -12000)
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
            if (!Entity.IsDestroyed)
                Entity.Destroy();
            return true;
        }
    }
}
