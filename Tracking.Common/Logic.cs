using System;
using System.Collections.Generic;

namespace KinectBridge.Tracking
{
    public sealed class ConnectionWatchdog
    {
        private readonly TimeSpan _timeout;

        public ConnectionWatchdog(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            _timeout = timeout;
        }

        public TimeSpan Timeout
        {
            get { return _timeout; }
        }

        public DateTime? LastAcceptedUtc { get; private set; }

        public void MarkAccepted(DateTime acceptedUtc)
        {
            LastAcceptedUtc = acceptedUtc;
        }

        public void Reset()
        {
            LastAcceptedUtc = null;
        }

        public bool IsConnected(DateTime nowUtc)
        {
            if (!LastAcceptedUtc.HasValue)
            {
                return false;
            }

            return nowUtc - LastAcceptedUtc.Value <= _timeout;
        }
    }

    public sealed class LogThrottle
    {
        private sealed class Entry
        {
            public string Message;
            public DateTime LastLoggedUtc;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private readonly object _gate = new object();

        public bool ShouldLog(string key, string message, TimeSpan repeatInterval, DateTime nowUtc)
        {
            lock (_gate)
            {
                Entry entry;
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new Entry();
                    _entries[key] = entry;
                }

                if (!string.Equals(entry.Message, message, StringComparison.Ordinal))
                {
                    entry.Message = message;
                    entry.LastLoggedUtc = nowUtc;
                    return true;
                }

                if (nowUtc - entry.LastLoggedUtc >= repeatInterval)
                {
                    entry.LastLoggedUtc = nowUtc;
                    return true;
                }

                return false;
            }
        }

        public void Reset(string key)
        {
            lock (_gate)
            {
                _entries.Remove(key);
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
            }
        }
    }

    public sealed class ArmFrameNormalizer
    {
        private readonly ArmTrackingNormalizationOptions _options;

        public ArmFrameNormalizer()
            : this(new ArmTrackingNormalizationOptions())
        {
        }

        public ArmFrameNormalizer(ArmTrackingNormalizationOptions options)
        {
            _options = options ?? new ArmTrackingNormalizationOptions();
        }

        public ArmTrackingNormalizationResult TryNormalize(ArmTrackingJointCollection raw)
        {
            ArmTrackingNormalizationResult result = new ArmTrackingNormalizationResult();

            if (raw == null)
            {
                result.ErrorMessage = "正規化対象の関節セットがありません。";
                return result;
            }

            if (!HasUsableShoulderJoints(raw))
            {
                result.ErrorMessage = "肩の関節が NotTracked です。";
                return result;
            }

            ArmTrackingJointSample leftShoulder = raw.shoulderLeft;
            ArmTrackingJointSample rightShoulder = raw.shoulderRight;

            float centerX = (leftShoulder.x + rightShoulder.x) * 0.5f;
            float centerY = (leftShoulder.y + rightShoulder.y) * 0.5f;
            float centerZ = (leftShoulder.z + rightShoulder.z) * 0.5f;

            float widthX = leftShoulder.x - rightShoulder.x;
            float widthY = leftShoulder.y - rightShoulder.y;
            float widthZ = leftShoulder.z - rightShoulder.z;
            float width = (float)Math.Sqrt((widthX * widthX) + (widthY * widthY) + (widthZ * widthZ));

            if (width < _options.MinimumShoulderWidthMeters)
            {
                result.ErrorMessage = "肩幅が極端に小さいため正規化できません。";
                return result;
            }

            ArmTrackingJointSample normalizationOrigin = new ArmTrackingJointSample
            {
                x = centerX,
                y = centerY,
                z = centerZ,
                state = Math.Min(leftShoulder.state, rightShoulder.state)
            };

            result.Success = true;
            result.NormalizationOrigin = normalizationOrigin;
            result.ShoulderCenter = CreateNormalizedOrigin(normalizationOrigin.state);
            result.ShoulderWidthMeters = width;
            result.NormalizedJoints = NormalizeCollection(raw, normalizationOrigin, width, normalizationOrigin.state);
            return result;
        }

        private static bool HasUsableShoulderJoints(ArmTrackingJointCollection raw)
        {
            return raw.shoulderLeft != null &&
                   raw.shoulderRight != null &&
                   raw.shoulderLeft.state != (int)ArmTrackingJointState.NotTracked &&
                   raw.shoulderRight.state != (int)ArmTrackingJointState.NotTracked;
        }

