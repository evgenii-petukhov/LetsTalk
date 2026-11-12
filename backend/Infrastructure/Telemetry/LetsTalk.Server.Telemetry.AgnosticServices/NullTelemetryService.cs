using LetsTalk.Server.Telemetry.Abstractions;
using LetsTalk.Server.Telemetry.Models;

namespace LetsTalk.Server.Telemetry.AgnosticServices;

/// <summary>
/// Telemetry that goes nowhere. It is used when <c>Features:Telemetry</c> is <c>None</c>,
/// so the application runs without any telemetry account.
/// </summary>
public class NullTelemetryService : ITelemetryService
{
    public void TrackStartOutgoingCall(
        string callId,
        string chatId,
        string accountId,
        int iceGatheringMs,
        bool iceCollectedAll,
        ConnectionDiagnostics connectionDiagnostics)
    {
    }

    public void TrackHandleIncomingCall(
        string callId,
        string chatId,
        string accountId,
        int iceGatheringMs,
        bool iceCollectedAll,
        ConnectionDiagnostics connectionDiagnostics)
    {
    }

    public void TrackConnectionEstablished(
        string callId,
        string chatId,
        string accountId,
        ConnectionDiagnostics connectionDiagnostics)
    {
    }

    public void TrackConnectionFailed(
        string callId,
        string chatId,
        string accountId,
        ConnectionDiagnostics connectionDiagnostics,
        RtcErrorType errorType,
        string error,
        string errorMessage,
        string stackTrace)
    {
    }
}
