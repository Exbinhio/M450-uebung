# Antworten – Testen mit Test Doubles

Gewähltes Testframework: **xUnit** (`tests/TestDoublesExercise.Tests`)

```bash
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```

| Datei | Inhalt |
| --- | --- |
| `tests/TestDoublesExercise.Tests/ReservationTestDoubles.cs` | Selbst geschriebene Doubles (Stub, Spies, einfache Doubles) |
| `tests/TestDoublesExercise.Tests/ReservationServiceTests.cs` | Tests für Aufgaben 1–5 |
| `tests/TestDoublesExercise.Tests/ReserveBatchTests.cs` | Tests für die Gruppenreservierung |
| `test-doubles/ReservationService.cs` | Erweitert um `ReserveBatch` und `TrySaveBatch` |

## Aufgabe 1 – Nur die Schnittstelle ist bekannt

Der `ReservationService` hängt nur von der Schnittstelle `ISeatAvailability` ab, nicht von einer konkreten Klasse. Darum können wir im Test einen eigenen Stub übergeben, der feste Werte liefert, und den Service so testen, bevor es eine echte Implementierung gibt.

**Was die Schnittstelle bereits sagt:** Es gibt eine Methode `GetAvailableSeats`, die einen Veranstaltungscode (`string`) entgegennimmt und eine Anzahl (`int`) zurückgibt. Mehr nicht – wie die Zahl berechnet wird, woher sie kommt oder wie mit unbekannten Codes umgegangen wird, ist offen.

**Unsere fachliche Annahme:** `GetAvailableSeats(eventCode)` liefert die Anzahl der aktuell noch freien Plätze dieser Veranstaltung (≥ 0). Eine Reservierung ist möglich, wenn diese Zahl grösser oder gleich der angefragten Platzanzahl ist. Für unbekannte Codes liefert unser Stub 0.

## Aufgabe 2 – Fehlende Aufrufe sichtbar machen

Ohne Produktionscode zu ändern, übergeben wir Doubles, die jeden Aufruf aufzeichnen (`SeatAvailabilitySpy`, `ReservationRepositorySpy`, `ConfirmationSenderSpy`). Nach dem Test prüfen wir, dass ihre Aufzeichnungen leer sind. Alternativ ginge das mit Moq über `Verify(..., Times.Never())` bzw. `VerifyNoOtherCalls()`.

## Aufgabe 3 – Stub vs. Spy

Unser **Stub** (`SeatAvailabilityStub`) liefert dem Service nur vorgegebene Daten (freie Plätze), während unser **Spy** (`ConfirmationSenderSpy`) aufzeichnet, welche Reservierungen ihm übergeben wurden, damit der Test diese Aufrufe nachher prüfen kann.

## Aufgabe 5 – Handgeschrieben oder Moq?

Für die Verfügbarkeit und den Sender verwenden wir die handgeschriebenen Spies, weil sie bereits existieren und die Prüfung mit `Assert.Empty(...)` sehr gut lesbar ist. Für das Repository verwenden wir Moq, weil `Verify(..., Times.Never())` zusammen mit `VerifyNoOtherCalls()` ohne zusätzlichen Code ausdrückt, dass weder `TrySave` noch `TrySaveBatch` aufgerufen wurde. So sieht man in einem Test auch direkt, dass beide Ansätze dasselbe Ziel erreichen.

## Fortgeschrittene – Exception in `Send` nach erfolgreichem Speichern

Wirft `Send` nach dem Speichern eine Exception, ist die Reservierung in der Datenbank gespeichert, der Aufrufer bekommt aber eine Exception statt `true`. Er glaubt dann womöglich, die Reservierung sei fehlgeschlagen, und versucht es erneut – das führt zu doppelten Reservierungen. Bei `ReserveBatch` kann es zudem passieren, dass nur ein Teil der Kunden eine Bestätigung erhält, obwohl alle Plätze reserviert sind.

**Mögliche fachliche Lösung:** Das Speichern der Reservierung ist das entscheidende Ereignis; die Bestätigung wird davon entkoppelt. Die Reservierung gilt als erfolgreich, sobald sie gespeichert ist, und der Bestätigungsversand wird als „ausstehend“ vermerkt und später erneut versucht (z. B. über eine Warteschlange bzw. Outbox). Gelingt der Versand dauerhaft nicht, wird der Kundendienst informiert, der den Kunden manuell kontaktiert.
