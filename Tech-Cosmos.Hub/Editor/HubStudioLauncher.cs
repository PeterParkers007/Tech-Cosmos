#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TechCosmos.Hub.Editor
{
    internal static class HubStudioLauncher
    {
        private const int DefaultPort = 8765;
        private const string Url = "http://127.0.0.1:8765";

        [MenuItem("Tech-Cosmos/Hub Studio (Web)", false, 101)]
        public static void OpenHubStudio()
        {
            var studioDir = Path.Combine(HubPaths.HubRoot, "Tools", "HubStudio");
            var serverScript = Path.Combine(studioDir, "server.py");

            if (!File.Exists(serverScript))
            {
                EditorUtility.DisplayDialog(
                    "Hub Studio",
                    $"未找到 Hub Studio：\n{serverScript}\n\n请确认 Tech-Cosmos.Hub 包完整。",
                    "确定");
                return;
            }

            if (!IsServerRunning())
                StartServer(studioDir, serverScript);

            EditorApplication.delayCall += () => Application.OpenURL(Url);
        }

        private static bool IsServerRunning()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var response = client.GetAsync($"{Url}/api/meta").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static void StartServer(string studioDir, string serverScript)
        {
            var python = ResolvePythonExecutable();
            if (python == null)
            {
                var ps1 = Path.Combine(studioDir, "start.ps1");
                if (File.Exists(ps1))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1}\"",
                        WorkingDirectory = studioDir,
                        UseShellExecute = true,
                    });
                    return;
                }

                EditorUtility.DisplayDialog(
                    "Hub Studio",
                    "未找到 Python 3。\n\n请安装 Python 后重试，或在 Hub 包目录运行：\nTools/HubStudio/start.ps1",
                    "确定");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = python.FileName,
                Arguments = python.Arguments + $" \"{serverScript}\"",
                WorkingDirectory = studioDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!Process.Start(startInfo))
                Debug.LogWarning("[Tech-Cosmos Hub] 无法启动 Hub Studio 服务进程。");
            else
                Debug.Log("[Tech-Cosmos Hub] Hub Studio 服务已启动: " + Url);
        }

        private sealed class PythonCommand
        {
            public string FileName;
            public string Arguments = string.Empty;
        }

        private static PythonCommand ResolvePythonExecutable()
        {
            if (TryPython("py", "-3")) return new PythonCommand { FileName = "py", Arguments = "-3" };
            if (TryPython("python", string.Empty)) return new PythonCommand { FileName = "python" };
            if (TryPython("python3", string.Empty)) return new PythonCommand { FileName = "python3" };
            return null;
        }

        private static bool TryPython(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments + " -c \"import sys; print(sys.version)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit(3000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
