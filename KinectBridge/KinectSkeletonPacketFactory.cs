using System;
using Microsoft.Kinect;
using KinectBridge.Tracking;

namespace KinectBridge
{
    internal static class KinectSkeletonPacketFactory
    {
        public static ArmTrackingPacket CreateTrackedPacket(Skeleton skeleton, string sessionId, long frameId, long timestampMs)
        {
            ArmTrackingPacket packet = new ArmTrackingPacket();
            packet.sessionId = sessionId;
            packet.frameId = frameId;
            packet.timestampMs = timestampMs;
            packet.tracked = true;
            packet.trackingId = skeleton.TrackingId;
            packet.joints = CreateJoints(skeleton);
            return packet;
        }

        public static ArmTrackingPacket CreateUntrackedPacket(string sessionId, long frameId, long timestampMs)
        {
            ArmTrackingPacket packet = new ArmTrackingPacket();
            packet.sessionId = sessionId;
            packet.frameId = frameId;
            packet.timestampMs = timestampMs;
            packet.tracked = false;
            packet.trackingId = null;
            packet.joints = null;
            return packet;
        }

        private static ArmTrackingJointCollection CreateJoints(Skeleton skeleton)
        {
            ArmTrackingJointCollection joints = new ArmTrackingJointCollection();
            joints.shoulderCenter = ConvertJoint(skeleton, JointType.ShoulderCenter);
            joints.spine = ConvertJoint(skeleton, JointType.Spine);
            joints.hipCenter = ConvertJoint(skeleton, JointType.HipCenter);
            joints.shoulderLeft = ConvertJoint(skeleton, JointType.ShoulderLeft);
            joints.elbowLeft = ConvertJoint(skeleton, JointType.ElbowLeft);
            joints.wristLeft = ConvertJoint(skeleton, JointType.WristLeft);
            joints.handLeft = ConvertJoint(skeleton, JointType.HandLeft);
            joints.shoulderRight = ConvertJoint(skeleton, JointType.ShoulderRight);
            joints.elbowRight = ConvertJoint(skeleton, JointType.ElbowRight);
            joints.wristRight = ConvertJoint(skeleton, JointType.WristRight);
            joints.handRight = ConvertJoint(skeleton, JointType.HandRight);
            return joints;
        }

        private static ArmTrackingJointSample ConvertJoint(Skeleton skeleton, JointType type)
        {
            Joint joint = skeleton.Joints[type];
            return new ArmTrackingJointSample
            {
                x = joint.Position.X,
                y = joint.Position.Y,
                z = joint.Position.Z,
                state = (int)joint.TrackingState
            };
        }
    }
}
