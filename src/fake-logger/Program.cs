﻿using System;
using System.Threading;
using Prometheus;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Elasticsearch;

namespace FakeLogger
{
    class Program
    {
        static Random rnd = new Random();
        static Logger log;
        static Counter logMetrics;

        static void Dbg(string msgTmpl, params object[] args)
        {
            log.Debug(msgTmpl, args);
            logMetrics.WithLabels(new [] { "Debug" }).Inc();
        }

        static void Inf(string msgTmpl, params object[] args)
        {
            log.Information(msgTmpl, args);
            logMetrics.WithLabels(new [] { "Info" }).Inc();
        }

        static void GenerateLogEntry()
        {
            var ssv = rnd.Next(50000, 20000000);
            var docCount = rnd.Next(50, 2000000);
            var db = (new []{ "Bla Blub EN", "Blub EN", "Foo Boo Zoo News DE", "Arrrrrrrhhh_DE" })[rnd.Next(0, 3)];
            var p = rnd.Next(1, 230);

            if (p == 1)   Dbg("{@scope} Commit {@ssv} done (took: 123ms) {@thread}[{@db}]", "Database.Disk", ssv, "DatabaseOperationThread", db);
            if (p <= 2)   Dbg("{@scope} Committing {@ssv}... {@thread}[{@db}]", "Database.Disk", ssv, "DatabaseOperationThread", db);
            if (p <= 3)   Dbg("{@scope} New snapshot version {@ssv} was built for database '{@db}', new document count {@docCount}, took XXXms {@thread}", "Database.Snapshot", ssv, db, docCount, "SnapshotWorker#25");
            if (p <= 4)   Dbg("{@scope} No changes for snapshot version {@ssv} of '{@db}', document count: {@docCount} {@thread}", "Database.Snapshot", ssv, db, docCount, "SnapshotWorker#25");
            if (p <= 7)   Dbg("{@scope} 1 snapshot creations were skipped for '{@db}'. {@thread}", "Database.Snapshot", db, "SnapshotWorker#25");
            if (p <= 16)  Inf("{@scope} Received LOG_ENTRIES (7 entries), scheduling now {@thread}", "InternalCommunication.Replication", "Replication thread");
            if (p <= 25)  Dbg("{@scope} Sending GetLogEntries(DataspaceID: 426f86bca105913b, Versions: {@ssv}) {@thread}", "InternalCommunication.Replication", ssv, "Replication thread");
            if (p <= 35)  Dbg("{@scope} 7 entries scheduled {@thread}", "InternalCommunication.Replication", "Replication thread");
            if (p <= 45)  Dbg("{@scope} No more work items for background snapshot creations. {@thread}", "Database.Snapshot", "SnapshotWorker#25");
            if (p <= 67)  Dbg("{@scope} Increased snapshot version for '{@db}' from {@ssvOld} to version {@ssvNew}, document count: {doccount} {@thread}", "Database.Snapshot", db, ssv, ssv+2, "SnapshotWorker#27");
            if (p <= 90)  Inf("{@scope} Snapshot version {@ssv} for '{@db}' has been disposed, took 0 ms. {@thread}", "Database.Snapshot", ssv, db, "SnapshotWorker#25");
            if (p <= 114) Dbg("{@scope} 1kB have been written to position 99 in file '~\\SomeWhere\\Over\\The\\Rainbow\\Index\\DataLog\\271e16.datalog' (took 0ms) {@thread}", "Dataspace.DataLog", "Replication thread");
            if (p <= 141) Dbg("{@scope} Increasing version to {@ssv} without actual changes. {@thread}[{@db}]", "Database.Disk", ssv, "DatabaseOperationThread", db);
            if (p <= 169) Dbg("{@scope} Data log entry 393805899 of file 271e16.datalog marked as executed (with success). {@thread}[{@db}]", "Dataspace.DataLog", "DatabaseOperationThread", db);
            if (p <= 197) Inf("{@scope} Snapshot update scheduled for version {@ssv} {@thread}[{@db}]", "Database.Snapshot", ssv, "DatabaseOperationThread", db);
            else          Dbg("{@scope} Recovered operation (UpdateDocumentsOperationData, Guid: f6ceb5df-03d2-41bd-a69f-8e70a23e83cc, Version: 393805899) {@thread}", "InternalCommunication.Replication", "Replication thread");
        }

        static void Main(string[] args)
        {
            log = new LoggerConfiguration()
                .WriteTo.Console(new ElasticsearchJsonFormatter(
                    renderMessage: true,
                    renderMessageTemplate: true
                ))
                .CreateLogger();

            var metricServer = new MetricServer(port: 8000);
            metricServer.Start();

            logMetrics = Metrics.CreateCounter("app_logged_msg_total", "Number of logged messages.",
                new CounterConfiguration { LabelNames = new [] { "level" }}
            );

            var sleepCounter = 0;
            while(true)
            {
                GenerateLogEntry();
                sleepCounter = ++sleepCounter % 1000;
                if (sleepCounter == 1) Thread.Sleep(rnd.Next(10, 5000));
            }
        }
    }
}