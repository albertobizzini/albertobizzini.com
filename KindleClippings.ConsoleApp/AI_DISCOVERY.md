# AI topic discovery

Questa fase propone una tassonomia e confronta modelli locali senza modificare i clipping e
senza persistere classificazioni definitive. Le future tabelle AI risiederanno nello schema
`ai` dello stesso database `KindleClippings`; i dati originali continueranno a risiedere in `dbo`.

## Prerequisiti su Windows 11

1. Pubblicare `KindleClippings.Database` per creare lo schema `ai`.
2. Installare Ollama per Windows.
3. Scaricare i modelli configurati in `appsettings.json`:

   ```powershell
   ollama pull qwen3:8b
   ollama pull gemma3:4b
   ```

4. Verificare che SQL Server `KindleClippings` sia raggiungibile con la connection string
   configurata.

I tag dei modelli sono configurabili: prima dell'esecuzione verificarne la disponibilità nella
libreria Ollama installata. Non aggiungere al repository password o connection string personali.

## Configurazione

`appsettings.json` contiene endpoint Ollama, modelli, dimensioni dei campioni e cartella dei
report. Una cartella relativa come `AiReports` viene creata accanto all'eseguibile, per esempio
`bin\Debug\net10.0\AiReports`; all'avvio il comando ne stampa sempre il percorso assoluto. È
possibile usare un file esterno:

```powershell
dotnet run --project KindleClippings.ConsoleApp -- discover-taxonomy `
  --config C:\percorso\ai-settings.json
```

## Discovery

Il comando seleziona deterministicamente un campione bilanciato fra i libri, genera temi
candidati in batch e li consolida in tre varianti: `compact`, `balanced` e `detailed`.

```powershell
dotnet run --project KindleClippings.ConsoleApp -- discover-taxonomy
```

È possibile scegliere un modello o una cartella di output:

```powershell
dotnet run --project KindleClippings.ConsoleApp -- discover-taxonomy `
  --model qwen3:8b --output C:\Temp\AiReports
```

Il comando genera un file JSON, utilizzabile dal confronto, e un report Markdown leggibile.
I report possono contenere testo e metadati delle citazioni e non devono essere pubblicati senza
una verifica editoriale.

Dopo ogni batch viene inoltre aggiornato un file `*-checkpoint.json`. Se la generazione viene
interrotta o fallisce durante il consolidamento, una nuova esecuzione con la stessa configurazione
riprende dai batch già completati invece di richiamare nuovamente il modello per tutto il campione.
Le tre granularità vengono consolidate separatamente, così un modello non deve produrre tre
strutture complesse in una singola risposta. Anche ogni variante completata viene aggiunta al
checkpoint. Il nome della variante (`compact`, `balanced` o `detailed`) è assegnato
dall'applicazione e non dipende dalla presenza del campo `name` nella risposta del modello.

## Confronto

Il confronto usa un campione deterministico che esclude le citazioni impiegate nella discovery.
Per ogni citazione richiama tutti i modelli configurati e produce risultati affiancati.

```powershell
dotnet run --project KindleClippings.ConsoleApp -- compare-models `
  --taxonomy AiReports\taxonomy-discovery-qwen3-8b-YYYYMMDD-HHMMSS.json `
  --variant balanced
```

Il Markdown presenta i modelli come risultato A e B; la chiave è riportata alla fine. Per ogni
citazione annotare `A`, `B`, `equivalenti` oppure `entrambi errati`. Il JSON conserva inoltre
durata, validità dell'output e risultati completi per elaborazioni successive.

## Proprietà della fase

- Non usa servizi a pagamento o API remote.
- Non modifica `dbo.Clipping`.
- Non persiste ancora classificazioni definitive.
- Non usa il campione di discovery per il confronto.
- Temperatura e seed sono fissati per favorire risultati ripetibili.
- `Ctrl+C` interrompe in modo controllato il processo.
