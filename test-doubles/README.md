# Lernaufgabe: Testen mit Test Doubles

## Ausgangslage

Ein `ReservationService` nimmt Sitzplatzreservierungen entgegen. Er fragt die verfügbaren Plätze ab, speichert eine Reservierung und verschickt danach eine Bestätigung. Der Service und die drei benötigten Schnittstellen stehen in [`ReservationService.cs`](ReservationService.cs). Eine echte Implementierung von `ISeatAvailability` gibt es noch nicht. Tests und Test Doubles erstellt ihr selbst.

Wählt **xUnit oder MSTest**. Beide Testprojekte sind vorbereitet und enthalten Moq. Bearbeitet die Aufgabe in **einem** der beiden Projekte:

| Testframework | Testprojekt | Testmethode |
| --- | --- | --- |
| xUnit | `tests/TestDoublesExercise.Tests` | `[Fact]` oder `[Theory]` mit `[InlineData(...)]` |
| MSTest | `tests/TestDoublesExercise.MSTest.Tests` | `[TestMethod]`, optional mit `[DataRow(...)]`; die Klasse erhält `[TestClass]` |

Für die ersten Aufgaben schreibt ihr die Doubles von Hand. Erst in Aufgabe 4 verwendet ihr Moq. Verwendet keine Datenbank und verschickt keine echten Nachrichten.

```bash
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
# oder
dotnet test tests/TestDoublesExercise.MSTest.Tests/TestDoublesExercise.MSTest.Tests.csproj
```

In beiden Testprojekten findet ihr `TestDoubleExamples.cs` mit **lauffähigen Beispielen** für einen selbst geschriebenen Stub, einen selbst geschriebenen Spy und Moq. Das kleine Lagerbestandsbeispiel ist unabhängig von der Reservierungsaufgabe. Übertragt die Ideen auf `ReservationService` und schreibt dafür eigene Doubles.

In `ReservationServiceTests.cs` findet ihr Testmethoden mit Arrange-, Act- und Assert-Hinweisen. Diese sind zunächst mit `Skip` (xUnit) beziehungsweise `Ignore` (MSTest) markiert. Implementiert einen Test und entfernt dann seine Markierung. Ergänzt weitere Tests für alle unten geforderten Fälle. Verändert den bestehenden `Reserve`-Ablauf für die Grundaufgaben nicht.

Schreibt bei MSTest `[TestClass]` über die Testklasse und `[TestMethod]` über jede einzelne Testmethode; bei xUnit genügt `[Fact]` über der Methode. Verwendet die Assertions eures gewählten Frameworks: bei MSTest zum Beispiel `Assert.IsTrue` und `Assert.ThrowsExactly<T>`, bei xUnit `Assert.True` und `Assert.Throws<T>`.

## Lernziele

- Einen **Stub** schreiben, der kontrollierte Rückgabewerte liefert.
- Einen **Spy** schreiben, der Aufrufe und Argumente aufzeichnet.
- Mit **Moq** Rückgabewerte festlegen und gezielt Interaktionen prüfen.
- Einen Service testen, obwohl von einer benötigten Komponente nur die Schnittstelle existiert.
- Beobachtbares Verhalten und sinnvolle Fehlerfälle mit unabhängigen Tests abdecken.

## Kleine Aufgaben

### 1. Nur die Schnittstelle ist bekannt

Von `ISeatAvailability` ist bisher nur die Schnittstelle bekannt; die produktive Implementierung fehlt. Wie könnt ihr `ReservationService` trotzdem jetzt testen? Schreibt eine kurze Antwort in eure Testdatei oder in eine eigene Markdown-Datei. Nennt dabei, welche Aussagen die Schnittstelle bereits erlaubt und welche fachliche Annahme ihr für `GetAvailableSeats` trefft.

Implementiert dann im **Testprojekt** einen eigenen `ISeatAvailability`-Stub. Für einen angegebenen Veranstaltungscode soll er eine festgelegte Anzahl verfügbarer Plätze liefern. Testet mindestens:

- 3 verfügbare Plätze, 2 angefragt: `Reserve` liefert `true`, wenn das Speichern gelingt.
- 1 verfügbarer Platz, 2 angefragt: `Reserve` liefert `false`.
- Genau 2 verfügbar und 2 angefragt: Die Reservierung ist möglich.

Für diese Aufgabe dürft ihr ein einfaches, selbst geschriebenes `IReservationRepository` verwenden, dessen `TrySave` immer `true` liefert, sowie einen `IConfirmationSender`, dessen `Send` nichts tut. Der Verfügbarkeits-Stub soll keine echte Verfügbarkeit berechnen.

### 2. Grenzfälle und Isolation

Ergänzt Tests für `seatCount` 0 und −1 sowie für einen leeren Veranstaltungscode. Prüft den erwarteten Exception-Typ. Bei ungültigen Eingaben darf keine Abhängigkeit aufgerufen werden. Überlegt, wie ihr diese fehlenden Aufrufe sichtbar machen könnt, ohne Produktionscode zu ändern.

### 3. Einen Spy von Hand schreiben

