using Microsoft.Xna.Framework;
using Nez;

namespace TeamProject3
{
    public class ProjectileController : Component, IUpdatable
    {
        ProjectileMover _mover;

        public Vector2 Velocity { get; set; }

        public ProjectileController(Vector2 velocity) => Velocity = velocity;

        void IUpdatable.Update()
        {
            if (_mover.Move(Velocity * Time.DeltaTime) ||
                Entity.Position.X > 1200 ||
                Entity.Position.X < -1200)
            {
                Entity.Destroy();
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();

            _mover = Entity.GetComponent<ProjectileMover>();
        }
    }
}
