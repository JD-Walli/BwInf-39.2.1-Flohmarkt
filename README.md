# BwInf-39.2.1-Flohmarkt

Lösung der Aufgabe "Spiessgesellen" aus der zweiten Runde des 39ten Bundeswettbewerb Informatik.

## Aufgabe

Einmal im Monat findet in Langdorf ein großer Flohmarkt von 8 Uhr bis 18 Uhr statt. Hier
bieten Privatleute Dinge an, die sie nicht mehr brauchen. Der Flohmarkt ist sehr beliebt; daher
ist eine Voranmeldung nötig. Typischerweise können nicht alle Voranmeldungen berücksichtigt
werden.
Standplätze werden meterweise vermietet. Mietbeginn und -ende sind jeweils zur vollen Stunde.
Die Mietkosten betragen 1 Euro pro Stunde und Meter. Bei ihrer Voranmeldung geben die
Anbieter an, zu welcher Zeit sie wie viele Meter mieten möchten. Beispielsweise möchten Anna
von 11 bis 16 Uhr einen Stand von 5 Metern, Sophie von 16 bis 18 Uhr einen Stand von 3 Metern
und Max von 10 bis 14 Uhr einen Stand von 4 Metern haben. In diesem Fall kann Sophie dort
untergebracht werden, wo Annas Stand zuvor war. Max jedoch wird einen anderen Standplatz
bekommen müssen.
Der Flohmarkt zieht sich in einer Reihe entlang der Hauptstraße und ist 1000 Meter lang. Diese
recht große Ausdehnung des Flohmarktes macht die Verwaltung der Voranmeldungen jedoch
schwierig.

Schreibe ein Programm, das den Organisatoren des Flohmarkts hilft. Dein Programm soll eine
Liste von Voranmeldungen der Anbieter einlesen und eine Auswahl aus diesen so treffen, dass
für alle ausgewählten Anmeldungen ein Standplatz gefunden werden kann und die Mieteinnahmen
möglichst hoch sind.

## Lösungsansatz

Die erste Lösungsidee bestand darin, allen Voranmeldungen zufällig eine Position zuzuteilen und daraufhin mit dem Optimierungsverfahren „Simulated Annealing“ die Lösung zu verbessern. In der Hoffnung, noch bessere Lösungen zu finden, habe ich die initiale Positionierung sowie die möglichen Positionsveränderungen der Anmeldungen in mehreren Stufen eingeschränkt beziehungsweise präzisiert. Dazu wurde eine Heuristik entwickelt, die die Anmeldungen so positioniert, dass möglichst gute Lösungen herauskommen. Diese Heuristik lässt sich mit mehreren Parametern anpassen. Mit welchen Parametern dabei das beste Ergebnis erzielt wird, hängt von den jeweiligen eingegebenen Anmeldedaten ab.

## Programminstallation

Am einfachsten ist das Programm über Visual Studio ausführbar. Dazu muss die ".sln" Datei im Flohmarkt Ordner geöffnet werden. Um das Programm auszuführen, muss in Visual Studio die Programmiersprache C# installiert sein.
