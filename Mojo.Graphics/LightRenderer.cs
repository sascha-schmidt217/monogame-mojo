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
    public enum ShadowType
    {
        Solid,
        Illuminated
    }

    public interface IShadowRenderer
    {
        int ShadowCount { get;  }
        MojoVertex[] ShadowBuffer { get;  }
        Effect Effect { get; }
        Matrix Projection { set; }

        void OnLoad();
        void UpdateLight(Vector2 location, float size);
        void AddShadowVertices(ShadowType type, List<Vector2> vertices, int start, int length);
    }

    public class ShadowOp
    {
        public ShadowType ShadowType { get; set; } = ShadowType.Illuminated;
        public int Offset { get; set; }
        public int Length { get; set; }
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
        private List<Vector2> _shadowVertices = new List<Vector2>(16536);

        private Matrix _projection;
        private BasicEffect _defaultEffect;
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
        private MojoVertex[] _shadowCasterVertices = new MojoVertex[4096];

        public Color AmbientColor { get; set; }

        public LightRenderer()
        {
            _spotLightEffect = new SpotLightEffect(Global.Content.Load<Effect>("Effects/spot_light"));
            _pointLightEffect = new PointLightEffect(Global.Content.Load<Effect>("Effects/point_light"));
            _defaultEffect = new BasicEffect(Global.Device);
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

        public void AddShadowCaster(Transform2D mat, Vector2[] vertices, float tx, float ty, ShadowType shadowType = ShadowType.Illuminated)
        {
            var op = new ShadowOp();
            op.ShadowType = shadowType;
            op.Offset = _shadowVertices.Count;
            op.Length = vertices.Length;
           
            _shadowOps.Add(op);

            unsafe
            {
                var tv = new Vector2(tx, ty);

                for (int i = 0; i < vertices.Length; ++i)
                {
                    var sv = vertices[i];
                    sv += tv;
                    
                    var lv = new Vector2(
                        sv.X * mat._ix + sv.Y * mat._jx + mat._tx,
                        sv.X * mat._iy + sv.Y * mat._jy + mat._ty);

                    _shadowVertices.Add(lv);
                }
            }
        }

       
        public void AddPointLight(Transform2D mat, Color c, float range, float intensity, float size)
        {
            _pointLights.Add(new PointLightOp()
            {
                Color = c,
                Location = new Vector2(mat._tx, mat._ty),
                Transform = mat,
                Range = range,
                Intensity = intensity,
                Size = size
            });
        }

        public void AddSpotLight(Transform2D mat, Color c, float inner, float outer, float range, float intensity, float size)
        {
            _spotLights.Add(new SpotLightOp()
            {
                Color = c,
                Location = new Vector2(mat._tx, mat._ty),
                Transform = mat,
                Range = range,
                Intensity = intensity,
                Inner = inner * DEG_TO_RAD,
                Outer = outer * DEG_TO_RAD,
                Dir = new Vector2(mat._ix, mat._iy),
                Size = size
            });
        }

        private void DrawShadows(Vector2 lv, float size, float range)
        {
            // clear shadow map
            // 
            Global.Device.SetRenderTarget(_shadowmap);
            Global.Device.Clear(Color.Black);

            _shadowRenderer.UpdateLight(lv,size);

            unsafe
            {
                var vertexArray = _shadowVertices;
                foreach (var op in _shadowOps)
                {
                    for (int i = 0; i < op.Length; ++i)
                    {
                        if (Vector2.DistanceSquared(vertexArray[op.Offset + i], lv) < range)
                        {
                            _shadowRenderer.AddShadowVertices(op.ShadowType, vertexArray, op.Offset, op.Length);
                            break;
                        }
                    }
                }

                if (_shadowRenderer.ShadowCount > 0)
                {

                    // Draw shadow geometry
                    //
                    _shadowRenderer.Effect.CurrentTechnique.Passes.First().Apply();
                    Global.Device.BlendState = MojoBlend.BlendShadow;
                    Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                                PrimitiveType.TriangleList, _shadowRenderer.ShadowBuffer, 0, _shadowRenderer.ShadowCount * 4,
                                Global.QuadIndices, 0, _shadowRenderer.ShadowCount * 2);


                    // Draw shadow casters, considering ShadowType
                    //
                   
                    _defaultEffect.CurrentTechnique.Passes.First().Apply();

                    foreach (var sop in _shadowOps)
                    {
                        Global.Device.BlendState =  sop.ShadowType == ShadowType.Illuminated ?
                            BlendState.Opaque : MojoBlend.BlendShadow;

                        fixed (MojoVertex* ptr = &_shadowCasterVertices[0])
                        {
                            int len = sop.Length;
                            for (int i = 0; i < len; ++i)
                            {
                                var v = _shadowVertices[sop.Offset + i];
                                ptr[i].Transform(v.X, v.Y, Color.Black);
                            }
                        }

                        Global.Device.DrawUserIndexedPrimitives<MojoVertex>(PrimitiveType.TriangleList,
                            _shadowCasterVertices, 0, sop.Length, Global.FanIndices, 0, sop.Length - 2);
                    }

                }
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
                _projection = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(+.0f, width + .0f, height + .0f, +.0f, 0, 1);
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
            _shadowRenderer.Projection = _projection;
            _defaultEffect.Projection = _projection;

            // fill lightmap with ambient color
            Global.Device.SetRenderTarget(_lightmap);
            Global.Device.Clear(AmbientColor);

            // clear shadow map
            Global.Device.SetRenderTarget(_shadowmap);
            Global.Device.Clear(Color.White);

            // render pointlights 
            foreach (var op in _pointLights)
            {
                DrawShadows(op.Location, op.Size, op.Range*op.Range);
                DrawPointLight(op as PointLightOp);
            }

            // render spotlights
            foreach ( var op in _spotLights)
            {
                DrawShadows(op.Location, op.Size, op.Range * op.Range);
                DrawSpotLight(op as SpotLightOp);
            }
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
            _pointLightEffect.WorldViewProj = _projection;
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
            _spotLightEffect.WorldViewProj = _projection;
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
