using UnityEditor;
using UnityEngine;

namespace PigTycoon.Editor
{
    [InitializeOnLoad]
    public static class EmoteSpriteVerifier
    {
        static EmoteSpriteVerifier()
        {
            EditorApplication.delayCall += VerifyEmotes;
        }

        [MenuItem("PigTycoon/Verify Emote Sprites")]
        public static void VerifyEmotes()
        {
            string[] testKeys = new string[]
            {
                "❤️", "<3", "5 Tim", "Cám", "Đói", "Nước", "Khát", "Zzz", "Ngủ", "Bùn", "?", "SOS", "🆘", "👽", "ufo", "No nê", "Khỏe mạnh", "..."
            };

            int success = 0;
            foreach (var key in testKeys)
            {
                var sp = Presentation.UISpriteLoader.GetEmoteSprite(key);
                if (sp != null)
                {
                    success++;
                }
                else
                {
                    Debug.LogError($"[EmoteSpriteVerifier] FAILED to load emote for key: '{key}'");
                }
            }

            Debug.Log($"[EmoteSpriteVerifier] SUCCESS: {success}/{testKeys.Length} emotes verified and loaded.");
        }
    }
}
