﻿using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.Events;
using Serilog;
using System;
using System.Text;
using TheInternet.Common.ElementOperations;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings;
using TheInternet.Common.ExecutionContext.Runtime.BrowserSettings.Contracts;
using TheInternet.Common.ExecutionContext.Runtime.ControlSettings;
using TheInternet.Common.ExecutionContext.Runtime.DeviceSettings;
using TheInternet.Common.ExecutionContext.Runtime.DeviceSettings.Contracts;
using TheInternet.Common.ExecutionContext.Runtime.RemoteWebDriverSettings;
using TheInternet.Common.Reporting.Contracts;
using TheInternet.Common.SessionManagement.Contracts;
using TheInternet.Common.WebDrivers;

namespace TheInternet.Common.SessionManagement
{
    public class DriverSessionFactory : IDriverSessionFactory
    {
        // TODO: Refactor. Too many parameters. 
        public IDriverSession Create(IDeviceProperties deviceProperties, IBrowserProperties browserProperties, RemoteWebDriverSettings remoteWebDriverSettings, EnvironmentSettings environmentSettings, IControlSettings controlSettings, ILogger logger, ITestCaseReporter testCaseReporter)
        {
            if (deviceProperties == null) throw new System.ArgumentNullException(nameof(deviceProperties));
            if (browserProperties == null) throw new System.ArgumentNullException(nameof(browserProperties));
            if (remoteWebDriverSettings == null) throw new System.ArgumentNullException(nameof(remoteWebDriverSettings));
            if (environmentSettings == null) throw new System.ArgumentNullException(nameof(environmentSettings));
            if (controlSettings == null) throw new System.ArgumentNullException(nameof(controlSettings));
            if (logger == null) throw new System.ArgumentNullException(nameof(logger));
            if (testCaseReporter == null) throw new System.ArgumentNullException(nameof(testCaseReporter));

            var browser = default(IWebDriver);

            // There are several kinds of platform we can create our software on:
            // 1. Desktop (ie: 'traditional' Web Browser Automation)
            // 2. Android (Devices)

            if(deviceProperties.Name == "DESKTOP")
            {
                switch (browserProperties.Name)
                {
                    case "CHROME":
                        var chromeBrowserSettings = browserProperties.BrowserSettings as ChromeBrowserSettings;
                        if (null == chromeBrowserSettings) throw new System.InvalidOperationException($"The browserSettings for {browserProperties.Name} are not availble. Were they correctly registered in the Container? ");

                        browser = StartBrowser(remoteWebDriverSettings, chromeBrowserSettings, controlSettings);

                        break;
                    case "EDGE":
                        var edgeBrowserSettings = browserProperties.BrowserSettings as EdgeBrowserSettings;
                        if (null == edgeBrowserSettings) throw new System.InvalidOperationException($"The browserSettings for {browserProperties.Name} are not availble. Were they correctly registered in the Container? ");

                        browser = StartBrowser(remoteWebDriverSettings, edgeBrowserSettings, controlSettings);

                        break;
                    case "INTERNETEXPLORER":
                        var ieBrowserSettings = browserProperties.BrowserSettings as InternetExplorerBrowserSettings;
                        if (null == ieBrowserSettings) throw new System.InvalidOperationException($"The browserSettings for {browserProperties.Name} are not availble. Were they correctly registered in the Container? ");

                        browser = StartBrowser(remoteWebDriverSettings, ieBrowserSettings, controlSettings);

                        break;
                    case "FIREFOX":
                        var ffBrowserSettings = browserProperties.BrowserSettings as FireFoxBrowserSettings;
                        if (null == ffBrowserSettings) throw new System.InvalidOperationException($"The browserSettings for {browserProperties.Name} are not availble. Were they correctly registered in the Container? ");

                        browser = StartBrowser(remoteWebDriverSettings, ffBrowserSettings, controlSettings);

                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException($"There is no support for starting browsers of type {browserProperties.Name}");
                }

                var decoratedWebDriver = new DecoratedWebDriver(browser, controlSettings, testCaseReporter, logger);

                return new DriverSession(decoratedWebDriver, environmentSettings, controlSettings, testCaseReporter);
            }

            if(deviceProperties.Name == "ANDROID")
            {
                if (!remoteWebDriverSettings.UseRemoteDriver)
                {
                    throw new System.InvalidOperationException($"For Appium, we always configure the RemoteWebDriver settings. If running Appium locally, use the 'common-localhost-appium.json' file. ");
                }

                var appiumSettings = deviceProperties.DeviceSettings as AppiumSettings;
                var appiumSettingsAdapter = new AppiumSettingsAdapter();
                var options = appiumSettingsAdapter.ToAppiumOptions(appiumSettings);

                var androidDriver = new AndroidDriver<AppiumWebElement>(new Uri(remoteWebDriverSettings.RemoteUri), options);

                return new DriverSession(new DecoratedWebDriver(androidDriver, controlSettings, testCaseReporter, logger), environmentSettings, controlSettings, testCaseReporter);
            }

            throw new InvalidOperationException($"The device {deviceProperties.Name} is not supported as a Driver Session. ");
        }

