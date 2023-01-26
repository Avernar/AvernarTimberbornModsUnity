using HarmonyLib;
using TimberApi.ConsoleSystem;
using TimberApi.ModSystem;
using UnityEngine;

namespace Avernar.Gauge {
    public class Plugin : IModEntrypoint
    {
        public const string PluginGuid = "avernar.advancedstreamgauge";
        public const string PluginName = "AdvancedStreamGauge";
        public const string PluginVersion = "1.1.4.1";

        public static IConsoleWriter Log;

        public void Entry(IMod mod, IConsoleWriter consoleWriter) {
            Log = consoleWriter;
            Debug.Log($"Loaded {PluginName} Script");
            new Harmony(PluginGuid).PatchAll();
        }
    }
}
