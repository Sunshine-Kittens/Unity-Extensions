using System;

namespace UnityEngine.Extension
{
    public readonly struct AnimationPlayable
    {
        public readonly IAnimation Animation;
        public readonly float StartTime;
        public readonly PlaybackMode PlaybackMode;
        public readonly EasingMode EasingMode;
        public readonly float PlaybackSpeed;
        public readonly TimeMode TimeMode;
        
        public AnimationPlayable(IAnimation animation, in AnimationPlaybackParams playbackParams)
        {
            Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            EasingMode = playbackParams.EasingMode;
            StartTime = playbackParams.StartTime;
            PlaybackMode = playbackParams.PlaybackMode;
            TimeMode = playbackParams.TimeMode;
            PlaybackSpeed = playbackParams.PlaybackSpeed;
        }
        
        public AnimationPlayable(IAnimation animation, float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, 
            EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            EasingMode = easingMode;
            StartTime = Mathf.Clamp(startTime, 0.0F, animation.Length);
            PlaybackMode = playbackMode;
            TimeMode = timeMode;
            PlaybackSpeed = playbackSpeed;
        }

        public bool IsValid()
        {
            return Animation != null;
        }
        
        public AnimationPlayable CreateInverse()
        {
            return new AnimationPlayable(Animation, Animation.Length - StartTime, PlaybackMode.Invert(), EasingMode.GetInverseEasingMode(), TimeMode, PlaybackSpeed);
        }
    }
}
