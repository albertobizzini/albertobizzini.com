using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class TaxonomyDiscoveryService(OllamaClient ollamaClient)
{
    private const int CheckpointVersion = 3;

    private const string DiscoverySystemPrompt = """
        Sei un tassonomista editoriale. Analizza citazioni italiane e inglesi e proponi macro-temi
        utili alla navigazione di un archivio pubblico. Restituisci esclusivamente JSON valido.
        I codici devono essere snake_case inglesi, stabili e concisi. Evita categorie duplicate,
        nomi di autori, titoli di libri e temi applicabili a una sola citazione. I temi devono
        descrivere il contenuto, non la forma retorica o il genere: non usare categorie come
        filosofia, metafora, esperienza, citazione o altro. Preferisci concetti navigabili come
        identità, relazioni, potere, conoscenza, tempo, lavoro, libertà, paura o cambiamento.
        """;

    private const string ReductionSystemPrompt = """
        Sei un tassonomista editoriale. Riduci e deduplica un elenco di temi candidati mantenendo
        la varietà semantica. Restituisci esclusivamente un oggetto JSON con la proprietà topics.
        Produci da 8 a 12 temi di contenuto. Non usare categorie ombrello come filosofia,
        psicologia, esperienza o altro; non usare forme retoriche come metafora. Non creare temi
        legati a un singolo libro o episodio. Ogni tema deve avere code, nameIt, nameEn,
        descriptionIt, descriptionEn ed exampleClippingIds.
        """;

    private const string ConsolidationSystemPrompt = """
        Sei un tassonomista editoriale. Consolida temi candidati derivati da citazioni italiane e
        inglesi. Restituisci esclusivamente JSON valido con le proprietà name, description e topics.
        Ogni tema deve avere code, nameIt, nameEn, descriptionIt, descriptionEn ed
        exampleClippingIds. I codici devono essere snake_case inglesi e univoci. Mantieni solo gli
        ID di esempio ricevuti, massimo cinque per tema. Le descrizioni devono chiarire i confini
        del tema e ridurre le sovrapposizioni. I temi devono descrivere contenuti e non generi,
        discipline, forme retoriche o categorie residuali. Non usare filosofia, psicologia,
        esperienza, metafora, citazione o altro come temi.
        """;

    private static readonly (string Name, int Minimum, int Maximum)[] Variants =
    [
        ("compact", 10, 12),
        ("balanced", 15, 18),
        ("detailed", 20, 25)
    ];

    public async Task<DiscoveryReport> DiscoverAsync(
        IReadOnlyCollection<AiClipping> corpus,
        string model,
        int sampleSize,
        int batchSize,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var stopwatch = Stopwatch.StartNew();
        var sample = DeterministicSampler.SelectRepresentative(corpus, sampleSize, "taxonomy-discovery-v2");
        var batches = sample.Chunk(batchSize).ToList();
        var checkpointPath = Path.Combine(
            outputDirectory,
            $"taxonomy-discovery-{SafeName(model)}-checkpoint.json");
        var checkpoint = await LoadCheckpointAsync(
            checkpointPath,
            model,
            batchSize,
            sample,
            cancellationToken);
        var candidates = checkpoint?.CandidateTopics ?? [];
        var reducedCandidates = checkpoint?.ReducedCandidateTopics ?? [];
        var completedReductionBatches = checkpoint?.CompletedReductionBatches ?? 0;
        var completedVariants = checkpoint?.CompletedVariants ?? [];
        var firstBatch = checkpoint?.CompletedBatches ?? 0;

        if (firstBatch > 0)
            Console.WriteLine($"Ripresa dal checkpoint: {firstBatch}/{batches.Count} batch già completati.");

        for (var index = firstBatch; index < batches.Count; index++)
        {
            Console.WriteLine($"Discovery batch {index + 1}/{batches.Count}...");
            var prompt = BuildDiscoveryPrompt(batches[index]);
            var response = await ollamaClient.GenerateJsonAsync<CandidateTopicSet>(
                model,
                DiscoverySystemPrompt,
                prompt,
                cancellationToken,
                OllamaJsonSchemas.TopicSet(5, 10));
            candidates.AddRange(response.Value.Topics);
            await SaveCheckpointAsync(
                checkpointPath,
                new DiscoveryCheckpoint
                {
                    Version = CheckpointVersion,
                    Model = model,
                    BatchSize = batchSize,
                    CompletedBatches = index + 1,
                    SampleClippingIds = sample.Select(x => x.Id).ToList(),
                    CandidateTopics = candidates,
                    CompletedReductionBatches = completedReductionBatches,
                    ReducedCandidateTopics = reducedCandidates,
                    CompletedVariants = completedVariants
                },
                cancellationToken);
        }

        var reductionBatches = candidates.Chunk(35).ToList();
        for (var index = completedReductionBatches; index < reductionBatches.Count; index++)
        {
            Console.WriteLine($"Riduzione candidati {index + 1}/{reductionBatches.Count}...");
            var response = await ollamaClient.GenerateRawJsonAsync(
                model,
                ReductionSystemPrompt,
                BuildReductionPrompt(reductionBatches[index]),
                cancellationToken,
                OllamaJsonSchemas.TopicSet(8, 12));
            var rawReductionPath = Path.Combine(
                outputDirectory,
                $"taxonomy-discovery-{SafeName(model)}-reduction-{index + 1}.json");
            await File.WriteAllTextAsync(rawReductionPath, response.Json, cancellationToken);
            var reduction = TaxonomyResponseParser.Parse(response.Json, $"reduction-{index + 1}");
            TaxonomyValidator.ValidateVariant(reduction, $"reduction-{index + 1}");

            reducedCandidates.AddRange(reduction.Topics);
            completedReductionBatches = index + 1;
            await SaveCheckpointAsync(
                checkpointPath,
                CreateCheckpoint(
                    model,
                    batchSize,
                    batches.Count,
                    sample,
                    candidates,
                    completedReductionBatches,
                    reducedCandidates,
                    completedVariants),
                cancellationToken);
        }

        Console.WriteLine(
            $"Consolidamento di {reducedCandidates.Count} temi intermedi " +
            $"(derivati da {candidates.Count} candidati)...");
        var taxonomies = new TaxonomyVariants();
        taxonomies.Variants.AddRange(completedVariants);
        foreach (var variant in Variants)
        {
            if (taxonomies.Variants.Any(x =>
                    x.Name.Equals(variant.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Tassonomia {variant.Name} caricata dal checkpoint.");
                continue;
            }

            Console.WriteLine(
                $"Generazione tassonomia {variant.Name} ({variant.Minimum}-{variant.Maximum} temi)...");
            var response = await ollamaClient.GenerateRawJsonAsync(
                model,
                ConsolidationSystemPrompt,
                BuildConsolidationPrompt(reducedCandidates, variant),
                cancellationToken,
                OllamaJsonSchemas.TaxonomyVariant(variant.Minimum, variant.Maximum));
            var rawResponsePath = Path.Combine(
                outputDirectory,
                $"taxonomy-discovery-{SafeName(model)}-{variant.Name}-raw.json");
            await File.WriteAllTextAsync(rawResponsePath, response.Json, cancellationToken);
            var normalized = TaxonomyResponseParser.Parse(response.Json, variant.Name);
            TaxonomyValidator.ValidateVariant(normalized, variant.Name);
            if (normalized.Topics.Count < variant.Minimum ||
                normalized.Topics.Count > variant.Maximum)
            {
                Console.WriteLine(
                    $"Avviso: il modello ha proposto {normalized.Topics.Count} temi per '{variant.Name}' " +
                    $"anziché {variant.Minimum}-{variant.Maximum}. La proposta viene conservata per la " +
                    "valutazione editoriale.");
            }

            taxonomies.Variants.Add(normalized);
            await SaveCheckpointAsync(
                checkpointPath,
                CreateCheckpoint(
                    model,
                    batchSize,
                    batches.Count,
                    sample,
                    candidates,
                    completedReductionBatches,
                    reducedCandidates,
                    taxonomies.Variants),
                cancellationToken);
        }

        TaxonomyValidator.Validate(taxonomies);

        return new DiscoveryReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Model = model,
            CorpusSize = corpus.Count,
            SampleSize = sample.Count,
            BatchSize = batchSize,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
            SampleClippingIds = sample.Select(x => x.Id).ToList(),
            SampleClippings = sample,
            Taxonomies = taxonomies
        };
    }

    private static string BuildDiscoveryPrompt(IEnumerable<AiClipping> clippings)
    {
        var input = clippings.Select(x => new
        {
            x.Id,
            x.Language,
            x.Text
        });

        return """
            Proponi da 5 a 10 macro-temi rappresentati nel seguente batch. Un tema può coprire più
            citazioni. Restituisci un oggetto con la proprietà topics. Ogni elemento deve contenere:
            code, nameIt, nameEn, descriptionIt, descriptionEn ed exampleClippingIds (massimo 5).

            Citazioni:
            """ + JsonSerializer.Serialize(input, JsonDefaults.Options);
    }

    private static string BuildConsolidationPrompt(
        IEnumerable<CandidateTopic> candidates,
        (string Name, int Minimum, int Maximum) variant)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            $"Genera esclusivamente la variante '{variant.Name}' con un numero di temi compreso " +
            $"fra {variant.Minimum} e {variant.Maximum}.");
        builder.AppendLine(variant.Name switch
        {
            "compact" => "Usa pochi concetti ampi ma concreti, senza categorie residuali o formali.",
            "balanced" => "Usa macro-temi distinti e immediatamente utili come filtri di navigazione.",
            "detailed" => "Usa temi più specifici e vari, senza limitarti a rinominare quelli più ampi.",
            _ => string.Empty
        });
        builder.AppendLine($"La proprietà name deve essere esattamente '{variant.Name}'.");
        builder.AppendLine("Temi candidati da consolidare:");
        builder.Append(JsonSerializer.Serialize(candidates, JsonDefaults.Options));
        return builder.ToString();
    }

    private static string BuildReductionPrompt(IEnumerable<CandidateTopic> candidates) =>
        "Temi candidati da ridurre e deduplicare:\n" +
        JsonSerializer.Serialize(candidates, JsonDefaults.Options);

    private static DiscoveryCheckpoint CreateCheckpoint(
        string model,
        int batchSize,
        int completedBatches,
        IReadOnlyCollection<AiClipping> sample,
        List<CandidateTopic> candidates,
        int completedReductionBatches,
        List<CandidateTopic> reducedCandidates,
        List<TaxonomyVariant> completedVariants) =>
        new()
        {
            Version = CheckpointVersion,
            Model = model,
            BatchSize = batchSize,
            CompletedBatches = completedBatches,
            SampleClippingIds = sample.Select(x => x.Id).ToList(),
            CandidateTopics = candidates,
            CompletedReductionBatches = completedReductionBatches,
            ReducedCandidateTopics = reducedCandidates,
            CompletedVariants = completedVariants
        };

    private static async Task<DiscoveryCheckpoint?> LoadCheckpointAsync(
        string path,
        string model,
        int batchSize,
        IReadOnlyCollection<AiClipping> sample,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        var checkpoint = await JsonSerializer.DeserializeAsync<DiscoveryCheckpoint>(
            stream,
            JsonDefaults.Options,
            cancellationToken);
        var sampleIds = sample.Select(x => x.Id);
        if (checkpoint is null ||
            !checkpoint.Model.Equals(model, StringComparison.Ordinal) ||
            checkpoint.BatchSize != batchSize ||
            !checkpoint.SampleClippingIds.SequenceEqual(sampleIds))
        {
            Console.WriteLine("Checkpoint ignorato perché non è compatibile con la configurazione corrente.");
            return null;
        }

        if (checkpoint.Version == 2)
        {
            Console.WriteLine(
                "Checkpoint v2 migrato: i batch di discovery vengono conservati; " +
                "riduzioni e tassonomie non valide saranno rigenerate.");
            return new DiscoveryCheckpoint
            {
                Version = CheckpointVersion,
                Model = checkpoint.Model,
                BatchSize = checkpoint.BatchSize,
                CompletedBatches = checkpoint.CompletedBatches,
                SampleClippingIds = checkpoint.SampleClippingIds,
                CandidateTopics = checkpoint.CandidateTopics
            };
        }

        if (checkpoint.Version != CheckpointVersion)
        {
            Console.WriteLine("Checkpoint ignorato perché appartiene a una versione non supportata.");
            return null;
        }

        return checkpoint;
    }

    private static async Task SaveCheckpointAsync(
        string path,
        DiscoveryCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                checkpoint,
                JsonDefaults.Options,
                cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }

    private static string SafeName(string value) =>
        string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
}
