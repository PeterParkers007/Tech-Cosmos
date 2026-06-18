#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.Hub.Editor
{
    internal static class HubStudioLauncher
    {
        [MenuItem("Tech-Cosmos/Hub Studio", false, 1)]
        private static void OpenHubStudio()
        {
            var studioDir = Path.Combine(HubPaths.HubRoot, "Tools", "HubStudio");
            var startPs1 = Path.Combine(studioDir, "start.ps1");
            var serverPy = Path.Combine(studioDir, "server.py");

            if (!File.Exists(serverPy))
            {
                EditorUtility.DisplayDialog(
                    "Hub Studio",
                    $"未找到 Hub Studio 服务脚本：\n{serverPy}\n\n请确认已安装完整 com.techcosmos.hub 包。",
                    "确定");
                return;
            }

            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor && File.Exists(startPs1))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{startPs1}\"",
                        WorkingDirectory = studioDir,
                        UseShellExecute = true,
                    });
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{serverPy}\"",
                    WorkingDirectory = studioDir,
                    UseShellExecute = true,
                });
                Application.OpenURL("http://127.0.0.1:8765");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Hub Studio",
                    $"启动失败：{ex.Message}\n\n请手动运行：\ncd Tools/HubStudio && python server.py",
                    "确定");
            }
        }
    }
}
#endif
