using System;

namespace UnityEngine.Extension
{
    public abstract class AnimationEvent
    {
        public readonly float Time;

        private AnimationEvent() { }

        protected AnimationEvent(float time)
        {
            Time = time;
        }

        public abstract void Invoke();
    }

    public sealed class AnimationEventVoid : AnimationEvent
    {
        private readonly Action _eventAction;
        
        public AnimationEventVoid(float time, Action eventAction) : base(time)
        {
            _eventAction = eventAction ?? throw new ArgumentNullException(nameof(eventAction));;
        }
        
        public override void Invoke()
        {
            _eventAction.Invoke();
        }
    }
    
    public abstract class AnimationEventT<T> : AnimationEvent
    {
        private readonly Func<T> _paramFunc;
        private readonly Action<T> _eventAction;
        
        protected AnimationEventT(float time, Func<T> paramFunc, Action<T> eventAction) : base(time)
        {
            _paramFunc = paramFunc ?? throw new ArgumentNullException(nameof(paramFunc));
            _eventAction = eventAction ?? throw new ArgumentNullException(nameof(eventAction));;
        }
        
        public override void Invoke()
        {
            _eventAction.Invoke(_paramFunc.Invoke());
        }
    }
    
    public sealed class AnimationEventFloat : AnimationEventT<float>
    {
        public AnimationEventFloat(float time, Func<float> paramFunc, Action<float> eventAction) : base(time, paramFunc, eventAction) { }
    } 
    
    public sealed class AnimationEventInt : AnimationEventT<int>
    {
        public AnimationEventInt(float time, Func<int> paramFunc, Action<int> eventAction) : base(time, paramFunc, eventAction) { }
    }
    
    public sealed class AnimationEventString : AnimationEventT<string>
    {
        public AnimationEventString(float time, Func<string> paramFunc, Action<string> eventAction) : base(time, paramFunc, eventAction) { }
    }
    
    public sealed class AnimationEventObj : AnimationEventT<object>
    {
        public AnimationEventObj(float time, Func<object> paramFunc, Action<object> eventAction) : base(time, paramFunc, eventAction) { }
    }

	public sealed class AnimationEventUnityObj : AnimationEventT<UnityEngine.Object>
    {
        public AnimationEventUnityObj(float time, Func<UnityEngine.Object> paramFunc, Action<UnityEngine.Object> eventAction) : base(time, paramFunc, eventAction) { }
    }
}
