using System.Collections.Generic;
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
#if !UNITY_EDITOR
        private static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        public static void InjectBundleShaders(Shader[] bundleShaders)
        {
            if (bundleShaders == null) return;
            shaders.Clear();

            foreach (var shader in bundleShaders)
            {
                if (shader != null)
                {
                    shaders.Add(shader.name, shader);

                    UnityEngine.Debug.Log($"Injected {shader} in TextureConverter");
                }
            }
        }
#endif

        private static Shader GetShader(string shaderName)
        {
#if UNITY_EDITOR
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.Log(shaderName + " not found in Shaders");

                shaderName = shaderName.Replace("Hidden/Blit/", "");
                shader = Resources.Load<Shader>(shaderName);
                if (shader == null)
                    Debug.Log(shaderName + " not found in Resources");
            }
            return shader;
#else
            if (shaders.Count == 0)
            {
                Debug.LogError($"[TextureConverter] Shader dictionary is not initialized. Was InjectFromBundle called?");
                return null;
            }

            if (shaders.TryGetValue(shaderName, out Shader shader))
                return shader;

            Debug.LogError($"[TextureConverter] Shader '{shaderName}' not found in the injected bundle.");
            return null;
#endif
        }

        private static Texture2D ApplyBlit(Texture inputTexture, Material mat)
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

            return convertedTexture;
        }

        public static Texture2D GlosToMetRough(Texture texGlos)
        {
            Texture2D texRoughness = Invert(texGlos, false);
            return RoughToMetRough(texRoughness);
        }

        public static Texture2D RoughToMetRough(Texture texRough)
        {
            // in case a normal map is used as a glossiness map, the red channel won't work, so we use the green channel
            // old materials like the ones on interchange have a bad habit of not having slot appropriate bespoke textures
            // and instead substitute by using textures that were intended for other uses, ultimately killing my entire conversion approach in concept.
            // but more often than not, it turns out fine.
            Texture2D texG = ChannelToGrayscale(texRough, 1);

            Texture2D texMetRough = CombineR(Texture2D.blackTexture, texG, Texture2D.blackTexture, Texture2D.whiteTexture);
            return texMetRough;
        }

        public static Texture2D Invert(Texture inputTexture)
        {
            if (inputTexture == null) return null;

            Material mat = new Material(GetShader("Hidden/Blit/Invert"));
            mat.SetTexture("_MainTex", inputTexture);

            return ApplyBlit(inputTexture, mat);
        }

        public static Texture2D Power(Texture inputTex, float exponent = 2f)
        {
            Material mat = new Material(GetShader("Hidden/Blit/Power"));
            mat.SetTexture("_MainTex", inputTex);
            mat.SetFloat("_Exponent", exponent);

            return ApplyBlit(inputTex, mat);
        }

        public static Texture2D Multiply(Texture texA, Texture texB)
        {
            Material mat = new Material(GetShader("Hidden/Blit/Multiply"));
            mat.SetTexture("_TexA", texA);
            mat.SetTexture("_TexB", texB);

            return ApplyBlit(texA, mat);
        }

        public static Texture2D CombineR(Texture texR, Texture texG, Texture texB, Texture texA)
        {
            Material mat = new Material(GetShader("Hidden/Blit/CombineR"));
            mat.SetTexture("_RTex", texR);
            mat.SetTexture("_GTex", texG);
            mat.SetTexture("_BTex", texB);
            mat.SetTexture("_ATex", texA);

            return ApplyBlit(texG, mat);
        }

        public static Texture2D Invert(Texture tex, bool invertAlpha = true)
        {
            Shader shaderInvert = GetShader("Hidden/Blit/Invert");
            Material mat = new Material(shaderInvert);
            mat.SetTexture("_MainTex", tex);
            mat.SetKeyword(new UnityEngine.Rendering.LocalKeyword(shaderInvert, "INVERT_ALPHA"), invertAlpha);

            return ApplyBlit(tex, mat);
        }

        public static Texture2D ChannelToGrayscale(Texture tex, int channel)
        {
            Material mat = new Material(GetShader("Hidden/Blit/ChannelToGrayscale"));

            mat.SetTexture("_MainTex", tex);
            mat.SetInt("_ChannelSelect", channel);

            return ApplyBlit(tex, mat);
        }

        public static Texture2D OverrideAlpha(Texture tex, Texture texWithAlpha)
        {
            Material matBlit = new Material(GetShader("Hidden/Blit/SetAlphaFromTexture"));
            matBlit.SetTexture("_MainTex", tex);
            matBlit.SetTexture("_AlphaTex", tex);
            return ApplyBlit(tex, matBlit);
        }

        public static Texture2D PackGrayscaleTextureToOneChannel(Texture tex, int channel)
        {
            Material channelMixer = new Material(GetShader("Hidden/Blit/ChannelMixer"));
            channelMixer.SetTexture("_TexFirst", tex);
            channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
            channelMixer.SetFloat("_SourceR", channel == 0 ? (int)ChannelSource.TexFirst_Red : (int)ChannelSource.TexSecond_Red);
            channelMixer.SetFloat("_SourceG", channel == 1 ? (int)ChannelSource.TexFirst_Red : (int)ChannelSource.TexSecond_Red);
            channelMixer.SetFloat("_SourceB", channel == 2 ? (int)ChannelSource.TexFirst_Red : (int)ChannelSource.TexSecond_Red);
            channelMixer.SetFloat("_SourceA", channel == 3 ? (int)ChannelSource.TexFirst_Red : (int)ChannelSource.TexSecond_Red);

            Texture2D texResult = ApplyBlit(tex, channelMixer);

            return texResult;
        }

        public static Texture2D BlendOverlay(Texture2D texBase, Texture2D texTop, Texture2D texMask, float factor)
        {
            Material mat = new Material(GetShader("Hidden/Blit/BlendOverlay"));
            mat.SetTexture("_MainTex", texBase);
            mat.SetTexture("_TopTex", texTop);
            mat.SetTexture("_MaskTex", texMask);
            mat.SetFloat("_Factor", factor);

            return ApplyBlit(texBase, mat);
        }

        public static Texture2D CreateGrayscaleFromAlpha(Texture texMain)
        {
            Material channelMixer = new Material(GetShader("Hidden/Blit/ChannelMixer"));
            channelMixer.SetTexture("_TexFirst", texMain);
            channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
            channelMixer.SetFloat("_SourceR", (int)ChannelSource.TexFirst_Alpha);
            channelMixer.SetFloat("_SourceG", (int)ChannelSource.TexFirst_Alpha);
            channelMixer.SetFloat("_SourceB", (int)ChannelSource.TexFirst_Alpha);
            channelMixer.SetFloat("_SourceA", (int)ChannelSource.TexSecond_Red);
            return ApplyBlit(texMain, channelMixer);
        }

        public static Texture2D FillAlpha(Texture tex, float alpha = 1.0f)
        {
            return tex as Texture2D;
            Material mat = new Material(GetShader("Hidden/Blit/FillAlpha"));

            return ApplyBlit(tex, mat);
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
