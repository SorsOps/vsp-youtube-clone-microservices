﻿using Application.Handlers;
using Infrastructure.MongoDb.Contexts;
using Library.Domain.DomainEvents.UserProfiles;
using Library.Domain.Models;
using MongoDB.Driver;

namespace Library.Infrastructure.DomainEventHandlers.UserProfiles {
    public class UserProfileUpdatedDomainEventHandler : IDomainEventHandler<UserProfileUpdatedDomainEvent> {

        private readonly IMongoCollectionContext<UserProfile> _context;

        public UserProfileUpdatedDomainEventHandler (IMongoCollectionContext<UserProfile> context) {
            _context = context;
        }

        public Task Handle (UserProfileUpdatedDomainEvent @event, CancellationToken cancellationToken) {
            var filterBuilder = Builders<UserProfile>.Filter;

            var filter = filterBuilder.Eq(nameof(UserProfile.Id), @event.Id) &
                         filterBuilder.Lt(nameof(UserProfile.PrimaryVersion), @event.PrimaryVersion);

            var update = Builders<UserProfile>.Update
                .Set(nameof(UserProfile.DisplayName), @event.DisplayName)
                .Set(nameof(UserProfile.Handle), @event.Handle)
                .Set(nameof(UserProfile.ThumbnailUrl), @event.ThumbnailUrl)
                .Set(nameof(UserProfile.PrimaryVersion), @event.PrimaryVersion);

            _context.UpdateOne(filter, update);
            return Task.CompletedTask;
        }
    }
}
