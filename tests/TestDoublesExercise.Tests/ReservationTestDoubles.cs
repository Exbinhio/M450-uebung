namespace TestDoublesExercise.Tests;

// Stub: liefert pro Veranstaltungscode eine fest vorgegebene Anzahl freier Plätze.
// Er berechnet keine echte Verfügbarkeit und merkt sich keine Aufrufe.
// Unbekannte Veranstaltungscodes haben 0 freie Plätze.
public sealed class SeatAvailabilityStub : ISeatAvailability
{
    private readonly Dictionary<string, int> _availableSeats = [];

    public SeatAvailabilityStub(string eventCode, int availableSeats)
    {
        _availableSeats[eventCode] = availableSeats;
    }

    public SeatAvailabilityStub WithEvent(string eventCode, int availableSeats)
    {
        _availableSeats[eventCode] = availableSeats;
        return this;
    }

    public int GetAvailableSeats(string eventCode) =>
        _availableSeats.TryGetValue(eventCode, out var seats) ? seats : 0;
}

// Spy für die Verfügbarkeit: liefert immer denselben Wert und zeichnet die abgefragten Codes auf.
// Damit lässt sich prüfen, ob und wie oft die Verfügbarkeit abgefragt wurde.
public sealed class SeatAvailabilitySpy(int availableSeats) : ISeatAvailability
{
    private readonly List<string> _requestedEventCodes = [];

    public IReadOnlyList<string> RequestedEventCodes => _requestedEventCodes;

    public int GetAvailableSeats(string eventCode)
    {
        _requestedEventCodes.Add(eventCode);
        return availableSeats;
    }
}

// Einfaches Repository für Aufgabe 1: Speichern gelingt immer.
public sealed class AlwaysSuccessfulRepository : IReservationRepository
{
    public bool TrySave(Reservation reservation) => true;

    public bool TrySaveBatch(IReadOnlyList<Reservation> reservations) => true;
}

// Repository-Spy: liefert ein vorgegebenes Ergebnis und zeichnet alle Speicherversuche auf.
public sealed class ReservationRepositorySpy(bool saveResult) : IReservationRepository
{
    private readonly List<Reservation> _savedReservations = [];
    private readonly List<IReadOnlyList<Reservation>> _savedBatches = [];

    public IReadOnlyList<Reservation> SavedReservations => _savedReservations;

    public IReadOnlyList<IReadOnlyList<Reservation>> SavedBatches => _savedBatches;

    public bool TrySave(Reservation reservation)
    {
        _savedReservations.Add(reservation);
        return saveResult;
    }

    public bool TrySaveBatch(IReadOnlyList<Reservation> reservations)
    {
        _savedBatches.Add(reservations.ToList());
        return saveResult;
    }
}

// Sender für Aufgabe 1: tut nichts.
public sealed class NoOpConfirmationSender : IConfirmationSender
{
    public void Send(Reservation reservation)
    {
    }
}

// Spy: zeichnet alle übergebenen Reservierungen in ihrer Reihenfolge auf, statt etwas zu versenden.
public sealed class ConfirmationSenderSpy : IConfirmationSender
{
    private readonly List<Reservation> _sentReservations = [];

    public IReadOnlyList<Reservation> SentReservations => _sentReservations;

    public void Send(Reservation reservation) => _sentReservations.Add(reservation);
}
