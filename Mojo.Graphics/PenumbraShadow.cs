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
        private EffectParameter _pWorldViewProjection;
        private EffectParameter _pLightPosition;
        private EffectParameter _LightRadius;

        public void OnLoad()
        {
            _shadowEffect = Global.Content.Load<Effect>("Effects/penumbra_shadow");
            _pWorldViewProjection = _shadowEffect.Parameters["WorldViewProjection"];
            _pLightPosition = _shadowEffect.Parameters["LightPosition"];
            _LightRadius = _shadowEffect.Parameters["LightRadius"];
        }

        public void OnRelease()
        {
            if(_shadowEffect!= null)
            {
                _shadowEffect.Dispose();
                _shadowEffect = null;
            }
        }

        public void OnUpdateFrame(Canvas canvas)
        {
            _pWorldViewProjection.SetValue(canvas.WorldViewProj);
        }

        public void OnUpdateLight(LightOp op)
        {
            _pLightPosition.SetValue(op.Location);
            _LightRadius.SetValue(op.Size);
        }

        public void OnDrawShadow(Canvas canvas, ShadowType type, int offset, int count, List<Vector2> _shadowVertices)
        {
            unsafe
            {
                var vert0 = offset;
                var nverts = count;

                Vector2 prevPoint = _shadowVertices[vert0 + nverts - 1];
                for (int i = 0; i < nverts; ++i)
                {
                    Vector2 currentPoint = _shadowVertices[vert0 + i];

                    var tp = canvas.AddDrawOp((int)PrimType.Quad, 1, null, _shadowEffect, BlendMode.Subtract);

                    tp[0].Position = _shadow0;
                    tp[0].Tex0 = prevPoint;
                    tp[0].Tex1 = currentPoint;

                    tp[1].Position = _shadow1;
                    tp[1].Tex0 = prevPoint;
                    tp[1].Tex1 = currentPoint;

                    tp[3].Position = _shadow3;
                    tp[3].Tex0 = prevPoint;
                    tp[3].Tex1 = currentPoint;

                    tp[2].Position = _shadow2;
                    tp[2].Tex0 = prevPoint;
                    tp[2].Tex1 = currentPoint;

                    prevPoint = currentPoint;
                }
            }
        }
    }
}
