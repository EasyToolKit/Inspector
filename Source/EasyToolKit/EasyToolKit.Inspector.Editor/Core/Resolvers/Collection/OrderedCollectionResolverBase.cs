using System.Collections.Generic;

namespace EasyToolKit.Inspector.Editor
{
    public interface IOrderedCollectionResolver : ICollectionResolver
    {
        void QueueInsertElementAt(int targetIndex, int index, object value);
        void QueueRemoveElementAt(int targetIndex, int index);
        void QueueMoveElemenetAt(int targetIndex, int sourceIndex, int destinationIndex);
    }

    public abstract class OrderedCollectionResolverBase<TCollection, TElement> : CollectionResolverBase<TCollection, TElement>, IOrderedCollectionResolver
        where TCollection : IEnumerable<TElement>
    {
        public void QueueInsertElementAt(int targetIndex, int index, object value)
        {
            EnqueueChange(() => InsertElementAt(targetIndex, index, (TElement)value));
        }

        public void QueueRemoveElementAt(int targetIndex, int index)
        {
            EnqueueChange(() => RemoveElementAt(targetIndex, index));
        }

        public void QueueMoveElemenetAt(int targetIndex, int sourceIndex, int destinationIndex)
        {
            EnqueueChange(() => MoveElemenetAt(targetIndex, sourceIndex, destinationIndex));
        }

        protected abstract void InsertElementAt(int targetIndex, int index, TElement value);

        protected abstract void RemoveElementAt(int targetIndex, int index);

        protected abstract void MoveElemenetAt(int targetIndex, int sourceIndex, int destinationIndex);
    }
}
