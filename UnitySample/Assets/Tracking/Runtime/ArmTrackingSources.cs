using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using CommonArmTrackingFrame = KinectBridge.Tracking.ArmTrackingFrame;
using CommonArmTrackingPacket = KinectBridge.Tracking.ArmTrackingPacket;
using CommonArmTrackingStateMachine = KinectBridge.Tracking.ArmTrackingStateMachine;
using CommonWirePacketDecoder = KinectBridge.Tracking.WirePacketDecoder;
using CommonWirePacketParseResult = KinectBridge.Tracking.WirePacketParseResult;

namespace Tracking.Runtime
{
    public sealed class UdpArmTrackingSource : IArmTrackingSource, IArmTrackingSourceLifecycle, IArmTrackingSourceTickable
    {
        private readonly CommonArmTrackingStateMachine _stateMachine;
        private readonly LogThrottle _logThrottle = new LogThrottle();
        private readonly object _packetGate = new object();
        private Thread _receiveThread;
        private UdpClient _udpClient;
        private volatile bool _running;
        private byte[] _latestPayload;
        private bool _hasPendingPayload;
        private ArmTrackingFrame _latestFrame;
        private string _lastReceiveError;
        private string _listenAddress;
        private int _listenPort;

        public UdpArmTrackingSource(string listenAddress, int listenPort, TimeSpan connectionTimeout)
        {
            _listenAddress = string.IsNullOrEmpty(listenAddress) ? "127.0.0.1" : listenAddress;
            _listenPort = listenPort;
            _stateMachine = new CommonArmTrackingStateMachine(connectionTimeout);
        }

        public bool IsConnected
        {
            get { return _running && _stateMachine.ConnectionWatchdog.IsConnected(DateTime.UtcNow); }
        }

        public bool IsTracked
        {
            get { return Status == TrackingSourceStatus.Tracking || Status == TrackingSourceStatus.TrackingButNormalizationInvalid; }
        }

        public TrackingSourceStatus Status
        {
            get { return ResolveStatus(_running, _stateMachine, _latestFrame); }
        }

        public string LastReceiveError
        {
            get { return _lastReceiveError; }
        }

