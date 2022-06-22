﻿namespace FoxIDs.Models.Config
{
    public class CacheSettings
    {
        /// <summary>
        /// Time to cache custom domains in seconds (default 24 hours).
        /// </summary>
        public int CustomDomainCacheLifetime { get; set; } = 86400;
    }
}
