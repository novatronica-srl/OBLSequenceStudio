# Editor unificato OpenBrickLight

L'editor integra le modalità della UI di programmazione negli step indipendenti dei 28 canali. Supporta configurazione offline e collegamento Modbus RTU alla scheda; le sequenze miste sono eseguite autonomamente dal firmware 2.0.

## Modifica dell'interfaccia in Visual Studio

La [mappa del codice](docs/Code-map.md) descrive modelli, servizi, controlli, localizzazione e bootloader. La guida tecnica [UI maintenance](docs/UI-maintenance.md) approfondisce responsabilità delle form, flusso dei dati, pagine del wizard e gestione delle risorse.

La finestra principale è divisa in `MainForm.cs` (comportamento, eventi e localizzazione) e `MainForm.Designer.cs` (controlli e layout). Dopo una compilazione, aprire `MainForm.cs` con **Visualizza finestra di progettazione** (`Shift+F7`) per modificare visivamente pannelli, pulsanti, griglia e separatori. Le traduzioni vengono applicate all'avvio: per modificare i testi dell'applicazione usare `Localization/Strings.json`.

Anche `Controls/FirmwareUpdateWizard.cs` può essere aperto con **Visualizza finestra di progettazione**: il layout e i controlli riutilizzabili sono in `FirmwareUpdateWizard.Designer.cs`. Il designer mostra tutti i controlli delle pagine; durante l'esecuzione il wizard mostra quelli della pagina corrente. Il caricamento del firmware e la ricerca delle porte seriali avvengono soltanto a runtime.

L'editor dei parametri degli step rimane dinamico perché i campi dipendono dalla modalità selezionata. Timeline e anteprima LED mantengono il disegno personalizzato; i relativi controlli possono essere posizionati e ridimensionati nel designer della finestra principale.

## Utilizzo dell'editor

Selezionare un canale, aggiungere uno step e selezionare la modalità nel pannello a destra. La tabella mostra durata, posizione finale, modalità e riepilogo dei parametri. Il pannello mostra soltanto i parametri della modalità selezionata.

| Modalità | Parametri |
| --- | --- |
| Rampa | Luminosità iniziale e finale, 0–100% |
| Luce fissa | Luminosità 0–100%; 0% equivale a spento |
| Fuoco | Media, deviazione standard e filtro 0–100%; aggiornamento 1–10000 ms |
| Pulsazione | Minimo e massimo 0–100%, minimo ≤ massimo; periodo 100–10000 ms |
| Flash | Minimo e massimo 0–100%, periodo 100–10000 ms, duty cycle 0–100% |

Ogni step ha una durata di 10–60000 ms; ogni canale può contenere fino a 10 step. Il periodo dell'effetto è distinto dalla durata dello step. I preset Fuoco riprendono quelli del programmatore: Candela (70,15,50,100), Caminetto (90,30,30,25), Torcia (50,30,50,25), nell'ordine media, deviazione standard, filtro e aggiornamento.

Le sequenze sono sempre cicliche e ogni canale ripete alla propria durata totale. Dopo l'ultimo step si riparte dal primo. Il controllo facoltativo Ripeti è stato sostituito dall'indicazione Sequenza ciclica continua. I vecchi JSON con Repeat=false vengono caricati come ciclici; JSON e CSV salvano Repeat=true. Un singolo step non Rampa continua il proprio effetto senza azzerare la fase alla scadenza della durata; una singola Rampa ripete la rampa. All'ingresso di ogni step in una sequenza multipla l'effetto riparte dalla fase iniziale.

Esempio: Luce fissa 0% per 10000 ms → Flash min 0%, max 100%, periodo 500 ms, duty 20% per 3000 ms → Luce fissa 0% per 5000 ms. La durata totale è 18000 ms.

La timeline mostra i blocchi con un'anteprima degli effetti. Fare clic su un blocco per selezionarne lo step. Trascinare il punto finale per cambiarne la durata; per le rampe si modifica anche la luminosità finale. L'anteprima Fuoco è deterministica e indicativa, non una riproduzione del generatore casuale del firmware.

## LED virtuali

