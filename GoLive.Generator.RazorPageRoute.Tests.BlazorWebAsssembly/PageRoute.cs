// This file was generated on Mon, 20 Nov 2023 18:59:19 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebAsssembly
{
    public static partial class PageRoutes
    {
        public static string counter(string QSInput = default)
        {
            string url = "/counter";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            return url;
        }

        public static void counter(this NavigationManager manager, string QSInput = default, bool forceLoad = false, bool replace = false)
        {
            string url = "/counter";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string counter_view(string id, string QSInput = default)
        {
            string url = $"/counter/view/{id.ToString()}";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            return url;
        }

        public static void counter_view(this NavigationManager manager, string id, string QSInput = default, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/view/{id.ToString()}";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string counter_viewbyid(System.Int32 id, string QSInput = default)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            return url;
        }

        public static void counter_viewbyid(this NavigationManager manager, System.Int32 id, string QSInput = default, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            Dictionary<string, string> queryString = new();
            if (!string.IsNullOrWhiteSpace(QSInput))
            {
                queryString.Add("QSInput", QSInput.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string fetchdata(HttpClient Http = default)
        {
            string url = "/fetchdata";
            Dictionary<string, string> queryString = new();
            if (Http != default)
            {
                queryString.Add("Http", Http.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            return url;
        }

        public static void fetchdata(this NavigationManager manager, HttpClient Http = default, bool forceLoad = false, bool replace = false)
        {
            string url = "/fetchdata";
            Dictionary<string, string> queryString = new();
            if (Http != default)
            {
                queryString.Add("Http", Http.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string Home(NavigationManager navi = default)
        {
            string url = "/";
            Dictionary<string, string> queryString = new();
            if (navi != default)
            {
                queryString.Add("navi", navi.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            return url;
        }

        public static void Home(this NavigationManager manager, NavigationManager navi = default, bool forceLoad = false, bool replace = false)
        {
            string url = "/";
            Dictionary<string, string> queryString = new();
            if (navi != default)
            {
                queryString.Add("navi", navi.ToString());
            }

            url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);
            manager.NavigateTo(url, forceLoad, replace);
        }
    }
}