using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityGLTF.Extensions;
using static UnityGLTF.GLTFSceneExporter;

namespace UnityGLTF.Plugins
{
	public class TarkovMaterialExport : GLTFExportPlugin
	{
		public override string DisplayName => "Convert Tarkov shaders and textures";
		public override string Description => "";
		public override GLTFExportPluginContext CreateInstance(ExportContext context)
		{
			return new TarkovMaterialExportContext();
		}
	}
	
	public class TarkovMaterialExportContext : GLTFExportPluginContext
    {
        public override void AfterSceneExport(GLTFSceneExporter _, GLTFRoot __)
		{
			RenderTexture.active = null;
		}

		public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            if (material.shader.name == "p0/Reflective/Bumped Animated Emissive Specular SMap")
            {
                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                var pbr = new PbrMetallicRoughness();
                pbr.MetallicFactor = 0;

                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
                float floatDiffuse = material.GetVector("_DefVals").x;
                pbr.BaseColorFactor.R *= floatDiffuse;
                pbr.BaseColorFactor.G *= floatDiffuse;
                pbr.BaseColorFactor.B *= floatDiffuse;

                Texture texAlbedoSpec = material.GetTexture("_MainTex");
                Texture texGlos = material.GetTexture("_SpecMap");

                if (texGlos == null)
                    texGlos = Texture2D.whiteTexture;
                if (texAlbedoSpec == null)
                    texAlbedoSpec = Texture2D.whiteTexture;

                Texture2D texRoughness = TextureConverter.Invert(texGlos, false);
                Texture2D texMetRough = TextureConverter.CombineR(Texture2D.blackTexture, texRoughness, Texture2D.blackTexture, Texture2D.whiteTexture);
                texMetRough.name = texGlos.name + "_METROUGH";
                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, "_SpecMap", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                Texture2D texAlbedo = TextureConverter.FillAlpha(texAlbedoSpec, 1f);

                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texAlbedo, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));

                GLTF.Math.Color specularColor = KHR_materials_specular.COLOR_DEFAULT;

                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_Glossness");
                floatSpec = Mathf.Clamp01(floatSpec);
                floatSpec *= material.GetVector("_SpecVals").x;

                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();
                Texture2D texSpec = TextureConverter.ChannelToGrayscale(texAlbedoSpec, 3);
                texSpec.name = texAlbedoSpec.name + "_SPEC";
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                float floatGlos = material.GetFloat("_Specularness");
                float glossinessFactor = Mathf.Clamp01(floatGlos);
                pbr.RoughnessFactor = glossinessFactor;

                exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_specular_Factory.EXTENSION_NAME] = KHRspecular;

                materialNode.PbrMetallicRoughness = pbr;

                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                KHR_materials_emissive_strength emissive = new KHR_materials_emissive_strength();
                emissive.emissiveStrength = material.GetFloat("_EmissionPower");
                exporter.DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, true);
                materialNode.Extensions[KHR_materials_emissive_strength_Factory.EXTENSION_NAME] = emissive;

                if (material.HasTexture("_EmissionMapAnim1"))
                {
                    materialNode.EmissiveTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture("_EmissionMapAnim1"), "_EmissionMapAnim1", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                    materialNode.EmissiveFactor = material.GetColor("_EmAnim1Color").ToNumericsColorLinear();
                }
                if (material.HasTexture("_EmissionMapAnim2"))
                {
                    // only export the file, can't do much else
                    exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture("_EmissionMapAnim2"), "_EmissionMapAnim2", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                }

                return true;
            }
            else if (material.shader.name.Contains("SMap") && material.shader.name.Contains("Reflective"))
            {
                bool TransparentCutoff = material.shader.name.Contains("Transparent Cutoff");

                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                var pbr = new PbrMetallicRoughness();
                pbr.MetallicFactor = 0;

                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
                float floatDiffuse = material.GetVector("_DefVals").x;
                pbr.BaseColorFactor.R *= floatDiffuse;
                pbr.BaseColorFactor.G *= floatDiffuse;
                pbr.BaseColorFactor.B *= floatDiffuse;

                Texture texAlbedoSpec = material.GetTexture("_MainTex");
                Texture texGlos = material.GetTexture("_SpecMap");
                if (TransparentCutoff)
                {
                    // yes this is the correct setup in the 'Transparent Cutoff' shader
                    texAlbedoSpec = material.GetTexture("_SpecMap");
                    texGlos = material.GetTexture("_MainTex");
                }

                if (texGlos == null)
                    texGlos = Texture2D.whiteTexture;
                if (texAlbedoSpec == null)
                    texAlbedoSpec = Texture2D.whiteTexture;

                Texture2D texRoughness = TextureConverter.Invert(texGlos, false);
                Texture2D texMetRough = TextureConverter.CombineR(Texture2D.blackTexture, texRoughness, Texture2D.blackTexture, Texture2D.whiteTexture);
                texMetRough.name = texGlos.name + "_METROUGH";
                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, TransparentCutoff ? "_MainTex" : "_SpecMap", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                Texture2D texAlbedo = TextureConverter.FillAlpha(texAlbedoSpec, 1f);

                if (TransparentCutoff)
                    materialNode.AlphaMode = AlphaMode.MASK;

                if (material.HasFloat("_HasTint") && material.GetFloat("_HasTint") > 0.5f)
                {
                    Color colorTint = material.GetColor("_BaseTintColor");
                    Texture2D texDiffuseWithTint = TextureConverter.BlendOverlay(
                        texAlbedo as Texture2D, 
                        TextureConverter.CreateSolidColorTexture(2, 2, colorTint), 
                        material.GetTexture("_TintMask") as Texture2D, 1f);

                    texDiffuseWithTint.name = texAlbedoSpec.name + "_" + ColorUtility.ToHtmlStringRGB(colorTint);

                    pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texDiffuseWithTint, TransparentCutoff ? "_SpecMap" : "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                }
                else
                {
                    pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texAlbedo, TransparentCutoff ? "_SpecMap" : "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                }

                GLTF.Math.Color specularColor = KHR_materials_specular.COLOR_DEFAULT;

                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_Glossness");
                floatSpec = Mathf.Clamp01(floatSpec);
                floatSpec *= material.GetVector("_SpecVals").x;

                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();
                Texture2D texSpec = TextureConverter.ChannelToGrayscale(texAlbedoSpec, 3);
                texSpec.name = texAlbedoSpec.name + "_SPEC";
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, TransparentCutoff ? "_SpecMap" : "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                float floatGlos = material.GetFloat("_Specularness");
                float glossinessFactor = Mathf.Clamp01(floatGlos);
                pbr.RoughnessFactor = glossinessFactor;

                exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_specular_Factory.EXTENSION_NAME] = KHRspecular;

                materialNode.PbrMetallicRoughness = pbr;

                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex != null)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                if (material.shader.name.Contains("Emissive") && material.HasColor("_EmissiveColor"))
                {
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    materialNode.EmissiveTexture = exporter.ExportTextureInfoWithTextureTransform(material, texEmission, "_EmissionMap", exporter.GetExportSettingsForSlot(TextureMapType.sRGB));

                    materialNode.EmissiveFactor = material.GetColor("_EmissiveColor").ToNumericsColorLinear();

                    KHR_materials_emissive_strength emissive = new KHR_materials_emissive_strength();
                    emissive.emissiveStrength = material.GetFloat("_EmissionPower") / 10f; // not sure about this

                    exporter.DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, true);
                    materialNode.Extensions[KHR_materials_emissive_strength_Factory.EXTENSION_NAME] = emissive;
                }

                return true;
            }
            else if (material.shader.name == "p0/Reflective/Bumped Specular" || material.shader.name == "p0/Reflective/Bumped Emissive Specular")
            {
                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                var pbr = new PbrMetallicRoughness();
                pbr.MetallicFactor = 0;

                Color diffuseFactor = material.GetColor("_Color");
                float floatDiffuse = material.GetVector("_DefVals").x;
                diffuseFactor.r *= floatDiffuse;
                diffuseFactor.g *= floatDiffuse;
                diffuseFactor.b *= floatDiffuse;
                pbr.BaseColorFactor = diffuseFactor.ToNumericsColorLinear();

                Texture texAlbedoSpec = material.GetTexture("_MainTex");
                if (texAlbedoSpec == null)
                    texAlbedoSpec = Texture2D.whiteTexture;
                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texAlbedoSpec, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));

                Texture2D texSpec = TextureConverter.ChannelToGrayscale(texAlbedoSpec, 3);
                texSpec.name = texAlbedoSpec.name + "_SPEC";
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_SpecPower");
                floatSpec = Mathf.Clamp01(floatSpec / 10f);  // dumbass approximation
                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();

                float floatGlos = material.GetFloat("_SpecPower") * material.GetFloat("_Shininess"); // dumbass approximation 2
                pbr.RoughnessFactor = 1f - floatGlos;

                materialNode.PbrMetallicRoughness = pbr;

                exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_specular_Factory.EXTENSION_NAME] = KHRspecular;

                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                    // exporter.ExportTextureInfoWithTextureTransform(material, normalTex, "_BumpMap", exporter.GetExportSettingsForSlot(TextureMapType.Normal));
                    // the normal texture tiling isn't used in-game, but some materials have random values for some reason, so we omit exporting transform for normals
                }

                if (material.HasFloat("_EmissionVisibility"))
                {
                    materialNode.EmissiveTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.GetTexture("_EmissionMap"), "_EmissionMap", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));

                    KHR_materials_emissive_strength emissive = new KHR_materials_emissive_strength();
                    emissive.emissiveStrength = material.GetFloat("_EmissionPower") / 10f; // there is also _EmissionVisibility, but it seems to multiply diffuse, which doesn't make sense to me, black emission shouldn't make diffuse black

                    exporter.DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, true);
                    materialNode.Extensions[KHR_materials_emissive_strength_Factory.EXTENSION_NAME] = emissive;
                }

                return true;
            }
            else if (material.shader.name == "p0/Cutout/Bumped Diffuse")
            {
                // cant remember why this is empty, probably because default UnityGLTF settings export it fine
                material.EnableKeyword("_BUMPMAP");
            }
            else if (material.shader.name == "Global Fog/Transparent Reflective Specular")
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.mainTexture, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
                pbr.BaseColorFactor.A = 1f;

                pbr.MetallicRoughnessTexture = pbr.BaseColorTexture;
                pbr.RoughnessFactor = 1f;
                pbr.MetallicFactor = 0f;

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;

                exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = transmission;

                materialNode.PbrMetallicRoughness = pbr;
                materialNode.AlphaMode = AlphaMode.BLEND;

                return true;
            }
            else if (material.shader.name.Contains("CW FX/Collimator"))
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 0 };

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;

                exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = transmission;

                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }
            else if (material.shader.name.Contains("Custom/OpticGlass"))
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 1f, RoughnessFactor = 0.05f };
                pbr.BaseColorFactor = Color.black.ToNumericsColorLinear();
                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }

            else if (material.shader.name.Contains("Transparent/DepthZwrite"))
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

                Material channelMixer = new Material(Shader.Find("Hidden/Blit/ChannelMixer"));
                Texture2D texMain = TextureConverter.Invert(material.GetTexture("_MainTex"));
                channelMixer.SetTexture("_TexFirst", texMain);
                channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
                channelMixer.SetFloat("_SourceR", (int)ChannelSource.TexFirst_Alpha);
                channelMixer.SetFloat("_SourceG", (int)ChannelSource.TexFirst_Alpha);
                channelMixer.SetFloat("_SourceB", (int)ChannelSource.TexFirst_Alpha);
                channelMixer.SetFloat("_SourceA", (int)ChannelSource.TexSecond_Red);
                Texture2D texTransmission = TextureConverter.Convert(texMain, channelMixer, "TRANSMISSION");
                texTransmission = TextureConverter.Invert(texTransmission);

                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();
                pbr.BaseColorFactor.A = 1f;

                pbr.RoughnessFactor = 0f;
                pbr.MetallicFactor = 0f;

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;
                transmission.transmissionTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMain, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = transmission;

                materialNode.PbrMetallicRoughness = pbr;


                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                return true;
            }
            else if (material.shader.name.Contains("Cloth/ClothShader"))
            {
                var pbr = new PbrMetallicRoughness();

                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();

                Material setAlpha = new Material(Shader.Find("Hidden/Blit/SetAlphaFromTexture"));
                if (material.HasTexture("_CutoutMask"))
                    setAlpha.SetTexture("_AlphaTex", material.GetTexture("_CutoutMask"));
                else
                    setAlpha.SetTexture("_AlphaTex", Texture2D.whiteTexture);
                Texture mainTex = TextureConverter.Convert(material.GetTexture("_MainTex"), setAlpha);
                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, mainTex, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                materialNode.AlphaMode = AlphaMode.MASK;

                Material channelMixer = new Material(Shader.Find("Hidden/Blit/ChannelMixer"));
                Texture2D texRoughness = TextureConverter.Invert(material.GetTexture("_GlossMap"));
                channelMixer.SetTexture("_TexFirst", texRoughness);
                channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
                channelMixer.SetFloat("_SourceR", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceG", (int)ChannelSource.TexFirst_Red);
                channelMixer.SetFloat("_SourceB", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceA", (int)ChannelSource.TexSecond_Red);
                Texture2D texMetallicRoughness = TextureConverter.Convert(material.GetTexture("_GlossMap"), channelMixer, "MR");

                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetallicRoughness, "_GlossMap", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                pbr.RoughnessFactor = 1f - material.GetFloat("_Glossiness");
                pbr.MetallicFactor = (material.GetFloat("_Metallic") + 1f) / 2f;

                var normalTex = material.GetTexture("_NormalMap1");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }
            else if (material.shader.name.Contains("Vert Paint Shader"))
            {
                // unexportable
                // the shader logic must be remade manually in blender

                // just export the textures for finding convenience, need to deal with them manually later
                // extensively uses vertex colors
                exporter.ExportTextureInfo(material.GetTexture("_Heights"), TextureMapType.Linear);

                exporter.ExportTextureInfo(material.GetTexture("_MainTex0"), TextureMapType.Linear);
                exporter.ExportTextureInfo(material.GetTexture("_MainTex1"), TextureMapType.Linear);
                exporter.ExportTextureInfo(material.GetTexture("_MainTex2"), TextureMapType.Linear);

                exporter.ExportTextureInfo(material.GetTexture("_BumpMap0"), TextureMapType.Normal);
                exporter.ExportTextureInfo(material.GetTexture("_BumpMap1"), TextureMapType.Normal);
                exporter.ExportTextureInfo(material.GetTexture("_BumpMap2"), TextureMapType.Normal);

                // maybe think of something. it must be possible to bake with a shader. not sure about tiling tho

                if (material.shader.name.Contains("Gloss"))
                {
                    exporter.ExportTextureInfo(material.GetTexture("_SpecTex0"), TextureMapType.Linear);
                    exporter.ExportTextureInfo(material.GetTexture("_SpecTex1"), TextureMapType.Linear);
                    exporter.ExportTextureInfo(material.GetTexture("_SpecTex2"), TextureMapType.Linear);
                }

                // skip material exporting completely
                return true;
            }
            else if (material.shader.name == "Decal/Water Deferred Decal")
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 0 };

                Material channelMixer = new Material(Shader.Find("Hidden/Blit/ChannelMixer"));
                Texture2D texMain = material.GetTexture("_MainTex") as Texture2D;
                channelMixer.SetTexture("_TexFirst", texMain);
                channelMixer.SetTexture("_TexSecond", Texture2D.whiteTexture);
                channelMixer.SetFloat("_SourceR", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceG", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceB", (int)ChannelSource.TexSecond_Red);
                channelMixer.SetFloat("_SourceA", (int)ChannelSource.TexFirst_Red);

                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMain, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Linear));

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;

                exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
                if (materialNode.Extensions == null)
                    materialNode.Extensions = new Dictionary<string, IExtension>();
                materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = transmission;

                materialNode.PbrMetallicRoughness = pbr;

                materialNode.AlphaMode = AlphaMode.BLEND;

                // in blender can disable shadows with a script, based on this imported custom property
                var extras = new JObject();
                extras.Add("disabledShadow", true);
                materialNode.Extras = extras;

                return true;
            }
            else if (material.shader.name == "Decal/Ultra Deferred Decal Of God 3000")
            {
                var pbr = new PbrMetallicRoughness();

                pbr.BaseColorFactor = Vector4ToColor(material.GetVector("_Color")).ToNumericsColorLinear();

                var baseTex = material.GetTexture("_MainTex");
                if (baseTex)
                {
                    pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, baseTex, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                }

                pbr.MetallicFactor = 0f;
                pbr.RoughnessFactor = 0.85f;

                materialNode.PbrMetallicRoughness = pbr;

                materialNode.AlphaMode = AlphaMode.BLEND;

                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                var extras = new JObject();
                extras.Add("disabledShadow", true);
                materialNode.Extras = extras;

                return true;
            }

            return false;
		}

        static Color Vector4ToColor(Vector4 vector4) => new Color(vector4.x, vector4.y, vector4.z, vector4.w);
    }
}
