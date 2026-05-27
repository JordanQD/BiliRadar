using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using SystemTray.Interfaces;
using SystemTray.UI;
using Windows.ApplicationModel;
using Windows.Foundation;

// -----------------------------------------------------------------------------
// SystemTray for WinUI 3
// A complete system tray (notification area) implementation for WinUI 3 apps.
//
// Original: https://github.com/MEHDIMYADI/SystemTrayWinUI3
// Modified for BiliRadar: configurable menu items, icon path, and actions.
// -----------------------------------------------------------------------------

namespace SystemTray.Core
{
    public class SystemTrayManager : IDisposable
    {
        private readonly WindowHelper windowHelper;
        private SystemTrayIcon SystemTrayIcon;
        private SystemTrayContextMenuWindow? contextMenuWindow;
        private SystemTrayContextMenuWindow.Item[] menuItems = [];

        public record MenuItemConfig(string Text, ICommand? Command, bool IsSeparator = false);

        public bool IsIconVisible
        {
            get => SystemTrayIcon != null && SystemTrayIcon.IsVisible;
            set
            {
                if (value)
                    SystemTrayIcon?.Show();
                else
                    SystemTrayIcon?.Hide();
                windowHelper.IsIconVisible = value;
            }
        }

        private string iconToolTip = "";
        public string IconToolTip
        {
            get => iconToolTip;
            set
            {
                iconToolTip = value;
                if (SystemTrayIcon != null)
                    SystemTrayIcon.Text = iconToolTip;
            }
        }

        public bool IsWindowVisible
        {
            get
            {
                var appWindow = windowHelper.AppWindow;
                return appWindow != null && appWindow.IsVisible;
            }
        }

        private bool minimizeToTray = true;
        public bool MinimizeToTray
        {
            get => minimizeToTray;
            set => minimizeToTray = value;
        }

        private bool closeButtonMinimizesToTray = true;
        public bool CloseButtonMinimizesToTray
        {
            get => closeButtonMinimizesToTray;
            set
            {
                closeButtonMinimizesToTray = value;
                windowHelper.CloseButtonMinimizesToTray = value;
            }
        }

        public SystemTrayManager(
            WindowHelper windowHelper,
            string iconPath,
            string tooltip,
            IReadOnlyList<MenuItemConfig> menuItems,
            Action? leftClickAction = null)
        {
            this.windowHelper = windowHelper ?? throw new ArgumentNullException(nameof(windowHelper));
            iconToolTip = tooltip;

            this.windowHelper.CloseButtonPressed += OnWindowCloseButtonPressed;

            SystemTrayIcon = new SystemTrayIcon(windowHelper)
            {
                Id = Guid.NewGuid(),
                Icon = new IcoIcon(iconPath),
                Text = tooltip
            };

            BuildMenuItems(menuItems);
            contextMenuWindow = new SystemTrayContextMenuWindow(this.menuItems);

            SystemTrayIcon.RightClick += (_, e) =>
            {
                if (double.IsInfinity(e.Rect.X) || double.IsInfinity(e.Rect.Y))
                {
                    var mousePos = GetMousePosition();
                    contextMenuWindow?.Show((int)mousePos.X, (int)mousePos.Y);
                }
                else
                {
                    contextMenuWindow?.Show((int)e.Rect.X, (int)e.Rect.Y);
                }
            };

            SystemTrayIcon.LeftClick += (_, _) =>
            {
                if (leftClickAction != null)
                    leftClickAction();
                else
                    ToggleWindowVisibility();
            };

            SystemTrayIcon.Show();

            if (windowHelper.AppWindow != null)
            {
                windowHelper.AppWindow.Changed += AppWindow_Changed;
            }
        }

        public void RefreshIcon(string iconPath)
        {
            if (SystemTrayIcon != null)
                SystemTrayIcon.Icon = new IcoIcon(iconPath);
        }

