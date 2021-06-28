using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Flogger.Core.Exceptions;
using Flogger.Core.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.Redis.List;

namespace Flogger.Core.Helpers
{
    public static class Flogger
    {
        private static ILogger _perfLogger;
        private static ILogger _usageLogger;
        private static ILogger _errorLogger;
        private static ILogger _diagnosticLogger;


        static Flogger()
        {
            Configuration = Extension.StaticConfig;

            var performanceTable = Configuration.GetValue("FloggerCore:Tables:Performance", "");
            var usageTable = Configuration.GetValue("FloggerCore:Tables:Usage", "");
            var errorTable = Configuration.GetValue("FloggerCore:Tables:Error", "");
            var diagnosticTable = Configuration.GetValue("FloggerCore:Tables:Diagnostic", "");

            SwitchDriver(configuration: Configuration, performanceTable: performanceTable, usageTable: usageTable,
                errorTable: errorTable, diagnosticTable: diagnosticTable);
        }

        public static IConfiguration Configuration { get; set; }

        private static string GetMessageFromException(Exception ex)
        {
            return ex.InnerException != null ? GetMessageFromException(ex.InnerException) : ex.Message;
        }

        private static string FindProcName(Exception ex)
        {
            var sqlEx = ex as SqlException;
            var procName = sqlEx?.Procedure;
            if (!string.IsNullOrEmpty(procName))
                return procName;

            if (!string.IsNullOrEmpty((string) ex.Data["Procedure"])) return (string) ex.Data["Procedure"];

            return ex.InnerException != null ? FindProcName(ex.InnerException) : null;
        }

        public static ColumnOptions GetSqlColumnOptions()
        {
            var colOptions = new ColumnOptions();
            colOptions.Store.Remove(StandardColumn.Properties);
            colOptions.Store.Remove(StandardColumn.MessageTemplate);
            colOptions.Store.Remove(StandardColumn.Message);
            colOptions.Store.Remove(StandardColumn.Exception);
            colOptions.Store.Remove(StandardColumn.TimeStamp);
            colOptions.Store.Remove(StandardColumn.Level);

            colOptions.AdditionalDataColumns = new Collection<DataColumn>
            {
                new DataColumn {DataType = typeof(DateTime), ColumnName = "Timestamp"},
                new DataColumn {DataType = typeof(string), ColumnName = "Product"},
                new DataColumn {DataType = typeof(string), ColumnName = "Layer"},
                new DataColumn {DataType = typeof(string), ColumnName = "Location"},
                new DataColumn {DataType = typeof(string), ColumnName = "Message"},
                new DataColumn {DataType = typeof(string), ColumnName = "Hostname"},
                new DataColumn {DataType = typeof(string), ColumnName = "UserId"},
                new DataColumn {DataType = typeof(string), ColumnName = "UserName"},
                new DataColumn {DataType = typeof(string), ColumnName = "Exception"},
                new DataColumn {DataType = typeof(int), ColumnName = "ElapsedMilliseconds"},
                new DataColumn {DataType = typeof(string), ColumnName = "CorrelationId"},
                new DataColumn {DataType = typeof(string), ColumnName = "CustomException"},
                new DataColumn {DataType = typeof(string), ColumnName = "AdditionalInfo"}
            };

            return colOptions;
        }

        public static void WritePerf(FlogDetail infoToLog)
        {
            _perfLogger.Write(LogEventLevel.Information,
                "{Timestamp}{Message}{Layer}{Location}{Product}" +
                "{CustomException}{ElapsedMilliseconds}{Exception}{Hostname}" +
                "{UserId}{UserName}{CorrelationId}{AdditionalInfo}",
                infoToLog.Timestamp, infoToLog.Message,
                infoToLog.Layer, infoToLog.Location,
                infoToLog.Product, infoToLog.CustomException,
                infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                infoToLog.Hostname, infoToLog.UserId,
                infoToLog.UserName, infoToLog.CorrelationId,
                infoToLog.AdditionalInfo
            );
        }

