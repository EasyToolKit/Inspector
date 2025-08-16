using System;
using EasyToolKit.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public static class CollectionDrawerStaticContext
    {
        public static InspectorProperty CurrentDraggingPropertyInfo;
        public static InspectorProperty CurrentDroppingPropertyInfo;
        public static DelayedGUIDrawer DelayedGUIDrawer = new DelayedGUIDrawer();
    }

    [DrawerPriority(DrawerPriorityLevel.Value + 9)]
    public partial class CollectionDrawer<T> : EasyValueDrawer<T>
    {
        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            return property.ChildrenResolver is ICollectionResolver;
        }

        private ICollectionResolver _collectionResolver;
        [CanBeNull] private IOrderedCollectionResolver _orderedCollectionResolver;
        [CanBeNull] private ListDrawerSettingsAttribute _listDrawerSettings;
        [CanBeNull] private Type _listDrawerTargetType;
        private bool _isListDrawerClassAttribute;

        private bool _isReadOnly;
        private int _count;

        [CanBeNull] private ValueDropdownAttribute _valueDropdownAttribute;
        private bool _isValueDropdownClassAttribute;
        [CanBeNull] private Type _valueDropdownTargetType;

        private string _error;

        protected override void Initialize()
        {
            _collectionResolver = (ICollectionResolver)Property.ChildrenResolver;
            _orderedCollectionResolver = Property.ChildrenResolver as IOrderedCollectionResolver;

            _listDrawerSettings = Property.GetAttribute<MetroListDrawerSettingsAttribute>();
            _valueDropdownAttribute = Property.GetAttribute<ValueDropdownAttribute>();
            if (_listDrawerSettings == null)
            {
                _listDrawerSettings = Property.GetAttribute<ListDrawerSettingsAttribute>();
            }

            if (_listDrawerSettings != null)
            {
                _isListDrawerClassAttribute = Property.GetAttributeSource(_listDrawerSettings) == AttributeSource.Type;
                _listDrawerTargetType = _isListDrawerClassAttribute
                    ? Property.ValueEntry.ValueType
                    : Property.Parent.ValueEntry.ValueType;
            }

            if (_valueDropdownAttribute != null)
            {
                _isValueDropdownClassAttribute = _valueDropdownAttribute != null && Property.GetAttributeSource(_valueDropdownAttribute) == AttributeSource.Type;
                _valueDropdownTargetType = _isValueDropdownClassAttribute
                    ? Property.ValueEntry.ValueType
                    : Property.Parent.ValueEntry.ValueType;
            }

            try
            {
                InitializeLogic();
                InitializeDraw();
                InitializeDragAndDrop();
            }
            catch (Exception e)
            {
                _error = e.Message;
            }
        }
    }
}
