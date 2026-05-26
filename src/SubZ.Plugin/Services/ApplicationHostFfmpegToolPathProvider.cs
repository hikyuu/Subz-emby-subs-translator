using System;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common;
using MediaBrowser.Controller.MediaEncoding;

namespace SubZ.Plugin.Services;

public sealed class ApplicationHostFfmpegToolPathProvider : IFfmpegToolPathProvider
{
    private readonly IApplicationHost _applicationHost;

    public ApplicationHostFfmpegToolPathProvider(IApplicationHost applicationHost)
    {
        _applicationHost = applicationHost ?? throw new ArgumentNullException(nameof(applicationHost));
    }

    public object? ResolveFfmpegManager()
    {
        var managerType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType("MediaBrowser.Controller.MediaEncoding.IFfmpegManager", throwOnError: false))
            .FirstOrDefault(t => t != null);

        return managerType == null ? null : TryResolve(managerType);
    }

    public object? ResolveFfmpegConfiguration()
    {
        try
        {
            return _applicationHost.TryResolve<IFfmpegConfiguration>();
        }
        catch
        {
            return null;
        }
    }

    private object? TryResolve(Type serviceType)
    {
        try
        {
            var method = typeof(IApplicationHost)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "TryResolve"
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 0);

            return method?.MakeGenericMethod(serviceType).Invoke(_applicationHost, null);
        }
        catch
        {
            return null;
        }
    }
}
