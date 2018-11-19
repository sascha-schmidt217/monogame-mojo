using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    public class SimpleShadow : IShadowRenderer
    {
        const float EXTRUDE = 1024;

        private Vector2 lv = new Vector2();
        private Color c = new Color(Color.Black, 1.0f);

        public void OnLoad()
        {
        }

        public void OnRelease()
        {
        }

        public void OnUpdateFrame(Canvas canvas)
        {
            canvas.Effect = canvas.DefaultEffect;
        }

        public void OnUpdateLight(LightOp op)
        {
            lv = op.Location;
        }

        public void OnDrawShadow(Canvas canvas, ShadowType type, int offset, int count, List<Vector2> _shadowVertices)
        {
            var vert0 = offset;
            var nverts = count;

            Vector2 tv = _shadowVertices[vert0 + nverts - 1];
            for (int i = 0; i < nverts; ++i)
            {
                var pv = tv;
                tv = _shadowVertices[vert0 + i];

                if (is_back_facing(lv, pv, tv))
                    continue;

                var pv2 = pv + Vector2.Normalize(pv - lv) * EXTRUDE;
                var tv2 = tv + Vector2.Normalize(tv - lv) * EXTRUDE;

                unsafe
                {

                    var tp = canvas.AddDrawOp((int)PrimType.Quad, 1, null, canvas.DefaultEffect,
                        BlendMode.Subtract);

                    tp[0].Position.X = tv.X;
                    tp[0].Position.Y = tv.Y;
                    tp[0].Color = c;

                    tp[1].Position.X = tv2.X;
                    tp[1].Position.Y = tv2.Y;
                    tp[1].Color = c;

                    tp[2].Position.X = pv2.X;
                    tp[2].Position.Y = pv2.Y;
                    tp[2].Color = c;

                    tp[3].Position.X = pv.X;
                    tp[3].Position.Y = pv.Y;
                    tp[3].Color = c;
                }
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool is_back_facing(Vector2 lv, Vector2 pv, Vector2 tv)
        {
            // The normal for the edge
            var dv = tv - pv;
            var nv = new Vector2(-dv.Y, dv.X);
            return Vector2.Dot(lv - pv, nv) >= 0;
        }
    }
}
