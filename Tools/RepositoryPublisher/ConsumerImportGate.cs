using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public static class ConsumerImportGate
{
    private const string Prefix = "ConsumerImportGate.";

    static ConsumerImportGate()
    {
        if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
    }

    public static void Run()
    {
        string[] args = Environment.GetCommandLineArgs();
        string Argument(string name) => args.SkipWhile(value => value != name).Skip(1).First();
        SessionState.SetBool(Prefix + "active", true);
        SessionState.SetString(Prefix + "report", Argument("-consumerReport"));
        SessionState.SetString(Prefix + "assembly", Argument("-consumerAssembly"));
        SessionState.SetFloat(Prefix + "start", (float)EditorApplication.timeSinceStartup);
        SessionState.SetString(Prefix + "error", string.Empty);
        Subscribe();
        AssetDatabase.ImportPackage(Argument("-consumerPackage"), false);
    }

    private static void Subscribe()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        CompilationPipeline.assemblyCompilationFinished -= Compiled;
        CompilationPipeline.assemblyCompilationFinished += Compiled;
    }

    private static void Compiled(string assembly, CompilerMessage[] messages)
    {
        string error = string.Join("\n", messages.Where(message => message.type == CompilerMessageType.Error)
            .Select(message => message.message));
        if (!string.IsNullOrEmpty(error)) SessionState.SetString(Prefix + "error", error);
    }

    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string error = SessionState.GetString(Prefix + "error", string.Empty);
        if (!string.IsNullOrEmpty(error)) { Finish(false, error); return; }
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Prefix + "start", 0) > 240)
        { Finish(false, "Installer did not finish within 240 seconds."); return; }
        string assembly = SessionState.GetString(Prefix + "assembly", string.Empty);
        string bootstrap = "Assets/Editor/StellarFramework/KitPackageBootstrap";
        bool pending = Directory.Exists(bootstrap) && Directory.GetFiles(bootstrap, "__StellarFramework-KitBootstrap-*.json").Length > 0;
        bool loaded = AppDomain.CurrentDomain.GetAssemblies().Any(value => value.GetName().Name == assembly);
        if (loaded && !pending) Finish(true, string.Empty);
    }

    private static void Finish(bool success, string error)
    {
        SessionState.SetBool(Prefix + "active", false);
        EditorApplication.update -= Tick;
        string[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(value => value.GetName().Name)
            .Where(name => name.StartsWith("StellarFramework", StringComparison.Ordinal)).OrderBy(name => name).ToArray();
        var report = new Report { status = success ? "PASS" : "FAIL", error = error, assemblies = assemblies,
            manifest = File.ReadAllText("Packages/manifest.json") };
        File.WriteAllText(SessionState.GetString(Prefix + "report", string.Empty), JsonUtility.ToJson(report, true));
        Debug.Log("[ConsumerImportGate] " + report.status + " " + error);
        EditorApplication.Exit(success ? 0 : 1);
    }

    [Serializable] private sealed class Report
    {
        public string status;
        public string error;
        public string[] assemblies;
        public string manifest;
    }
}
