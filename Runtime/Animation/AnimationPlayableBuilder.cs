using System;

using Unity.Burst;

namespace UnityEngine.Extension
{
    [BurstCompile]
    public readonly ref struct AnimationPlayableBuilder
    {
        [Flags]
        private enum BuilderFlags : byte
        {
            None = 0,
            StartTime = 1 << 0,
            PlaybackMode = 1 << 1,
            EasingMode = 1 << 2,
            TimeMode = 1 << 3,
            PlaybackSpeed = 1 << 4,
            Length = 1 << 5
        }

        private readonly float _startTime;
        private readonly PlaybackMode _playbackMode;
        private readonly EasingMode _easingMode;
        private readonly TimeMode _timeMode;
        private readonly float _playbackSpeed;
        private readonly float _length;
        private readonly BuilderFlags _flags;
        
        private readonly IAnimation _animation;

        internal AnimationPlayableBuilder(IAnimation animation)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            _animation = animation;
            _startTime = 0.0F;
            _playbackMode = PlaybackMode.Forward;
            _easingMode = EasingMode.Linear;
            _timeMode = TimeMode.Scaled;
            _playbackSpeed = 1.0F;
            _length = 1.0F;
            _flags = BuilderFlags.None;
        }
         
        private AnimationPlayableBuilder(IAnimation animation, float startTime, PlaybackMode playbackMode, EasingMode easingMode, TimeMode timeMode, 
            float playbackSpeed, float length, BuilderFlags flags)
        {
            _animation = animation;
            _startTime = startTime;
            _playbackMode = playbackMode;
            _easingMode = easingMode;
            _timeMode = timeMode;
            _playbackSpeed = playbackSpeed;
            _length = length;
            _flags = flags;
        }
        
        public AnimationPlayableBuilder WithStartTime(float startTime)
        {
            return new AnimationPlayableBuilder(
                _animation,
                startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _length,
                _flags | BuilderFlags.StartTime 
            );
        }

        public AnimationPlayableBuilder WithPlaybackMode(PlaybackMode playbackMode)
        {
            return new AnimationPlayableBuilder(
                _animation,
                _startTime,
                playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                _length,
                _flags | BuilderFlags.PlaybackMode 
            );
        }

        public AnimationPlayableBuilder WithEasingMode(EasingMode easingMode)
        {
            return new AnimationPlayableBuilder(
                _animation,
                _startTime,
                _playbackMode,
                easingMode,
                _timeMode,
                _playbackSpeed,
                _length,
                _flags | BuilderFlags.EasingMode 
            );
        }

        public AnimationPlayableBuilder WithTimeMode(TimeMode timeMode)
        {
            return new AnimationPlayableBuilder(
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                timeMode,
                _playbackSpeed,
                _length,
                _flags | BuilderFlags.TimeMode 
            );
        }

        public AnimationPlayableBuilder WithPlaybackSpeed(float playbackSpeed)
        {
            return new AnimationPlayableBuilder(
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                playbackSpeed,
                _length,
                _flags | BuilderFlags.PlaybackSpeed 
            );
        }

        public AnimationPlayableBuilder WithLength(float length)
        {
            return new AnimationPlayableBuilder(
                _animation,
                _startTime,
                _playbackMode,
                _easingMode,
                _timeMode,
                _playbackSpeed,
                length,
                _flags | BuilderFlags.Length 
            );
        }
        
        public AnimationPlayable Create()
        {
            float startTime = _flags.HasFlag(BuilderFlags.StartTime) ? _startTime : 0.0F;
            PlaybackMode playbackMode = _flags.HasFlag(BuilderFlags.PlaybackMode) ? _playbackMode : PlaybackMode.Forward;
            EasingMode easingMode = _flags.HasFlag(BuilderFlags.EasingMode) ? _easingMode : EasingMode.Linear;
            TimeMode timeMode = _flags.HasFlag(BuilderFlags.TimeMode) ? _timeMode : TimeMode.Scaled;
            float playbackSpeed = _flags.HasFlag(BuilderFlags.PlaybackSpeed) ? _playbackSpeed : 1.0F;
            if (_flags.HasFlag(BuilderFlags.Length))
                playbackSpeed = (_animation.Length / _length) * playbackSpeed;
            return new AnimationPlayable(_animation, startTime, playbackMode, easingMode, timeMode, playbackSpeed);
        }
    }
}