using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace StellarFramework.Verification.Editor
{
    /// <summary>Dev-only export evidence. Generated packages stay outside Assets.</summary>
    public static class ConsumerExportVerification
    {
        private const string EvidenceRoot = "BuildArtifacts/Verification/ConsumerExport";

        [MenuItem("Tools/StellarFramework/Verification/Export Consumer Regression Matrix")]
        public static void ExportMatrix()
        {
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("StellarFramework.Editor.AssetsMapGenerator"))
                .First(type => type != null).GetMethod("GenerateAndLog").Invoke(null, null);
            var report = new ExportReport { status = "RUNNING" };
            Directory.CreateDirectory(EvidenceRoot);
            Type publisher = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("StellarFramework.Editor.Modules.StellarFrameworkPackagePublisher"))
                .First(type => type != null);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            MethodInfo exporter = publisher.GetMethod("ExportKitPackageGroupToDirectoryInternal", flags);
            var profiles = (IEnumerable)publisher.GetMethod("GetSourceProjectExportProfiles", flags).Invoke(null, null);
            var plans = profiles.Cast<object>().Select(profile => new ExportPlan
            {
                name = (string)profile.GetType().GetField("id").GetValue(profile),
                ids = new[] { (string)profile.GetType().GetField("id").GetValue(profile) }
            }).ToList();
            plans.Add(new ExportPlan { name = "resources-and-assetbundle", ids = new[] { "reskit.resources", "reskit.assetbundle" } });
            plans.Add(new ExportPlan { name = "yooasset-resource-and-code-update", ids = new[] { "reskit.yooasset", "reskit.contentupdate.yooasset", "reskit.codeupdate.hybridclr" } });
            plans.Add(new ExportPlan { name = "http-and-localization", ids = new[] { "httpkit", "localizationkit.core" } });
            try
            {
                foreach (ExportPlan plan in plans)
                {
                    string output = (string)exporter.Invoke(null, new object[]
                    {
                        plan.ids, plan.name + ".unitypackage", Path.GetFullPath(EvidenceRoot), false
                    });
                    report.packages.Add(new ExportPackage { name = plan.name, path = output, profileIds = plan.ids });
                    File.WriteAllText(EvidenceRoot + "/export-result.json", JsonUtility.ToJson(report, true));
                }
                report.status = "PASS";
            }
            catch (Exception exception)
            {
                report.status = "FAIL";
                report.error = (exception.InnerException ?? exception).ToString();
                Debug.LogException(exception);
            }
            File.WriteAllText(EvidenceRoot + "/export-result.json", JsonUtility.ToJson(report, true));
            Debug.Log($"[ConsumerExportVerification] {report.status}, packages={report.packages.Count}");
        }

        private sealed class ExportPlan { public string name; public string[] ids; }
        [Serializable] private sealed class ExportReport
        {
            public string status;
            public string error;
            public List<ExportPackage> packages = new List<ExportPackage>();
        }
        [Serializable] private sealed class ExportPackage
        {
            public string name;
            public string path;
            public string[] profileIds;
        }
    }
}
