﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Revolution.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HookRedirectAttribute : Attribute
    {
        public HookRedirectAttribute(string type, string method)
        {
            Type = type;
            Method = method;
        }

        public string Type { get; set; }
        public string Method { get; set; }
    }
}
