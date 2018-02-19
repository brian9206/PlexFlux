using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;

namespace PlexFlux
{
    // simple ObservableCollection helper
    internal static class ObservableCollectionExtension
    {
        /// <summary>
        /// Add items in an IEnumerable after clearing the collection.
        /// </summary>
        public static void FromEnumerable<T>(this ObservableCollection<T> observableCollection, IEnumerable list)
        {
            observableCollection.Clear();
            observableCollection.ConcatEnumerable(list);
        }

        /// <summary>
        /// Add items in an array after clearing the collection. This is an alias of FromEnumerable.
        /// </summary>
        public static void FromArray<T>(this ObservableCollection<T> observableCollection, T[] list)
        {
            observableCollection.FromEnumerable(list);
        }

        /// <summary>
        /// Add items in an IEnumerable.
        /// </summary>
        public static void ConcatEnumerable<T>(this ObservableCollection<T> observableCollection, IEnumerable list)
        {
            foreach (var item in list)
                observableCollection.Add((T)item);
        }

        /// <summary>
        /// Add items in an array. This is an alias of ConcatEnumerable.
        /// </summary>
        public static void ConcatArray<T>(this ObservableCollection<T> observableCollection, T[] list)
        {
            observableCollection.ConcatEnumerable(list);
        }
    }
}
