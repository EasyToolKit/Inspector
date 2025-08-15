using System;
using System.Collections.Generic;
using EasyToolKit.Core;
using EasyToolKit.Core.Editor;
using EasyToolKit.Inspector.Editor.Internal;
using EasyToolKit.ThirdParty.OdinSerializer;
using UnityEditor;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public static class CollectionDrawerStyles
    {
        private static GUIStyle s_metroHeaderLabelStyle;
        private static GUIStyle s_listItemStyle;
        public static GUIStyle MetroHeaderLabelStyle
        {
            get
            {
                if (s_metroHeaderLabelStyle == null)
                {
                    s_metroHeaderLabelStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = EasyGUIStyles.Foldout.fontSize + 1,
                        alignment = TextAnchor.MiddleLeft,
                    };
                }
                return s_metroHeaderLabelStyle;
            }
        }
        public static readonly Color ItemSelectedBackgroundColor = Color.white * 1.2f;
        public static readonly Color MetroItemSelectedBackgroundColor = Color.white * 1.1f;

        public static readonly Color MetroHeaderBackgroundColor = new Color(0.8f, 0.8f, 0.8f);
        public static readonly Color MetroItemBackgroundColor = new Color(0.9f, 0.9f, 0.9f);

        public static GUIStyle ListItemStyle = new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(25, 20, 3, 3)
        };

        public static GUIStyle MetroListItemStyle = new GUIStyle(GUIStyle.none)
        {
            padding = new RectOffset(30, 37, 3, 3)
        };
    }

    public partial class CollectionDrawer<T>
    {
        protected override void DrawProperty(GUIContent label)
        {
            if (_error.IsNotNullOrEmpty())
            {
                EasyEditorGUI.MessageBox(_error, MessageType.Error);
                return;
            }

            if (_iconTextureGetterResolver != null && _iconTextureGetterResolver.HasError(out var error))
            {
                EasyEditorGUI.MessageBox(error, MessageType.Error);
                return;
            }

            if (_valueDropdownOptionsGetterResolver != null && _valueDropdownOptionsGetterResolver.HasError(out error))
            {
                EasyEditorGUI.MessageBox(error, MessageType.Error);
                return;
            }

            if (Event.current.type == EventType.Layout)
            {
                _count = Property.Children.Count;
            }
            else
            {
                var newCount = Property.Children.Count;
                if (_count > newCount)
                {
                    _count = newCount;
                }
            }

            var rect = EasyEditorGUI.BeginIndentedVertical(EasyGUIStyles.PropertyPadding);
            _controlID = EditorGUIUtility.GetControlID(nameof(_controlID).GetHashCode(), FocusType.Keyboard, rect);
            _dragDropControlID = EditorGUIUtility.GetControlID(nameof(_dragDropControlID).GetHashCode(), FocusType.Passive, rect);

            if (_listDrawerSettings is MetroListDrawerSettingsAttribute)
            {
                DrawMetroHeader(label);
                DrawMetroItems();
            }
            else
            {
                DrawHeader(label);
                DrawItems();
            }
            EasyEditorGUI.EndIndentedVertical();
        }

        private void DrawHeader(GUIContent label)
        {
            EasyEditorGUI.BeginHorizontalToolbar();

            if (label != null)
            {
                GUILayout.Label(label);
            }

            GUILayout.FlexibleSpace();

            if (_listDrawerSettings?.HideAddButton != false && !_isReadOnly)
            {
                var buttonRect = GUILayoutUtility.GetRect(22, 22, GUILayout.ExpandWidth(false));
                if (EasyEditorGUI.ToolbarButton(buttonRect, EasyEditorIcons.Plus))
                {
                    DoAddElement(buttonRect);
                }
            }

            EasyEditorGUI.EndHorizontalToolbar();
        }

        private void DrawMetroHeader(GUIContent label)
        {
            EasyGUIHelper.PushColor(CollectionDrawerStyles.MetroHeaderBackgroundColor);
            EasyEditorGUI.BeginHorizontalToolbar(30);
            EasyGUIHelper.PopColor();

            if (_iconTextureGetterResolver != null)
            {
                var iconTexture = _iconTextureGetterResolver.Resolve(Property.Parent.ValueEntry.WeakSmartValue);
                GUILayout.Label(iconTexture, GUILayout.Width(30), GUILayout.Height(30));
            }

            if (label != null)
            {
                GUILayout.Label(label, CollectionDrawerStyles.MetroHeaderLabelStyle, GUILayout.Height(30));
            }

            GUILayout.FlexibleSpace();

            if (_listDrawerSettings?.HideAddButton != false && !_isReadOnly)
            {
                var btnRect = GUILayoutUtility.GetRect(
                    EasyEditorIcons.Plus.HighlightedContent,
                    "Button",
                    GUILayout.ExpandWidth(false),
                    GUILayout.Width(30),
                    GUILayout.Height(30));

                if (GUI.Button(btnRect, GUIContent.none, "Button"))
                {
                    EasyGUIHelper.RemoveFocusControl();
                    DoAddElement(btnRect);
                }

                if (Event.current.type == EventType.Repaint)
                {
                    EasyEditorIcons.Plus.Draw(btnRect.AlignCenter(25, 25));
                }
            }

            EasyEditorGUI.EndHorizontalToolbar();
        }

        private void DrawItems()
        {
            EasyEditorGUI.BeginVerticalList();

            for (int i = 0; i < _count; i++)
            {
                var child = Property.Children[i];
                DrawItem(child, i);
            }

            EasyEditorGUI.EndVerticalList();
        }

        private void DrawMetroItems()
        {
            EasyEditorGUI.BeginVerticalList();

            for (int i = 0; i < _count; i++)
            {
                var child = Property.Children[i];
                DrawMetroItem(child, i);
            }

            EasyEditorGUI.EndVerticalList();
        }

        private void DrawItem(InspectorProperty property, int index)
        {
            var selected = _selectionList.Contains(index);

            if (selected)
            {
                EasyGUIHelper.PushColor(CollectionDrawerStyles.ItemSelectedBackgroundColor);
            }
            var rect = EasyEditorGUI.BeginListItem(false, CollectionDrawerStyles.ListItemStyle, GUILayout.MinHeight(25), GUILayout.ExpandWidth(true));
            if (selected)
            {
                EasyGUIHelper.PopColor();
            }


            var dragHandleRect = new Rect(rect.x + 4, rect.y + 2 + ((int)rect.height - 23) / 2, 20, 20);

            GUI.Label(dragHandleRect, EasyEditorIcons.List.InactiveTexture, GUIStyle.none);

            HandlePreSelection(rect, index);
            DrawElementProperty(property, index);
            HandlePostSelection(rect, index);

            if (_listDrawerSettings?.HideRemoveButton == false && !_isReadOnly)
            {
                var removeBtnRect = new Rect(dragHandleRect.x + rect.width - 22, dragHandleRect.y + 1, 14, 14);
                if (EasyEditorGUI.IconButton(removeBtnRect, EasyEditorIcons.X))
                {
                    if (_orderedCollectionResolver != null)
                    {
                        DoRemoveElementAt(index, property);
                    }
                    else
                    {
                        DoRemoveElement(property);
                    }
                }
            }

            EasyEditorGUI.EndListItem();
        }

        private void DrawMetroItem(InspectorProperty property, int index)
        {
            var selected = _selectionList.Contains(index);

            if (selected)
            {
                EasyGUIHelper.PushColor(CollectionDrawerStyles.MetroItemSelectedBackgroundColor);
            }
            else
            {
                EasyGUIHelper.PushColor(CollectionDrawerStyles.MetroItemBackgroundColor);
            }
            var rect = EasyEditorGUI.BeginListItem(false, CollectionDrawerStyles.MetroListItemStyle, GUILayout.MinHeight(25), GUILayout.ExpandWidth(true));
            EasyGUIHelper.PopColor();

            var dragHandleRect = new Rect(rect.x + 4, rect.y + 2 + ((int)rect.height - 23) / 2, 23, 23);

            GUI.Label(dragHandleRect, EasyEditorIcons.List.InactiveTexture, GUIStyle.none);

            HandlePreSelection(rect, index);
            DrawElementProperty(property, index);
            HandlePostSelection(rect, index);

            if (_listDrawerSettings?.HideRemoveButton == false && !_isReadOnly)
            {
                var removeBtnRect = new Rect(dragHandleRect.x + rect.width - 37, dragHandleRect.y - 5, 30, 30);
                if (GUI.Button(removeBtnRect, GUIContent.none, "Button"))
                {
                    EasyGUIHelper.RemoveFocusControl();

                    if (_orderedCollectionResolver != null)
                    {
                        DoRemoveElementAt(index, property);
                    }
                    else
                    {
                        DoRemoveElement(property);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    EasyEditorIcons.X.Draw(removeBtnRect.AlignCenter(25, 25));
                }
            }


            EasyEditorGUI.EndListItem();
        }

        protected virtual void DrawElementProperty(InspectorProperty property, int index)
        {
            if (_listDrawerSettings?.ShowIndexLabel == true)
            {
                string indexLabel;
                if (_customIndexLabelFunction != null)
                {
                    var target = _isListDrawerClassAttribute ? Property.ValueEntry.WeakSmartValue : Property.Parent.ValueEntry.WeakSmartValue;
                    indexLabel = _customIndexLabelFunction(target, index);
                }
                else
                {
                    indexLabel = $"{index}:";
                }
                if (property.Children != null)
                {
                    property.State.Expanded = EasyEditorGUI.Foldout(property.State.Expanded, EditorHelper.TempContent(indexLabel));
                    if (property.State.Expanded)
                    {
                        EditorGUI.indentLevel++;
                        property.Draw(null);
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    property.Draw(EditorHelper.TempContent(indexLabel));
                }
            }
            else
            {
                property.Draw(null);
            }
        }

        protected virtual void DoAddElement(Rect addButtonRect)
        {
            if (_valueDropdownOptionsGetterResolver != null)
            {
                var options = _valueDropdownOptionsGetterResolver.Resolve(Property.Parent.ValueEntry.WeakSmartValue);
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

                    for (int i = 0; i < Property.Tree.Targets.Length; i++)
                    {
                        DoAddElement(i, value);
                    }
                }, (item) => new GUIContent(item.GetText()));
                return;
            }

            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoAddElement(i, GetValueToAdd(i));
            }
        }

        protected virtual void DoAddElement(int targetIndex, object valueToAdd)
        {
            _collectionResolver.QueueInsertElement(targetIndex, valueToAdd);
            _onAddedElementCallback?.Invoke(Property.Parent.ValueEntry.WeakValues[targetIndex], valueToAdd);
        }

        protected virtual void DoRemoveElementAt(int index, InspectorProperty propertyToRemove)
        {
            for (int i = 0; i < Property.Tree.Targets.Length; i++)
            {
                DoRemoveElementAt(i, index, propertyToRemove);
            }
        }

        protected virtual void DoRemoveElementAt(int targetIndex, int index, InspectorProperty propertyToRemove)
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

            var valueToRemove = propertyToRemove.ValueEntry.WeakValues[targetIndex];
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
