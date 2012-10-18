using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.TapeStorage;
using Mono.Cecil;
//using SaaS.Client;
//using SaaS.Wires;

namespace DDDSample
{
    public static class StartupProjectionRebuilder
    {
        public static void Rebuild(CancellationToken token, IDocumentStore targetContainer, MessageStore stream, Func<IDocumentStore, IEnumerable<object>> projectors)
        {
            var strategy = targetContainer.Strategy;
            var memory = new MemoryStorageConfig();

            var memoryContainer = memory.CreateNuclear(strategy).Container;
            var tracked = new ProjectionInspectingStore(memoryContainer);

            var projections = new List<object>();
            projections.AddRange(projectors(tracked));

            if (tracked.Projections.Count != projections.Count())
                throw new InvalidOperationException("Count mismatch");
            tracked.ValidateSanity();

            var storage = new NuclearStorage(targetContainer);
            var persistedHashes = new Dictionary<string, string>();
            var name = "domain";
            storage.GetEntity<ProjectionHash>(name).IfValue(v => persistedHashes = v.BucketHashes);

            var activeMemoryProjections = projections.Select((projection, i) =>
            {
                var proj = tracked.Projections[i];
                var bucketName = proj.StoreBucket;
                var viewType = proj.EntityType;

                var projectionHash =
                    "Global change on 2012-08-24\r\n" +
                    GetClassHash(projection.GetType()) +
                    "\r\n " + GetClassHash(viewType) + "\r\n" + GetClassHash(strategy.GetType());

                bool needsRebuild = !persistedHashes.ContainsKey(bucketName) || persistedHashes[bucketName] != projectionHash;
                return new
                {
                    bucketName,
                    projection,
                    hash = projectionHash,
                    needsRebuild
                };
            }).ToArray();

            foreach (var memoryProjection in activeMemoryProjections)
            {
                if (memoryProjection.needsRebuild)
                {
                    SystemObserver.Notify("[warn] {0} needs rebuild", memoryProjection.bucketName);
                }
                else
                {
                    SystemObserver.Notify("[good] {0} is up-to-date", memoryProjection.bucketName);
                }
            }


            var needRebuild = activeMemoryProjections.Where(x => x.needsRebuild).ToArray();

            if (needRebuild.Length == 0)
            {
                return;
            }


            var watch = Stopwatch.StartNew();

            var wire = new RedirectToDynamicEvent();
            needRebuild.ForEach(x => wire.WireToWhen(x.projection));


            var handlersWatch = Stopwatch.StartNew();



            ObserveWhileCan(stream.EnumerateAllItems(0, int.MaxValue), wire, token);

            if (token.IsCancellationRequested)
            {
                SystemObserver.Notify("[warn] Aborting projections before anything was changed");
                return;
            }

            var timeTotal = watch.Elapsed.TotalSeconds;
            var handlerTicks = handlersWatch.ElapsedTicks;
            var timeInHandlers = Math.Round(TimeSpan.FromTicks(handlerTicks).TotalSeconds, 1);
            SystemObserver.Notify("Total Elapsed: {0}sec ({1}sec in handlers)", Math.Round(timeTotal, 0), timeInHandlers);


            // update projections that need rebuild
            foreach (var b in needRebuild)
            {
                // server might shut down the process soon anyway, but we'll be
                // in partially consistent mode (not all projections updated)
                // so at least we blow up between projection buckets
                token.ThrowIfCancellationRequested();

                var bucketName = b.bucketName;
                var bucketHash = b.hash;

                // wipe contents
                targetContainer.Reset(bucketName);
                // write new versions
                var contents = memoryContainer.EnumerateContents(bucketName);
                targetContainer.WriteContents(bucketName, contents);

                // update hash
                storage.UpdateEntityEnforcingNew<ProjectionHash>(name, x =>
                {
                    x.BucketHashes[bucketName] = bucketHash;
                });

                SystemObserver.Notify("[good] Updated View bucket {0}.{1}", name, bucketName);
            }

            // Clean up obsolete views
            var allBuckets = new HashSet<string>(activeMemoryProjections.Select(p => p.bucketName));
            var obsoleteBuckets = persistedHashes.Where(s => !allBuckets.Contains(s.Key)).ToArray();
            foreach (var hash in obsoleteBuckets)
            {
                // quit at this stage without any bad side effects
                if (token.IsCancellationRequested)
                    return;

                var bucketName = hash.Key;
                SystemObserver.Notify("[warn] {0} is obsolete", bucketName);
                targetContainer.Reset(bucketName);

                storage.UpdateEntityEnforcingNew<ProjectionHash>(name, x => x.BucketHashes.Remove(bucketName));

                SystemObserver.Notify("[good] Cleaned up obsolete view bucket {0}.{1}", name, bucketName);
            }
        }



