using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    public class ShadowCaster
    {
        internal Vector2[] _vertices;

        public ShadowCaster(float radius, int segments)
        {
            _vertices = new Vector2[segments];

            for (int i = 0; i < segments; ++i)
            {
                _vertices[i] = 
                    new Vector2(
                       (float)System.Math.Cos(i * (System.Math.PI * 2) / segments) * radius,
                       (float)System.Math.Sin(i * (System.Math.PI * 2) / segments) * radius);
            }
        }

    }


    public class Image : IDisposable
    {
        private bool _external = false;

        internal Texture2D _texture;
        internal Vector2 _handle;
        internal Rectangle _rect;
        internal Vector2 _size;
        internal TexturedQuad Quad = new TexturedQuad();

        public RenderTarget2D RenderTarget { get; private set; }
        public Texture2D Texture => _texture;
        public Vector2 Size => _size;
        public int Width => _rect.Width;
        public int Height => _rect.Height;
        public Vector2 Handle { get { return _handle; } set { _handle = value; } }
        public ShadowCaster ShadowCaster { get; set; }

        public static implicit operator Texture2D(Image img)
        {
            if (img != null)
            {
                return img.Texture;
            }
            else
            {
                return null;
            }
        }

        public static implicit operator RenderTarget2D(Image img)
        {
            if (img != null)
            {
                if(img.RenderTarget == null)
                {
                    img.RenderTarget = new RenderTarget2D(Global.Game.GraphicsDevice,img.Width, img.Height, false, SurfaceFormat.Color, DepthFormat.None);
                    img.Init(img.RenderTarget, new Rectangle(0, 0, img.Width, img.Height), img.Handle);
                }
                return img.RenderTarget;
            }
            else
            {
                return null;
            }

        }
        public static Image FromFile(string filename)
        {
            Image img = null;
            using (System.IO.Stream stream = new System.IO.FileStream(filename, FileMode.Open))
            {
                img = new Image(Texture2D.FromStream(Global.Device, stream));
            }
            return img;
        }

        private void Init(Texture2D tex, Rectangle rect, Vector2 handle)
        {
            _texture = tex;
            _handle = handle;
            _rect = rect;
            _size = new Vector2(rect.Width, rect.Height);
            _external = true;
        }

        public Image(Texture2D tex)
        {
            _external = true;
            Init(tex, new Rectangle(0, 0, tex.Width, tex.Height), new Vector2(0, 0));
            UpdateCoords();
        }

        public Image(string filename, float xHandle = 0.0f, float yHandle = 0.0f)
        {
            var tex = Global.Content.Load<Texture2D>(filename);
            Init(tex, new Rectangle(0, 0, tex.Width, tex.Height), new Vector2(xHandle, yHandle));
            UpdateCoords();
        }

        public Image(Image img, int x, int y, int w, int h, float xHandle = 0.0f, float yHandle = 0.0f)
        {
            Init(img._texture, new Rectangle(x + img._rect.X, y + img._rect.Y, w, h), new Vector2(xHandle, yHandle));
            UpdateCoords();
        }

        public Image(int w, int h, float xHandle = 0.0f, float yHandle = 0.0f)
        {
            Init(new Texture2D(Global.Game.GraphicsDevice, w, h), new Rectangle(0, 0, w, h), new Vector2(xHandle, yHandle));
            UpdateCoords();
        }

        public Image(int w, int h, RenderTargetUsage usage, float xHandle = 0.0f, float yHandle = 0.0f)
        {
            RenderTarget = new RenderTarget2D(Global.Game.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, usage);
            Init(RenderTarget, new Rectangle(0, 0, w, h), new Vector2(xHandle, yHandle));
            UpdateCoords();
        }

        private void UpdateCoords()
        {
            // vertices 
            Quad.vertex0 = new Vector2(0, 0);
            Quad.vertex1 = new Vector2(_rect.Width, 0);
            Quad.vertex2 = new Vector2(_rect.Width, _rect.Height);
            Quad.vertex3 = new Vector2(0, _rect.Height);

            // texture coords
            Quad.u0 = (float)_rect.X / _texture.Width;
            Quad.v0 = (float)_rect.Y / _texture.Height;
            Quad.u1 = (float)(_rect.X + _rect.Width) / _texture.Width;
            Quad.v1 = (float)(_rect.Y + _rect.Height) / _texture.Height;
        }

        public void Save(string filename)
        {
            using (var stream = File.Create(filename))
            {
                _texture.SaveAsPng(stream, Width, Height);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!_external && _texture != null)
                    {
                        _texture.Dispose();
                        _texture = null;
                    }
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
