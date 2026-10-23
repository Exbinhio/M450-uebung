using Moq;

namespace TestDoublesExercise.Tests;

public sealed class ReserveBatchTests
{
    private const string Concert = "CONCERT-1";
    private const string Theatre = "THEATRE-7";

    // ---------- Ungültige Eingaben ----------

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
    [InlineData("", Concert)]
    [InlineData("   ", Concert)]
    [InlineData("anna@example.com", "")]
    [InlineData("anna@example.com", "   ")]
    public void ReserveBatch_WithMissingEmailOrEventCode_ThrowsBeforeCallingDependencies(string email, string eventCode)
    {
        // Arrange: Der ungültige Eintrag steht an zweiter Stelle.
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        var requests = new[]
        {
            new Reservation("ben@example.com", Concert, 1),
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
            new Reservation("ben@example.com", Concert, 1),
            new Reservation("anna@example.com", Concert, seatCount),
        };

        // Act
        Action action = () => service.ReserveBatch(requests);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Empty(availability.RequestedEventCodes);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }

    // ---------- Verfügbarkeit ----------

    [Fact]
    public void ReserveBatch_WhenCombinedSeatsExceedCapacity_DoesNotSaveOrSend()
    {
        // Arrange: Je 2 Plätze passen einzeln in 3 freie Plätze, zusammen (4) aber nicht.
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(Concert, 3), repository.Object, sender);
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Concert, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>()), Times.Never());
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WhenCombinedSeatsExactlyFit_ReturnsTrue()
    {
        // Arrange
        var service = new ReservationService(
            new SeatAvailabilityStub(Concert, 4), new AlwaysSuccessfulRepository(), new ConfirmationSenderSpy());
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Concert, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ReserveBatch_WithSameEventCodeTwice_QueriesAvailabilityOnce()
    {
        // Arrange
        var availability = new SeatAvailabilitySpy(10);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), new ConfirmationSenderSpy());
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Concert, 3),
        };

        // Act
        service.ReserveBatch(requests);

        // Assert
        Assert.Equal([Concert], availability.RequestedEventCodes);
    }

    [Fact]
    public void ReserveBatch_WithDifferentEventCodes_QueriesEachEventCodeExactlyOnce()
    {
        // Arrange
        var availability = new Mock<ISeatAvailability>();
        availability.Setup(a => a.GetAvailableSeats(Concert)).Returns(5);
        availability.Setup(a => a.GetAvailableSeats(Theatre)).Returns(5);
        var service = new ReservationService(availability.Object, new AlwaysSuccessfulRepository(), new ConfirmationSenderSpy());
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 1),
            new Reservation("ben@example.com", Theatre, 1),
            new Reservation("carla@example.com", Concert, 1),
        };

        // Act
        service.ReserveBatch(requests);

        // Assert
        availability.Verify(a => a.GetAvailableSeats(Concert), Times.Once());
        availability.Verify(a => a.GetAvailableSeats(Theatre), Times.Once());
        availability.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReserveBatch_WhenOneEventHasTooFewSeats_DoesNotSaveOrSend()
    {
        // Arrange: Konzert hat genug Plätze, Theater nicht.
        var availability = new SeatAvailabilityStub(Concert, 10).WithEvent(Theatre, 1);
        var repository = new ReservationRepositorySpy(saveResult: true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Theatre, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        Assert.Empty(repository.SavedBatches);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    // ---------- Speichern und Bestätigen ----------

    [Fact]
    public void ReserveBatch_WithEnoughSeats_SavesAllReservationsInOneBatchCall()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(true);
        var availability = new SeatAvailabilityStub(Concert, 10).WithEvent(Theatre, 10);
        var service = new ReservationService(availability, repository.Object, new ConfirmationSenderSpy());
        var anna = new Reservation("anna@example.com", Concert, 2);
        var ben = new Reservation("ben@example.com", Theatre, 3);

        // Act
        var result = service.ReserveBatch([anna, ben]);

        // Assert
        Assert.True(result);
        repository.Verify(
            repo => repo.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(list =>
                list.Count == 2 &&
                list[0].CustomerEmail == "anna@example.com" && list[0].EventCode == Concert && list[0].SeatCount == 2 &&
                list[1].CustomerEmail == "ben@example.com" && list[1].EventCode == Theatre && list[1].SeatCount == 3)),
            Times.Once());
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
    }

    [Fact]
    public void ReserveBatch_AfterSuccessfulSave_SendsOneConfirmationPerEntryInInputOrder()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var availability = new SeatAvailabilityStub(Concert, 10).WithEvent(Theatre, 10);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), sender);
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Theatre, 1),
            new Reservation("carla@example.com", Concert, 4),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.True(result);
        Assert.Equal(requests, sender.SentReservations);
    }

    [Fact]
    public void ReserveBatch_WhenBatchSaveFails_ReturnsFalseAndSendsNoConfirmation()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(Concert, 10), repository.Object, sender);
        var requests = new[]
        {
            new Reservation("anna@example.com", Concert, 2),
            new Reservation("ben@example.com", Concert, 2),
        };

        // Act
        var result = service.ReserveBatch(requests);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(list => list.Count == 2)), Times.Once());
        Assert.Empty(sender.SentReservations);
    }
}