        private static ArmTrackingJointCollection NormalizeCollection(ArmTrackingJointCollection raw, ArmTrackingJointSample center, float width, int centerState)
        {
            ArmTrackingJointCollection normalized = new ArmTrackingJointCollection
            {
                shoulderCenter = CreateNormalizedOrigin(centerState),
                shoulderLeft = NormalizeJoint(raw.shoulderLeft, center, width),
                elbowLeft = NormalizeJoint(raw.elbowLeft, center, width),
                wristLeft = NormalizeJoint(raw.wristLeft, center, width),
                handLeft = NormalizeJoint(raw.handLeft, center, width),
                shoulderRight = NormalizeJoint(raw.shoulderRight, center, width),
                elbowRight = NormalizeJoint(raw.elbowRight, center, width),
                wristRight = NormalizeJoint(raw.wristRight, center, width),
                handRight = NormalizeJoint(raw.handRight, center, width)
            };

            return normalized;
        }

        private static ArmTrackingJointSample CreateNormalizedOrigin(int state)
        {
            return new ArmTrackingJointSample
            {
                x = 0f,
                y = 0f,
                z = 0f,
                state = state
            };
        }

        private static ArmTrackingJointSample NormalizeJoint(ArmTrackingJointSample joint, ArmTrackingJointSample center, float width)
        {
            if (joint == null)
            {
                return null;
            }

            return new ArmTrackingJointSample
            {
                x = (joint.x - center.x) / width,
                y = (joint.y - center.y) / width,
                z = (joint.z - center.z) / width,
                state = joint.state
            };
        }
    }

    public sealed class ArmMotionMetricsCalculator
    {
        public bool TryCalculate(ArmTrackingFrame previousFrame, ArmTrackingFrame currentFrame, out ArmTrackingMetrics metrics, out string errorMessage)
        {
            metrics = null;
            errorMessage = null;

            if (previousFrame == null)
            {
                errorMessage = "前フレームがありません。";
                return false;
            }

            if (currentFrame == null)
            {
                errorMessage = "現在フレームがありません。";
                return false;
            }

            if (currentFrame.RawJoints == null || previousFrame.RawJoints == null)
            {
                errorMessage = "関節データが不足しています。";
                return false;
            }

            if (currentFrame.ShoulderWidthMeters <= 0f)
            {
                errorMessage = "肩幅が不正です。";
                return false;
            }

            double deltaSeconds = (currentFrame.TimestampMs - previousFrame.TimestampMs) / 1000.0;
            if (deltaSeconds <= 0.0)
            {
                errorMessage = "前フレームとの差分時間が不正です。";
                return false;
            }

            ArmTrackingJointCollection prev = previousFrame.RawJoints;
            ArmTrackingJointCollection curr = currentFrame.RawJoints;

            if (!HasUsableArmJoints(prev, curr))
            {
                errorMessage = "腕の関節が NotTracked です。";
                return false;
            }

            metrics = new ArmTrackingMetrics();
            metrics.LeftElbowAngleDegrees = ComputeElbowAngle(curr.shoulderLeft, curr.elbowLeft, curr.handLeft);
            metrics.RightElbowAngleDegrees = ComputeElbowAngle(curr.shoulderRight, curr.elbowRight, curr.handRight);
            metrics.LeftHandSpeedMetersPerSecond = ComputeSpeed(prev.handLeft, curr.handLeft, deltaSeconds);
            metrics.RightHandSpeedMetersPerSecond = ComputeSpeed(prev.handRight, curr.handRight, deltaSeconds);
            metrics.LeftHandTowardKinectSpeedMetersPerSecond = ComputeTowardKinectSpeed(prev.handLeft, curr.handLeft, deltaSeconds);
            metrics.RightHandTowardKinectSpeedMetersPerSecond = ComputeTowardKinectSpeed(prev.handRight, curr.handRight, deltaSeconds);
            metrics.LeftShoulderToHandDistanceMeters = Distance(curr.shoulderLeft, curr.handLeft);
            metrics.RightShoulderToHandDistanceMeters = Distance(curr.shoulderRight, curr.handRight);
            metrics.LeftArmExtensionNormalized = metrics.LeftShoulderToHandDistanceMeters / currentFrame.ShoulderWidthMeters;
            metrics.RightArmExtensionNormalized = metrics.RightShoulderToHandDistanceMeters / currentFrame.ShoulderWidthMeters;
            return true;
        }

