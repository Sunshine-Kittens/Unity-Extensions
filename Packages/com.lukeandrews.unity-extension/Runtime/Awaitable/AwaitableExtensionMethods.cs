using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace UnityEngine.Extension.Awaitable
{
    public static class AwaitableExtensionMethods
    {
        public static TaskAwaiter<AsyncOperation> GetAwaiter(this AsyncOperation asyncOperation)
        {
            TaskCompletionSource<AsyncOperation> completionSource = new TaskCompletionSource<AsyncOperation>();
            if (asyncOperation.isDone)
            {
                completionSource.SetResult(asyncOperation);
            }
            else
            {
                asyncOperation.completed += delegate (AsyncOperation asyncOperation)
                {
                    completionSource.SetResult(asyncOperation);
                };
            }
            return completionSource.Task.GetAwaiter();
        }
    }
}
