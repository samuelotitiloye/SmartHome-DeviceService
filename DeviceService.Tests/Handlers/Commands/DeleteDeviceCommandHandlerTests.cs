using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Application.Interfaces;
using DeviceService.Domain.Entities;
using Moq;

public class DeleteDeviceCommandHandlerTests
{
    private readonly Mock<IDeviceRepository> _repoMock;
    private readonly DeleteDeviceCommandHandler _handler;

    public DeleteDeviceCommandHandlerTests()
    {
        _repoMock = new Mock<IDeviceRepository>();
        _handler = new DeleteDeviceCommandHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenDeviceNotFound()
    {
        //Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Device?)null);

        var cmd = new DeleteDeviceCommand(Guid.NewGuid());

        //Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.False(result);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletesDevice_WhenFound()
    {
        //Arrange
        var id = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByIdAsync(id))
                 .ReturnsAsync(new Device {Id = id });

        var cmd = new DeleteDeviceCommand(id);

        //Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        //Assert
        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }
}