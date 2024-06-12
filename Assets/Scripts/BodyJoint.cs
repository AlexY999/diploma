using System.Collections;
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