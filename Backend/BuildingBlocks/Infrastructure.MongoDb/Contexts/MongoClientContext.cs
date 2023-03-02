﻿using Infrastructure.MongoDb.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.MongoDb.Contexts {
    public sealed class MongoClientContext : IMongoClientContext {

        private readonly IServiceProvider _services;
        private readonly MongoDbContextConfiguration _config;
        private readonly object _syncLock;

        private Task<IClientSessionHandle>? _startSessionTask;
        private bool _executingTransactionScope;
        private bool _insideTransactionScope;

        public IMongoClient MongoClient { get; init; }
        public IClientSessionHandle? CurrentSession { get; private set; }
        public bool IsInTransaction => CurrentSession != null && (CurrentSession.IsInTransaction || _insideTransactionScope);

        public MongoClientContext (IServiceProvider services, IMongoClient mongoClient, IOptions<MongoDbContextConfiguration> config) {
            _services = services;
            MongoClient = mongoClient;
            _config = config.Value;
            _syncLock = new object();
        }

        public Task<IClientSessionHandle> StartSessionAsync (ClientSessionOptions? options = null, CancellationToken cancellationToken = default) {
            lock (_syncLock) {
                if (_startSessionTask != null) {
                    return _startSessionTask;
                }

                if (options == null) {
                    options = _config.DefaultClientSessionOptions;
                }

                async Task<IClientSessionHandle> DoStartSession (ClientSessionOptions? options = null, CancellationToken cancellationToken = default) {
                    CurrentSession = await MongoClient.StartSessionAsync(options, cancellationToken);
                    return CurrentSession;
                }

                _startSessionTask = DoStartSession(options, cancellationToken);
                return _startSessionTask;
            }
        }

        public async Task<long> CommitAsync (CancellationToken cancellationToken = default) {
            bool executingRetryScope = _executingTransactionScope;
            var collectionContexts = _services.GetServices<IMongoCollectionContextBase>();

            var writeOperationsCount = collectionContexts.Sum(x => x.WriteOperationsCount);

            try {
                if (writeOperationsCount == 1) {
                    return await CommitSingleAsync(collectionContexts, cancellationToken);
                } else if (writeOperationsCount > 1) {
                    return await CommitInTransactionAsync(collectionContexts, cancellationToken);
                }
            } catch (MongoException) {
                if (CurrentSession != null && CurrentSession.IsInTransaction) {
                    await CurrentSession.AbortTransactionAsync(cancellationToken);
                }

                throw;
            }

            return 0;
        }

        private static async Task<long> CommitSingleAsync (IEnumerable<IMongoCollectionContextBase> collectionContexts, CancellationToken cancellationToken) {
            return (await Task.WhenAll(collectionContexts.Select(x => x.CommitAsync(cancellationToken)))).Sum();
        }

        private async Task<long> CommitInTransactionAsync (IEnumerable<IMongoCollectionContextBase> collectionContexts, CancellationToken cancellationToken) {
            if (CurrentSession != null && CurrentSession.IsInTransaction) {
                return (await Task.WhenAll(collectionContexts.Select(x => x.CommitAsync(cancellationToken)))).Sum();
            } else {
                if (CurrentSession == null) {
                    await StartSessionAsync(_config.DefaultClientSessionOptions, cancellationToken);
                }

                long updatedCount = 0;

                await ExecuteTransactionAsync(async () => {
                    updatedCount = (await Task.WhenAll(collectionContexts.Select(x => x.CommitAsync(cancellationToken)))).Sum();
                }, null, cancellationToken);

                return updatedCount;
            }
        }

        public async Task ExecuteTransactionAsync (Func<Task> task, TransactionOptions? options = null, CancellationToken cancellationToken = default) {
            if (CurrentSession == null) {
                await StartSessionAsync(_config.DefaultClientSessionOptions, cancellationToken);
            }

            lock (_syncLock) {
                if (_executingTransactionScope) {
                    //throw new InvalidOperationException("Transaction scope is being executed");
                    task.Invoke();
                    return;
                }

                _executingTransactionScope = true;
            }

            try {
                await CurrentSession!.WithTransactionAsync<object?>(
                    async (session, cancellationToken) => {
                        try {
                            _insideTransactionScope = true;
                            await task.Invoke();
                        } catch (MongoException ex)
                            when (ex.HasErrorLabel("TransientTransactionError") ||
                                  ex.HasErrorLabel("UnknownTransactionCommitResult")) {
                            await Task.Delay(_config.TransactionRetryDelay);
                            throw;
                        } finally {
                            _insideTransactionScope = false;
                        }
                        return null;
                    },
                    options,
                    cancellationToken);
            } finally {
                lock (_syncLock) {
                    _executingTransactionScope = false;
                }
            }
        }

        public IMongoCollectionContext<TDocument> GetCollection<TDocument> () {
            return _services.GetRequiredService<IMongoCollectionContext<TDocument>>();
        }

        public void Dispose () {
            ResetSession();
        }

        private void ResetSession () {
            if (CurrentSession != null) {
                CurrentSession.Dispose();
                CurrentSession = null;
                _startSessionTask = null;
            }
        }

        public void Reset () {
            var collectionContexts = _services.GetServices<IMongoCollectionContextBase>();
            foreach (var collectionContext in collectionContexts) {
                collectionContext.Reset();
            }
        }
    }
}
