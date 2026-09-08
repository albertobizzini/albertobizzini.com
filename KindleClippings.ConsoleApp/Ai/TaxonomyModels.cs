namespace KindleClippings.ConsoleApp.Ai;

public sealed class CandidateTopic
{
    public string Code { get; init; } = string.Empty;
    public string NameIt { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string DescriptionIt { get; init; } = string.Empty;
    public string DescriptionEn { get; init; } = string.Empty;
    public List<string> ExampleClippingIds { get; init; } = [];
}

public sealed class CandidateTopicSet
{
    public List<CandidateTopic> Topics { get; init; } = [];
}

public sealed class TaxonomyVariant
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<CandidateTopic> Topics { get; init; } = [];
}

public sealed class TaxonomyVariants
{
    public List<TaxonomyVariant> Variants { get; init; } = [];
}

public sealed class DiscoveryReport
{
    public DateTimeOffset GeneratedAt { get; init; }
    public string Model { get; init; } = string.Empty;
    public int CorpusSize { get; init; }
    public int SampleSize { get; init; }
    public int BatchSize { get; init; }
    public long DurationMilliseconds { get; init; }
    public List<string> SampleClippingIds { get; init; } = [];
    public List<AiClipping> SampleClippings { get; init; } = [];
    public TaxonomyVariants Taxonomies { get; init; } = new();
}

public sealed class DiscoveryCheckpoint
{
    public int Version { get; init; }
    public string Model { get; init; } = string.Empty;
    public int BatchSize { get; init; }
    public int CompletedBatches { get; init; }
    public List<string> SampleClippingIds { get; init; } = [];
    public List<CandidateTopic> CandidateTopics { get; init; } = [];
    public int CompletedReductionBatches { get; init; }
    public List<CandidateTopic> ReducedCandidateTopics { get; init; } = [];
    public List<TaxonomyVariant> CompletedVariants { get; init; } = [];
}

public sealed class TopicAssignment
{
    public string Code { get; init; } = string.Empty;
    public double Score { get; init; }
}

public sealed class ClippingClassification
{
    public TopicAssignment? PrimaryTopic { get; init; }
    public List<TopicAssignment> SecondaryTopics { get; init; } = [];
    public double AphorismScore { get; init; }
    public double ContextDependencyScore { get; init; }
}

public sealed class ModelClassificationResult
{
    public string Model { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public long DurationMilliseconds { get; init; }
    public string? Error { get; init; }
    public ClippingClassification? Classification { get; init; }
}

public sealed class ComparisonItem
{
    public required AiClipping Clipping { get; init; }
    public List<ModelClassificationResult> Results { get; init; } = [];
}

public sealed class ComparisonReport
{
    public DateTimeOffset GeneratedAt { get; init; }
    public string TaxonomySource { get; init; } = string.Empty;
    public string TaxonomyVariant { get; init; } = string.Empty;
    public List<string> Models { get; init; } = [];
    public int CorpusSize { get; init; }
    public int SampleSize { get; init; }
    public List<ComparisonItem> Items { get; init; } = [];
}
