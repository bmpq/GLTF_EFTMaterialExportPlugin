using static UnityGLTF.GLTFSceneExporter;

namespace UnityGLTF.Plugins
{
    public class SanitizeTextureNames : GLTFExportPlugin
    {
        public override string DisplayName => "Sanitize Texture Names";
        public override string Description => "";
        public override GLTFExportPluginContext CreateInstance(ExportContext context)
        {
            return new SanitizeTextureNamesContext();
        }
    }

    public class SanitizeTextureNamesContext : GLTFExportPluginContext
    {
        private const string UNNAMED_TEXTURE = "Unnamed_Texture";

        public override void BeforeTextureExport(GLTFSceneExporter exporter, ref UniqueTexture texture, string textureSlot)
        {
            texture.Texture.name = SanitizeName(texture.Texture.name);

            if (texture.Texture.name == UNNAMED_TEXTURE)
                texture.Texture.name += texture.GetHashCode();
        }

        public static string SanitizeName(string inputName, char replacementChar = '_')
        {
            if (string.IsNullOrEmpty(inputName))
            {
                return UNNAMED_TEXTURE;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(inputName.Length);

            foreach (char c in inputName)
            {
                if (char.IsLetterOrDigit(c) || c == replacementChar)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(replacementChar);
                }
            }

            return sb.ToString();
        }
    }
}
