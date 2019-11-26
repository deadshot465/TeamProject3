using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Nez.Farseer;
using System;
using System.Collections.Generic;

namespace TeamProject3
{
    public static class Helper
    {
        private static Random _random = new Random();

        public static int ScreenWidth => 1920;
        public static int ScreenHeight => 1080;

        public static void Shuffle<T>(this IList<T> collection)
        {
            var n = collection.Count;

            while (n > 1)
            {
                n--;
                var k = _random.Next(n + 1);
                T value = collection[k];
                collection[k] = collection[n];
                collection[n] = value;
            }
        }

        public static Tuple<FSRigidBody, Fixture> CreateFarseerFixture(ref Nez.Entity entity,
            BodyType bodyType, float mass,
            float collisionBoxWidth, float collisionBoxHeight, float density = 1.0f)
        {
            var rigidBody = entity.AddComponent<FSRigidBody>().SetBodyType(bodyType)
                .SetMass(mass).SetIsAwake(true).SetIsSleepingAllowed(false);
            var vertices = new Vertices();
            float x1 = FSConvert.ToSimUnits(-collisionBoxWidth);
            float x2 = FSConvert.ToSimUnits(collisionBoxWidth);
            float y1 = FSConvert.ToSimUnits(-collisionBoxHeight);
            float y2 = FSConvert.ToSimUnits(collisionBoxHeight);
            vertices.Add(new Vector2(x1, y1));
            vertices.Add(new Vector2(x2, y1));
            vertices.Add(new Vector2(x1, y2));
            vertices.Add(new Vector2(x2, y2));
            var fixture = rigidBody.Body.CreateFixture(new PolygonShape(vertices, density));
            return new Tuple<FSRigidBody, Fixture>(rigidBody, fixture);
        }
    }
}
