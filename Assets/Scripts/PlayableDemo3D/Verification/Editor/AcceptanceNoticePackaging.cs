using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PigTycoon.PlayableDemo3D.Verification.EditorTools
{
    /// <summary>Packages the full checked license whenever imported Timeline motion is shipped.</summary>
    public sealed class AcceptanceNoticePackaging : IPostprocessBuildWithReport
    {
        public int callbackOrder => 10;
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64) return;
            bool adopted = report.GetFiles().Any(f => f.path.Replace('\\','/').Contains("TimelineMotionCandidates"));
            // Packed build-file paths need not expose source assets. Also inspect the exact scene's
            // dependency graph so embedded clips cannot omit their notice because of packing.
            foreach (string scene in report.summary.outputPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? Environment.GetCommandLineArgs().SkipWhile(a => a != "-demoScene").Skip(1).Take(1) : Array.Empty<string>())
                adopted |= AssetDatabase.GetDependencies(scene,true).Any(p => p.Contains("TimelineMotionCandidates"));
            if (!adopted) return;
            Type helper = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PigTycoon.PlayableDemo3D.EditorTools.TimelineHumanoidAnimationImport")).FirstOrDefault(t=>t!=null);
            MethodInfo copy = helper?.GetMethod("CopyDeliveryNotices", BindingFlags.Public|BindingFlags.Static);
            if (copy == null) throw new BuildFailedException("Timeline motion is packaged but its checked full-license delivery helper is missing.");
            string directory = Path.GetDirectoryName(Path.GetFullPath(report.summary.outputPath));
            string notices = (string)copy.Invoke(null,new object[]{directory,null});
            if (!File.Exists(Path.Combine(notices,"UNITY-COMPANION-LICENSE-v1.4.txt")))
                throw new BuildFailedException("Full Unity Companion License was not copied to the player delivery.");
            Debug.Log("[PlayableDemoBuild] Timeline full notices: "+notices);
        }
    }
}
