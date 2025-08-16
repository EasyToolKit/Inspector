using System;
using System.Collections.Generic;
using EasyToolKit.Core;
using EasyToolKit.Core.Editor;
using EasyToolKit.Inspector.Editor.Internal;
using EasyToolKit.ThirdParty.OdinSerializer;
using JetBrains.Annotations;
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

        public static readonly Color ListSelectionBorderColor = new Color(0.24f, 0.59f, 0.9f);

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

    class CollectionItemContext
    {
        public Rect RemoveBtnRect;
        public Rect DragHandleRect;
    }

    public partial class CollectionDrawer<T>
    {
        [CanBeNull] private ICodeValueResolver<Texture> _iconTextureGetterResolver;
        [CanBeNull] private Func<object, int, string> _customIndexLabelFunction;

        private bool _hideRemoveButton;
        private bool _hideAddButton;
        private Vector2 _layoutMousePosition;

        private void InitializeDraw()
        {
            if (_listDrawerSettings != null)
            {
                if (_listDrawerSettings is MetroListDrawerSettingsAttribute metroListDrawerSettings)
                {
                    if (metroListDrawerSettings.IconTextureGetter.IsNotNullOrEmpty())
                    {
                        _iconTextureGetterResolver = CodeValueResolver.Create<Texture>(metroListDrawerSettings.IconTextureGetter, _listDrawerTargetType);
                    }
                }

                if (_listDrawerSettings.ShowIndexLabel != false && _listDrawerSettings.CustomIndexLabelFunction.IsNotNullOrEmpty())
                {
                    var customIndexLabelFunction = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomIndexLabelFunction, BindingFlagsHelper.All, typeof(int))
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomIndexLabelFunction}' in '{_listDrawerTargetType}'");
                    _customIndexLabelFunction = (instance, index) =>
                    {
                        return (string)customIndexLabelFunction.Invoke(instance, new object[] { index });
                    };
                }

                if (_listDrawerSettings.CustomRemoveIndexFunction.IsNotNullOrEmpty())
                {
                    var customRemoveIndexFunction = _listDrawerTargetType.GetMethodEx(_listDrawerSettings.CustomRemoveIndexFunction, BindingFlagsHelper.All, typeof(int))
                        ?? throw new Exception($"Cannot find method '{_listDrawerSettings.CustomRemoveIndexFunction}' in '{_listDrawerTargetType}'");
                    _customRemoveIndexFunction = (instance, index) =>
                    {
                        customRemoveIndexFunction.Invoke(instance, new object[] { index });
                    };
                }
            }

        }

        protected override void DrawProperty(GUIContent label)
        {
            _isReadOnly = _collectionResolver.IsReadOnly || _listDrawerSettings?.IsReadOnly == true;
            _hideRemoveButton = _listDrawerSettings?.HideRemoveButton == true || _isReadOnly;
            _hideAddButton = _listDrawerSettings?.HideAddButton == true || _isReadOnly;
            CollectionDrawerStyles.ListItemStyle.padding.left = _isDraggable ? 25 : 7;
            CollectionDrawerStyles.ListItemStyle.padding.right = _hideRemoveButton ? 4 : 20;

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
            BeginDropZone();
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
            EndDropZone();
            EasyEditorGUI.EndIndentedVertical();
            UpdateLogic();
        }

        private void DrawHeader(GUIContent label)
        {
            EasyEditorGUI.BeginHorizontalToolbar();

            if (label != null)
            {
                GUILayout.Label(label);
            }

            GUILayout.FlexibleSpace();

            if (!_hideAddButton)
            {
                var buttonRect = GUILayoutUtility.GetRect(22, 22, GUILayout.ExpandWidth(false));
                if (EasyEditorGUI.ToolbarButton(buttonRect, EasyEditorIcons.Plus))
                {
                    DoAddElement(buttonRect);
                }
            }

            EasyEditorGUI.EndHorizontalToolbar();
        }

        private void DrawItems()
        {
            int from = 0;
            int to = _count;
            var drawEmptySpace = _dropZone != null && _dropZone.IsBeingHovered || _isDroppingUnityObjects;
            float height = drawEmptySpace ? _isDroppingUnityObjects ? 16 : (DragAndDropManager.CurrentDraggingHandle.Rect.height - 3) : 0;
            var rect = EasyEditorGUI.BeginVerticalList();

            for (int i = 0, j = from, k = from; j < to; i++, j++)
            {
                var dragHandle = BeginDragHandle(j, i);
                {
                    if (drawEmptySpace)
                    {
                        var topHalf = dragHandle.Rect;
                        topHalf.height /= 2;
                        if (topHalf.Contains(_layoutMousePosition) || topHalf.y > _layoutMousePosition.y && i == 0)
                        {
                            GUILayout.Space(height);
                            drawEmptySpace = false;
                            _insertAt = k;
                        }
                    }

                    if (dragHandle.IsDragging == false)
                    {
                        k++;
                        DrawItem(Property.Children[j], dragHandle, j);
                    }
                    else
                    {
                        GUILayout.Space(3);
                        CollectionDrawerStaticContext.DelayedGUIDrawer.Begin(dragHandle.Rect.width, dragHandle.Rect.height, dragHandle.CurrentMethod != DragAndDropMethods.Move);
                        DragAndDropManager.AllowDrop = false;
                        DrawItem(Property.Children[j], dragHandle, j);
                        DragAndDropManager.AllowDrop = true;
                        CollectionDrawerStaticContext.DelayedGUIDrawer.End();
                        if (dragHandle.CurrentMethod != DragAndDropMethods.Move)
                        {
                            GUILayout.Space(3);
                        }
                    }

                    if (drawEmptySpace)
                    {
                        var bottomHalf = dragHandle.Rect;
                        bottomHalf.height /= 2;
                        bottomHalf.y += bottomHalf.height;

                        if (bottomHalf.Contains(_layoutMousePosition) || bottomHalf.yMax < _layoutMousePosition.y && j + 1 == to)
                        {
                            GUILayout.Space(height);
                            drawEmptySpace = false;
                            _insertAt = Mathf.Min(k, to);
                        }
                    }
                }
                EndDragHandle(i);
            }

            if (drawEmptySpace)
            {
                GUILayout.Space(height);
                _insertAt = Event.current.mousePosition.y > rect.center.y ? to : from;
            }

            if (to == Property.Children.Count && Property.ValueEntry.IsConflicted())
            {
                EasyEditorGUI.BeginListItem(false);
                GUILayout.Label(EditorHelper.TempContent("------"), EditorStyles.centeredGreyMiniLabel);
                EasyEditorGUI.EndListItem();
            }

            EasyEditorGUI.EndVerticalList();

            if (Event.current.type == EventType.Repaint)
            {
                _layoutMousePosition = Event.current.mousePosition;
            }
        }

        private void DrawItem(InspectorProperty property, DragHandle dragHandle, int index)
        {
            var itemContext = property.GetPersistentContext("ItemContext", new CollectionItemContext()).Value;
            var rect = EasyEditorGUI.BeginListItem(false, CollectionDrawerStyles.ListItemStyle, GUILayout.MinHeight(25), GUILayout.ExpandWidth(true));
            {
                if (Event.current.type == EventType.Repaint && !_isReadOnly)
                {
                    dragHandle.DragHandleRect = new Rect(rect.x + 4, rect.y, 20, rect.height);
                    itemContext.DragHandleRect = new Rect(rect.x + 4, rect.y + 2 + ((int)rect.height - 23) / 2, 20, 20);
                    itemContext.RemoveBtnRect = new Rect(itemContext.DragHandleRect.x + rect.width - 22, itemContext.DragHandleRect.y + 1, 14, 14);

                    if (_isDraggable)
                    {
                        GUI.Label(itemContext.DragHandleRect, EasyEditorIcons.List.InactiveTexture, GUIStyle.none);
                    }
                }

                EasyGUIHelper.PushHierarchyMode(false);

                DrawElementProperty(property, index);

                EasyGUIHelper.PopHierarchyMode();

                if (!_hideRemoveButton)
                {
                    if (EasyEditorGUI.IconButton(itemContext.RemoveBtnRect, EasyEditorIcons.X))
                    {
                        if (_orderedCollectionResolver != null)
                        {
                            if (index >= 0)
                            {
                                _removeAt = index;
                            }
                        }
                        else
                        {
                            var values = new object[property.ValueEntry.ValueCount];

                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = property.ValueEntry.WeakValues[i];
                            }

                            _removeValues = values;
                        }
                    }
                }
            }
            EasyEditorGUI.EndListItem();
        }

        protected virtual void DrawElementProperty(InspectorProperty property, int index)
        {
            GUIContent label = null;

            if (_listDrawerSettings?.ShowIndexLabel != false)
            {
                label = new GUIContent(index.ToString());
            }

            if (_customIndexLabelFunction != null)
            {
                var value = property.ValueEntry.WeakSmartValue;

                if (object.ReferenceEquals(value, null))
                {
                    if (label == null)
                    {
                        label = new GUIContent("Null");
                    }
                    else
                    {
                        label.text += " : Null";
                    }
                }
                else
                {
                    label = label ?? new GUIContent("");
                    if (label.text != "") label.text += " : ";

                    var target = _isListDrawerClassAttribute ? Property.ValueEntry.WeakSmartValue : Property.Parent.ValueEntry.WeakSmartValue;
                    object text = _customIndexLabelFunction(target, index);
                    label.text += (text == null ? "" : text.ToString());
                }
            }

            if (label != null)
            {
                if (property.Children != null)
                {
                    property.State.Expanded = EasyEditorGUI.Foldout(property.State.Expanded, label);
                    if (property.State.Expanded)
                    {
                        EditorGUI.indentLevel++;
                        property.Draw();
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    property.Draw(label);
                }
            }
            else
            {
                property.Draw(null);
            }
        }
    }
}