Il pannello a destra della timeline mostra 28 LED con la percentuale PWM. **Riproduci**, **Pausa** e **Riavvolgi** controllano un orologio condiviso; ciascun canale segue i propri step e la propria ripetizione. Fare clic sulla timeline porta l'anteprima in pausa al tempo selezionato. Fare clic su un LED seleziona il relativo canale. I parametri modificati nell'editor aggiornano anche il pannello.

Il pulsante **▶ Riproduci** avvia la simulazione e diventa **Ⅱ Pausa** durante la riproduzione. Per scegliere il colore, selezionare un LED e premere **Colore LED…** nel pannello di simulazione. Ogni canale salva il proprio `LedColor` nel JSON come `#RRGGBB`; i vecchi file usano il colore ambra predefinito. La modifica del colore attiva l'indicatore di modifiche non salvate. Il colore è una preferenza grafica della simulazione e non modifica i valori PWM del firmware.

La simulazione usa le semantiche del firmware 2.0: pulsazione triangolare continua, Flash con periodo e duty percentuale, Fuoco con primo campione immediato, gaussiana limitata a 0–100 e filtro in virgola mobile. Il generatore casuale locale è deterministico per consentire il riavvolgimento; i campioni Fuoco sono indicativi e non identici a quelli casuali della scheda. Il refresh riguarda soltanto Fuoco. Il PWM rappresenta il duty 0–100%, senza correzione gamma; la resa ottica del monitor non è calibrata.

Quando la scheda è connessa, **PWM reale dalla scheda** mostra invece i valori letti dai registri di telemetria, inclusi gli effetti dei comandi Pausa e Ferma. Deselezionarlo permette di usare Riproduci/Pausa/Riavvolgi per la simulazione locale. I comandi locali della simulazione non avviano né fermano la scheda. La telemetria viene aggiornata circa ogni 300 ms; impulsi molto brevi possono non essere visibili.

## File e compatibilità

- Il JSON versione 2 conserva modalità, tutti i parametri (anche quelli temporaneamente non attivi), ripetizione e tempi. I vecchi JSON senza modalità sono aperti come rampe.
- **Importa configurazione XML** legge il formato del programmatore: una riga ConfigurationFile con un Payload base64 di 224 registri little-endian. Crea uno step ripetuto per ogni canale, conserva BoardPID/BoardVersion come metadati e converte i canali disabilitati in luce fissa 0%. Il tempo alto del Flash viene convertito in duty percentuale, come nella UI originale. I parametri attivi fuori intervallo vengono rifiutati.
- Gun non è implementato nel motore originale e non ha un editor dei parametri: i file che lo contengono vengono rifiutati esplicitamente.
- Il CSV contiene una riga per step e conserva i parametri. Le colonne tecniche restano in inglese, indipendentemente dalla lingua selezionata. Il CSV è una descrizione logica della sequenza, senza indirizzi Modbus o nuove assegnazioni di registri.
- Il precedente metodo di esportazione dei 30 registri continua a supportare solo rampe e rifiuta altre modalità, per evitare perdite di parametri.
- La connessione online richiede il firmware 2.0 e la nuova mappa dei registri. Il firmware 1.x non è supportato; i vecchi file XML possono essere importati offline.

Il refresh Fuoco è limitato a 1–10000 ms secondo la mappa documentata del firmware; il vecchio controllo numerico consentiva 0–65535 ms. Il periodo Pulse/Flash segue i controlli della UI originale (100–10000 ms).

## Connessione online con firmware 2.0

**Connetti scheda** ricerca sulle porte COM a 38400/19200/9600 baud, parità None/Even/Odd, 8 bit dati, uno stop bit, slave Modbus 1. Controlla PID 1, firmware 2.x, protocollo 0x0200, capacità e dimensioni della mappa (28 canali, 10 step, 14 parole/step, 142 parole/canale). Una scheda con firmware precedente viene segnalata come incompatibile.

