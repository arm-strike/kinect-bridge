using System;
using UnityEngine;
using CommonArmTrackingFrame = KinectBridge.Tracking.ArmTrackingFrame;
using CommonArmTrackingJointCollection = KinectBridge.Tracking.ArmTrackingJointCollection;
using CommonArmTrackingJointSample = KinectBridge.Tracking.ArmTrackingJointSample;
using CommonArmTrackingMetrics = KinectBridge.Tracking.ArmTrackingMetrics;
using CommonArmTrackingPacket = KinectBridge.Tracking.ArmTrackingPacket;

namespace Tracking.Runtime
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

    public enum TrackingSourceStatus
    {
        Disconnected = 0,
        NoPerson = 1,
        Tracking = 2,
        TrackingButNormalizationInvalid = 3
    }

    [Serializable]
    public sealed class ArmTrackingJointSample
    {
        public float x;
        public float y;
        public float z;
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

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public sealed class ArmTrackingJointCollection
    {
        public ArmTrackingJointSample shoulderCenter;
        public ArmTrackingJointSample shoulderLeft;
        public ArmTrackingJointSample elbowLeft;
        public ArmTrackingJointSample wristLeft;
        public ArmTrackingJointSample handLeft;
        public ArmTrackingJointSample shoulderRight;
        public ArmTrackingJointSample elbowRight;
        public ArmTrackingJointSample wristRight;
        public ArmTrackingJointSample handRight;

        public ArmTrackingJointCollection Clone()
        {
            return new ArmTrackingJointCollection
            {
                shoulderCenter = CloneJoint(shoulderCenter),
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
    public sealed class ArmTrackingPacket
    {
        public int version = ProtocolConstants.CurrentVersion;
        public string sessionId;
        public long frameId;
        public long timestampMs;
        public bool tracked;
        public long? trackingId;
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
    public sealed class UnityArmTrackingPacketDto
    {
        public int version = ProtocolConstants.CurrentVersion;
        public string sessionId;
        public long frameId;
        public long timestampMs;
        public bool tracked;
        public long trackingId;
        public UnityArmTrackingJointCollection joints;

        public ArmTrackingPacket ToSnapshot()
        {
            ArmTrackingPacket packet = new ArmTrackingPacket();
            packet.version = version;
            packet.sessionId = sessionId;
            packet.frameId = frameId;
            packet.timestampMs = timestampMs;
            packet.tracked = tracked;
            packet.trackingId = tracked && trackingId > 0 ? (long?)trackingId : null;
            packet.joints = tracked && joints != null ? joints.ToTrackingJoints() : null;
            return packet;
        }
    }

    [Serializable]
    public sealed class UnityArmTrackingJointCollection
    {
        public ArmTrackingJointSample shoulderCenter;
        public ArmTrackingJointSample shoulderLeft;
        public ArmTrackingJointSample elbowLeft;
        public ArmTrackingJointSample wristLeft;
        public ArmTrackingJointSample handLeft;
        public ArmTrackingJointSample shoulderRight;
        public ArmTrackingJointSample elbowRight;
        public ArmTrackingJointSample wristRight;
        public ArmTrackingJointSample handRight;

        public ArmTrackingJointCollection ToTrackingJoints()
        {
            return new ArmTrackingJointCollection
            {
                shoulderCenter = shoulderCenter == null ? null : shoulderCenter.Clone(),
                shoulderLeft = shoulderLeft == null ? null : shoulderLeft.Clone(),
                elbowLeft = elbowLeft == null ? null : elbowLeft.Clone(),
                wristLeft = wristLeft == null ? null : wristLeft.Clone(),
                handLeft = handLeft == null ? null : handLeft.Clone(),
                shoulderRight = shoulderRight == null ? null : shoulderRight.Clone(),
                elbowRight = elbowRight == null ? null : elbowRight.Clone(),
                wristRight = wristRight == null ? null : wristRight.Clone(),
                handRight = handRight == null ? null : handRight.Clone()
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
    }

    public interface IArmTrackingSource : IDisposable
    {
        bool IsConnected { get; }
        bool IsTracked { get; }
        TrackingSourceStatus Status { get; }
        string LastReceiveError { get; }
        ArmTrackingFrame LatestFrame { get; }
    }

    public interface IArmTrackingSourceTickable
    {
        void Tick(float deltaTime);
    }

    public interface IArmTrackingSourceLifecycle
    {
        void Start();
        void Stop();
    }

    internal static class TrackingModelMapper
    {
        public static ArmTrackingPacket ToRuntime(this CommonArmTrackingPacket packet)
        {
            if (packet == null)
            {
                return null;
            }

            return new ArmTrackingPacket
            {
                version = packet.version,
                sessionId = packet.sessionId,
                frameId = packet.frameId,
                timestampMs = packet.timestampMs,
                tracked = packet.tracked,
                trackingId = packet.trackingId,
                joints = packet.joints.ToRuntime()
            };
        }

        public static CommonArmTrackingPacket ToCommon(this ArmTrackingPacket packet)
        {
            if (packet == null)
            {
                return null;
            }

            return new CommonArmTrackingPacket
            {
                version = packet.version,
                sessionId = packet.sessionId,
                frameId = packet.frameId,
                timestampMs = packet.timestampMs,
                tracked = packet.tracked,
                trackingId = packet.trackingId,
                joints = packet.joints.ToCommon()
            };
        }

        public static ArmTrackingFrame ToRuntime(this CommonArmTrackingFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            return new ArmTrackingFrame
            {
                SessionId = frame.SessionId,
                FrameId = frame.FrameId,
                TimestampMs = frame.TimestampMs,
                Tracked = frame.Tracked,
                TrackingId = frame.TrackingId,
                RawJoints = frame.RawJoints.ToRuntime(),
                NormalizedJoints = frame.NormalizedJoints.ToRuntime(),
                IsNormalizedValid = frame.IsNormalizedValid,
                NormalizationError = frame.NormalizationError,
                ShoulderWidthMeters = frame.ShoulderWidthMeters,
                ShoulderCenter = frame.ShoulderCenter.ToRuntime(),
                NormalizationOrigin = frame.NormalizationOrigin.ToRuntime(),
                Metrics = frame.Metrics.ToRuntime()
            };
        }

        public static CommonArmTrackingFrame ToCommon(this ArmTrackingFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            return new CommonArmTrackingFrame
            {
                SessionId = frame.SessionId,
                FrameId = frame.FrameId,
                TimestampMs = frame.TimestampMs,
                Tracked = frame.Tracked,
                TrackingId = frame.TrackingId,
                RawJoints = frame.RawJoints.ToCommon(),
                NormalizedJoints = frame.NormalizedJoints.ToCommon(),
                IsNormalizedValid = frame.IsNormalizedValid,
                NormalizationError = frame.NormalizationError,
                ShoulderWidthMeters = frame.ShoulderWidthMeters,
                ShoulderCenter = frame.ShoulderCenter.ToCommon(),
                NormalizationOrigin = frame.NormalizationOrigin.ToCommon(),
                Metrics = frame.Metrics.ToCommon()
            };
        }

        public static ArmTrackingJointCollection ToRuntime(this CommonArmTrackingJointCollection joints)
        {
            if (joints == null)
            {
                return null;
            }

            return new ArmTrackingJointCollection
            {
                shoulderCenter = joints.shoulderCenter.ToRuntime(),
                shoulderLeft = joints.shoulderLeft.ToRuntime(),
                elbowLeft = joints.elbowLeft.ToRuntime(),
                wristLeft = joints.wristLeft.ToRuntime(),
                handLeft = joints.handLeft.ToRuntime(),
                shoulderRight = joints.shoulderRight.ToRuntime(),
                elbowRight = joints.elbowRight.ToRuntime(),
                wristRight = joints.wristRight.ToRuntime(),
                handRight = joints.handRight.ToRuntime()
            };
        }

        public static CommonArmTrackingJointCollection ToCommon(this ArmTrackingJointCollection joints)
        {
            if (joints == null)
            {
                return null;
            }

            return new CommonArmTrackingJointCollection
            {
                shoulderCenter = joints.shoulderCenter.ToCommon(),
                shoulderLeft = joints.shoulderLeft.ToCommon(),
                elbowLeft = joints.elbowLeft.ToCommon(),
                wristLeft = joints.wristLeft.ToCommon(),
                handLeft = joints.handLeft.ToCommon(),
                shoulderRight = joints.shoulderRight.ToCommon(),
                elbowRight = joints.elbowRight.ToCommon(),
                wristRight = joints.wristRight.ToCommon(),
                handRight = joints.handRight.ToCommon()
            };
        }

        public static ArmTrackingJointSample ToRuntime(this CommonArmTrackingJointSample joint)
        {
            if (joint == null)
            {
                return null;
            }

            return new ArmTrackingJointSample
            {
                x = joint.x,
                y = joint.y,
                z = joint.z,
                state = joint.state
            };
        }

        public static CommonArmTrackingJointSample ToCommon(this ArmTrackingJointSample joint)
        {
            if (joint == null)
            {
                return null;
            }

            return new CommonArmTrackingJointSample
            {
                x = joint.x,
                y = joint.y,
                z = joint.z,
                state = joint.state
            };
        }

        public static ArmTrackingMetrics ToRuntime(this CommonArmTrackingMetrics metrics)
        {
            if (metrics == null)
            {
                return null;
            }

            return new ArmTrackingMetrics
            {
                LeftElbowAngleDegrees = metrics.LeftElbowAngleDegrees,
                RightElbowAngleDegrees = metrics.RightElbowAngleDegrees,
                LeftHandSpeedMetersPerSecond = metrics.LeftHandSpeedMetersPerSecond,
                RightHandSpeedMetersPerSecond = metrics.RightHandSpeedMetersPerSecond,
                LeftHandTowardKinectSpeedMetersPerSecond = metrics.LeftHandTowardKinectSpeedMetersPerSecond,
                RightHandTowardKinectSpeedMetersPerSecond = metrics.RightHandTowardKinectSpeedMetersPerSecond,
                LeftShoulderToHandDistanceMeters = metrics.LeftShoulderToHandDistanceMeters,
                RightShoulderToHandDistanceMeters = metrics.RightShoulderToHandDistanceMeters,
                LeftArmExtensionNormalized = metrics.LeftArmExtensionNormalized,
                RightArmExtensionNormalized = metrics.RightArmExtensionNormalized
            };
        }

        public static CommonArmTrackingMetrics ToCommon(this ArmTrackingMetrics metrics)
        {
            if (metrics == null)
            {
                return null;
            }

            return new CommonArmTrackingMetrics
            {
                LeftElbowAngleDegrees = metrics.LeftElbowAngleDegrees,
                RightElbowAngleDegrees = metrics.RightElbowAngleDegrees,
                LeftHandSpeedMetersPerSecond = metrics.LeftHandSpeedMetersPerSecond,
                RightHandSpeedMetersPerSecond = metrics.RightHandSpeedMetersPerSecond,
                LeftHandTowardKinectSpeedMetersPerSecond = metrics.LeftHandTowardKinectSpeedMetersPerSecond,
                RightHandTowardKinectSpeedMetersPerSecond = metrics.RightHandTowardKinectSpeedMetersPerSecond,
                LeftShoulderToHandDistanceMeters = metrics.LeftShoulderToHandDistanceMeters,
                RightShoulderToHandDistanceMeters = metrics.RightShoulderToHandDistanceMeters,
                LeftArmExtensionNormalized = metrics.LeftArmExtensionNormalized,
                RightArmExtensionNormalized = metrics.RightArmExtensionNormalized
            };
        }
    }
}
