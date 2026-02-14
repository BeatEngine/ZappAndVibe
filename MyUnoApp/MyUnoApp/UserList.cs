using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace MyUnoApp
{
    class UserList<T> : ObservableCollection<T>
    {
        // Sortiert die Liste basierend auf einer Property, ohne die Referenzen zu zerstören
        public void Sort<TKey>(Func<T, TKey> keySelector, bool ascending = true)
        {
            var sortedItems = ascending
                ? this.OrderBy(keySelector).ToList()
                : this.OrderByDescending(keySelector).ToList();

            for (int i = 0; i < sortedItems.Count; i++)
            {
                var item = sortedItems[i];
                int oldIndex = IndexOf(item);

                // Nur verschieben, wenn der Index sich tatsächlich geändert hat
                if (oldIndex != i)
                {
                    Move(oldIndex, i); // Benachrichtigt das UI automatisch
                }
            }
        }
    }
}
