// This file was generated on Fri, 11 Nov 2022 22:27:45 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebAsssembly
{
    public static class PageRoutes
    {
        public static string counter()
        {
            string url = "/counter";
            return url;
        }

        public static void counter(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/counter";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string counter_view(string id)
        {
            string url = $"/counter/view/{id.ToString()}";
            return url;
        }

        public static void counter_view(this NavigationManager manager, string id, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/view/{id.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string counter_viewbyid(System.Int32 id)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            return url;
        }

        public static void counter_viewbyid(this NavigationManager manager, System.Int32 id, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string fetchdata()
        {
            string url = "/fetchdata";
            return url;
        }

        public static void fetchdata(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/fetchdata";
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
    }
}