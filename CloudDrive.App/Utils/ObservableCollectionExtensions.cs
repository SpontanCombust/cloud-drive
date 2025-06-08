using System.Collections.ObjectModel;

namespace CloudDrive.App.Utils
{
    internal static class ObservableCollectionExtensions
    {
        public static void InsertSorted<T>(this ObservableCollection<T> collection, T item, Comparison<T> comparison)
        {
            if (collection.Count == 0)
            {
                collection.Add(item);
            }
            else
            {
                bool last = true;

                for (int i = 0; i < collection.Count; i++)
                {
                    int result = comparison.Invoke(collection[i], item);
                    if (result >= 1)
                    {
                        collection.Insert(i, item);
                        last = false;
                        break;
                    }
                }

                if (last)
                {
                    collection.Add(item);
                }
            }
        }
    }
}
