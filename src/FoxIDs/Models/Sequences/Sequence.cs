﻿using Newtonsoft.Json;

namespace FoxIDs.Models.Sequences
{
    public class Sequence
    {
        public string Id { get; set; }

        public long CreateTime { get; set; }

        public bool? AccountAction { get; set; }

        public string Culture { get; set; }

        public string DownPartyId { get; set; }

        public PartyTypes DownPartyType { get; set; }

        public string UiUpPartyId { get; set; }
    }
}
