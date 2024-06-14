using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PoseAnalyzerConfig", menuName = "Configuration/PoseAnalyzerConfig")]
public class PoseAnalyzerConfig : ScriptableObject
{
    [Header("Model Configuration")]
    public NNModel modelData;
    public WorkerFactory.Type workerType = WorkerFactory.Type.Auto;
    public bool debugMode = true;

    [Header("Processing Configuration")]
    public int targetImageSize;
    public int heatMapColumns;
    public bool enableSmoothing;
    public float smoothingFactor;

    [Header("Kalman Filter Settings")]
    public float kalmanQ;
    public float kalmanR;

    [Header("Visibility threshold")]
    public float visibilityThreshold = 0.3f;
}