using System;
using System.Collections;
using System.Collections.Generic;

namespace TowerAutobattler.Domain;

// A live read port, not a snapshot. It never exposes the source collection or a
// mutable element. Authoritative writes remain on the single-threaded owner.
public sealed class ReadOnlyListView<T> : IReadOnlyList<T>
{
    private readonly Func<int> _count;
    private readonly Func<int, T> _read;
    internal ReadOnlyListView(Func<int> count, Func<int, T> read) { _count = count; _read = read; }
    public int Count => _count();
    public T this[int index] => _read(index);
    public int IndexOf(T value)
    {
        for (var i = 0; i < Count; i++) if (EqualityComparer<T>.Default.Equals(this[i], value)) return i;
        return -1;
    }
    public int FindIndex(Predicate<T> predicate)
    {
        for (var i = 0; i < Count; i++) if (predicate(this[i])) return i;
        return -1;
    }
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
