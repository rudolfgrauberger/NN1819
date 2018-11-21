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
| Manual  | Hier kann man das Gefährt mit A (Links), D (Rechts) und Leertaste (Springen) steuern. |
| Automatic  | Aktuell macht dieser Modus nichts  |
| Manual Record For Training | Startet das Spiel mit der Geschwindigkeit 0.1, Zeichnet die Steuerung bis zum Crash auf, beim Crash werden die aufgezeichneten Daten in die Datei die unter *Train Set File* angegeben ist gespeichert. Eine weitere Besonderheit in diesem Modus ist, dass mit W (Normale Geschwindigkeit) und mit S (0.05 Geschwindigkeit) eingeschaltet wird ohne das diese Aktion aufgezeichnet wird. |
| Train From Data | Startet das Spiel und Trainiert das Netz mit den Daten aus der Datei die in *Train Set File* benannt ist und nutzt anschließend die "intelligenz" um das Gefährt zu steuern |
