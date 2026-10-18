# Istruzioni di stile per Codex — derivate da Flaskk.FLLService

## Obiettivo e applicazione

Scrivi codice con lo stile descritto qui: C# diretto, orientato agli oggetti, con nomi inglesi e documentazione inglese. Mantieni visibili il flusso operativo e le responsabilità delle classi.

Applica queste convenzioni al codice nuovo o modificato, senza riformattare file estranei alla richiesta. Rispetta le istruzioni esplicite dell'utente, le istruzioni specifiche del progetto e i formatter già configurati. In caso di incompatibilità, segnala brevemente l'adattamento.

Questo profilo riguarda soprattutto C#/.NET. In altri linguaggi conserva chiarezza, organizzazione e lingua dei commenti, adattando naming e sintassi alle convenzioni del linguaggio.

## Nomi e lingua

- Usa inglese per namespace, tipi, metodi, proprietà, campi e variabili.
- Usa PascalCase per tipi, metodi, proprietà ed enum; camelCase per parametri e variabili locali.
- Usa `_camelCase` per campi privati, incluse le dipendenze: `_logger`, `_configuration`, `_state`.
- Preferisci nomi legati al dominio, con suffissi esplicativi quando utili: `SpinCommand`, `WorkerOptions`, `StatusRegister`, `FillStateMachine`, `SendStatusJob`.
- Usa il prefisso `I` per le interfacce e `EventArgs` per gli argomenti degli eventi.
- Scrivi commenti e documentazione XML in inglese. Mantieni coerente con il progetto destinatario la lingua dei log e dei messaggi all'utente.
- Conserva nomi esterni imposti da protocolli o contratti; non propagare le loro eccezioni al naming interno.

## Formattazione C#

- Usa quattro spazi per livello e parentesi graffe in stile Allman, su righe separate.
- Nei nuovi file usa namespace file-scoped, se supportati dalla versione C# del destinatario.
- Colloca i `using` prima del namespace; evita direttive inutili e rispetta l'ordinamento del progetto.
- Separa i membri e i blocchi logici con una riga vuota, senza accumulare righe vuote.
- Usa proprietà automatiche per i dati semplici e membri expression-bodied per proprietà o metodi realmente brevi.
- Usa `new()` quando il tipo è già evidente e la versione del linguaggio lo permette.
- Alterna tipi espliciti e `var` con criterio: tipo esplicito quando chiarisce il dato; `var` quando il tipo è evidente dalla costruzione o l'espressione è complessa.
- Per uniformare le varianti presenti nel riferimento, usa le graffe nei nuovi `if`, cicli e blocchi di controllo, anche brevi.
- Evita espressioni dense e catene LINQ che nascondono passaggi importanti; usa LINQ per trasformazioni semplici e leggibili.

## Organizzazione e progettazione

- Organizza cartelle e namespace per responsabilità, usando il namespace radice del progetto destinatario. Esempi possibili: `Commands`, `Settings`, `Hardware`, `Registers`, `FSM`, `Scheduler`, `API`.
- Crea soltanto le cartelle necessarie al dominio: questa struttura è un riferimento, non uno scheletro obbligatorio.
- Mantieni di norma un tipo principale per file e fai corrispondere il nome del file al tipo. Tipi ausiliari strettamente collegati possono stare nello stesso file.
- Preferisci classi concrete e metodi espliciti. Introduci interfacce o classi base quando esiste un contratto utile, una variante reale o una necessità concreta di isolamento.
- Usa ereditarietà per comportamento condiviso effettivo, come una base comune per comandi; evita gerarchie speculative.
- Usa extension methods statici per piccole operazioni riutilizzabili che si leggono naturalmente come operazioni sul tipo esteso.
- Nei file lunghi puoi usare `#region` con titoli inglesi per gruppi coerenti, come `Private members`, `Dependency Injection`, `Properties`, `Public methods`. Non aggiungerli sistematicamente ai file brevi.
- In una classe nuova ordina normalmente campi, costruttori, proprietà/eventi, metodi pubblici e helper privati; conserva l'ordine locale quando modifichi una classe esistente.
- Evita framework aggiuntivi, livelli architetturali e astrazioni non necessari alla richiesta.

## Dipendenze, configurazione e integrazioni

