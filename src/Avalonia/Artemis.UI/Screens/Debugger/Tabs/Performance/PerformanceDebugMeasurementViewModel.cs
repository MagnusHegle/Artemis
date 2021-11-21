﻿using Artemis.Core;
using Artemis.UI.Shared;
using ReactiveUI;

namespace Artemis.UI.Screens.Debugger.Tabs.Performance
{
    public class PerformanceDebugMeasurementViewModel : ViewModelBase
    {
        private string? _average;
        private string? _last;
        private string? _max;
        private string? _min;
        private string? _percentile;

        public PerformanceDebugMeasurementViewModel(ProfilingMeasurement measurement)
        {
            Measurement = measurement;
        }

        public ProfilingMeasurement Measurement { get; }

        public string? Last
        {
            get => _last;
            set => this.RaiseAndSetIfChanged(ref _last, value);
        }

        public string? Average
        {
            get => _average;
            set => this.RaiseAndSetIfChanged(ref _average, value);
        }

        public string? Min
        {
            get => _min;
            set => this.RaiseAndSetIfChanged(ref _min, value);
        }

        public string? Max
        {
            get => _max;
            set => this.RaiseAndSetIfChanged(ref _max, value);
        }

        public string? Percentile
        {
            get => _percentile;
            set => this.RaiseAndSetIfChanged(ref _percentile, value);
        }

        public void Update()
        {
            Last = Measurement.GetLast().TotalMilliseconds + " ms";
            Average = Measurement.GetAverage().TotalMilliseconds + " ms";
            Min = Measurement.GetMin().TotalMilliseconds + " ms";
            Max = Measurement.GetMax().TotalMilliseconds + " ms";
            Percentile = Measurement.GetPercentile(0.95).TotalMilliseconds + " ms";
        }
    }
}