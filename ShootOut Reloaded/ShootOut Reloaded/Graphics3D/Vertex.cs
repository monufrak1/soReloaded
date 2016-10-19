using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    struct VertexPos
    {
        public Vector3 pos;
        
        public VertexPos(Vector3 pos)
        {
            this.pos = pos;
        }

        public static readonly VertexDeclaration VertexLayout =
            new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3,
                    VertexElementUsage.Position, 0) );
    }


    struct VertexPosTex
    {
        public Vector3 pos;
        public Vector2 tex;

        public VertexPosTex(Vector3 pos, Vector2 tex)
        {
            this.pos = pos;
            this.tex = tex;
        }

        public static readonly VertexDeclaration VertexLayout =
            new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3,
                    VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2,
                    VertexElementUsage.TextureCoordinate, 0) );
    }

    struct VertexPosNorm
    {
        public Vector3 pos;
        public Vector3 norm;

        public VertexPosNorm(Vector3 pos, Vector3 norm)
        {
            this.pos = pos;
            this.norm = norm;
        }

        public static readonly VertexDeclaration VertexLayout =
            new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3,
                    VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3,
                    VertexElementUsage.Normal, 0));
    }


    struct VertexPosNormTex
    {
        public Vector3 pos;
        public Vector3 norm;
        public Vector2 tex;

        public VertexPosNormTex(Vector3 pos, Vector3 norm, Vector2 tex)
        {
            this.pos = pos;
            this.norm = norm;
            this.tex = tex;
        }

        public static readonly VertexDeclaration VertexLayout = 
            new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3,
                    VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3,
                    VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2,
                    VertexElementUsage.TextureCoordinate, 0) );
    }


    struct VertexPosTangNormTex
    {
        public Vector3 pos;
        public Vector3 tang;
        public Vector3 norm;
        public Vector2 tex;

        public VertexPosTangNormTex(Vector3 pos, Vector3 tang, Vector3 norm, Vector2 tex)
        {
            this.pos = pos;
            this.tang = tang;
            this.norm = norm;
            this.tex = tex;
        }

        public static readonly VertexDeclaration VertexLayout = 
            new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3,
                        VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3,
                        VertexElementUsage.Tangent, 0),
                new VertexElement(24, VertexElementFormat.Vector3,
                    VertexElementUsage.Normal, 0),
                new VertexElement(36, VertexElementFormat.Vector2,
                    VertexElementUsage.TextureCoordinate, 0) );
    }
}
