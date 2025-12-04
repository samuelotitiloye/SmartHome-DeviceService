using MediatR;
using Microsoft.Extensions.DependencyInjection;
using DeviceService.Application.Devices.Queries.GetDeviceById;
using DeviceService.Application.Devices.Queries.ListDevices;
using DeviceService.Application.Devices.Commands.UpdateDevice;
using DeviceService.Application.Devices.Commands.RegisterDevice;
using DeviceService.Application.Devices.Commands.DeleteDevice;
using DeviceService.Application.Devices.Caching;
using DeviceService.Application.Devices.Dto;
using DeviceService.Application.Common.Models;
using Scrutor;

namespace DeviceService.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // ------------------------
            //   QUERY DECORATORS
            // ------------------------
            services.Decorate<IRequestHandler<GetDeviceByIdQuery, DeviceDto?>, GetDeviceByIdCachedQueryHandler>();

            services.Decorate<
                IRequestHandler<ListDevicesQuery, PaginatedResult<DeviceDto>>,
                ListDevicesCachedQueryHandler>();

            // ------------------------
            //   COMMAND DECORATORS
            // ------------------------
            services.Decorate<IRequestHandler<UpdateDeviceCommand, DeviceDto>, UpdateDeviceCacheInvalidationHandler>();
            services.Decorate<IRequestHandler<RegisterDeviceCommand, DeviceDto>, RegisterDeviceCacheInvalidationHandler>();
            services.Decorate<IRequestHandler<DeleteDeviceCommand, bool>, DeleteDeviceCacheInvalidationHandler>();

            // Cache invalidation helper
            services.AddSingleton<DeviceCacheInvalidator>();

            return services;
        }
    }
}
