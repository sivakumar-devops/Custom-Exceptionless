using System.Collections.Concurrent;

namespace Exceptionless.Core.Utility;

public interface IConnectionMapping
{
    Task AddAsync(string key, string connectionId);
    Task<ICollection<string>> GetConnectionsAsync(string key);
    Task<int> GetConnectionCountAsync(string key);
    Task RemoveAsync(string key, string connectionId);
}

public class ConnectionMapping : IConnectionMapping
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();
    private readonly ConcurrentDictionary<string, object> _keyLocks = new();

    public Task AddAsync(string key, string connectionId)
    {
        if (key is null)
            return Task.CompletedTask;

        var keyLock = _keyLocks.GetOrAdd(key, _ => new object());

        _connections.AddOrUpdate(key, [.. new[] { connectionId }], (_, hs) =>
        {
            lock (keyLock)
            {
                hs.Add(connectionId);
            }
            return hs;
        });

        return Task.CompletedTask;
    }

    public Task<ICollection<string>> GetConnectionsAsync(string key)
    {
        if (key is null)
            return Task.FromResult<ICollection<string>>(new List<string>());

        return Task.FromResult<ICollection<string>>(_connections.GetOrAdd(key, []));
    }

    public Task<int> GetConnectionCountAsync(string key)
    {
        if (key is null)
            return Task.FromResult(0);

        if (_connections.TryGetValue(key, out var connections))
            return Task.FromResult(connections.Count);

        return Task.FromResult(0);
    }

    public Task RemoveAsync(string key, string connectionId)
    {
        if (key is null)
            return Task.CompletedTask;

        bool shouldRemove = false;
        _connections.AddOrUpdate(key, [], (_, hs) =>
        {
            hs.Remove(connectionId);
            if (hs.Count == 0)
                shouldRemove = true;

            return hs;
        });

        if (!shouldRemove)
            return Task.CompletedTask;

        if (_connections.TryRemove(key, out var connections) && connections.Count > 0)
            _connections.TryAdd(key, connections);

        // Remove from _keyLocks as well
        _keyLocks.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}

public static class ConnectionMappingExtensions
{
    public const string UserIdPrefix = "u-";
    public const string GroupPrefix = "g-";

    public static Task GroupAddAsync(this IConnectionMapping map, string group, string connectionId)
    {
        return map.AddAsync(GroupPrefix + group, connectionId);
    }

    public static Task GroupRemoveAsync(this IConnectionMapping map, string group, string connectionId)
    {
        return map.RemoveAsync(GroupPrefix + group, connectionId);
    }

    public static Task<ICollection<string>> GetGroupConnectionsAsync(this IConnectionMapping map, string group)
    {
        return map.GetConnectionsAsync(GroupPrefix + group);
    }

    public static Task<int> GetGroupConnectionCountAsync(this IConnectionMapping map, string group)
    {
        return map.GetConnectionCountAsync(GroupPrefix + group);
    }

    public static Task UserIdAddAsync(this IConnectionMapping map, string userId, string connectionId)
    {
        return map.AddAsync(UserIdPrefix + userId, connectionId);
    }

    public static Task UserIdRemoveAsync(this IConnectionMapping map, string userId, string connectionId)
    {
        return map.RemoveAsync(UserIdPrefix + userId, connectionId);
    }

    public static Task<ICollection<string>> GetUserIdConnectionsAsync(this IConnectionMapping map, string userId)
    {
        return map.GetConnectionsAsync(UserIdPrefix + userId);
    }
}