La lettura esegue LOAD e legge la finestra di ogni canale in blocchi da 100 e 42 parole, conservando modalità e parametri di tutti gli step. LOAD non cambia l'esecuzione attiva. La revisione viene controllata prima e dopo per rilevare modifiche concorrenti. I colori del documento sono mantenuti perché sono preferenze locali. Prima di sostituire il documento viene chiesto di salvare eventuali modifiche.

**Modifiche live** invia le variazioni dopo 400 ms; ogni canale modificato viene sostituito con APPLY atomico e avviato. Sono supportati tutti gli step, le rampe e la ripetizione indipendente. Disattivare Modifiche live per preparare più modifiche prima di premere **Applica**. Applica e Avvia correggono anche eventuali canali letti dalla scheda con ripetizione disabilitata, inviando repeat=1. Per conservare la modifica dopo un riavvio usare Salva EEPROM. Il selettore accanto ai comandi consente di operare su tutti i canali oppure su uno specifico canale. I colori non causano scritture.

**Avvia**, **Pausa**, **Riprendi**, **Ferma** e **Riavvia** comandano la riproduzione sulla scheda. Avvia non riavvia un canale già in esecuzione; Riavvia riparte sempre dall'inizio. Prima di Avvia/Riavvia vengono inviati gli step modificati nell'ambito selezionato. Pausa/Riprendi/Ferma non applicano automaticamente modifiche in attesa.

**Salva EEPROM** applica e avvia tutte le modifiche in attesa e richiede il salvataggio persistente dell'intera configurazione. Attende lo stato di completamento EEPROM, distinguendolo dalla semplice conferma del comando. Le normali modifiche live rimangono in RAM finché non vengono salvate. La riga di telemetria mostra stato, step, PWM del canale selezionato e stato EEPROM.

Il protocollo usa la finestra holding 8–149, il comando nei registri 1–3 e scrive per ultimo command_seq in holding 0. Ogni comando attende ack_seq e risultato nei registri input 8–11. Gli errori di validazione o scheda occupata vengono mostrati senza dichiarare l'invio riuscito. Gli errori di trasporto disconnettono la scheda: riconnettere per rileggere lo stato effettivo.

Disconnettere conserva il documento. Aprire/importare un altro file o creare un progetto disconnette prima di sostituirlo. La chiusura rilascia la porta. Chiudere altri programmi che occupano la stessa COM.

La mappa completa è documentata in [FirmwareV2/docs/FIRMWARE_V2_REGISTERS_IT.md](FirmwareV2/docs/FIRMWARE_V2_REGISTERS_IT.md).

## Wizard di aggiornamento firmware

**Aggiorna firmware…** rilascia la connessione Modbus e apre il wizard, lasciando il documento nell'editor. Salvare su file la configurazione da conservare prima dell'aggiornamento.

1. Il wizard cerca prima nella sottocartella **firmware** della cartella dell'eseguibile, indipendentemente dalla directory di lavoro. Se trova un unico file **.bin** valido, lo carica e passa direttamente alla preparazione della scheda. Se la cartella manca, il file non è valido o ci sono più file .bin, mostra la selezione manuale, aperta inizialmente nella cartella firmware quando disponibile. Indietro permette comunque di scegliere un altro file. Il nome selezionato viene mostrato nella preparazione e nel riepilogo; la programmazione richiede sempre la conferma finale. Selezionare un file **.bin** per OpenBrickLight, compilato per l'indirizzo **0x08000000**. Vengono controllati dimensione (massimo 128 KB), stack iniziale e vettore di reset Thumb. Il file viene caricato in memoria e ne viene calcolato SHA-256; i controlli strutturali non ne certificano origine o funzionalità.
2. **Spegnere la scheda, scollegarla, mettere il jumper su BOOT e ricollegarla al PC**, alimentandola. Aggiornare l'elenco COM se necessario, scegliere la porta e confermare di aver eseguito i passaggi.
3. Il wizard apre STBootLib a 115200, 8E1, identifica il bootloader e controlla MCU ID **0x0410** e capacità flash **128 KB** prima di abilitare la programmazione. In questa fase non viene cancellata la flash.
4. Controllare file, hash, porta e dispositivo nel riepilogo. **Programma** esegue cancellazione globale, scrittura a blocchi e rilettura integrale con confronto byte per byte. La chiusura è bloccata durante l'operazione. Non viene eseguita automaticamente alcuna rimozione delle protezioni.
5. Solo dopo la verifica completa viene mostrato l'esito positivo: **spegnere, riportare il jumper da BOOT alla posizione normale e ricollegare la scheda**. Chiudere il wizard e premere Connetti per rileggere la configurazione.
6. In caso di errore, il wizard riporta l'esito negativo e la procedura per riprovare dopo uno spegnimento e riavvio in BOOT. Non avvia automaticamente un'immagine non verificata.

