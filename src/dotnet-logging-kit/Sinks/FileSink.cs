using JG.Logging.Abstractions;
using System.IO;

namespace JG.Logging.Sinks;

/// <summary>
/// Specifies the interval for rolling log files.
/// </summary>
public enum RollingInterval
{
    /// <summary>
    /// No rolling (single file).</summary>
    None = 0,

    /// <summary>
    /// Roll every day at midnight.</summary>
    Day = 1,

    /// <summary>
    /// Roll every hour on the hour.</summary>
    Hour = 2,

    /// <summary>
    /// Roll every month on the first day.</summary>
    Month = 3
}

/// <summary>
/// File sink that writes formatted log entries to rotating log files.
/// </summary>
public sealed class FileSink : ILogSink
{
    private readonly ILogFormatter _formatter;
    private readonly string _directory;
    private readonly string _fileNamePrefix;
    private readonly long _maxFileSizeBytes;
    private readonly RollingInterval _rollingInterval;
    private readonly int _maxBackupFiles;
    private readonly object _lockObject = new();
    private StreamWriter? _currentWriter;
    private string _currentFileName = string.Empty;
    private DateTime _lastRollTime = DateTime.UtcNow;
    private long _currentFileSize;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FileSink class.
    /// </summary>
    /// <param name="formatter">The formatter to use for formatting log entries.</param>
    /// <param name="directory">The directory to write log files to.</param>
    /// <param name="fileNamePrefix">The prefix for log file names.</param>
    /// <param name="maxFileSizeBytes">Maximum size of a single log file in bytes. Default 10MB.</param>
    /// <param name="rollingInterval">Interval for rolling files. Default is None.</param>
    /// <param name="maxBackupFiles">Maximum number of backup files to keep. Default 10.</param>
    public FileSink(
        ILogFormatter formatter,
        string directory,
        string fileNamePrefix = "log",
        long maxFileSizeBytes = 10_485_760,
        RollingInterval rollingInterval = RollingInterval.None,
        int maxBackupFiles = 10)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _fileNamePrefix = fileNamePrefix ?? throw new ArgumentNullException(nameof(fileNamePrefix));
        _maxFileSizeBytes = maxFileSizeBytes;
        _rollingInterval = rollingInterval;
        _maxBackupFiles = Math.Max(1, maxBackupFiles);

        Directory.CreateDirectory(_directory);
    }

    /// <summary>
    /// Writes a formatted log entry to the file.
    /// </summary>
    public async ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var formatted = _formatter.Format(entry);
        lock (_lockObject)
        {
            EnsureWriter();
            var bytes = System.Text.Encoding.UTF8.GetByteCount(formatted + Environment.NewLine);
            CheckAndRollFile(bytes);

            _currentWriter!.WriteLine(formatted);
            _currentFileSize += bytes;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Releases resources associated with this sink.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            _currentWriter?.Dispose();
            _disposed = true;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void EnsureWriter()
    {
        if (_currentWriter != null && !ShouldRoll())
            return;

        _currentWriter?.Dispose();
        _currentFileName = GenerateFileName();
        _currentFileSize = 0;
        _lastRollTime = DateTime.UtcNow;

        var filePath = Path.Combine(_directory, _currentFileName);
        var stream = new FileStream(
            filePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            4096,
            useAsync: false);

        _currentWriter = new StreamWriter(stream) { AutoFlush = true };
    }

    private bool ShouldRoll()
    {
        if (_rollingInterval == RollingInterval.None)
            return _currentFileSize > _maxFileSizeBytes;

        return _rollingInterval switch
        {
            RollingInterval.Day => DateTime.UtcNow.Date > _lastRollTime.Date,
            RollingInterval.Hour => DateTime.UtcNow.Hour > _lastRollTime.Hour,
            RollingInterval.Month => DateTime.UtcNow.Month > _lastRollTime.Month,
            _ => _currentFileSize > _maxFileSizeBytes
        };
    }

    private void CheckAndRollFile(int bytesBeingWritten)
    {
        if (_currentFileSize + bytesBeingWritten <= _maxFileSizeBytes && !ShouldRoll())
            return;

        _currentWriter?.Dispose();
        _lastRollTime = DateTime.UtcNow;
        _currentFileSize = 0;

        _currentFileName = GenerateFileName();
        var filePath = Path.Combine(_directory, _currentFileName);

        var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            4096,
            useAsync: false);

        _currentWriter = new StreamWriter(stream) { AutoFlush = true };

        PruneOldFiles();
    }

    private void PruneOldFiles()
    {
        try
        {
            var files = Directory.GetFiles(_directory, $"{_fileNamePrefix}-*.log")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .Skip(_maxBackupFiles)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Silently ignore pruning errors
        }
    }

    private string GenerateFileName()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        return $"{_fileNamePrefix}-{timestamp}.log";
    }
}
