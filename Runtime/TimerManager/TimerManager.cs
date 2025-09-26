using System;
using System.Collections.Generic;

using UnityEngine.PlayerLoop;

namespace UnityEngine.Extension
{
    public delegate void TimerDelegate();

    public enum TimerStatus
    {
        Pending,
        Active,
        Paused,
        Executing,
        ActivePendingRemoval
    }

    public struct TimerHandle : IEquatable<TimerHandle>
    {
        public ulong handle
        {
            get { return _handle; }
            set { _handle = value; }
        }

        private ulong _handle;

        public bool IsValid()
        {
            return _handle != 0;
        }

        public void Invalidate()
        {
            _handle = 0;
        }

        public bool Equals(TimerHandle other)
        {
            return other._handle == _handle;
        }

        public override bool Equals(object obj)
        {
            if (obj is TimerHandle)
            {
                return Equals((TimerHandle)obj);
            }
            return false;
        }

        public static bool operator ==(TimerHandle lhs, TimerHandle rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TimerHandle lhs, TimerHandle rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }
    }

    public class TimerHandleComparison : IComparer<TimerHandle>
    {
        public Dictionary<ulong, TimerData> Timers = new Dictionary<ulong, TimerData>();

        public int Compare(TimerHandle lhs, TimerHandle rhs)
        {
            TimerData lhsData = Timers[lhs.handle];
            TimerData rhsData = Timers[rhs.handle];
            if (lhsData.ExpireTime < rhsData.ExpireTime)
            {
                return 1;
            }
            else if (lhsData.ExpireTime > rhsData.ExpireTime)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class TimerData
    {
        public bool Loop = false;
        public TimerStatus Status = TimerStatus.Pending;
        public float Rate = 0.0F;
        public double ExpireTime = 0.0;
        public bool UnscaledTime = true;
        public TimerDelegate TimerDelegate = null;
        public TimerHandle Handle = new TimerHandle();
    }

    public static class TimerManager
    {
        private class TimerManagerPlayerLoopSystem : IPlayerLoopSystem
        {
            public EntryPointLocation Location
            {
                get { return EntryPointLocation.Before; }
            }

            public Type EntryPoint
            {
                get { return typeof(Update.ScriptRunBehaviourUpdate); }
            }

            private Action _update = null;

            private TimerManagerPlayerLoopSystem() { }

            public TimerManagerPlayerLoopSystem(Action update)
            {
                _update = update;
            }

            public void Update()
            {
                _update();
            }
        }

        private static ulong _lastAssignedHandle = 0;

        private static readonly Dictionary<ulong, TimerData> _Timers = new Dictionary<ulong, TimerData>();
        private static readonly List<TimerHandle> _ActiveTimers = new List<TimerHandle>();
        private static readonly HashSet<TimerHandle> _PendingTimers = new HashSet<TimerHandle>();
        private static readonly HashSet<TimerHandle> _PausedTimers = new HashSet<TimerHandle>();

        private static TimerHandleComparison _handleComparison
        {
            get
            {
                if (_handleComparisonInstance == null)
                {
                    _handleComparisonInstance = new TimerHandleComparison();
                    _handleComparisonInstance.Timers = _Timers;
                }
                return _handleComparisonInstance;
            }
        }

        private static TimerHandleComparison _handleComparisonInstance = null;

        private static int _lastUpdatedFrame = 0;
        private static double _internalTime = 0.0;
        private static double _internalUnscaledTime = 0.0;

        private static TimerHandle _currentlyExecutingTimer = new TimerHandle();

        public static IPlayerLoopSystem PlayerLoopSystem => _PlayerLoopSystem;

        private static readonly TimerManagerPlayerLoopSystem _PlayerLoopSystem = new TimerManagerPlayerLoopSystem(Update);

        static TimerManager() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            _Timers.Clear();
            _ActiveTimers.Clear();
            _PendingTimers.Clear();
            _PausedTimers.Clear();
            _lastUpdatedFrame = 0;
            _internalTime = 0.0;
            _internalUnscaledTime = 0.0;
            _currentlyExecutingTimer = new TimerHandle();
        }

        private static void Update()
        {
            if (!HasUpdatedThisFrame())
            {
                _internalTime += Time.deltaTime;
                _internalUnscaledTime += Time.unscaledDeltaTime;

                while (_ActiveTimers.Count > 0)
                {
                    int topIndex = _ActiveTimers.Count - 1;
                    TimerHandle topHandle = _ActiveTimers[topIndex];
                    TimerData topData = GetTimer(in topHandle);
                    if (topData.Status == TimerStatus.ActivePendingRemoval)
                    {
                        _ActiveTimers.RemoveAt(topIndex);
                        RemoveTimer(in topHandle);
                        continue;
                    }

                    double internalTime = GetInternalTime(topData.UnscaledTime);
                    if (internalTime > topData.ExpireTime)
                    {
                        // Remove timer from active list and store it during execution
                        _currentlyExecutingTimer = topHandle;
                        _ActiveTimers.RemoveAt(topIndex);
                        // Set status to executing
                        topData.Status = TimerStatus.Executing;

                        // Determine how many times the timer may have elapsed (e.g. for large DeltaTime on a short looping timer)
                        int executionCount = topData.Loop ? Mathf.FloorToInt((float)(internalTime - topData.ExpireTime) / topData.Rate) + 1 : 1;
                        for (int i = 0; i < executionCount; i++)
                        {
                            topData.TimerDelegate.Invoke();

                            // Check whether timer has been invalidated after execution
                            if (!_currentlyExecutingTimer.IsValid() || topData.Status != TimerStatus.Executing)
                            {
                                break;
                            }
                        }

                        if (topData.Loop)
                        {
                            topData.ExpireTime += executionCount * topData.Rate;
                            topData.Status = TimerStatus.Active;
                            AddActiveTimer(topHandle);
                        }

                        _currentlyExecutingTimer.Invalidate();
                    }
                    else
                    {
                        // No need to continue as there will be no timers to execute
                        break;
                    }
                }

                _lastUpdatedFrame = Time.frameCount;
                if (_PendingTimers.Count > 0)
                {
                    foreach (TimerHandle timerHandle in _PendingTimers)
                    {
                        TimerData timerData = GetTimer(timerHandle);
                        timerData.ExpireTime += GetInternalTime(timerData.UnscaledTime);
                        timerData.Status = TimerStatus.Active;
                        AddActiveTimer(timerHandle);
                    }
                    _PendingTimers.Clear();
                }
            }
        }

        private static bool HasUpdatedThisFrame()
        {
            return _lastUpdatedFrame == Time.frameCount;
        }

        public static void SetTimer(ref TimerHandle timerHandle, TimerDelegate timerDelegate, float rate, bool loop = false, float initialDelay = -1.0F, bool unscaledTime = true)
        {
            TimerData timerData = null;
            if (FindTimer(in timerHandle, ref timerData))
            {
                InternalClearTimer(in timerData);
            }

            if (rate > 0.0F)
            {
                TimerData newTimerData = new TimerData();
                newTimerData.TimerDelegate = timerDelegate;
                newTimerData.Rate = rate;
                newTimerData.Loop = loop;
                newTimerData.UnscaledTime = unscaledTime;

                double expireTime = initialDelay >= 0.0 ? initialDelay : rate;

                TimerHandle newHandle;
                if (!HasUpdatedThisFrame())
                {
                    newTimerData.ExpireTime = GetInternalTime(unscaledTime) + expireTime;
                    newTimerData.Status = TimerStatus.Active;
                    newHandle = AddTimer(newTimerData);
                    AddActiveTimer(newHandle);
                }
                else
                {
                    newTimerData.ExpireTime = expireTime;
                    newTimerData.Status = TimerStatus.Pending;
                    newHandle = AddTimer(newTimerData);
                    _PendingTimers.Add(newHandle);
                }
                timerHandle = newHandle;
            }
            else
            {
                timerHandle.Invalidate();
            }
        }

        public static void SetTimerForNextTick(ref TimerHandle timerHandle, TimerDelegate timerDelegate)
        {
            TimerData newTimerData = new TimerData();
            newTimerData.TimerDelegate = timerDelegate;
            newTimerData.Rate = 0.0F;
            newTimerData.Loop = false;
            newTimerData.UnscaledTime = true;
            newTimerData.ExpireTime = GetInternalTime(true);
            newTimerData.Status = TimerStatus.Active;

            TimerHandle newHandle;
            newHandle = AddTimer(newTimerData);
            AddActiveTimer(newHandle);
            timerHandle = newHandle;
        }

        public static bool PauseTimer(in TimerHandle timerHandle)
        {
            TimerData timerData = null;
            if (FindTimer(in timerHandle, ref timerData))
            {
                if (timerData.Status != TimerStatus.Paused)
                {
                    TimerStatus previousStatus = timerData.Status;
                    switch (previousStatus)
                    {
                        case TimerStatus.Pending:
                            _PendingTimers.Remove(timerHandle);
                            break;
                        case TimerStatus.Active:
                            _ActiveTimers.Remove(timerHandle);
                            break;
                        case TimerStatus.Executing:
                            _currentlyExecutingTimer.Invalidate();
                            break;
                    }

                    if (previousStatus == TimerStatus.Executing && !timerData.Loop)
                    {
                        RemoveTimer(timerHandle);
                    }
                    else
                    {
                        _PausedTimers.Add(timerHandle);
                        timerData.Status = TimerStatus.Paused;
                        if (previousStatus == TimerStatus.Pending)
                        {
                            timerData.ExpireTime -= GetInternalTime(timerData.UnscaledTime);
                        }
                    }
                }
            }
            return false;
        }

        public static bool UnPauseTimer(in TimerHandle timerHandle)
        {
            TimerData timerData = null;
            if (FindTimer(in timerHandle, ref timerData))
            {
                if (HasUpdatedThisFrame())
                {
                    timerData.ExpireTime += GetInternalTime(timerData.UnscaledTime);
                    timerData.Status = TimerStatus.Active;
                    AddActiveTimer(timerHandle);
                }
                else
                {
                    timerData.Status = TimerStatus.Pending;
                    _PendingTimers.Add(timerHandle);
                }
                _PausedTimers.Remove(timerHandle);
            }
            return false;
        }

        public static bool ClearTimer(in TimerHandle timerHandle)
        {
            TimerData timerData = null;
            if (FindTimer(in timerHandle, ref timerData))
            {
                InternalClearTimer(timerData);
                return true;
            }
            return false;
        }

        private static void InternalClearTimer(in TimerData timerData)
        {
            switch (timerData.Status)
            {
                case TimerStatus.Pending:
                    RemoveTimer(timerData.Handle);
                    break;
                case TimerStatus.Active:
                    timerData.Status = TimerStatus.ActivePendingRemoval;
                    break;
                case TimerStatus.Paused:
                    _PausedTimers.Remove(timerData.Handle);
                    RemoveTimer(timerData.Handle);
                    break;
                case TimerStatus.Executing:
                    _currentlyExecutingTimer.Invalidate();
                    RemoveTimer(timerData.Handle);
                    break;
                case TimerStatus.ActivePendingRemoval:
                    break;
            }
        }

        public static void ClearAllTimers()
        {
            ulong[] keys = new ulong[_Timers.Count];
            _Timers.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                InternalClearTimer(_Timers[keys[i]]);
            }
        }

