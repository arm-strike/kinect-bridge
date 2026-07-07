using System;
using System.Runtime.Serialization;

namespace KinectBridge.Tracking
{
    public static class ProtocolConstants
    {
        public const int CurrentVersion = 1;
    }

    public enum ArmTrackingJointState
    {
        NotTracked = 0,
        Inferred = 1,
        Tracked = 2
    }

    [Serializable]
    [DataContract]
    public sealed class ArmTrackingJointSample
    {
        [DataMember(Name = "x")]
        public float x;

        [DataMember(Name = "y")]
        public float y;

        [DataMember(Name = "z")]
        public float z;

        [DataMember(Name = "state")]
        public int state;

        public ArmTrackingJointSample Clone()
        {
            return new ArmTrackingJointSample
            {
                x = x,
                y = y,
                z = z,
                state = state
            };
        }
    }

    [Serializable]
    [DataContract]
    public sealed class ArmTrackingJointCollection
    {
        [DataMember(Name = "shoulderCenter")]
        public ArmTrackingJointSample shoulderCenter;

        [DataMember(Name = "spine")]
        public ArmTrackingJointSample spine;

        [DataMember(Name = "hipCenter")]
        public ArmTrackingJointSample hipCenter;

        [DataMember(Name = "spine")]
        public ArmTrackingJointSample spine;

        [DataMember(Name = "hipCenter")]
        public ArmTrackingJointSample hipCenter;

        [DataMember(Name = "shoulderLeft")]
        public ArmTrackingJointSample shoulderLeft;

        [DataMember(Name = "elbowLeft")]
        public ArmTrackingJointSample elbowLeft;

        [DataMember(Name = "wristLeft")]
        public ArmTrackingJointSample wristLeft;

        [DataMember(Name = "handLeft")]
        public ArmTrackingJointSample handLeft;

        [DataMember(Name = "shoulderRight")]
        public ArmTrackingJointSample shoulderRight;

        [DataMember(Name = "elbowRight")]
        public ArmTrackingJointSample elbowRight;

        [DataMember(Name = "wristRight")]
        public ArmTrackingJointSample wristRight;

        [DataMember(Name = "handRight")]
        public ArmTrackingJointSample handRight;

        public ArmTrackingJointCollection Clone()
        {
            return new ArmTrackingJointCollection
            {
                shoulderCenter = CloneJoint(shoulderCenter),
                spine = CloneJoint(spine),
                hipCenter = CloneJoint(hipCenter),
                spine = CloneJoint(spine),
                hipCenter = CloneJoint(hipCenter),
                shoulderLeft = CloneJoint(shoulderLeft),
                elbowLeft = CloneJoint(elbowLeft),
                wristLeft = CloneJoint(wristLeft),
                handLeft = CloneJoint(handLeft),
                shoulderRight = CloneJoint(shoulderRight),
                elbowRight = CloneJoint(elbowRight),
                wristRight = CloneJoint(wristRight),
                handRight = CloneJoint(handRight)
            };
        }

        private static ArmTrackingJointSample CloneJoint(ArmTrackingJointSample joint)
        {
            return joint == null ? null : joint.Clone();
        }
    }

    [Serializable]
    [DataContract]
    public sealed class ArmTrackingPacket
    {
        [DataMember(Name = "version")]
        public int version = ProtocolConstants.CurrentVersion;

        [DataMember(Name = "sessionId")]
        public string sessionId;

        [DataMember(Name = "frameId")]
        public long frameId;

        [DataMember(Name = "timestampMs")]
        public long timestampMs;

        [DataMember(Name = "tracked")]
        public bool tracked;

        [DataMember(Name = "trackingId", EmitDefaultValue = false)]
        public long? trackingId;

        [DataMember(Name = "joints", EmitDefaultValue = false)]
        public ArmTrackingJointCollection joints;

        public ArmTrackingPacket Clone()
        {
            return new ArmTrackingPacket
            {
                version = version,
                sessionId = sessionId,
                frameId = frameId,
                timestampMs = timestampMs,
                tracked = tracked,
                trackingId = trackingId,
                joints = joints == null ? null : joints.Clone()
            };
        }
    }

    [Serializable]
    public sealed class ArmTrackingFrame
    {
        public string SessionId { get; set; }

        public long FrameId { get; set; }

        public long TimestampMs { get; set; }

        public bool Tracked { get; set; }

        public long? TrackingId { get; set; }

        public ArmTrackingJointCollection RawJoints { get; set; }

        public ArmTrackingJointCollection NormalizedJoints { get; set; }

        public bool IsNormalizedValid { get; set; }

        public string NormalizationError { get; set; }

        public float ShoulderWidthMeters { get; set; }

        public ArmTrackingJointSample ShoulderCenter { get; set; }

        public ArmTrackingJointSample NormalizationOrigin { get; set; }

        public ArmTrackingMetrics Metrics { get; set; }
    }

    [Serializable]
    public sealed class ArmTrackingNormalizationOptions
    {
        public float MinimumShoulderWidthMeters { get; set; } = 0.05f;
    }

    [Serializable]
    public sealed class ArmTrackingNormalizationResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public ArmTrackingJointSample ShoulderCenter { get; set; }

        public ArmTrackingJointSample NormalizationOrigin { get; set; }

        public float ShoulderWidthMeters { get; set; }

        public ArmTrackingJointCollection NormalizedJoints { get; set; }
    }

    [Serializable]
    public sealed class ArmTrackingMetrics
    {
        public double LeftElbowAngleDegrees { get; set; }

        public double RightElbowAngleDegrees { get; set; }

        public double LeftHandSpeedMetersPerSecond { get; set; }

        public double RightHandSpeedMetersPerSecond { get; set; }

        public double LeftHandTowardKinectSpeedMetersPerSecond { get; set; }

        public double RightHandTowardKinectSpeedMetersPerSecond { get; set; }

        public double LeftShoulderToHandDistanceMeters { get; set; }

        public double RightShoulderToHandDistanceMeters { get; set; }

        public double LeftArmExtensionNormalized { get; set; }

        public double RightArmExtensionNormalized { get; set; }

        public double LeftHandForwardFromShoulderMeters { get; set; }

        public double RightHandForwardFromShoulderMeters { get; set; }

        public double LeftHandForwardFromShoulderMeters { get; set; }

        public double RightHandForwardFromShoulderMeters { get; set; }
    }
}
