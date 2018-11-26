using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mojo.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MojoVertex : IVertexType
    {
        public const int Size = 28;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0,  VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8,  VertexElementFormat.Color,   VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1));

        public Vector2 Position;
        public Color Color;
        public Vector2 Tex0;
        public Vector2 Tex1;

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public override string ToString() =>
            $"Position:{Position.ToString()} Color:{Color.ToString()}  Tex0:{Tex0.ToString()}  Tex1:{Tex1.ToString()}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, float u0, float v0, float u1, float v1, Transform2D t, Color color)
        {
            if (t._tFormed)
            {
                Position.X = x0 * t._ix + y0 * t._jx + t._tx;
                Position.Y = x0 * t._iy + y0 * t._jy + t._ty;
            }
            else
            {
                Position.X = x0;
                Position.Y = y0;
            }
            Tex0.X = u0;
            Tex0.Y = v0;
            Tex1.X = u1;
            Tex1.Y = v1;
            Color = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, float u0, float v0, Transform2D t, Color color)
        {
            if (t._tFormed)
            {
                Position.X = x0 * t._ix + y0 * t._jx + t._tx;
                Position.Y = x0 * t._iy + y0 * t._jy + t._ty;
            }
            else
            {
                Position.X = x0;
                Position.Y = y0;
            }
            Tex0.X = u0;
            Tex0.Y = v0;
            Tex1.X = t._tanX;
            Tex1.Y = t._tanY;
            Color = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void TransformPixelPerfect(float x0, float y0, float u0, float v0, Transform2D t, Color color)
        {
            if (t._tFormed)
            {
                Position.X = (int)(0.5f + x0 * t._ix + y0 * t._jx + t._tx);
                Position.Y = (int)(0.5f + x0 * t._iy + y0 * t._jy + t._ty);
            }
            else
            {
                Position.X = x0;
                Position.Y = y0;
            }
            Tex0.X = u0;
            Tex0.Y = v0;
            Tex1.X = t._tanX;
            Tex1.Y = t._tanY;
            Color = color;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, Transform2D t, Color color)
        {
            if (t._tFormed)
            {
                Position.X = x0 * t._ix + y0 * t._jx + t._tx;
                Position.Y = x0 * t._iy + y0 * t._jy + t._ty;
            }
            else
            {
                Position.X = x0;
                Position.Y = y0;
            }
            Color = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, float u0, float v0, Color color)
        {
            Position.X = x0;
            Position.Y = y0;
            Tex0.X = u0;
            Tex0.Y = v0;
            Color = color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, Color color)
        {
            Position.X = x0;
            Position.Y = y0;
            Color = color;

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, float u0, float v0)
        {
            Position.X = x0;
            Position.Y = y0;
            Tex0.X = u0;
            Tex0.Y = v0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(float x0, float y0, Transform2D t)
        {
            if (t._tFormed)
            {
                Position.X = x0 * t._ix + y0 * t._jx + t._tx;
                Position.Y = x0 * t._iy + y0 * t._jy + t._ty;
            }
            else
            {
                Position.X = x0;
                Position.Y = y0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Transform(Vector2 p, Transform2D t)
        {
            Position.X = p.X * t._ix + p.Y * t._jx + t._tx;
            Position.Y = p.X * t._iy + p.Y * t._jy + t._ty;
        }
    }

    public class Buffer
    {
        private DynamicVertexBuffer _vbo;
        private bool _dirty = false;

        public int MaxSize { get; private set; }
        public int Size { get; private set; }
        public MojoVertex[] VertexArray { get; private set; }
        public List<DrawOp> DrawOps { get; private set; }
        public bool VertexBufferEnabled { get; set; }

        public Buffer(int maxVerts = Global.MAX_VERTS)
        {
            MaxSize = maxVerts;
            VertexArray = new MojoVertex[maxVerts];
            DrawOps = new List<DrawOp>();
        }

        public void Clear()
        {
            _dirty = true;
            Size = 0;
            DrawOps.Clear();
        }

        public DrawOp AddDrawOp()
        {
            var op = new DrawOp();
            DrawOps.Add(op);
            return op;
        }

        public unsafe MojoVertex* AddVertices(int count)
        {
            unsafe
            {
                _dirty = true;
                fixed (MojoVertex* vptr = &VertexArray[Size])
                {
                    Size += count;
                    return vptr;
                }
            }
        }

        public unsafe MojoVertex* GetVerteyPtr(int index)
        {
            fixed (MojoVertex* vptr = &VertexArray[index])
            {
                return vptr;
            }
        }

        public VertexBuffer VertexBuffer
        {
            get
            {
                if(_dirty)
                {
                    _dirty = false;
                    if(_vbo == null)
                    {
                        _vbo = new DynamicVertexBuffer(Global.Device, MojoVertex.VertexDeclaration, MaxSize, BufferUsage.WriteOnly);
                    }
                    _vbo.SetData(VertexArray, 0, Size);
                }
                return _vbo;
            }
        }
    }

}
