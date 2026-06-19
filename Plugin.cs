using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GorillaWindowController
{
    [BepInPlugin("com.ali.gorillawindowcontroller", "Gorilla Window Controller", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log;

        private static ConfigEntry<int> ScreenWidth;
        private static ConfigEntry<int> ScreenHeight;
        private static ConfigEntry<bool> Fullscreen;
        private static ConfigEntry<int> DisplayIndex;

        private static bool guiVisible;
        private static string widthText, heightText;
        private static int selectedDisplay;
        private static bool fsToggle;

        private static bool f8WasDown;

        // Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern System.IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(System.IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(System.IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern System.IntPtr MonitorFromWindow(System.IntPtr hWnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(System.IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        private const int GWL_STYLE = -16;
        private const int WS_BORDER = 0x00800000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_MAXIMIZE = 0x01000000;
        private const int WS_MINIMIZE = 0x20000000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_SIZEBOX = 0x00040000;

        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int MONITOR_DEFAULTTONEAREST = 2;

        private const int VK_F8 = 0x77;

        private void Awake()
        {
            Log = base.Logger;

            ScreenWidth   = Config.Bind("Window", "Width",  1920);
            ScreenHeight  = Config.Bind("Window", "Height", 1080);
            Fullscreen    = Config.Bind("Window", "Fullscreen", false);
            DisplayIndex  = Config.Bind("Window", "DisplayIndex", 0);

            widthText  = ScreenWidth.Value.ToString();
            heightText = ScreenHeight.Value.ToString();
            selectedDisplay = DisplayIndex.Value;
            fsToggle = Fullscreen.Value;

            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        private void Update()
        {
            bool f8Down = (GetAsyncKeyState(VK_F8) & 0x8000) != 0;
            if (f8Down && !f8WasDown)
                guiVisible = !guiVisible;
            f8WasDown = f8Down;
        }

        private void OnGUI()
        {
            if (!guiVisible) return;

            int boxW = 300, boxH = 330;
            Rect box = new Rect(Screen.width / 2 - boxW / 2, Screen.height / 2 - boxH / 2, boxW, boxH);
            GUI.Box(box, "Window Controller");

            GUILayout.BeginArea(new Rect(box.x + 10, box.y + 25, boxW - 20, boxH - 35));

            GUILayout.Label("Width");
            widthText = GUILayout.TextField(widthText, 5);

            GUILayout.Label("Height");
            heightText = GUILayout.TextField(heightText, 5);

            GUILayout.Label("Fullscreen");
            fsToggle = GUILayout.Toggle(fsToggle, fsToggle ? "On" : "Off");

            GUILayout.Label($"Display ({Display.displays.Length} available)");
            string[] displayLabels = new string[Display.displays.Length];
            for (int i = 0; i < Display.displays.Length; i++)
                displayLabels[i] = $"Display {i}";
            selectedDisplay = GUILayout.SelectionGrid(selectedDisplay, displayLabels, 3);

            GUILayout.Space(5);
            GUILayout.Label("Presets");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1920x1080"))
            {
                widthText = "1920"; heightText = "1080";
                ApplySettings();
            }
            if (GUILayout.Button("3440x1440"))
            {
                widthText = "3440"; heightText = "1440";
                ApplySettings();
            }
            if (GUILayout.Button("OBS"))
            {
                widthText = "1920"; heightText = "1111";
                ApplySettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("Apply"))
                ApplySettings();

            if (GUILayout.Button("Close (F8)"))
                guiVisible = false;

            GUILayout.EndArea();
        }

        private static void ApplySettings()
        {
            int.TryParse(widthText,  out int w);
            int.TryParse(heightText, out int h);
            if (w < 640 || h < 480) { w = 1920; h = 1080; }

            System.IntPtr hWnd = FindWindow(null, "Gorilla Tag");
            if (hWnd == System.IntPtr.Zero)
            {
                Log.LogWarning("Gorilla Tag window not found");
                return;
            }

            Log.LogInfo($"Found window, applying {w}x{h} fullscreen={fsToggle} display={selectedDisplay}");

            // Show and restore the window first
            ShowWindow(hWnd, SW_RESTORE);
            ShowWindow(hWnd, SW_SHOW);

            if (fsToggle)
            {
                // Remove border/titlebar for fullscreen
                int style = GetWindowLong(hWnd, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_SIZEBOX);
                style |= WS_POPUP;
                SetWindowLong(hWnd, GWL_STYLE, style);

                // Get the target monitor's full dimensions
                System.IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(hMonitor, ref mi);

                int screenW = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int screenH = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

                SetWindowPos(hWnd, System.IntPtr.Zero,
                    mi.rcMonitor.Left, mi.rcMonitor.Top,
                    screenW, screenH,
                    SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                ShowWindow(hWnd, SW_MAXIMIZE);
            }
            else
            {
                // Add border/titlebar back for windowed mode
                int style = GetWindowLong(hWnd, GWL_STYLE);
                style |= WS_CAPTION | WS_BORDER | WS_THICKFRAME | WS_SYSMENU | WS_SIZEBOX;
                style &= ~WS_POPUP;
                SetWindowLong(hWnd, GWL_STYLE, style);

                // Position on the selected display
                int x = 0, y = 0;
                if (selectedDisplay >= 0 && selectedDisplay < Display.displays.Length)
                {
                    for (int i = 0; i < selectedDisplay; i++)
                        x += Display.displays[i].systemWidth;
                }

                SetWindowPos(hWnd, System.IntPtr.Zero,
                    x, y, w, h,
                    SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            }

            Screen.SetResolution(w, h, fsToggle ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

            ScreenWidth.Value   = w;
            ScreenHeight.Value  = h;
            Fullscreen.Value    = fsToggle;
            DisplayIndex.Value  = selectedDisplay;

            Log.LogInfo($"Applied: {w}x{h} fullscreen={fsToggle} display={selectedDisplay}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GorillaTagger), "Start")]
        private static void OnGameStart()
        {
            ApplySettings();
        }
    }
}
