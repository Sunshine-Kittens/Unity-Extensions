using System;

namespace UnityEngine.Extension
{
    public readonly struct AnimationPlaybackParams
    {
        public readonly float StartTime;
        public readonly PlaybackMode PlaybackMode;
        public readonly EasingMode EasingMode;
        public readonly float PlaybackSpeed;
        public readonly TimeMode TimeMode;
        
        public AnimationPlaybackParams(float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear, 
            TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            EasingMode = easingMode;
            StartTime = startTime;
            PlaybackMode = playbackMode;
            TimeMode = timeMode;
            PlaybackSpeed = playbackSpeed;
        }
    }
}
