using System;
using System.Collections.Generic;

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
            set
            {
                _handle = value;
            }
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

    // Arbitrarily low execution order value, may need updating manually to confirm with project specifics
    [DefaultExecutionOrder(-999)]
    public class TimerManager : Singleton<TimerManager>
    {
        private static ulong _lastAssignedHandle = 0;

        private Dictionary<ulong, TimerData> _timers = new Dictionary<ulong, TimerData>();
        private List<TimerHandle> _activeTimers = new List<TimerHandle>();
        private HashSet<TimerHandle> _pendingTimers = new HashSet<TimerHandle>();
        private HashSet<TimerHandle> _pausedTimers = new HashSet<TimerHandle>();

        private TimerHandleComparison _handleComparison
        {
            get
            {
                if (_handleComparisonInstance == null)
                {
                    _handleComparisonInstance = new TimerHandleComparison();
                    _handleComparisonInstance.Timers = _timers;
                }
                return _handleComparisonInstance;
            }
        }

        protected override bool Persists { get { return true; } }

        private TimerHandleComparison _handleComparisonInstance = null;

        private int _lastUpdatedFrame = 0;
        private double _internalTime = 0.0;
        private double _internalUnscaledTime = 0.0;

        private TimerHandle _currentlyExecutingTimer = new TimerHandle();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAllTimers();
        }

        private void Update()
        {
            if (!HasUpdatedThisFrame())
            {
                _internalTime += Time.deltaTime;
                _internalUnscaledTime += Time.unscaledDeltaTime;

                while (_activeTimers.Count > 0)
                {
                    int topIndex = _activeTimers.Count - 1;
                    TimerHandle topHandle = _activeTimers[topIndex];
                    TimerData topData = GetTimer(in topHandle);
                    if (topData.Status == TimerStatus.ActivePendingRemoval)
                    {
                        _activeTimers.RemoveAt(topIndex);
                        RemoveTimer(in topHandle);
                        continue;
                    }

                    double internalTime = GetInternalTime(topData.UnscaledTime);
                    if (internalTime > topData.ExpireTime)
                    {
                        // Remove timer from active list and store it during execution
                        _currentlyExecutingTimer = topHandle;
                        _activeTimers.RemoveAt(topIndex);
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

                if (_pendingTimers.Count > 0)
                {
                    foreach (TimerHandle timerHandle in _pendingTimers)
                    {
                        TimerData timerData = GetTimer(timerHandle);
                        timerData.ExpireTime += GetInternalTime(timerData.UnscaledTime);
                        AddActiveTimer(timerHandle);
                    }
                    _pendingTimers.Clear();
                }
            }
        }

        private bool HasUpdatedThisFrame()
        {
            return _lastUpdatedFrame == Time.frameCount;
        }

        public void SetTimer(ref TimerHandle timerHandle, TimerDelegate timerDelegate, float rate, bool loop = false, float initialDelay = -1.0F, bool unscaledTime = true)
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
                if (HasUpdatedThisFrame())
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
                    _pendingTimers.Add(newHandle);
                }
                timerHandle = newHandle;
            }
            else
            {
                timerHandle.Invalidate();
            }
        }

        public void SetTimerForNextTick(ref TimerHandle timerHandle, TimerDelegate timerDelegate)
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

        public bool PauseTimer(in TimerHandle timerHandle)
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
                            _pendingTimers.Remove(timerHandle);
                            break;
                        case TimerStatus.Active:
                            _activeTimers.Remove(timerHandle);
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
                        _pausedTimers.Add(timerHandle);
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

        public bool UnPauseTimer(in TimerHandle timerHandle)
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
                    _pendingTimers.Add(timerHandle);
                }
                _pausedTimers.Remove(timerHandle);
            }
            return false;
        }

        public bool ClearTimer(in TimerHandle timerHandle)
        {
            TimerData timerData = null;
            if (FindTimer(in timerHandle, ref timerData))
            {
                InternalClearTimer(timerData);
                return true;
            }
            return false;
        }

        private void InternalClearTimer(in TimerData timerData)
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
                    _pausedTimers.Remove(timerData.Handle);
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

        public void ClearAllTimers()
        {
            ulong[] keys = new ulong[_timers.Count];
            _timers.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                InternalClearTimer(_timers[keys[i]]);
            }
        }

        private TimerHandle AddTimer(TimerData timerData)
        {
            TimerHandle newHandle = new TimerHandle();
            SetHandle(ref newHandle);
            timerData.Handle = newHandle;
            _timers.Add(newHandle.handle, timerData);
            return newHandle;
        }

        private void RemoveTimer(in TimerHandle timerHandle)
        {
            _timers.Remove(timerHandle.handle);
        }

        private TimerData GetTimer(in TimerHandle timerHandle)
        {
            return _timers[timerHandle.handle];
        }

        private bool FindTimer(in TimerHandle timerHandle, ref TimerData timerData)
        {
            if (timerHandle.IsValid())
            {
                if (_timers.TryGetValue(timerHandle.handle, out timerData))
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

        private void SetHandle(ref TimerHandle timerHandle)
        {
            _lastAssignedHandle++;
            timerHandle.handle = _lastAssignedHandle;
        }

        private double GetInternalTime(bool unscaled)
        {
            return unscaled ? _internalUnscaledTime : _internalTime;
        }

        private void AddActiveTimer(in TimerHandle timerHandle)
        {
            int index = _activeTimers.BinarySearch(timerHandle, _handleComparison);
            _activeTimers.Insert(~index, timerHandle);
        }
    }
}