I file .hex/.elf non vengono convertiti automaticamente: esportare il binario dal toolchain del firmware. Il contenuto viene allineato a 4 byte con padding 0xFF. La programmazione cancella l'intera flash interna; le impostazioni nella EEPROM esterna restano fisicamente separate ma la loro compatibilità dipende dal firmware installato. Dopo l'aggiornamento il software rimane offline fino alla riconnessione esplicita.

STBootLib è inclusa come sorgente dal progetto del programmatore, con correzioni locali a sincronizzazione, timeout seriali, GET a lunghezza variabile, controllo ACK finali, rilevamento EOF e frame di scrittura. Dettagli in [STBootLib/README.md](STBootLib/README.md).

## Lingue e verifiche

Interfaccia, messaggi online e wizard sono tradotti in italiano, inglese, spagnolo, tedesco e francese. Il catalogo completo è in [Localization/TRANSLATIONS.md](Localization/TRANSLATIONS.md).

Eseguire:

```powershell
dotnet build Tests/RegressionTests.csproj --disable-build-servers -p:OutputPath=bin/Verification/
dotnet Tests/bin/Verification/RegressionTests.dll
```

Le verifiche coprono editor, isolamento canali, limiti, JSON/XML/CSV, colori e formattazione nelle cinque lingue. Un dispositivo V2 in memoria verifica LOAD, APPLY, ordine dei frame, conferme negative, comandi per canale e globali, salvataggio EEPROM e telemetria. I test di aggiornamento verificano file non validi, schede incompatibili senza cancellazione, cancellazione anticipata, scrittura, verifica negativa, errori I/O e progresso positivo soltanto dopo il confronto completo. Una simulazione dei frame ROM prova direttamente STBootLib, compresi GET variabile, identità, erase, scrittura, rilettura, EOF e NACK. Il wizard viene renderizzato nelle cinque lingue.

Non è stata eseguita una programmazione su hardware reale: collegamento elettrico, accesso al bootloader ROM tramite l'interfaccia della scheda e aggiornamento fisico richiedono la verifica sulla scheda.


### Diagnostica connessione

La telemetria viene letta in blocchi da **100 + 60 registri**: il gestore FC04 del firmware installato usa un confronto stretto con 125 e rifiuta una richiesta di esattamente 125 registri con eccezione 3. Il precedente editor si collegava e caricava i canali, ma si disconnetteva al primo aggiornamento della telemetria.

La ricerca attende 100 ms dopo l'apertura della COM e riprova fino a tre volte l'identificazione dopo errori transitori. Usa un timeout di 500 ms in ricerca e 1500 ms dopo il riconoscimento della scheda. Le risposte RTU vengono lette in blocchi, gestendo anche letture parziali.

Il file %LOCALAPPDATA%/ModbusSequenceStudio/connection.log contiene parametri, frame TX/RX ed errori, senza nascondere gli errori del caricamento dietro il solo messaggio generico. Viene rinnovato a ogni ricerca e ruotato oltre 4 MB. Chiudere altri tool che occupano la COM prima di connettere l'editor.



### Avvio e guida dell'interfaccia

L'applicazione si avvia con la finestra massimizzata. I tooltip descrivono pulsanti, selettori, timeline, tabella, parametri dello step, anteprima LED e wizard firmware. Cambiano insieme alla lingua dell'interfaccia; i controlli disabilitati mantengono una spiegazione consultabile. Sui LED il tooltip indica canale e PWM. Le celle e le intestazioni della tabella spiegano significato e unità di misura del campo.
