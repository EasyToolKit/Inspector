using System;
using System.Collections.Generic;
using EasyToolKit.Core;
using EasyToolKit.Core.Editor;
using EasyToolKit.ThirdParty.OdinSerializer;
using JetBrains.Annotations;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public partial class CollectionDrawer<T>
    {
        [CanBeNull] private Action<object, object> _onAddedElementCallback;
        [CanBeNull] private Action<object, object> _onRemovedElementCallback;

        [CanBeNull] private Func<object, object> _customCreateElementFunction;
        [CanBeNull] private Action<object, object> _customRemoveElementFunction;
        [CanBeNull] private Action<object, int> _customRemoveIndexFunction;

        [CanBeNull] private ICodeValueResolver<object> _valueDropdownOptionsGetterResolver;

        private void InitializeLogic()
        {
            if (_listDrawerSettings != null)
            {
                if (_listDrawerSettings.OnAddedElementCallback.IsNotNullOrEmpty())
                {
                    var onAddedElementMethod = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.OnAddedElementCallback, BindingFlagsHelper.All, typeof(object))
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.OnAddedElementCallback}' in '{_listDrawerTargetType}'");

                    _onAddedElementCallback = (instance, value) =>
                    {
                        onAddedElementMethod.Invoke(instance, new object[] { value });
                    };
                }

                if (_listDrawerSettings.OnRemovedElementCallback.IsNotNullOrEmpty())
                {
                    var onRemovedElementMethod = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.OnRemovedElementCallback, BindingFlagsHelper.All, typeof(object))
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.OnRemovedElementCallback}' in '{_listDrawerTargetType}'");

                    _onRemovedElementCallback = (instance, value) =>
                    {
                        onRemovedElementMethod.Invoke(instance, new object[] { value });
                    };
                }

                if (_listDrawerSettings.CustomCreateElementFunction.IsNotNullOrEmpty())
                {
                    var customCreateElementFunction = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomCreateElementFunction, BindingFlagsHelper.All)
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomCreateElementFunction}' in '{_listDrawerTargetType}'");

                    _customCreateElementFunction = instance =>
                    {
                        return customCreateElementFunction.Invoke(instance, null);
                    };
                }

                if (_listDrawerSettings.CustomRemoveElementFunction.IsNotNullOrEmpty())
                {
                    var customRemoveElementFunction = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomRemoveElementFunction, BindingFlagsHelper.All)
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomRemoveElementFunction}' in '{_listDrawerTargetType}'");

                    _customRemoveElementFunction = (instance, value) =>
                    {
                        customRemoveElementFunction.Invoke(instance, new object[] { value });
                    };
                }
            }

            if (_valueDropdownAttribute != null)
            {
                _valueDropdownOptionsGetterResolver = CodeValueResolver.Create<object>(_valueDropdownAttribute.OptionsGetter, _valueDropdownTargetType);
            }
        }

        protected virtual void DoAddElement(Rect addButtonRect)
        {
            if (_valueDropdownOptionsGetterResolver != null)
            {
                var target = _isValueDropdownClassAttribute
                    ? Property.ValueEntry.WeakSmartValue
                    : Property.Parent.ValueEntry.WeakSmartValue;

                var options = _valueDropdownOptionsGetterResolver.Resolve(target);
                var dropdownItems = new ValueDropdownList();
                if (options is IEnumerable<IValueDropdownItem> valueDropdownItems)
                {
                    dropdownItems.AddRange(valueDropdownItems);
                }
                else if (options is IEnumerable<object> objectOptions)
                {
                    foreach (var item in objectOptions)
                    {
                        dropdownItems.Add(item);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"The return type of '{_valueDropdownAttribute.OptionsGetter}' must be IEnumerable<IValueDropdownItem> or IEnumerable<object>");
                }

                EasyEditorGUI.ShowValueDropdownMenu(addButtonRect, null, dropdownItems.ToArray(), (item) =>
                {
                    var value = item.GetValue();

                    DoAddElement(value);
                }, (item) => new GUIContent(item.GetText()));
                return;
            }

            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoAddElement(GetValueToAdd(i));
            }
        }

        private void DoAddElement(object valueToAdd)
        {
            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoAddElement(i, valueToAdd);
            }
        }

        protected virtual void DoAddElement(int targetIndex, object valueToAdd)
        {
            _collectionResolver.QueueAddElement(targetIndex, valueToAdd);
            _onAddedElementCallback?.Invoke(Property.Parent.ValueEntry.WeakValues[targetIndex], valueToAdd);
        }

        private void DoInsertElement(int index, object valueToAdd)
        {
            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoInsertElement(i, index, valueToAdd);
            }
        }

        private void DoInsertElement(int targetIndex, int index, object valueToAdd)
        {
            if (_orderedCollectionResolver == null)
            {
                throw new InvalidOperationException($"The property '{Property.Path}' is not ordered collection, so you cannot insert elements at a specific index.");
            }
            _orderedCollectionResolver.QueueInsertElementAt(targetIndex, index, valueToAdd);
            _onAddedElementCallback?.Invoke(Property.Parent.ValueEntry.WeakValues[targetIndex], valueToAdd);
        }

        protected virtual void DoRemoveElementAt(int index)
        {
            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoRemoveElementAt(i, index);
            }
        }

        protected virtual void DoRemoveElementAt(int targetIndex, int index)
        {
            var parent = Property.Parent.ValueEntry.WeakValues[targetIndex];
            if (_customRemoveIndexFunction != null)
            {
                _customRemoveIndexFunction.Invoke(parent, index);
            }
            else
            {
                Assert.IsNotNull(_orderedCollectionResolver);
                _orderedCollectionResolver.QueueRemoveElementAt(targetIndex, index);
            }

            var valueToRemove = Property.Children[index].ValueEntry.WeakValues[targetIndex];
            _onRemovedElementCallback?.Invoke(parent, valueToRemove);
        }

        protected virtual void DoRemoveElement(InspectorProperty propertyToRemove)
        {
            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoRemoveElement(i, propertyToRemove);
            }
        }

        protected virtual void DoRemoveElement(int targetIndex, InspectorProperty propertyToRemove)
        {
            var parent = Property.Parent.ValueEntry.WeakValues[targetIndex];
            var valueToRemove = propertyToRemove.ValueEntry.WeakValues[targetIndex];
            if (_customRemoveElementFunction != null)
            {
                _customRemoveElementFunction.Invoke(parent, valueToRemove);
            }
            else
            {
                _collectionResolver.QueueRemoveElement(targetIndex, valueToRemove);
            }

            _onRemovedElementCallback?.Invoke(parent, valueToRemove);
        }

        protected virtual object GetValueToAdd(int targetIndex)
        {
            var parent = Property.Parent.ValueEntry.WeakValues[targetIndex];
            if (_customCreateElementFunction != null)
            {
                return _customCreateElementFunction.Invoke(parent);
            }

            if (_collectionResolver.ElementType.IsInheritsFrom<UnityEngine.Object>())
            {
                return null;
            }

            return UnitySerializationUtility.CreateDefaultUnityInitializedObject(_collectionResolver.ElementType);
        }
    }
}