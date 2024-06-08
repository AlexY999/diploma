using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Position index of joint points
/// </summary>
public enum BodyJoint : int
{
    rightShoulder = 0,
    rightElbow,
    rightHand,
    rightThumb,
    rightFinger,

    leftShoulder,
    leftElbow,
    leftHand,
    leftThumb,
    leftFinger,

    leftEar,
    leftEye,
    rightEar,
    rightEye,
    centralNose,

    rightUpperLeg,
    rightLowerLeg,
    rightFoot,
    rightToe,

    leftUpperLeg,
    leftLowerLeg,
    leftFoot,
    leftToe,

    upperAbdomen,

    // Computed positions
    centerHip,
    topHead,
    centralNeck,
    middleSpine,

    totalCount,
    undefined,
}

public static class EnumExtend
{
    public static int Int(this BodyJoint joint)
    {
        return (int)joint;
    }
}

public class PoseModel : MonoBehaviour
{
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

    private class BoneConnection
    {
        public GameObject LineObject;
        public LineRenderer Line;

        public JointPoint StartJoint = null;
        public JointPoint EndJoint = null;
    }

    private readonly List<BoneConnection> _boneConnections = new List<BoneConnection>();
    public Material connectionMaterial;

    public bool showSkeleton;
    private bool _useSkeleton;
    public float skeletonOffsetX;
    public float skeletonOffsetY;
    public float skeletonOffsetZ;
    public float skeletonScale;

    // Joint position and bone
    private JointPoint[] _jointPoints;
    public JointPoint[] JointPoints { get { return _jointPoints; } }

    private Vector3 _initPosition; // Initial center position

    private Quaternion _initGazeRotation;
    private Quaternion _gazeRotationInverse;

    // UnityChan
    public GameObject avatarObject;
    public GameObject avatarNose;
    private Animator _avatarAnimator;

    // Move in z direction
    private const float BaseHeight = 224 * 0.75f;
    private float _currentHeight = 224 * 0.75f;
    private float _previousHeight = 224 * 0.75f;
    public float zVerticalScale = 0.8f;

    private void Update()
    {
        if (_jointPoints != null)
        {
            UpdatePose();
        }
    }

