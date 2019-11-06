using System;
using System.Diagnostics;
using System.Threading;
using Prometheus;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Elasticsearch;

namespace FakeLogger
{
    class Program
    {
        static Random rnd = new Random();
        static Logger log;
        static Counter logMetrics;
        static ulong globalLogCounter = 0;

        static void Err(string msgTmpl, params object[] args)
        {
            log.Error(msgTmpl, args);
            logMetrics.WithLabels(new [] { "Error" }).Inc();
        }

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
            var p = rnd.Next(0, 2300);

            if (p == 0)         Err("{@counter} {@scope} Something terrible went wrong {@thread}[{@db}]",                                                                                       globalLogCounter, "Database.Disk", "DatabaseOperationThread", db);
            else if (p <= 10)   Dbg("{@counter} {@scope} Commit {@ssv} done (took: 123ms) {@thread}[{@db}]",                                                                                    globalLogCounter, "Database.Disk", ssv, "DatabaseOperationThread", db);
            else if (p <= 20)   Dbg("{@counter} {@scope} Committing {@ssv}... {@thread}[{@db}]",                                                                                                globalLogCounter, "Database.Disk", ssv, "DatabaseOperationThread", db);
            else if (p <= 30)   Dbg("{@counter} {@scope} New snapshot version {@ssv} was built for database '{@db}', new document count {@docCount}, took XXXms {@thread}",                     globalLogCounter, "Database.Snapshot", ssv, db, docCount, "SnapshotWorker#25");
            else if (p <= 40)   Dbg("{@counter} {@scope} No changes for snapshot version {@ssv} of '{@db}', document count: {@docCount} {@thread}",                                             globalLogCounter, "Database.Snapshot", ssv, db, docCount, "SnapshotWorker#25");
            else if (p <= 70)   Dbg("{@counter} {@scope} 1 snapshot creations were skipped for '{@db}'. {@thread}",                                                                             globalLogCounter, "Database.Snapshot", db, "SnapshotWorker#25");
            else if (p <= 160)  Inf("{@counter} {@scope} Received LOG_ENTRIES (7 entries), scheduling now {@thread}",                                                                           globalLogCounter, "InternalCommunication.Replication", "Replication thread");
            else if (p <= 250)  Dbg("{@counter} {@scope} Sending GetLogEntries(DataspaceID: 426f86bca105913b, Versions: {@ssv}) {@thread}",                                                     globalLogCounter, "InternalCommunication.Replication", ssv, "Replication thread");
            else if (p <= 350)  Dbg("{@counter} {@scope} 7 entries scheduled {@thread}",                                                                                                        globalLogCounter, "InternalCommunication.Replication", "Replication thread");
            else if (p <= 450)  Dbg("{@counter} {@scope} No more work items for background snapshot creations. {@thread}",                                                                      globalLogCounter, "Database.Snapshot", "SnapshotWorker#25");
            else if (p <= 670)  Dbg("{@counter} {@scope} Increased snapshot version for '{@db}' from {@ssvOld} to version {@ssvNew}, document count: {doccount} {@thread}",                     globalLogCounter, "Database.Snapshot", db, ssv, ssv+2, "SnapshotWorker#27");
            else if (p <= 900)  Inf("{@counter} {@scope} Snapshot version {@ssv} for '{@db}' has been disposed, took 0 ms. {@thread}",                                                          globalLogCounter, "Database.Snapshot", ssv, db, "SnapshotWorker#25");
            else if (p <= 1140) Dbg("{@counter} {@scope} 1kB have been written to position 99 in file '~\\SomeWhere\\Over\\The\\Rainbow\\Index\\DataLog\\271e16.datalog' (took 0ms) {@thread}", globalLogCounter, "Dataspace.DataLog", "Replication thread");
            else if (p <= 1410) Dbg("{@counter} {@scope} Increasing version to {@ssv} without actual changes. {@thread}[{@db}]",                                                                globalLogCounter, "Database.Disk", ssv, "DatabaseOperationThread", db);
            else if (p <= 1690) Dbg("{@counter} {@scope} Data log entry 393805899 of file 271e16.datalog marked as executed (with success). {@thread}[{@db}]",                                  globalLogCounter, "Dataspace.DataLog", "DatabaseOperationThread", db);
            else if (p <= 1970) Inf("{@counter} {@scope} Snapshot update scheduled for version {@ssv} {@thread}[{@db}]",                                                                        globalLogCounter, "Database.Snapshot", ssv, "DatabaseOperationThread", db);
            else                Dbg("{@counter} {@scope} Recovered operation (UpdateDocumentsOperationData, Guid: f6ceb5df-03d2-41bd-a69f-8e70a23e83cc, Version: 393805899) {@thread}",         globalLogCounter, "InternalCommunication.Replication", "Replication thread");

            globalLogCounter++;
        }

        static void Main(string[] args)
        {
            var running = new Stopwatch();
            running.Start();

            log = new LoggerConfiguration()
                .WriteTo.Console(new ElasticsearchJsonFormatter(
                    renderMessage: true,
                    renderMessageTemplate: true
                ))
                .CreateLogger();

            var metricServer = new MetricServer(port: 8000);
            metricServer.Start();

            var pressureMetrics = Metrics.CreateGauge("app_current_pressure_percent", "Current logging pressure.");
            logMetrics = Metrics.CreateCounter("app_logged_msg_total", "Number of logged messages.", new [] { "level" });

            var msgCounter = 0;
            while(true)
            {
                GenerateLogEntry();
                msgCounter = (msgCounter + 1) % 300;
                if (msgCounter == 0)
                {
                    // Time based sinus wave of logging pressure.
                    var pressure = Math.Cos(Math.PI*(running.ElapsedMilliseconds)/100000)
                        + Math.Cos(Math.PI*(running.ElapsedMilliseconds)/433000);
                    pressure = (2 + pressure) / 4;
                    pressure = pressure * (0.9 + 0.1 * rnd.NextDouble());
                    pressureMetrics.Set(pressure);
                    var sleep = 1 + 1000 * (1.0 - pressure);
                    Thread.Sleep((int)sleep);
                };
            }
        }
    }
}
