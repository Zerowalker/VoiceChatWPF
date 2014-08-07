using System;
using System.Runtime.InteropServices;

namespace VoiceChatWPF.Models
{
    internal static class NativeMethods
    {
        [DllImport("winmm.dll", EntryPoint = "waveOutSetVolume")]
        public static extern int WaveOutSetVolume(IntPtr hwo, uint dwVolume);


        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);
    }

    public static class SystemVolumeChanger
    {                     
        const int Volumecalc = (ushort.MaxValue / 100);
        public static int GetVolume()
        {
            uint volume;
            NativeMethods.waveOutGetVolume(IntPtr.Zero, out volume);
            ushort calcVol = (ushort)((volume & 0x0000ffff) / Volumecalc);
            return calcVol;
        }

        public static void SetVolume(int value)
        {
            double newVolume = (Volumecalc * value);
            uint newVolumeAllChannels = (((uint) newVolume & 0x0000ffff) | ((uint) newVolume << 16));
            NativeMethods.WaveOutSetVolume(IntPtr.Zero, newVolumeAllChannels);
        }
    }
}