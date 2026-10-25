using System.Threading;

namespace UnityEngine.Extension
{
    public static class AnimationAwaiter
    {
        public static Awaitable<IAnimation> WaitUntilComplete(this AnimationPlayer animationPlayer)
        {
            AwaitableCompletionSource<IAnimation> source = new AwaitableCompletionSource<IAnimation>();
            void OnComplete(IAnimation animation)
            {
                animationPlayer.OnComplete -= OnComplete;
                source.TrySetResult(animation);
            }

            animationPlayer.OnComplete += OnComplete;
            return source.Awaitable;
        }
    }
}
