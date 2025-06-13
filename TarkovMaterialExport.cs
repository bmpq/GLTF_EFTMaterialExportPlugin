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
        private const string TEXNAME_POSTFIX_SPECULAR = "_SPEC";
        private const string TEXNAME_POSTFIX_METALLICROUGHNESS = "_METROUGH";

        public override void AfterSceneExport(GLTFSceneExporter _, GLTFRoot __)
        {
            RenderTexture.active = null;
        }

        public override bool BeforeMaterialExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Material material, GLTFMaterial materialNode)
        {
            if (material.shader.name == "p0/Reflective/Bumped Animated Emissive Specular SMap")
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };

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

                Texture2D texMetRough = TextureConverter.GlosToMetRough(texGlos);
                texMetRough.name = texGlos.name + TEXNAME_POSTFIX_METALLICROUGHNESS;
                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, "_SpecMap", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                Texture2D texAlbedo = TextureConverter.FillAlpha(texAlbedoSpec, 1f);

                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texAlbedo, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));

                GLTF.Math.Color specularColor = KHR_materials_specular.COLOR_DEFAULT;

                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_Glossness");
                floatSpec = Mathf.Clamp01(floatSpec);
                floatSpec *= material.GetVector("_SpecVals").x;

                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();
                Texture2D texSpec = TextureConverter.ChannelToGrayscale(texAlbedoSpec, 3);
                texSpec.name = texAlbedoSpec.name + TEXNAME_POSTFIX_SPECULAR;
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                float floatGlos = material.GetFloat("_Specularness");
                float glossinessFactor = Mathf.Clamp01(floatGlos);
                pbr.RoughnessFactor = glossinessFactor;

                DeclareExtensionSpecular(exporter, materialNode, KHRspecular);

                materialNode.PbrMetallicRoughness = pbr;

                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                KHR_materials_emissive_strength emissiveStrength = new KHR_materials_emissive_strength();
                emissiveStrength.emissiveStrength = material.GetFloat("_EmissionPower");
                DeclareExtensionEmissiveStrength(exporter, materialNode, emissiveStrength);

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

                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1f };

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

                Texture2D texMetRough = TextureConverter.GlosToMetRough(texGlos);
                texMetRough.name = texGlos.name + TEXNAME_POSTFIX_METALLICROUGHNESS;
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

                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();
                Texture2D texSpec = TextureConverter.ChannelToGrayscale(texAlbedoSpec, 3);
                texSpec.name = texAlbedoSpec.name + TEXNAME_POSTFIX_SPECULAR;
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, TransparentCutoff ? "_SpecMap" : "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                DeclareExtensionSpecular(exporter, materialNode, KHRspecular);

                float floatGlos = material.GetFloat("_Specularness");
                float glossinessFactor = Mathf.Clamp01(floatGlos);
                pbr.RoughnessFactor = glossinessFactor;

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

                    KHR_materials_emissive_strength emissiveStrength = new KHR_materials_emissive_strength();
                    emissiveStrength.emissiveStrength = material.GetFloat("_EmissionPower") / 10f; // not sure about this

                    DeclareExtensionEmissiveStrength(exporter, materialNode, emissiveStrength);
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
                texSpec.name = texAlbedoSpec.name + TEXNAME_POSTFIX_SPECULAR;
                KHRspecular.specularTexture = exporter.ExportTextureInfoWithTextureTransform(material, texSpec, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                Color colorSpec = material.GetColor("_SpecColor");
                float floatSpec = material.GetFloat("_SpecPower");
                floatSpec = Mathf.Clamp01(floatSpec / 10f);  // dumbass approximation
                KHRspecular.specularFactor = floatSpec;
                KHRspecular.specularColorFactor = colorSpec.ToNumericsColorLinear();

                float floatGlos = material.GetFloat("_SpecPower") * material.GetFloat("_Shininess"); // dumbass approximation 2
                pbr.RoughnessFactor = 1f - floatGlos;

                materialNode.PbrMetallicRoughness = pbr;

                DeclareExtensionSpecular(exporter, materialNode, KHRspecular);

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

                    DeclareExtensionEmissiveStrength(exporter, materialNode, emissive);
                }

                return true;
            }
            else if (material.shader.name == "p0/Transparent/Reflective/Specular")
            {
                // _MainTex is diffuse with alpha
                // _SpecTex is glossiness
                // _ReflectColor is specular IOR factor, no tint

                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1.0f };
                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, material.mainTexture, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();

                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                KHRspecular.specularFactor = material.GetColor("_ReflectColor").r;
                DeclareExtensionSpecular(exporter, materialNode, KHRspecular);

                Texture texGlos = material.GetTexture("_SpecTex");
                Texture2D texMetRough = TextureConverter.GlosToMetRough(texGlos);
                texMetRough.name = texGlos.name + TEXNAME_POSTFIX_METALLICROUGHNESS;
                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, "_SpecTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));

                materialNode.PbrMetallicRoughness = pbr;
                materialNode.AlphaMode = AlphaMode.BLEND;

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

                pbr.MetallicRoughnessTexture = pbr.BaseColorTexture; // todo: check if this is correct

                KHR_materials_transmission KHRtransmission = new KHR_materials_transmission();
                KHRtransmission.transmissionFactor = 1f;
                DeclareExtensionTransmission(exporter, materialNode, KHRtransmission);

                materialNode.PbrMetallicRoughness = pbr;
                materialNode.AlphaMode = AlphaMode.BLEND;

                return true;
            }
            else if (material.shader.name.Contains("CW FX/Collimator"))
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 0 };

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;
                DeclareExtensionTransmission(exporter, materialNode, transmission);

                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }
            else if (material.shader.name.Contains("Custom/OpticGlass"))
            {
                // black opaque reflective material

                var pbr = new PbrMetallicRoughness() { MetallicFactor = 1f, RoughnessFactor = 0.05f };
                pbr.BaseColorFactor = Color.black.ToNumericsColorLinear();
                materialNode.PbrMetallicRoughness = pbr;

                return true;
            }

            else if (material.shader.name == "Transparent/DepthZwriteDithered")
            {
                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1f };

                Texture2D texBaseColor = TextureConverter.CreateGrayscaleFromAlpha(material.GetTexture("_SpecTex"));
                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texBaseColor, "_SpecTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));
                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();

                Texture texRough = TextureConverter.CreateGrayscaleFromAlpha(material.GetTexture("_MainTex"));
                Texture2D texMetRough = TextureConverter.GlosToMetRough(TextureConverter.Invert(texRough));
                texMetRough.name += TEXNAME_POSTFIX_METALLICROUGHNESS;
                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));
                pbr.RoughnessFactor = 1f;
                materialNode.PbrMetallicRoughness = pbr;

                KHR_materials_specular KHRspecular = new KHR_materials_specular();
                KHRspecular.specularFactor = material.GetFloat("_Glossness") / 5f; // goes up to 50, but has no noticable visual effect
                KHRspecular.specularColorFactor = material.GetColor("_SpecColor").ToNumericsColorLinear();
                DeclareExtensionSpecular(exporter, materialNode, KHRspecular);

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;
                DeclareExtensionTransmission(exporter, materialNode, transmission);

                materialNode.PbrMetallicRoughness = pbr;
                var normalTex = material.GetTexture("_BumpMap");
                if (normalTex && normalTex is Texture2D)
                {
                    materialNode.NormalTexture = exporter.ExportNormalTextureInfo(normalTex, TextureMapType.Normal, material);
                }

                return true;
            }
            else if (material.shader.name == "Transparent/DepthZwrite")
            {
                // transparency/glass is handled way different in the game engine vs pbr workflow
                // this is very much an artistic interpretation of the shader, even more so than in the other shaders

                var pbr = new PbrMetallicRoughness() { MetallicFactor = 0, RoughnessFactor = 1f };
                pbr.BaseColorFactor = material.GetColor("_Color").ToNumericsColorLinear();

                Texture texDiffuseAlpha = TextureConverter.CreateGrayscaleFromAlpha(material.GetTexture("_MainTex"));
                Texture texA = TextureConverter.Power(texDiffuseAlpha, 2f);
                Texture texB = TextureConverter.Invert(material.GetTexture("_SpecTex"));
                Texture texRough = TextureConverter.Multiply(texA, texB);
                Texture texMetRough = TextureConverter.RoughToMetRough(texRough);
                texMetRough.name += TEXNAME_POSTFIX_METALLICROUGHNESS;

                pbr.MetallicRoughnessTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMetRough, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Custom_Unknown));
                materialNode.PbrMetallicRoughness = pbr;

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;
                DeclareExtensionTransmission(exporter, materialNode, transmission);

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

                Texture texWithAlpha = material.HasTexture("_CutoutMask") ? material.GetTexture("_CutoutMask") : Texture2D.whiteTexture;
                Texture2D mainTex = TextureConverter.OverrideAlpha(material.GetTexture("_MainTex"), texWithAlpha);

                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, mainTex, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.BaseColor));
                materialNode.AlphaMode = AlphaMode.MASK;

                Texture2D texRoughness = TextureConverter.Invert(material.GetTexture("_GlossMap"));
                Texture2D texMetallicRoughness = TextureConverter.PackGrayscaleTextureToOneChannel(texRoughness, 1);
                texMetallicRoughness.name += TEXNAME_POSTFIX_METALLICROUGHNESS;

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

                Texture2D texMain = TextureConverter.PackGrayscaleTextureToOneChannel(material.GetTexture("_MainTex"), 3);

                pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, texMain, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Linear));

                KHR_materials_transmission transmission = new KHR_materials_transmission();
                transmission.transmissionFactor = 1f;
                DeclareExtensionTransmission(exporter, materialNode, transmission);

                materialNode.PbrMetallicRoughness = pbr;

                materialNode.AlphaMode = AlphaMode.BLEND;
                DeclareDisableShadow(materialNode);

                return true;
            }
            else if (material.shader.name == "Decal/Ultra Deferred Decal Of God 3000")
            {
                var pbr = new PbrMetallicRoughness();

                pbr.BaseColorFactor = Vector4ToColor(material.GetVector("_Color")).ToNumericsColorLinear();

                var baseTex = material.GetTexture("_MainTex");
                if (baseTex)
                {
                    pbr.BaseColorTexture = exporter.ExportTextureInfoWithTextureTransform(material, baseTex, "_MainTex", exporter.GetExportSettingsForSlot(TextureMapType.Linear));
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

                DeclareDisableShadow(materialNode);

                return true;
            }

            return false;
        }

        // in blender can then use a custom script to disable shadows based on this imported custom property
        private static void DeclareDisableShadow(GLTFMaterial materialNode)
        {
            var extras = new JObject();
            extras.Add("disabledShadow", true);
            materialNode.Extras = extras;
        }

        private static void DeclareExtensionSpecular(GLTFSceneExporter exporter, GLTFMaterial materialNode, KHR_materials_specular KHRspecular)
        {
            exporter.DeclareExtensionUsage(KHR_materials_specular_Factory.EXTENSION_NAME, true);
            if (materialNode.Extensions == null)
                materialNode.Extensions = new Dictionary<string, IExtension>();
            materialNode.Extensions[KHR_materials_specular_Factory.EXTENSION_NAME] = KHRspecular;
        }

        private static void DeclareExtensionTransmission(GLTFSceneExporter exporter, GLTFMaterial materialNode, KHR_materials_transmission KHRtransmission)
        {
            exporter.DeclareExtensionUsage(KHR_materials_transmission_Factory.EXTENSION_NAME, true);
            if (materialNode.Extensions == null)
                materialNode.Extensions = new Dictionary<string, IExtension>();
            materialNode.Extensions[KHR_materials_transmission_Factory.EXTENSION_NAME] = KHRtransmission;

            // could potentially declare transmission to not have any shadow, for cycles render to look better,
            // as opposed to the 'Is Shadow Ray' hack, but then I'd lose control over how much shadow to cast, so I won't use it for now
            // also it will break everything if there is also opaque materials on the same object
            // DisableShadow(materialNode);
        }

        private static void DeclareExtensionEmissiveStrength(GLTFSceneExporter exporter, GLTFMaterial materialNode, KHR_materials_emissive_strength KHRemissive_strength)
        {
            exporter.DeclareExtensionUsage(KHR_materials_emissive_strength_Factory.EXTENSION_NAME, true);
            if (materialNode.Extensions == null)
                materialNode.Extensions = new Dictionary<string, IExtension>();
            materialNode.Extensions[KHR_materials_emissive_strength_Factory.EXTENSION_NAME] = KHRemissive_strength;
        }

        static Color Vector4ToColor(Vector4 vector4) => new Color(vector4.x, vector4.y, vector4.z, vector4.w);
    }
}
