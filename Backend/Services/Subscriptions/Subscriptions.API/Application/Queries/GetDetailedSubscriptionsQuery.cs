﻿using Application.Contracts;
using Subscriptions.API.Application.DtoModels;
using Subscriptions.Domain.Specifications;

namespace Subscriptions.API.Application.Queries {
    public class GetDetailedSubscriptionsQuery : IQuery<DetailedSubscriptionsDto> {
        public string UserId { get; set; }
        public SubscriptionTargetSort Sort { get; set; }
        public Pagination? Pagination { get; set; }
        public bool IncludeTotalCount { get; set; }

        public GetDetailedSubscriptionsQuery (string userId, SubscriptionTargetSort sort, Pagination? pagination, bool includeTotalCount) {
            UserId = userId;
            Sort = sort;
            Pagination = pagination;
            IncludeTotalCount = includeTotalCount;
        }
    }
}
