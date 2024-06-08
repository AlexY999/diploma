using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Barracuda;
using Debug = UnityEngine.Debug;

/// <summary>
/// Define Joint points
/// </summary>
public class PoseAnalyzer : MonoBehaviour
{
    /// <summary>
    /// Neural network model
    /// </summary>
    public NNModel ModelData;

    public WorkerFactory.Type WorkerType = WorkerFactory.Type.Auto;
    public bool DebugMode = true;

    public VNectModel PoseModel;
    public StreamHandler VideoStreamHandler;

    private Model poseModel;
    private IWorker poseWorker;
    private VNectModel.JointPoint[] jointPositions;

    private const int TotalJoints = 24;
    
    public int TargetImageSize;
    private float HalfImageSize;


    public int HeatMapColumns;
    private float ImageSizeFloat;


    private int HeatMapColumnsSquared;
    private int HeatMapColumnsCubed;
    private float ImageResizeFactor;


    private float[] twoDHeatMap;
    private float[] twoDOffsets;
    private float[] threeDHeatMap;

    private float[] threeDOffsets;
    private float scaleUnit;
    

    private int DoubleJointCount = TotalJoints * 2;
    private int TripleJointCount = TotalJoints * 3;


    private int HeatMapJointProduct;
    private int LinearCubeOffset;
    private int SquaredCubeOffset;
    
    public float KalmanQ;
    public float KalmanR;
    
    private bool UpdateLock = true;

    public bool EnableSmoothing;
    public float SmoothingFactor;

    public Text StatusMessage;
    public float ModelLoadingDelay = 10f;
    private float LoadingCountdown = 0;
    public Texture2D PlaceholderImage;
    private float AveragePerformanceScore = 0.0f;
    private int UpdateCycleCount = 0;
    private float TotalProcessingTime = 0.0f; // Загальний час обробки усіх кадрів
    private int ProcessedFrames = 0; // Кількість оброблених кадрів
    private Stopwatch processingTimer = new Stopwatch(); // Таймер для вимірювання часу обробки

    private void Start()
    {
        HeatMapColumnsSquared = HeatMapColumns * HeatMapColumns;
        HeatMapColumnsCubed = HeatMapColumns * HeatMapColumns * HeatMapColumns;
        HeatMapJointProduct = HeatMapColumns * TotalJoints;
        LinearCubeOffset = HeatMapColumns * TripleJointCount;
        SquaredCubeOffset = HeatMapColumnsSquared * TripleJointCount;

        twoDHeatMap = new float[TotalJoints * HeatMapColumnsSquared];
        twoDOffsets = new float[TotalJoints * HeatMapColumnsSquared * 2];
        threeDHeatMap = new float[TotalJoints * HeatMapColumnsCubed];
        threeDOffsets = new float[TotalJoints * HeatMapColumnsCubed * 3];
        scaleUnit = 1f / (float)HeatMapColumns;
        ImageSizeFloat = TargetImageSize;
        HalfImageSize = ImageSizeFloat / 2f;
        ImageResizeFactor = TargetImageSize / (float)HeatMapColumns;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        poseModel = ModelLoader.Load(ModelData, DebugMode);
        poseWorker = WorkerFactory.CreateWorker(WorkerType, poseModel, DebugMode);

        StartCoroutine("WaitForModelLoad");
    }

    private void Update()
    {
        if (!UpdateLock)
        {
            processingTimer.Reset(); // Скидання таймера
            processingTimer.Start(); // Запуск таймера перед обробкою кадру

            UpdateVNectModel();
        
            float totalScore = 0.0f;

            for (int j = 0; j < TotalJoints; j++)
            {
                totalScore += jointPositions[j].score3D;
            }

            float averageScore = totalScore / TotalJoints; 
            AveragePerformanceScore += averageScore; 
            UpdateCycleCount++;

            processingTimer.Stop(); // Зупинка таймера після обробки кадру
            float frameTime = (float)processingTimer.Elapsed.TotalMilliseconds; // Час обробки одного кадру в мілісекундах
            TotalProcessingTime += frameTime; // Додавання часу кадру до загального часу
            ProcessedFrames++; // Збільшення лічильника кадрів

            // Debug.Log("Текущий средний score3D: " + averageScore);
            // Debug.Log("Час обробки кадру: " + frameTime + " ms");
        }

        if (UpdateCycleCount > 0)
        {
            float cumulativeAverageScore = AveragePerformanceScore / UpdateCycleCount;
            float averageFrameTime = TotalProcessingTime / ProcessedFrames; // Середній час обробки кадру
            // Debug.Log("Средний score3D за все время: " + cumulativeAverageScore);
            // Debug.Log("Середній час обробки кадру: " + averageFrameTime + " ms");
        }
    }
    
