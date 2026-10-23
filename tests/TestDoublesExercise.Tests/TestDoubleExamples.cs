using Moq;

namespace TestDoublesExercise.Tests;

// Kleines, unabhängiges Beispiel. Für die Reservierungsaufgabe schreibt ihr eigene Doubles.
public interface IStockReader
{
    int GetStock(string productCode);
}

public interface IOrderNotifier
{
    void Notify(string productCode);
}

public sealed class StockCheckService(IStockReader stockReader, IOrderNotifier notifier)
{
    public bool TryOrder(string productCode)
    {
        if (stockReader.GetStock(productCode) <= 0)
        {
            return false;
        }

        notifier.Notify(productCode);
        return true;
    }
}

public sealed class TestDoubleExamples
{
    [Fact]
    public void HandwrittenStub_ControlsTheStockResponse()
    {
        // Arrange: Der Stub liefert den Wert, den dieser Test braucht.
        var stock = new FixedStockStub(0);
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(stock, notifier);

        // Act
        var result = service.TryOrder("BOOK-1");

        // Assert
        Assert.False(result);
        Assert.Empty(notifier.NotifiedProductCodes);
    }

    [Fact]
    public void HandwrittenSpy_RecordsTheNotification()
    {
        // Arrange: Der Spy sammelt Aufrufe, statt eine echte Nachricht zu senden.
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(new FixedStockStub(2), notifier);

        // Act
        var result = service.TryOrder("BOOK-1");

        // Assert
        Assert.True(result);
        Assert.Equal("BOOK-1", Assert.Single(notifier.NotifiedProductCodes));
    }

    [Fact]
    public void Moq_ConfiguresAndVerifiesTheStockReader()
    {
        // Arrange: Moq liefert einen Wert und zeichnet den Methodenaufruf auf.
        var stock = new Mock<IStockReader>();
        stock.Setup(reader => reader.GetStock("BOOK-1")).Returns(2);
        var notifier = new RecordingOrderSpy();
        var service = new StockCheckService(stock.Object, notifier);

        // Act
        var result = service.TryOrder("BOOK-1");

        // Assert
        Assert.True(result);
        stock.Verify(reader => reader.GetStock("BOOK-1"), Times.Once());
        Assert.Equal("BOOK-1", Assert.Single(notifier.NotifiedProductCodes));
    }

    private sealed class FixedStockStub(int availableItems) : IStockReader
    {
        public int GetStock(string productCode) => availableItems;
    }

    private sealed class RecordingOrderSpy : IOrderNotifier
    {
        private readonly List<string> _notifiedProductCodes = [];

        public IReadOnlyList<string> NotifiedProductCodes => _notifiedProductCodes;

        public void Notify(string productCode) => _notifiedProductCodes.Add(productCode);
    }
}
