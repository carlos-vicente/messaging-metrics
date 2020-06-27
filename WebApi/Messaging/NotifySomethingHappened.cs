using System;

namespace WebApi.Messaging
{
    public class NotifySomethingHappened
    {
        public Guid HappeningId { get; set; }
        public String WhatHappened { get; set; }
    }
}