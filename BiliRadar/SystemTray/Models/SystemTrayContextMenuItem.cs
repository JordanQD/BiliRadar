// -----------------------------------------------------------------------------
// SystemTray for WinUI 3
// A complete system tray (notification area) implementation for WinUI 3 apps.
//
// Repository: https://github.com/MEHDIMYADI
// Author: Mehdi Dimyadi
// License: MIT
// -----------------------------------------------------------------------------

using System;

namespace SystemTray.Models
{
    abstract class SystemTrayContextMenuItem
    {
        public abstract string Text { get; set; }

        public abstract bool IsEnabled { get; set; }

#pragma warning disable CS0649
        public EventHandler<EventArgs>? Click;
#pragma warning restore CS0649
    }
}