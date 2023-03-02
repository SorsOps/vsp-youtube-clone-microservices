﻿using Application.Handlers;
using Community.Domain.DomainEvents;
using Community.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Community.Infrastructure.DomainEventHandlers {
    public class VideoCommentVoteChangedDomainEventHandler : IDomainEventHandler<VideoCommentVoteChangedDomainEvent> {

        private readonly CommunityDbContext _dbContext;

        public VideoCommentVoteChangedDomainEventHandler (CommunityDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task Handle (VideoCommentVoteChangedDomainEvent @event, CancellationToken cancellationToken) {
            if (_dbContext.Database.CurrentTransaction == null) {
                throw new InvalidOperationException("Transaction is required for this operation");
            }

            var videoCommentVote = @event.VideoCommentVote;

            if (@event.Previous == VoteType.None) {
                switch (@event.Current) {
                    case VoteType.Like:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""LikesCount"" = GREATEST(0, ""LikesCount"" + 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                    case VoteType.Dislike:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""DislikesCount"" = GREATEST(0, ""DislikesCount"" + 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                }
            } else if (@event.Previous == VoteType.Like) {
                switch (@event.Current) {
                    case VoteType.None:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""LikesCount"" = GREATEST(0, ""LikesCount"" - 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                    case VoteType.Dislike:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""LikesCount"" = GREATEST(0, ""LikesCount"" - 1), ""DislikesCount"" = GREATEST(0, ""DislikesCount"" + 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                }
            } else if (@event.Previous == VoteType.Dislike) {
                switch (@event.Current) {
                    case VoteType.None:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""DislikesCount"" = GREATEST(0, ""DislikesCount"" - 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                    case VoteType.Like:
                        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                            $@"UPDATE ""VideoComments"" SET ""LikesCount"" = GREATEST(0, ""LikesCount"" + 1), ""DislikesCount"" = GREATEST(0, ""DislikesCount"" - 1) WHERE ""Id"" = {videoCommentVote.VideoCommentId}");
                        break;
                }
            }
        }

    }
}
