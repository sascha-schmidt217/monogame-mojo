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

    public enum BlendMode
    {
        None,
        Opaque,
        Alpha,
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
        private Stack<Transform2D> _matrixStack = new Stack<Transform2D>();
        private Transform2D _transform = new Transform2D();
        private bool _lighting = false;
        private float _alpha = 1.0f;
        private BlendMode _blendMode = BlendMode.None;
        private Rectangle _scissorRect;
        private Color _color;
        private Color _internalColor = Color.White;
        private Image _diffuseMap;
        private DrawOp _drawOp = new DrawOp();
        private Quad _rect = new Quad();
        private Effect _currentEffect;
        private Image _originalRendertarget = null;
        private RenderTarget2D _currentRendertarget = null;
        private Buffer _drawBuffer { get; set; }
        private Buffer _defaultBuffer;
        private ILightRenderer _lightRenderer;
        private SpriteFont _spriteFont;
        private bool initialized = false;
        private Image _fontImage;

        public GraphicsDevice Device { get; private set; }
        public BasicEffect DefaultEffect { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

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
                    _lightRenderer.OnLoad(this);
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
                        _currentEffect = DefaultEffect;
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

        public bool CanFlush
        {
            get
            {
                return _drawBuffer.DrawOps.Count > 0;
            }
        }

        public void Flush(bool preserveBuffer = false)
        {
            if (!CanFlush)
                return;

            if (TextureFilteringEnabled)
            {
                Device.SamplerStates[0] = SamplerState.LinearClamp;
            }
            else
            {
                Device.SamplerStates[0] = SamplerState.PointClamp;
            }

            RenderDrawOps();

            if(_lighting)
            {
                // TODO
                // render normal map
                // render specular map
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
            if (_lighting) return;
            _lighting = true;

            Begin();
            RenderTarget = Global.CreateRenderImage(Width, Height, ref _diffuseMap);

            _diffuseMap.RenderTarget.Name = "Diffuse";
        }

        /// <summary>
        ///  Renders lighting and ends lighting mode.
        /// </summary>
        public void EndLighting()
        {
            if (!_lighting) return;
            _lighting = false;

            // Flush everything to diffusemap
            Flush();

            // Update lighting
            //

            LightRenderer.AmbientColor = AmbientColor;
            LightRenderer.Resize(this.Width, this.Height);
            LightRenderer.Render();
            LightRenderer.Reset();

            // Combine diffuse an Lighting
            //
            PushMatrix();
            ResetMatrix();
            RenderTarget = null;
            Color = Color.White;
            Alpha = 1;

            // diffuse to backbuffer
            //
            BlendMode = BlendMode.Alpha;
            DrawImage(_diffuseMap, 0, 0);

            // multiply with lightmap
            //
            BlendMode = BlendMode.Multiply;
            DrawImage(LightRenderer.LightMap, 0, 0);
            Flush();
            PopMatrix();

            // the device could be used in multiple canvases,
            // herefore the rendertarget must be deactivated.
            //
            //Device.SetRenderTarget(null);
        }

        public void Begin()
        {
            Flush();

            Device.RasterizerState = RasterizerState.CullNone;
            Device.DepthStencilState = DepthStencilState.None;
            Device.BlendState = MojoBlend.BlendAlpha;

            ResetMatrix();

            BlendMode = BlendMode.Alpha;
            Color = Color.White;
            Alpha = 1.0f;
            Effect = DefaultEffect;
            RenderTarget = null;

            DefaultEffect.Projection = WorldViewProj;
            Effect.Parameters["WorldViewProj"].SetValue(WorldViewProj); // Add matrix dirty flag^
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
                _color = new Color(value.R, value.G, value.B);
                _color.A = (byte)(_alpha * 255);
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
                    var ptr = AddDrawOp((int)PrimType.Quad, 1, null, DefaultEffect, BlendMode.Opaque);

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
                var ptr = AddDrawOp((int)PrimType.Quad, 1, null, _currentEffect, _blendMode);

                _rect.vertex0.X = x;
                _rect.vertex0.Y = y;
                _rect.vertex1.X = x + w;
                _rect.vertex1.Y = y;
                _rect.vertex2.X = x + w;
                _rect.vertex2.Y = y + h;
                _rect.vertex3.X = x;
                _rect.vertex3.Y = y + h;

                ptr[0].Transform( _rect.vertex0.X, _rect.vertex0.Y, _transform, _color);
                ptr[1].Transform( _rect.vertex1.X, _rect.vertex1.Y, _transform, _color);
                ptr[2].Transform( _rect.vertex2.X, _rect.vertex2.Y, _transform, _color);
                ptr[3].Transform( _rect.vertex3.X, _rect.vertex3.Y, _transform, _color);
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
                var ptr = AddDrawOp((int)PrimType.Line, 1, null, _currentEffect, _blendMode);
                ptr[0].Transform(x0, y0, _transform, _color);
                ptr[1].Transform(x1, y1, _transform, _color);
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

                        var vertex = AddDrawOp((int)PrimType.Quad, 1, _fontImage, DefaultEffect, _blendMode);
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
        public void AddShadowCaster(Vector2[] vertices, float x, float y)
        {
            LightRenderer.AddShadowCaster(this, vertices, x, y);
        }

        /// <summary>
        /// Adds a shadow caster to the canvas.
        /// </summary>
        public void AddShadowCaster(Vector2[] vertices, float x, float y, float rz)
        {
            PushMatrix();
            Translate(x, y);
            Rotate(rz);
            AddShadowCaster(vertices, 0, 0);
            PopMatrix();
        }

        /// <summary>
        /// Adds a shadow caster to the canvas.
        /// </summary>
        public void AddShadowCaster(Vector2[] vertices, float x, float y, float rz, float sx, float sy)
        {
            PushMatrix();
            TranslateRotateScale(x, y, sx, sy, rz);
            AddShadowCaster(vertices, 0,0);
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
        public void AddPointLight(float x, float y, float range, float intensity = 4.0f, float size = 4.0f)
        {
            PushMatrix();
            Translate(x, y);
            LightRenderer.AddPointLight(this, 0,0, range, intensity, size);
            PopMatrix();
        }

        /// <summary>
        /// Adds a spot light to the canvas.
        /// </summary>
        public void AddSpotLight(float x, float y, float angle, float range,
            float inner = 25, float outer = 45, float intensity = 4.0f, float size = 4.0f)
        {
            PushMatrix();
            Translate(x, y);
            Rotate(angle);
            Scale(1, 1);
            LightRenderer.AddSpotLight(this, inner, outer, range, intensity, size);
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

            DefaultEffect = new BasicEffect(Device);
            DefaultEffect.VertexColorEnabled = true;
            Effect = DefaultEffect;

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

                ptr[0].Transform(quad.vertex0.X + x, quad.vertex0.Y + y, quad.u0, quad.v0, _transform, _color);
                ptr[1].Transform(quad.vertex1.X + x, quad.vertex1.Y + y, quad.u1, quad.v0, _transform, _color);
                ptr[2].Transform(quad.vertex2.X + x, quad.vertex2.Y + y, quad.u1, quad.v1, _transform, _color);
                ptr[3].Transform(quad.vertex3.X + x, quad.vertex3.Y + y, quad.u0, quad.v1, _transform, _color);

                if(img.ShadowCaster != null)
                {
                    AddShadowCaster(img.ShadowCaster.Vertices, x + img.Width / 2 , y + img.Height / 2);
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
                DefaultEffect.Projection = WorldViewProj;
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

                if (opEffect == DefaultEffect)
                {
                    if (op.img != null)
                    {
                        DefaultEffect.TextureEnabled = true;
                        DefaultEffect.Parameters["Texture"].SetValue(op.img._texture);
                    }
                    else
                    {
                        DefaultEffect.TextureEnabled = false;
                    }
                }

                if (blendMode != op.blendMode)
                {
                    blendMode = op.blendMode;
                    SetInternalBlendMode(blendMode);
                }

                if (_drawBuffer.VertexBufferEnabled)
                {
                    RenderVertexBuffer(_drawBuffer, op.primType, opEffect, op.primOffset, op.primCount * op.primType);
                }
                else
                {
                    Render(_drawBuffer, op.primType, opEffect, op.primOffset, op.primCount * op.primType);
                }
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

                    case PrimType.Tri:


                        Device.Indices = Global._iboFan;
                        Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, offset, 0, count - 2);
                        break;

                }
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Render(Buffer drawList, int primType, Effect effect, int offset, int count)
        {
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
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
                    DefaultEffect.Dispose();
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
