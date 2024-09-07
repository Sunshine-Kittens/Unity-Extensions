namespace UnityEngine.Extension
{
    public static class PlaybackModeExtensions
    {
        public static PlaybackMode InvertPlayMode(this PlaybackMode playbackMode)
        {
            return playbackMode ^ (PlaybackMode)1;
        }
    }
}