        private IWebDriver StartBrowser(RemoteWebDriverSettings remoteWebDriverSettings, ChromeBrowserSettings settings, IControlSettings controlSettings)
        {
            var adapter = new ChromeBrowserSettingsAdapter();
            var chromeOptions = adapter.ToChromeOptions(settings);

            Environment.SetEnvironmentVariable("CHROME_LOG_FILE", null);
            if (settings.Options.CaptureChromeLogFile)
            {
                Environment.SetEnvironmentVariable("CHROME_LOG_FILE", settings.Options.ChromeLogPath);
            }
            if (!remoteWebDriverSettings.UseRemoteDriver)
            {
                var chromeDriver = controlSettings.AttachToExistingSessionIfItExists ? new AttachableChromeDriver(chromeOptions) : new ChromeDriver(chromeOptions);
                return chromeDriver;
            }

            return StartRemoteBrowser(remoteWebDriverSettings, chromeOptions, controlSettings);
        }

        private static IWebDriver StartBrowser(RemoteWebDriverSettings remoteDriverSettings, FireFoxBrowserSettings settings, IControlSettings controlSettings)
        {
            // Reference: https://stackoverflow.com/questions/41644381/python-set-firefox-preferences-for-selenium-download-location
            var adapter = new FireFoxBrowserSettingsAdapter();
            var profile = adapter.ToFirefoxProfile(settings);
            var options = adapter.ToFirefoxOptions(settings);

            // Reference: In .Net Core, we need to explicitly set the encoding
            // https://github.com/SeleniumHQ/selenium/issues/4816
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            options.Profile = profile;

            if (!remoteDriverSettings.UseRemoteDriver)
            {
                return new FirefoxDriver(options);
            }

            return StartRemoteBrowser(remoteDriverSettings, options, controlSettings);
        }

        private static IWebDriver StartBrowser(RemoteWebDriverSettings remoteDriverSettings, InternetExplorerBrowserSettings settings, IControlSettings controlSettings)
        {
            var adapter = new InternetExplorerBrowserSettingsAdapter();
            InternetExplorerOptions options = adapter.ToInternetExplorerOptions(settings);

            if (!remoteDriverSettings.UseRemoteDriver)
            {
                var ieDriver = controlSettings.AttachToExistingSessionIfItExists ? new AttachableInternetExplorerDriver(options) : new InternetExplorerDriver(options);

                return ieDriver;
            }

            return StartRemoteBrowser(remoteDriverSettings, options, controlSettings);
        }

        private static IWebDriver StartBrowser(RemoteWebDriverSettings remoteDriverSettings, EdgeBrowserSettings settings, IControlSettings controlSettings)
        {
            var adapter = new EdgeBrowserSettingsAdapter();
            var options = adapter.ToEdgeOptions(settings);

            if (!remoteDriverSettings.UseRemoteDriver)
            {
                var edgeDriver = controlSettings.AttachToExistingSessionIfItExists ? new AttachableEdgeDriver(options) : new EdgeDriver(options);

                return edgeDriver;
            }

            return StartRemoteBrowser(remoteDriverSettings, options, controlSettings);
        }

        private static IWebDriver StartRemoteBrowser(RemoteWebDriverSettings remoteDriverSettings, DriverOptions options, IControlSettings controlSettings)
        {
            RemoteWebDriver remoteWebDriver = controlSettings.AttachToExistingSessionIfItExists ? new AttachableRemoteWebDriver(new Uri(remoteDriverSettings.RemoteUri), options) : new RemoteWebDriver(new Uri(remoteDriverSettings.RemoteUri), options);
            return remoteWebDriver;
        }
    }
}
