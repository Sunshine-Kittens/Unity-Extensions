namespace UnityEngine.Extension
{
    public delegate void AnimationEvent(IAnimation animation);

    public interface IAnimation
    {
        public float Length { get; }

        public void Evaluate(float normalisedTime);
        public void Prepare();
    }
}
