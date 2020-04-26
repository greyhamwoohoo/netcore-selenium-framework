﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;
using TheInternet.Common.ElementOperations;
using TheInternet.Common.ExecutionContext.Runtime;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings.Contracts;
using TheInternet.Common.ExecutionContext.Runtime.ControlSettings;
using TheInternet.Common.ExecutionContext.Runtime.DeviceSettings;
using TheInternet.Common.ExecutionContext.Runtime.DeviceSettings.Contracts;
using TheInternet.Common.ExecutionContext.Runtime.RemoteWebDriverSettings;
using TheInternet.Common.SessionManagement;
using TheInternet.Common.SessionManagement.Contracts;

namespace TheInternet.Common.Infrastructure
{
    public static class ContainerSingleton
    {
        private static IServiceProvider _instance;
        private static object locker = new object();

        public static IServiceProvider Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null) throw new InvalidOperationException($"You must call Container.Initialize exactly once to initialize the container. ");

                    return _instance;
                }
            }
        }
        public static void Initialize(string prefix)
        {
            Initialize(prefix, (prefix, services) => { });
        }

        public static void Initialize(string prefix, Action<string, IServiceCollection> beforeContainerBuild)
        {
            if (_instance != null) throw new InvalidOperationException($"The Initialize method has already been called. ");
            if (prefix == null) throw new System.ArgumentNullException(nameof(prefix));
            if (beforeContainerBuild == null) throw new System.ArgumentNullException(nameof(beforeContainerBuild));

            System.Environment.SetEnvironmentVariable("TEST_OUTPUT_FOLDER", Directory.GetCurrentDirectory(), EnvironmentVariableTarget.Process);

            var services = new ServiceCollection();

            ConfigureDeviceSettings(prefix, services);
            ConfigureBrowserSettings(prefix, services);
            ConfigureRemoteWebDriverSettings(prefix, services);
            ConfigureEnvironmentSettings(prefix, services);
            ConfigureControlSettings(prefix, services);

            services.AddSingleton<IDriverSessionFactory, DriverSessionFactory>();
            services.AddScoped(sp =>
            {
                var serilogContext = BuildSerilogConfiguration();

                Serilog.ILogger logger = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(serilogContext)
                    .Enrich
                    .FromLogContext()
                    .CreateLogger();

                return logger;
            });

            services.AddScoped(isp =>
            {
                var factory = isp.GetRequiredService<IDriverSessionFactory>();
                var browserProperties = isp.GetRequiredService<IBrowserProperties>();
                var remoteWebDriverSettings = isp.GetRequiredService<RemoteWebDriverSettings>();
                var environmentSettings = isp.GetRequiredService<EnvironmentSettings>();
                var controlSettings = isp.GetRequiredService<IControlSettings>();
                var logger = isp.GetRequiredService<ILogger>();

                var driverSession = factory.Create(browserProperties, remoteWebDriverSettings, environmentSettings, controlSettings, logger);
                return driverSession;
            });

            beforeContainerBuild(prefix, services);

            _instance = services.BuildServiceProvider();
        }

        private static void ConfigureDeviceSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "DeviceSettings", "desktop-selenium-default.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var deviceName = configurationRoot.GetSection("deviceName")?.Value?.ToUpper();
            var deviceSettings = configurationRoot.GetSection("deviceSettings");

            switch(deviceName)
            {
                case "DESKTOP":
                    var instance = new DesktopSettings();

                    instance.DeviceName = deviceName;

                    services.AddSingleton(instance);
                    services.AddSingleton<IDeviceProperties>(instance);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException($"The device called {deviceName} is currently not supported. ");
            }
        }

        private static void ConfigureBrowserSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "BrowserSettings", "default-chrome.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var technology = configurationRoot.GetSection("technology")?.Value?.ToUpper();
            var browserName = configurationRoot.GetSection("browserName")?.Value?.ToUpper();
            var browserSettings = configurationRoot.GetSection("browserSettings");

            switch(browserName)
            {
                case "CHROME":
                    var instance = new ChromeBrowserSettings();
                    
                    browserSettings.Bind(instance);

                    instance = SubstituteEnvironmentVariables<ChromeBrowserSettings>(instance);

                    instance.BrowserName = browserName;
                    instance.Cleanse();

                    services.AddSingleton(instance);
                    services.AddSingleton<IBrowserProperties>(instance);
                    break;
                case "EDGE":
                    var edgeInstance = new EdgeBrowserSettings();

                    browserSettings.Bind(edgeInstance);

                    edgeInstance = SubstituteEnvironmentVariables<EdgeBrowserSettings>(edgeInstance);

                    edgeInstance.BrowserName = browserName;
                    edgeInstance.Cleanse();

                    services.AddSingleton(edgeInstance);
                    services.AddSingleton<IBrowserProperties>(edgeInstance);
                    break;
                case "FIREFOX":
                    var ffInstance = new FireFoxBrowserSettings();

                    browserSettings.Bind(ffInstance);

                    ffInstance = SubstituteEnvironmentVariables<FireFoxBrowserSettings>(ffInstance);

                    ffInstance.BrowserName = browserName;
                    ffInstance.Cleanse();

                    services.AddSingleton(ffInstance);
                    services.AddSingleton<IBrowserProperties>(ffInstance);
                    break;
                case "INTERNETEXPLORER":
                    var ieInstance = new InternetExplorerBrowserSettings();

                    browserSettings.Bind(ieInstance);

                    ieInstance = SubstituteEnvironmentVariables<InternetExplorerBrowserSettings>(ieInstance);

                    ieInstance.BrowserName = browserName;
                    ieInstance.Cleanse();

                    services.AddSingleton(ieInstance);
                    services.AddSingleton<IBrowserProperties>(ieInstance);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException($"The browser called {browserName} is currently not supported. ");
            }
        }

        private static void ConfigureRemoteWebDriverSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "RemoteWebDriverSettings", "localhost-selenium.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var remoteWebDriverSettings = configurationRoot.GetSection("remoteWebDriverSettings");

            var instance = new RemoteWebDriverSettings();

            remoteWebDriverSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables<RemoteWebDriverSettings>(instance);

            instance.Cleanse();

            services.AddSingleton(instance);
        }

        private static void ConfigureControlSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "ControlSettings", "default.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var controlSettings = configurationRoot.GetSection("controlSettings");

            var instance = new ControlSettings();

            controlSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables<ControlSettings>(instance);

            instance.Cleanse();

            services.AddSingleton<IControlSettings, ControlSettings>(isp => instance);
        }

        private static void ConfigureEnvironmentSettings(string prefix, IServiceCollection services)
        {
            var runtimeSettingsUtilities = new RuntimeSettingsUtilities();
            var paths = runtimeSettingsUtilities.GetSettingsFiles(prefix, Path.Combine(Directory.GetCurrentDirectory(), "Runtime"), "EnvironmentSettings", "internet.json");
            var configurationRoot = runtimeSettingsUtilities.Buildconfiguration(prefix, paths);

            var environmentSettings = configurationRoot.GetSection("EnvironmentSettings");

            var instance = new EnvironmentSettings();

            environmentSettings.Bind(instance);

            instance = SubstituteEnvironmentVariables<EnvironmentSettings>(instance);

            instance.Cleanse();

            services.AddSingleton(instance);
        }

        /// <summary>
        /// Brute force any environment variables embedded within the properties of the object. 
        /// TODO: use a ConfigurationProvider to do this
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T SubstituteEnvironmentVariables<T>(T value) where T: class
        {
            var variables = System.Environment.GetEnvironmentVariables();
            var content = JsonConvert.SerializeObject(value);
            if (content.Contains("%"))
            {
                foreach (var variable in variables.Keys.Cast<string>())
                {
                    var candidateString = JsonConvert.ToString(variables[variable]).Trim('"');
                    content = content.Replace($"%{variable.ToUpper()}%", candidateString);
                }

                return JsonConvert.DeserializeObject<T>(content);
            } 
            else
            {
                return value;    
            }
        }

        private static IConfigurationRoot BuildSerilogConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("serilogsettings.json", optional: false)
                .Build();

            return configuration;
        }
    }
}
