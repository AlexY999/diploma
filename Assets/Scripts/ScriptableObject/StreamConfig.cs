using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "StreamConfig", menuName = "Configuration/StreamConfig")]
public class StreamConfig : ScriptableObject
{
    [Tooltip("Enable to use the webcam, disable to use a video file.")]
    public bool useWebCam;

    [Tooltip("Video file to be used if not using webcam.")]
    public VideoClip videoFile;
}