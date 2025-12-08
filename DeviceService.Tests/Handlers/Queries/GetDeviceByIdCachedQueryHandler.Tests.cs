using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Queries.GetDeviceById;
using DeviceService.Application.Interfaces;
using MediatR;
using Moq;

public class GetDeviceByIdCachedQueryHandlerTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IRequestHandler<GetDeviceByIdQuery, DeviceDto>> _innerMock;
    private readonly GetDeviceByIdCachedQueryHandler _handler;

    public GetDeviceByIdCachedQueryHandlerTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _innerMock = new Mock<IRequestHandler<GetDeviceByIdQuery, DeviceDto?>>();

        _handler = new GetDeviceByIdCachedQueryHandler(
            _innerMock.Object, 
            _cacheMock.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsCachedValue_WhenCacheHit()
    {
        //Arrange
        var id = Guid.NewGuid();
        var key = DeviceCacheKeys.DeviceById(id);

        var expected = new DeviceDto(
            id, 
            "Name", 
            "Type", 
            "Loc", 
            true, 
            10, 
            "ABC", 
            DateTime.UtcNow
        );

        _cacheMock
            .Setup(c => c.GetAsync<DeviceDto>(key))
            .ReturnsAsync(expected);

        var query = new GetDeviceByIdQuery(id);

        //Act
        var result = await _handler.Handle(query, CancellationToken.None);

        //Assert
        Assert.Equal(expected, result);
        _innerMock.Verify(h => h.Handle(It.IsAny<GetDeviceByIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);   
    
        _cacheMock.Verify(
            c => c.GetAsync<DeviceDto>(key), 
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReturnsInnerValueAndCaches_WhenCacheMiss()
    {
        //Arrange
        var id = Guid.NewGuid();
        var key = DeviceCacheKeys.DeviceById(id);

        _cacheMock
            .Setup(c => c.GetAsync<DeviceDto>(key))
            .ReturnsAsync((DeviceDto?)null);

        var expected = new DeviceDto(id, "Test", "Sensor", "Kitchen", true, 50, "SN123", DateTime.UtcNow);
    

        _innerMock
            .Setup(h => h.Handle(It.IsAny<GetDeviceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var query = new GetDeviceByIdQuery(id);
        
        //Act
        var result = await _handler.Handle(query, CancellationToken.None);

        //Assert
        Assert.Equal(expected, result);

        _innerMock.Verify(
            h => h.Handle(query, It.IsAny<CancellationToken>()), 
            Times.Once
        );

        _cacheMock.Verify(
            c => c.SetAsync(key, expected, 300), 
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_DoesNotCache_WhenInnerReturnsNull()
    {
        //Arrange
        var id = Guid.NewGuid();
        var key = DeviceCacheKeys.DeviceById(id);

        _cacheMock
            .Setup(c => c.GetAsync<DeviceDto>(key))
            .ReturnsAsync((DeviceDto?)null);

        _innerMock
            .Setup(h => h.Handle(It.IsAny<GetDeviceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceDto?)null);

        var query = new GetDeviceByIdQuery(id);

        //Act
        var result = await _handler.Handle(query, CancellationToken.None);

        //Assert
        Assert.Null(result);

        _cacheMock.Verify(
            c => c.SetAsync(It.IsAny<string>(), It.IsAny<DeviceDto?>(), It.IsAny<int>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_UsesCorrectCacheKey()
    {
        //Arrange
        var id = Guid.NewGuid();
        var key = DeviceCacheKeys.DeviceById(id);

        _cacheMock
            .Setup(c => c.GetAsync<DeviceDto>(key))
            .ReturnsAsync((DeviceDto?)null);

        _innerMock
            .Setup(h => h.Handle(It.IsAny<GetDeviceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceDto?)null);

        var query = new GetDeviceByIdQuery(id);

        //Act
        await _handler.Handle(query, CancellationToken.None);

        //Assert
        _cacheMock.Verify(
            c => c.GetAsync<DeviceDto>(key),
            Times.Once
        );
    }
}