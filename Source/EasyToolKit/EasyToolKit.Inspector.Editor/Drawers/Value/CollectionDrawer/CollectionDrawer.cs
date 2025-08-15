using System;
using EasyToolKit.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
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
        private bool _isListDrawerClassAttribute;

        private bool _isReadOnly;

        private int _count;
        private int _controlID;
        private int _dragDropControlID;

        [CanBeNull] private ICodeValueResolver<Texture> _iconTextureGetterResolver;

        [CanBeNull] private Action<object, object> _onAddedElementCallback;
        [CanBeNull] private Action<object, object> _onRemovedElementCallback;

        [CanBeNull] private Func<object, object> _customCreateElementFunction;
        [CanBeNull] private Action<object, object> _customRemoveElementFunction;
        [CanBeNull] private Action<object, int> _customRemoveIndexFunction;
        [CanBeNull] private Func<object, int, string> _customIndexLabelFunction;

        [CanBeNull] private ValueDropdownAttribute _valueDropdownAttribute;
        [CanBeNull] private ICodeValueResolver<object> _valueDropdownOptionsGetterResolver;

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

            _isReadOnly = _collectionResolver.IsReadOnly || _listDrawerSettings?.IsReadOnly == true;

            _isListDrawerClassAttribute = _listDrawerSettings != null && Property.GetAttributeSource(_listDrawerSettings) == AttributeSource.Type;
            var listDrawerTargetType = _isListDrawerClassAttribute
                ? Property.ValueEntry.ValueType
                : Property.Parent.ValueEntry.ValueType;

            if (_listDrawerSettings != null)
            {
                if (_listDrawerSettings is MetroListDrawerSettingsAttribute metroListDrawerSettings)
                {
                    if (metroListDrawerSettings.IconTextureGetter.IsNotNullOrEmpty())
                    {
                        _iconTextureGetterResolver = CodeValueResolver.Create<Texture>(metroListDrawerSettings.IconTextureGetter, listDrawerTargetType);
                    }
                }

                try
                {
                    if (_listDrawerSettings.ShowIndexLabel && _listDrawerSettings.CustomIndexLabelFunction.IsNotNullOrEmpty())
                    {
                        var customIndexLabelFunction = listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomIndexLabelFunction, BindingFlagsHelper.All, typeof(int))
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomIndexLabelFunction}' in '{listDrawerTargetType}'");
                        _customIndexLabelFunction = (instance, index) =>
                        {
                            return (string)customIndexLabelFunction.Invoke(instance, new object[] { index });
                        };
                    }

                    if (_listDrawerSettings.OnAddedElementCallback.IsNotNullOrEmpty())
                    {
                        var onAddedElementMethod = listDrawerTargetType.GetMethodEx(_listDrawerSettings.OnAddedElementCallback, BindingFlagsHelper.All, typeof(object))
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.OnAddedElementCallback}' in '{listDrawerTargetType}'");

                        _onAddedElementCallback = (instance, value) =>
                        {
                            onAddedElementMethod.Invoke(instance, new object[] { value });
                        };
                    }

                    if (_listDrawerSettings.OnRemovedElementCallback.IsNotNullOrEmpty())
                    {
                        var onRemovedElementMethod = listDrawerTargetType.GetMethodEx(_listDrawerSettings.OnRemovedElementCallback, BindingFlagsHelper.All, typeof(object))
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.OnRemovedElementCallback}' in '{listDrawerTargetType}'");

                        _onRemovedElementCallback = (instance, value) =>
                        {
                            onRemovedElementMethod.Invoke(instance, new object[] { value });
                        };
                    }

                    if (_listDrawerSettings.CustomCreateElementFunction.IsNotNullOrEmpty())
                    {
                        var customCreateElementFunction = listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomCreateElementFunction, BindingFlagsHelper.All)
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomCreateElementFunction}' in '{listDrawerTargetType}'");

                        _customCreateElementFunction = instance =>
                        {
                            return customCreateElementFunction.Invoke(instance, null);
                        };
                    }

                    if (_listDrawerSettings.CustomRemoveElementFunction.IsNotNullOrEmpty())
                    {
                        var customRemoveElementFunction = listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomRemoveElementFunction, BindingFlagsHelper.All)
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomRemoveElementFunction}' in '{listDrawerTargetType}'");

                        _customRemoveElementFunction = (instance, value) =>
                        {
                            customRemoveElementFunction.Invoke(instance, new object[] { value });
                        };
                    }

                    if (_listDrawerSettings.CustomRemoveIndexFunction.IsNotNullOrEmpty())
                    {
                        var customRemoveIndexFunction = listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomRemoveIndexFunction, BindingFlagsHelper.All, typeof(int))
                            ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomRemoveIndexFunction}' in '{listDrawerTargetType}'");
                        _customRemoveIndexFunction = (instance, index) =>
                        {
                            customRemoveIndexFunction.Invoke(instance, new object[] { index });
                        };
                    }
                }
                catch (Exception e)
                {
                    _error = e.Message;
                }
            }

            var isValueDropdownClassAttribute = _valueDropdownAttribute != null && Property.GetAttributeSource(_valueDropdownAttribute) == AttributeSource.Type;
            var valueDropdownTargetType = isValueDropdownClassAttribute
                ? Property.ValueEntry.ValueType
                : Property.Parent.ValueEntry.ValueType;

            if (_valueDropdownAttribute != null)
            {
                _valueDropdownOptionsGetterResolver = CodeValueResolver.Create<object>(_valueDropdownAttribute.OptionsGetter, valueDropdownTargetType);
            }
        }
    }
}
