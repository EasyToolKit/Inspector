using EasyToolKit.Core;
using EasyToolKit.Core.Editor;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public partial class CollectionDrawer<T>
    {
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


        private void DrawMetroItem(InspectorProperty property, int index)
        {
            EasyGUIHelper.PushColor(CollectionDrawerStyles.MetroItemBackgroundColor);
            var rect = EasyEditorGUI.BeginListItem(false, CollectionDrawerStyles.MetroListItemStyle, GUILayout.MinHeight(25), GUILayout.ExpandWidth(true));
            EasyGUIHelper.PopColor();

            var dragHandleRect = new Rect(rect.x + 4, rect.y + 2 + ((int)rect.height - 23) / 2, 23, 23);

            GUI.Label(dragHandleRect, EasyEditorIcons.List.InactiveTexture, GUIStyle.none);

            DrawElementProperty(property, index);

            if (_listDrawerSettings?.HideRemoveButton == false && !_isReadOnly)
            {
                var removeBtnRect = new Rect(dragHandleRect.x + rect.width - 37, dragHandleRect.y - 5, 30, 30);
                if (GUI.Button(removeBtnRect, GUIContent.none, "Button"))
                {
                    EasyGUIHelper.RemoveFocusControl();

                    if (_orderedCollectionResolver != null)
                    {
                        DoRemoveElementAt(index);
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
    }
}
