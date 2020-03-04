﻿using OpenQA.Selenium.Edge;

namespace TheInternet.Common.ExecutionContext.Runtime.BrowserSettings
{
    public class EdgeBrowserSettingsAdapter
    {
        public EdgeOptions ToEdgeOptions(EdgeBrowserSettings settings)
        {
            var result = new EdgeOptions();
            result.UseInPrivateBrowsing = settings.Options.UseInPrivateBrowsing;
            return result;
        }
    }
}
