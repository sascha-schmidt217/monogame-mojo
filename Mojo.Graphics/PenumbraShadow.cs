using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    public class PenumbraShadow : IShadowRenderer
    {
        // x=0: dealing with segment vertex A; X=1: dealing with segment vertex B.	
        // y=0: not projecting the vertex; y=1: projecting the vertex.	
        private Vector2 _shadow0 = new Vector2(0.0f, 0.0f);
        private Vector2 _shadow1 = new Vector2(1.0f, 0.0f);
        private Vector2 _shadow2 = new Vector2(1.0f, 1.0f);
        private Vector2 _shadow3 = new Vector2(0.0f, 1.0f);

        private Effect _shadowEffect;
        private MojoVertex[] _buffer;
        private int _ShadowCount;
        private EffectParameter _pWorldViewProjection;
        private EffectParameter _pLightPosition;
        private EffectParameter _LightRadius;

        public Effect Effect => _shadowEffect;
        public int ShadowCount => _ShadowCount;
        public MojoVertex[] ShadowBuffer => _buffer;

        public Matrix Projection
        {
            set
            {
                _pWorldViewProjection.SetValue(value);
            }
        }

        public void OnLoad()
        {
            _shadowEffect = Global.Content.Load<Effect>("Effects/penumbra_shadow");
            _pWorldViewProjection = _shadowEffect.Parameters["WorldViewProjection"];
            _pLightPosition = _shadowEffect.Parameters["LightPosition"];
            _LightRadius = _shadowEffect.Parameters["LightRadius"];

            _buffer = new MojoVertex[65536/4];
            for(int i = 0; i < _buffer.Length;i+=4)
            {
                _buffer[i+0].Position = _shadow0;
                _buffer[i+1].Position = _shadow1;
                _buffer[i+3].Position = _shadow3;
                _buffer[i+2].Position = _shadow2;
            }
        }

        public void UpdateLight(Vector2 location, float size )
        {
            _pLightPosition.SetValue(location);
            _LightRadius.SetValue(size);
            _ShadowCount = 0;
        }


        public bool AddShadowVertices(ShadowType type, MojoVertex[] _shadowVertices, int start, int length)
        {
            if((ShadowCount + length) *4 >= ShadowBuffer.Length)
            {
                return false;
            }

            unsafe
            {
                var vert0 = start;
                var nverts = length;

                Vector2 prevPoint = _shadowVertices[vert0 + nverts - 1].Position;
                for (int i = 0; i < nverts; ++i)
                {
                    Vector2 currentPoint = _shadowVertices[vert0 + i].Position;

                    fixed (MojoVertex* tp = &_buffer[_ShadowCount++ * 4])
                    {
                        tp[0].Tex0 = prevPoint;
                        tp[0].Tex1 = currentPoint;
                        tp[1].Tex0 = prevPoint;
                        tp[1].Tex1 = currentPoint;
                        tp[3].Tex0 = prevPoint;
                        tp[3].Tex1 = currentPoint;
                        tp[2].Tex0 = prevPoint;
                        tp[2].Tex1 = currentPoint;
                    }

                    prevPoint = currentPoint;
                }
            }

            return true;
        }
    }
}
