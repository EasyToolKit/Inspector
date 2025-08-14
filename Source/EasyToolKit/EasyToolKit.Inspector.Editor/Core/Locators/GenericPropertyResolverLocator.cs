using System;
using System.Collections.Generic;
using EasyToolKit.Core;

namespace EasyToolKit.Inspector.Editor
{
    public class GenericPropertyResolverLocator : PropertyResolverLocator
    {
        public override IPropertyResolver GetResolver(InspectorProperty property)
        {
            var type = property.ValueEntry.ValueType;

            if (type.IsImplementsOpenGenericType(typeof(IEnumerable<>)))
            {
                var elementType = type.GetArgumentsOfInheritedOpenGenericType(typeof(IEnumerable<>))[0];

                if (type.IsImplementsOpenGenericType(typeof(IList<>)))
                {
                    return typeof(IListResolver<,>).MakeGenericType(type, elementType).CreateInstance<IPropertyResolver>();
                }
                else if (type.IsImplementsOpenGenericType(typeof(IReadOnlyList<>)))
                {
                    return typeof(IReadOnlyListResolver<,>).MakeGenericType(type, elementType).CreateInstance<IPropertyResolver>();
                }
                else
                {
                    throw new NotImplementedException($"Not implemented {type}.");
                }
            }
            else
            {
                return new GenericPropertyResolver();
            }
        }
    }
}