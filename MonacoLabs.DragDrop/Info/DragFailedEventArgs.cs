namespace MonacoLabs.DragDrop;

/// <summary>
/// Data for <see cref="Abstractions.DragOperationBase.DragFailed"/>.
/// </summary>
public class DragFailedEventArgs(Exception exception) : EventArgs
{
    /// <summary>
    /// The exception the drag threw. On Win32, an exception from a drop handler arrives as the <see cref="System.Runtime.InteropServices.COMException"/>
    /// the platform reduced it to.
    /// </summary>
    public Exception Exception { get; } = exception;
}
