using DeviceService.Application.Devices.Queries.GetDeviceById;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Interfaces;
using Moq;

public class GetDeviceByIdQueryHandlerTests
{
    private readonly Mock<IDevicesService> _serviceMock;
    private readonly GetDeviceByIdQueryHandler _handler;

    public GetDeviceByIdQueryHandlerTests()
    {
        _serviceMock = new Mock<IDevicesService>();
        _handler = new GetDeviceByIdQueryHandler(_serviceMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsDeviceDto_WhenDeviceExists()
    {
        var id = Guid.NewGuid();

        var expected = new DeviceDto(
            id,
            "Test Device",
            "sensor",
            "Kitchen",
            true,
            50,
            "ABC123",
            DateTime.UtcNow
        );

        _serviceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var query = new GetDeviceByIdQuery(id);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);

        _serviceMock.Verify(
            s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenDeviceNotFound()
    {
        var id = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceDto?)null);

        var query = new GetDeviceByIdQuery(id);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Null(result);

        _serviceMock.Verify(
            s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
