﻿// GoingToAbout.cs file version: 1
// -------------------------------
// How to use:
// This source file should be copied to assembly which is supposed to open About page.
// Then please create instance of GoingToAbout, fill its Dto property and call its Go() method.

using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using Dietphone.Tools;
using System.Reflection;

namespace Pabloware.About
{
    internal class GoingToAbout
    {
        public AboutDto Dto { get; private set; }
        private readonly NavigationService service;

        public GoingToAbout(NavigationService service)
        {
            Dto = new AboutDto();
            this.service = service;
        }

        public void Go()
        {
            var target = "/Pabloware.About.Phone;component/Views/About.xaml";
            var queryString = SerializeToQueryString();
            var uri = new Uri(target + queryString, UriKind.Relative);
            service.Navigate(uri);
        }

        private string SerializeToQueryString()
        {
            var builder = new StringBuilder();
            var type = typeof(AboutDto);
            var properties = type.GetRuntimeProperties();
            foreach (var property in properties)
            {
                var getMethod = property.GetMethod;
                if (getMethod != null)
                {
                    var value = getMethod.Invoke(Dto, null);
                    if (builder.Length == 0)
                    {
                        builder.Append("?");
                    }
                    else
                    {
                        builder.Append("&");
                    }
                    builder.Append(property.Name);
                    builder.Append("=");
                    var strValue = value.ToString();
                    strValue = Uri.EscapeDataString(strValue);
                    builder.Append(strValue);
                }
            }
            return builder.ToString();
        }
    }

    internal class AboutDto
    {
        public string AppName { get; set; }
        public string Version { get; set; }
        public string Mail { get; set; }
        public string Url { get; set; }
        public string Publisher { get; set; }
        public string PathToLicense { get; set; }
        public string ChangelogUrl { get; set; }
        public string UiCulture { get; set; }
    }
}