        public ArmTrackingFrame LatestFrame
        {
            get { return _latestFrame; }
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(_listenAddress), _listenPort));
            _running = true;
            _latestFrame = null;
            _lastReceiveError = null;
            _stateMachine.Reset();
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
            Debug.Log("[Tracking] UDP 受信を開始しました: " + _listenAddress + ":" + _listenPort);
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                }
            }
            catch
            {
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1000);
            }

            _receiveThread = null;
            if (_udpClient != null)
            {
                _udpClient.Dispose();
                _udpClient = null;
            }

            lock (_packetGate)
            {
                _latestPayload = null;
                _hasPendingPayload = false;
            }

            _latestFrame = null;
            _lastReceiveError = null;
            _stateMachine.Reset();
        }

        public void Tick(float deltaTime)
        {
            byte[] payload = null;
            lock (_packetGate)
            {
                if (_hasPendingPayload)
                {
                    payload = _latestPayload;
                    _latestPayload = null;
                    _hasPendingPayload = false;
                }
            }

            if (payload == null)
            {
                return;
            }

            CommonWirePacketParseResult parseResult;
            if (!CommonWirePacketDecoder.TryParseUtf8(payload, out parseResult))
            {
                _lastReceiveError = parseResult.ErrorMessage;
                WarnOnce("wire-parse-" + parseResult.Error, parseResult.ErrorMessage, true);
                return;
            }

            CommonArmTrackingPacket packet = parseResult.Packet;

            CommonArmTrackingFrame frame;
            string logMessage;
            bool accepted = _stateMachine.TryApply(packet, DateTime.UtcNow, out frame, out logMessage);
            if (!accepted)
            {
                if (!string.IsNullOrEmpty(logMessage))
                {
                    WarnOnce("reject-" + logMessage, logMessage, true);
                }

                return;
            }

            _latestFrame = frame.ToRuntime();
            _lastReceiveError = null;

            if (!string.IsNullOrEmpty(logMessage))
            {
                InfoOnce("state-" + logMessage, logMessage);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    IPEndPoint endPoint = null;
                    byte[] payload = _udpClient.Receive(ref endPoint);
                    lock (_packetGate)
                    {
                        _latestPayload = payload;
                        _hasPendingPayload = true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (!_running)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    WarnOnce("udp-receive", "UDP受信に失敗しました: " + ex.Message, true);
                }
            }
        }

        private static TrackingSourceStatus ResolveStatus(bool running, CommonArmTrackingStateMachine stateMachine, ArmTrackingFrame latestFrame)
        {
            if (!running || !stateMachine.ConnectionWatchdog.IsConnected(DateTime.UtcNow))
            {
                return TrackingSourceStatus.Disconnected;
            }

            if (latestFrame == null || !latestFrame.Tracked)
            {
                return TrackingSourceStatus.NoPerson;
            }

            return latestFrame.IsNormalizedValid ? TrackingSourceStatus.Tracking : TrackingSourceStatus.TrackingButNormalizationInvalid;
        }

        private void InfoOnce(string key, string message)
        {
            if (_logThrottle.ShouldLog(key, message, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.Log("[Tracking] " + message);
            }
        }

        private void WarnOnce(string key, string message, bool alwaysThrottle = false)
        {
            if (!alwaysThrottle || _logThrottle.ShouldLog(key, message, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.LogWarning("[Tracking] " + message);
            }
        }
    }

    public sealed class KeyboardArmTrackingSource : IArmTrackingSource, IArmTrackingSourceLifecycle, IArmTrackingSourceTickable
    {
        private readonly CommonArmTrackingStateMachine _stateMachine;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private string _sessionId = "keyboard-" + Guid.NewGuid().ToString("N");
        private readonly LogThrottle _logThrottle = new LogThrottle();
        private readonly float _moveSpeedMetersPerSecond;
        private readonly float _depthSpeedMetersPerSecond;
        private readonly float _baseShoulderWidthMeters;
        private readonly float _baseShoulderHeightMeters;
        private readonly float _baseHandForwardMeters;
        private readonly Vector3 _bodyCenter;
        private long _frameId;
        private bool _started;
        private ArmTrackingJointCollection _currentRawJoints;
        private ArmTrackingFrame _latestFrame;

        public KeyboardArmTrackingSource(Vector3 bodyCenter, float baseShoulderWidthMeters, float moveSpeedMetersPerSecond, float depthSpeedMetersPerSecond, TimeSpan connectionTimeout)
        {
            _stateMachine = new CommonArmTrackingStateMachine(connectionTimeout);
            _bodyCenter = bodyCenter;
            _baseShoulderWidthMeters = Mathf.Max(0.1f, baseShoulderWidthMeters);
            _baseShoulderHeightMeters = 0.18f;
            _baseHandForwardMeters = 0.25f;
            _moveSpeedMetersPerSecond = moveSpeedMetersPerSecond;
            _depthSpeedMetersPerSecond = depthSpeedMetersPerSecond;
            _currentRawJoints = BuildDefaultPose();
        }

        public bool IsConnected
        {
            get { return _started; }
        }

        public bool IsTracked
        {
            get { return Status == TrackingSourceStatus.Tracking || Status == TrackingSourceStatus.TrackingButNormalizationInvalid; }
        }

        public TrackingSourceStatus Status
        {
            get { return ResolveStatus(_started, _stateMachine, _latestFrame); }
        }

        public string LastReceiveError
        {
            get { return null; }
        }

        public ArmTrackingFrame LatestFrame
        {
            get { return _latestFrame; }
        }

        public void Start()
        {
            _started = true;
            _frameId = 0;
            _stopwatch.Restart();
            _sessionId = "keyboard-" + Guid.NewGuid().ToString("N");
            _currentRawJoints = BuildDefaultPose();
            _latestFrame = null;
            _stateMachine.Reset();
            Debug.Log("[Tracking] KeyboardArmTrackingSource を開始しました。");
        }

        public void Stop()
        {
            _started = false;
            _latestFrame = null;
            _stateMachine.Reset();
        }

        public void Tick(float deltaTime)
        {
            if (!_started)
            {
                return;
            }

            UpdateTargets(deltaTime);
            ArmTrackingPacket packet = CreatePacketFromPose();
            CommonArmTrackingFrame frame;
            string logMessage;
            bool accepted = _stateMachine.TryApply(packet.ToCommon(), DateTime.UtcNow, out frame, out logMessage);
            if (!accepted)
            {
                if (!string.IsNullOrEmpty(logMessage))
                {
                    WarnOnce("keyboard-state-" + logMessage, logMessage, true);
                }

                return;
            }

            _latestFrame = frame.ToRuntime();
            if (!string.IsNullOrEmpty(logMessage))
            {
                InfoOnce("keyboard-state-" + logMessage, logMessage);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void UpdateTargets(float deltaTime)
        {
            float moveStep = _moveSpeedMetersPerSecond * deltaTime;
            float depthStep = _depthSpeedMetersPerSecond * deltaTime;

            MoveLeftHand(moveStep, depthStep);
            MoveRightHand(moveStep, depthStep);

            _currentRawJoints = RecomputePose(_currentRawJoints);
        }

        private void MoveLeftHand(float moveStep, float depthStep)
        {
            if (Input.GetKey(KeyCode.A))
            {
                _currentRawJoints.handLeft.x -= moveStep;
            }
            if (Input.GetKey(KeyCode.D))
            {
                _currentRawJoints.handLeft.x += moveStep;
            }
            if (Input.GetKey(KeyCode.W))
            {
                _currentRawJoints.handLeft.y += moveStep;
            }
            if (Input.GetKey(KeyCode.S))
            {
                _currentRawJoints.handLeft.y -= moveStep;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                _currentRawJoints.handLeft.z -= depthStep;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _currentRawJoints.handLeft.z += depthStep;
            }
        }

        private void MoveRightHand(float moveStep, float depthStep)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _currentRawJoints.handRight.x -= moveStep;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                _currentRawJoints.handRight.x += moveStep;
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                _currentRawJoints.handRight.y += moveStep;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                _currentRawJoints.handRight.y -= moveStep;
            }
            if (Input.GetKey(KeyCode.PageUp))
            {
                _currentRawJoints.handRight.z -= depthStep;
            }
            if (Input.GetKey(KeyCode.PageDown))
            {
                _currentRawJoints.handRight.z += depthStep;
            }
        }

        private ArmTrackingJointCollection BuildDefaultPose()
        {
            ArmTrackingJointCollection joints = new ArmTrackingJointCollection();
            joints.shoulderCenter = new ArmTrackingJointSample { x = _bodyCenter.x, y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.shoulderLeft = new ArmTrackingJointSample { x = _bodyCenter.x - (_baseShoulderWidthMeters * 0.5f), y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.shoulderRight = new ArmTrackingJointSample { x = _bodyCenter.x + (_baseShoulderWidthMeters * 0.5f), y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.handLeft = new ArmTrackingJointSample { x = joints.shoulderLeft.x - 0.25f, y = joints.shoulderLeft.y - 0.15f, z = joints.shoulderLeft.z + _baseHandForwardMeters, state = (int)ArmTrackingJointState.Tracked };
            joints.handRight = new ArmTrackingJointSample { x = joints.shoulderRight.x + 0.25f, y = joints.shoulderRight.y - 0.15f, z = joints.shoulderRight.z + _baseHandForwardMeters, state = (int)ArmTrackingJointState.Tracked };
            return RecomputePose(joints);
        }

        private ArmTrackingJointCollection RecomputePose(ArmTrackingJointCollection joints)
        {
            if (joints == null)
            {
                joints = new ArmTrackingJointCollection();
            }

            joints.shoulderCenter = new ArmTrackingJointSample { x = _bodyCenter.x, y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.shoulderLeft = new ArmTrackingJointSample { x = _bodyCenter.x - (_baseShoulderWidthMeters * 0.5f), y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.shoulderRight = new ArmTrackingJointSample { x = _bodyCenter.x + (_baseShoulderWidthMeters * 0.5f), y = _bodyCenter.y + _baseShoulderHeightMeters, z = _bodyCenter.z, state = (int)ArmTrackingJointState.Tracked };
            joints.elbowLeft = BuildElbow(joints.shoulderLeft, joints.handLeft, -1f);
            joints.elbowRight = BuildElbow(joints.shoulderRight, joints.handRight, 1f);
            joints.wristLeft = BuildWrist(joints.elbowLeft, joints.handLeft);
            joints.wristRight = BuildWrist(joints.elbowRight, joints.handRight);
            joints.handLeft.state = (int)ArmTrackingJointState.Tracked;
            joints.handRight.state = (int)ArmTrackingJointState.Tracked;
            return joints;
        }

        private ArmTrackingJointSample BuildElbow(ArmTrackingJointSample shoulder, ArmTrackingJointSample hand, float outwardSign)
        {
            float midX = (shoulder.x + hand.x) * 0.5f;
            float midY = (shoulder.y + hand.y) * 0.5f;
            float midZ = (shoulder.z + hand.z) * 0.5f;
            return new ArmTrackingJointSample
            {
                x = midX + (0.12f * outwardSign),
                y = midY + 0.05f,
                z = midZ,
                state = (int)ArmTrackingJointState.Tracked
            };
        }

        private ArmTrackingJointSample BuildWrist(ArmTrackingJointSample elbow, ArmTrackingJointSample hand)
        {
            return new ArmTrackingJointSample
            {
                x = (elbow.x + hand.x) * 0.5f,
                y = (elbow.y + hand.y) * 0.5f,
                z = (elbow.z + hand.z) * 0.5f,
                state = (int)ArmTrackingJointState.Tracked
            };
        }

        private ArmTrackingPacket CreatePacketFromPose()
        {
            long nextFrameId = ++_frameId;
            return new ArmTrackingPacket
            {
                sessionId = _sessionId,
                frameId = nextFrameId,
                timestampMs = _stopwatch.ElapsedMilliseconds,
                tracked = true,
                trackingId = 1,
                joints = _currentRawJoints.Clone()
            };
        }

        private void InfoOnce(string key, string message)
        {
            if (_logThrottle.ShouldLog(key, message, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.Log("[Tracking] " + message);
            }
        }
    }

    public sealed class ReplayArmTrackingSource : IArmTrackingSource, IArmTrackingSourceLifecycle, IArmTrackingSourceTickable
    {
        private readonly CommonArmTrackingStateMachine _stateMachine;
        private readonly LogThrottle _logThrottle = new LogThrottle();
        private readonly TextAsset _sourceAsset;
        private readonly float _frameIntervalSeconds;
        private readonly bool _loop;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private string _sessionId = "replay-" + Guid.NewGuid().ToString("N");
        private readonly List<string> _lines = new List<string>();
        private int _index;
        private float _accumulator;
        private bool _running;
        private ArmTrackingFrame _latestFrame;
        private string _lastReceiveError;

        public ReplayArmTrackingSource(TextAsset sourceAsset, float frameIntervalSeconds, bool loop, TimeSpan connectionTimeout)
        {
            _sourceAsset = sourceAsset;
            _frameIntervalSeconds = Mathf.Max(0.01f, frameIntervalSeconds);
            _loop = loop;
            _stateMachine = new CommonArmTrackingStateMachine(connectionTimeout);
            LoadLines();
        }

        public bool IsConnected
        {
            get { return _running && _stateMachine.ConnectionWatchdog.IsConnected(DateTime.UtcNow); }
        }

        public bool IsTracked
        {
            get { return Status == TrackingSourceStatus.Tracking || Status == TrackingSourceStatus.TrackingButNormalizationInvalid; }
        }

        public TrackingSourceStatus Status
        {
            get { return ResolveStatus(_running, _stateMachine, _latestFrame); }
        }

        public string LastReceiveError
        {
            get { return _lastReceiveError; }
        }

        public ArmTrackingFrame LatestFrame
        {
            get { return _latestFrame; }
        }

        public void Start()
        {
            _running = true;
            _index = 0;
            _accumulator = 0f;
            _latestFrame = null;
            _lastReceiveError = null;
            _stateMachine.Reset();
        }

        public void Stop()
        {
            _running = false;
            _latestFrame = null;
            _lastReceiveError = null;
            _stateMachine.Reset();
        }

        public void Tick(float deltaTime)
        {
            if (!_running || _lines.Count == 0)
            {
                return;
            }

            _accumulator += deltaTime;
            if (_accumulator < _frameIntervalSeconds)
            {
                return;
            }

            _accumulator = 0f;
            if (_index >= _lines.Count)
            {
                if (_loop)
                {
                    _index = 0;
                }
                else
                {
                    return;
                }
            }

            string json = _lines[_index++];
            CommonWirePacketParseResult parseResult;
            if (!CommonWirePacketDecoder.TryParseJson(json, out parseResult))
            {
                _lastReceiveError = parseResult.ErrorMessage;
                WarnOnce("replay-wire-" + parseResult.Error, parseResult.ErrorMessage, true);
                return;
            }

            CommonArmTrackingFrame frame;
            string message;
            if (!_stateMachine.TryApply(parseResult.Packet, DateTime.UtcNow, out frame, out message))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    WarnOnce("replay-state-" + message, message, true);
                }
                return;
            }

            _latestFrame = frame.ToRuntime();
            _lastReceiveError = null;
            if (!string.IsNullOrEmpty(message))
            {
                InfoOnce("replay-state-" + message, message);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void LoadLines()
        {
            _lines.Clear();
            if (_sourceAsset == null || string.IsNullOrEmpty(_sourceAsset.text))
            {
                return;
            }

            string[] split = _sourceAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++)
            {
                string line = split[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    _lines.Add(line);
                }
            }
        }

        private static TrackingSourceStatus ResolveStatus(bool running, CommonArmTrackingStateMachine stateMachine, ArmTrackingFrame latestFrame)
        {
            if (!running || !stateMachine.ConnectionWatchdog.IsConnected(DateTime.UtcNow))
            {
                return TrackingSourceStatus.Disconnected;
            }

            if (latestFrame == null || !latestFrame.Tracked)
            {
                return TrackingSourceStatus.NoPerson;
            }

            return latestFrame.IsNormalizedValid ? TrackingSourceStatus.Tracking : TrackingSourceStatus.TrackingButNormalizationInvalid;
        }

        private void InfoOnce(string key, string message)
        {
            if (_logThrottle.ShouldLog(key, message, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.Log("[Tracking] " + message);
            }
        }

        private void WarnOnce(string key, string message, bool alwaysThrottle = false)
        {
            if (!alwaysThrottle || _logThrottle.ShouldLog(key, message, TimeSpan.FromSeconds(5), DateTime.UtcNow))
            {
                Debug.LogWarning("[Tracking] " + message);
            }
        }
    }

    public sealed class ArmTrackingSourceHost : MonoBehaviour
    {
        public enum SourceKind
        {
            Udp,
            Keyboard,
            Replay
        }

        [SerializeField]
        private SourceKind sourceKind = SourceKind.Udp;

        [SerializeField]
        private string udpListenAddress = "127.0.0.1";

        [SerializeField]
        private int udpListenPort = 5005;

        [SerializeField]
        private float connectionTimeoutSeconds = 1.0f;

        [SerializeField]
        private Vector3 keyboardBodyCenter = new Vector3(0f, 1.35f, 2.0f);

        [SerializeField]
        private float keyboardMoveSpeedMetersPerSecond = 0.75f;

        [SerializeField]
        private float keyboardDepthSpeedMetersPerSecond = 0.60f;

        [SerializeField]
        private float keyboardShoulderWidthMeters = 0.42f;

        [SerializeField]
        private TextAsset replaySource;

        [SerializeField]
        private float replayFrameIntervalSeconds = 0.033f;

        [SerializeField]
        private bool replayLoop = true;

        private IArmTrackingSource _source;
        private IArmTrackingSourceLifecycle _lifecycle;
        private IArmTrackingSourceTickable _tickable;

        public IArmTrackingSource Source
        {
            get { return _source; }
        }

        private void Awake()
        {
            BuildSource();
        }

        private void OnEnable()
        {
            if (_source == null)
            {
                BuildSource();
            }

            if (_lifecycle != null)
            {
                _lifecycle.Start();
            }
        }

        private void Update()
        {
            if (_tickable != null)
            {
                _tickable.Tick(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (_lifecycle != null)
            {
                _lifecycle.Stop();
            }

            IDisposable disposable = _source as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }

            _source = null;
            _lifecycle = null;
            _tickable = null;
        }

        private void BuildSource()
        {
            IDisposable disposable = _source as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }

            _source = null;
            _lifecycle = null;
            _tickable = null;

            switch (sourceKind)
            {
                case SourceKind.Keyboard:
                    KeyboardArmTrackingSource keyboard = new KeyboardArmTrackingSource(keyboardBodyCenter, keyboardShoulderWidthMeters, keyboardMoveSpeedMetersPerSecond, keyboardDepthSpeedMetersPerSecond, TimeSpan.FromSeconds(connectionTimeoutSeconds));
                    _source = keyboard;
                    _lifecycle = keyboard;
                    _tickable = keyboard;
                    break;
                case SourceKind.Replay:
                    ReplayArmTrackingSource replay = new ReplayArmTrackingSource(replaySource, replayFrameIntervalSeconds, replayLoop, TimeSpan.FromSeconds(connectionTimeoutSeconds));
                    _source = replay;
                    _lifecycle = replay;
                    _tickable = replay;
                    break;
                default:
                    UdpArmTrackingSource udp = new UdpArmTrackingSource(udpListenAddress, udpListenPort, TimeSpan.FromSeconds(connectionTimeoutSeconds));
                    _source = udp;
                    _lifecycle = udp;
                    _tickable = udp;
                    break;
            }
        }
    }
}
