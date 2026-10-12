// Based on https://github.com/andrewlock/StronglyTypedId/blob/master/src/StronglyTypedIds/EquatableArray.cs
// Which is based on https://github.com/CommunityToolkit/dotnet/blob/7b53ae23dfc6a7fb12d0fc058b89b6e948f48448/src/CommunityToolkit.Mvvm.SourceGenerators/Helpers/EquatableArray%7BT%7D.cs
// Backing type is changed by me from T[] to ImmutableArray<T>

using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace RWGenerators;

/// <summary>
/// An immutable, equatable array. This is equivalent to <see cref="ImmutableArray{T}"/> but with value equality support.
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    /// <summary>
    /// The underlying <see cref="ImmutableArray{T}"/>.
    /// </summary>
    private readonly ImmutableArray<T> _array;

    /// <summary>
    /// Creates a new <see cref="EquatableArray{T}"/> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray{T}"/> to wrap.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    /// <sinheritdoc/>
    public bool Equals(EquatableArray<T> array)
    {
        return AsSpan().SequenceEqual(array.AsSpan());
    }

    /// <sinheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> array && this.Equals(array);
    }

    /// <sinheritdoc/>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        foreach (T item in _array)
        {
            hashCode.Add(item);
        }
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> wrapping the current items.</returns>
    public ReadOnlySpan<T> AsSpan()
    {
        return _array.AsSpan();
    }

    /// <summary>
    /// Gets the underlying immutable array
    /// </summary>
    public ImmutableArray<T> ImmutableArray => _array;

    public int Length => _array.Length;

    public T this[int index] => _array[index];

    /// <summary>
    /// Returns an enumerator for the contents of the array.
    /// </summary>
    /// <returns>An enumerator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T>.Enumerator GetEnumerator()
    {
        return _array.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator for the contents of the array.
    /// </summary>
    /// <returns>An enumerator.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator for the contents of the array.
    /// </summary>
    /// <returns>An enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_array).GetEnumerator();
    }

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}

internal static class EquatableArray
{
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> values) where T: IEquatable<T>
    {
        if (values.IsEmpty)
        {
            return EquatableArray<T>.Empty;
        }

        return new EquatableArray<T>(values.ToImmutableArray());
    }
}
