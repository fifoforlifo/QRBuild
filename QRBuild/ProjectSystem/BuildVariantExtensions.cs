using System;
using System.Text;

namespace QRBuild.ProjectSystem
{
    public static class BuildVariantExtensions
    {
        public static string ToVariantString(this BuildVariant variant)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < variant.Elements.Count; i++) {
                BuildVariantElementInfo elementInfo = variant.Elements[i];
                string value = elementInfo.GetValue(variant);
                builder.Append(value);
                if (i < variant.Elements.Count - 1) {
                    builder.Append(".");
                }
            }
            string result = builder.ToString();
            return result;
        }

        public static void FromVariantString(
            this BuildVariant variant,
            string variantString)
        {
            string trimmedValue = variantString.Trim();
            string[] values = trimmedValue.Split('.');
            int count = Math.Min(values.Length, variant.Elements.Count);
            for (int i = 0; i < count; i++) {
                string value = values[i];
                if (!String.IsNullOrEmpty(value)) {
                    variant.Elements[i].SetValue(variant, value);
                }
            }
        }

        public static string GetVariantStringFormat(this BuildVariant variant)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < variant.Elements.Count; i++) {
                BuildVariantElementInfo elementInfo = variant.Elements[i];
                builder.Append(elementInfo.Name);
                if (i < variant.Elements.Count - 1) {
                    builder.Append(".");
                }
            }
            string result = builder.ToString();
            return result;
        }

        public static string GetVariantStringOptions(this BuildVariant variant)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < variant.Elements.Count; i++) {
                BuildVariantElementInfo elementInfo = variant.Elements[i];
                builder.AppendFormat("{0}: ", elementInfo.Name);
                if (elementInfo.ElementType.IsEnum) {
                    // print enum elements
                    builder.Append(" {");
                    string[] names = Enum.GetNames(elementInfo.ElementType);
                    for (int j = 0; j < names.Length; j++) {
                        builder.Append(names[j]);
                        if (j < names.Length - 1) {
                            builder.Append(",");
                        }
                    }
                    builder.Append("}");
                }
                else {
                    // print out the type
                    builder.Append(elementInfo.ElementType.Name);
                }
                builder.Append("\n");
            }
            string result = builder.ToString();
            return result;
        }

    }
}
