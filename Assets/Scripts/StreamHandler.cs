using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamHandler : MonoBehaviour
{
    public GameObject SourceTextureObject;
    public RawImage DisplayScreen;
    public GameObject DisplayBackground;
    public float BackgroundScaleModifier;
    public LayerMask DisplayLayer;
    public bool EnableWebCam = true;
    public int CameraIndex = 0;
    public VideoPlayer MediaPlayer;

    private WebCamTexture cameraTexture;
    private RenderTexture mediaTexture;

    private int screenWidth = 2560;
    private int backgroundWidth, backgroundHeight;

    public RenderTexture OutputTexture { get; private set; }

    /// <summary>
    /// Initialize Camera
    /// </summary>
    /// <param name="bgWidth"></param>
    /// <param name="bgHeight"></param>
    public void Init(int bgWidth, int bgHeight)
    {
        backgroundWidth = bgWidth;
        backgroundHeight = bgHeight;
        if (EnableWebCam) StartWebCamera();
        else StartVideo();
    }

    private void StartWebCamera()
    {
        var camDevices = WebCamTexture.devices;
        if (camDevices.Length <= CameraIndex) CameraIndex = 0;

        cameraTexture = new WebCamTexture(camDevices[CameraIndex].name);

        var displayRect = DisplayScreen.GetComponent<RectTransform>();
        DisplayScreen.texture = cameraTexture;

        cameraTexture.Play();

        displayRect.sizeDelta = new Vector2(screenWidth, screenWidth * cameraTexture.height / cameraTexture.width);
        var aspectRatio = (float)cameraTexture.width / cameraTexture.height;
        DisplayBackground.transform.localScale = new Vector3(aspectRatio, 1, 1) * BackgroundScaleModifier;
        DisplayBackground.GetComponent<Renderer>().material.mainTexture = cameraTexture;

        SetupOutputTexture();
    }

    private void StartVideo()
    {
        mediaTexture = new RenderTexture((int)MediaPlayer.clip.width, (int)MediaPlayer.clip.height, 24);

        MediaPlayer.renderMode = VideoRenderMode.RenderTexture;
        MediaPlayer.targetTexture = mediaTexture;

        var displayRect = DisplayScreen.GetComponent<RectTransform>();
        displayRect.sizeDelta =
            new Vector2(screenWidth, (int)(screenWidth * MediaPlayer.clip.height / MediaPlayer.clip.width));
        DisplayScreen.texture = mediaTexture;

        MediaPlayer.Play();

        var aspectRatio = (float)mediaTexture.width / mediaTexture.height;

        DisplayBackground.transform.localScale = new Vector3(aspectRatio, 1, 1) * BackgroundScaleModifier;
        DisplayBackground.GetComponent<Renderer>().material.mainTexture = mediaTexture;

        SetupOutputTexture();
    }


    /// <summary>
    /// Initialize Main Texture
    /// </summary>
    private void SetupOutputTexture()
    {
        var go = new GameObject("OutputTextureCamera", typeof(Camera));

        go.transform.parent = DisplayBackground.transform;
        go.transform.localScale = Vector3.one * -1;
        go.transform.localPosition = new Vector3(0.0f, 0.0f, -2.0f);
        go.transform.localEulerAngles = Vector3.zero;
        go.layer = DisplayLayer;

        var cam = go.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 0.75f;
        cam.depth = -5;
        cam.depthTextureMode = DepthTextureMode.None;
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.black;
        cam.cullingMask = DisplayLayer;
        cam.useOcclusionCulling = false;
        cam.nearClipPlane = 1.0f;
        cam.farClipPlane = 5.0f;
        cam.allowMSAA = false;
        cam.allowHDR = false;

        OutputTexture = new RenderTexture(backgroundWidth, backgroundHeight, 0, RenderTextureFormat.RGB565,
            RenderTextureReadWrite.sRGB)
        {
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        cam.targetTexture = OutputTexture;
        if (SourceTextureObject.activeSelf)
            SourceTextureObject.GetComponent<Renderer>().material.mainTexture = OutputTexture;
    }
}