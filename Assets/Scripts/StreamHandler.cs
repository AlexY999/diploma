using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamHandler : MonoBehaviour
{
    [Header("Configuration")]
    public StreamConfig streamConfig;
    public int cameraIndex = 0;

    [Header("Display Settings")]
    public GameObject sourceTextureObject;
    public RawImage displayScreen;
    public GameObject displayBackground;
    public float backgroundScaleModifier;
    public LayerMask displayLayer;

    [Header("Media Players")]
    public VideoPlayer mediaPlayer;

    [Header("Textures")]
    private WebCamTexture _cameraTexture;
    private RenderTexture _mediaTexture;

    [Header("Screen Configuration")]
    private const int ScreenWidth = 2560;
    private int _backgroundWidth, _backgroundHeight;

    [field: Header("Output")]
    public RenderTexture OutputTexture { get; private set; }

    /// <summary>
    /// Initialize Camera
    /// </summary>
    /// <param name="bgWidth"></param>
    /// <param name="bgHeight"></param>
    public void Init(int bgWidth, int bgHeight)
    {
        _backgroundWidth = bgWidth;
        _backgroundHeight = bgHeight;
        if (streamConfig.useWebCam) StartWebCamera();
        else StartVideo();
    }

    private void StartWebCamera()
    {
        var camDevices = WebCamTexture.devices;
        if (camDevices.Length <= cameraIndex) cameraIndex = 0;

        _cameraTexture = new WebCamTexture(camDevices[cameraIndex].name);

        var displayRect = displayScreen.GetComponent<RectTransform>();
        displayScreen.texture = _cameraTexture;

        _cameraTexture.Play();

        displayRect.sizeDelta = new Vector2(ScreenWidth, ScreenWidth * _cameraTexture.height / _cameraTexture.width);
        var aspectRatio = (float)_cameraTexture.width / _cameraTexture.height;
        displayBackground.transform.localScale = new Vector3(aspectRatio, 1, 1) * backgroundScaleModifier;
        displayBackground.GetComponent<Renderer>().material.mainTexture = _cameraTexture;

        SetupOutputTexture();
    }

    private void StartVideo()
    {
        _mediaTexture = new RenderTexture((int)mediaPlayer.clip.width, (int)mediaPlayer.clip.height, 24);

        mediaPlayer.renderMode = VideoRenderMode.RenderTexture;
        mediaPlayer.targetTexture = _mediaTexture;
        mediaPlayer.clip = streamConfig.videoFile;

        var displayRect = displayScreen.GetComponent<RectTransform>();
        displayRect.sizeDelta =
            new Vector2(ScreenWidth, (int)(ScreenWidth * mediaPlayer.clip.height / mediaPlayer.clip.width));
        displayScreen.texture = _mediaTexture;

        mediaPlayer.Play();

        var aspectRatio = (float)_mediaTexture.width / _mediaTexture.height;

        displayBackground.transform.localScale = new Vector3(aspectRatio, 1, 1) * backgroundScaleModifier;
        displayBackground.GetComponent<Renderer>().material.mainTexture = _mediaTexture;

        SetupOutputTexture();
    }
    
    private void SetupOutputTexture()
    {
        var go = new GameObject("OutputTextureCamera", typeof(Camera));

        go.transform.parent = displayBackground.transform;
        go.transform.localScale = Vector3.one * -1;
        go.transform.localPosition = new Vector3(0.0f, 0.0f, -2.0f);
        go.transform.localEulerAngles = Vector3.zero;
        go.layer = displayLayer;

        var cam = go.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 0.75f;
        cam.depth = -5;
        cam.depthTextureMode = DepthTextureMode.None;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.black;
        cam.cullingMask = displayLayer;
        cam.useOcclusionCulling = false;
        cam.nearClipPlane = 1.0f;
        cam.farClipPlane = 5.0f;
        cam.allowMSAA = false;
        cam.allowHDR = false;

        OutputTexture = new RenderTexture(_backgroundWidth, _backgroundHeight, 0, RenderTextureFormat.RGB565,
            RenderTextureReadWrite.sRGB)
        {
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        cam.targetTexture = OutputTexture;
        if (sourceTextureObject.activeSelf)
            sourceTextureObject.GetComponent<Renderer>().material.mainTexture = OutputTexture;
    }
}