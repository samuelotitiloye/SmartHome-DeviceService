using DeviceService.Application.Devices.Queries.ListDevices;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Devices.Models;
using DeviceService.Application.Common.Models;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using Moq;

public class ListDevicesQueryHandlerTests
{
    private readonly Mock<IDeviceRepository> _repoMock;
    private readonly ListDevicesQueryHandler _handler;

    public ListDevicesQueryHandlerTests()
    {
        _repoMock = new Mock<IDeviceRepository>();
        _handler = new ListDevicesQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedPaginatedResult()
    {
        // Arrange
        var filter = new DeviceFilter { NameContains = "Test" };
        var pagination = new PaginationParameters(pageNumber: 1, pageSize: 10);

        var domainDevices = new List<Device>
        {
            new Device
            {
                Id = Guid.NewGuid(),
                Name = "Test Device 1",
                Type = "sensor",
                Location = "Kitchen",
                IsOnline = true,
                ThresholdWatts = 50,
                SerialNumber = "ABC123"
            },
            new Device
            {
                Id = Guid.NewGuid(),
                Name = "Test Device 2",
                Type = "actuator",
                Location = "Office",
                IsOnline = false,
                ThresholdWatts = 0,        // int, not nullable
                SerialNumber = "XYZ789"
            }
        };

        var repoResult = new PaginatedResult<Device>(
        items: domainDevices,
        pageNumber: 1,
        pageSize: 10,
        totalCount: 10
    );


        _repoMock.Setup(r =>
            r.GetDevicesAsync(
                filter,
                pagination,
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(repoResult);

        var query = new ListDevicesQuery(filter, pagination);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repoResult.TotalCount, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);

       // Verify mapping to DTO
        var items = result.Items.ToList();

        Assert.Equal(domainDevices.Count, items.Count);
        Assert.Equal(domainDevices[0].Id, items[0].Id);
        Assert.Equal(domainDevices[0].Name, items[0].Name);
        Assert.Equal(domainDevices[1].Id, items[1].Id);


        _repoMock.Verify(r =>
            r.GetDevicesAsync(filter, pagination, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ReturnsEmptyResult_WhenRepositoryReturnsEmpty()
    {
        // Arrange
        var filter = new DeviceFilter();
        var pagination = new PaginationParameters(pageSize: 10, pageNumber: 1);

        var repoResult = new PaginatedResult<Device>(
        items: new List<Device>(),
        pageNumber: 1,
        pageSize: 10,
        totalCount: 10
    );



        _repoMock.Setup(r =>
            r.GetDevicesAsync(
                filter,
                pagination,
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(repoResult);

        var query = new ListDevicesQuery(filter, pagination);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(repoResult.TotalCount, result.TotalCount);


        _repoMock.Verify(r =>
            r.GetDevicesAsync(filter, pagination, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
