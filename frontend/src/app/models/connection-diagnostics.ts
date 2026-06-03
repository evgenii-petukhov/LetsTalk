export interface ConnectionDiagnostics {
    connectionState: RTCPeerConnectionState;
    localCandidateTypes: Record<string, number>;
    remoteCandidateTypes: Record<string, number>;
    browser: string;
    platform: string;
}