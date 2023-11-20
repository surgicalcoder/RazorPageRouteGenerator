// This file was generated on Mon, 20 Nov 2023 23:11:20 GMT
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
        public static string Counter(string QSInput = default)
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

        public static void Counter(this NavigationManager manager, string QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string Counter_View(string id, string QSInput = default)
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

        public static void Counter_View(this NavigationManager manager, string id, string QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string Counter_Viewbyid(System.Int32 id, string QSInput = default)
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

        public static void Counter_Viewbyid(this NavigationManager manager, System.Int32 id, string QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string Fetchdata()
        {
            string url = "/fetchdata";
            return url;
        }

        public static void Fetchdata(this NavigationManager manager, bool forceLoad = false, bool replace = false)
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