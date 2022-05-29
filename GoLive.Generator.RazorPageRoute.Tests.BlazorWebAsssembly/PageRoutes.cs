using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebAsssembly
{
    public static class PageRoutes
    {
        public static string counter()
        {
            string url = "/counter";
            return url;
        }

        public static string counter_view(string id)
        {
            string url = $"/counter/view/{id.ToString()}";
            return url;
        }

        public static string counter_viewbyid(System.Int32 id)
        {
            string url = $"/counter/viewbyid/{id.ToString()}";
            return url;
        }

        public static string fetchdata()
        {
            string url = "/fetchdata";
            return url;
        }

        public static string Home()
        {
            string url = "/";
            return url;
        }
    }
}