using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public static class Animation
    {
        private sealed class UnityAnimationClipAnimation : IAnimation
        {
            public float Length => _clip.length;
            public IReadOnlyList<AnimationEvent> Events => new List<AnimationEvent>(0);

            private readonly GameObject _gameObject;
            private readonly AnimationClip _clip;
        
            public UnityAnimationClipAnimation(GameObject gameObject, AnimationClip clip)
            {
                _clip = clip ?? throw new ArgumentNullException(nameof(clip));
                _gameObject = gameObject ??  throw new ArgumentNullException(nameof(gameObject));
            }
        
            public void Evaluate(float normalisedTime)
            {
                _clip.SampleAnimation(_gameObject, normalisedTime * _clip.length);
            }

            public void Prepare() { }
        }
        
        public static AnimationPlaybackParams PlaybackParams(float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, 
            EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            return new AnimationPlaybackParams(startTime, playbackMode, easingMode, timeMode, playbackSpeed);
        }

        public static AnimationPlaybackParams PlaybackParams(this IAnimation animation, float length, float startTime = 0.0F, 
            PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear, 
            TimeMode timeMode = TimeMode.Scaled)
        {
            return new AnimationPlaybackParams(startTime, playbackMode, easingMode, timeMode, animation.Length / length);
        }
        
        public static AnimationPlayable Playable(this IAnimation animation, float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, 
            EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            return new AnimationPlayable(animation, startTime, playbackMode, easingMode, timeMode, playbackSpeed);
        }
        
        public static AnimationPlayable Playable(this IAnimation animation, float length, float startTime = 0.0F, 
            PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled)
        {
            return new AnimationPlayable(animation, startTime, playbackMode, easingMode, timeMode, animation.Length / length);
        }
        
        public static AnimationPlayable Playable(this IAnimation animation, float length, in AnimationPlaybackParams playbackParams)
        {
            return new AnimationPlayable(animation, playbackParams.StartTime, playbackParams.PlaybackMode, playbackParams.EasingMode, 
                playbackParams.TimeMode, animation.Length / length);
        }
        
        public static AnimationPlayable Playable(this IAnimation animation, in AnimationPlaybackParams playbackParams)
        {
            return new AnimationPlayable(animation, playbackParams);
        }
        
        public static AnimationPlayable Playable(this AnimationClip animationClip, GameObject gameObject, float startTime = 0.0F, 
            PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, 
            float playbackSpeed = 1.0F)
        {
            return new AnimationPlayable(new UnityAnimationClipAnimation(gameObject, animationClip), startTime, playbackMode, easingMode, timeMode, 
                playbackSpeed);
        }
        
        public static AnimationPlayable Playable(this AnimationClip animationClip, GameObject gameObject, in AnimationPlaybackParams playbackParams)
        {
            return new AnimationPlayable(new UnityAnimationClipAnimation(gameObject, animationClip), in  playbackParams);
        }
        
        public static PlaybackMode Invert(this PlaybackMode playbackMode)
        {
            return playbackMode ^ (PlaybackMode)1;
        }

        public static AnimationEventVoid Event(float time, Action eventAction)
        {
            return new AnimationEventVoid(time, eventAction);
        }
        
        public static AnimationEventFloat FloatEvent(float time, Func<float> paramFunc, Action<float> eventAction)
        {
            return new AnimationEventFloat(time, paramFunc, eventAction);
        }
        
        public static AnimationEventInt IntEvent(float time, Func<int> paramFunc, Action<int> eventAction)
        {
            return new AnimationEventInt(time, paramFunc, eventAction);
        }
        
        public static AnimationEventString StringEvent(float time, Func<string> paramFunc, Action<string> eventAction)
        {
            return new AnimationEventString(time, paramFunc, eventAction);
        }
        
        public static AnimationEventObj ObjectEvent(float time, Func<object> paramFunc, Action<object> eventAction)
        {
            return new AnimationEventObj(time, paramFunc, eventAction);
        }
        
        public static AnimationEventUnityObj UnityObjectEvent(float time, Func<UnityEngine.Object> paramFunc, Action<UnityEngine.Object> eventAction)
        {
            return new AnimationEventUnityObj(time, paramFunc, eventAction);
        }
    }
}
