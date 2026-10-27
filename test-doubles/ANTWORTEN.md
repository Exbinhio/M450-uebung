# Antworten

## Aufgabe 1

`ReservationService` hängt nur von der Schnittstelle `ISeatAvailability` ab. Deshalb können wir im Test einen eigenen Stub übergeben, der feste Werte liefert, und den Service testen, obwohl es noch keine echte Implementierung gibt.

Die Schnittstelle sagt nur aus, dass `GetAvailableSeats` einen Veranstaltungscode als `string` erhält und eine Anzahl als `int` zurückgibt. Wie diese Zahl zustande kommt, legt sie nicht fest.

Fachliche Annahme: `GetAvailableSeats` liefert die Anzahl der noch freien Plätze der Veranstaltung. Eine Reservierung ist möglich, wenn diese Zahl mindestens so gross ist wie die angefragte Platzanzahl.

## Aufgabe 3

Der Stub (`SeatAvailabilityStub`) liefert dem Service nur vorgegebene Daten, während der Spy (`ConfirmationSenderSpy`) die Aufrufe mit ihren Argumenten aufzeichnet, damit der Test sie nachher prüfen kann.

## Aufgabe 5

Für die Verfügbarkeit und den Sender verwenden wir die handgeschriebenen Spies, weil sie schon vorhanden sind und sich mit `Assert.Empty` einfach prüfen lässt, dass sie nicht aufgerufen wurden. Für das Repository verwenden wir Moq, weil `Verify(..., Times.Never())` ohne eigenen Code direkt ausdrückt, dass kein Speicherversuch stattgefunden hat.

## Gruppenreservierung: Exception in `Send`

Wirft `Send` nach erfolgreichem Speichern eine Exception, sind die Reservierungen gespeichert, aber der Aufrufer erhält eine Exception statt `true` und hält die Reservierung für fehlgeschlagen. Reserviert er erneut, werden Plätze doppelt belegt. Bei `ReserveBatch` erhalten ausserdem nur die Kunden vor dem Fehler eine Bestätigung.

Mögliche fachliche Lösung: Eine Reservierung gilt als erfolgreich, sobald sie gespeichert ist. Kann die Bestätigung nicht versendet werden, wird sie als ausstehend vermerkt und später erneut versendet.
