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
        public Dictionary<ulong, TimerData> timers = new Dictionary<ulong, TimerData>();

        public int Compare(TimerHandle lhs, TimerHandle rhs)
        {
            TimerData lhsData = timers[lhs.handle];
            TimerData rhsData = timers[rhs.handle];
            if (lhsData.expireTime < rhsData.expireTime)
            {
                return 1;
            }
            else if (lhsData.expireTime > rhsData.expireTime)
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
        public bool loop = false;
        public TimerStatus status = TimerStatus.Pending;
        public float rate = 0.0F;
        public double expireTime = 0.0;
        public bool unscaledTime = true;
        public TimerDelegate timerDelegate = null;
        public TimerHandle handle = new TimerHandle();
    }

    // Arbitrarily low execution order value, may need updating manually to confirm with project specifics
    [DefaultExecutionOrder(-999)]
    public class TimerManager : Singleton<TimerManager>
    {
        private static ulong lastAssignedHandle = 0;

        private Dictionary<ulong, TimerData> timers = new Dictionary<ulong, TimerData>();
        private List<TimerHandle> activeTimers = new List<TimerHandle>();
        private HashSet<TimerHandle> pendingTimers = new HashSet<TimerHandle>();
        private HashSet<TimerHandle> pausedTimers = new HashSet<TimerHandle>();

        private TimerHandleComparison handleComparison
        {
            get
            {
                if (_handleComparison == null)
                {
                    _handleComparison = new TimerHandleComparison();
                    _handleComparison.timers = timers;
                }
                return _handleComparison;
            }
        }

        protected override bool persists { get { return true; } }

        private TimerHandleComparison _handleComparison = null;

        private int lastUpdatedFrame = 0;
        private double internalTime = 0.0;
        private double internalUnscaledTime = 0.0;

        private TimerHandle currentlyExecutingTimer = new TimerHandle();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAllTimers();
        }

        private void Update()
        {
            if (!HasUpdatedThisFrame())
            {
                internalTime += Time.deltaTime;
                internalUnscaledTime += Time.unscaledDeltaTime;

                while (activeTimers.Count > 0)
                {
                    int topIndex = activeTimers.Count - 1;
                    TimerHandle topHandle = activeTimers[topIndex];
                    TimerData topData = GetTimer(in topHandle);
                    if (topData.status == TimerStatus.ActivePendingRemoval)
                    {
                        activeTimers.RemoveAt(topIndex);
                        RemoveTimer(in topHandle);
                        continue;
                    }

                    double internalTime = GetInternalTime(topData.unscaledTime);
                    if (internalTime > topData.expireTime)
                    {
                        // Remove timer from active list and store it during execution
                        currentlyExecutingTimer = topHandle;
                        activeTimers.RemoveAt(topIndex);
                        // Set status to executing
                        topData.status = TimerStatus.Executing;

                        // Determine how many times the timer may have elapsed (e.g. for large DeltaTime on a short looping timer)
                        int executionCount = topData.loop ? Mathf.FloorToInt((float)(internalTime - topData.expireTime) / topData.rate) + 1 : 1;
                        for (int i = 0; i < executionCount; i++)
                        {
                            topData.timerDelegate.Invoke();

                            // Check whether timer has been invalidated after execution
                            if (!currentlyExecutingTimer.IsValid() || topData.status != TimerStatus.Executing)
                            {
                                break;
                            }
                        }

                        if (topData.loop)
                        {
                            topData.expireTime += executionCount * topData.rate;
                            topData.status = TimerStatus.Active;
                            AddActiveTimer(topHandle);
                        }

                        currentlyExecutingTimer.Invalidate();
                    }
                    else
                    {
                        // No need to continue as there will be no timers to execute
                        break;
                    }
                }

                lastUpdatedFrame = Time.frameCount;

                if (pendingTimers.Count > 0)
                {
                    foreach (TimerHandle timerHandle in pendingTimers)
                    {
                        TimerData timerData = GetTimer(timerHandle);
                        timerData.expireTime += GetInternalTime(timerData.unscaledTime);
                        AddActiveTimer(timerHandle);
                    }
                    pendingTimers.Clear();
                }
            }
        }

        private bool HasUpdatedThisFrame()
        {
            return lastUpdatedFrame == Time.frameCount;
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
                newTimerData.timerDelegate = timerDelegate;
                newTimerData.rate = rate;
                newTimerData.loop = loop;
                newTimerData.unscaledTime = unscaledTime;

                double expireTime = initialDelay >= 0.0 ? initialDelay : rate;

                TimerHandle newHandle;
                if (HasUpdatedThisFrame())
                {
                    newTimerData.expireTime = GetInternalTime(unscaledTime) + expireTime;
                    newTimerData.status = TimerStatus.Active;
                    newHandle = AddTimer(newTimerData);
                    AddActiveTimer(newHandle);
                }
                else
                {
                    newTimerData.expireTime = expireTime;
                    newTimerData.status = TimerStatus.Pending;
                    newHandle = AddTimer(newTimerData);
                    pendingTimers.Add(newHandle);
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
            newTimerData.timerDelegate = timerDelegate;
            newTimerData.rate = 0.0F;
            newTimerData.loop = false;
            newTimerData.unscaledTime = true;
            newTimerData.expireTime = GetInternalTime(true);
            newTimerData.status = TimerStatus.Active;

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
                if (timerData.status != TimerStatus.Paused)
                {
                    TimerStatus previousStatus = timerData.status;
                    switch (previousStatus)
                    {
                        case TimerStatus.Pending:
                            pendingTimers.Remove(timerHandle);
                            break;
                        case TimerStatus.Active:
                            activeTimers.Remove(timerHandle);
                            break;
                        case TimerStatus.Executing:
                            currentlyExecutingTimer.Invalidate();
                            break;
                    }

                    if (previousStatus == TimerStatus.Executing && !timerData.loop)
                    {
                        RemoveTimer(timerHandle);
                    }
                    else
                    {
                        pausedTimers.Add(timerHandle);
                        timerData.status = TimerStatus.Paused;
                        if (previousStatus == TimerStatus.Pending)
                        {
                            timerData.expireTime -= GetInternalTime(timerData.unscaledTime);
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
                    timerData.expireTime += GetInternalTime(timerData.unscaledTime);
                    timerData.status = TimerStatus.Active;
                    AddActiveTimer(timerHandle);
                }
                else
                {
                    timerData.status = TimerStatus.Pending;
                    pendingTimers.Add(timerHandle);
                }
                pausedTimers.Remove(timerHandle);
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
            switch (timerData.status)
            {
                case TimerStatus.Pending:
                    RemoveTimer(timerData.handle);
                    break;
                case TimerStatus.Active:
                    timerData.status = TimerStatus.ActivePendingRemoval;
                    break;
                case TimerStatus.Paused:
                    pausedTimers.Remove(timerData.handle);
                    RemoveTimer(timerData.handle);
                    break;
                case TimerStatus.Executing:
                    currentlyExecutingTimer.Invalidate();
                    RemoveTimer(timerData.handle);
                    break;
                case TimerStatus.ActivePendingRemoval:
                    break;
            }
        }

        public void ClearAllTimers()
        {
            ulong[] keys = new ulong[timers.Count];
            timers.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                InternalClearTimer(timers[keys[i]]);
            }
        }

        private TimerHandle AddTimer(TimerData timerData)
        {
            TimerHandle newHandle = new TimerHandle();
            SetHandle(ref newHandle);
            timerData.handle = newHandle;
            timers.Add(newHandle.handle, timerData);
            return newHandle;
        }

        private void RemoveTimer(in TimerHandle timerHandle)
        {
            timers.Remove(timerHandle.handle);
        }

        private TimerData GetTimer(in TimerHandle timerHandle)
        {
            return timers[timerHandle.handle];
        }

        private bool FindTimer(in TimerHandle timerHandle, ref TimerData timerData)
        {
            if (timerHandle.IsValid())
            {
                if (timers.TryGetValue(timerHandle.handle, out timerData))
                {
                    if (timerData.status != TimerStatus.ActivePendingRemoval)
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
            lastAssignedHandle++;
            timerHandle.handle = lastAssignedHandle;
        }

        private double GetInternalTime(bool unscaled)
        {
            return unscaled ? internalUnscaledTime : internalTime;
        }

        private void AddActiveTimer(in TimerHandle timerHandle)
        {
            int index = activeTimers.BinarySearch(timerHandle, handleComparison);
            activeTimers.Insert(~index, timerHandle);
        }
    }
}