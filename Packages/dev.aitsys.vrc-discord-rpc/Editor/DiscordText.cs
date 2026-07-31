using System.Globalization;
using System.Text;

namespace AITSYS.VRCUnity.DiscordRPC
{
    internal static class DiscordText
    {
        internal const int ActivityTextLimit = 128;

        internal static string ClampActivityText(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= ActivityTextLimit)
                return value;

            var builder = new StringBuilder(ActivityTextLimit);
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(value);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (builder.Length + element.Length > ActivityTextLimit)
                    break;

                builder.Append(element);
            }

            return builder.ToString();
        }
    }
}
