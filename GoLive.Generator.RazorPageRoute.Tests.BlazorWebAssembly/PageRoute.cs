// This file was generated on Thu, 18 Jun 2026 21:09:28 GMT
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebassembly
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

        public static string customauthpage()
        {
            string url = "/CustomAuthPage";
            return url;
        }

        /// <summary>
        /// Page Requires Authentication, Custom Authentication Provider (CustomAuth)
        /// Custom Authentication ProviderName: CustomAuth
        /// Custom Auth Constructor Params: [arg0, new[] { "Admin" }]
        /// Custom Auth Named Params: 
        /// </summary>
        public static void customauthpage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/CustomAuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public class customauthpage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "new[] { \"Admin\" }, superuser";
            public string AuthenticationSchemes { get; set; } = String.Empty;
        }

        public static string authpage()
        {
            string url = "/AuthPage";
            return url;
        }

        /// <summary>
        /// Page Requires Authentication
        /// Roles: Const.Admin
        /// </summary>
        public static void authpage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/AuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public class authpage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "Const.Admin";
            public string AuthenticationSchemes { get; set; } = String.Empty;
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