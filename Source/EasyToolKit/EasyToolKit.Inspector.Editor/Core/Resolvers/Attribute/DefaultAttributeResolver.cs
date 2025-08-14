using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyToolKit.Inspector.Editor
{
    public class DefaultAttributeResolver : AttributeResolver
    {
        private Attribute[] _attributes;
        private Dictionary<Attribute, AttributeSource> _attributeSources;

        public override Attribute[] GetAttributes()
        {
            if (_attributes != null)
            {
                return _attributes;
            }

            _attributeSources = new Dictionary<Attribute, AttributeSource>();

            var memberInfo = Property.Info.TryGetMemberInfo();
            if (memberInfo != null)
            {
                var memberAttributes = memberInfo.GetCustomAttributes();
                foreach (var attribute in memberAttributes)
                {
                    _attributeSources[attribute] = AttributeSource.Member;
                }
            }

            if (Property.ValueEntry != null)
            {
                var typeAttributes = Property.ValueEntry.ValueType.GetCustomAttributes(true).Cast<Attribute>();
                foreach (var attribute in typeAttributes)
                {
                    _attributeSources[attribute] = AttributeSource.Type;
                }
            }

            _attributes = _attributeSources.Keys.ToArray();
            return _attributes;
        }

        public override AttributeSource GetAttributeSource(Attribute attribute)
        {
            if (_attributeSources == null)
            {
                GetAttributes();
            }

            if (_attributeSources.TryGetValue(attribute, out var source))
            {
                return source;
            }

            throw new ArgumentException($"Attribute '{attribute.GetType()}' not found in the property '{Property}'");
        }
    }
}
