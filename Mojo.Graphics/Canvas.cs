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
    public enum PrimType
    {
        Point = 1,
        Line = 2,
        Tri = 3,
        Quad = 4
    }

    public enum OutlineMode
    {
        None = 0,
        Solid = 1,
        Smooth = 2
    }

    public enum BlendMode
    {
        None,
        Opaque,
        Alpha,
        Premultiplied,
        InverseAlpha,
        Additive,
        Subtract,
        Multiply,
        Screen,
        Darken,
        Lighten,
        LiearDodge,
        LinearBurn
    }

    public class DrawOp
    {
        public Image img;
        public BlendMode blendMode;
        public Effect effect;
        public int primType;
        public int primCount;
        public int primOffset;
    }

    public class Canvas : IDisposable
    {
        // internal state
        private readonly bool initialized = false;
        private Stack<Transform2D> _matrixStack = new Stack<Transform2D>();
        private Transform2D _transform = new Transform2D();
        private bool _lighting = false;
        private Rectangle _scissorRect;
        private Buffer _drawBuffer { get; set; }
        private Buffer _defaultBuffer;
        private BlendMode _blendMode = BlendMode.None;
        private float _alpha = 1.0f;
        private Color _color;
        private Color _internalColor = Color.White;
        private Image _originalRendertarget = null;
        private RenderTarget2D _currentRendertarget = null;
        private DrawOp _drawOp = new DrawOp();

        // G-Buffer
        private RenderTargetBinding[] _gBuffer;
        private Image _diffuseMap;
        private Image _normalMap;
        private Image _specularMap;

        // currently active shader when not in lighting mode
        private Effect _currentEffect;

        // shader that combines lighting and diffuse, 
        // in order to create the final image
        private Effect _lightingEffect;

        // shader used to render to G-Buffer
        private DefaultEffect _defaultEffect;

        // light renderer: G-Buffer => Lighting 
        private ILightRenderer _lightRenderer;

        // image of the currectly active font
        private Image _fontImage;
        private SpriteFont _spriteFont;

        ///////////////////////////////////////////////
        ///
        public GraphicsDevice Device { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool LineSmooth { get; set; } = false;
        public OutlineMode OutlineMode { get; set; } = OutlineMode.None;
        public Color OutlineColor { get; set; } = Color.White;
        public float OutlineWidth { get; set; } = 2;

        /// <summary>
        /// The current drawing matrix
        /// </summary>
        public Transform2D Matrix
        {
            get
            {
                return _transform;
            }
            set
            {
                SetMatrix(value);
            }
        }

        /// <summary>
        /// Enables or disables a debug overlay, which displays the G-Buffer.
        /// </summary>
        public bool ShowGBuffer { get; set; } = false;

        /// <summary>
        /// Enables or disables shadow volumes.
        /// </summary>
        public bool ShadowEnabled { get; set; } = true;

        /// <summary>
        /// Enables or disables normal mapping..
        /// </summary>
        public bool NormalmapEnabled { get; set; } = true;

        /// <summary>
        /// Enables or disables specular mapping..
        /// </summary>
        public bool SpecularEnabled { get; set; } = true;

        /// <summary>
        /// The current world view projection matrix.
        /// </summary>
        public Matrix WorldViewProj { get; set; }

        /// <summary>
        /// Sets the ambient light color for lighting.
        /// </summary>
        public Color AmbientColor { get; set; } = new Color(32, 32, 32);

        /// <summary>
        /// Texture filtering enabled state.
        /// </summary>
        public bool TextureFilteringEnabled { get; set; } = false;

        /// <summary>
        /// The current point size for use with DrawPoint.
        /// </summary>
        public float PointSize { get; set; } = 1.0f;

        /// <summary>
        /// The current line width for use with DrawLine.
        /// </summary>
        public float LineWidth { get; set; } = 1.0f;

        /// <summary>
        /// Creates a canvas that renders to an image.
        /// </summary>
        public Canvas(Image img)
        {
            Device = Global.Game.GraphicsDevice;
            Initialize(img);
        }

        /// <summary>
        /// Creates a canvas that renders to the backbuffer.
        /// </summary>
        public Canvas()
        {
            Device = Global.Game.GraphicsDevice;
            Initialize(null);
        }

        public ILightRenderer LightRenderer
        {
            get
            {
                if (_lightRenderer == null)
                {
                    _lightRenderer = new LightRenderer();
                }
                return _lightRenderer;
            }
            private set
            {
                _lightRenderer = value;
            }
        }

        /// <summary>
        ///  The current font for use with DrawText. 
        /// </summary>
        public SpriteFont Font
        {
            get
            {
                return _spriteFont;
            }
            set
            {
                if (_spriteFont != value)
                {
                    _spriteFont = value;
                    _fontImage = new Image(value.Texture);
                }
            }
        }

        /// <summary>
        /// The current Effect.
        /// </summary>
        public Effect Effect
        {
            get
            {
                return _currentEffect;
            }
            set
            {
                if (value != _currentEffect)
                {
                    Flush();

                    if (value == null)
                    {
                        _currentEffect = _defaultEffect;
                    }
                    else
                    {
                        _currentEffect = value;
                    }
                }
            }
        }

        /// <summary>
        /// The current render target.
        /// </summary>
        public RenderTarget2D RenderTarget
        {
            get
            {
                return _currentRendertarget;
            }
            set
            {
                if (value == null)
                {
                    SetRenderTargetInternal(_originalRendertarget);
                }
                else
                {
                    SetRenderTargetInternal(value);
                }
            }
        }


        public void Flush(bool preserveBuffer = false)
        {
            if (_drawBuffer.DrawOps.Count == 0)
            {
                return;
            }

            if (_lighting)
            {
                // diffuse + normal
                //
                Device.SetRenderTargets(_gBuffer);
                RenderDrawOps();

                // TODO
                // normal
                //
                //Device.SetRenderTarget(_normalMap);
                //RenderDrawOps();

                // back to default rendertarget
                //
                Device.SetRenderTarget(RenderTarget);
            }
            else
            {
                RenderDrawOps();
            }

            if (!preserveBuffer)
            {
                _drawBuffer.Clear();
                _drawOp = new DrawOp();
            }
        }

        public void ClearBuffer()
        {
            _drawBuffer.Clear();
            _drawOp = new DrawOp();
        }

        /// <summary>
        /// Translates the drawing matrix.
        /// </summary>
        public void Translate(float x, float y)
        {
            _transform.Transform(1, 0, 0, 1, x, y);
        }

        /// <summary>
        /// Translates the drawing matrix.
        /// </summary>
        public void Translate(int x, int y)
        {
            _transform.Transform(1, 0, 0, 1, (float)x, (float)y);
        }

        /// <summary>
        /// Scales the drawing matrix.
        /// </summary>
        public void Scale(float x, float y)
        {
            _transform.Transform(x, 0, 0, y, 0, 0);
        }

        public void TranslateRotateScale(float tx, float ty, float sx, float sy, float rz)
        {
            _transform.TranslateRotateScale(tx, ty, sx, sy, rz);
        }

        /// <summary>
        ///  Rotates the drawing matrix.
        /// </summary>
        public void Rotate(float angle)
        {
            _transform.Rotate(angle);
        }

        public void ResetMatrix()
        {
            _transform.Reset();
        }

        /// <summary>
        ///  Pushes the drawing matrix onto the internal matrix stack.
        /// </summary>
        public void PushMatrix()
        {
            _matrixStack.Push(_transform);
        }

        /// <summary>
        /// Pops the drawing matrix off the internal matrix stack.
        /// </summary>
        public void PopMatrix()
        {
            _transform = _matrixStack.Pop();
        }

        public void SetMatrix(Transform2D _trans)
        {
            _transform = _trans;
        }

        /// <summary>
        /// Puts the canvas into lighting mode.
        /// </summary>
        public void BeginLighting()
        {
            System.Diagnostics.Debug.Assert(!_lighting, "Already lighting");

            if (_lighting)
            {
                return;
            }

            _lighting = true;

            Begin();

            // create gbuffer
            //

            var refDiffuse = _diffuseMap;
            var refNormal = _normalMap;

            _diffuseMap = Global.CreateRenderImage(Width, Height, ref _diffuseMap, RenderTargetUsage.PreserveContents);
            _normalMap = Global.CreateRenderImage(Width, Height, ref _normalMap);
            _specularMap = Global.CreateRenderImage(Width, Height, ref _specularMap, RenderTargetUsage.DiscardContents);

            if (refDiffuse != _diffuseMap || refNormal != _normalMap)
            {
                _gBuffer = new RenderTargetBinding[]
                {
                        new RenderTargetBinding(_diffuseMap),
                        new RenderTargetBinding(_normalMap),
                        new RenderTargetBinding(_specularMap)
                };
            };

            // clear gbuffer
            //
            Device.SetRenderTarget(_diffuseMap);
            Device.Clear(Color.Black);
            Device.SetRenderTarget(_normalMap);
            Device.Clear(new Color(0.5f, 0.5f, 0, 1.0f));
            Device.SetRenderTarget(_specularMap);
            Device.Clear(Color.TransparentBlack);// Clear Alpha, because SurafceFprmat is Alpha8

            RenderTarget = _diffuseMap;
        }

        /// <summary>
        ///  Renders lighting and ends lighting mode.
        /// </summary>
        public void EndLighting()
        {
            if (!_lighting) return;

            // draw everything
            //
            Flush(); _lighting = false;

            // Update lighting
            //
            LightRenderer.Resize(this.Width, this.Height);
            var lightmap = LightRenderer.Render(_normalMap, AmbientColor, ShadowEnabled, NormalmapEnabled);
     

            // Combine diffuse an Lighting
            //  diffuse * Lighting + specular
            //
            PushMatrix();
            ResetMatrix();
            RenderTarget = null;
            Color = Color.White;
            Effect = _lightingEffect;
            BlendMode = BlendMode.Opaque;
           
            Alpha = 1;

            _lightingEffect.Parameters["WorldViewProjection"].SetValue(WorldViewProj);
            _lightingEffect.Parameters["DiffuseSampler"].SetValue(_diffuseMap);
            _lightingEffect.Parameters["LightmapSampler"].SetValue(lightmap);
            _lightingEffect.Parameters["SpecularMapSampler"].SetValue(_specularMap);

            Device.SamplerStates[0] = SamplerState.PointClamp;
            Device.SamplerStates[1] = SamplerState.PointClamp;
            Device.SamplerStates[2] = SamplerState.PointClamp;

            DrawImage(_diffuseMap, 0, 0 );

            // Show
            //
            Flush();
            Effect = null;

            if (ShowGBuffer)
            {
                Device.SamplerStates[0] = SamplerState.PointClamp;
                var filter = TextureFilteringEnabled;
                TextureFilteringEnabled = true;

                BlendMode = BlendMode.Opaque;
                // Diffuse
                DrawImage(_diffuseMap, 0, 0, 0.2f, 0.2f, 0);

                // Normal
                DrawImage(_normalMap, _normalMap.Width * 0.2f, 0, 0.2f, 0.2f, 0);

                // specular map
                Color = Color.White;
                BlendMode = BlendMode.Opaque;
                DrawImage(_specularMap, _normalMap.Width * 0.4f, 0, 0.2f, 0.2f, 0);

                // light
                if (lightmap != null)
                {
                    BlendMode = BlendMode.Alpha;
                    Color = Color.Black;
                    DrawRect(_normalMap.Width * 0.6f, 0, _normalMap.Width * 0.2f, _normalMap.Height * 0.2f);
                    Color = Color.White;

                    // specular reflection factor
                    var lightmapImg = new Image(lightmap);
                    DrawImage(lightmapImg, _normalMap.Width * 0.6f, 0, 0.2f, 0.2f, 0);

                    // lightmap
                    BlendMode = BlendMode.Opaque;
                    DrawImage(lightmapImg, _normalMap.Width * 0.8f, 0, 0.2f, 0.2f, 0);
                }

                Flush();
                TextureFilteringEnabled = filter;
            }

            PopMatrix();
        }

        public void Begin()
        {
            Flush();

            Device.RasterizerState = RasterizerState.CullNone;
            Device.DepthStencilState = DepthStencilState.None;
            Device.BlendState = MojoBlend.BlendAlpha;

            if (TextureFilteringEnabled)
            {
                Device.SamplerStates[0] = SamplerState.LinearClamp;
            }
            else
            {
                Device.SamplerStates[0] = SamplerState.PointClamp;
            }

            ResetMatrix();

            BlendMode = BlendMode.Alpha;
            Color = Color.White;
            Alpha = 1.0f;
            RenderTarget = null;
            _defaultEffect.WorldViewProjection = WorldViewProj;
            Effect = _defaultEffect;
        }

        

        public void End()
        {
            Flush();

            // TODO 
            // check: got error without this call, when switching Lighting on/Off
            LightRenderer.Reset(); 

            // the device could be used in multiple canvases,
            // herefore the rendertarget must be deactivated.
            Device.SetRenderTarget(null);
        }

        /// <summary>
        /// The current drawing blend mode.
        /// </summary>
        public BlendMode BlendMode
        {
            set
            {
                _blendMode = value;
            }
            get
            {
                return _blendMode;
            }
        }

        /// <summary>
        /// The current drawing color.
        /// </summary>
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _internalColor = value;
                _color = new Color(value, _alpha);
            }
        }

        /// <summary>
        /// The current drawing alpha level.
        /// </summary>
        public float Alpha
        {
            get
            {
                return _alpha;
            }
            set
            {
                _alpha = value;
                _color.A = (byte)(value * 255);
            }
        }

        /// <summary>
        ///  The current scissor rect.
        /// </summary>
        public Rectangle ScissorRect
        {
            get
            {
                return _scissorRect;
            }
            set
            {
                SetScissor(value.X, value.Y, value.Width, value.Height);
            }
        }

        /// <summary>
        /// Sets the current scissor rect.
        /// </summary>
        public void SetScissor(int x, int y, int w, int h)
        {
            Flush();

            int r = Math.Min(x + w, Width);
            int b = Math.Min(y + h, Height);
            x = Math.Max(x, 0);
            y = Math.Max(y, 0);

            if (r > x && b > y)
            {
                w = r - x;
                h = b - y;
            }
            else
            {
                x = y = w = h = 0;
            }

            _scissorRect.X = x;
            _scissorRect.Y = y;
            _scissorRect.Width = w;
            _scissorRect.Height = h;

            if (x != 0 || y != 0 || w != Width || h != Height)
            {
                Device.RasterizerState = Global.RasterizerStateScissor;
            }
            else
            {
                Device.RasterizerState = RasterizerState.CullNone;
            }

            Device.ScissorRectangle = _scissorRect;
        }

        /// <summary>
        /// Clears the current viewport to `color`.
        /// </summary>
        public void Clear(float r, float g, float b)
        {
            Clear(new Color((int)r, (int)g, (int)b)); 
        }

        /// <summary>
        /// Clears the current viewport to `color`.
        /// </summary>
        public void Clear(Color c)
        {
            _drawBuffer.Clear();
            _drawOp = new DrawOp();

            if (Device.RasterizerState.ScissorTestEnable)
            {
                Rectangle sr = Device.ScissorRectangle;
                float x = sr.X;
                float y = sr.Y;
                float w = sr.Width;
                float h = sr.Height;

                unsafe
                {
                    var ptr = AddDrawOp((int)PrimType.Quad, 1, null, _defaultEffect, BlendMode.Opaque);

                    ptr[0].Transform(x + 0, y + 0, c);
                    ptr[1].Transform(x + w, y + 0, c);
                    ptr[2].Transform(x + w, y + h, c);
                    ptr[3].Transform(x + 0, y + h, c);
                }
            }
            else
            {
                Device.Clear(c);
            }
        }

        /// <summary>
        /// Draws a rectangle in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawRect(float x, float y, float w, float h)
        {
            unsafe
            {
                float x0 = x;
                float y0 = y;
                float x1 = x + w;
                float y1 = y;
                float x2 = x + w;
                float y2 = y + h;
                float x3 = x;
                float y3 = y + h;

                var ptr = AddDrawOp((int)PrimType.Quad, 1, null, _currentEffect, _blendMode);

                ptr[0].Transform( x0, y0, _transform, _color);
                ptr[1].Transform( x1, y1, _transform, _color); 
                ptr[2].Transform( x2, y2, _transform, _color);
                ptr[3].Transform( x3, y3, _transform, _color);

                if(OutlineMode != OutlineMode.None)
                {
                    DrawOutlineLine(x0, y0, x1, y1);
                    DrawOutlineLine(x1, y1, x2, y2);
                    DrawOutlineLine(x2, y2, x3, y3);
                    DrawOutlineLine(x3, y3, x0, y0);
                }
            }

        }

        /// <summary>
        /// Draws a rectangle in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawRect(Rectangle rect)
        {
            DrawRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws a point in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawPoint(Vector2 p)
        {
            DrawPoint(p.X, p.Y);
        }

        /// <summary>
        /// Draws a point in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawPoint(Point p)
        {
            DrawPoint((float)p.X, (float)p.Y);
        }

        /// <summary>
        /// Draws a point in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawPoint(float x, float y)
        {
            unsafe
            {
                var _vertices = AddDrawOp((int)PrimType.Quad, 1, null, _currentEffect, _blendMode);

                _vertices[0].Transform(x, y, _color);
                _vertices[1].Transform(x + PointSize, y, _color);
                _vertices[2].Transform(x + PointSize, y + PointSize, _color);
                _vertices[3].Transform(x, y + PointSize, _color);
            }
        }

        /// <summary>
        /// Draws a line in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawLine(Vector2 p0, Vector2 p1)
        {
            DrawLine(p0.X, p0.Y, p1.X, p1.Y);
        }

        /// <summary>
        /// Draws a line in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawLine(Point p0, Point p1)
        {
            DrawLine((float)p0.X, (float)p0.Y, (float)p1.X, (float)p1.Y);
        }

       
        /// <summary>
        /// Draws a line in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawLine(float x0, float y0, float x1, float y1)
        {
            unsafe
            {
                if (LineWidth <= 0)
                {
                    var ptr = AddDrawOp((int)PrimType.Line, 1, null, _currentEffect, _blendMode);
                    ptr[0].Transform(x0, y0, _transform, _color);
                    ptr[1].Transform(x1, y1, _transform, _color);
                    return;
                }

                float dx = y0 - y1;
                float dy = x1 - x0;
                float sc = 0.5f / (float)Math.Sqrt(dx * dx + dy * dy) * LineWidth;
                dx *= sc;
                dy *= sc;

                if (!LineSmooth)
                {
                    var ptr = AddDrawOp((int)PrimType.Quad, 1, null, _currentEffect, _blendMode);
                    ptr[0].Transform(x0 - dx, y0 - dy, _transform, _color);
                    ptr[1].Transform(x0 + dx, y0 + dy, _transform, _color);
                    ptr[2].Transform(x1 + dx, y1 + dy, _transform, _color);
                    ptr[3].Transform(x1 - dx, y1 - dy, _transform, _color);
                }
                else
                {
                    var ptr = AddDrawOp((int)PrimType.Quad, 2, null, _currentEffect, _blendMode);

                    ptr[0].Transform(x0, y0, _transform, _color);
                    ptr[1].Transform(x1, y1, _transform, _color);

                    ptr[2].Transform(x1 - dx, y1 - dy, _transform, Color.TransparentBlack);
                    ptr[3].Transform(x0 - dx, y0 - dy, _transform, Color.TransparentBlack);

                    ptr[4].Transform(x0 + dx, y0 + dy, _transform, Color.TransparentBlack);
                    ptr[5].Transform(x1 + dx, y1 + dy, _transform, Color.TransparentBlack);

                    ptr[6].Transform(x1, y1, _transform, _color);
                    ptr[7].Transform(x0, y0, _transform, _color);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawOutlineLine(float x0, float y0, float x1, float y1)
        {
            unsafe
            {
                var blendMode = OutlineMode == OutlineMode.Smooth ? BlendMode.Alpha : BlendMode.Opaque;

                if (OutlineWidth <= 0)
                {
                    var ptr = AddDrawOp((int)PrimType.Line, 1, null, _currentEffect, blendMode);
                    ptr[0].Transform(x0, y0, _transform, OutlineColor);
                    ptr[1].Transform(x1, y1, _transform, OutlineColor);
                    return;
                }

                float dx = y0 - y1;
                float dy = x1 - x0;
                float sc = 0.5f / (float)Math.Sqrt(dx * dx + dy * dy) * OutlineWidth;
                dx *= sc;
                dy *= sc;

                if (OutlineMode == OutlineMode.Solid)
                {
                    var ptr = AddDrawOp((int)PrimType.Quad, 1, null, _currentEffect, _blendMode);
                    ptr[0].Transform(x0 - dx, y0 - dy, _transform, OutlineColor);
                    ptr[1].Transform(x0 + dx, y0 + dy, _transform, OutlineColor);
                    ptr[2].Transform(x1 + dx, y1 + dy, _transform, OutlineColor);
                    ptr[3].Transform(x1 - dx, y1 - dy, _transform, OutlineColor);
                }
                else
                {
                    var ptr = AddDrawOp((int)PrimType.Quad, 2, null, _currentEffect, blendMode);

                    ptr[0].Transform(x0, y0, _transform, OutlineColor);
                    ptr[1].Transform(x1, y1, _transform, OutlineColor);

                    ptr[2].Transform(x1 - dx, y1 - dy, _transform, Color.TransparentBlack);
                    ptr[3].Transform(x0 - dx, y0 - dy, _transform, Color.TransparentBlack);

                    ptr[4].Transform(x0 + dx, y0 + dy, _transform, Color.TransparentBlack);
                    ptr[5].Transform(x1 + dx, y1 + dy, _transform, Color.TransparentBlack);

                    ptr[6].Transform(x1, y1, _transform, OutlineColor);
                    ptr[7].Transform(x0, y0, _transform, OutlineColor);
                }
            }
        }


        /// <summary>
        /// Draws an oval in the current [[Color]] using the current [[BlendMode]].
        /// </summary>
        public void DrawOval(float x, float y, float w, float h)
        {
            float xr = w / 2.0f;
            float yr = h / 2.0f;
        
            int segs;
            if (_transform._tFormed)
            {
                float dx_x = xr * _transform._ix;
                float dx_y = xr * _transform._iy;
                float dx = (float)Math.Sqrt(dx_x * dx_x + dx_y * dx_y);
                float dy_x = yr * _transform._jx;
                float dy_y = yr * _transform._jy;
                float dy = (float)Math.Sqrt(dy_x * dy_x + dy_y * dy_y);
                segs = (int)(dx + dy);
            }
            else
            {
                segs = (int)(Math.Abs(xr) + Math.Abs(yr));
            }
            segs =  Math.Min(8192, Math.Max(segs, 12) & ~3);

            float x0 = x + xr;
            float y0 = y + yr;
        
            unsafe
            {
                var ptr = AddDrawOp(segs, 1, null, _currentEffect, _blendMode);
                for (int i = 0; i < segs; ++i)
                {
                    float th = -(float)i * (float)(Math.PI * 2.0) / (float)segs;
                    float px = x0 + (float)Math.Cos(th) * xr;
                    float py = y0 - (float)Math.Sin(th) * yr;

                    ptr[i].Transform(px, py, _transform, _color);
                }
            }
        }

        /// <summary>
        /// Draws a polygon using the current [[Color]], [[BlendMode]] and [[Matrix]].
        /// </summary>
        public void DrawPoly(float[] verts)
        {
            int n = verts.Length / 2;
            if (n < 3 || n > 8192) return;
            unsafe
            {
                var ptr = AddDrawOp(n, 1, null, _currentEffect, _blendMode);
                for (int i = 0; i < n; ++i)
                { 
                    ptr[i].Transform(verts[i * 2], verts[i * 2 + 1], _transform, _color);
                }
            }
        }

        /// <summary>
        /// Draws text using the current [[Color]], [[BlendMode]] and [[Matrix]].
        /// </summary>
        public void DrawText(string text, float x, float y)
        {
            if (string.IsNullOrEmpty(text))
                return;

            unsafe
            {
                float lineSpacing = _spriteFont.LineSpacing; 
                float start_x = (float)Math.Floor(x);
                float start_y = (float)Math.Floor(y);

                float x0 = start_x;
                float y0 = start_y;

                float scale_x = 1.0f / Font.Texture.Width;
                float scale_y = 1.0f / Font.Texture.Height;


                var firstGlyphOfLine = true;
                var glyphsArray = _spriteFont.Glyphs;

                fixed (SpriteFont.Glyph* pGlyphs = glyphsArray)
                {
                    for (int i = 0; i < text.Length; ++i)
                    {

                        var chr = text[i];

                        if (chr == '\r')
                            continue;

                        if (chr == '\n')
                        {
                            x0 = start_x;
                            y0 += lineSpacing;
                            firstGlyphOfLine = true;
                            continue;
                        }

                        var glyph = pGlyphs[Math.Max(0, Math.Min(glyphsArray.Length-1, (int)chr-32))];
                        var cropping = glyph.Cropping;
                        var bounds = glyph.BoundsInTexture;

                        if (firstGlyphOfLine)
                        {
                            x0 += Math.Max(glyph.LeftSideBearing, 0);
                            firstGlyphOfLine = false;
                        }
                        else
                        {
                            x0 += Font.Spacing + glyph.LeftSideBearing;
                        }

                        float px = x0 + cropping.X;
                        float py = y0 + cropping.Y;

                        int width = bounds.Width;
                        int height = bounds.Height;

                        float u0 = bounds.X * scale_x;
                        float v0 = bounds.Y * scale_y;
                        float u1 = (bounds.X + bounds.Width) * scale_x;
                        float v1 = (bounds.Y + bounds.Height) * scale_y;

                        var vertex = AddDrawOp((int)PrimType.Quad, 1, _fontImage, _defaultEffect, _blendMode);
                        {
                            (vertex + 0)->Transform(px, py, u0, v0, _transform, _color);
                            (vertex + 1)->Transform(px + width, py, u1, v0, _transform, _color);
                            (vertex + 2)->Transform(px + width, py + height, u1, v1, _transform, _color);
                            (vertex + 3)->Transform(px, py + height, u0, v1, _transform, _color);
                        }

                        x0 += (glyph.Width + glyph.RightSideBearing);
                    }
                }
            }
        }

        /// <summary>
        /// Draws an image using the current [[Color]], [[BlendMode]] and without [[Matrix]] transform.
        /// </summary>
        public void DrawImageAbs(Image img, float x, float y, float w, float h)
        {
            unsafe
            {
                var ptr = AddDrawOp((int)PrimType.Quad, 1, img, _currentEffect, _blendMode);
                var quad = img.Quad;

                ptr[0].Transform(x + 0, y + 0, img.Quad.u0, img.Quad.v0, _color);
                ptr[1].Transform(x + w, y + 0, img.Quad.u1, img.Quad.v0, _color);
                ptr[2].Transform(x + w, y + h, img.Quad.u1, img.Quad.v1, _color);
                ptr[3].Transform(x + 0, y + h, img.Quad.u0, img.Quad.v1, _color);
            }
        }

        /// <summary>
        /// Adds a shadow caster to the canvas.
        /// </summary>
        public void AddShadowCaster(Vector2[] vertices, float x, float y, ShadowType shadowType = ShadowType.Illuminated)
        {
            LightRenderer.AddShadowCaster(Matrix, vertices, x, y, shadowType);
        }

        /// <summary>
        /// Adds a shadow caster to the canvas.
        /// </summary>
        public void AddShadowCaster(Vector2[] vertices, float x, float y, float rz, ShadowType shadowType = ShadowType.Illuminated)
        {
            PushMatrix();
            Translate(x, y);
            Rotate(rz);
            AddShadowCaster(vertices, 0, 0, shadowType);
            PopMatrix();
        }

        /// <summary>
        /// Adds a shadow caster to the canvas.
        /// </summary>
        public void AddShadowCaster(Vector2[] vertices, float x, float y, float rz, float sx, float sy, ShadowType shadowType = ShadowType.Illuminated)
        {
            PushMatrix();
            TranslateRotateScale(x, y, sx, sy, rz);
            AddShadowCaster(vertices, 0,0, shadowType);
            PopMatrix();
        }

        /// <summary>
        /// Draws an image using the current [[Color]], [[BlendMode]] and [[Matrix]].
        /// </summary>
        public void DrawImage(Image img, float x, float y)
        {
            DrawImageInternal(img, x - img._handle.X, y - img._handle.Y);
        }

        /// <summary>
        /// Draws an image using the current [[Color]], [[BlendMode]] and [[Matrix]].
        /// </summary>
        public void DrawImage(Image img, float x, float y, float sx, float sy, float angle)
        {
            PushMatrix();
            Translate(x, y);
            Rotate(angle);
            Scale(sx, sy);
            Translate(-img._handle.X, -img._handle.Y);
            DrawImageInternal(img, 0.0f, 0.0f);
            PopMatrix();
        }

        /// <summary>
        /// Adds a point light to the canvas.
        /// </summary>
        public void AddPointLight(float x, float y, float range, float intensity = 4.0f, float size = 4.0f, float depth = 96.0f)
        {
            PushMatrix();
            Translate(x, y);
            LightRenderer.AddPointLight(Matrix, Color, range, intensity, size, depth);
            PopMatrix();
        }

        /// <summary>
        /// Adds a spot light to the canvas.
        /// </summary>
        public void AddSpotLight(float x, float y, float angle, float range,
            float inner = 25, float outer = 45, float intensity = 4.0f, float size = 4.0f, float depth = 96.0f)
        {
            PushMatrix();
            Translate(x, y);
            Rotate(angle);
            Scale(1, 1);
            LightRenderer.AddSpotLight(Matrix, Color, inner, outer, range, intensity, size, depth);
            PopMatrix();
        }

        public Buffer Buffer
        {
            get
            {
                return _drawBuffer;
            }
            set
            {
                if(value == null)
                {
                    _drawBuffer = _defaultBuffer;
                }
                else
                {
                    _drawBuffer = value;
                }
            }
        }

        #region internal stuff

      

        private void Initialize(Image rt)
        {
            _originalRendertarget = rt;
            _drawBuffer = new Buffer();
            _defaultBuffer = _drawBuffer;

            _defaultEffect = new DefaultEffect(Global.Content.Load<Effect>("Effects/bump"));
            Effect = _defaultEffect;

            _lightingEffect = Global.Content.Load<Effect>("Effects/lighting");

            ResetMatrix();
            RenderTarget = _originalRendertarget;
            WorldViewProj = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(+.0f, Width + .0f, Height + .0f, +.0f, 0, 1);
            SetScissor(0, 0, Width, Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe MojoVertex* AddDrawOp(int primType, int primCount, Image img, Effect effect, BlendMode blendMode)
        {
            if (_drawBuffer.Size + primCount * (int)primType > Global.MAX_VERTS)
            {
                Flush();
            }

            if (blendMode == BlendMode.None)
            {
                BlendMode = _blendMode;
            }

            if (img != _drawOp.img || effect != _drawOp.effect || primType != _drawOp.primType || blendMode != _drawOp.blendMode)
            {
                _drawOp = _drawBuffer.AddDrawOp();
                _drawOp.img = img;
                _drawOp.effect = effect;
                _drawOp.primType = primType;
                _drawOp.blendMode = blendMode;
                _drawOp.primCount = primCount;
                _drawOp.primOffset = _drawBuffer.Size;
            }
            else
            {
                _drawOp.primCount += primCount;
            }

            return _drawBuffer.AddVertices(primType * primCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawImageInternal(Image img, float x, float y)
        {
            unsafe
            {
                var ptr = AddDrawOp((int)PrimType.Quad, 1, img, _currentEffect, _blendMode);
                var quad = img.Quad;

                ptr[0].Transform(quad.vertex0.X + x, quad.vertex0.Y + y, quad.u0, quad.v0, _transform._tanX, _transform._tanY, _transform, _color);
                ptr[1].Transform(quad.vertex1.X + x, quad.vertex1.Y + y, quad.u1, quad.v0, _transform._tanX, _transform._tanY, _transform, _color);
                ptr[2].Transform(quad.vertex2.X + x, quad.vertex2.Y + y, quad.u1, quad.v1, _transform._tanX, _transform._tanY, _transform, _color);
                ptr[3].Transform(quad.vertex3.X + x, quad.vertex3.Y + y, quad.u0, quad.v1, _transform._tanX, _transform._tanY, _transform, _color);

                if(img.ShadowCaster != null)
                {
                    LightRenderer.AddShadowCaster(Matrix, img.ShadowCaster, x + img.Width / 2, y + img.Height / 2);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetRenderTargetInternal(RenderTarget2D value)
        {
            if (initialized && value == _currentRendertarget)
            {
                return;
            }

            Flush();

            _currentRendertarget = value;

            int last_width = Width;
            int last_height = Height;

            if (value != null)
            {
                Width = value.Width;
                Height = value.Height;
            }
            else
            {
                Width = Device.PresentationParameters.BackBufferWidth;
                Height = Device.PresentationParameters.BackBufferHeight;
            }

            if (Height != last_height || Width != last_width)
            {
                WorldViewProj = Microsoft.Xna.Framework.Matrix.CreateOrthographicOffCenter(+.0f, Width + .0f, Height + .0f, +.0f, 0, 1);
                _defaultEffect.WorldViewProjection = WorldViewProj;
                SetScissor(0, 0, Width, Height);
            }

            Device.SetRenderTarget(_currentRendertarget);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenderDrawOps()
        {
            var blendMode = BlendMode.None;

            foreach (var op in _drawBuffer.DrawOps)
            {
                var opEffect = op.effect;

                if( opEffect == _defaultEffect)
                {
                    if(op.img == null)
                    {
                        _defaultEffect.TextureEnabled = false;
                        _defaultEffect.SpecularEnabled = SpecularEnabled;
                        _defaultEffect.NormalEnabled = NormalmapEnabled;
                    }
                    else
                    {
                        _defaultEffect.TextureEnabled = true;
                        _defaultEffect.SpecularEnabled = SpecularEnabled;
                        _defaultEffect.NormalEnabled = NormalmapEnabled;
                        _defaultEffect.Texture = op.img._texture;

                        if(NormalmapEnabled)
                        {
                            _defaultEffect.Normalmap = op.img._normal ?? Global.DefaultNormal;
                        }

                        if (op.img._specular == null)
                        {
                            var specularFactor = Math.Max(0, Math.Min(255, (int)(op.img.Specularity * 255)));
                            _defaultEffect.Specularmap = Global.DefaultSpecular[specularFactor];
                        }
                        else
                        {
                            _defaultEffect.Specularmap = op.img._specular;
                        }
                    }
                   
                }

                if (blendMode != op.blendMode)
                {
                    blendMode = op.blendMode;
                    SetInternalBlendMode(blendMode);
                }

                Render(_drawBuffer, op.primType, opEffect, op.primOffset, op.primCount * op.primType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetInternalBlendMode(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    Device.BlendState = BlendState.Opaque;
                    break;
                case BlendMode.Alpha:
                    Device.BlendState = MojoBlend.BlendAlpha;
                    break;
                case BlendMode.InverseAlpha:
                    Device.BlendState = MojoBlend.BlendInverseAlpha;
                    break;
                case BlendMode.Multiply:
                    Device.BlendState = MojoBlend.BlendMultiply;
                    break;
                case BlendMode.Additive:
                    Device.BlendState = MojoBlend.BlendAdd;
                    break;
                case BlendMode.Subtract:
                    Device.BlendState = MojoBlend.BlendShadow;
                    break;
                case BlendMode.Screen:
                    Device.BlendState = MojoBlend.BlendScreen;
                    break;
                case BlendMode.Darken:
                    Device.BlendState = MojoBlend.BlendDarken;
                    break;
                case BlendMode.Lighten:
                    Device.BlendState = MojoBlend.BlendLighten;
                    break;
                case BlendMode.LiearDodge:
                    Device.BlendState = MojoBlend.BlendLinearDodge;
                    break;
                case BlendMode.LinearBurn:
                    Device.BlendState = MojoBlend.BlendLienarBurn;
                    break;
                default:
                    Device.BlendState = MojoBlend.BlendAlpha;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenderVertexBuffer(Buffer drawList, int primType, Effect effect, int offset, int count)
        {
            Device.SetVertexBuffer(drawList.VertexBuffer);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                switch ((PrimType)primType)
                {
                    case PrimType.Line:

                        Device.Indices = Global._iboQuad;
                        Device.DrawIndexedPrimitives(PrimitiveType.LineList, offset,  0, count);
    
                        break;

                    case PrimType.Quad:

                        Device.Indices = Global._iboQuad;
                        Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, offset, 0, count / 2);
                        break;

                    default:

                        Device.Indices = Global._iboFan;
                        int cnt = count / primType;
                        for (int i = 0; i < cnt; ++i)
                        {
                            Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, offset + i * primType, 0, count - 2);
                        }
                        break;

                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Render(Buffer drawList, int primType, Effect effect, int offset, int count)
        {
            var passes = effect.CurrentTechnique.Passes;
            for(int p = 0; p< passes.Count; ++p)
            {
                passes[p].Apply();
                switch ((PrimType)primType)
                {
                    case PrimType.Line:

                        Device.DrawUserPrimitives<MojoVertex>(
                            PrimitiveType.LineList, drawList.VertexArray, offset, count);

                        break;

                    case PrimType.Quad:

                        Device.DrawUserIndexedPrimitives<MojoVertex>(
                            PrimitiveType.TriangleList, drawList.VertexArray, offset, count,
                            Global.QuadIndices,  0, count / 2);

                        break;

                    default:

                        int cnt = count / primType;
                        for (int i = 0; i < cnt; ++i) 
                        {
                            Device.DrawUserIndexedPrimitives<MojoVertex>(
                                PrimitiveType.TriangleList,
                                drawList.VertexArray, offset + i * primType, primType,
                                Global.FanIndices, 0, primType - 2);
                        }

                        break;
                }
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _defaultEffect.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
