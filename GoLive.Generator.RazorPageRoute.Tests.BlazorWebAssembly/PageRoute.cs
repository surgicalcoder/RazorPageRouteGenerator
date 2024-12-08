// This file was generated on Mon, 02 Dec 2024 15:07:21 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
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

        public class CustomAuthPage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "Admin, superuser";
            public string AuthenticationSchemes { get; set; } = String.Empty;
        }

        /// <summary>
        /// Page Requires Authentication, Custom Authentication Provider (CustomAuth)
        /// Custom Authentication ProviderName: GoLive.Generator.RazorPageRoute.Tests.BlazorWebAssembly.CustomAuth
        /// Custom Auth Constructor Params: [roles, Admin]
        /// Custom Auth Named Params: 
        /// </summary>
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

        public class AuthPage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "Admin";
            public string AuthenticationSchemes { get; set; } = String.Empty;
        }

        /// <summary>
        /// Page Requires Authentication
        /// Roles: Admin
        /// </summary>
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

        public static string NotFound(System.String path)
        {
            string url = $"/{path.ToString()}";
            return url;
        }

        public static void NotFound(this NavigationManager manager, System.String path, bool forceLoad = false, bool replace = false)
        {
            string url = $"/{path.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }
    }
}