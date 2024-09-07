using System;

namespace UnityEngine.Extension
{
    public enum PlaybackMode
    {
        Forward,
        Reverse
    }

    public class AnimationPlayer
    {
        public struct PlaybackData
        {
            public float CurrentTime { get { return _player == null ? 0.0F : _player.CurrentTime; } } 
            public float CurrentNormalisedTime { get { return _player == null ? 0.0F : _player.CurrentNormalisedTime; } }
            public float Length { get { return _player == null ? 0.0F : _player.Length; } }
            public float RemainingTime { get { return _player == null ? 0.0F : _player.RemainingTime; } }
            public bool IsPlaying { get { return _player == null ? false : _player.IsPlaying; } }
            public bool IsPaused { get { return _player == null ? false : _player.IsPaused; } }

            private AnimationPlayer _player;

            public PlaybackData(AnimationPlayer player)
            {
                _player = player;
            }

            public void ReleaseReferences()
            {
                _player = null;
            }
        }

        public IAnimation Animation { get; private set; } = null;
        public PlaybackMode PlaybackMode { get; private set; } = PlaybackMode.Forward;
        public EasingMode EasingMode { get; private set; } = EasingMode.Linear;
        public float PlaybackSpeed { get; private set; } = 1.0F;
        public TimeMode TimeMode { get; private set; } = TimeMode.Scaled;
        public float CurrentTime { get; private set; } = 0.0F;
        public float CurrentNormalisedTime { get { return CurrentTime / Length; } }

        public float Length
        {
            get
            {
                return Animation != null ? Animation.Length : 0.0F;
            }
        }

        public float RemainingTime
        {
            get
            {
                switch (PlaybackMode)
                {
                    case PlaybackMode.Forward:
                        return Length - CurrentTime;
                    case PlaybackMode.Reverse:
                        return CurrentTime;
                }
                return 0.0F;
            }
        }

        public bool IsPlaying { get; private set; } = false;
        public bool IsPaused { get; private set; } = false;

        public PlaybackData Data { get; private set; } = default;

        public AnimationEvent OnComplete { get; set; } = default;

        private int _lastUpdateFrame = 0;

        private AnimationPlayer() { }

        public AnimationPlayer(IAnimation animation)
        {
            if(animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            Animation = animation;
            Data = new PlaybackData(this);
        }

        public static AnimationPlayer PlayAnimation(in AnimationPlayable animationPlayable)
        {
            return PlayAnimation(animationPlayable.Animation, animationPlayable.StartTime,
                animationPlayable.PlaybackMode, animationPlayable.EasingMode, animationPlayable.TimeMode,
                animationPlayable.PlaybackSpeed);
        }

        public static AnimationPlayer PlayAnimation(IAnimation animation, float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, 
            EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            AnimationPlayer player = new AnimationPlayer(animation);
            player.Play(startTime, playbackMode, easingMode, timeMode);
            return player;
        }

        public void Play(float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear, 
            TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {   
            if(!IsPlaying)
            {
                IsPlaying = true;
                AnimationSystemRunner.AddPlayer(this);
            }
            Animation.Prepare();

            this.TimeMode = timeMode;
            this.EasingMode = easingMode;
            this.PlaybackMode = playbackMode;
            CurrentTime = Mathf.Clamp(startTime, 0.0F, Length);

            EvaluateAnimation();
        }

        public bool Rewind()
        {
            if (IsPlaying)
            {
                PlaybackMode ^= (PlaybackMode)1;
                return true;
            }
            return false;
        }

        public bool Stop()
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                IsPaused = false;
                return true;
            }
            return false;
        }

        public bool Pause()
        {
            if(IsPlaying && !IsPaused)
            {
                IsPaused = true;
                return true;
            }
            return false;
        }

        public bool Resume()
        {
            if(IsPlaying && IsPaused)
            {
                IsPaused = false;
                return true;
            }
            return false;
        }

        public bool SetCurrentTime(float time)
        {
            if (IsPlaying)
            {
                CurrentTime = Mathf.Clamp(time, 0.0F, Length);
                float endTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;
                bool isAtEnd = Mathf.Approximately(CurrentTime, endTime);

                EvaluateAnimation();

                if (isAtEnd)
                {
                    IsPlaying = false;
                    OnComplete.Invoke(Animation);
                }
                return true;
            }
            return false;
        }

        public bool Complete()
        {
            if (IsPlaying)
            {
                CurrentTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;

                EvaluateAnimation();

                IsPlaying = false;
                OnComplete.Invoke(Animation);
                return true;
            }
            return false;
        }

        public void Update()
        {
            if (IsPlaying && _lastUpdateFrame != Time.frameCount)
            {
                float deltaTime = TimeMode == TimeMode.Scaled? Time.deltaTime : Time.unscaledDeltaTime;
                deltaTime *= PlaybackSpeed;

                bool isAtEnd = false;
                if (!IsPaused)
                {
                    float animationDelta = PlaybackMode == PlaybackMode.Forward ? deltaTime : -deltaTime;
                    CurrentTime = Mathf.Clamp(CurrentTime + animationDelta, 0.0F, Length);

                    float endTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;
                    isAtEnd = Mathf.Approximately(CurrentTime, endTime);
                    // Double set to account for tolerance
                    CurrentTime = isAtEnd ? endTime : CurrentTime;
                }

                EvaluateAnimation();

                if (isAtEnd)
                {
                    IsPlaying = false;
                    OnComplete.Invoke(Animation);
                }
            }                        
        }

        private void EvaluateAnimation()
        {
            _lastUpdateFrame = Time.frameCount;
            Animation.Evaluate(Easing.PerformEase(CurrentNormalisedTime, EasingMode));
        }
    }
}