        public static void WriteUsage(FlogDetail infoToLog)
        {
            _usageLogger.Write(LogEventLevel.Information,
                "{Timestamp}{Message}{Layer}{Location}{Product}" +
                "{CustomException}{ElapsedMilliseconds}{Exception}{Hostname}" +
                "{UserId}{UserName}{CorrelationId}{AdditionalInfo}",
                infoToLog.Timestamp, infoToLog.Message,
                infoToLog.Layer, infoToLog.Location,
                infoToLog.Product, infoToLog.CustomException,
                infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                infoToLog.Hostname, infoToLog.UserId,
                infoToLog.UserName, infoToLog.CorrelationId,
                infoToLog.AdditionalInfo
            );
        }

        public static void WriteError(FlogDetail infoToLog)
        {
            if (infoToLog.Exception != null)
            {
                var procName = FindProcName(infoToLog.Exception);
                infoToLog.Location = string.IsNullOrEmpty(procName) ? infoToLog.Location : procName;
                infoToLog.Message = GetMessageFromException(infoToLog.Exception);
            }

            //_errorLogger.Write(LogEventLevel.Information, "{@FlogDetail}", infoToLog);            
            _errorLogger.Write(LogEventLevel.Error,
                "{Timestamp}{Message}{Layer}{Location}{Product}" +
                "{CustomException}{ElapsedMilliseconds}{Exception}{Hostname}" +
                "{UserId}{UserName}{CorrelationId}{AdditionalInfo}",
                infoToLog.Timestamp, infoToLog.Message,
                infoToLog.Layer, infoToLog.Location,
                infoToLog.Product, infoToLog.CustomException,
                infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                infoToLog.Hostname, infoToLog.UserId,
                infoToLog.UserName, infoToLog.CorrelationId,
                infoToLog.AdditionalInfo
            );
        }

        public static void WriteDiagnostic(FlogDetail infoToLog)
        {
            var writeDiagnostics = Convert.ToBoolean(Configuration.GetValue("FloggerCore:EnableDiagnostic", false));
            if (!writeDiagnostics)
                return;

            _diagnosticLogger.Write(LogEventLevel.Information,
                "{Timestamp}{Message}{Layer}{Location}{Product}" +
                "{CustomException}{ElapsedMilliseconds}{Exception}{Hostname}" +
                "{UserId}{UserName}{CorrelationId}{AdditionalInfo}",
                infoToLog.Timestamp,
                infoToLog.Message,
                infoToLog.Layer, infoToLog.Location,
                infoToLog.Product, infoToLog.CustomException,
                infoToLog.ElapsedMilliseconds, infoToLog.Exception?.ToBetterString(),
                infoToLog.Hostname, infoToLog.UserId,
                infoToLog.UserName, infoToLog.CorrelationId,
                infoToLog.AdditionalInfo
            );
        }


        private static void SqlServerSelect(string connection, string performanceTable, string usageTable,
            string errorTable, string diagnosticTable)
        {
            _perfLogger = new LoggerConfiguration()
                .WriteTo.MSSqlServer(connection, performanceTable, autoCreateSqlTable: true,
                    columnOptions: GetSqlColumnOptions(), batchPostingLimit: 20)
                .CreateLogger();

            _usageLogger = new LoggerConfiguration()
                .WriteTo.MSSqlServer(connection, usageTable, autoCreateSqlTable: true,
                    columnOptions: GetSqlColumnOptions(), batchPostingLimit: 20)
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
                .WriteTo.MSSqlServer(connection, errorTable, autoCreateSqlTable: true,
                    columnOptions: GetSqlColumnOptions(), batchPostingLimit: 20)
                .CreateLogger();


            _diagnosticLogger = new LoggerConfiguration()
                .WriteTo.MSSqlServer(connection, diagnosticTable, autoCreateSqlTable: true,
                    columnOptions: GetSqlColumnOptions(), batchPostingLimit: 20)
                .CreateLogger();
        }

        private static void MongoSelect()
        {
        }

