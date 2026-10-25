using System;
using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public enum PlaybackMode
    {
        Forward,
        Reverse
    }

    public sealed class AnimationPlayer
    {
        public readonly struct PlaybackData
        {
            public EasingMode EasingMode => _player == null ? 0.0F : _player.EasingMode;
            public float PlaybackSpeed => _player == null ? 0.0F : _player.PlaybackSpeed;
            public float CurrentTime => _player == null ? 0.0F : _player.CurrentTime;
            public float CurrentNormalisedTime => _player == null ? 0.0F : _player.CurrentNormalisedTime;
            public float Length => _player == null ? 0.0F : _player.Length;
            public float RemainingTime => _player == null ? 0.0F : _player.RemainingTime;
            public bool IsPlaying => _player == null ? false : _player.IsPlaying;
            public bool IsPaused => _player == null ? false : _player.IsPaused;

            private readonly AnimationPlayer _player;

            public PlaybackData(AnimationPlayer player)
            {
                _player = player;
            }
        }

        public IAnimation Animation { get; private set; } = null;
        public PlaybackMode PlaybackMode { get; private set; } = PlaybackMode.Forward;
        public EasingMode EasingMode { get; private set; } = EasingMode.Linear;
        public float PlaybackSpeed { get; private set; } = 1.0F;
        public TimeMode TimeMode { get; private set; } = TimeMode.Scaled;
        public float CurrentTime { get; private set; } = 0.0F;
        public float CurrentNormalisedTime => CurrentTime / Length;

        public float Length => Animation != null ? Animation.Length : 0.0F;

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

        public AnimationAction OnComplete { get; set; } = default;
        
        private AnimationEvent[] _sortedEvents;
        private int _nextEventIndex = 0;
        private int _lastUpdateFrame = 0;
        private float _lastFrameTime = 0.0F;
        
        private static readonly Stack<AnimationPlayer> _AnimationPlayerPool = new();

        private static AnimationPlayer GetOrCreate(IAnimation animation)
        {
            AnimationPlayer player;
            if (_AnimationPlayerPool.Count > 0)
            {
                player = _AnimationPlayerPool.Pop();
                player.Reinitialize(animation);
            }
            else
            {
                player = new AnimationPlayer(animation);
            }
            return player;
        }

        private static void ReturnToPool(AnimationPlayer animationPlayer)
        {
            animationPlayer.Reset();
            _AnimationPlayerPool.Push(animationPlayer);
        }

        public static AnimationPlayer PlayAnimation(in AnimationPlayable animationPlayable)
        {
            return PlayAnimation(animationPlayable.Animation, animationPlayable.StartTime,
                animationPlayable.PlaybackMode, animationPlayable.EasingMode, animationPlayable.TimeMode,
                animationPlayable.PlaybackSpeed);
        }

        public static AnimationPlayer PlayAnimation(IAnimation animation, in AnimationPlaybackParams playbackParams)
        {
            AnimationPlayer player = GetOrCreate(animation);
            player.Play(playbackParams.StartTime, playbackParams.PlaybackMode, playbackParams.EasingMode, playbackParams.TimeMode,
                playbackParams.PlaybackSpeed);
            return player;
        }

        public static AnimationPlayer PlayAnimation(IAnimation animation, float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward,
            EasingMode easingMode = EasingMode.Linear, TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            AnimationPlayer player = GetOrCreate(animation);
            player.Play(startTime, playbackMode, easingMode, timeMode, playbackSpeed);
            return player;
        }

        public static AnimationPlayer Duplicate(AnimationPlayer animationPlayer)
        {
            AnimationPlayer duplicate = GetOrCreate(animationPlayer.Animation);
            duplicate.PlaybackMode = animationPlayer.PlaybackMode;
            duplicate.EasingMode = animationPlayer.EasingMode;
            duplicate.PlaybackSpeed = animationPlayer.PlaybackSpeed;
            duplicate.TimeMode = animationPlayer.TimeMode;
            duplicate.CurrentTime = animationPlayer.CurrentTime;
            duplicate.IsPlaying = animationPlayer.IsPlaying;
            duplicate.IsPaused = animationPlayer.IsPaused;
            // OnComplete is deliberately not copied: the source's subscribers belong to the source's owner,
            // and a copied invocation list cannot be unsubscribed through the source. Callers wire their own.
            duplicate._sortedEvents = animationPlayer._sortedEvents;
            duplicate._nextEventIndex = animationPlayer._nextEventIndex;
            duplicate._lastUpdateFrame = animationPlayer._lastUpdateFrame;
            duplicate._lastFrameTime = animationPlayer._lastFrameTime;
            if (duplicate.IsPlaying)
            {
                AnimationSystemRunner.AddPlayer(duplicate);
            }
            return duplicate;
        }
        
        private AnimationPlayer() { }

        public AnimationPlayer(IAnimation animation)
        {
            if (animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            Animation = animation;
            Data = new PlaybackData(this);
            if (Animation.Events != null)
            {
                _sortedEvents = new AnimationEvent[Animation.Events.Count];
                for (int i = 0; i < Animation.Events.Count; i++)
                {
                    _sortedEvents[i] = Animation.Events[i];
                }

                int Comparer(AnimationEvent lhs, AnimationEvent rhs)
                {
                    return lhs.Time.CompareTo(rhs.Time);
                }

                Array.Sort(_sortedEvents, Comparer<AnimationEvent>.Create(Comparer));
            }
            else
            {
                _sortedEvents = Array.Empty<AnimationEvent>();
            }
        }

        private void Reinitialize(IAnimation animation)
        {
            if (animation == null)
            {
                throw new ArgumentNullException(nameof(animation));
            }

            Animation = animation;
            if (Animation.Events != null)
            {
                _sortedEvents = new AnimationEvent[Animation.Events.Count];
                for (int i = 0; i < Animation.Events.Count; i++)
                {
                    _sortedEvents[i] = Animation.Events[i];
                }

                int Comparer(AnimationEvent lhs, AnimationEvent rhs)
                {
                    return lhs.Time.CompareTo(rhs.Time);
                }

                Array.Sort(_sortedEvents, Comparer<AnimationEvent>.Create(Comparer));
            }
            else
            {
                _sortedEvents = Array.Empty<AnimationEvent>();
            }
        }

        public void Play(float startTime = 0.0F, PlaybackMode playbackMode = PlaybackMode.Forward, EasingMode easingMode = EasingMode.Linear,
            TimeMode timeMode = TimeMode.Scaled, float playbackSpeed = 1.0F)
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (!IsPlaying)
            {
                IsPlaying = true;
                AnimationSystemRunner.AddPlayer(this);
            }

            TimeMode = timeMode;
            EasingMode = easingMode;
            PlaybackMode = playbackMode;
            PlaybackSpeed = playbackSpeed;
            CurrentTime = Mathf.Clamp(startTime, 0.0F, Length);
            _lastFrameTime = CurrentTime;
            _nextEventIndex = GetNextAnimationEventIndex(PlaybackMode, CurrentTime);

            EvaluateAnimation();
            InvokeEvents();
        }

        public bool Rewind()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying)
            {
                PlaybackMode ^= (PlaybackMode)1;
                _nextEventIndex = GetNextAnimationEventIndex(PlaybackMode, CurrentTime);
                return true;
            }
            return false;
        }

        public bool Stop()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
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
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying && !IsPaused)
            {
                IsPaused = true;
                return true;
            }
            return false;
        }

        public bool Resume()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying && IsPaused)
            {
                IsPaused = false;
                return true;
            }
            return false;
        }

        public bool SetCurrentTime(float time)
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying)
            {
                CurrentTime = Mathf.Clamp(time, 0.0F, Length);
                _lastFrameTime = CurrentTime;
                _nextEventIndex = GetNextAnimationEventIndex(PlaybackMode, CurrentTime);

                float endTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;
                bool isAtEnd = Mathf.Approximately(CurrentTime, endTime);

                EvaluateAnimation();

                if (isAtEnd)
                {
                    IsPlaying = false;
                    OnComplete?.Invoke(Animation);
                }
                return true;
            }
            return false;
        }

        public bool Complete()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying)
            {
                CurrentTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;
                _lastFrameTime = CurrentTime;
                _nextEventIndex = GetNextAnimationEventIndex(PlaybackMode, CurrentTime);

                EvaluateAnimation();

                IsPlaying = false;
                OnComplete?.Invoke(Animation);
                return true;
            }
            return false;
        }

        public void Update()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer does not hold a reference to a valid animation.");
            
            if (IsPlaying && _lastUpdateFrame != Time.frameCount)
            {
                float deltaTime = TimeMode == TimeMode.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;
                deltaTime *= PlaybackSpeed;

                bool isAtEnd = false;
                if (!IsPaused)
                {
                    _lastFrameTime = CurrentTime;
                    float animationDelta = PlaybackMode == PlaybackMode.Forward ? deltaTime : -deltaTime;
                    float currentTime = Mathf.Clamp(CurrentTime + animationDelta, 0.0F, Length);
                    float endTime = PlaybackMode == PlaybackMode.Forward ? Length : 0.0F;
                    isAtEnd = Mathf.Approximately(currentTime, endTime);
                    CurrentTime = isAtEnd ? endTime : currentTime;
                }

                EvaluateAnimation();
                InvokeEvents();

                if (isAtEnd)
                {
                    IsPlaying = false;
                    OnComplete?.Invoke(Animation);
                }
            }
        }

        private void EvaluateAnimation()
        {
            _lastUpdateFrame = Time.frameCount;
            Animation.Evaluate(Easing.PerformEase(CurrentNormalisedTime, EasingMode));
        }

        private void InvokeEvents()
        {
            switch (PlaybackMode)
            {
                case PlaybackMode.Forward:
                    for (int i = _nextEventIndex; i >= 0 && i < _sortedEvents.Length; i++)
                    {
                        AnimationEvent animEvent = _sortedEvents[i];
                        if (animEvent.Time >= _lastFrameTime && animEvent.Time <= CurrentTime)
                        {
                            animEvent.Invoke();
                            _nextEventIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;
                case PlaybackMode.Reverse:
                    for (int i = _nextEventIndex; i >= 0 && i < _sortedEvents.Length; i--)
                    {
                        AnimationEvent animEvent = _sortedEvents[i];
                        // Time runs backwards, so the frame's elapsed window is [CurrentTime, _lastFrameTime].
                        if (animEvent.Time <= _lastFrameTime && animEvent.Time >= CurrentTime)
                        {
                            animEvent.Invoke();
                            _nextEventIndex--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;
            }
        }

        // Index of the next event to fire, or a past-the-end sentinel when none remain: Length going forward,
        // -1 going in reverse. A single -1 for both would be read as a valid index by the forward loop.
        private int GetNextAnimationEventIndex(PlaybackMode playbackMode, float time)
        {
            switch (playbackMode)
            {
                case PlaybackMode.Forward:
                    for (int i = 0; i < _sortedEvents.Length; i++)
                    {
                        if (_sortedEvents[i].Time >= time)
                        {
                            return i;
                        }
                    }
                    return _sortedEvents.Length;
                case PlaybackMode.Reverse:
                    for (int i = _sortedEvents.Length - 1; i >= 0; i--)
                    {
                        if (_sortedEvents[i].Time <= time)
                        {
                            return i;
                        }
                    }
                    return -1;
            }
            return -1;
        }
        
        public void Release()
        {
            if (Animation == null) throw new InvalidOperationException("AnimationPlayer has already been released.");
            AnimationSystemRunner.RemovePlayer(this);
            ReturnToPool(this);
        }
        
        private void Reset()
        {
            Animation = null;
            PlaybackMode = PlaybackMode.Forward;
            EasingMode = EasingMode.Linear;
            PlaybackSpeed = 1.0F;
            TimeMode = TimeMode.Scaled;
            CurrentTime = 0.0F;
            IsPlaying = false;
            IsPaused = false;
            OnComplete = null;
            _sortedEvents = null;
            _nextEventIndex = 0;
            _lastUpdateFrame = 0;
            _lastFrameTime = 0.0F;
        }
    }
}