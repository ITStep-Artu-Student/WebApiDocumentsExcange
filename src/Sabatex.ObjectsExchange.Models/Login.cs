﻿namespace Sabatex.ObjectsExchange.Models
{
#if NET6_0_OR_GREATER
    using System;
    public class Login
    {
        public Guid ClientId { get; set; }
        public string Password { get; set; }  = default!;
    }
#endif

}