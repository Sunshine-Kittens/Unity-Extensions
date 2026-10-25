using System.Collections.Generic;

namespace UnityEngine.Extension
{
    public delegate void AnimationAction(IAnimation animation);
    
    public interface IAnimation
    {
        public float Length { get; }
        public IReadOnlyList<AnimationEvent> Events { get; }
        
        public void Evaluate(float normalisedTime);
    }
}
