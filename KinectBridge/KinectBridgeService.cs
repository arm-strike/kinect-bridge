using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Kinect;
using KinectBridge.Tracking;

namespace KinectBridge
{
    public sealed class KinectBridgeService : IDisposable
    {
        private sealed class DiagnosticCounters
        {
            public long SkeletonEvents;
            public long OpenedFrames;
            public long NullFrames;
            public long SkeletonFrames;
            public long NoTrackedBodyFrames;
            public long TrackedBodyFrames;
            public long TrackedPackets;
            public long UntrackedPackets;
            public long UdpSent;
            public long UdpErrors;
            public long JsonSerializationErrors;
        }

        private readonly BridgeConfiguration _configuration;
        private readonly BridgeLogger _logger;
        private readonly UdpPacketSender _sender;
        private readonly Stopwatch _stopwatch;
        private readonly Timer _rescanTimer;
        private readonly Timer _diagnosticTimer;
        private readonly object _sensorGate = new object();
        private readonly TimeSpan _statusLogInterval;
        private readonly DiagnosticCounters _counters = new DiagnosticCounters();

        private KinectSensor _sensor;
        private long _frameId;
        private string _sessionId;
        private bool _started;
        private bool _disposed;
        private bool _missingSensorLogged;
        private bool _waitingForReconnect;
        private bool _hasConnectedSensor;
        private bool _hasLoggedFirstSkeletonFrame;
        private bool _hasLoggedFirstTrackedPerson;
        private bool _isTrackingPerson;
        private int? _lastTrackingId;
        private bool _hasLoggedFirstTrackedPacketJson;
        private bool _hasLoggedFirstUntrackedPacketJson;
        private bool _hasLoggedFirstUdpSuccess;
        private bool _hasUdpError;

