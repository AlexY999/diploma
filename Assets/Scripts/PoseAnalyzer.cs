using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Barracuda;
using Debug = UnityEngine.Debug;

public class PoseAnalyzer : MonoBehaviour
{
    [Header("Configuration")]
    public PoseAnalyzerConfig poseAnalyzerConfig;

    [Header("Model Components")]
    public PoseModel poseModel;
    public StreamHandler videoStreamHandler;

    [Header("Internal Model Details")]
    private Model _poseModel;
    private IWorker _poseWorker;
    private JointPoint[] _jointPositions;

    [Header("Pose Processing")]
    private const int TotalJoints = 24;
    private float _halfImageSize;
    private float _imageSizeFloat;
    private int _heatMapColumnsSquared;
    private int _heatMapColumnsCubed;
    private float _imageResizeFactor;
    private float[] _twoDHeatMap;
    private float[] _twoDOffsets;
    private float[] _threeDHeatMap;
    private float[] _threeDOffsets;
    private float _scaleUnit;
    private const int DoubleJointCount = TotalJoints * 2;
    private const int TripleJointCount = TotalJoints * 3;
    private int _heatMapJointProduct;
    private int _linearCubeOffset;
    private int _squaredCubeOffset;

    [Header("UI Elements")]
    public Text statusMessage;
    public Texture2D placeholderImage;
    public RawImage leftWarning;
    public RawImage rightWarning;
    public RawImage topWarning;
    public RawImage bottomWarning;
    
    [Header("UI Animation")]
    private float _targetAlphaTop;
    private float _targetAlphaBottom;
    private float _targetAlphaLeft;
    private float _targetAlphaRight;
    private const float FadeDuration = 0.2f;

    [Header("Performance Metrics")]
    public float modelLoadingDelay = 0.1f;
    private float _averagePerformanceScore = 0.0f;
    private int _updateCycleCount = 0;
    private float _totalProcessingTime = 0.0f; // Загальний час обробки усіх кадрів
    private int _processedFrames = 0; // Кількість оброблених кадрів
    private readonly Stopwatch _processingTimer = new(); // Таймер для вимірювання часу обробки
    private float _averageConfidence3D = 0f;

    [Header("System Control")]
    private bool _updateLock = true;

    private void Start()
    {
        _heatMapColumnsSquared = poseAnalyzerConfig.heatMapColumns * poseAnalyzerConfig.heatMapColumns;
        _heatMapColumnsCubed = poseAnalyzerConfig.heatMapColumns * poseAnalyzerConfig.heatMapColumns * poseAnalyzerConfig.heatMapColumns;
        _heatMapJointProduct = poseAnalyzerConfig.heatMapColumns * TotalJoints;
        _linearCubeOffset = poseAnalyzerConfig.heatMapColumns * TripleJointCount;
        _squaredCubeOffset = _heatMapColumnsSquared * TripleJointCount;

        _twoDHeatMap = new float[TotalJoints * _heatMapColumnsSquared];
        _twoDOffsets = new float[TotalJoints * _heatMapColumnsSquared * 2];
        _threeDHeatMap = new float[TotalJoints * _heatMapColumnsCubed];
        _threeDOffsets = new float[TotalJoints * _heatMapColumnsCubed * 3];
        _scaleUnit = 1f / (float)poseAnalyzerConfig.heatMapColumns;
        _imageSizeFloat = poseAnalyzerConfig.targetImageSize;
        _halfImageSize = _imageSizeFloat / 2f;
        _imageResizeFactor = poseAnalyzerConfig.targetImageSize / (float)poseAnalyzerConfig.heatMapColumns;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        _poseModel = ModelLoader.Load(poseAnalyzerConfig.modelData, poseAnalyzerConfig.debugMode);
        _poseWorker = WorkerFactory.CreateWorker(poseAnalyzerConfig.workerType, _poseModel, poseAnalyzerConfig.debugMode);

        StartCoroutine("WaitForModelLoad");
    }

    private void Update()
    {
        if (!_updateLock)
        {
            _processingTimer.Reset(); // Скидання таймера
            _processingTimer.Start(); // Запуск таймера перед обробкою кадру

            UpdateModel();
            UpdateVisibilityWarnings();
            
            var totalScore = 0.0f;

            for (var j = 0; j < TotalJoints; j++) totalScore += _jointPositions[j].Confidence3D;
            
            var averageScore = totalScore / TotalJoints;
            _averageConfidence3D = averageScore;
            _averagePerformanceScore += averageScore;
            _updateCycleCount++;

            _processingTimer.Stop(); // Зупинка таймера після обробки кадру
            var frameTime =
                (float)_processingTimer.Elapsed.TotalMilliseconds; // Час обробки одного кадру в мілісекундах
            _totalProcessingTime += frameTime; // Додавання часу кадру до загального часу
            _processedFrames++; // Збільшення лічильника кадрів

            // Debug.Log("Текущий средний score3D: " + averageScore);
            // Debug.Log("Час обробки кадру: " + frameTime + " ms");
        }

        if (_updateCycleCount > 0)
        {
            var cumulativeAverageScore = _averagePerformanceScore / _updateCycleCount;
            var averageFrameTime = _totalProcessingTime / _processedFrames; // Середній час обробки кадру
            // Debug.Log("Средний score3D за все время: " + cumulativeAverageScore);
            // Debug.Log("Середній час обробки кадру: " + averageFrameTime + " ms");
        }
    }

