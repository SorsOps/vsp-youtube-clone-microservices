﻿using VideoProcessor.Domain.Models;

namespace VideoProcessor.Application.BackgroundTasks.Processors.VideoThumbnailGenerators {
    public interface IVideoThumbnailGenerator {
        Task<List<VideoThumbnail>> GenerateAsync (
            Video video,
            VideoInfo videoInfo,
            string videoFilePath,
            string tempDirPath,
            CancellationToken cancellationToken = default);
    }
}
