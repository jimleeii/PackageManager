using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using System.Xml.Linq;

namespace PackageManager.Core;

/// <summary>
/// Provides the context for a NuGet project.
/// </summary>
internal sealed class ProjectContext : INuGetProjectContext
{
    /// <summary>
    /// Gets or sets the context for extracting packages.
    /// </summary>
    public required PackageExtractionContext PackageExtractionContext { get; set; }

    /// <summary>
    /// Gets the source control manager provider.
    /// </summary>
    public ISourceControlManagerProvider? SourceControlManagerProvider => null;

    /// <summary>
    /// Gets the execution context of the operation.
    /// </summary>
    public NuGet.ProjectManagement.ExecutionContext? ExecutionContext => null;

    /// <summary>
    /// Gets the original packages configuration.
    /// </summary>
    public required XDocument OriginalPackagesConfig { get; set; }

    /// <summary>
    /// Gets or sets the type of action being performed.
    /// </summary>
    public NuGetActionType ActionType { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the operation.
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Reports an error message from a NuGet operation.
    /// </summary>
    /// <param name="message">The log message.</param>
    public void Log(ILogMessage message)
    {
        Log(MessageLevel.Info, message.Message);
    }

    /// <summary>
    /// Reports an error message from a NuGet operation.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void ReportError(string message)
    {
        Log(MessageLevel.Error, message);
    }

    /// <summary>
    /// Reports an error message from a NuGet operation.
    /// </summary>
    /// <param name="message">The error message.</param>
    public void ReportError(ILogMessage message)
    {
        Log(MessageLevel.Error, message.Message);
    }

    /// <summary>
    /// Logs a message at the specified level.
    /// </summary>
    /// <param name="level">The message level.</param>
    /// <param name="message">The message.</param>
    /// <param name="args">The log message arguments.</param>
    public void Log(MessageLevel level, string message, params object[] args) => Console.WriteLine(message, args);

    /// <summary>
    /// Resolves a file conflict.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    /// <returns>The file conflict action.</returns>
    public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.Ignore;
}