﻿namespace Subscriptions.API.Application.DtoModels {
    public class DetailedUserProfileDto {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string? Handle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public long SubscribersCount { get; set; }
    }
}
