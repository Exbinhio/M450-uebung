using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    private const string Email = "anna@example.com";
    private const string EventCode = "CONCERT-1";
    private const string OtherEventCode = "THEATRE-7";

    [Fact]
    public void Reserve_WithEnoughSeats_ReturnsTrue()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, 3);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 1), repository, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithExactlyEnoughSeats_Succeeds()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, 2);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Reserve_WithZeroSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, EventCode, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithNegativeSeats_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, EventCode, -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEmptyEventCode_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);

        // Act
        Action action = () => service.Reserve(Email, "", 2);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_AfterSuccessfulSave_SendsExactlyOneConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), new AlwaysSuccessfulRepository(), sender);

        // Act
        service.Reserve(Email, EventCode, 2);

        // Assert
        var confirmation = Assert.Single(sender.SentReservations);
        Assert.Equal(Email, confirmation.CustomerEmail);
        Assert.Equal(EventCode, confirmation.EventCode);
        Assert.Equal(2, confirmation.SeatCount);
    }

    [Fact]
    public void Reserve_WhenSavingFails_DoesNotSendConfirmation()
    {
        // Arrange
        var repository = new ReservationRepositorySpy(saveResult: false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), repository, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithEnoughSeats_SavesCorrectReservationOnce()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
        repository.Verify(
            repo => repo.TrySave(It.Is<Reservation>(r =>
                r.CustomerEmail == Email &&
                r.EventCode == EventCode &&
                r.SeatCount == 2)),
            Times.Once());
        Assert.Equal(new Reservation(Email, EventCode, 2), Assert.Single(sender.SentReservations));
    }

    [Fact]
    public void Reserve_WhenMockedSaveFails_ReturnsFalseAndSendsNoConfirmation()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 1), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        Action action = () => service.Reserve("   ", EventCode, 2);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WithNullRequests_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        Action action = () => service.ReserveBatch(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WithEmptyList_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        Action action = () => service.ReserveBatch([]);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    [Theory]
    [InlineData("", EventCode)]
    [InlineData(Email, "")]
    public void ReserveBatch_WithEmptyEmailOrEventCode_ThrowsBeforeCallingDependencies(string email, string eventCode)
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        var requests = new[]
        {
            new Reservation("ben@example.com", EventCode, 1),
            new Reservation(email, eventCode, 1),
        };

        // Act
        Action action = () => service.ReserveBatch(requests);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReserveBatch_WithNonPositiveSeatCount_ThrowsBeforeCallingDependencies(int seatCount)
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        var requests = new[]
        {
            new Reservation("ben@example.com", EventCode, 1),
            new Reservation(Email, EventCode, seatCount),
        };

        // Act
        Action action = () => service.ReserveBatch(requests);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_QueriesAvailabilityOncePerDistinctEventCode()
    {
        // Arrange
        var availability = new Mock<ISeatAvailability>();
        availability.Setup(a => a.GetAvailableSeats(EventCode)).Returns(5);
        availability.Setup(a => a.GetAvailableSeats(OtherEventCode)).Returns(5);
        var service = new ReservationService(availability.Object, new AlwaysSuccessfulRepository(), new NoOpConfirmationSender());
        var requests = new[]
        {
            new Reservation(Email, EventCode, 1),
            new Reservation("ben@example.com", OtherEventCode, 1),
            new Reservation("carla@example.com", EventCode, 1),
        };

        // Act
        service.ReserveBatch(requests);

        // Assert
        availability.Verify(a => a.GetAvailableSeats(EventCode), Times.Once());
        availability.Verify(a => a.GetAvailableSeats(OtherEventCode), Times.Once());
    }

    [Fact]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), repository.Object, sender);
        var requests = new[]
        {
            new Reservation(Email, EventCode, 2),
            new Reservation("ben@example.com", EventCode, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WhenOneEventHasTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, 10).WithEvent(OtherEventCode, 1);
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        var requests = new[]
        {
            new Reservation(Email, EventCode, 2),
            new Reservation("ben@example.com", OtherEventCode, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WithEnoughSeats_SavesAllReservationsInOneBatchCall()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(true);
        var availability = new SeatAvailabilityStub(EventCode, 10).WithEvent(OtherEventCode, 10);
        var service = new ReservationService(availability, repository.Object, new NoOpConfirmationSender());
        var requests = new[]
        {
            new Reservation(Email, EventCode, 2),
            new Reservation("ben@example.com", OtherEventCode, 3),
        };

        // Act
        service.ReserveBatch(requests);

        // Assert
        repository.Verify(
            repo => repo.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(list =>
                list.Count == 2 &&
                list[0].CustomerEmail == Email && list[0].EventCode == EventCode && list[0].SeatCount == 2 &&
                list[1].CustomerEmail == "ben@example.com" && list[1].EventCode == OtherEventCode && list[1].SeatCount == 3)),
            Times.Once());
    }

    [Fact]
    public void ReserveBatch_WhenBatchSaveFails_ReturnsFalseAndSendsNoConfirmation()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 10), repository.Object, sender);
        var requests = new[]
        {
            new Reservation(Email, EventCode, 2),
            new Reservation("ben@example.com", EventCode, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_AfterSuccessfulSave_SendsOneConfirmationPerEntryInInputOrder()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var availability = new SeatAvailabilityStub(EventCode, 10).WithEvent(OtherEventCode, 10);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), sender);
        var requests = new[]
        {
            new Reservation(Email, EventCode, 2),
            new Reservation("ben@example.com", OtherEventCode, 1),
            new Reservation("carla@example.com", EventCode, 4),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
        Assert.Equal(requests, sender.SentReservations);
    }

    private sealed class SeatAvailabilityStub : ISeatAvailability
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

        public int GetAvailableSeats(string eventCode) => _availableSeats[eventCode];
    }

    private sealed class SeatAvailabilitySpy(int availableSeats) : ISeatAvailability
    {
        private readonly List<string> _requestedEventCodes = [];

        public IReadOnlyList<string> RequestedEventCodes => _requestedEventCodes;

        public int GetAvailableSeats(string eventCode)
        {
            _requestedEventCodes.Add(eventCode);
            return availableSeats;
        }
    }

    private sealed class AlwaysSuccessfulRepository : IReservationRepository
    {
        public bool TrySave(Reservation reservation) => true;

        public bool TrySaveBatch(IReadOnlyList<Reservation> reservations) => true;
    }

    private sealed class ReservationRepositorySpy(bool saveResult) : IReservationRepository
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
            _savedBatches.Add(reservations);
            return saveResult;
        }
    }

    private sealed class NoOpConfirmationSender : IConfirmationSender
    {
        public void Send(Reservation reservation)
        {
        }
    }

    private sealed class ConfirmationSenderSpy : IConfirmationSender
    {
        private readonly List<Reservation> _sentReservations = [];

        public IReadOnlyList<Reservation> SentReservations => _sentReservations;

        public void Send(Reservation reservation) => _sentReservations.Add(reservation);
    }
}
