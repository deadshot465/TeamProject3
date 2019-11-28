using Nez.Sprites;
using Nez.Textures;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Nez;

namespace TeamProject3.UI
{
    public class HpGauge : SpriteRenderer
    {
        public float Percentage { get; set; }

        public HpGauge(Texture2D texture) : base(texture)
        {
            Percentage = 1.0f;
        }

        public override void Render(Batcher batcher, Camera camera)
        {
            float width = Sprite.Texture2D.Width * Percentage;

            batcher.Draw(Sprite.Texture2D,
                Entity.Position + LocalOffset,
                new Rectangle(0, 0, (int)width,
                Sprite.Texture2D.Height), Color);
        }
    }
}
