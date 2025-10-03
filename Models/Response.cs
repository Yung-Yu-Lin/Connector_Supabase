using System;
namespace LIS_Middleware.Models
{
    public class Response
    {
        public bool success { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    }
}