    private IEnumerator WaitForModelLoad()
    {
        inputs[inputName_1] = new Tensor(PlaceholderImage);
        inputs[inputName_2] = new Tensor(PlaceholderImage);
        inputs[inputName_3] = new Tensor(PlaceholderImage);

        // Create input and Execute model
        yield return poseWorker.StartManualSchedule(inputs);

        // Get outputs
        for (var i = 2; i < poseModel.outputs.Count; i++)
        {
            b_outputs[i] = poseWorker.PeekOutput(poseModel.outputs[i]);
        }

        // Get data from outputs
        threeDOffsets = b_outputs[2].data.Download(b_outputs[2].shape);
        threeDHeatMap = b_outputs[3].data.Download(b_outputs[3].shape);

        // Release outputs
        for (var i = 2; i < b_outputs.Length; i++)
        {
            b_outputs[i].Dispose();
        }

        // Init VNect model
        jointPositions = PoseModel.Init();

        PredictPose();

        yield return new WaitForSeconds(ModelLoadingDelay);

        // Init VideoCapture
        VideoStreamHandler.Init(TargetImageSize, TargetImageSize);
        UpdateLock = false;
        StatusMessage.gameObject.SetActive(false);
    }

    private const string inputName_1 = "input.1";
    private const string inputName_2 = "input.4";
    private const string inputName_3 = "input.7";
    /*
    private const string inputName_1 = "0";
    private const string inputName_2 = "1";
    private const string inputName_3 = "2";
    */

    private void UpdateVNectModel()
    {
        input = new Tensor(VideoStreamHandler.OutputTexture);
        if (inputs[inputName_1] == null)
        {
            inputs[inputName_1] = input;
            inputs[inputName_2] = new Tensor(VideoStreamHandler.OutputTexture);
            inputs[inputName_3] = new Tensor(VideoStreamHandler.OutputTexture);
        }
        else
        {
            inputs[inputName_3].Dispose();

            inputs[inputName_3] = inputs[inputName_2];
            inputs[inputName_2] = inputs[inputName_1];
            inputs[inputName_1] = input;
        }

        StartCoroutine(ExecuteModelAsync());
    }

    /// <summary>
    /// Tensor has input image
    /// </summary>
    /// <returns></returns>
    Tensor input = new Tensor();
    Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>() { { inputName_1, null }, { inputName_2, null }, { inputName_3, null }, };
    Tensor[] b_outputs = new Tensor[4];

    private IEnumerator ExecuteModelAsync()
    {
        // Create input and Execute model
        yield return poseWorker.StartManualSchedule(inputs);

        // Get outputs
        for (var i = 2; i < poseModel.outputs.Count; i++)
        {
            b_outputs[i] = poseWorker.PeekOutput(poseModel.outputs[i]);
        }

        // Get data from outputs
        threeDOffsets = b_outputs[2].data.Download(b_outputs[2].shape);
        threeDHeatMap = b_outputs[3].data.Download(b_outputs[3].shape);
        
        // Release outputs
        for (var i = 2; i < b_outputs.Length; i++)
        {
            b_outputs[i].Dispose();
        }

        PredictPose();
    }

