using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Extension.Awaitable
{
    public sealed class WhenAll
    {
        public UnityEngine.Awaitable Awaitable => _completionSource.Awaitable;
        
        private readonly UnityEngine.Awaitable[] _awaitables;
        private readonly ConcurrentQueue<Exception> _exceptions = new();
        private readonly AwaitableCompletionSource _completionSource = new();
        private readonly CancellationToken _cancellationToken;
        private int _remaining;
        private int _started;

        private WhenAll(UnityEngine.Awaitable[] awaitables, CancellationToken cancellationToken)
        {
            _awaitables = awaitables ?? throw new ArgumentNullException(nameof(awaitables));
            _remaining = awaitables.Length;
            _cancellationToken = cancellationToken;
        }
        
        public static WhenAll Await(params UnityEngine.Awaitable[] awaitables) => Await(CancellationToken.None, awaitables);
        
        public static WhenAll Await(CancellationToken cancellationToken, params UnityEngine.Awaitable[] awaitables)
        {
            var instance = new WhenAll(awaitables, cancellationToken);
            instance.Execute();
            return instance;
        }
        
        public UnityEngine.Awaitable.Awaiter GetAwaiter() => _completionSource.Awaitable.GetAwaiter();
        
        private void Execute()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;
            
            if (_cancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetException(new OperationCanceledException(_cancellationToken));
                return;
            }
            
            if (_remaining == 0)
            {
                _completionSource.TrySetResult();
                return;
            }
            
            foreach (UnityEngine.Awaitable awaitable in _awaitables)
            {
                _ = AwaitAndSignalAsync(awaitable);
            }
        }
        
        private async UnityEngine.Awaitable AwaitAndSignalAsync(UnityEngine.Awaitable awaitable)
        {
            try
            {
                await awaitable;
            }
            catch (OperationCanceledException operationCancelledException) when (operationCancelledException.CancellationToken == _cancellationToken)
            {
                _exceptions.Enqueue(operationCancelledException);
            }
            catch (Exception exception)
            {
                _exceptions.Enqueue(exception);
            }
            finally
            {
                if (Interlocked.Decrement(ref _remaining) == 0)
                {
                    FinalizeCompletion();
                }
            }
        }
        
        private void FinalizeCompletion()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _completionSource.TrySetException(new OperationCanceledException(_cancellationToken));
                return;
            }
            
            if (!_exceptions.IsEmpty)
            {
                List<Exception> list = new List<Exception>();
                while (_exceptions.TryDequeue(out Exception exception))
                    list.Add(exception);

                AggregateException aggregateException = new AggregateException(list);
                _completionSource.TrySetException(aggregateException);
            }
            else
            {
                _completionSource.TrySetResult();
            }
        }
    }

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
                asyncOperation.completed += delegate(AsyncOperation asyncOperation) { completionSource.SetResult(asyncOperation); };
            }
            return completionSource.Task.GetAwaiter();
        }
    }
}