    private IEnumerator WaitForModelLoad()
    {
        inputs[inputName_1] = new Tensor(placeholderImage);
        inputs[inputName_2] = new Tensor(placeholderImage);
        inputs[inputName_3] = new Tensor(placeholderImage);

        // Create input and Execute model
        yield return _poseWorker.StartManualSchedule(inputs);

        // Get outputs
        for (var i = 2; i < _poseModel.outputs.Count; i++) b_outputs[i] = _poseWorker.PeekOutput(_poseModel.outputs[i]);

        // Get data from outputs
        _threeDOffsets = b_outputs[2].data.Download(b_outputs[2].shape);
        _threeDHeatMap = b_outputs[3].data.Download(b_outputs[3].shape);

        // Release outputs
        for (var i = 2; i < b_outputs.Length; i++) b_outputs[i].Dispose();

        // Init model
        _jointPositions = poseModel.Init();

        PredictPose();

        yield return new WaitForSeconds(modelLoadingDelay);

        // Init VideoCapture
        videoStreamHandler.Init(poseAnalyzerConfig.targetImageSize, poseAnalyzerConfig.targetImageSize);
        _updateLock = false;
        statusMessage.gameObject.SetActive(false);
    }

    private const string inputName_1 = "input.1";
    private const string inputName_2 = "input.4";

    private const string inputName_3 = "input.7";
    /*
    private const string inputName_1 = "0";
    private const string inputName_2 = "1";
    private const string inputName_3 = "2";
    */