        private static bool HasUsableArmJoints(ArmTrackingJointCollection previous, ArmTrackingJointCollection current)
        {
            return IsUsable(previous.handLeft) &&
                   IsUsable(previous.handRight) &&
                   IsUsable(current.shoulderLeft) &&
                   IsUsable(current.elbowLeft) &&
                   IsUsable(current.handLeft) &&
                   IsUsable(current.shoulderRight) &&
                   IsUsable(current.elbowRight) &&
                   IsUsable(current.handRight);
        }

        private static bool IsUsable(ArmTrackingJointSample joint)
        {
            return joint != null && joint.state != (int)ArmTrackingJointState.NotTracked;
        }

        private static double ComputeSpeed(ArmTrackingJointSample previous, ArmTrackingJointSample current, double deltaSeconds)
        {
            return Distance(previous, current) / deltaSeconds;
        }

        private static double ComputeTowardKinectSpeed(ArmTrackingJointSample previous, ArmTrackingJointSample current, double deltaSeconds)
        {
            return (previous.z - current.z) / deltaSeconds;
        }

        private static double ComputeElbowAngle(ArmTrackingJointSample shoulder, ArmTrackingJointSample elbow, ArmTrackingJointSample hand)
        {
            if (shoulder == null || elbow == null || hand == null)
            {
                return double.NaN;
            }

            Vector3 first = new Vector3(shoulder.x - elbow.x, shoulder.y - elbow.y, shoulder.z - elbow.z);
            Vector3 second = new Vector3(hand.x - elbow.x, hand.y - elbow.y, hand.z - elbow.z);
            double firstLength = first.Length;
            double secondLength = second.Length;
            if (firstLength <= double.Epsilon || secondLength <= double.Epsilon)
            {
                return double.NaN;
            }

            double dot = (first.X * second.X) + (first.Y * second.Y) + (first.Z * second.Z);
            double cosine = dot / (firstLength * secondLength);
            if (cosine > 1.0)
            {
                cosine = 1.0;
            }
            else if (cosine < -1.0)
            {
                cosine = -1.0;
            }

            return Math.Acos(cosine) * (180.0 / Math.PI);
        }