Implementiert im Testprojekt einen `IConfirmationSender`-Spy. Er soll alle übergebenen `Reservation`-Objekte in ihrer Reihenfolge aufzeichnen. Prüft mit ihm:

- Nach erfolgreichem Speichern wird genau eine Bestätigung mit E-Mail-Adresse, Veranstaltungscode und Platzanzahl versendet.
- Bei zu wenigen Plätzen wird keine Bestätigung versendet.
- Wenn `TrySave` `false` liefert, wird keine Bestätigung versendet.

Erklärt in einem Satz den Unterschied zwischen eurem Stub und eurem Spy.

### 4. Moq für das Repository einsetzen

Ersetzt euer selbst geschriebenes Repository-Double in neuen Tests durch `Mock<IReservationRepository>`. Legt mit `Setup(...).Returns(...)` je einen erfolgreichen und einen fehlgeschlagenen Speicherversuch fest. Verwendet `mock.Object` beim Erstellen des Service.

Prüft mit `Verify(..., Times.Once())`, dass bei ausreichenden Plätzen die **richtige** Reservierung gespeichert wird. Prüft mit `Times.Never()`, dass bei zu wenigen Plätzen kein Speicherversuch stattfindet. Kontrolliert zusätzlich jeweils den Rückgabewert und den Spy. Nutzt für die Argumentprüfung die Eigenschaften der `Reservation`, nicht nur `It.IsAny<Reservation>()`.

### 5. Handgeschrieben oder mit Moq?

Schreibt einen weiteren Test für eine E-Mail-Adresse, die nur aus Leerzeichen besteht. Wählt für jede Abhängigkeit selbst zwischen einem handgeschriebenen Double und Moq. Begründet die Wahl in zwei bis drei Sätzen. Der Test soll die Exception und das Ausbleiben von Speichern und Bestätigung prüfen.

## Grössere Aufgabe für Fortgeschrittene: Gruppenreservierung

Erweitert den Service um `bool ReserveBatch(IReadOnlyList<Reservation> requests)` und das Repository-Interface um `bool TrySaveBatch(IReadOnlyList<Reservation> reservations)`. Die Methode soll folgende Regeln erfüllen:

1. `requests` darf nicht `null` sein und muss mindestens einen Eintrag enthalten. E-Mail-Adresse und Veranstaltungscode jedes Eintrags dürfen nicht leer sein; `SeatCount` muss positiv sein. Ungültige Eingaben führen zu einer passenden Exception, bevor eine Abhängigkeit aufgerufen wird.
2. Mehrere Einträge dürfen denselben Veranstaltungscode haben. Addiert die angefragten Plätze pro Veranstaltung. Fragt die Verfügbarkeit **genau einmal pro unterschiedlichem Veranstaltungscode** ab.
3. Reichen die Plätze bei mindestens einer Veranstaltung nicht aus, gebt `false` zurück. Speichert nichts und verschickt keine Bestätigungen.
4. Sind genügend Plätze vorhanden, übergebt **alle** Reservierungen in einem Aufruf an `TrySaveBatch`. Das Repository garantiert dabei eine atomare Speicherung: entweder alle oder keine.
5. Gibt `TrySaveBatch` `false` zurück, gebt `false` zurück und verschickt keine Bestätigungen. Nach erfolgreichem Speichern verschickt ihr für jeden Eintrag genau eine Bestätigung in der Reihenfolge der Eingabe und gebt `true` zurück.

Schreibt zuerst Tests für die Regeln und implementiert danach die Methode. Verwendet mindestens einen selbst geschriebenen Stub, einen selbst geschriebenen Spy und Moq. Prüft mit Moq die Zahl und die Argumente des Batch-Speicheraufrufs. Ein besonders wichtiger Test enthält **zwei Reservierungen für dieselbe Veranstaltung**, die einzeln passen, zusammen aber die Kapazität überschreiten.

Haltet zum Schluss kurz fest, was passieren könnte, wenn `Send` nach erfolgreichem Speichern eine Exception wirft. Nennt eine mögliche fachliche Lösung; eine technische Umsetzung ist hier nicht erforderlich.

## Abgabe und Selbstkontrolle

- Testcode mit getrennten, aussagekräftig benannten xUnit- oder MSTest-Tests und klaren Arrange-, Act- und Assert-Abschnitten.
- Eigener Verfügbarkeits-Stub und eigener Bestätigungs-Spy im Testprojekt.
- Mindestens zwei Tests mit Moq für das Repository.
- Kurze Antworten zu Aufgabe 1, 3 und 5; für Fortgeschrittene zusätzlich die Überlegung zum Fehler beim Bestätigungsversand.
- Alle geschriebenen Tests bestehen mit `dotnet test` für euer gewähltes Testprojekt.

Hinweis: Ein Stub liefert Daten für den Test. Ein Spy merkt sich Aufrufe, damit der Test sie nachher prüfen kann. Moq kann beide Rollen übernehmen; hier übt ihr zuerst die Mechanismen von Hand. Die [Moq-Projektseite](https://github.com/devlooped/moq) zeigt `Setup`, `Object` und `Verify`.
