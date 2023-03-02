﻿using Application.Handlers;
using History.Domain.DomainEvents.Videos;

namespace History.API.Application.DomainEventHandlers {
    public class VideoCreatedDomainEventHandler : IDomainEventHandler<VideoCreatedDomainEvent> {

        public Task Handle (VideoCreatedDomainEvent @event, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
