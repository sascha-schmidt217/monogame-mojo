using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    public enum ShadowType
    {
        Solid,
        Illuminated,
        Occluded
    }

    public interface IShadowRenderer
    {
        int ShadowCount { get;  }
        MojoVertex[] ShadowBuffer { get;  }
        Effect Effect { get; }
        Matrix Projection { set; }

        void OnLoad();
        void UpdateLight(Vector2 location, float size);
        bool AddShadowVertices(ShadowType type, List<Vector2> vertices, int start, int length);
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
        public float Depth;
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

    class SimplePool<T>  where T : new() 
    {
        private List<T> _pool;
        private int _poolIndex = 0;

        public SimplePool(int size)
        {
            if (size <= 0)
                throw new Exception();

            _pool = new List<T>(size);
            for (int i = 0; i < size; ++i)
            {
                _pool.Add(new T());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            if (_poolIndex >= _pool.Count)
            {
                int cnt = _pool.Count;
                for (int i = 0; i < cnt; ++i)
                {
                    _pool.Add(new T());
                }
            }
            return _pool[_poolIndex++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _poolIndex = 0;
        }
    }

    public class LightRenderer : ILightRenderer
    {
        const float DEG_TO_RAD = 0.0174532925199432957692369076848861f;

        public static BlendState LightBlend = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };

        private int _width = 0;
        private int _height = 0;

        private IShadowRenderer _shadowRenderer = new PenumbraShadow();

        private List<PointLightOp> _pointLights = new List<PointLightOp>(1024);
        private List<SpotLightOp> _spotLights = new List<SpotLightOp>(1024);
        private List<ShadowOp> _shadowOps = new List<ShadowOp>(1024);
        private List<Vector2> _shadowVertices = new List<Vector2>(65536);

        private readonly MojoVertex[] _lightVertices = new MojoVertex[4];
        private readonly MojoVertex[] _shadowCasterVertices = new MojoVertex[4096];

        private Matrix _projection;
        private BasicEffect _defaultEffect;
        private LightEffect _lightEffect;
        private Image _lightmap;
        private Image _shadowmap;

        private SimplePool<ShadowOp> _shadowOpPool = new SimplePool<ShadowOp>(4096);

        public LightRenderer()
        {
            _lightEffect = new LightEffect(Global.Content.Load<Effect>("Effects/light"));
            _defaultEffect = new BasicEffect(Global.Device);
            _shadowRenderer.OnLoad();

            for(int i = 0; i< _shadowCasterVertices.Length; ++i)
            {
                _shadowCasterVertices[i].Color = Color.Black;
            }
        }

        public void AddShadowCaster(Transform2D mat, ShadowCaster caster, float tx, float ty)
        {
            AddShadowCaster(mat, caster.Vertices, tx, ty, caster.ShadowType);
        }


        public void AddShadowCaster(Transform2D mat, Vector2[] vertices, float tx, float ty, ShadowType shadowType = ShadowType.Illuminated)
        {
            var op = _shadowOpPool.Get();
            op.ShadowType = shadowType;
            op.Offset = _shadowVertices.Count;
            op.Length = vertices.Length;

            _shadowOps.Add(op);

            unsafe
            {
                var tv = new Vector2(tx, ty);

                for (int i = 0; i < vertices.Length; ++i)
                {
                    var sv = vertices[i] + tv;

                    _shadowVertices.Add(new Vector2(
                        sv.X * mat._ix + sv.Y * mat._jx + mat._tx,
                        sv.X * mat._iy + sv.Y * mat._jy + mat._ty));
                }
            }
        }

        public void AddPointLight(Transform2D mat, Color c, float range, float intensity, float size, float depth )
        {
            _pointLights.Add(new PointLightOp()
            {
                Color = c,
                Location = new Vector2(mat._tx, mat._ty),
                Transform = mat,
                Range = range,
                Intensity = intensity,
                Size = size,
                Depth = depth
            });
        }

        public void AddSpotLight(Transform2D mat, Color c, float inner, float outer, float range, float intensity, float size, float depth )
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
                Size = size,
                Depth = depth
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
                bool hasShadows = false;
                var vertexArray = _shadowVertices;
                foreach (var op in _shadowOps)
                {
                    for (int i = 0; i < op.Length; ++i)
                    {
                        // shadow caster is visible from light?
                        if (Vector2.DistanceSquared(vertexArray[op.Offset + i], lv) < range)
                        {
                            while(!_shadowRenderer.AddShadowVertices(op.ShadowType, vertexArray, op.Offset, op.Length))
                            {
                                Global.Device.BlendState = MojoBlend.BlendShadow;
                                Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                                            PrimitiveType.TriangleList, _shadowRenderer.ShadowBuffer, 0, _shadowRenderer.ShadowCount * 4,
                                            Global.QuadIndices, 0, _shadowRenderer.ShadowCount * 2);

                                _shadowRenderer.UpdateLight(lv, size);
                            }
                            hasShadows = true;
                            break;
                        }
                    }
                }

                if (hasShadows)
                {

                    // Draw shadow geometry
                    //
                    if (_shadowRenderer.ShadowCount > 0)
                    {
                        _shadowRenderer.Effect.CurrentTechnique.Passes.First().Apply();
                        Global.Device.BlendState = MojoBlend.BlendShadow;
                        Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                                    PrimitiveType.TriangleList, _shadowRenderer.ShadowBuffer, 0, _shadowRenderer.ShadowCount * 4,
                                    Global.QuadIndices, 0, _shadowRenderer.ShadowCount * 2);

                    }

                    // cutting out the shadow caster from the shadow volume
                    //
                    bool _defaultShaderInitialize = false;

                    foreach (var sop in _shadowOps)
                    {

                        if (sop.ShadowType != ShadowType.Occluded) 
                        {
                            if(!_defaultShaderInitialize)
                            {
                                _defaultShaderInitialize = true;
                                _defaultEffect.CurrentTechnique.Passes.First().Apply();
                            }

                            switch (sop.ShadowType)
                            {
                                case ShadowType.Illuminated:
                                    Global.Device.BlendState = BlendState.Opaque;
                                    break;
                                case ShadowType.Solid:
                                    Global.Device.BlendState = MojoBlend.BlendShadow;
                                    break;
                            }

                            fixed (MojoVertex* ptr = &_shadowCasterVertices[0])
                            {
                                int len = Math.Min(_shadowCasterVertices.Length,  sop.Length);
                                for (int i = 0; i < len; ++i)
                                {
                                    ptr[i].Position = _shadowVertices[sop.Offset + i];
                                }
                            }

                            Global.Device.DrawUserIndexedPrimitives<MojoVertex>(PrimitiveType.TriangleList,
                                    _shadowCasterVertices, 0, sop.Length, Global.FanIndices, 0, sop.Length - 2);
                        }
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
                img = new Image(w, h, RenderTargetUsage.PreserveContents, SurfaceFormat.Color);
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

                _lightEffect.InvTexSize = new Vector2(1.0f / _width, 1.0f / _height);
            }
        }

        public RenderTarget2D Render(RenderTarget2D normapMap, Color ambientColor, bool shadowEnabled, bool normalmapEnabled)
        {
            _lightEffect.ShadowEnabled = shadowEnabled;
            _lightEffect.NormalmapEnabled = normalmapEnabled;
            _lightEffect.WorldViewProj = _projection;

            if (shadowEnabled)
            {
                _shadowRenderer.Projection = _projection;
                _defaultEffect.Projection = _projection;
            }

            if (normalmapEnabled)
            {
                _lightEffect.Normalmap = normapMap;
            }

            // fill lightmap with ambient color
            Global.Device.SetRenderTarget(_lightmap);
            Global.Device.Clear(new Color(ambientColor, 0.0f));

            // clear shadow map
            Global.Device.SetRenderTarget(_shadowmap);
            Global.Device.Clear(Color.White);

            if (shadowEnabled)
            {
                // render pointlights 
                _lightEffect.UseSpotLight = false;
                foreach (var op in _pointLights)
                {
                    DrawShadows(op.Location, op.Size, op.Range * op.Range);

                    _lightEffect.Shadowmap = _shadowmap;

                    DrawPointLight(op as PointLightOp);
                }

                // render spotlights
                _lightEffect.UseSpotLight = true;
                foreach (var op in _spotLights)
                {
                    DrawShadows(op.Location, op.Size, op.Range * op.Range);

                    _lightEffect.Shadowmap = _shadowmap;

                    DrawSpotLight(op as SpotLightOp);
                }
            }
            else
            {
                // render pointlights 
                _lightEffect.UseSpotLight = false;
                foreach (var op in _pointLights)
                {
                    DrawPointLight(op as PointLightOp);
                }

                // render spotlights
                _lightEffect.UseSpotLight = true;
                foreach (var op in _spotLights)
                {
                    DrawSpotLight(op as SpotLightOp);
                }
            }

            Reset();

            return _lightmap;
        }

        public void Reset()
        {
            _shadowOpPool.Reset();
            _shadowVertices.Clear();
            _spotLights.Clear();
            _pointLights.Clear();
            _shadowOps.Clear();
        }

        private void DrawPointLight(PointLightOp op)
        {
            _lightEffect.Range = op.Range;
            _lightEffect.Intensity = op.Intensity;
            _lightEffect.Position = new Vector2(op.Location.X, op.Location.Y);
            _lightEffect.Depth = op.Depth;
            _lightEffect.CurrentTechnique.Passes.First().Apply();

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
            Global.Device.BlendState = LightBlend;
            Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                         PrimitiveType.TriangleList, _lightVertices, 0, 4,
                         Global.QuadIndices, 0, 2);
        }

        private void DrawSpotLight(SpotLightOp op)
        {
            _lightEffect.Range = op.Range;
            _lightEffect.Intensity = op.Intensity;
            _lightEffect.Position = new Vector2(op.Location.X, op.Location.Y);
            _lightEffect.Inner = op.Inner;
            _lightEffect.Outer = op.Outer;
            _lightEffect.LightDir = op.Dir;
            _lightEffect.Depth = op.Depth;
            _lightEffect.CurrentTechnique.Passes.First().Apply();

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
            Global.Device.BlendState = LightBlend;
            Global.Device.DrawUserIndexedPrimitives<MojoVertex>(
                         PrimitiveType.TriangleList, _lightVertices, 0, 4,
                         Global.QuadIndices, 0, 2);
        }

    }
}