        private static double Distance(ArmTrackingJointSample first, ArmTrackingJointSample second)
        {
            if (first == null || second == null)
            {
                return double.NaN;
            }

            double dx = first.x - second.x;
            double dy = first.y - second.y;
            double dz = first.z - second.z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private struct Vector3
        {
            public Vector3(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X;
            public double Y;
            public double Z;

            public double Length
            {
                get { return Math.Sqrt((X * X) + (Y * Y) + (Z * Z)); }
            }
        }
    }

    public sealed class ArmTrackingStateMachine
    {
        private readonly ArmFrameNormalizer _normalizer;
        private readonly ArmMotionMetricsCalculator _metricsCalculator;
        private readonly ConnectionWatchdog _connectionWatchdog;
        private string _currentSessionId;
        private long _lastAcceptedFrameId = long.MinValue;
        private long? _currentTrackingId;
        private ArmTrackingFrame _previousTrackedFrame;

        public ArmTrackingStateMachine(TimeSpan connectionTimeout)
            : this(new ArmFrameNormalizer(), new ArmMotionMetricsCalculator(), new ConnectionWatchdog(connectionTimeout))
        {
        }

        public ArmTrackingStateMachine(ArmFrameNormalizer normalizer, ArmMotionMetricsCalculator metricsCalculator, ConnectionWatchdog connectionWatchdog)
        {
            _normalizer = normalizer ?? new ArmFrameNormalizer();
            _metricsCalculator = metricsCalculator ?? new ArmMotionMetricsCalculator();
            _connectionWatchdog = connectionWatchdog ?? new ConnectionWatchdog(TimeSpan.FromSeconds(1));
        }

        public ArmTrackingFrame LatestFrame { get; private set; }

        public long LastAcceptedFrameId
        {
            get { return _lastAcceptedFrameId; }
        }

        public string CurrentSessionId
        {
            get { return _currentSessionId; }
        }

        public long? CurrentTrackingId
        {
            get { return _currentTrackingId; }
        }

        public ConnectionWatchdog ConnectionWatchdog
        {
            get { return _connectionWatchdog; }
        }

        public bool TryApply(ArmTrackingPacket packet, DateTime acceptedAtUtc, out ArmTrackingFrame frame, out string logMessage)
        {
            frame = null;
            logMessage = null;

            if (!IsPacketUsable(packet, out logMessage))
            {
                return false;
            }

            bool sessionChanged = !string.Equals(_currentSessionId, packet.sessionId, StringComparison.Ordinal);
            if (sessionChanged)
            {
                ResetForSession(packet.sessionId);
                logMessage = "Bridge の sessionId が切り替わりました。";
            }

            if (packet.sessionId == _currentSessionId && packet.frameId <= _lastAcceptedFrameId)
            {
                logMessage = "古い frameId を受信したため破棄しました。";
                return false;
            }

            if (!packet.tracked)
            {
                _currentTrackingId = null;
                _previousTrackedFrame = null;

                frame = CreateFrameFromPacket(packet, tracked: false);
                LatestFrame = frame;
                _lastAcceptedFrameId = packet.frameId;
                _connectionWatchdog.MarkAccepted(acceptedAtUtc);
                return true;
            }

            if (!packet.trackingId.HasValue)
            {
                logMessage = "tracked:true ですが trackingId がありません。";
                return false;
            }

            if (!_currentTrackingId.HasValue || _currentTrackingId.Value != packet.trackingId.Value)
            {
                _previousTrackedFrame = null;
                _currentTrackingId = packet.trackingId.Value;
                logMessage = "trackingId が切り替わったため履歴をリセットしました。";
            }

            if (packet.joints == null)
            {
                logMessage = "tracked:true ですが joints がありません。";
                return false;
            }

            frame = CreateFrameFromPacket(packet, tracked: true);

            ArmTrackingNormalizationResult normalization = _normalizer.TryNormalize(packet.joints);
            frame.IsNormalizedValid = normalization.Success;
            frame.NormalizationError = normalization.ErrorMessage;
            frame.ShoulderCenter = normalization.ShoulderCenter;
            frame.NormalizationOrigin = normalization.NormalizationOrigin;
            frame.ShoulderWidthMeters = normalization.ShoulderWidthMeters;
            frame.NormalizedJoints = normalization.NormalizedJoints;

            if (_previousTrackedFrame != null && frame.IsNormalizedValid && _previousTrackedFrame.IsNormalizedValid)
            {
                ArmTrackingMetrics metrics;
                string metricsError;
                if (_metricsCalculator.TryCalculate(_previousTrackedFrame, frame, out metrics, out metricsError))
                {
                    frame.Metrics = metrics;
                }
                else
                {
                    logMessage = metricsError;
                }
            }

            _previousTrackedFrame = frame;
            LatestFrame = frame;
            _lastAcceptedFrameId = packet.frameId;
            _connectionWatchdog.MarkAccepted(acceptedAtUtc);
            return true;
        }

        public bool IsConnected(DateTime nowUtc)
        {
            return _connectionWatchdog.IsConnected(nowUtc);
        }

        public void Reset()
        {
            _currentSessionId = null;
            _currentTrackingId = null;
            _lastAcceptedFrameId = long.MinValue;
            _previousTrackedFrame = null;
            LatestFrame = null;
            _connectionWatchdog.Reset();
        }

        private void ResetForSession(string sessionId)
        {
            _currentSessionId = sessionId;
            _currentTrackingId = null;
            _lastAcceptedFrameId = long.MinValue;
            _previousTrackedFrame = null;
            LatestFrame = null;
        }

        private static bool IsPacketUsable(ArmTrackingPacket packet, out string logMessage)
        {
            logMessage = null;
            if (packet == null)
            {
                logMessage = "パケットが null です。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(packet.sessionId))
            {
                logMessage = "sessionId がありません。";
                return false;
            }

            if (packet.frameId < 0)
            {
                logMessage = "frameId が不正です。";
                return false;
            }

            if (packet.timestampMs < 0)
            {
                logMessage = "timestampMs が不正です。";
                return false;
            }

            return true;
        }

        private static ArmTrackingFrame CreateFrameFromPacket(ArmTrackingPacket packet, bool tracked)
        {
            return new ArmTrackingFrame
            {
                SessionId = packet.sessionId,
                FrameId = packet.frameId,
                TimestampMs = packet.timestampMs,
                Tracked = tracked,
                TrackingId = packet.trackingId,
                RawJoints = packet.joints == null ? null : packet.joints.Clone(),
                NormalizedJoints = null,
                IsNormalizedValid = false,
                NormalizationError = null,
                ShoulderCenter = null,
                NormalizationOrigin = null,
                ShoulderWidthMeters = 0f,
                Metrics = null
            };
        }
    }
}
