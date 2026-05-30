using UnityEngine;

namespace Henrizy.Editor.ColoredFolder
{
    public static class EditorUtils
    {
        public static Texture2D MakeGradientTex2D( int width, int height, Vector2 direction, Color startColor, Color endColor )
        {
            Texture2D texture = new Texture2D( Mathf.Max(1, width), Mathf.Max(1, height) );
            try
            {
                Color[] pixels = new Color[ width * height ];

                direction.Normalize();

                float minDot = float.MaxValue;
                float maxDot = float.MinValue;

                for ( int y = 0; y < height; y++ )
                {
                    for ( int x = 0; x < width; x++ )
                    {
                        float dot = Vector2.Dot( new Vector2( x, y ), direction );
                        if ( dot <= minDot )
                            minDot = dot;
                        if ( dot > maxDot )
                            maxDot = dot;
                    }
                }

                float dotRange = maxDot - minDot;

                for ( int y = 0; y < height; y++ )
                {
                    for ( int x = 0; x < width; x++ )
                    {
                        float dot = Vector2.Dot( new Vector2( x, y ), direction );
                        float t = Mathf.Clamp01((dot - minDot) / dotRange);
                        pixels[ y * width + x ] = Color.Lerp( startColor, endColor, t );
                    }
                }

                texture.SetPixels( pixels );
                texture.Apply();
                return texture;
            }
            catch
            {
                return texture;
            }
        }
        public static string ReplaceAny(this string text, string[] oldStrings, string newString )
        {
            if (oldStrings == null || oldStrings.Length == 0)
                return text;
            if (string.IsNullOrEmpty(text))
                return text;
            foreach ( var oldString in oldStrings )
            {
                text = text.Replace( oldString, newString );
            }
            return text;
        }
    }
}