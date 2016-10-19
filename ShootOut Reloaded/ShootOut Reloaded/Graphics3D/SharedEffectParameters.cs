using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Graphics3D
{
    static class SharedEffectParameters
    {
        // SHARED EFFECT VARIABLE VALUES

        // Common data
        public static float xTime;
        public static Vector3 xEyePosW;
        public static Matrix xViewMatrix;
        public static Matrix xProjectionMatrix;
        public static Matrix xReflectionViewMatrix;
        public static Matrix xReflectionProjectionMatrix;

        // Light data
        public static Vector3 xLightPosition;
        public static Vector3 xLightDirection;
        public static Vector4 xLightAmbient;
        public static Vector4 xLightDiffuse;
        public static Vector4 xLightSpecular;
        public static Matrix xLightViewMatrix;
        public static Matrix xLightProjectionMatrix;
        public static Texture2D xShadowMap;
        public static bool xShadowsEnabled;

        // Water data
        public static float xWaterHeight;
        public static float xDeepWaterFogDistance;
        public static Vector4 xWaterColor;

        // Fog data
        public static float xFogStart;
        public static float xFogRange;
        public static Vector4 xSkyColor;  // Also used as fog color
        public static float xDarkSkyOffset;
    }
}
