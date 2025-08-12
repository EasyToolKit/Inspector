using System;
using System.Diagnostics;
using UnityEngine;

namespace EasyToolKit.Inspector
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class MetroListDrawerSettingsAttribute : ListDrawerSettingsAttribute
    {
        public Color SideLineColor { get; set; } = Color.green;
        public string IconTextureGetter { get; set; }

        public MetroListDrawerSettingsAttribute()
        {
        }
    }
}
