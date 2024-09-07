namespace UnityEngine.Extension
{
    public static class PlaybackModeExtensions
    {
        public static PlaybackMode Invert(this PlaybackMode playbackMode)
        {
            return playbackMode ^ (PlaybackMode)1;
        }
    }
}