- Quando il destinatario usa Dependency Injection, ricevi le dipendenze nel costruttore e conservale in campi `private readonly` quando non devono essere riassegnate.
- Usa `ILogger<T>` per il logging nei progetti che adottano Microsoft.Extensions.Logging.
- Rappresenta impostazioni correlate con classi dedicate, proprietà semplici e descrizioni XML; usa il sistema di configurazione già adottato dal destinatario.
- Mantieni composizione e registrazione dei servizi nel punto di avvio previsto dal progetto.
- Separa logica applicativa e dettagli dei dispositivi, dei protocolli e dei servizi esterni.
- Estendi codice generato tramite `partial`, adapter o file separati quando possibile, evitando modifiche manuali che la rigenerazione perderebbe.
- Non trasferire automaticamente Redis, Quartz, Newtonsoft.Json, websocket, hardware o il target .NET 6: sono scelte del progetto di riferimento, non regole stilistiche.

## Flusso operativo e documentazione

- Preferisci metodi con un compito riconoscibile e passaggi espliciti.
- Per processi realmente a stati, usa enum descrittivi e transizioni leggibili, eventualmente con uno `switch` e un metodo come `Step()`. Non introdurre una FSM per flussi semplici.
- Per notifiche locali, usa gli eventi .NET quando appropriato, con `EventHandler<TEventArgs>` e metodi `On...` se serve un punto di estensione.
- Documenta con `/// <summary>` tipi e membri pubblici con significato di dominio, configurazioni e dettagli operativi non ovvi.
- Aggiungi `<param>`, `<returns>` e `<exception>` quando aiutano a capire il contratto; mantienili allineati alla firma.
- I commenti spiegano intento, vincoli, unità di misura, timeout o protocollo. Evita commenti che ripetono semplicemente l'istruzione.
- Usa `TODO` solo per una limitazione concreta, con una descrizione comprensibile.

## Correttezza: non riprodurre le debolezze del riferimento

Le seguenti sono cautele per il riuso, non convenzioni osservate uniformemente nel progetto originale:

- Rispetta la nullability del destinatario; inizializza correttamente i membri e rappresenta esplicitamente l'assenza di valori. Non silenziare avvisi soltanto per imitare il riferimento.
- Nei flussi asincroni usa `await`, propagando il `CancellationToken` quando disponibile; evita `.Wait()` e `.Result` sulle operazioni asincrone.
- Gestisci le eccezioni dove puoi recuperare, aggiungere contesto o tradurre un errore nel contratto previsto. Non trasformare indiscriminatamente gli errori in `false`, `null` o valori predefiniti.
- Nel logging conserva eccezione e contesto, preferendo template strutturati. Non registrare token o credenziali.
- Non copiare campi pubblici mutabili, blocchi di codice commentato, abbreviazioni opache, refusi, `using` inutilizzati o incoerenze di spaziatura.
- Usa costanti o impostazioni per valori significativi; conserva valori letterali di protocollo soltanto se chiari e documentati.
- Mantieni esplicita la proprietà dello stato mutabile e delle risorse: clona i buffer quando serve uno snapshot e rilascia correttamente le risorse.
- Mantieni framework, dipendenze e funzionalità del linguaggio compatibili con il progetto destinatario.

## Esempio di stile

L'esempio presuppone C# con namespace file-scoped e le astrazioni di logging .NET già disponibili. `IStatusReader` rappresenta un contratto del progetto destinatario, non una dipendenza da aggiungere automaticamente.

```csharp
using Microsoft.Extensions.Logging;

namespace Example.Services;

/// <summary>
/// Manage the service status
/// </summary>
public class StatusService
{
    private readonly ILogger<StatusService> _logger;
    private readonly IStatusReader _reader;

    public StatusService(ILogger<StatusService> logger, IStatusReader reader)
    {
        _logger = logger;
        _reader = reader;
    }

    /// <summary>
    /// Read the current device status
    /// </summary>
    /// <param name="cancellationToken">Permit to cancel the operation.</param>
    /// <returns>Code returned by device.</returns>
    public async Task<int> ReadStatusAsync(CancellationToken cancellationToken)
    {
        int status = await _reader.ReadAsync(cancellationToken);
        _logger.LogDebug("Device status: {Status}", status);
        return status;
    }
}
```

## Verifica prima della consegna

- Controlla coerenza di naming, formattazione, documentazione e organizzazione con queste regole e il contesto locale.
- Esegui build, formatter e test pertinenti previsti dal progetto quando disponibili; indica eventuali verifiche non eseguibili.
- Non introdurre dipendenze o modifiche funzionali per il solo scopo di uniformare lo stile.
- Riassumi le modifiche e gli eventuali adattamenti necessari al destinatario.