        public KinectBridgeService(BridgeConfiguration configuration, BridgeLogger logger)
        {
            _configuration = configuration ?? BridgeConfiguration.CreateDefault();
            _logger = logger ?? new BridgeLogger();
            _sender = new UdpPacketSender(_configuration.TargetAddress, _configuration.TargetPort);
            _stopwatch = Stopwatch.StartNew();
            _statusLogInterval = _configuration.RepeatedStateLogInterval;
            _rescanTimer = new Timer(RescanTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            _diagnosticTimer = new Timer(DiagnosticTimerTick, null, Timeout.Infinite, Timeout.Infinite);
            _sessionId = Guid.NewGuid().ToString("D");
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _logger.Info("Kinect Bridge を起動しました。");
            _logger.Info("sessionId = " + _sessionId);
            _logger.Info("UDP送信先 = " + _sender.TargetDescription);

            KinectSensor.KinectSensors.StatusChanged += OnSensorsStatusChanged;
            _rescanTimer.Change(TimeSpan.Zero, _configuration.SensorRescanInterval);
            _diagnosticTimer.Change(_configuration.DiagnosticSummaryInterval, _configuration.DiagnosticSummaryInterval);
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            _started = false;
            _rescanTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _diagnosticTimer.Change(Timeout.Infinite, Timeout.Infinite);
            KinectSensor.KinectSensors.StatusChanged -= OnSensorsStatusChanged;

            lock (_sensorGate)
            {
                DetachSensorLocked();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Stop();
            _diagnosticTimer.Dispose();
            _rescanTimer.Dispose();
            _sender.Dispose();
        }

        private void OnSensorsStatusChanged(object sender, StatusChangedEventArgs e)
        {
            _logger.InfoThrottled("sensor-status-" + e.Status, "Kinect センサー状態変化: " + e.Status, _statusLogInterval);
            EnsureSensorBinding();
        }

        private void RescanTimerTick(object state)
        {
            EnsureSensorBinding();
        }

        private void DiagnosticTimerTick(object state)
        {
            try
            {
                if (!_started)
                {
                    return;
                }

                if (!_hasConnectedSensor && Interlocked.Read(ref _counters.SkeletonEvents) == 0)
                {
                    return;
                }

                _logger.Debug(BuildDiagnosticSummary());
            }
            catch (Exception ex)
            {
                _logger.ErrorThrottled("diagnostic-summary", "診断サマリーログの出力に失敗しました: " + ex.Message, _statusLogInterval);
            }
        }

        private void EnsureSensorBinding()
        {
            if (!_started)
            {
                return;
            }

            lock (_sensorGate)
            {
                if (_sensor != null)
                {
                    if (_sensor.Status == KinectStatus.Connected)
                    {
                        _missingSensorLogged = false;
                        return;
                    }

                    _logger.Warn("Kinect センサー切断: " + _sensor.Status);
                    BeginReconnectWaitLocked();
                    DetachSensorLocked();
                }

                KinectSensor available = KinectSensor.KinectSensors.FirstOrDefault(sensor => sensor != null && sensor.Status == KinectStatus.Connected);
                if (available == null)
                {
                    if (!_waitingForReconnect)
                    {
                        if (!_missingSensorLogged)
                        {
                            _logger.Warn("Kinect センサーが見つかりません。接続を待機します。");
                            _missingSensorLogged = true;
                        }
                    }
                    else if (!_missingSensorLogged)
                    {
                        _logger.Info("センサー切断後の再検出開始");
                        _missingSensorLogged = true;
                    }

                    return;
                }

                AttachSensorLocked(available);
            }
        }

        private void AttachSensorLocked(KinectSensor sensor)
        {
            try
            {
                _sensor = sensor;
                _sensor.SkeletonStream.Enable(_configuration.ToTransformSmoothParameters());
                _sensor.SkeletonFrameReady += OnSkeletonFrameReady;
                _sensor.Start();

                if (_waitingForReconnect)
                {
                    _logger.Info("センサー再接続成功");
                }
                else if (!_hasConnectedSensor)
                {
                    _logger.Info("Kinect センサー接続");
                }

                _logger.Info("Kinect センサーを Connected として取得しました。");
                _logger.Info("Skeleton Stream開始成功");
                _hasConnectedSensor = true;
                _waitingForReconnect = false;
                _missingSensorLogged = false;
            }
            catch (Exception ex)
            {
                _logger.Error("Skeleton Stream開始失敗: " + ex.Message);
                DetachSensorLocked();
            }
        }

        private void BeginReconnectWaitLocked()
        {
            _waitingForReconnect = true;
            _missingSensorLogged = false;
        }

        private void DetachSensorLocked()
        {
            if (_sensor == null)
            {
                return;
            }

            try
            {
                _sensor.SkeletonFrameReady -= OnSkeletonFrameReady;
            }
            catch
            {
            }

            try
            {
                _sensor.Stop();
            }
            catch
            {
            }

            _sensor = null;
        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Interlocked.Increment(ref _counters.SkeletonEvents);

            try
            {
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame == null)
                    {
                        Interlocked.Increment(ref _counters.NullFrames);
                        _logger.WarnThrottled("skeleton-frame-null", "OpenSkeletonFrame() が null を返しました。", _configuration.FrameMissingLogInterval);
                        return;
                    }

                    Interlocked.Increment(ref _counters.OpenedFrames);
                    if (!_hasLoggedFirstSkeletonFrame)
                    {
                        _hasLoggedFirstSkeletonFrame = true;
                        _logger.Info("初めてSkeletonフレームを取得しました。");
                    }

                    Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);
                    Interlocked.Increment(ref _counters.SkeletonFrames);

                    Skeleton trackedSkeleton = SelectClosestTrackedSkeleton(skeletons);
                    UpdateTrackingState(trackedSkeleton);

                    long nextFrameId = Interlocked.Increment(ref _frameId);
                    long timestampMs = _stopwatch.ElapsedMilliseconds;
                    ArmTrackingPacket packet = CreatePacket(trackedSkeleton, nextFrameId, timestampMs);

                    if (packet.tracked)
                    {
                        Interlocked.Increment(ref _counters.TrackedPackets);
                    }
                    else
                    {
                        Interlocked.Increment(ref _counters.UntrackedPackets);
                    }

                    string json;
                    try
                    {
                        json = ArmTrackingPacketSerializer.SerializeToJson(packet);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _counters.JsonSerializationErrors);
                        _logger.ErrorThrottled("json-serialize", "JSONシリアライズに失敗しました: " + ex.Message, _statusLogInterval);
                        return;
                    }

                    LogFirstPacketJsonIfNeeded(packet, json);

                    try
                    {
                        _sender.SendJson(json);
                        Interlocked.Increment(ref _counters.UdpSent);

                        if (!_hasLoggedFirstUdpSuccess)
                        {
                            _hasLoggedFirstUdpSuccess = true;
                            _logger.Info("UDP送信が初めて成功しました。");
                        }

                        if (_hasUdpError)
                        {
                            _hasUdpError = false;
                            _logger.Info("UDP送信がエラー状態から復旧しました。");
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _counters.UdpErrors);
                        if (!_hasUdpError)
                        {
                            _hasUdpError = true;
                            _logger.Error("UDP送信で初めてエラーが発生しました: " + ex.Message);
                        }
                        else
                        {
                            _logger.ErrorThrottled("udp-send-error", "UDP送信に失敗しました: " + ex.Message, _statusLogInterval);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorThrottled("skeleton-frame-exception", "SkeletonFrameReady 処理で例外が発生しました: " + ex, _statusLogInterval);
            }
        }

        private void UpdateTrackingState(Skeleton trackedSkeleton)
        {
            if (trackedSkeleton == null)
            {
                Interlocked.Increment(ref _counters.NoTrackedBodyFrames);
                if (_isTrackingPerson)
                {
                    _isTrackingPerson = false;
                    _lastTrackingId = null;
                    _logger.Info("人物を見失った");
                }

                return;
            }

            Interlocked.Increment(ref _counters.TrackedBodyFrames);
            if (!_hasLoggedFirstTrackedPerson)
            {
                _hasLoggedFirstTrackedPerson = true;
                _logger.Info("初めて人物を追跡しました。");
            }

            _isTrackingPerson = true;
            if (!_lastTrackingId.HasValue || _lastTrackingId.Value != trackedSkeleton.TrackingId)
            {
                _logger.Info("trackingId が変化しました: " + FormatTrackingId(_lastTrackingId) + " -> " + trackedSkeleton.TrackingId);
                _lastTrackingId = trackedSkeleton.TrackingId;
            }
        }

        private ArmTrackingPacket CreatePacket(Skeleton trackedSkeleton, long frameId, long timestampMs)
        {
            if (trackedSkeleton == null)
            {
                return KinectSkeletonPacketFactory.CreateUntrackedPacket(_sessionId, frameId, timestampMs);
            }

            return KinectSkeletonPacketFactory.CreateTrackedPacket(trackedSkeleton, _sessionId, frameId, timestampMs);
        }

        private void LogFirstPacketJsonIfNeeded(ArmTrackingPacket packet, string json)
        {
            if (!packet.tracked && !_hasLoggedFirstUntrackedPacketJson)
            {
                _hasLoggedFirstUntrackedPacketJson = true;
                _logger.Debug("最初の tracked:false JSON = " + json);
            }

            if (packet.tracked && !_hasLoggedFirstTrackedPacketJson)
            {
                _hasLoggedFirstTrackedPacketJson = true;
                _logger.Debug("最初の tracked:true JSON = " + json);
            }
        }

        private string BuildDiagnosticSummary()
        {
            return "skeletonEvents=" + Interlocked.Read(ref _counters.SkeletonEvents) +
                   ", openedFrames=" + Interlocked.Read(ref _counters.OpenedFrames) +
                   ", nullFrames=" + Interlocked.Read(ref _counters.NullFrames) +
                   ", skeletonFrames=" + Interlocked.Read(ref _counters.SkeletonFrames) +
                   ", noTrackedBodyFrames=" + Interlocked.Read(ref _counters.NoTrackedBodyFrames) +
                   ", trackedBodyFrames=" + Interlocked.Read(ref _counters.TrackedBodyFrames) +
                   ", trackedPackets=" + Interlocked.Read(ref _counters.TrackedPackets) +
                   ", untrackedPackets=" + Interlocked.Read(ref _counters.UntrackedPackets) +
                   ", udpSent=" + Interlocked.Read(ref _counters.UdpSent) +
                   ", udpErrors=" + Interlocked.Read(ref _counters.UdpErrors) +
                   ", jsonSerializationErrors=" + Interlocked.Read(ref _counters.JsonSerializationErrors);
        }

        private static string FormatTrackingId(int? trackingId)
        {
            return trackingId.HasValue ? trackingId.Value.ToString() : "(none)";
        }

        private static Skeleton SelectClosestTrackedSkeleton(Skeleton[] skeletons)
        {
            Skeleton closest = null;
            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton == null || skeleton.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (closest == null || skeleton.Position.Z < closest.Position.Z)
                {
                    closest = skeleton;
                }
            }

            return closest;
        }
    }
}
