namespace JG.Logging.Internal;

/// <summary>
/// Thread-safe scope context provider for grouping related log entries.
/// </summary>
public sealed class ScopeContextProvider
{
    private static readonly AsyncLocal<Stack<Dictionary<string, object?>>> ScopeStack = new();

    /// <summary>
    /// Gets the current scope properties.
    /// </summary>
    /// <returns>A dictionary of all properties in the current scope hierarchy, or empty if no scope is active.</returns>
    public static IDictionary<string, object?> GetScopeProperties()
    {
        var result = new Dictionary<string, object?>();
        var stack = ScopeStack.Value;

        if (stack == null || stack.Count == 0)
            return result;

        foreach (var scope in stack.Reverse())
        {
            foreach (var kvp in scope)
            {
                if (!result.ContainsKey(kvp.Key))
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a new scope that groups related log entries.
    /// </summary>
    /// <returns>A disposable scope that restores the previous scope context when disposed.</returns>
    public static IDisposable CreateScope()
    {
        var stack = ScopeStack.Value ??= new Stack<Dictionary<string, object?>>();
        stack.Push(new Dictionary<string, object?>());
        return new ScopeHandle(stack);
    }

    /// <summary>
    /// Adds a property to the current scope.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    public static void AddPropertyToCurrentScope(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var stack = ScopeStack.Value;
        if (stack?.Count > 0)
        {
            var currentScope = stack.Peek();
            currentScope[key] = value;
        }
    }

    private sealed class ScopeHandle : IDisposable
    {
        private readonly Stack<Dictionary<string, object?>> _stack;
        private bool _disposed;

        public ScopeHandle(Stack<Dictionary<string, object?>> stack)
        {
            _stack = stack;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_stack.Count > 0)
                _stack.Pop();
            _disposed = true;
        }
    }
}
