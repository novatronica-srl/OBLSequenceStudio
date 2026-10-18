# STBootLib incorporata

I sei sorgenti originali provengono dal progetto locale `openbricklight_sw/OpenBrickLightProgrammer/STBootLib`. Sono compilati insieme all'editor, senza dipendenze aggiuntive oltre a System.IO.Ports.

La copia locale corregge il semaforo dei comandi, usa 8E1 esplicito, accetta il GET a lunghezza variabile e verifica gli ACK finali di GET/GET_ID. Le letture seriali sincrone sono eseguite su worker con timeout: non restano letture asincrone pendenti dopo un timeout; EOF interrompe la ricezione. La cancellazione globale può attendere fino a 30 secondi e usa la coppia speciale FF 00 prevista da AN3155 (oppure FF FF 00 per Extended Erase). Le scritture richiedono indirizzo e lunghezza allineati a 4 byte e inviano lunghezza, dati e checksum in un solo frame.

`FirmwareUpdateSession` verifica l'identità e la capacità prima della cancellazione e confronta l'intera immagine riletta dopo la programmazione. Non usa Unprotect o Jump. La sessione seriale rimane aperta tra identificazione e aggiornamento: un nuovo autobaud richiede il riavvio fisico in BOOT. Il costruttore interno con Stream consente test senza porte reali.

Riferimenti ufficiali ST: [AN3155, protocollo USART del bootloader](https://www.st.com/resource/en/application_note/an3155-usart-protocol-used-in-the-stm32-bootloader-stmicroelectronics.pdf), [AN2606, avvio in memoria di sistema](https://www.st.com/resource/en/application_note/an2606-introduction-to-system-memory-boot-mode-on-stm32-mcus-stmicroelectronics.pdf).
