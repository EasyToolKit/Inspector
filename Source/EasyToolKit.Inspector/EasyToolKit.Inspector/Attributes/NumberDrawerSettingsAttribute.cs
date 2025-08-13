using System;

namespace EasyToolKit.Inspector
{
    public enum NumberDrawerStyle
    {
        Default,
        SpinBox
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class NumberDrawerSettingsAttribute : Attribute
    {
        public NumberDrawerStyle Style { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Step { get; set; }

        public NumberDrawerSettingsAttribute(NumberDrawerStyle style = NumberDrawerStyle.Default)
        {
            Style = style;
        }
    }
}