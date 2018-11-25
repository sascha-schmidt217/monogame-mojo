using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    class PostEffectPipeline
    {
        private Image _src;
        private Image _dst;

        public List<Effect> Filters { get; private set; } = new List<Effect>();

        internal RenderTarget2D GetRenderTarget(Image reference)
        {
            if (Filters.Count > 0)
            {
                _src = Global.CreateRenderImage(reference.Width, reference.Height, ref _src);
                _dst = Global.CreateRenderImage(reference.Width, reference.Height, ref _dst);
                return _dst;
            }
            else
            {
                return null;
            }
        }

        internal void Render(Canvas canvas)
        {
            canvas.PushMatrix();
            canvas.ResetMatrix();
            canvas.Alpha = 1.0f;
            canvas.Color = Color.White;
            canvas.BlendMode = BlendMode.Opaque;

            for (int i = 0; i < Filters.Count; ++i)
            {
                if (i > 0)
                {
                    var tmp = _dst;
                    _dst = _src;
                    _src = tmp;
                }

                if (i == Filters.Count - 1)
                {
                    canvas.RenderTarget = null;
                }
                else
                {
                    canvas.RenderTarget = _dst;
                }

                canvas.Effect = Filters[i];
                canvas.DrawImage(_src, 0, 0);
                canvas.Flush();
            }

            canvas.Effect = null;
            canvas.PopMatrix();            
        }
    }
}