        public void ToggleWindowVisibility()
        {
            if (windowHelper.AppWindow.IsVisible)
                windowHelper.HideWindowToTray();
            else
                windowHelper.ShowWindowFromTray();
        }

        private void BuildMenuItems(IReadOnlyList<MenuItemConfig> configs)
        {
            var items = new List<SystemTrayContextMenuWindow.Item>();
            foreach (var cfg in configs)
            {
                if (cfg.IsSeparator)
                    items.Add(new SystemTrayContextMenuWindow.Item("--", null));
                else
                    items.Add(new SystemTrayContextMenuWindow.Item(cfg.Text, cfg.Command));
            }
            menuItems = [.. items];
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (!MinimizeToTray || !IsIconVisible)
                return;

            if (args.DidSizeChange || args.DidVisibilityChange)
            {
                if (sender.Presenter is OverlappedPresenter presenter)
                {
                    if (presenter.State == OverlappedPresenterState.Minimized)
                    {
                        var appWindow = windowHelper.AppWindow;
                        appWindow.Hide();
                    }
                }
            }
        }

        private void OnWindowCloseButtonPressed()
        {
            if (!CloseButtonMinimizesToTray)
            {
                SystemTrayIcon?.Dispose();
                SystemTrayIcon = null!;
                Application.Current.Exit();
            }
        }

        public void Dispose()
        {
            SystemTrayIcon?.Dispose();
            SystemTrayIcon = null!;
        }

        private static Point GetMousePosition()
        {
            if (GetCursorPos(out POINT point))
                return new Point(point.X, point.Y);
            return new Point(100, 100);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        #region LibIcon & IcoIcon

        public sealed class LibIcon : IIconFile, IDisposable
        {
            private SafeIconHandle iconHandle;

            public LibIcon(string fileName, uint iconIndex)
            {
                iconHandle = new SafeIconHandle(ExtractIcon(nint.Zero, fileName, iconIndex), true);
                if (iconHandle.IsInvalid) throw new InvalidOperationException("Cannot extract icon.");
            }

            public nint Handle => iconHandle.DangerousGetHandle();

            public void Dispose() => iconHandle?.Dispose();

            private sealed class SafeIconHandle : SafeHandle
            {
                public SafeIconHandle(nint handle, bool ownsHandle) : base(nint.Zero, ownsHandle) => SetHandle(handle);
                public override bool IsInvalid => handle == nint.Zero;
                protected override bool ReleaseHandle() => DestroyIcon(handle);
            }

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            private static extern nint ExtractIcon(nint hInst, string lpszExeFileName, uint nIconIndex);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool DestroyIcon(nint hIcon);
        }

        public sealed class IcoIcon : IIconFile, IDisposable
        {
            private SafeIconHandle iconHandle;

            public IcoIcon(string path)
            {
                string fullPath = Path.Combine(Package.Current.InstalledLocation.Path, path);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException("Icon file not found.", fullPath);

                var hIcon = LoadImageIcon(fullPath);
                iconHandle = new SafeIconHandle(hIcon, true);
                if (iconHandle.IsInvalid)
                    throw new InvalidOperationException("Cannot load .ico file.");
            }

            public nint Handle => iconHandle.DangerousGetHandle();

            public void Dispose() => iconHandle?.Dispose();

            private static nint LoadImageIcon(string path)
            {
                return LoadImage(IntPtr.Zero, path, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
            }

            private sealed class SafeIconHandle : SafeHandle
            {
                public SafeIconHandle(nint handle, bool ownsHandle) : base(nint.Zero, ownsHandle) => SetHandle(handle);
                public override bool IsInvalid => handle == nint.Zero;
                protected override bool ReleaseHandle() => DestroyIcon(handle);
            }

            private const uint IMAGE_ICON = 1;
            private const uint LR_LOADFROMFILE = 0x00000010;
            private const uint LR_DEFAULTSIZE = 0x00000040;

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool DestroyIcon(nint hIcon);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern nint LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);
        }

        #endregion
    }
}
