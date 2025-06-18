using Prometheus;

namespace Shared.Utils.Metrics;

public static class MetricsRegistry
{
    // Queue metrics
    public static readonly Gauge QueueDepth = Prometheus.Metrics.CreateGauge(
        "app_queue_depth",
        "Current number of messages waiting in the queue",
        new GaugeConfiguration
        {
            LabelNames = new[] { "queue_name" }
        });

    public static readonly Counter ProcessedMessages = Prometheus.Metrics.CreateCounter(
        "app_processed_messages_total",
        "Total number of messages that have been processed by the service",
        new CounterConfiguration
        {
            LabelNames = new[] { "queue_name", "status" }
        });

    // Gossip metrics
    public static readonly Histogram GossipRtt = Prometheus.Metrics.CreateHistogram(
        "app_gossip_rtt_seconds",
        "Round-trip time for gossip protocol messages between nodes",
        new HistogramConfiguration
        {
            LabelNames = new[] { "node_id" },
            Buckets = new[] { 0.01, 0.05, 0.1, 0.5, 1.0, 2.0, 5.0 }
        });

    public static readonly Gauge ActiveNodes = Prometheus.Metrics.CreateGauge(
        "app_active_nodes",
        "Current number of active nodes in the distributed system",
        new GaugeConfiguration
        {
            LabelNames = new[] { "node_type" }
        });

    // Snapshot metrics
    public static readonly Histogram SnapshotDuration = Prometheus.Metrics.CreateHistogram(
        "app_snapshot_duration_seconds",
        "Time taken to complete distributed snapshot operations",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation" },
            Buckets = new[] { 0.1, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0 }
        });

    public static readonly Counter SnapshotOperations = Prometheus.Metrics.CreateCounter(
        "app_snapshot_operations_total",
        "Total number of distributed snapshot operations performed",
        new CounterConfiguration
        {
            LabelNames = new[] { "operation", "status" }
        });

    // Message rate metrics
    public static readonly Counter MessageRate = Prometheus.Metrics.CreateCounter(
        "app_message_rate_total",
        "Total number of messages processed per second by each service",
        new CounterConfiguration
        {
            LabelNames = new[] { "service", "message_type" }
        });
} 