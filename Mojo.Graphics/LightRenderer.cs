using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    public enum ShadowType
    {
        Solid,
        Illuminated
    }

    public interface IShadowRenderer
    {
        void OnLoad();
        void OnRelease();
        void OnUpdateFrame(Canvas canvas);
        void OnUpdateLight(LightOp op);
        void OnDrawShadow(Canvas canvas, ShadowType type, int offset, int count, List<Vector2> shadow_vertices);
    }

    public class ShadowOp
    {
        public ShadowType ShadowType { get; set; } = ShadowType.Illuminated;
        public Vector2[] Caster { get; set; }
        public int Offset { get; set; }
    }
    public class LightOp
    {
        public Transform2D Transform;
        public Color Color;
        public float Alpha;
        public Vector2 Location;
        public float Range;
        public float Intensity;
        public float Size;
    }
    public class SpotLightOp : LightOp
    {
        public float Inner;
        public float Outer;
        public Vector2 Dir;
    }
    public class PointLightOp : LightOp
    {
    }

    public class LightRenderer : ILightRenderer
    {
        const float DEG_TO_RAD = 0.0174532925199432957692369076848861f;

        private List<ShadowOp> _shadowOps = new List<ShadowOp>(1024);
        private List<Vector2> _shadowVertices = new List<Vector2>(1024);
        private Buffer _shadowBuffer;
        private Canvas _canvas;
        private PointLightEffect _pointLightEffect;
        private SpotLightEffect _spotLightEffect;
        private Image _lightmap;
        private Image _shadowmap;
        private List<PointLightOp> _pointLights = new List<PointLightOp>(1024);
        private List<SpotLightOp> _spotLights = new List<SpotLightOp>(1024);
        private IShadowRenderer _shadowRenderer = new PenumbraShadow();
        private int _width = 0;
        private int _height = 0;
        private MojoVertex[] _lightVertices = new MojoVertex[4];

        public Color AmbientColor { get; set; }

        public void OnLoad(Canvas c)
        {
            _shadowBuffer = new Buffer(4096);
            _spotLightEffect = new SpotLightEffect(Global.Content.Load<Effect>("Effects/spot_light"));
            _pointLightEffect = new PointLightEffect(Global.Content.Load<Effect>("Effects/point_light"));
   
            Resize(c.Width, c.Height);

            _canvas = new Canvas(LightMap);
            _shadowRenderer.OnLoad();           
        }

        public Image LightMap
        {
            get
            {
                return _lightmap;
            }
            private set
            {
                _lightmap = value;
            }
        }

        public void AddShadowCaster(Canvas c, Vector2[] vertices, float tx, float ty, ShadowType shadowType = ShadowType.Illuminated)
        {
            var op = new ShadowOp();
            op.Caster = vertices;
            op.Offset = _shadowVertices.Count;
            op.ShadowType = shadowType;

            _shadowOps.Add(op);

            unsafe
            {
                var trans = c.Matrix;
                var tv = new Vector2(tx, ty);

                for (int i = 0; i < vertices.Length; ++i)
                {
                    var sv = vertices[i];
                    sv += tv;

                    var lv = new Vector2(
                        sv.X * trans._ix + sv.Y * trans._jx + trans._tx,
                        sv.X * trans._iy + sv.Y * trans._jy + trans._ty);

                    _shadowVertices.Add(lv);
                }
            }
        }

       
        public void AddPointLight(Canvas c, float x, float y, float range, float intensity, float size)
        {
            Vector2 location = c.Matrix.TransformPoint( new Vector2(x, y));

            _pointLights.Add(new PointLightOp()
            {
                Color = c.Color,
                Alpha  = c.Alpha,
                Location = location,
                Transform = c.Matrix,
                Range = range,
                Intensity = intensity,
                Size = size
            });
        }

        public void AddSpotLight(Canvas c, float inner, float outer, float range, float intensity, float size)
        {
            _spotLights.Add(new SpotLightOp()
            {
                Color = c.Color,
                Alpha = c.Alpha,
                Location = new Vector2(c.Matrix._tx, c.Matrix._ty),
                Transform = c.Matrix,
                Range = range,
                Intensity = intensity,
                Inner = inner * DEG_TO_RAD,
                Outer = outer * DEG_TO_RAD,
                Dir = new Vector2(c.Matrix._ix, c.Matrix._iy),
                Size = size
            });
        }

        private void DrawShadows(Vector2 lv, float range)
        {
            // clear shadow map
            _canvas.RenderTarget = _shadowmap;
            _canvas.Clear(Color.Black);

            unsafe
            { 
                foreach (var op in _shadowOps)
                {
                    bool cast_shadow = false;
                    for (int i = 0; i < op.Caster.Length; ++i)
                    {
                        if (Vector2.DistanceSquared(_shadowVertices[op.Offset + i], lv) < range)
                        {
                            cast_shadow = true;
                            break;
                        }
                    }

                    if (cast_shadow)
                    {
                       _shadowRenderer.OnDrawShadow(_canvas, op.ShadowType, op.Offset, op.Caster.Length, _shadowVertices);
                    }
                }

            }

            if (_canvas.CanFlush)
            {
                foreach (var sop in _shadowOps)
                {
                    var blend = sop.ShadowType == ShadowType.Illuminated ? 
                        BlendMode.Opaque : BlendMode.Subtract;
                   
                    var vert0 = sop.Offset;
                    var nverts = sop.Caster.Length;

                    unsafe
                    {
                        var count = sop.Caster.Length;
                        var ptr = _canvas.AddDrawOp(count, 1, null, _canvas.DefaultEffect, blend);
                        for (int i = 0; i < count; ++i)
                        {
                            var v = _shadowVertices[sop.Offset + i];
                            ptr[i].Transform(v.X, v.Y, Color.Black);
                        }
                    }
                    
                }

                _canvas.Flush();
            }
        }

        public static Image CreateImage(int w, int h, ref Image img)
        {
            if (img != null && (img.Width != w || img.Height != h))
            {
                img.Dispose();
                img = null;
            }
            if (img == null)
            {
                img = new Image(w, h, RenderTargetUsage.PreserveContents);
            }
            return img;
        }

        public void Resize(int width, int height)
        {
            if (_width != width || _height != height)
            {
                _width = width;
                _height = height;

                CreateImage(_width, _height, ref _shadowmap);
                _shadowmap.RenderTarget.Name = "_shadowmap";

                CreateImage(_width, _height, ref _lightmap);
                _lightmap.RenderTarget.Name = "_lightmap";

                _spotLightEffect.InvTexSize = new Vector2(1.0f / _width, 1.0f / _height);
                _pointLightEffect.InvTexSize = new Vector2(1.0f / _width, 1.0f / _height);
            }
        }

        public void Render()
        {
            _canvas.Begin();
            _canvas.PushMatrix();

            // fill lightmap with ambient color
            Global.Device.SetRenderTarget(_lightmap);
            Global.Device.Clear(AmbientColor);

            // clear shadow map
            Global.Device.SetRenderTarget(_shadowmap);
            Global.Device.Clear(Color.White);

            _shadowRenderer.OnUpdateFrame(_canvas);

            // render pointlights 
            foreach (var op in _pointLights)
            {
                _shadowRenderer.OnUpdateLight(op);
       
                DrawShadows(op.Location, op.Range*op.Range);
                DrawPointLight(op as PointLightOp);
            }

            // render spotlights
            foreach ( var op in _spotLights)
            {
                _shadowRenderer.OnUpdateLight(op);

                DrawShadows(op.Location, op.Range * op.Range);
                DrawSpotLight(op as SpotLightOp);
            }

            _canvas.Effect = _canvas.DefaultEffect;
            _canvas.PopMatrix();
            _canvas.End();
        }

        public void Reset()
        {
            _shadowVertices.Clear();
            _spotLights.Clear();
            _pointLights.Clear();
            _shadowOps.Clear();
        }

        private void DrawPointLight(PointLightOp op)
        {
            _pointLightEffect.Shadowmap = _shadowmap;
            _pointLightEffect.WorldViewProj = _canvas.WorldViewProj;
            _pointLightEffect.Range = op.Range;
            _pointLightEffect.Intensity = op.Intensity;
            _pointLightEffect.Position = new Vector2(op.Location.X, op.Location.Y);
            _pointLightEffect.CurrentTechnique.Passes.First().Apply();

            unsafe
            {
                fixed (MojoVertex* ptr = &_lightVertices[0])
                {
                    ptr[0].Transform(-op.Range, -op.Range, op.Transform, op.Color);
                    ptr[1].Transform(-op.Range + op.Range * 2, -op.Range, op.Transform, op.Color);
                    ptr[2].Transform(-op.Range + op.Range * 2, -op.Range + op.Range * 2, op.Transform, op.Color);
                    ptr[3].Transform(-op.Range, -op.Range + op.Range * 2, op.Transform, op.Color);
                }
            }

            Global.Device.SetRenderTarget(_lightmap);
            Global.Device.BlendState = MojoBlend.BlendAdd;
            Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                         PrimitiveType.TriangleList, _lightVertices, 0, 4,
                         Global.QuadIndices, 0, 2);
        }

        private void DrawSpotLight(SpotLightOp op)
        {
            _spotLightEffect.Shadowmap = _shadowmap;
            _spotLightEffect.WorldViewProj = _canvas.WorldViewProj;
            _spotLightEffect.Range = op.Range;
            _spotLightEffect.Intensity = op.Intensity;
            _spotLightEffect.Position = new Vector2(op.Location.X, op.Location.Y);
            _spotLightEffect.Inner = op.Inner;
            _spotLightEffect.Outer = op.Outer;
            _spotLightEffect.LightDir = op.Dir;
            _spotLightEffect.CurrentTechnique.Passes.First().Apply();

            var transform = op.Transform;
            transform.Translate(0, -op.Range);

            unsafe
            {
                fixed (MojoVertex* ptr = &_lightVertices[0])
                {
                    ptr[0].Transform(0, 0, transform, op.Color);
                    ptr[1].Transform(0 + op.Range, 0, transform, op.Color);
                    ptr[2].Transform(0 + op.Range, 0 + op.Range * 2, transform, op.Color);
                    ptr[3].Transform(0, 0 + op.Range * 2, transform, op.Color);
                }
            }

            Global.Device.SetRenderTarget(_lightmap);
            Global.Device.BlendState = MojoBlend.BlendAdd;
            Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                         PrimitiveType.TriangleList, _lightVertices, 0, 4,
                         Global.QuadIndices, 0, 2);
        }

    }
}
