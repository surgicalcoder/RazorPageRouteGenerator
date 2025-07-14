// This file was generated on Mon, 14 Jul 2025 11:14:57 GMT
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

        public static string Counter_View(string id)
        {
            string url = $"/counter/view/{id.ToString()}";
            return url;
        }

        public static void Counter_View(this NavigationManager manager, string id, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/view/{id.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string Counter_Viewbyid(System.Int32 id)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            return url;
        }

        public static void Counter_Viewbyid(this NavigationManager manager, System.Int32 id, bool forceLoad = false, bool replace = false)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public static string CustomAuthPage()
        {
            string url = "/CustomAuthPage";
            return url;
        }

        /// <summary>
        /// Page Requires Authentication, Custom Authentication Provider (CustomAuth)
        /// Custom Authentication ProviderName: CustomAuth
        /// Custom Auth Constructor Params: [arg0, [Admin]]
        /// Custom Auth Named Params: 
        /// </summary>
        public static void CustomAuthPage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/CustomAuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public class CustomAuthPage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "[Admin], superuser";
            public string AuthenticationSchemes { get; set; } = String.Empty;
        }

        public static string AuthPage()
        {
            string url = "/AuthPage";
            return url;
        }

        /// <summary>
        /// Page Requires Authentication
        /// Roles: Administrator
        /// </summary>
        public static void AuthPage(this NavigationManager manager, bool forceLoad = false, bool replace = false)
        {
            string url = "/AuthPage";
            manager.NavigateTo(url, forceLoad, replace);
        }

        public class AuthPage_AuthData : IAuthorizeData
        {
            public string Policy { get; set; } = String.Empty;
            public string Roles { get; set; } = "Administrator";
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