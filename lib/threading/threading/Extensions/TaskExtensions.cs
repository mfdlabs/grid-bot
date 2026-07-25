namespace Threading.Extensions;

using System.Threading.Tasks;

#nullable enable

/// <summary>
/// <see cref="Task{TResult}"/> extension methods.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Calls the specified <see cref="Task{TResult}"/> in a sync context.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The <see cref="Task{TResult}"/></param>
    /// <returns>The <typeparamref name="T"/></returns>
    public static T Sync<T>(this Task<T> task) => task.GetAwaiter().GetResult();

    /// <summary>
    /// Tries to <see cref="Sync{T}(Task{T})"/> the specified task and 
    /// returns the default of <typeparamref name="T"/> if it fails.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The <see cref="Task{TResult}"/></param>
    /// <returns>The <typeparamref name="T"/></returns>
    public static T? SyncOrDefault<T>(this Task<T> task)
    {
        try
        {
            return task.Sync();
        }
        catch
        {
            return default(T);
        }
    }
}
