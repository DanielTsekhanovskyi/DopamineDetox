using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Tracker.DesktopAgent;

public static class Win32Api
{
    // Отримує вказівник (handle) на активне вікно
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    // Отримує текст заголовка вікна за його вказівником
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    // Отримує ідентифікатор процесу (PID), якому належить вікно
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    public struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
    
    [DllImport("user32.dll")]
    public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    
    public static int GetIdleTimeSeconds()
    {
        var lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        GetLastInputInfo(ref lastInputInfo);
    
        // Environment.TickCount показує час роботи системи з моменту запуску
        int idleTimeMs = Environment.TickCount - (int)lastInputInfo.dwTime;
        return idleTimeMs / 1000;
    }
}