    /// <summary>
    /// Predict positions of each of joints based on network
    /// </summary>
    private void PredictPose()
    {
        for (var j = 0; j < TotalJoints; j++)
        {
            var maxXIndex = 0;
            var maxYIndex = 0;
            var maxZIndex = 0;
            jointPositions[j].score3D = 0.0f;
            var jj = j * HeatMapColumns;
            for (var z = 0; z < HeatMapColumns; z++)
            {
                var zz = jj + z;
                for (var y = 0; y < HeatMapColumns; y++)
                {
                    var yy = y * HeatMapColumnsSquared * TotalJoints + zz;
                    for (var x = 0; x < HeatMapColumns; x++)
                    {
                        float v = threeDHeatMap[yy + x * HeatMapJointProduct];
                        if (v > jointPositions[j].score3D)
                        {
                            jointPositions[j].score3D = v;
                            maxXIndex = x;
                            maxYIndex = y;
                            maxZIndex = z;
                        }
                    }
                }
            }
           
            jointPositions[j].Now3D.x = (threeDOffsets[maxYIndex * SquaredCubeOffset + maxXIndex * LinearCubeOffset + j * HeatMapColumns + maxZIndex] + 0.5f + (float)maxXIndex) * ImageResizeFactor - HalfImageSize;
            jointPositions[j].Now3D.y = HalfImageSize - (threeDOffsets[maxYIndex * SquaredCubeOffset + maxXIndex * LinearCubeOffset + (j + TotalJoints) * HeatMapColumns + maxZIndex] + 0.5f + (float)maxYIndex) * ImageResizeFactor;
            jointPositions[j].Now3D.z = (threeDOffsets[maxYIndex * SquaredCubeOffset + maxXIndex * LinearCubeOffset + (j + DoubleJointCount) * HeatMapColumns + maxZIndex] + 0.5f + (float)(maxZIndex - 14)) * ImageResizeFactor;
        }

        // Calculate hip location
        var lc = (jointPositions[PositionIndex.rThighBend.Int()].Now3D + jointPositions[PositionIndex.lThighBend.Int()].Now3D) / 2f;
        jointPositions[PositionIndex.hip.Int()].Now3D = (jointPositions[PositionIndex.abdomenUpper.Int()].Now3D + lc) / 2f;

        // Calculate neck location
        jointPositions[PositionIndex.neck.Int()].Now3D = (jointPositions[PositionIndex.rShldrBend.Int()].Now3D + jointPositions[PositionIndex.lShldrBend.Int()].Now3D) / 2f;

        // Calculate head location
        var cEar = (jointPositions[PositionIndex.rEar.Int()].Now3D + jointPositions[PositionIndex.lEar.Int()].Now3D) / 2f;
        var hv = cEar - jointPositions[PositionIndex.neck.Int()].Now3D;
        var nhv = Vector3.Normalize(hv);
        var nv = jointPositions[PositionIndex.Nose.Int()].Now3D - jointPositions[PositionIndex.neck.Int()].Now3D;
        jointPositions[PositionIndex.head.Int()].Now3D = jointPositions[PositionIndex.neck.Int()].Now3D + nhv * Vector3.Dot(nhv, nv);

        // Calculate spine location
        jointPositions[PositionIndex.spine.Int()].Now3D = jointPositions[PositionIndex.abdomenUpper.Int()].Now3D;

        // Kalman filter
        foreach (var jp in jointPositions)
        {
            KalmanUpdate(jp);
        }

        // Low pass filter
        if (EnableSmoothing)
        {
            foreach (var jp in jointPositions)
            {
                jp.PrevPos3D[0] = jp.Pos3D;
                for (var i = 1; i < jp.PrevPos3D.Length; i++)
                {
                    jp.PrevPos3D[i] = jp.PrevPos3D[i] * SmoothingFactor + jp.PrevPos3D[i - 1] * (1f - SmoothingFactor);
                }
                jp.Pos3D = jp.PrevPos3D[jp.PrevPos3D.Length - 1];
            }
        }
    }

    /// <summary>
    /// Kalman filter
    /// </summary>
    /// <param name="measurement">joint points</param>
    void KalmanUpdate(VNectModel.JointPoint measurement)
    {
        measurementUpdate(measurement);
        measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;
        measurement.X = measurement.Pos3D;
    }

	void measurementUpdate(VNectModel.JointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + KalmanQ) / (measurement.P.x + KalmanQ + KalmanR);
        measurement.K.y = (measurement.P.y + KalmanQ) / (measurement.P.y + KalmanQ + KalmanR);
        measurement.K.z = (measurement.P.z + KalmanQ) / (measurement.P.z + KalmanQ + KalmanR);
        measurement.P.x = KalmanR * (measurement.P.x + KalmanQ) / (KalmanR + measurement.P.x + KalmanQ);
        measurement.P.y = KalmanR * (measurement.P.y + KalmanQ) / (KalmanR + measurement.P.y + KalmanQ);
        measurement.P.z = KalmanR * (measurement.P.z + KalmanQ) / (KalmanR + measurement.P.z + KalmanQ);
    }
}
