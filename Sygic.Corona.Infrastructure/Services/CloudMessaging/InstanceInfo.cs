﻿using Sygic.Corona.Domain;

namespace Sygic.Corona.Infrastructure.Services.CloudMessaging
{
    public class InstanceInfo
    {
        public string ApplicationVersion { get; set; }
        public string Application { get; set; }
        public string Scope { get; set; }
        public string AuthorizedEntity { get; set; }
        public Platform Platform { get; set; }
    }
}