using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MinchoCandyWars
{
    public static class SettingUtility
    {
        // 是否为调试模式
        public static bool IsDebugMode()
        {
            return DebugSettings.godMode && MinchoCandyWarsMod.Instance.setting.isDevMode;
        }
    }
}
