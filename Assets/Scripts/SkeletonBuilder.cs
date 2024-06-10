using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneConnection
{
    public GameObject LineObject;
    public LineRenderer Line;

    public JointPoint StartJoint = null;
    public JointPoint EndJoint = null;
}

public class SkeletonBuilder : MonoBehaviour
{
    public Material connectionMaterial;
    public bool showSkeleton = true;
    public float skeletonOffsetX = 0;
    public float skeletonOffsetY = 0;
    public float skeletonOffsetZ = 0;
    public float skeletonScale = 0.008f;

    private readonly List<BoneConnection> _boneConnections = new List<BoneConnection>();
    private bool _useSkeleton;

    private void Start()
    {
        _useSkeleton = showSkeleton;
    }

    public void CreateSkeletonStructure(JointPoint[] _jointPoints)
    {
        if (_useSkeleton)
        {
            GameObject skeletonRoot = new GameObject("SkeletonRoot");
            skeletonRoot.transform.SetParent(this.transform);

            // Line Child Settings
            // Right Arm
            AddSkeletonLine(_jointPoints, BodyJoint.rightShoulder, BodyJoint.rightElbow, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightElbow, BodyJoint.rightHand, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightHand, BodyJoint.rightThumb, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightHand, BodyJoint.rightFinger, skeletonRoot);

            // Left Arm
            AddSkeletonLine(_jointPoints, BodyJoint.leftShoulder, BodyJoint.leftElbow, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftElbow, BodyJoint.leftHand, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftHand, BodyJoint.leftThumb, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftHand, BodyJoint.leftFinger, skeletonRoot);

            // Fase
            AddSkeletonLine(_jointPoints, BodyJoint.leftEar, BodyJoint.centralNose, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightEar, BodyJoint.centralNose, skeletonRoot);

            // Right Leg
            AddSkeletonLine(_jointPoints, BodyJoint.rightUpperLeg, BodyJoint.rightLowerLeg, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightLowerLeg, BodyJoint.rightFoot, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightFoot, BodyJoint.rightToe, skeletonRoot);

            // Left Leg
            AddSkeletonLine(_jointPoints, BodyJoint.leftUpperLeg, BodyJoint.leftLowerLeg, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftLowerLeg, BodyJoint.leftFoot, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftFoot, BodyJoint.leftToe, skeletonRoot);

            // etc
            AddSkeletonLine(_jointPoints, BodyJoint.middleSpine, BodyJoint.centralNeck, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.centralNeck, BodyJoint.topHead, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.topHead, BodyJoint.centralNose, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.centralNeck, BodyJoint.rightShoulder, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.centralNeck, BodyJoint.leftShoulder, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightUpperLeg, BodyJoint.rightShoulder, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftUpperLeg, BodyJoint.leftShoulder, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightShoulder, BodyJoint.upperAbdomen, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftShoulder, BodyJoint.upperAbdomen, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.rightUpperLeg, BodyJoint.upperAbdomen, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftUpperLeg, BodyJoint.upperAbdomen, skeletonRoot);
            AddSkeletonLine(_jointPoints, BodyJoint.leftUpperLeg, BodyJoint.rightUpperLeg, skeletonRoot);
        }
    }

    public void UpdateBoneLines()
    {
        if (_useSkeleton)
        {
            foreach (var sk in _boneConnections)
            {
                var s = sk.StartJoint;
                var e = sk.EndJoint;

                sk.Line.SetPosition(
                    0, 
                    new Vector3(s.Position3D.x * skeletonScale + skeletonOffsetX,
                        s.Position3D.y * skeletonScale + skeletonOffsetY,
                        s.Position3D.z * skeletonScale + skeletonOffsetZ));
                sk.Line.SetPosition(
                    1,
                    new Vector3(e.Position3D.x * skeletonScale + skeletonOffsetX,
                        e.Position3D.y * skeletonScale + skeletonOffsetY,
                        e.Position3D.z * skeletonScale + skeletonOffsetZ));
            }
        }
    }

    private void AddSkeletonLine(JointPoint[] _jointPoints, BodyJoint startBodyJoint, BodyJoint endBodyJoint, GameObject parentObject)
    {
        var lineObject = new GameObject("Line");
        lineObject.transform.SetParent(parentObject.transform);

        var sk = new BoneConnection()
        {
            LineObject = lineObject,
            StartJoint = _jointPoints[startBodyJoint.Int()],
            EndJoint = _jointPoints[endBodyJoint.Int()],
        };

        sk.Line = lineObject.AddComponent<LineRenderer>();
        sk.Line.startWidth = 0.04f;
        sk.Line.endWidth = 0.01f;
        sk.Line.positionCount = 2;
        sk.Line.material = connectionMaterial;

        _boneConnections.Add(sk);
    }
}