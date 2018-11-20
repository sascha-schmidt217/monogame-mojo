using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Mojo.Graphics
{
    public interface ILightRenderer
    {
        Image LightMap { get; }
        Color AmbientColor { get; set; }
        void Resize(int width, int height);
        void Render();
        void Reset();
        void AddShadowCaster(Transform2D mat, Vector2[] vertices, float tx, float ty, ShadowType shadowType = ShadowType.Illuminated);
        void AddPointLight(Transform2D mat, Color c, float range, float intensity, float size);
        void AddSpotLight(Transform2D mat, Color c, float inner, float outer, float range, float intensity, float size);
    }

    public static class Global
    {
        public const int MAX_VERTS = 16536;
        public const int MAX_LINES = MAX_VERTS / 2;
        public const int MAX_QUADS = MAX_VERTS / 4;

        internal static Game Game;
        public static ContentManager Content;
        internal static GraphicsDevice Device => Game.GraphicsDevice;
        internal static DynamicIndexBuffer _iboQuad;
        internal static DynamicIndexBuffer _iboFan;

        internal static Int16[] QuadIndices;
        internal static Int16[] FanIndices;
        internal static RasterizerState RasterizerStateScissor;


        public static void Initialize(Game game)
        {
            Game = game;
            Content = game.Content;

            RasterizerStateScissor = new RasterizerState();
            RasterizerStateScissor.ScissorTestEnable = true;
            RasterizerStateScissor.SlopeScaleDepthBias = 100.0f;
            RasterizerStateScissor.CullMode = CullMode.None;

            QuadIndices = new Int16[MAX_QUADS * 6];
            for (int i = 0; i < MAX_QUADS; ++i)
            {
                QuadIndices[i * 6 + 0] = (short)(i * 4 + 0);
                QuadIndices[i * 6 + 1] = (short)(i * 4 + 1);
                QuadIndices[i * 6 + 2] = (short)(i * 4 + 2);
                QuadIndices[i * 6 + 3] = (short)(i * 4 + 0);
                QuadIndices[i * 6 + 4] = (short)(i * 4 + 2);
                QuadIndices[i * 6 + 5] = (short)(i * 4 + 3);
            }

            FanIndices = new Int16[MAX_VERTS * 3];
            for (int i = 0; i < MAX_VERTS; ++i)
            {
                FanIndices[i * 3 + 0] = (short)(0);
                FanIndices[i * 3 + 1] = (short)(i + 1);
                FanIndices[i * 3 + 2] = (short)(i + 2);
            }
        }

        public static void LoadContent()
        {
            _iboQuad = new DynamicIndexBuffer(Device, IndexElementSize.SixteenBits, MAX_QUADS * 6, BufferUsage.WriteOnly);
            _iboQuad.SetData(QuadIndices, 0, MAX_QUADS * 6);
            _iboFan = new DynamicIndexBuffer(Device, IndexElementSize.SixteenBits, MAX_QUADS * 3, BufferUsage.WriteOnly);
            _iboFan.SetData(FanIndices, 0, MAX_QUADS * 3);
        }

        public static RenderTarget2D CreateRendertarget(int w, int h, ref RenderTarget2D img)
        {
            if (img != null && (img.Width != w || img.Height != h))
            {
                img.Dispose();
            }
            if (img == null)
            {
                img = new RenderTarget2D(Device, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
            return img;
        }

        public static Image CreateRenderImage(int w, int h, ref Image img, RenderTargetUsage usage = RenderTargetUsage.DiscardContents)
        {
            if (img != null && (img.Width != w || img.Height != h))
            {
                img.Dispose();
                img = null;
            }
            if (img == null)
            {
                img = new Image(w, h, usage);
            }
            return img;
        }

        public static void SaveRendertarget(RenderTarget2D rendertarget, string file)
        {
            using (var stream = new System.IO.FileStream(file,
                System.IO.FileMode.OpenOrCreate))
            {
                rendertarget.SaveAsPng(stream, rendertarget.Width, rendertarget.Height);
            }
        }
    }

    public class Quad
    {
        // precalculated vertex coordinates
        public Vector2 vertex0;
        public Vector2 vertex1;
        public Vector2 vertex2;
        public Vector2 vertex3;
    }

    public class TexturedQuad
    {
        // precalculated texture coordinates
        public float u0;
        public float u1;
        public float v0;
        public float v1;

        // precalculated vertex coordinates
        public Vector2 vertex0;
        public Vector2 vertex1;
        public Vector2 vertex2;
        public Vector2 vertex3;
    };
   

    public struct Transform2D
    {
        private static Transform2D _inverseTransform = new Transform2D();

        public float _ix, _iy, _jx, _jy, _tx, _ty;
        public bool _tFormed;

        public Vector2 TransformPoint(Vector2 v)
        {
            Vector2 dst = new Vector2();
            dst.X = v.X * _ix + v.Y * _jx + _tx;
            dst.Y = v.X * _iy + v.Y * _jy + _ty;
            return dst;
        }

        public void Reset()
        {
            _ix = 1.0f;
            _iy = 0.0f;
            _jx = 0.0f;
            _jy = 1.0f;
            _tx = 0.0f;
            _ty = 0.0f;
        }

        public void Transform(float ix, float iy, float jx, float jy, float tx, float ty)
        {
            float ix2 = ix * _ix + iy * _jx;
            float iy2 = ix * _iy + iy * _jy;
            float jx2 = jx * _ix + jy * _jx;
            float jy2 = jx * _iy + jy * _jy;
            float tx2 = tx * _ix + ty * _jx + _tx;
            float ty2 = tx * _iy + ty * _jy + _ty;
            SetMatrix(ix2, iy2, jx2, jy2, tx2, ty2);
        }

        public void SetMatrix(float ix, float iy, float jx, float jy, float tx, float ty)
        {
            _ix = ix;
            _iy = iy;
            _jx = jx;
            _jy = jy;
            _tx = tx;
            _ty = ty;
            _tFormed = (ix != 1 || iy != 0 || jx != 0 || jy != 1 || tx != 0 || ty != 0);
        }

        public void SetMatrix(Transform2D _trans)
        {
            this = _trans;
            _tFormed = (_trans._ix != 1 || _trans._iy != 0 || _trans._jx != 0 || _trans._jy != 1 || _trans._tx != 0 || _trans._ty != 0);
        }

        public void Rotate(float angle)
        {
            float radians = angle * 0.0174532925199432957692369076848861f;
            Transform((float)Math.Cos(radians), -(float)Math.Sin(radians), (float)Math.Sin(radians), (float)Math.Cos(radians), 0, 0);
        }

        public void TranslateRotateScale(float tx, float ty, float sx, float sy, float rz)
        {
            Translate(tx, ty);
            Rotate(rz);
            Scale(sx, sy);
        }

        public void Translate(float x, float y)
        {
            Transform(1, 0, 0, 1, x, y);
        }

        public void Translate(int x, int y)
        {
            Transform(1, 0, 0, 1, (float)x, (float)y);
        }

        public void Scale(float x, float y)
        {
            Transform(x, 0, 0, y, 0, 0);
        }

        public Transform2D InverseTransform
        {
            get
            {
                float m00 = _ix;
                float m10 = _jx;
                float m20 = _tx;
                float m01 = _iy;
                float m11 = _jy;
                float m21 = _ty;
                float det = m00 * m11 - m01 * m10;
                float idet = 1.0f / det;
                float r00 = m11 * idet;
                float r10 = -m10 * idet;
                float r20 = (m10 * m21 - m11 * m20) * idet;
                float r01 = -m01 * idet;
                float r11 = m00 * idet;
                float r21 = (m01 * m20 - m00 * m21) * idet;

                _ix = r00;
                _jx = r10;
                _tx = r20;
                _iy = r01;
                _jy = r11;
                _ty = r21;

                return _inverseTransform;
            }
        }

    };

    public static class MojoBlend
    {
        public static BlendState CreateBlendState(BlendFunction func, Blend colorSrc, Blend alphaSrc, Blend colorDst, Blend alphaDst)
        {
            var blendState = new BlendState();
            blendState.ColorBlendFunction = func;
            blendState.ColorSourceBlend = colorSrc;
            blendState.AlphaSourceBlend = alphaSrc;
            blendState.ColorDestinationBlend = colorDst;
            blendState.AlphaDestinationBlend = alphaDst;
            return blendState;
        }

        public static BlendState BlendShadow = new BlendState
        {
            ColorWriteChannels = ColorWriteChannels.Alpha,
            AlphaBlendFunction = BlendFunction.ReverseSubtract,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState BlendAdd = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState BlendAlpha = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState BlendMultiply = new BlendState
        {
            //ColorSourceBlend = Blend.DestinationColor,
            //ColorDestinationBlend = Blend.InverseSourceAlpha,
            //ColorBlendFunction = BlendFunction.Add,
            //AlphaSourceBlend = Blend.SourceAlpha,
            //AlphaDestinationBlend = Blend.InverseSourceAlpha
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };

        public static BlendState BlendScreen = new BlendState
        {
            ColorSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        public static BlendState BlendDarken = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Min,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        public static BlendState BlendLighten = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Max,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        public static BlendState BlendLinearDodge = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
        public static BlendState BlendLienarBurn = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorBlendFunction = BlendFunction.ReverseSubtract,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };
    }

}