        [DataContract]
        public sealed class ProjectionHash
        {
            [DataMember(Order = 1)]
            public Dictionary<string, string> BucketHashes { get; set; }

            public ProjectionHash()
            {
                BucketHashes = new Dictionary<string, string>();
            }
        }


        sealed class ProjectionInspectingStore : IDocumentStore
        {
            readonly IDocumentStore _real;

            public ProjectionInspectingStore(IDocumentStore real)
            {
                _real = real;
            }

            public readonly List<Projection> Projections = new List<Projection>();


            public sealed class Projection
            {
                public Type EntityType;
                public string StoreBucket;
            }

            public void ValidateSanity()
            {
                if (Projections.Count == 0)
                    throw new InvalidOperationException("There were no projections registered");

                var viewsWithMultipleProjections = Projections.GroupBy(e => e.EntityType).Where(g => g.Count() > 1).ToList();
                if (viewsWithMultipleProjections.Count > 0)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Please, define only one projection per view. These views were referenced more than once:");
                    foreach (var projection in viewsWithMultipleProjections)
                    {
                        builder.AppendLine("  " + projection.Key);
                    }
                    builder.AppendLine("NB: you can use partials or dynamics in edge cases");
                    throw new InvalidOperationException(builder.ToString());
                }

                var viewsWithSimilarBuckets = Projections
                    .GroupBy(e => e.StoreBucket.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .ToArray();

                if (viewsWithSimilarBuckets.Length > 0)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine("Following views will be stored in same location, which will cause problems:");
                    foreach (var i in viewsWithSimilarBuckets)
                    {
                        var @join = string.Join(",", i.Select(x => x.EntityType));
                        builder.AppendFormat(" {0} : {1}", i.Key, @join).AppendLine();
                    }
                    throw new InvalidOperationException(builder.ToString());
                }

            }

            public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
            {
                Projections.Add(new Projection()
                {
                    EntityType = typeof(TEntity),
                    StoreBucket = _real.Strategy.GetEntityBucket<TEntity>()
                });

                return _real.GetWriter<TKey, TEntity>();
            }

            public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
            {
                return _real.GetReader<TKey, TEntity>();
            }

            public IDocumentStrategy Strategy
            {
                get { return _real.Strategy; }
            }

            public IEnumerable<DocumentRecord> EnumerateContents(string bucket)
            {
                return _real.EnumerateContents(bucket);
            }

            public void WriteContents(string bucket, IEnumerable<DocumentRecord> records)
            {
                _real.WriteContents(bucket, records);
            }

            public void Reset(string bucket)
            {
                _real.Reset(bucket);
            }
        }



        static string GetClassHash(Type type1)
        {
            var location = type1.Assembly.Location;
            var mod = ModuleDefinition.ReadModule(location);
            var builder = new StringBuilder();
            var type = type1;


            var typeDefinition = mod.GetType(type.FullName);
            builder.AppendLine(typeDefinition.Name);
            ProcessMembers(builder, typeDefinition);

            // we include nested types
            foreach (var nested in typeDefinition.NestedTypes)
            {
                ProcessMembers(builder, nested);
            }

            return builder.ToString();
        }

        static void ProcessMembers(StringBuilder builder, TypeDefinition typeDefinition)
        {
            foreach (var md in typeDefinition.Methods.OrderBy(m => m.ToString()))
            {
                builder.AppendLine("  " + md);

                foreach (var instruction in md.Body.Instructions)
                {
                    // we don't care about offsets
                    instruction.Offset = 0;
                    builder.AppendLine("    " + instruction);
                }
            }
            foreach (var field in typeDefinition.Fields.OrderBy(f => f.ToString()))
            {
                builder.AppendLine("  " + field);
            }
        }


        static void ObserveWhileCan(IEnumerable<StoreRecord> records, RedirectToDynamicEvent wire, CancellationToken token)
        {
            var watch = Stopwatch.StartNew();
            int count = 0;
            foreach (var record in records)
            {
                count += 1;

                if (token.IsCancellationRequested)
                    return;
                if (count % 50000 == 0)
                {
                    SystemObserver.Notify("Observing {0} {1}", count,
                        Math.Round(watch.Elapsed.TotalSeconds, 2));
                    watch.Restart();
                }
                foreach (var message in record.Items)
                {
                    if (message is IEvent)
                    {
                        wire.InvokeEvent(message);
                    }
                }
            }
        }
    }

}