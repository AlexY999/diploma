using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
	[SerializeField] [Range(30, 120)] private int maxFps = 30;
	float deltaTime = 0.0f;

    private void Start()
    {
	    QualitySettings.vSyncCount = 0;
	    Application.targetFrameRate = maxFps;
    }

    void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
	}

	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(12, 12, w, h * 2 / 50);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 100;
		style.normal.textColor = Color.red;
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
}
