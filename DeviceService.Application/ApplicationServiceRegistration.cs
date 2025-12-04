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
            // Cache invalidation helper
            services.AddSingleton<DeviceCacheInvalidator>();

            return services;
        }
    }
}