        private static TimerHandle AddTimer(TimerData timerData)
        {
            TimerHandle newHandle = new TimerHandle();
            SetHandle(ref newHandle);
            timerData.Handle = newHandle;
            _Timers.Add(newHandle.handle, timerData);
            return newHandle;
        }

        private static void RemoveTimer(in TimerHandle timerHandle)
        {
            _Timers.Remove(timerHandle.handle);
        }

        private static TimerData GetTimer(in TimerHandle timerHandle)
        {
            return _Timers[timerHandle.handle];
        }

        private static bool FindTimer(in TimerHandle timerHandle, ref TimerData timerData)
        {
            if (timerHandle.IsValid())
            {
                if (_Timers.TryGetValue(timerHandle.handle, out timerData))
                {
                    if (timerData.Status != TimerStatus.ActivePendingRemoval)
                    {
                        return true;
                    }
                    else
                    {
                        timerData = null;
                        return false;
                    }
                }
            }
            return false;
        }

        private static void SetHandle(ref TimerHandle timerHandle)
        {
            _lastAssignedHandle++;
            timerHandle.handle = _lastAssignedHandle;
        }

        private static double GetInternalTime(bool unscaled)
        {
            return unscaled ? _internalUnscaledTime : _internalTime;
        }

        private static void AddActiveTimer(in TimerHandle timerHandle)
        {
            int index = _ActiveTimers.BinarySearch(timerHandle, _handleComparison);
            _ActiveTimers.Insert(~index, timerHandle);
        }
    }
}