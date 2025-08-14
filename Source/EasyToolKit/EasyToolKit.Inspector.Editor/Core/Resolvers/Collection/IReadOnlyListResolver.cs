using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyToolKit.Inspector.Editor
{
    public class IReadOnlyListResolver<TCollection, TElement> : CollectionResolverBase<TCollection, TElement>
        where TCollection : IReadOnlyList<TElement>
    {
        private int _minLength;
        private readonly Dictionary<int, InspectorPropertyInfo> _propertyInfosByIndex =
            new Dictionary<int, InspectorPropertyInfo>();

        protected override void Initialize()
        {
            _minLength = CalculateMinLength();
        }

        public override int ChildNameToIndex(string name)
        {
            throw new NotSupportedException();
        }

        public override int GetChildCount()
        {
            return _minLength;
        }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            if (_propertyInfosByIndex.TryGetValue(childIndex, out var info))
            {
                return info;
            }

            info = InspectorPropertyInfo.CreateForValue(
                ElementType,
                $"[{childIndex}]",
                new GenericValueAccessor<TCollection, TElement>(
                    (list) => list[childIndex],
                    (list, value) => throw new NotSupportedException("Readonly list cannot be modified")
                )
            );

            _propertyInfosByIndex[childIndex] = info;
            return info;
        }

        protected override void InsertElement(int targetIndex, TElement value)
        {
            throw new NotSupportedException("Readonly list cannot be modified");
        }

        protected override void RemoveElement(int targetIndex, TElement value)
        {
            throw new NotSupportedException("Readonly list cannot be modified");
        }

        protected override bool IsReadOnlyCollection(TCollection collection)
        {
            return true;
        }

        protected override bool ApplyChanges()
        {
            var result = base.ApplyChanges();
            _minLength = CalculateMinLength();
            return result;
        }

        private int CalculateMinLength()
        {
            var minLength = int.MaxValue;
            foreach (var value in ValueEntry.Values)
            {
                if (value == null)
                {
                    return 0;
                }
                minLength = Mathf.Min(minLength, value.Count);
            }
            return minLength;
        }
    }
}
