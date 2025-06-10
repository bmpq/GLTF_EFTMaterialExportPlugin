using UnityEngine;

namespace UnityGLTF
{
    public enum ChannelSource
    {
        TexFirst_Red = 0,
        TexFirst_Green = 1,
        TexFirst_Blue = 2,
        TexFirst_Alpha = 3,
        TexSecond_Red = 4,
        TexSecond_Green = 5,
        TexSecond_Blue = 6,
        TexSecond_Alpha = 7
    }

    public static class TextureConverter
    {
        public static Texture2D Convert(Texture inputTexture, Material mat, string addTag = null)
        {
            if (inputTexture == null)
                return null;

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(inputTexture, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            convertedTexture.name = inputTexture.name;
            if (!string.IsNullOrEmpty(addTag))
                convertedTexture.name += $"_{addTag}";

            return convertedTexture;
        }

        public static Texture2D ConvertAlbedoSpecGlosToSpecGloss(Texture inputTextureAlbedoSpec, Texture inputTextureGloss)
        {
            Material mat = new Material(Shader.Find("Hidden/Blit/AlbedoSpecGlosToSpecGloss"));
            mat.SetTexture("_AlbedoSpecTex", inputTextureAlbedoSpec);
            mat.SetTexture("_GlossinessTex", inputTextureGloss);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(inputTextureAlbedoSpec.width, inputTextureAlbedoSpec.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(inputTextureAlbedoSpec, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            convertedTexture.name = inputTextureAlbedoSpec.name + "_SPECGLOS";

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D Invert(Texture inputTexture)
        {
            if (inputTexture == null) return null;

            Material mat = new Material(Shader.Find("Hidden/Blit/Invert"));
            mat.SetTexture("_MainTex", inputTexture);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(inputTexture, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();
            convertedTexture.name = inputTexture.name;

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D CombineR(Texture texR, Texture texG, Texture texB, Texture texA)
        {
            Material mat = new Material(Shader.Find("Hidden/Blit/CombineR"));
            mat.SetTexture("_RTex", texR);
            mat.SetTexture("_GTex", texG);
            mat.SetTexture("_BTex", texB);
            mat.SetTexture("_ATex", texA);

            int maxWidth = Mathf.Max(texR.width, texG.width, texB.width, texA.width);
            int maxHeight = Mathf.Max(texR.height, texG.height, texB.height, texA.height);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(maxWidth, maxHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(Texture2D.blackTexture, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D Invert(Texture tex, bool invertAlpha = true)
        {
            Shader shaderInvert = Shader.Find("Hidden/Blit/Invert");
            Material mat = new Material(shaderInvert);
            mat.SetTexture("_MainTex", tex);

            mat.SetKeyword(new UnityEngine.Rendering.LocalKeyword(shaderInvert, "INVERT_ALPHA"), invertAlpha);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(tex, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D ChannelToGrayscale(Texture tex, int channel)
        {
            Material mat = new Material(Shader.Find("Hidden/Blit/ChannelToGrayscale"));

            mat.SetTexture("_MainTex", tex);
            mat.SetInt("_ChannelSelect", channel);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(tex, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D BlendOverlay(Texture2D texBase, Texture2D texTop, Texture2D texMask, float factor)
        {
            Material mat = new Material(Shader.Find("Hidden/Blit/BlendOverlay"));
            mat.SetTexture("_MainTex", texBase);
            mat.SetTexture("_TopTex", texTop);
            mat.SetTexture("_MaskTex", texMask);
            mat.SetFloat("_Factor", factor);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(texBase.width, texBase.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(texBase, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        public static Texture2D FillAlpha(Texture tex, float alpha = 1.0f)
        {
            Material mat = new Material(Shader.Find("Hidden/Blit/FillAlpha"));

            return Convert(tex, mat);
        }

        static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            return tex;
        }

        public static Texture2D CreateSolidColorTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);

            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
