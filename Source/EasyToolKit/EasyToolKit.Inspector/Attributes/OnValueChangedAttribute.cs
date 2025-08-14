using System;

namespace EasyToolKit.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class OnValueChangedAttribute : Attribute
    {
        public string Method { get; set; }

        public OnValueChangedAttribute(string method)
        {
            Method = method;
        }
    }
}