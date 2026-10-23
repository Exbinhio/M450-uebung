using Moq;

namespace TestDoublesExercise.Tests;

// Antworten zu den Aufgaben 1, 3 und 5 stehen in test-doubles/ANTWORTEN.md.
public sealed class ReservationServiceTests
{
    private const string Email = "anna@example.com";
    private const string EventCode = "CONCERT-1";

    // ---------- Aufgabe 1: Verfügbarkeits-Stub ----------

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
    public void Reserve_WithTooFewSeats_ReturnsFalse()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(EventCode, 1);
        var service = new ReservationService(availability, new AlwaysSuccessfulRepository(), new NoOpConfirmationSender());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
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

    // ---------- Aufgabe 2: Grenzfälle und Isolation ----------

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
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("eventCode", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        Assert.Empty(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    // ---------- Aufgabe 3: Bestätigungs-Spy ----------

    [Fact]
    public void Reserve_AfterSuccessfulSave_SendsExactlyOneConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 3), new AlwaysSuccessfulRepository(), sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
        var confirmation = Assert.Single(sender.SentReservations);
        Assert.Equal(Email, confirmation.CustomerEmail);
        Assert.Equal(EventCode, confirmation.EventCode);
        Assert.Equal(2, confirmation.SeatCount);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_SendsNoConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 1), new AlwaysSuccessfulRepository(), sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
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
        Assert.Single(repository.SavedReservations);
        Assert.Empty(sender.SentReservations);
    }

    // ---------- Aufgabe 4: Moq für das Repository ----------

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
        repository.Verify(
            repo => repo.TrySave(It.Is<Reservation>(r =>
                r.CustomerEmail == Email &&
                r.EventCode == EventCode &&
                r.SeatCount == 2)),
            Times.Once());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_WithTooFewSeats_DoesNotCallRepository()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(repo => repo.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(EventCode, 1), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

    // ---------- Aufgabe 5: Handgeschrieben oder Moq? ----------

    [Fact]
    public void Reserve_WithWhitespaceEmail_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        // Verfügbarkeit und Sender: handgeschriebene Spies, weil sie schon existieren und gut lesbar sind.
        // Repository: Moq, weil Verify(..., Times.Never()) das fehlende Speichern direkt ausdrückt.
        var availability = new SeatAvailabilitySpy(10);
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);

        // Act
        Action action = () => service.Reserve("   ", EventCode, 2);

        // Assert
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("customerEmail", exception.ParamName);
        Assert.Empty(availability.RequestedEventCodes);
        repository.Verify(repo => repo.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.SentReservations);
    }
}
