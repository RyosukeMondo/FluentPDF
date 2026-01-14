using FluentPDF.Core.Caching;
using Xunit;

namespace FluentPDF.Core.Tests.Caching;

public class LruCacheTests
{
    private class DisposableItem : IDisposable
    {
        public int Value { get; }
        public bool IsDisposed { get; private set; }

        public DisposableItem(int value)
        {
            Value = value;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    [Fact]
    public void Constructor_WithValidCapacity_CreatesCache()
    {
        // Arrange & Act
        var cache = new LruCache<int, DisposableItem>(10);

        // Assert
        Assert.Equal(10, cache.Capacity);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<int, DisposableItem>(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<int, DisposableItem>(-1));
    }

    [Fact]
    public void Add_SingleItem_IncreasesCount()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var item = new DisposableItem(1);

        // Act
        cache.Add(1, item);

        // Assert
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var item = new DisposableItem(42);
        cache.Add(1, item);

        // Act
        var found = cache.TryGet(1, out var retrievedItem);

        // Assert
        Assert.True(found);
        Assert.NotNull(retrievedItem);
        Assert.Equal(42, retrievedItem.Value);
    }

    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);

        // Act
        var found = cache.TryGet(999, out var retrievedItem);

        // Assert
        Assert.False(found);
        Assert.Null(retrievedItem);
    }

    [Fact]
    public void TryGet_UpdatesLruOrder()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(3);
        var item1 = new DisposableItem(1);
        var item2 = new DisposableItem(2);
        var item3 = new DisposableItem(3);
        var item4 = new DisposableItem(4);

        cache.Add(1, item1);
        cache.Add(2, item2);
        cache.Add(3, item3);

        // Act - Access item1 to make it most recently used
        cache.TryGet(1, out _);

        // Add item4, which should evict item2 (least recently used)
        cache.Add(4, item4);

        // Assert
        Assert.True(cache.TryGet(1, out _)); // Should still exist
        Assert.False(cache.TryGet(2, out _)); // Should be evicted
        Assert.True(cache.TryGet(3, out _)); // Should still exist
        Assert.True(cache.TryGet(4, out _)); // Should exist
        Assert.True(item2.IsDisposed); // Evicted item should be disposed
    }

    [Fact]
    public void Add_BeyondCapacity_EvictsLeastRecentlyUsed()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(3);
        var item1 = new DisposableItem(1);
        var item2 = new DisposableItem(2);
        var item3 = new DisposableItem(3);
        var item4 = new DisposableItem(4);

        cache.Add(1, item1);
        cache.Add(2, item2);
        cache.Add(3, item3);

        // Act
        cache.Add(4, item4);

        // Assert
        Assert.Equal(3, cache.Count);
        Assert.False(cache.TryGet(1, out _)); // First item should be evicted
        Assert.True(cache.TryGet(2, out _));
        Assert.True(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(4, out _));
    }

    [Fact]
    public void Add_BeyondCapacity_DisposesEvictedItem()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(2);
        var item1 = new DisposableItem(1);
        var item2 = new DisposableItem(2);
        var item3 = new DisposableItem(3);

        cache.Add(1, item1);
        cache.Add(2, item2);

        // Act
        cache.Add(3, item3);

        // Assert
        Assert.True(item1.IsDisposed);
        Assert.False(item2.IsDisposed);
        Assert.False(item3.IsDisposed);
    }

    [Fact]
    public void Add_DuplicateKey_ReplacesAndDisposesOldValue()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var oldItem = new DisposableItem(1);
        var newItem = new DisposableItem(2);

        cache.Add(1, oldItem);

        // Act
        cache.Add(1, newItem);

        // Assert
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(1, out var retrievedItem));
        Assert.Equal(2, retrievedItem!.Value);
        Assert.True(oldItem.IsDisposed);
        Assert.False(newItem.IsDisposed);
    }

    [Fact]
    public void Clear_DisposesAllItems()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var item1 = new DisposableItem(1);
        var item2 = new DisposableItem(2);
        var item3 = new DisposableItem(3);

        cache.Add(1, item1);
        cache.Add(2, item2);
        cache.Add(3, item3);

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.Count);
        Assert.True(item1.IsDisposed);
        Assert.True(item2.IsDisposed);
        Assert.True(item3.IsDisposed);
    }

    [Fact]
    public void Dispose_DisposesAllItems()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var item1 = new DisposableItem(1);
        var item2 = new DisposableItem(2);

        cache.Add(1, item1);
        cache.Add(2, item2);

        // Act
        cache.Dispose();

        // Assert
        Assert.True(item1.IsDisposed);
        Assert.True(item2.IsDisposed);
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        var item = new DisposableItem(1);
        cache.Add(1, item);

        // Act
        cache.Dispose();
        cache.Dispose();

        // Assert - Should not throw
        Assert.True(item.IsDisposed);
    }

    [Fact]
    public void Add_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        cache.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => cache.Add(1, new DisposableItem(1)));
    }

    [Fact]
    public void TryGet_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        cache.Add(1, new DisposableItem(1));
        cache.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => cache.TryGet(1, out _));
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);
        cache.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => cache.Clear());
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAdds_AllItemsAdded()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(1000);
        var tasks = new List<Task>();
        var itemsAdded = new List<DisposableItem>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var item = new DisposableItem(index);
                lock (itemsAdded)
                {
                    itemsAdded.Add(item);
                }
                cache.Add(index, item);
            }));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert
        Assert.Equal(100, cache.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(cache.TryGet(i, out var item));
            Assert.NotNull(item);
        }
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentReadsAndWrites_NoExceptions()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(50);
        var tasks = new List<Task>();

        // Pre-populate cache
        for (int i = 0; i < 50; i++)
        {
            cache.Add(i, new DisposableItem(i));
        }

        // Act - Mix of reads and writes
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                if (index % 2 == 0)
                {
                    cache.TryGet(index % 50, out _);
                }
                else
                {
                    cache.Add(index, new DisposableItem(index));
                }
            }));
        }

        // Assert - Should not throw
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks.ToArray()));
        Assert.Null(exception);
    }

    [Fact]
    public void LruEviction_VerifiesCorrectOrder()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(3);
        var items = new[]
        {
            new DisposableItem(1),
            new DisposableItem(2),
            new DisposableItem(3),
            new DisposableItem(4),
            new DisposableItem(5)
        };

        // Act
        cache.Add(1, items[0]);
        cache.Add(2, items[1]);
        cache.Add(3, items[2]);

        // Access 1 and 2 to make them more recent than 3
        cache.TryGet(1, out _);
        cache.TryGet(2, out _);

        // Add 4, should evict 3 (least recently used)
        cache.Add(4, items[3]);

        // Assert
        Assert.True(cache.TryGet(1, out _));
        Assert.True(cache.TryGet(2, out _));
        Assert.False(cache.TryGet(3, out _));
        Assert.True(cache.TryGet(4, out _));
        Assert.True(items[2].IsDisposed); // Item 3 disposed
        Assert.False(items[0].IsDisposed);
        Assert.False(items[1].IsDisposed);
        Assert.False(items[3].IsDisposed);
    }

    [Fact]
    public void Count_ReflectsCurrentCacheSize()
    {
        // Arrange
        var cache = new LruCache<int, DisposableItem>(5);

        // Act & Assert
        Assert.Equal(0, cache.Count);

        cache.Add(1, new DisposableItem(1));
        Assert.Equal(1, cache.Count);

        cache.Add(2, new DisposableItem(2));
        Assert.Equal(2, cache.Count);

        cache.Add(3, new DisposableItem(3));
        Assert.Equal(3, cache.Count);

        cache.Clear();
        Assert.Equal(0, cache.Count);
    }
}