    private void UpdateModel()
    {
        input = new Tensor(videoStreamHandler.OutputTexture);
        if (inputs[inputName_1] == null)
        {
            inputs[inputName_1] = input;
            inputs[inputName_2] = new Tensor(videoStreamHandler.OutputTexture);
            inputs[inputName_3] = new Tensor(videoStreamHandler.OutputTexture);
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

    private Tensor input = new();

    private Dictionary<string, Tensor> inputs = new()
        { { inputName_1, null }, { inputName_2, null }, { inputName_3, null } };

    private Tensor[] b_outputs = new Tensor[4];

    private IEnumerator ExecuteModelAsync()
    {
        // Create input and Execute model
        yield return _poseWorker.StartManualSchedule(inputs);

        // Get outputs
        for (var i = 2; i < _poseModel.outputs.Count; i++) b_outputs[i] = _poseWorker.PeekOutput(_poseModel.outputs[i]);

        // Get data from outputs
        _threeDOffsets = b_outputs[2].data.Download(b_outputs[2].shape);
        _threeDHeatMap = b_outputs[3].data.Download(b_outputs[3].shape);

        // Release outputs
        for (var i = 2; i < b_outputs.Length; i++) b_outputs[i].Dispose();

        PredictPose();
    }

    private void PredictPose()
    {
        CalculateJointPositions();
        CalculateSpecialJointPositions();
        ApplyKalmanFilter();
        ApplySmoothingFilter();
    }

    private void CalculateJointPositions()
    {
        for (var j = 0; j < TotalJoints; j++)
        {
            var maxXIndex = 0;
            var maxYIndex = 0;
            var maxZIndex = 0;
            _jointPositions[j].Confidence3D = 0.0f;
            var jj = j * poseAnalyzerConfig.heatMapColumns;
            for (var z = 0; z < poseAnalyzerConfig.heatMapColumns; z++)
            {
                var zz = jj + z;
                for (var y = 0; y < poseAnalyzerConfig.heatMapColumns; y++)
                {
                    var yy = y * _heatMapColumnsSquared * TotalJoints + zz;
                    for (var x = 0; x < poseAnalyzerConfig.heatMapColumns; x++)
                    {
                        var v = _threeDHeatMap[yy + x * _heatMapJointProduct];
                        if (v > _jointPositions[j].Confidence3D)
                        {
                            _jointPositions[j].Confidence3D = v;
                            maxXIndex = x;
                            maxYIndex = y;
                            maxZIndex = z;
                        }
                    }
                }
            }

            _jointPositions[j].CurrentPosition3D.x = (_threeDOffsets[maxYIndex * _squaredCubeOffset + maxXIndex * _linearCubeOffset + j * poseAnalyzerConfig.heatMapColumns + maxZIndex] + 0.5f + (float)maxXIndex) * _imageResizeFactor - _halfImageSize;
            _jointPositions[j].CurrentPosition3D.y = _halfImageSize - (_threeDOffsets[maxYIndex * _squaredCubeOffset + maxXIndex * _linearCubeOffset + (j + TotalJoints) * poseAnalyzerConfig.heatMapColumns + maxZIndex] + 0.5f + (float)maxYIndex) * _imageResizeFactor;
            _jointPositions[j].CurrentPosition3D.z = (_threeDOffsets[maxYIndex * _squaredCubeOffset + maxXIndex * _linearCubeOffset + (j + DoubleJointCount) * poseAnalyzerConfig.heatMapColumns + maxZIndex] + 0.5f + (float)(maxZIndex - 14)) * _imageResizeFactor;
        }
    }

    private void CalculateSpecialJointPositions()
    {
        var lc = (_jointPositions[BodyJoint.rightUpperLeg.Int()].CurrentPosition3D + _jointPositions[BodyJoint.leftUpperLeg.Int()].CurrentPosition3D) / 2f;
        _jointPositions[BodyJoint.centerHip.Int()].CurrentPosition3D = (_jointPositions[BodyJoint.upperAbdomen.Int()].CurrentPosition3D + lc) / 2f;

        _jointPositions[BodyJoint.centralNeck.Int()].CurrentPosition3D = (_jointPositions[BodyJoint.rightShoulder.Int()].CurrentPosition3D + _jointPositions[BodyJoint.leftShoulder.Int()].CurrentPosition3D) / 2f;

        var cEar = (_jointPositions[BodyJoint.rightEar.Int()].CurrentPosition3D + _jointPositions[BodyJoint.leftEar.Int()].CurrentPosition3D) / 2f;
        var hv = cEar - _jointPositions[BodyJoint.centralNeck.Int()].CurrentPosition3D;
        var nhv = Vector3.Normalize(hv);
        var nv = _jointPositions[BodyJoint.centralNose.Int()].CurrentPosition3D - _jointPositions[BodyJoint.centralNeck.Int()].CurrentPosition3D;
        _jointPositions[BodyJoint.topHead.Int()].CurrentPosition3D = _jointPositions[BodyJoint.centralNeck.Int()].CurrentPosition3D + nhv * Vector3.Dot(nhv, nv);

        _jointPositions[BodyJoint.middleSpine.Int()].CurrentPosition3D = _jointPositions[BodyJoint.upperAbdomen.Int()].CurrentPosition3D;
    }

    private void ApplyKalmanFilter()
    {
        foreach (var jp in _jointPositions) KalmanUpdate(jp);
    }

    private void ApplySmoothingFilter()
    {
        if (poseAnalyzerConfig.enableSmoothing)
            foreach (var jp in _jointPositions)
            {
                jp.HistoricalPositions3D[0] = jp.Position3D;
                for (var i = 1; i < jp.HistoricalPositions3D.Length; i++)
                    jp.HistoricalPositions3D[i] = jp.HistoricalPositions3D[i] * poseAnalyzerConfig.smoothingFactor + jp.HistoricalPositions3D[i - 1] * (1f - poseAnalyzerConfig.smoothingFactor);
                jp.Position3D = jp.HistoricalPositions3D[jp.HistoricalPositions3D.Length - 1];
            }
    }

    private void KalmanUpdate(JointPoint measurement)
    {
        MeasurementUpdate(measurement);
        measurement.Position3D.x = measurement.EstimatedState.x + (measurement.CurrentPosition3D.x - measurement.EstimatedState.x) * measurement.KalmanGain.x;
        measurement.Position3D.y = measurement.EstimatedState.y + (measurement.CurrentPosition3D.y - measurement.EstimatedState.y) * measurement.KalmanGain.y;
        measurement.Position3D.z = measurement.EstimatedState.z + (measurement.CurrentPosition3D.z - measurement.EstimatedState.z) * measurement.KalmanGain.z;
        measurement.EstimatedState = measurement.Position3D;
    }

    private void MeasurementUpdate(JointPoint measurement)
    {
        measurement.KalmanGain.x = (measurement.PredictionError.x + poseAnalyzerConfig.kalmanQ) / (measurement.PredictionError.x + poseAnalyzerConfig.kalmanQ + poseAnalyzerConfig.kalmanR);
        measurement.KalmanGain.y = (measurement.PredictionError.y + poseAnalyzerConfig.kalmanQ) / (measurement.PredictionError.y + poseAnalyzerConfig.kalmanQ + poseAnalyzerConfig.kalmanR);
        measurement.KalmanGain.z = (measurement.PredictionError.z + poseAnalyzerConfig.kalmanQ) / (measurement.PredictionError.z + poseAnalyzerConfig.kalmanQ + poseAnalyzerConfig.kalmanR);
        measurement.PredictionError.x = poseAnalyzerConfig.kalmanR * (measurement.PredictionError.x + poseAnalyzerConfig.kalmanQ) / (poseAnalyzerConfig.kalmanR + measurement.PredictionError.x + poseAnalyzerConfig.kalmanQ);
        measurement.PredictionError.y = poseAnalyzerConfig.kalmanR * (measurement.PredictionError.y + poseAnalyzerConfig.kalmanQ) / (poseAnalyzerConfig.kalmanR + measurement.PredictionError.y + poseAnalyzerConfig.kalmanQ);
        measurement.PredictionError.z = poseAnalyzerConfig.kalmanR * (measurement.PredictionError.z + poseAnalyzerConfig.kalmanQ) / (poseAnalyzerConfig.kalmanR + measurement.PredictionError.z + poseAnalyzerConfig.kalmanQ);
    }
    
    public void UpdateVisibilityWarnings()
    {
        float upperBodyConfidence = CalculateAverageConfidence(
            BodyJoint.topHead, BodyJoint.centralNeck, BodyJoint.upperAbdomen,
            BodyJoint.rightShoulder, BodyJoint.leftShoulder);

        float lowerBodyConfidence = CalculateAverageConfidence(
            BodyJoint.rightUpperLeg, BodyJoint.leftUpperLeg,
            BodyJoint.rightLowerLeg, BodyJoint.leftLowerLeg,
            BodyJoint.rightFoot, BodyJoint.leftFoot,
            BodyJoint.rightToe, BodyJoint.leftToe);

        float leftSideConfidence = CalculateAverageConfidence(
            BodyJoint.leftShoulder, BodyJoint.leftElbow, BodyJoint.leftHand,
            BodyJoint.leftThumb, BodyJoint.leftFinger,
            BodyJoint.leftUpperLeg, BodyJoint.leftLowerLeg,
            BodyJoint.leftFoot, BodyJoint.leftToe);

        float rightSideConfidence = CalculateAverageConfidence(
            BodyJoint.rightShoulder, BodyJoint.rightElbow, BodyJoint.rightHand,
            BodyJoint.rightThumb, BodyJoint.rightFinger,
            BodyJoint.rightUpperLeg, BodyJoint.rightLowerLeg,
            BodyJoint.rightFoot, BodyJoint.rightToe);

        _targetAlphaTop = upperBodyConfidence > poseAnalyzerConfig.visibilityThreshold ? 0 : 1 - (upperBodyConfidence / poseAnalyzerConfig.visibilityThreshold);
        _targetAlphaBottom = lowerBodyConfidence > poseAnalyzerConfig.visibilityThreshold ? 0 : 1 - (lowerBodyConfidence / poseAnalyzerConfig.visibilityThreshold);
        _targetAlphaLeft = leftSideConfidence > poseAnalyzerConfig.visibilityThreshold ? 0 : 1 - (leftSideConfidence / poseAnalyzerConfig.visibilityThreshold);
        _targetAlphaRight = rightSideConfidence > poseAnalyzerConfig.visibilityThreshold ? 0 : 1 - (rightSideConfidence / poseAnalyzerConfig.visibilityThreshold);

        topWarning.color = new Color(topWarning.color.r, topWarning.color.g, topWarning.color.b, Mathf.Lerp(topWarning.color.a, _targetAlphaTop, Time.deltaTime / FadeDuration));
        bottomWarning.color = new Color(bottomWarning.color.r, bottomWarning.color.g, bottomWarning.color.b, Mathf.Lerp(bottomWarning.color.a, _targetAlphaBottom, Time.deltaTime / FadeDuration));
        leftWarning.color = new Color(leftWarning.color.r, leftWarning.color.g, leftWarning.color.b, Mathf.Lerp(leftWarning.color.a, _targetAlphaLeft, Time.deltaTime / FadeDuration));
        rightWarning.color = new Color(rightWarning.color.r, rightWarning.color.g, rightWarning.color.b, Mathf.Lerp(rightWarning.color.a, _targetAlphaRight, Time.deltaTime / FadeDuration));
    }

    private float CalculateAverageConfidence(params BodyJoint[] joints)
    {
        float sumConfidence = 0f;
        int count = 0;

        foreach (var joint in joints)
        {
            int index = joint.Int();
            if (index >= 0 && index < _jointPositions.Length)
            {
                sumConfidence += _jointPositions[index].Confidence3D;
                count++;
            }
        }

        return count > 0 ? sumConfidence / count : 0f;
    }
}