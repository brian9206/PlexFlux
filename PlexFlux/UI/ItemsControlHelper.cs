using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlexFlux.UI
{
    class ItemsControlHelper
    {
        public static DependencyObject GetItemChildByIndex(ItemsControl itemsControl, int index, int depth = 1)
        {
            var dependencyObject = itemsControl.ItemContainerGenerator.ContainerFromIndex(index);

            // probably virtualized
            if (dependencyObject == null)
                return null;

            // search in depth
            for (int i = 0; i < depth; i++)
                dependencyObject = VisualTreeHelper.GetChild(dependencyObject, 0);

            return dependencyObject;
        }

        public static int FindIndexByItemChild(ItemsControl itemsControl, DependencyObject itemChild, int depth = 1)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var dependencyObject = GetItemChildByIndex(itemsControl, i, depth);

                if (dependencyObject == itemChild)
                    return i;
            }

            // not found
            return -1;
        }
    }
}
