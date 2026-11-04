using System.Collections.Generic;
using System.Text;
using PigTycoon.Core;
using UnityEngine;

namespace PigTycoon.Presentation
{
    /// <summary>
    /// The forest edge holds one glade per in-forest event template (compose_forest.py: objects named
    /// "site:&lt;id&gt;..." / "site_prop:&lt;id&gt;..."). Only the glade of the expedition actually running is furnished;
    /// the others stay empty clearings, so each event reads as its own place (GDD G1225-1238).
    /// </summary>
    public sealed class ForestEventSites : MonoBehaviour
    {
        public static string ActiveSiteId { get; private set; }
        private MysteryEventId? shown;

        /// <summary>"E07_GiamDinhVoDanh" -> "giam_dinh_vo_danh".</summary>
        public static string SiteIdFor(MysteryEventId id)
        {
            string name = id.ToString();
            int cut = name.IndexOf('_');
            if (cut >= 0) name = name.Substring(cut + 1);
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private void Update()
        {
            var engine = MobileGameController.Instance?.Engine;
            if (engine == null) return;
            var exp = engine.ForestAndEvents.Expedition;
            MysteryEventId? current = exp.IsActive ? exp.EventId : (MysteryEventId?)null;
            if (current == shown && ActiveSiteId != null) return;
            shown = current;
            ActiveSiteId = current.HasValue ? SiteIdFor(current.Value) : null;
            Apply();
        }

        private void Apply()
        {
            var world = GameObject.Find("World");
            if (world == null) return;
            var toggles = new List<Transform>();
            foreach (Transform t in world.transform) toggles.Add(t);
            foreach (var t in toggles)
            {
                string n = t.name;
                string id = null;
                if (n.StartsWith("site:")) id = n.Substring(5);
                else if (n.StartsWith("site_prop:")) id = n.Substring(10);
                if (id == null) continue;
                int end = id.IndexOfAny(new[] { ':', ' ' });
                if (end >= 0) id = id.Substring(0, end);
                t.gameObject.SetActive(ActiveSiteId != null && id == ActiveSiteId);
            }
            Debug.Log($"[ForestEventSites] active site: {ActiveSiteId ?? "none"}");
        }
    }
}
