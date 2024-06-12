using System.Collections.Generic;
using UnityEngine;

public class PoseModel : MonoBehaviour
{
    public SkeletonBuilder _skeletonBuilder;

    // Joint position and bone
    private JointPoint[] _jointPoints;
    public JointPoint[] JointPoints => _jointPoints;

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
    private float _previousMinY = float.MaxValue;

    private void Update()
    {
        if (_jointPoints != null) UpdatePose();
    }
    
    public JointPoint[] Init()
    {
        _jointPoints = new JointPoint[BodyJoint.totalCount.Int()];
        for (var i = 0; i < BodyJoint.totalCount.Int(); i++) _jointPoints[i] = new JointPoint();

        LinkJoints();
        SetupInitialRotations();
        SetConfidenceLevels();

        _skeletonBuilder.CreateSkeletonStructure(_jointPoints);

        return JointPoints;
    }

    private void LinkJoints()
    {
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
    }

    private void SetupInitialRotations()
    {
        // Set Inverse
        var forward = TriangleNormal(
            _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position, 
            _jointPoints[BodyJoint.leftUpperLeg.Int()].BoneTransform.position,
            _jointPoints[BodyJoint.rightUpperLeg.Int()].BoneTransform.position);
        foreach (var jointPoint in _jointPoints)
        {
            if (jointPoint.BoneTransform != null) jointPoint.InitialRotation = jointPoint.BoneTransform.rotation;

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
    }

    private void SetConfidenceLevels()
    {
        _jointPoints[BodyJoint.centerHip.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.centralNeck.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.centralNose.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.topHead.Int()].Confidence3D = 1f;
        _jointPoints[BodyJoint.middleSpine.Int()].Confidence3D = 1f;
    }

    public void UpdatePose()
    {
        CalculateAndSmoothCharacterHeight();
        RotateBones();
        PositionHipsRelativeToFeet();

        _skeletonBuilder.UpdateBoneLines();
    }

private void CalculateAndSmoothCharacterHeight()
{
    // Calculate distances between joint points for a more accurate character height measurement
    var headToNeckDistance = Vector3.Distance(
        _jointPoints[BodyJoint.topHead.Int()].Position3D,
        _jointPoints[BodyJoint.centralNeck.Int()].Position3D);
    var neckToSpineDistance = Vector3.Distance(
        _jointPoints[BodyJoint.centralNeck.Int()].Position3D,
        _jointPoints[BodyJoint.middleSpine.Int()].Position3D);
    var pelvisMidpoint = (_jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D + _jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D) / 2f;
    var spineToPelvisDistance = Vector3.Distance(_jointPoints[BodyJoint.middleSpine.Int()].Position3D, pelvisMidpoint);
    var rightUpperToLowerLegDistance = Vector3.Distance(
        _jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D, 
        _jointPoints[BodyJoint.rightLowerLeg.Int()].Position3D);
    var leftUpperToLowerLegDistance = Vector3.Distance(
        _jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D,
        _jointPoints[BodyJoint.leftLowerLeg.Int()].Position3D);
    var averageLegLength = (rightUpperToLowerLegDistance + leftUpperToLowerLegDistance) / 2f;
    var rightLowerLegToFootDistance = Vector3.Distance(
        _jointPoints[BodyJoint.rightLowerLeg.Int()].Position3D,
        _jointPoints[BodyJoint.rightFoot.Int()].Position3D);
    var leftLowerLegToFootDistance = Vector3.Distance(
        _jointPoints[BodyJoint.leftLowerLeg.Int()].Position3D,
        _jointPoints[BodyJoint.leftFoot.Int()].Position3D);
    var averageFootHeight = (rightLowerLegToFootDistance + leftLowerLegToFootDistance) / 2f;
    var characterHeight = headToNeckDistance + neckToSpineDistance + spineToPelvisDistance + averageLegLength + averageFootHeight;

    // Apply a low pass filter to smooth character height variations
    _currentHeight = characterHeight * 0.7f + _previousHeight * 0.3f;
    _previousHeight = _currentHeight;

    // Ensure the character height does not drop to zero
    if (_currentHeight == 0) _currentHeight = BaseHeight;
}


    private void RotateBones()
    {
        var dz = (BaseHeight - _currentHeight) / BaseHeight * zVerticalScale;

        // movement and rotatation of center
        var forward = TriangleNormal(
            _jointPoints[BodyJoint.centerHip.Int()].Position3D,
            _jointPoints[BodyJoint.leftUpperLeg.Int()].Position3D,
            _jointPoints[BodyJoint.rightUpperLeg.Int()].Position3D);
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position = _jointPoints[BodyJoint.centerHip.Int()].Position3D * 0.005f + new Vector3(_initPosition.x, _initPosition.y, _initPosition.z + dz);
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.rotation = Quaternion.LookRotation(forward) * _jointPoints[BodyJoint.centerHip.Int()].ComputedRotation;

        // rotate each of bones
        foreach (var jointPoint in _jointPoints)
            if (jointPoint.ParentJoint != null)
            {
                var fv = jointPoint.ParentJoint.Position3D - jointPoint.Position3D;
                jointPoint.BoneTransform.rotation = Quaternion.LookRotation(jointPoint.Position3D - jointPoint.ChildJoint.Position3D, fv) * jointPoint.ComputedRotation;
            }
            else if (jointPoint.ChildJoint != null)
            {
                jointPoint.BoneTransform.rotation = Quaternion.LookRotation(jointPoint.Position3D - jointPoint.ChildJoint.Position3D, forward) * jointPoint.ComputedRotation;
            }

        // Head Rotation
        var gaze = _jointPoints[BodyJoint.centralNose.Int()].Position3D - _jointPoints[BodyJoint.topHead.Int()].Position3D;
        var f = TriangleNormal(
            _jointPoints[BodyJoint.centralNose.Int()].Position3D,
            _jointPoints[BodyJoint.rightEar.Int()].Position3D, 
            _jointPoints[BodyJoint.leftEar.Int()].Position3D);
        var head = _jointPoints[BodyJoint.topHead.Int()];
        head.BoneTransform.rotation = Quaternion.LookRotation(gaze, f) * head.ComputedRotation;

        // Wrist rotation (Test code)
        var lHand = _jointPoints[BodyJoint.leftHand.Int()];
        var lf = TriangleNormal(
            lHand.Position3D, 
            _jointPoints[BodyJoint.leftFinger.Int()].Position3D,
            _jointPoints[BodyJoint.leftThumb.Int()].Position3D);
        lHand.BoneTransform.rotation = Quaternion.LookRotation(_jointPoints[BodyJoint.leftThumb.Int()].Position3D - _jointPoints[BodyJoint.leftFinger.Int()].Position3D, lf) * lHand.ComputedRotation;

        var rHand = _jointPoints[BodyJoint.rightHand.Int()];
        var rf = TriangleNormal(
            rHand.Position3D, 
            _jointPoints[BodyJoint.rightThumb.Int()].Position3D,
            _jointPoints[BodyJoint.rightFinger.Int()].Position3D);
        //rHand.Transform.rotation = Quaternion.LookRotation(jointPoints[PositionIndex.rThumb2.Int()].Pos3D - jointPoints[PositionIndex.rMid1.Int()].Pos3D, rf) * rHand.InverseRotation;
        rHand.BoneTransform.rotation = Quaternion.LookRotation(_jointPoints[BodyJoint.rightThumb.Int()].Position3D - _jointPoints[BodyJoint.rightFinger.Int()].Position3D, rf) * rHand.ComputedRotation;
    }

    public void PositionHipsRelativeToFeet()
    {
        var leftFootPosition = GetJointPosition(BodyJoint.leftToe);
        var rightFootPosition = GetJointPosition(BodyJoint.rightToe);
        var currentMinY = Mathf.Min(leftFootPosition.y, rightFootPosition.y);
        var smoothingFactor = 0.01f;
        // float smoothedMinY = Mathf.Lerp(_previousMinY, currentMinY, smoothingFactor);
        var smoothedMinY = _previousMinY - currentMinY * 0.5f;

        var hipPosition = _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position;
        hipPosition.y -= currentMinY;
        _jointPoints[BodyJoint.centerHip.Int()].BoneTransform.position = hipPosition;
        _previousMinY = smoothedMinY;
    }


    public Vector3 GetJointPosition(BodyJoint joint)
    {
        if (_jointPoints == null)
        {
            Debug.LogError("Joint points are not initialized.");
            return Vector3.zero;
        }

        var jointIndex = joint.Int();
        if (jointIndex < 0 || jointIndex >= _jointPoints.Length)
        {
            Debug.LogError("Joint index is out of range.");
            return Vector3.zero;
        }

        return _jointPoints[jointIndex].BoneTransform.position;
    }


    private Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var d1 = a - b;
        var d2 = a - c;

        var dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.BoneTransform.position - p2.BoneTransform.position,
            forward));
    }
}