using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    class SelectionList : IEnumerable<int>
    {
        private List<int> _indices = new List<int>();
        private int? _lastFirstSelection;
        public int Length => _indices.Count;

        public void SelectWhenNoAction(int index)
        {
            if (!EditorGUI.actionKey && !Event.current.shift)
            {
                Select(index);
            }
        }

        public void AppendWithAction(int index, Event evt)
        {
            if (EditorGUI.actionKey)
            {
                if (Contains(index))
                {
                    Remove(index);
                }
                else
                {
                    Append(index);
                    _lastFirstSelection = index;
                }
            }
            else if (evt.shift && _indices.Count > 0 && _lastFirstSelection.HasValue)
            {
                _indices.Clear();

                AppendRange(_lastFirstSelection.Value, index);
            }
            else if (!Contains(index))
            {
                Select(index);
            }
        }

        public bool Contains(int index)
        {
            return _indices.Contains(index);
        }

        public void Select(int index)
        {
            _indices.Clear();
            _indices.Add(index);
            _lastFirstSelection = index;
        }

        public void Remove(int index)
        {
            _indices.Remove(index);
        }


        private void Append(int index)
        {
            if (index >= 0 && !_indices.Contains(index))
            {
                _indices.Add(index);
            }
        }

        private void AppendRange(int from, int to)
        {
            int dir = (int)Mathf.Sign(to - from);

            if (dir != 0)
            {
                for (int i = from; i != to; i += dir)
                {
                    Append(i);
                }
            }

            Append(to);
        }

        public void Clear()
        {
            _indices.Clear();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _indices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class DragList
    {

    }

    public partial class CollectionDrawer<T>
    {
        private bool _dragging;
        private bool _draggable;
        private bool _multipleSelection;
        private int _pressedIndex;
        private SelectionList _selectionList = new SelectionList();
        private DragList _dragList = new DragList();

        private void HandlePreSelection(Rect rect, int index)
        {
            if (Event.current.type == EventType.MouseDrag && _draggable && GUIUtility.hotControl == _controlID)
            {
                if (_selectionList.Length > 0 && UpdateDragPosition(Event.current.mousePosition, rect))
                {
                    GUIUtility.keyboardControl = _controlID;
                    _dragging = true;
                }

                Event.current.Use();
            }
        }

        private bool UpdateDragPosition(Vector2 mousePosition, Rect rect)
        {
            return true;
        }

        private void HandlePostSelection(Rect rect, int index)
        {
            switch (Event.current.GetTypeForControl(_controlID))
            {
                case EventType.MouseDown:
                    if (rect.Contains(Event.current.mousePosition) && IsSelectionButton())
                    {
                        DoSelection(index, GUIUtility.keyboardControl == 0 ||
                                           GUIUtility.keyboardControl == _controlID ||
                                           Event.current.button == 2);
                    }
                    else
                    {
                        _selectionList.Clear();
                    }
                    break;

                case EventType.MouseUp:
                    if (!_draggable)
                    {
                        _selectionList.SelectWhenNoAction(_pressedIndex);
                    }
                    else if (GUIUtility.hotControl == _controlID)
                    {
                        Event.current.Use();

                        if (_draggable)
                        {
                            _dragging = false;
                        }
                        else
                        {
                            _selectionList.SelectWhenNoAction(_pressedIndex);
                        }

                        GUIUtility.hotControl = 0;
                    }
                    break;
            }
        }

        private void DoSelection(int index, bool setKeyboardControl)
        {
            if (!_draggable)
            {
                _selectionList.SelectWhenNoAction(_pressedIndex = index);
            }
            else
            {
                _selectionList.Select(_pressedIndex = index);
            }

            if (setKeyboardControl)
            {
                GUIUtility.keyboardControl = _controlID;
            }

            Event.current.Use();
        }

        private bool IsSelectionButton()
        {
            return Event.current.button == 0 || Event.current.button == 2;
        }
    }
}
