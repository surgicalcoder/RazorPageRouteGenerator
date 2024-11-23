// This file was generated on Sat, 23 Nov 2024 17:36:02 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebassembly
{
    public static partial class PageRoutes
    {
        public static string Counter(System.String QSInput = default)
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

        // Requires Auth = False
        public static void Counter(this NavigationManager manager, System.String QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string Counter_View(string id, System.String QSInput = default)
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

        // Requires Auth = False
        public static void Counter_View(this NavigationManager manager, string id, System.String QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string Counter_Viewbyid(System.Int32 id, System.String QSInput = default)
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

        // Requires Auth = False
        public static void Counter_Viewbyid(this NavigationManager manager, System.Int32 id, System.String QSInput = default, bool forceLoad = false, bool replace = false)
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

        public static string CustomAuthPage()
        {
            string url = "/CustomAuthPage";
            return url;
        }

        // Requires Auth = True
        // Custom Auth Name = CustomAuth
        // Custom Auth Ctor Params = [roles, Admin]
        // Custom Auth Named Params = 
        public static void CustomAuthPage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/CustomAuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string AuthPage()
        {
            string url = "/AuthPage";
            return url;
        }

        // Requires Auth = True
        // Roles = Admin
        public static void AuthPage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/AuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string Fetchdata()
        {
            string url = "/fetchdata";
            return url;
        }

        // Requires Auth = False
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

        // Requires Auth = False
        public static void Home(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string NotFound(System.String path)
        {
            string url = $"/{path.ToString()}";
            return url;
        }

        // Requires Auth = False
        public static void NotFound(this NavigationManager manager, System.String path, bool forceLoad = false, bool replace = false)
        {
            string url = $"/{path.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }
    }
}