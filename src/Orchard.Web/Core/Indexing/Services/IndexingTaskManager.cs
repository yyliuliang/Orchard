﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Tasks.Scheduling;
using Orchard.Utility.Extensions;
using Orchard.Tasks.Indexing;
using Orchard.Core.Indexing.Models;
using Orchard.Services;

namespace Orchard.Core.Indexing.Services {
    [UsedImplicitly]
    public class IndexingTaskManager : IIndexingTaskManager {
        private readonly IContentManager _contentManager;
        private readonly IRepository<IndexingTaskRecord> _repository;
        private readonly IClock _clock;

        public IndexingTaskManager(
            IContentManager contentManager,
            IRepository<IndexingTaskRecord> repository,
            IClock clock) {
            _clock = clock;
            _repository = repository;
            _contentManager = contentManager;
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        private void CreateTask(ContentItem contentItem, int action) {
            if ( contentItem == null ) {
                throw new ArgumentNullException("contentItem");
            }

            DeleteTasks(contentItem);

            var taskRecord = new IndexingTaskRecord {
                CreatedUtc = _clock.UtcNow,
                ContentItemRecord = contentItem.Record,
                Action = action
            };

            _repository.Create(taskRecord);
            
        }

        public void CreateUpdateIndexTask(ContentItem contentItem) {

            CreateTask(contentItem, IndexingTaskRecord.Update);
            Logger.Information("Indexing task created for [{0}:{1}]", contentItem.ContentType, contentItem.Id);
        }

        public void CreateDeleteIndexTask(ContentItem contentItem) {

            CreateTask(contentItem, IndexingTaskRecord.Delete);
            Logger.Information("Deleting index task created for [{0}:{1}]", contentItem.ContentType, contentItem.Id);
        }

        public IEnumerable<IIndexingTask> GetTasks(DateTime? createdAfter) {
            return _repository
                .Fetch(x => x.CreatedUtc > createdAfter)
                .Select(x => new IndexingTask(_contentManager, x))
                .Cast<IIndexingTask>()
                .ToReadOnlyCollection();
        }

        /// <summary>
        /// Removes existing tasks for the specified content item
        /// </summary>
        public void DeleteTasks(ContentItem contentItem) {
            var tasks = _repository
                .Fetch(x => x.ContentItemRecord.Id == contentItem.Id)
                .ToArray();
            foreach (var task in tasks) {
                _repository.Delete(task);
            }
        }
    }
}
