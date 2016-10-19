using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics3D
{
    class TerrainPlane : TerrainGrid
    {
        public TerrainPlane(GraphicsDevice device, Texture2D lowLevelTexture, Texture2D highLevelTexture,
                           float textureScale)
            : base(device, lowLevelTexture, highLevelTexture, textureScale, 10000.0f)
        {
            numRows = 3;
            numCols = 3;

            heightValues = new float[numRows * numCols];
            normalValues = new Vector3[numRows * numCols];

            for(int i = 0; i < numRows * numCols; i++)
            {
                heightValues[i] = HEIGHT_OFFSET - 1.0f;
                normalValues[i] = Vector3.UnitY;
            }

            GenerateGeometry();
        }
    }
}