        private static void FileSelect(IConfiguration configuration)
        {
            var basedir = Directory.GetCurrentDirectory();
            var performancePath = configuration.GetValue("FloggerCore:Driver:File:PerformancePath", "");
            var usagePath = configuration.GetValue("FloggerCore:Driver:File:UsagePath", "");
            var errorPath = configuration.GetValue("FloggerCore:Driver:File:ErrorPath", "");
            var diagnosticPath = configuration.GetValue("FloggerCore:Driver:File:DiagnosticPath", "");
            var fileInterval = configuration.GetValue("FloggerCore:Driver:File:Interval", "Day");

            if (!Enum.TryParse<RollingInterval>(fileInterval, true, out var interval))
                interval = RollingInterval.Day;

            _perfLogger = new LoggerConfiguration()
                .WriteTo.File($"{basedir}/{performancePath}", rollingInterval: interval)
                .CreateLogger();

            _usageLogger = new LoggerConfiguration()
                .WriteTo.File($"{basedir}/{usagePath}", rollingInterval: interval)
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
                .WriteTo.File($"{basedir}/{errorPath}", rollingInterval: interval)
                .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration()
                .WriteTo.File($"{basedir}/{diagnosticPath}", rollingInterval: interval)
                .CreateLogger();
        }

        private static void RedisSelect(IConfiguration configuration)
        {
            _perfLogger = new LoggerConfiguration()
                .WriteTo.RedisList(configuration.GetValue("FloggerCore:Driver:Redis:Host", ""), "perfLogger")
                .CreateLogger();

            _usageLogger = new LoggerConfiguration()
                .WriteTo.RedisList(configuration.GetValue("FloggerCore:Driver:Redis:Host", ""), "usageLogger")
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
                .WriteTo.RedisList(configuration.GetValue("FloggerCore:Driver:Redis:Host", ""), "errorLogger")
                .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration()
                .WriteTo.RedisList(configuration.GetValue("FloggerCore:Driver:Redis:Host", ""), "diagnosticLogger")
                .CreateLogger();
        }

        private static void SeqSelect(IConfiguration configuration)
        {
            var url = configuration.GetValue("FloggerCore:Driver:Seq:Url", "");
            var token = configuration.GetValue("FloggerCore:Driver:Seq:Token", "");

            _perfLogger = new LoggerConfiguration()
                .WriteTo.Seq(url, apiKey: token)
                .CreateLogger();

            _usageLogger = new LoggerConfiguration()
                .WriteTo.Seq(url, apiKey: token)
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
                .WriteTo.Seq(url, apiKey: token)
                .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration()
                .WriteTo.Seq(url, apiKey: token)
                .CreateLogger();
        }

        private static void ConsoleSelect()
        {
            _perfLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            _usageLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            _errorLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            _diagnosticLogger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static void SwitchDriver(IConfiguration configuration, string performanceTable, string usageTable,
            string errorTable, string diagnosticTable)
        {
            var sqlServerDriver = configuration.GetValue("FloggerCore:Driver:SqlServer:Enable", false);
            var mongoDriver = configuration.GetValue("FloggerCore:Driver:Mongo:Enable", false);
            var redisDriver = configuration.GetValue("FloggerCore:Driver:Redis:Enable", false);
            var fileDriver = configuration.GetValue("FloggerCore:Driver:File:Enable", false);
            var seqDriver = configuration.GetValue("FloggerCore:Driver:Seq:Enable", false);
            var consoleDriver = configuration.GetValue("FloggerCore:Driver:Console:Enable", false);

            if (sqlServerDriver)
                SqlServerSelect(configuration.GetValue("FloggerCore:Driver:SqlServer:Connection", ""),
                    performanceTable,
                    usageTable,
                    errorTable, diagnosticTable);

            if (mongoDriver)
                MongoSelect();

            if (fileDriver)
                FileSelect(configuration);

            if (redisDriver)
                RedisSelect(configuration);

            if (seqDriver)
                SeqSelect(configuration);

            if (consoleDriver)
                ConsoleSelect();
        }


        private static LogEventLevel FetchLogEventLevel(string level)
        {
            return Enum.TryParse<LogEventLevel>(level, true, out var logLevel)
                ? logLevel
                : LogEventLevel.Information;
        }
    }
}