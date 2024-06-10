using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointPoint
{
    public Vector2 Position2D = new Vector2();
    public float Confidence2D;

    public Vector3 Position3D = new Vector3();
    public Vector3 CurrentPosition3D = new Vector3();
    public Vector3[] HistoricalPositions3D = new Vector3[6];
    public float Confidence3D;

    // Bones
    public Transform BoneTransform = null;
    public Quaternion InitialRotation;
    public Quaternion RotationInverse;
    public Quaternion ComputedRotation;

    public JointPoint ChildJoint = null;
    public JointPoint ParentJoint = null;

    // For Kalman filter
    public Vector3 PredictionError = new Vector3();
    public Vector3 EstimatedState = new Vector3();
    public Vector3 KalmanGain = new Vector3();
}