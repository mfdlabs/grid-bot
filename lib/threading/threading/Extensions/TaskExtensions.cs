using System.Threading.Tasks;

#nullable enable

namespace Threading.Extensions
{
    public static class TaskExtensions
    {
        public static T Sync<T>(this Task<T> task) => task.GetAwaiter().GetResult();
        public static T? SyncOrDefault<T>(this Task<T> task)
        {
            try
            {
                return task.Sync();
            }
            catch
            {
                return default;
            }
        }
    }
}
