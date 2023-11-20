// This file was generated on Mon, 20 Nov 2023 23:11:16 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebAsssembly3
{
    public static partial class PageRoutes
    {
        public static string Counter()
        {
            string url = "/counter";
            return url;
        }

        public static void Counter(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/counter";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string Home()
        {
            string url = "/";
            return url;
        }

        public static void Home(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string Weather()
        {
            string url = "/weather";
            return url;
        }

        public static void Weather(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/weather";
            manager.NavigateTo(url, forceLoad, replace);
        }
    }
}