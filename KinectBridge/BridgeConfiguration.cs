using System;
using Microsoft.Kinect;

namespace KinectBridge
{
    public sealed class BridgeConfiguration
    {
        public string TargetAddress { get; set; } = "127.0.0.1";

        public int TargetPort { get; set; } = 5005;

        public TimeSpan SensorRescanInterval { get; set; } = TimeSpan.FromSeconds(2);

        public TimeSpan FrameMissingLogInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan RepeatedStateLogInterval { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DiagnosticSummaryInterval { get; set; } = TimeSpan.FromSeconds(1);

        public float Smoothing { get; set; } = 0.7f;

        public float Correction { get; set; } = 0.3f;

        public float Prediction { get; set; } = 0.5f;

        public float JitterRadius { get; set; } = 0.05f;

        public float MaxDeviationRadius { get; set; } = 0.04f;

        public TransformSmoothParameters ToTransformSmoothParameters()
        {
            TransformSmoothParameters parameters = new TransformSmoothParameters();
            parameters.Smoothing = Smoothing;
            parameters.Correction = Correction;
            parameters.Prediction = Prediction;
            parameters.JitterRadius = JitterRadius;
            parameters.MaxDeviationRadius = MaxDeviationRadius;
            return parameters;
        }

        public static BridgeConfiguration CreateDefault()
        {
            return new BridgeConfiguration();
        }
    }
}
