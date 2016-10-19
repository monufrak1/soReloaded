using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Graphics3D
{
    class TerrainFromHeightMap : TerrainGrid
    {
        public TerrainFromHeightMap(GraphicsDevice device, Texture2D lowLevelTexture, Texture2D highLevelTexture,
                                    string terrainFileName, float textureScale, float spacing, float heightScale)
            : base(device, lowLevelTexture, highLevelTexture, textureScale, spacing)
        {
            ReadDataFromFile(terrainFileName, heightScale);
            GenerateGeometry();
        }

        private void ReadDataFromFile(string terrainFileName, float heightScale)
        {
            FileStream inFile = File.OpenRead(terrainFileName);
            byte[] data = new byte[inFile.Length];
            inFile.Read(data, 0, data.Length);
            inFile.Close();

            // Get height values
            heightValues = new float[data.Length];
            for (int i = 0; i < heightValues.Length; i++)
            {
                heightValues[i] = HEIGHT_OFFSET + (float)data[i] * heightScale;
            }

            numRows = (int)Math.Sqrt((double)data.Length);
            numCols = numRows;

            // Smooth Terrain
            SmoothTerrain();
        }

        private bool InBounds(int row, int col)
        {
            return ((row >= 0 && row < numRows) &&
                    (col >= 0 && col < numCols));
        }

        private float Average(int row, int col)
        {
            float avg = 0.0f;
            float total = 0.0f;

            for (int i = row - 1; i < row + 1; i++)
            {
                for (int j = col - 1; j < col + 1; j++)
                {
                    if (InBounds(i, j))
                    {
                        avg += heightValues[i * numCols + j];
                        total += 1.0f;
                    }
                }
            }

            return avg / total;
        }

        private void SmoothTerrain()
        {
            float[] smoothData = new float[heightValues.Length];

            // Copy heightValues into smoothData using average to smooth
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    smoothData[i * numCols + j] = Average(i, j);
                }
            }

            // Copy smoothData back to heightValues
            heightValues = smoothData;
        }
    }

}
