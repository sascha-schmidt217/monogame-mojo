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
        private BasicEffect DefaultEffect;
        private MojoVertex[] _buffer;

        public int ShadowCount { get; private set; } = 0;
        public MojoVertex[] ShadowBuffer => _buffer;
        public Effect Effect => DefaultEffect;

        public Matrix Projection
        {
            set
            {
                DefaultEffect.Projection = (value);
            }
        }

        public void OnLoad()
        {
            DefaultEffect = new BasicEffect(Global.Device);
            _buffer = new MojoVertex[65536];
            for(int i = 0; i< _buffer.Length;++i)
            {
                _buffer[i].Color = Color.Black;
            }
        }

        public void UpdateLight(Vector2 location, float size)
        {
            ShadowCount = 0;
            lv = location;
        }

        public void AddShadowVertices(ShadowType type, List<Vector2> _shadowVertices, int start, int length)
        {
            if ((ShadowCount + length) * 4 >= ShadowBuffer.Length)
            {
                return;
            }

            var vert0 = start;
            var nverts = length;
            
            Vector2 tv = _shadowVertices[vert0 + nverts - 1];
            for (int i = 0; i < nverts; ++i)
            {
                var pv = tv;
                tv = _shadowVertices[vert0 + i];
            
                if (IsBackFacing(lv, pv, tv))
                    continue;
            
                var pv2 = pv + Vector2.Normalize(pv - lv) * EXTRUDE;
                var tv2 = tv + Vector2.Normalize(tv - lv) * EXTRUDE;
            
                unsafe
                {
                    fixed (MojoVertex* tp = &_buffer[ShadowCount++ * 4])
                    {
                        tp[0].Position.X = tv.X;
                        tp[0].Position.Y = tv.Y;
                        tp[1].Position.X = tv2.X;
                        tp[1].Position.Y = tv2.Y;
                        tp[2].Position.X = pv2.X;
                        tp[2].Position.Y = pv2.Y;
                        tp[3].Position.X = pv.X;
                        tp[3].Position.Y = pv.Y;
                    }
                }
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsBackFacing(Vector2 lv, Vector2 pv, Vector2 tv)
        {
            // The normal for the edge
            var dv = tv - pv;
            var nv = new Vector2(-dv.Y, dv.X);
            return Vector2.Dot(lv - pv, nv) >= 0;
        }
    }
}
