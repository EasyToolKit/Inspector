using EasyToolKit.ThirdParty.OdinSerializer;
using System;

namespace EasyToolKit.Inspector.Editor
{
    public static class EasyDrawerExtensions
    {
        public static LocalPersistentContext<T> GetPersistentContext<T>(this IEasyDrawer drawer, string key,
            T defaultValue = default)
        {
            var key1 = TwoWaySerializationBinder.Default.BindToName(drawer.GetType());
            var key2 = TwoWaySerializationBinder.Default.BindToName(drawer.Property.Tree.TargetType);
            var key3 = drawer.Property.Path;

            return PersistentContext.GetLocal(string.Join("+", key1, key2, key3, key), defaultValue);
        }

        public static Type GetTargetTypeForResolver<TAttribute>(this EasyAttributeDrawer<TAttribute> drawer)
            where TAttribute : Attribute
        {
            return drawer.AttributeSource == AttributeSource.Type
                ? drawer.Property.ValueEntry.ValueType
                : drawer.Property.Parent.ValueEntry.ValueType;
        }

        public static object GetTargetForResolver<TAttribute>(this EasyAttributeDrawer<TAttribute> drawer)
            where TAttribute : Attribute
        {
            return drawer.AttributeSource == AttributeSource.Type
                ? drawer.Property.ValueEntry.WeakSmartValue
                : drawer.Property.Parent.ValueEntry.WeakSmartValue;
        }

        public static TValue GetTargetForResolver<TAttribute, TValue>(this EasyAttributeDrawer<TAttribute, TValue> drawer)
            where TAttribute : Attribute
        {
            return drawer.AttributeSource == AttributeSource.Type
                ? drawer.ValueEntry.SmartValue
                : (drawer.Property.Parent.ValueEntry as IPropertyValueEntry<TValue>).SmartValue;
        }
    }
}
