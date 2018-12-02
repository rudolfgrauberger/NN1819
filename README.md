# NN1819
# Anleitung

## Auschecken
Bitte als erstes über 

```sh
git clone https://github.com/rudolfgrauberger/NN1819.git
```

das Repository clonen.

## Öffnen
Anschließend Unity starten und dann in dem Register **Projects** im Start-Wizard über **Open** das gerade ausgecheckte Verzeichnis auswählen.

## Starten
In dem **Projects**-Register unter **Assets>Scenes** die Scene **SampleScene** mit einem Doppelklick auswählen. Jetzt kann man mit dem Play-Button das Projekt starten.

### Control Mode
Es gibt folgende Möglichkeiten wie unser TicTac-Gefährt gesteuert werden kann:

| Mode  | Beschreibung |
| ------------- | ------------- |
| Manual  | Hier kann man das Gefährt mit **A** (Links), **D** (Rechts) und **Leertaste** (Springen) steuern. Mit **X** wird der aktuelle Stand der Aufzeichnungen in ```./RecordedData/<GUID>/``` abgespeichert und die Anwendung beendet. |
| Automatic  | In diesem Modus wird die Datei ```./RecordedData/Default.csv``` geladen und das Netzwerk damit trainiert. Anschließend wird das Spiel gestartet und das Netzwerk übernimmt die Steuerung. |
