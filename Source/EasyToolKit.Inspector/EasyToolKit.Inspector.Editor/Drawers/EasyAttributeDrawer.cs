using System;

namespace EasyToolKit.Inspector.Editor
{
    public abstract class EasyAttributeDrawer<TAttribute> : EasyDrawer
        where TAttribute : Attribute
    {
        private TAttribute _attribute;
        private bool? _isClassAttribute;

        public TAttribute Attribute
        {
            get
            {
                if (_attribute == null)
                {
                    _attribute = Property.GetAttribute<TAttribute>();
                }

                return _attribute;
            }
        }

        /// <summary>
        /// Determines if the current attribute is a class-level attribute
        /// </summary>
        public bool IsClassAttribute
        {
            get
            {
                if (!_isClassAttribute.HasValue)
                {
                    _isClassAttribute = Property.IsClassAttribute(Attribute);
                }
                return _isClassAttribute ?? false;
            }
        }

        protected sealed override bool CanDrawProperty(InspectorProperty property)
        {
            if (property.ValueEntry != null && !CanDrawValueType(property.ValueEntry.ValueType))
            {
                return false;
            }

            return property.GetAttribute<TAttribute>() != null && CanDrawAttributeProperty(property);
        }

        protected virtual bool CanDrawValueType(Type valueType)
        {
            return true;
        }

        protected virtual bool CanDrawAttributeProperty(InspectorProperty property)
        {
            return true;
        }
    }

    public abstract class EasyAttributeDrawer<TAttribute, TValue> : EasyAttributeDrawer<TAttribute>
        where TAttribute : Attribute
    {
        private IPropertyValueEntry<TValue> _valueEntry;

        public IPropertyValueEntry<TValue> ValueEntry
        {
            get
            {
                if (_valueEntry == null)
                {
                    _valueEntry = Property.ValueEntry as IPropertyValueEntry<TValue>;
                }

                return _valueEntry;
            }
        }


        protected override bool CanDrawAttributeProperty(InspectorProperty property)
        {
            return property.ValueEntry != null &&
                   property.ValueEntry.ValueType == typeof(TValue) &&
                   CanDrawAttributeValueProperty(property);
        }

        protected virtual bool CanDrawAttributeValueProperty(InspectorProperty property)
        {
            return true;
        }
    }
}
