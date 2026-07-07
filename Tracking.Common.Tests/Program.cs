using System;
using KinectBridge.Tracking;

namespace KinectBridge.Tracking.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            TestRunner runner = new TestRunner();
            runner.Run("JSON roundtrip", TestJsonRoundtrip);
            runner.Run("wire decoder accepts tracked packet", TestWireDecoderAcceptsTrackedPacket);
            runner.Run("wire decoder rejects invalid sessionId", TestWireDecoderRejectsInvalidSessionId);
            runner.Run("wire decoder rejects tracked:false extras", TestWireDecoderRejectsTrackedFalseExtras);
            runner.Run("wire decoder rejects invalid UTF-8", TestWireDecoderRejectsInvalidUtf8);
            runner.Run("tracked:false omits optional fields", TestTrackedFalseOmission);
            runner.Run("normalization", TestNormalization);
            runner.Run("normalization rejects tiny shoulders", TestNormalizationRejectsTinyShoulders);
            runner.Run("normalization keeps raw joints intact", TestNormalizationPreservesRawJoints);
            runner.Run("elbow angle and speed", TestMetrics);
            runner.Run("connection timeout", TestConnectionTimeout);
            runner.Run("tracked:false handling", TestTrackedFalseHandling);
            runner.PrintSummary();
            return runner.FailedCount == 0 ? 0 : 1;
        }

        private static void TestJsonRoundtrip()
        {
            ArmTrackingPacket packet = BuildTrackedPacket();
            string json = ArmTrackingPacketSerializer.SerializeToJson(packet);
            ArmTrackingPacket roundtrip = ArmTrackingPacketSerializer.DeserializeFromJson(json);

            Assert.Equal(ProtocolConstants.CurrentVersion, roundtrip.version, "version");
            Assert.Equal(packet.sessionId, roundtrip.sessionId, "sessionId");
            Assert.Equal(packet.frameId, roundtrip.frameId, "frameId");
            Assert.Equal(packet.timestampMs, roundtrip.timestampMs, "timestampMs");
            Assert.True(roundtrip.tracked, "tracked");
            Assert.Equal(packet.trackingId, roundtrip.trackingId, "trackingId");
            Assert.NotNull(roundtrip.joints, "joints");
            Assert.Equal(packet.joints.handLeft.x, roundtrip.joints.handLeft.x, "handLeft.x");
        }

        private static void TestWireDecoderAcceptsTrackedPacket()
        {
            string sessionId = Guid.NewGuid().ToString("D");
            string json = BuildWirePacketJson(sessionId, true, 42, true);

            WirePacketParseResult result;
            bool ok = WirePacketDecoder.TryParseJson(json, out result);
            Assert.True(ok, "wire parse");
            Assert.NotNull(result, "parse result");
            Assert.NotNull(result.Packet, "packet");
            Assert.Equal(sessionId, result.Packet.sessionId, "sessionId");
            Assert.True(result.Packet.tracked, "tracked");
            Assert.Equal(42L, result.Packet.trackingId.Value, "trackingId");
            Assert.NotNull(result.Packet.joints, "joints");
            Assert.Equal(0f, result.Packet.joints.shoulderCenter.x, 0.0001f, "shoulderCenter.x");
        }

        private static void TestWireDecoderRejectsInvalidSessionId()
        {
            string json = BuildWirePacketJson("session-1", true, 42, true);

            WirePacketParseResult result;
            bool ok = WirePacketDecoder.TryParseJson(json, out result);
            Assert.False(ok, "wire parse should fail");
            Assert.NotNull(result, "parse result");
            Assert.Equal(WirePacketValidationError.InvalidSessionId, result.Error, "error");
            Assert.NotNull(result.ErrorMessage, "error message");
        }

        private static void TestWireDecoderRejectsTrackedFalseExtras()
        {
            string json = BuildWirePacketJson(Guid.NewGuid().ToString("D"), false, 99, true);

            WirePacketParseResult result;
            bool ok = WirePacketDecoder.TryParseJson(json, out result);
            Assert.False(ok, "wire parse should fail");
            Assert.NotNull(result, "parse result");
            Assert.Equal(WirePacketValidationError.UnexpectedTrackedFalseFields, result.Error, "error");
        }

        private static void TestWireDecoderRejectsInvalidUtf8()
        {
            WirePacketParseResult result;
            bool ok = WirePacketDecoder.TryParseUtf8(new byte[] { 0xC3, 0x28 }, out result);
            Assert.False(ok, "wire parse should fail");
            Assert.NotNull(result, "parse result");
            Assert.Equal(WirePacketValidationError.InvalidUtf8, result.Error, "error");
        }

        private static void TestTrackedFalseOmission()
        {
            ArmTrackingPacket packet = new ArmTrackingPacket
            {
                sessionId = "session-1",
                frameId = 2,
                timestampMs = 20,
                tracked = false,
                trackingId = null,
                joints = null
            };

            string json = ArmTrackingPacketSerializer.SerializeToJson(packet);
            Assert.True(json.Contains("\"version\":1"), "version emitted");
            Assert.False(json.Contains("\"trackingId\""), "trackingId omitted");
            Assert.False(json.Contains("\"joints\""), "joints omitted");
            Assert.True(json.Contains("\"tracked\":false"), "tracked emitted");
        }

        private static void TestNormalization()
        {
            ArmTrackingJointCollection raw = BuildJointCollection();
            ArmFrameNormalizer normalizer = new ArmFrameNormalizer(new ArmTrackingNormalizationOptions
            {
                MinimumShoulderWidthMeters = 0.05f
            });

            ArmTrackingNormalizationResult result = normalizer.TryNormalize(raw);
            Assert.True(result.Success, "normalization success");
            Assert.NotNull(result.NormalizationOrigin, "normalization origin");
            Assert.Equal(0f, result.ShoulderCenter.x, 0.0001f, "center.x");
            Assert.Equal(0f, result.ShoulderCenter.y, 0.0001f, "center.y");
            Assert.Equal(0f, result.ShoulderCenter.z, 0.0001f, "center.z");
            Assert.Equal(0f, result.NormalizedJoints.shoulderCenter.x, 0.0001f, "normalized shoulderCenter.x");
            Assert.Equal(0f, result.NormalizedJoints.shoulderCenter.y, 0.0001f, "normalized shoulderCenter.y");
            Assert.Equal(0f, result.NormalizedJoints.shoulderCenter.z, 0.0001f, "normalized shoulderCenter.z");
            Assert.Equal(0f, result.NormalizedJoints.spine.x, 0.0001f, "normalized spine.x");
            Assert.Equal(0f, result.NormalizedJoints.hipCenter.x, 0.0001f, "normalized hipCenter.x");
            Assert.Equal(0.4f, result.ShoulderWidthMeters, 0.0001f, "shoulder width");
            Assert.Equal(-0.5f, result.NormalizedJoints.shoulderLeft.x, 0.0001f, "normalized left shoulder");
            Assert.Equal(0.5f, result.NormalizedJoints.shoulderRight.x, 0.0001f, "normalized right shoulder");
            Assert.Equal(-0.75f, result.NormalizedJoints.elbowLeft.x, 0.0001f, "normalized left elbow x");
            Assert.Equal(-1.5f, result.NormalizedJoints.handLeft.y, 0.0001f, "normalized left hand y");
            Assert.Equal(1.0, Distance(result.NormalizedJoints.shoulderLeft, result.NormalizedJoints.shoulderRight), 0.0001, "normalized shoulder distance");
        }

        private static void TestNormalizationRejectsTinyShoulders()
        {
            ArmTrackingJointCollection tinyShoulders = BuildJointCollection();
            tinyShoulders.shoulderLeft.x = -0.01f;
            tinyShoulders.shoulderRight.x = 0.01f;

            ArmFrameNormalizer normalizer = new ArmFrameNormalizer(new ArmTrackingNormalizationOptions
            {
                MinimumShoulderWidthMeters = 0.05f
            });

            ArmTrackingNormalizationResult result = normalizer.TryNormalize(tinyShoulders);
            Assert.False(result.Success, "normalization should fail");
            Assert.NotNull(result.ErrorMessage, "normalization error");
            Assert.Null(result.NormalizedJoints, "normalized joints should be null");
        }

        private static void TestNormalizationPreservesRawJoints()
        {
            ArmTrackingJointCollection raw = BuildJointCollection();
            float originalShoulderCenterY = raw.shoulderCenter.y;
            float originalHandLeftX = raw.handLeft.x;

            ArmFrameNormalizer normalizer = new ArmFrameNormalizer();
            ArmTrackingNormalizationResult result = normalizer.TryNormalize(raw);

            Assert.True(result.Success, "normalization success");
            Assert.Equal(originalShoulderCenterY, raw.shoulderCenter.y, 0.0001f, "raw shoulderCenter preserved");
            Assert.Equal(originalHandLeftX, raw.handLeft.x, 0.0001f, "raw handLeft preserved");
            Assert.Equal(1.4f, result.NormalizationOrigin.y, 0.0001f, "origin keeps raw midpoint");
        }

        private static void TestMetrics()
        {
            ArmTrackingFrame previous = new ArmTrackingFrame
            {
                SessionId = "session-1",
                FrameId = 1,
                TimestampMs = 100,
                Tracked = true,
                TrackingId = 77,
                RawJoints = BuildJointCollection(),
                IsNormalizedValid = true,
                ShoulderWidthMeters = 0.4f
            };

            ArmTrackingFrame current = new ArmTrackingFrame
            {
                SessionId = "session-1",
                FrameId = 2,
                TimestampMs = 200,
                Tracked = true,
                TrackingId = 77,
                RawJoints = BuildShiftedJointCollection(),
                IsNormalizedValid = true,
                ShoulderWidthMeters = 0.4f
            };

            ArmMotionMetricsCalculator calculator = new ArmMotionMetricsCalculator();
            ArmTrackingMetrics metrics;
            string error;
            bool ok = calculator.TryCalculate(previous, current, out metrics, out error);
            Assert.True(ok, "metrics calculation");
            Assert.NotNull(metrics, "metrics");
            Assert.True(metrics.LeftHandSpeedMetersPerSecond > 0.0, "left hand speed");
            Assert.True(metrics.LeftHandTowardKinectSpeedMetersPerSecond > 0.0, "toward Kinect speed");
            Assert.True(metrics.LeftArmExtensionNormalized > 0.0, "extension");
            Assert.True(metrics.LeftElbowAngleDegrees > 0.0, "elbow angle");
            Assert.True(metrics.LeftHandForwardFromShoulderMeters > 0.0, "left forward from shoulder");
        }

        private static void TestConnectionTimeout()
        {
            ConnectionWatchdog watchdog = new ConnectionWatchdog(TimeSpan.FromMilliseconds(1000));
            DateTime accepted = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc);
            watchdog.MarkAccepted(accepted);
            Assert.True(watchdog.IsConnected(accepted.AddMilliseconds(999)), "connected before timeout");
            Assert.False(watchdog.IsConnected(accepted.AddMilliseconds(1001)), "disconnected after timeout");
        }

        private static void TestTrackedFalseHandling()
        {
            ArmTrackingStateMachine machine = new ArmTrackingStateMachine(TimeSpan.FromMilliseconds(1000));
            DateTime now = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc);

            bool accepted = machine.TryApply(new ArmTrackingPacket
            {
                sessionId = "session-a",
                frameId = 1,
                timestampMs = 100,
                tracked = true,
                trackingId = 15,
                joints = BuildJointCollection()
            }, now, out ArmTrackingFrame trackedFrame, out string message);

            Assert.True(accepted, "tracked packet accepted");
            Assert.True(trackedFrame.Tracked, "tracked frame");
            Assert.Equal(15L, trackedFrame.TrackingId.Value, "trackingId");

            accepted = machine.TryApply(new ArmTrackingPacket
            {
                sessionId = "session-a",
                frameId = 2,
                timestampMs = 200,
                tracked = false,
                trackingId = null,
                joints = null
            }, now.AddMilliseconds(16), out ArmTrackingFrame untrackedFrame, out message);

            Assert.True(accepted, "tracked:false packet accepted");
            Assert.False(untrackedFrame.Tracked, "untracked frame");
            Assert.Null(untrackedFrame.TrackingId, "trackingId cleared");
            Assert.Null(untrackedFrame.RawJoints, "joints cleared");
        }

        private static string BuildWirePacketJson(string sessionId, bool tracked, long? trackingId, bool includeJoints)
        {
            string json = "{\"version\":1,\"sessionId\":\"" + sessionId + "\",\"frameId\":10,\"timestampMs\":20,\"tracked\":" + (tracked ? "true" : "false");

            if (trackingId.HasValue)
            {
                json += ",\"trackingId\":" + trackingId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (includeJoints)
            {
                json += ",\"joints\":" + BuildWireJointCollectionJson();
            }

            return json + "}";
        }

        private static string BuildWireJointCollectionJson()
        {
            return "{"
                + "\"shoulderCenter\":" + BuildWireJointJson(0f, 1.4f, 2.0f) + ","
                + "\"spine\":" + BuildWireJointJson(0f, 1.2f, 2.0f) + ","
                + "\"hipCenter\":" + BuildWireJointJson(0f, 1.0f, 2.0f) + ","
                + "\"shoulderLeft\":" + BuildWireJointJson(-0.2f, 1.4f, 2.0f) + ","
                + "\"elbowLeft\":" + BuildWireJointJson(-0.3f, 1.1f, 2.0f) + ","
                + "\"wristLeft\":" + BuildWireJointJson(-0.35f, 0.85f, 2.0f) + ","
                + "\"handLeft\":" + BuildWireJointJson(-0.4f, 0.8f, 2.0f) + ","
                + "\"shoulderRight\":" + BuildWireJointJson(0.2f, 1.4f, 2.0f) + ","
                + "\"elbowRight\":" + BuildWireJointJson(0.3f, 1.1f, 2.0f) + ","
                + "\"wristRight\":" + BuildWireJointJson(0.35f, 0.85f, 2.0f) + ","
                + "\"handRight\":" + BuildWireJointJson(0.4f, 0.8f, 2.0f)
                + "}";
        }

        private static string BuildWireJointJson(float x, float y, float z)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{{\"x\":{0},\"y\":{1},\"z\":{2},\"state\":2}}",
                x,
                y,
                z);
        }

        private static ArmTrackingPacket BuildTrackedPacket()
        {
            return new ArmTrackingPacket
            {
                sessionId = "session-1",
                frameId = 1,
                timestampMs = 10,
                tracked = true,
                trackingId = 42,
                joints = BuildJointCollection()
            };
        }

        private static ArmTrackingJointCollection BuildJointCollection()
        {
            return new ArmTrackingJointCollection
            {
                shoulderCenter = new ArmTrackingJointSample { x = 0f, y = 1.4f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                spine = new ArmTrackingJointSample { x = 0f, y = 1.2f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                hipCenter = new ArmTrackingJointSample { x = 0f, y = 1.0f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                shoulderLeft = new ArmTrackingJointSample { x = -0.2f, y = 1.4f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                elbowLeft = new ArmTrackingJointSample { x = -0.3f, y = 1.1f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                wristLeft = new ArmTrackingJointSample { x = -0.35f, y = 0.85f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                handLeft = new ArmTrackingJointSample { x = -0.4f, y = 0.8f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                shoulderRight = new ArmTrackingJointSample { x = 0.2f, y = 1.4f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                elbowRight = new ArmTrackingJointSample { x = 0.3f, y = 1.1f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                wristRight = new ArmTrackingJointSample { x = 0.35f, y = 0.85f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked },
                handRight = new ArmTrackingJointSample { x = 0.4f, y = 0.8f, z = 2.0f, state = (int)ArmTrackingJointState.Tracked }
            };
        }

        private static ArmTrackingJointCollection BuildShiftedJointCollection()
        {
            return new ArmTrackingJointCollection
            {
                shoulderCenter = new ArmTrackingJointSample { x = 0f, y = 1.4f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                spine = new ArmTrackingJointSample { x = 0f, y = 1.2f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                hipCenter = new ArmTrackingJointSample { x = 0f, y = 1.0f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                shoulderLeft = new ArmTrackingJointSample { x = -0.2f, y = 1.4f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                elbowLeft = new ArmTrackingJointSample { x = -0.3f, y = 1.1f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                wristLeft = new ArmTrackingJointSample { x = -0.35f, y = 0.85f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                handLeft = new ArmTrackingJointSample { x = -0.35f, y = 0.8f, z = 1.85f, state = (int)ArmTrackingJointState.Tracked },
                shoulderRight = new ArmTrackingJointSample { x = 0.2f, y = 1.4f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                elbowRight = new ArmTrackingJointSample { x = 0.3f, y = 1.1f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                wristRight = new ArmTrackingJointSample { x = 0.35f, y = 0.85f, z = 1.9f, state = (int)ArmTrackingJointState.Tracked },
                handRight = new ArmTrackingJointSample { x = 0.35f, y = 0.8f, z = 1.85f, state = (int)ArmTrackingJointState.Tracked }
            };
        }

        private static double Distance(ArmTrackingJointSample first, ArmTrackingJointSample second)
        {
            double dx = first.x - second.x;
            double dy = first.y - second.y;
            double dz = first.z - second.z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }

        private sealed class TestRunner
        {
            private int _passed;
            private int _failed;

            public int FailedCount
            {
                get { return _failed; }
            }

            public void Run(string name, Action test)
            {
                try
                {
                    test();
                    _passed++;
                    Console.WriteLine("[PASS] " + name);
                }
                catch (Exception ex)
                {
                    _failed++;
                    Console.WriteLine("[FAIL] " + name);
                    Console.WriteLine(ex.Message);
                }
            }

            public void PrintSummary()
            {
                Console.WriteLine();
                Console.WriteLine("Passed: " + _passed);
                Console.WriteLine("Failed: " + _failed);
            }
        }

        private static class Assert
        {
            public static void True(bool condition, string name)
            {
                if (!condition)
                {
                    throw new InvalidOperationException("Assert True failed: " + name);
                }
            }

            public static void False(bool condition, string name)
            {
                if (condition)
                {
                    throw new InvalidOperationException("Assert False failed: " + name);
                }
            }

            public static void Null(object value, string name)
            {
                if (value != null)
                {
                    throw new InvalidOperationException("Assert Null failed: " + name);
                }
            }

            public static void NotNull(object value, string name)
            {
                if (value == null)
                {
                    throw new InvalidOperationException("Assert NotNull failed: " + name);
                }
            }

            public static void Equal<T>(T expected, T actual, string name)
            {
                if (!object.Equals(expected, actual))
                {
                    throw new InvalidOperationException("Assert Equal failed: " + name + " expected=" + expected + " actual=" + actual);
                }
            }

            public static void Equal(float expected, float actual, float tolerance, string name)
            {
                if (Math.Abs(expected - actual) > tolerance)
                {
                    throw new InvalidOperationException("Assert Equal failed: " + name + " expected=" + expected + " actual=" + actual);
                }
            }

            public static void Equal(double expected, double actual, double tolerance, string name)
            {
                if (Math.Abs(expected - actual) > tolerance)
                {
                    throw new InvalidOperationException("Assert Equal failed: " + name + " expected=" + expected + " actual=" + actual);
                }
            }
        }
    }
}
