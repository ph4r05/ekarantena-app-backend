﻿using System;
using System.Collections.Generic;

namespace Sygic.Corona.Contracts.Responses
{
    public class GetDeviceWithContactsResponse
    {
        public long Id { get; set; }
        public string DeviceId { get; set; }
        public string CovidPass { get; set; }
        public DateTime? QuarantineBeginning { get; set; }
        public DateTime? QuarantineEnd { get; set; }
        public IList<ContactResponse> Contacts { get; set; }
    }
}