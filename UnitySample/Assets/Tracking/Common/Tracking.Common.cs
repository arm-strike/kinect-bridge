using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

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

    public enum TrackingSourceStatus
    {
        Disconnected = 0,
        NoPerson = 1,
        Tracking = 2,
        TrackingButNormalizationInvalid = 3
    }

    public enum WirePacketValidationError
    {
        None = 0,
        InvalidUtf8,
        InvalidJson,
        MissingVersion,
        InvalidVersion,
        MissingSessionId,
        InvalidSessionId,
        MissingFrameId,
        InvalidFrameId,
        MissingTimestampMs,
        InvalidTimestampMs,
        MissingTracked,
        MissingTrackingId,
        InvalidTrackingId,
        MissingJoints,
        MissingJointSample,
        MissingJointCoordinate,
        InvalidJointCoordinate,
        MissingJointState,
        InvalidJointState,
        UnexpectedTrackedFalseFields
    }

    [Serializable]
    [DataContract]
    public sealed class WireJointSampleDto
    {
        [DataMember(Name = "x")]
        public float? x;

        [DataMember(Name = "y")]
        public float? y;

        [DataMember(Name = "z")]
        public float? z;

        [DataMember(Name = "state")]
        public int? state;
    }

    [Serializable]
    [DataContract]
    public sealed class WireJointCollectionDto
    {
        [DataMember(Name = "shoulderCenter")]
        public WireJointSampleDto shoulderCenter;

        [DataMember(Name = "spine")]
        public WireJointSampleDto spine;

        [DataMember(Name = "hipCenter")]
        public WireJointSampleDto hipCenter;

        [DataMember(Name = "shoulderLeft")]
        public WireJointSampleDto shoulderLeft;

        [DataMember(Name = "elbowLeft")]
        public WireJointSampleDto elbowLeft;

        [DataMember(Name = "wristLeft")]
        public WireJointSampleDto wristLeft;

        [DataMember(Name = "handLeft")]
        public WireJointSampleDto handLeft;

        [DataMember(Name = "shoulderRight")]
        public WireJointSampleDto shoulderRight;

        [DataMember(Name = "elbowRight")]
        public WireJointSampleDto elbowRight;

        [DataMember(Name = "wristRight")]
        public WireJointSampleDto wristRight;

        [DataMember(Name = "handRight")]
        public WireJointSampleDto handRight;
    }

    [Serializable]
    [DataContract]
    public sealed class WirePacketDto
    {
        [DataMember(Name = "version")]
        public int? version;

        [DataMember(Name = "sessionId")]
        public string sessionId;

        [DataMember(Name = "frameId")]
        public long? frameId;

        [DataMember(Name = "timestampMs")]
        public long? timestampMs;

        [DataMember(Name = "tracked")]
        public bool? tracked;

        [DataMember(Name = "trackingId", EmitDefaultValue = false)]
        public long? trackingId;

        [DataMember(Name = "joints", EmitDefaultValue = false)]
        public WireJointCollectionDto joints;
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
    }

    [Serializable]
    public sealed class WirePacketParseResult
    {
        public bool Success { get; set; }

        public WirePacketValidationError Error { get; set; }

        public string ErrorMessage { get; set; }

        public WirePacketDto WirePacket { get; set; }

        public ArmTrackingPacket Packet { get; set; }
    }

    public static class ArmTrackingPacketSerializer
    {
        public static string SerializeToJson(ArmTrackingPacket packet)
        {
            byte[] payload = SerializeToUtf8Bytes(packet);
            return Encoding.UTF8.GetString(payload);
        }

        public static byte[] SerializeToUtf8Bytes(ArmTrackingPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArmTrackingPacket));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, packet);
                return stream.ToArray();
            }
        }

        public static ArmTrackingPacket DeserializeFromJson(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ArmTrackingPacket));
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (ArmTrackingPacket)serializer.ReadObject(stream);
            }
        }
    }

    public static class WirePacketDecoder
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(WirePacketDto));

        public static bool TryParseUtf8(byte[] payload, out WirePacketParseResult result)
        {
            result = new WirePacketParseResult();

            if (payload == null)
            {
                return Fail(result, WirePacketValidationError.InvalidUtf8, "UTF-8 データがありません。");
            }

            string json;
            try
            {
                json = StrictUtf8.GetString(payload);
            }
            catch (Exception ex)
            {
                return Fail(result, WirePacketValidationError.InvalidUtf8, "UTF-8 デコードに失敗しました: " + ex.Message);
            }

            return TryParseJson(json, out result);
        }

        public static bool TryParseJson(string json, out WirePacketParseResult result)
        {
            result = new WirePacketParseResult();

            if (json == null)
            {
                return Fail(result, WirePacketValidationError.InvalidJson, "JSON が null です。");
            }

            WirePacketDto wirePacket;
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    wirePacket = Serializer.ReadObject(stream) as WirePacketDto;
                }
            }
            catch (Exception ex)
            {
                return Fail(result, WirePacketValidationError.InvalidJson, "JSON の解析に失敗しました: " + ex.Message);
            }

            if (wirePacket == null)
            {
                return Fail(result, WirePacketValidationError.InvalidJson, "JSON の解析結果が空です。");
            }

            string errorMessage;
            WirePacketValidationError error;
            ArmTrackingPacket packet;
            if (!TryValidateAndConvert(wirePacket, out packet, out error, out errorMessage))
            {
                return Fail(result, error, errorMessage, wirePacket);
            }

            result.Success = true;
            result.Error = WirePacketValidationError.None;
            result.ErrorMessage = null;
            result.WirePacket = wirePacket;
            result.Packet = packet;
            return true;
        }

        private static bool TryValidateAndConvert(WirePacketDto wirePacket, out ArmTrackingPacket packet, out WirePacketValidationError error, out string errorMessage)
        {
            packet = null;
            error = WirePacketValidationError.None;
            errorMessage = null;

            if (wirePacket.version == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingVersion, "version がありません。");
            }

            if (wirePacket.version.Value != ProtocolConstants.CurrentVersion)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidVersion, "version が不正です。 received=" + wirePacket.version.Value + " expected=" + ProtocolConstants.CurrentVersion);
            }

            if (string.IsNullOrWhiteSpace(wirePacket.sessionId))
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingSessionId, "sessionId がありません。");
            }

            if (!Guid.TryParseExact(wirePacket.sessionId.Trim(), "D", out _))
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidSessionId, "sessionId が GUID として不正です。");
            }

            if (wirePacket.frameId == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingFrameId, "frameId がありません。");
            }

            if (wirePacket.frameId.Value < 0)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidFrameId, "frameId が不正です。");
            }

            if (wirePacket.timestampMs == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingTimestampMs, "timestampMs がありません。");
            }

            if (wirePacket.timestampMs.Value < 0)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidTimestampMs, "timestampMs が不正です。");
            }

            if (wirePacket.tracked == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingTracked, "tracked がありません。");
            }

            if (!wirePacket.tracked.Value)
            {
                if (wirePacket.trackingId.HasValue || wirePacket.joints != null)
                {
                    return Fail(out error, out errorMessage, WirePacketValidationError.UnexpectedTrackedFalseFields, "tracked:false に trackingId または joints が含まれています。");
                }

                packet = new ArmTrackingPacket
                {
                    version = wirePacket.version.Value,
                    sessionId = wirePacket.sessionId,
                    frameId = wirePacket.frameId.Value,
                    timestampMs = wirePacket.timestampMs.Value,
                    tracked = false,
                    trackingId = null,
                    joints = null
                };
                return true;
            }

            if (!wirePacket.trackingId.HasValue)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingTrackingId, "tracked:true ですが trackingId がありません。");
            }

            if (wirePacket.trackingId.Value <= 0)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidTrackingId, "trackingId が不正です。");
            }

            if (wirePacket.joints == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingJoints, "tracked:true ですが joints がありません。");
            }

            ArmTrackingJointCollection joints;
            if (!TryConvertJoints(wirePacket.joints, out joints, out error, out errorMessage))
            {
                return false;
            }

            packet = new ArmTrackingPacket
            {
                version = wirePacket.version.Value,
                sessionId = wirePacket.sessionId,
                frameId = wirePacket.frameId.Value,
                timestampMs = wirePacket.timestampMs.Value,
                tracked = true,
                trackingId = wirePacket.trackingId,
                joints = joints
            };
            return true;
        }

        private static bool TryConvertJoints(WireJointCollectionDto wireJoints, out ArmTrackingJointCollection joints, out WirePacketValidationError error, out string errorMessage)
        {
            joints = new ArmTrackingJointCollection();
            error = WirePacketValidationError.None;
            errorMessage = null;

            if (!TryConvertJoint("shoulderCenter", wireJoints.shoulderCenter, out joints.shoulderCenter, out error, out errorMessage)) return false;
            if (!TryConvertOptionalJoint(wireJoints.spine, out joints.spine, out error, out errorMessage)) return false;
            if (!TryConvertOptionalJoint(wireJoints.hipCenter, out joints.hipCenter, out error, out errorMessage)) return false;
            if (!TryConvertJoint("shoulderLeft", wireJoints.shoulderLeft, out joints.shoulderLeft, out error, out errorMessage)) return false;
            if (!TryConvertJoint("elbowLeft", wireJoints.elbowLeft, out joints.elbowLeft, out error, out errorMessage)) return false;
            if (!TryConvertJoint("wristLeft", wireJoints.wristLeft, out joints.wristLeft, out error, out errorMessage)) return false;
            if (!TryConvertJoint("handLeft", wireJoints.handLeft, out joints.handLeft, out error, out errorMessage)) return false;
            if (!TryConvertJoint("shoulderRight", wireJoints.shoulderRight, out joints.shoulderRight, out error, out errorMessage)) return false;
            if (!TryConvertJoint("elbowRight", wireJoints.elbowRight, out joints.elbowRight, out error, out errorMessage)) return false;
            if (!TryConvertJoint("wristRight", wireJoints.wristRight, out joints.wristRight, out error, out errorMessage)) return false;
            if (!TryConvertJoint("handRight", wireJoints.handRight, out joints.handRight, out error, out errorMessage)) return false;

            return true;
        }

        private static bool TryConvertOptionalJoint(WireJointSampleDto wireJoint, out ArmTrackingJointSample joint, out WirePacketValidationError error, out string errorMessage)
        {
            joint = null;
            error = WirePacketValidationError.None;
            errorMessage = null;

            if (wireJoint == null)
            {
                return true;
            }

            return TryConvertJoint("optional", wireJoint, out joint, out error, out errorMessage);
        }

        private static bool TryConvertJoint(string jointName, WireJointSampleDto wireJoint, out ArmTrackingJointSample joint, out WirePacketValidationError error, out string errorMessage)
        {
            joint = null;
            error = WirePacketValidationError.None;
            errorMessage = null;

            if (wireJoint == null)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingJointSample, jointName + " がありません。");
            }

            if (!wireJoint.x.HasValue || !wireJoint.y.HasValue || !wireJoint.z.HasValue)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingJointCoordinate, jointName + " の座標がありません。");
            }

            if (float.IsNaN(wireJoint.x.Value) || float.IsNaN(wireJoint.y.Value) || float.IsNaN(wireJoint.z.Value) ||
                float.IsInfinity(wireJoint.x.Value) || float.IsInfinity(wireJoint.y.Value) || float.IsInfinity(wireJoint.z.Value))
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidJointCoordinate, jointName + " の座標が不正です。");
            }

            if (!wireJoint.state.HasValue)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.MissingJointState, jointName + " の state がありません。");
            }

            if (wireJoint.state.Value < (int)ArmTrackingJointState.NotTracked || wireJoint.state.Value > (int)ArmTrackingJointState.Tracked)
            {
                return Fail(out error, out errorMessage, WirePacketValidationError.InvalidJointState, jointName + " の state が不正です。");
            }

            joint = new ArmTrackingJointSample
            {
                x = wireJoint.x.Value,
                y = wireJoint.y.Value,
                z = wireJoint.z.Value,
                state = wireJoint.state.Value
            };
            return true;
        }

        private static bool Fail(out WirePacketValidationError error, out string errorMessage, WirePacketValidationError reason, string message)
        {
            error = reason;
            errorMessage = message;
            return false;
        }

        private static bool Fail(WirePacketParseResult result, WirePacketValidationError error, string errorMessage, WirePacketDto wirePacket = null)
        {
            result.Success = false;
            result.Error = error;
            result.ErrorMessage = errorMessage;
            result.WirePacket = wirePacket;
            result.Packet = null;
            return false;
        }
    }

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

        private readonly System.Collections.Generic.Dictionary<string, Entry> _entries = new System.Collections.Generic.Dictionary<string, Entry>();
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
