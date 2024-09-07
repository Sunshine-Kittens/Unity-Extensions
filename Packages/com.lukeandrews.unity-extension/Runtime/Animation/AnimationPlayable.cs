using System;

namespace UnityEngine.Extension
{
    public struct AnimationPlayable
    {       
        public IAnimation Animation { get; private set; }        
        public float StartTime { get; private set; }
        public PlaybackMode PlaybackMode { get; private set; }
        public EasingMode EasingMode { get; private set; }
        public float PlaybackSpeed { get; private set; }
        public TimeMode TimeMode { get; private set; }

        public AnimationPlayable(IAnimation animation, float startTime, PlaybackMode playbackMode, EasingMode easingMode, TimeMode timeMode, float playbackSpeed)
        {
            if(animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            Animation = animation;
            EasingMode = easingMode;
            StartTime = Mathf.Clamp(startTime, 0.0F, animation.Length);
            PlaybackMode = playbackMode;
            TimeMode = timeMode;
            PlaybackSpeed = playbackSpeed;
        }

        public void Invert()
        {
            StartTime = Animation.Length - StartTime;
            PlaybackMode = PlaybackMode.InvertPlayMode();
        }

        public AnimationPlayable CreateInverse(float startTime)
        {
            return new AnimationPlayable(Animation, startTime, PlaybackMode.InvertPlayMode(), 
                EasingMode.GetInverseEasingMode(), TimeMode, PlaybackSpeed);
        }

        public AnimationPlayable CreateInverse()
        {
            return new AnimationPlayable(Animation, Animation.Length - StartTime, PlaybackMode.InvertPlayMode(), 
                EasingMode.GetInverseEasingMode(), TimeMode, PlaybackSpeed);
        }
    }
}