    /// <summary>
    /// Initialize joint points
    /// </summary>
    /// <returns></returns>
    public JointPoint[] Init()
    {
        _jointPoints = new JointPoint[BodyJoint.totalCount.Int()];
        for (var i = 0; i < BodyJoint.totalCount.Int(); i++) _jointPoints[i] = new JointPoint();

        _avatarAnimator = avatarObject.GetComponent<Animator>();

        // Right Arm
        _jointPoints[BodyJoint.rightShoulder.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _jointPoints[BodyJoint.rightElbow.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _jointPoints[BodyJoint.rightHand.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        _jointPoints[BodyJoint.rightThumb.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
        _jointPoints[BodyJoint.rightFinger.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
        // Left Arm
        _jointPoints[BodyJoint.leftShoulder.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _jointPoints[BodyJoint.leftElbow.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _jointPoints[BodyJoint.leftHand.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        _jointPoints[BodyJoint.leftThumb.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
        _jointPoints[BodyJoint.leftFinger.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);

        // Face
        _jointPoints[BodyJoint.leftEar.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
        _jointPoints[BodyJoint.leftEye.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftEye);
        _jointPoints[BodyJoint.rightEar.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
        _jointPoints[BodyJoint.rightEye.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightEye);
        _jointPoints[BodyJoint.centralNose.Int()].BoneTransform = avatarNose.transform;

        // Right Leg
        _jointPoints[BodyJoint.rightUpperLeg.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        _jointPoints[BodyJoint.rightLowerLeg.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        _jointPoints[BodyJoint.rightFoot.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
        _jointPoints[BodyJoint.rightToe.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightToes);

        // Left Leg
        _jointPoints[BodyJoint.leftUpperLeg.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        _jointPoints[BodyJoint.leftLowerLeg.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        _jointPoints[BodyJoint.leftFoot.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _jointPoints[BodyJoint.leftToe.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftToes);

        // etc
        _jointPoints[BodyJoint.upperAbdomen.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Spine);
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Hips);
        _jointPoints[BodyJoint.topHead.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
        _jointPoints[BodyJoint.centralNeck.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Neck);
        _jointPoints[BodyJoint.middleSpine.Int()].BoneTransform = _avatarAnimator.GetBoneTransform(HumanBodyBones.Spine);

        // Child Settings
        // Right Arm
        _jointPoints[BodyJoint.rightShoulder.Int()].ChildJoint = _jointPoints[BodyJoint.rightElbow.Int()];
        _jointPoints[BodyJoint.rightElbow.Int()].ChildJoint = _jointPoints[BodyJoint.rightHand.Int()];
        _jointPoints[BodyJoint.rightElbow.Int()].ParentJoint = _jointPoints[BodyJoint.rightShoulder.Int()];

        // Left Arm
        _jointPoints[BodyJoint.leftShoulder.Int()].ChildJoint = _jointPoints[BodyJoint.leftElbow.Int()];
        _jointPoints[BodyJoint.leftElbow.Int()].ChildJoint = _jointPoints[BodyJoint.leftHand.Int()];
        _jointPoints[BodyJoint.leftElbow.Int()].ParentJoint = _jointPoints[BodyJoint.leftShoulder.Int()];

        // Fase

        // Right Leg
        _jointPoints[BodyJoint.rightUpperLeg.Int()].ChildJoint = _jointPoints[BodyJoint.rightLowerLeg.Int()];
        _jointPoints[BodyJoint.rightLowerLeg.Int()].ChildJoint = _jointPoints[BodyJoint.rightFoot.Int()];
        _jointPoints[BodyJoint.rightFoot.Int()].ChildJoint = _jointPoints[BodyJoint.rightToe.Int()];
        _jointPoints[BodyJoint.rightFoot.Int()].ParentJoint = _jointPoints[BodyJoint.rightLowerLeg.Int()];

        // Left Leg
        _jointPoints[BodyJoint.leftUpperLeg.Int()].ChildJoint = _jointPoints[BodyJoint.leftLowerLeg.Int()];
        _jointPoints[BodyJoint.leftLowerLeg.Int()].ChildJoint = _jointPoints[BodyJoint.leftFoot.Int()];
        _jointPoints[BodyJoint.leftFoot.Int()].ChildJoint = _jointPoints[BodyJoint.leftToe.Int()];
        _jointPoints[BodyJoint.leftFoot.Int()].ParentJoint = _jointPoints[BodyJoint.leftLowerLeg.Int()];

        // etc
        _jointPoints[BodyJoint.middleSpine.Int()].ChildJoint = _jointPoints[BodyJoint.centralNeck.Int()];
        _jointPoints[BodyJoint.centralNeck.Int()].ChildJoint = _jointPoints[BodyJoint.topHead.Int()];
        //jointPoints[PositionIndex.head.Int()].Child = jointPoints[PositionIndex.Nose.Int()];

        _useSkeleton = showSkeleton;
        if (_useSkeleton)
        {
            // Line Child Settings
            // Right Arm
            AddSkeleton(BodyJoint.rightShoulder, BodyJoint.rightElbow);
            AddSkeleton(BodyJoint.rightElbow, BodyJoint.rightHand);
            AddSkeleton(BodyJoint.rightHand, BodyJoint.rightThumb);
            AddSkeleton(BodyJoint.rightHand, BodyJoint.rightFinger);

            // Left Arm
            AddSkeleton(BodyJoint.leftShoulder, BodyJoint.leftElbow);
            AddSkeleton(BodyJoint.leftElbow, BodyJoint.leftHand);
            AddSkeleton(BodyJoint.leftHand, BodyJoint.leftThumb);
            AddSkeleton(BodyJoint.leftHand, BodyJoint.leftFinger);

            // Fase
            AddSkeleton(BodyJoint.leftEar, BodyJoint.centralNose);
            AddSkeleton(BodyJoint.rightEar, BodyJoint.centralNose);

            // Right Leg
            AddSkeleton(BodyJoint.rightUpperLeg, BodyJoint.rightLowerLeg);
            AddSkeleton(BodyJoint.rightLowerLeg, BodyJoint.rightFoot);
            AddSkeleton(BodyJoint.rightFoot, BodyJoint.rightToe);

            // Left Leg
            AddSkeleton(BodyJoint.leftUpperLeg, BodyJoint.leftLowerLeg);
            AddSkeleton(BodyJoint.leftLowerLeg, BodyJoint.leftFoot);
            AddSkeleton(BodyJoint.leftFoot, BodyJoint.leftToe);

            // etc
            AddSkeleton(BodyJoint.middleSpine, BodyJoint.centralNeck);
            AddSkeleton(BodyJoint.centralNeck, BodyJoint.topHead);
            AddSkeleton(BodyJoint.topHead, BodyJoint.centralNose);
            AddSkeleton(BodyJoint.centralNeck, BodyJoint.rightShoulder);
            AddSkeleton(BodyJoint.centralNeck, BodyJoint.leftShoulder);
            AddSkeleton(BodyJoint.rightUpperLeg, BodyJoint.rightShoulder);
            AddSkeleton(BodyJoint.leftUpperLeg, BodyJoint.leftShoulder);
            AddSkeleton(BodyJoint.rightShoulder, BodyJoint.upperAbdomen);
            AddSkeleton(BodyJoint.leftShoulder, BodyJoint.upperAbdomen);
            AddSkeleton(BodyJoint.rightUpperLeg, BodyJoint.upperAbdomen);
            AddSkeleton(BodyJoint.leftUpperLeg, BodyJoint.upperAbdomen);
            AddSkeleton(BodyJoint.leftUpperLeg, BodyJoint.rightUpperLeg);
        }

        // Set Inverse
        var forward = TriangleNormal(_jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position, _jointPoints[BodyJoint.leftUpperLeg.Int()].BoneTransform.position, _jointPoints[BodyJoint.rightUpperLeg.Int()].BoneTransform.position);
        foreach (var jointPoint in _jointPoints)
        {
            if (jointPoint.BoneTransform != null)
            {
                jointPoint.InitialRotation = jointPoint.BoneTransform.rotation;
            }

            if (jointPoint.ChildJoint != null)
            {
                jointPoint.RotationInverse = GetInverse(jointPoint, jointPoint.ChildJoint, forward);
                jointPoint.ComputedRotation = jointPoint.RotationInverse * jointPoint.InitialRotation;
            }
        }
        var hip = _jointPoints[BodyJoint.centerHip.Int()];
        _initPosition = _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position;
        hip.RotationInverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        hip.ComputedRotation = hip.RotationInverse * hip.InitialRotation;

        // For Head Rotation
        var head = _jointPoints[BodyJoint.topHead.Int()];
        head.InitialRotation = _jointPoints[BodyJoint.topHead.Int()].BoneTransform.rotation;
        var gaze = _jointPoints[BodyJoint.centralNose.Int()].BoneTransform.position - _jointPoints[BodyJoint.topHead.Int()].BoneTransform.position;
        head.RotationInverse = Quaternion.Inverse(Quaternion.LookRotation(gaze));
        head.ComputedRotation = head.RotationInverse * head.InitialRotation;
        
        var lHand = _jointPoints[BodyJoint.leftHand.Int()];
        var lf = TriangleNormal(lHand.Position3D, _jointPoints[BodyJoint.leftFinger.Int()].Position3D, _jointPoints[BodyJoint.leftThumb.Int()].Position3D);
        lHand.InitialRotation = lHand.BoneTransform.rotation;
        lHand.RotationInverse = Quaternion.Inverse(Quaternion.LookRotation(_jointPoints[BodyJoint.leftThumb.Int()].BoneTransform.position - _jointPoints[BodyJoint.leftFinger.Int()].BoneTransform.position, lf));
        lHand.ComputedRotation = lHand.RotationInverse * lHand.InitialRotation;

        var rHand = _jointPoints[BodyJoint.rightHand.Int()];
        var rf = TriangleNormal(rHand.Position3D, _jointPoints[BodyJoint.rightThumb.Int()].Position3D, _jointPoints[BodyJoint.rightFinger.Int()].Position3D);
        rHand.InitialRotation = _jointPoints[BodyJoint.rightHand.Int()].BoneTransform.rotation;
        rHand.RotationInverse = Quaternion.Inverse(Quaternion.LookRotation(_jointPoints[BodyJoint.rightThumb.Int()].BoneTransform.position - _jointPoints[BodyJoint.rightFinger.Int()].BoneTransform.position, rf));
        rHand.ComputedRotation = rHand.RotationInverse * rHand.InitialRotation;

        _jointPoints[BodyJoint.centerHip.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.centralNeck.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.centralNose.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.topHead.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.middleSpine.Int()].Confidence3D = 1f;


        return JointPoints;
    }

    public void UpdatePose()
    {
        // caliculate movement range of z-coordinate from height
        var t1 = Vector3.Distance(_jointPoints[BodyJoint.topHead.Int()].Position3D, _jointPoints[BodyJoint.centralNeck.Int()].Position3D);
        var t2 = Vector3.Distance(_jointPoints[BodyJoint.centralNeck.Int()].Position3D, _jointPoints[BodyJoint.middleSpine.Int()].Position3D);
        var pm = (_jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D + _jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D) / 2f;
        var t3 = Vector3.Distance(_jointPoints[BodyJoint.middleSpine.Int()].Position3D, pm);
        var t4r = Vector3.Distance(_jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D, _jointPoints[BodyJoint.rightLowerLeg.Int()].Position3D);
        var t4l = Vector3.Distance(_jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D, _jointPoints[BodyJoint.leftLowerLeg.Int()].Position3D);
        var t4 = (t4r + t4l) / 2f;
        var t5r = Vector3.Distance(_jointPoints[BodyJoint.rightLowerLeg.Int()].Position3D, _jointPoints[BodyJoint.rightFoot.Int()].Position3D);
        var t5l = Vector3.Distance(_jointPoints[BodyJoint.leftLowerLeg.Int()].Position3D, _jointPoints[BodyJoint.leftFoot.Int()].Position3D);
        var t5 = (t5r + t5l) / 2f;
        var t = t1 + t2 + t3 + t4 + t5;


        // Low pass filter in z direction
        _currentHeight = t * 0.7f + _previousHeight * 0.3f;
        _previousHeight = _currentHeight;

        if (_currentHeight == 0)
        {
            _currentHeight = BaseHeight;
        }
        var dz = (BaseHeight - _currentHeight) / BaseHeight * zVerticalScale;

        // movement and rotatation of center
        var forward = TriangleNormal(_jointPoints[BodyJoint.centerHip.Int()].Position3D, _jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D, _jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D);
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position = _jointPoints[BodyJoint.centerHip.Int()].Position3D * 0.005f + new Vector3(_initPosition.x, _initPosition.y, _initPosition.z + dz);
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.rotation = Quaternion.LookRotation(forward) * _jointPoints[BodyJoint.centerHip.Int()].ComputedRotation;

        // rotate each of bones
        foreach (var jointPoint in _jointPoints)
        {
            if (jointPoint.ParentJoint != null)
            {
                var fv = jointPoint.ParentJoint.Position3D - jointPoint.Position3D;
                jointPoint.BoneTransform.rotation = Quaternion.LookRotation(jointPoint.Position3D - jointPoint.ChildJoint.Position3D, fv) * jointPoint.ComputedRotation;
            }
            else if (jointPoint.ChildJoint != null)
            {
                jointPoint.BoneTransform.rotation = Quaternion.LookRotation(jointPoint.Position3D - jointPoint.ChildJoint.Position3D, forward) * jointPoint.ComputedRotation;
            }
        }

        // Head Rotation
        var gaze = _jointPoints[BodyJoint.centralNose.Int()].Position3D - _jointPoints[BodyJoint.topHead.Int()].Position3D;
        var f = TriangleNormal(_jointPoints[BodyJoint.centralNose.Int()].Position3D, _jointPoints[BodyJoint.rightEar.Int()].Position3D, _jointPoints[BodyJoint.leftEar.Int()].Position3D);
        var head = _jointPoints[BodyJoint.topHead.Int()];
        head.BoneTransform.rotation = Quaternion.LookRotation(gaze, f) * head.ComputedRotation;
        
        // Wrist rotation (Test code)
        var lHand = _jointPoints[BodyJoint.leftHand.Int()];
        var lf = TriangleNormal(lHand.Position3D, _jointPoints[BodyJoint.leftFinger.Int()].Position3D, _jointPoints[BodyJoint.leftThumb.Int()].Position3D);
        lHand.BoneTransform.rotation = Quaternion.LookRotation(_jointPoints[BodyJoint.leftThumb.Int()].Position3D - _jointPoints[BodyJoint.leftFinger.Int()].Position3D, lf) * lHand.ComputedRotation;

        var rHand = _jointPoints[BodyJoint.rightHand.Int()];
        var rf = TriangleNormal(rHand.Position3D, _jointPoints[BodyJoint.rightThumb.Int()].Position3D, _jointPoints[BodyJoint.rightFinger.Int()].Position3D);
        //rHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.rThumb2.Int()].Pos3D - jointPoints[PositionIndex.rMid1.Int()].Pos3D, rf) * rHand.InverseRotation;
        rHand.BoneTransform.rotation = Quaternion.LookRotation(_jointPoints[BodyJoint.rightThumb.Int()].Position3D - _jointPoints[BodyJoint.rightFinger.Int()].Position3D, rf) * rHand.ComputedRotation;

        foreach (var sk in _boneConnections)
        {
            var s = sk.StartJoint;
            var e = sk.EndJoint;

            sk.Line.SetPosition(0, new Vector3(s.Position3D.x * skeletonScale + skeletonOffsetX, s.Position3D.y * skeletonScale + skeletonOffsetY, s.Position3D.z * skeletonScale + skeletonOffsetZ));
            sk.Line.SetPosition(1, new Vector3(e.Position3D.x * skeletonScale + skeletonOffsetX, e.Position3D.y * skeletonScale + skeletonOffsetY, e.Position3D.z * skeletonScale + skeletonOffsetZ));
        }
    }

    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.BoneTransform.position - p2.BoneTransform.position, forward));
    }

    /// <summary>
    /// Add skelton from joint points
    /// </summary>
    /// <param name="s">position index</param>
    /// <param name="e">position index</param>
    private void AddSkeleton(BodyJoint s, BodyJoint e)
    {
        var sk = new BoneConnection()
        {
            LineObject = new GameObject("Line"),
            StartJoint = _jointPoints[s.Int()],
            EndJoint = _jointPoints[e.Int()],
        };

        sk.Line = sk.LineObject.AddComponent<LineRenderer>();
        sk.Line.startWidth = 0.04f;
        sk.Line.endWidth = 0.01f;
        
        // define the number of vertex
        sk.Line.positionCount = 2;
        sk.Line.material = connectionMaterial;

        _boneConnections.Add(sk);
    }
}
