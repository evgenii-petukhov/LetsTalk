import { Injectable } from '@angular/core';
import { ConnectionDiagnostics } from '../models/connection-diagnostics';

@Injectable({
    providedIn: 'root',
})
export class RtcConnectionDiagnosticsService {
    public async gatherConnectionDiagnostics(
        connection: RTCPeerConnection,
    ): Promise<ConnectionDiagnostics> {
        const stats = await connection.getStats();

        const diagnostics: ConnectionDiagnostics = {
            connectionState: connection.connectionState,
            localCandidateTypes: {},
            remoteCandidateTypes: {},
            browser: this.getBrowserInfo(),
            platform: navigator.platform,
        };

        stats.forEach((report) => {
            const candidateTypes =
                report.type === 'local-candidate'
                    ? diagnostics.localCandidateTypes
                    : report.type === 'remote-candidate'
                      ? diagnostics.remoteCandidateTypes
                      : null;

            if (candidateTypes && report.candidateType) {
                candidateTypes[report.candidateType] =
                    (candidateTypes[report.candidateType] || 0) + 1;
            }
        });

        return diagnostics;
    }

    private getBrowserInfo(): string {
        const ua = navigator.userAgent;
        if (ua.includes('Chrome')) return 'Chrome';
        if (ua.includes('Firefox')) return 'Firefox';
        if (ua.includes('Safari')) return 'Safari';
        if (ua.includes('Edge')) return 'Edge';
        return 'Unknown';
    }
}
