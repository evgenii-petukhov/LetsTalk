import { TestBed } from '@angular/core/testing';
import { RtcConnectionDiagnosticsService } from './rtc-connection-diagnostics.service';
import { beforeEach, describe, expect, it, vi } from 'vitest';

function makeConnection(
    state: RTCPeerConnectionState,
    stats: RTCStatsReport,
): RTCPeerConnection {
    return {
        connectionState: state,
        getStats: vi.fn().mockResolvedValue(stats),
    } as unknown as RTCPeerConnection;
}

function makeStats(
    reports: { type: string; candidateType?: string }[],
): RTCStatsReport {
    const map = new Map<string, unknown>(reports.map((r, i) => [String(i), r]));
    return map as unknown as RTCStatsReport;
}

describe('RtcConnectionDiagnosticsService', () => {
    let service: RtcConnectionDiagnosticsService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(RtcConnectionDiagnosticsService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('gatherConnectionDiagnostics', () => {
        it('should return connectionState from the peer connection', async () => {
            const connection = makeConnection('connected', makeStats([]));
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.connectionState).toBe('connected');
        });

        it('should count local candidate types', async () => {
            const stats = makeStats([
                { type: 'local-candidate', candidateType: 'host' },
                { type: 'local-candidate', candidateType: 'host' },
                { type: 'local-candidate', candidateType: 'srflx' },
            ]);
            const connection = makeConnection('connected', stats);
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.localCandidateTypes).toEqual({ host: 2, srflx: 1 });
        });

        it('should count remote candidate types', async () => {
            const stats = makeStats([
                { type: 'remote-candidate', candidateType: 'relay' },
                { type: 'remote-candidate', candidateType: 'relay' },
            ]);
            const connection = makeConnection('connected', stats);
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.remoteCandidateTypes).toEqual({ relay: 2 });
        });

        it('should ignore reports that are not local or remote candidates', async () => {
            const stats = makeStats([
                { type: 'inbound-rtp', candidateType: 'host' },
                { type: 'outbound-rtp' },
            ]);
            const connection = makeConnection('connected', stats);
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.localCandidateTypes).toEqual({});
            expect(result.remoteCandidateTypes).toEqual({});
        });

        it('should ignore candidate reports without a candidateType', async () => {
            const stats = makeStats([
                { type: 'local-candidate' },
                { type: 'remote-candidate' },
            ]);
            const connection = makeConnection('connected', stats);
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.localCandidateTypes).toEqual({});
            expect(result.remoteCandidateTypes).toEqual({});
        });

        it('should include platform from navigator', async () => {
            const connection = makeConnection('new', makeStats([]));
            const result =
                await service.gatherConnectionDiagnostics(connection);
            expect(result.platform).toBe(navigator.platform);
        });
    });

    describe('getBrowserInfo (via gatherConnectionDiagnostics)', () => {
        async function getBrowser(ua: string): Promise<string> {
            vi.spyOn(navigator, 'userAgent', 'get').mockReturnValue(ua);
            const connection = makeConnection('new', makeStats([]));
            const result =
                await service.gatherConnectionDiagnostics(connection);
            return result.browser;
        }

        it('should detect Chrome', async () => {
            expect(await getBrowser('Mozilla/5.0 Chrome/110')).toBe('Chrome');
        });

        it('should detect Firefox', async () => {
            expect(await getBrowser('Mozilla/5.0 Firefox/110')).toBe('Firefox');
        });

        it('should detect Safari', async () => {
            expect(await getBrowser('Mozilla/5.0 Safari/605')).toBe('Safari');
        });

        it('should detect Edge', async () => {
            expect(await getBrowser('Mozilla/5.0 Edge/110')).toBe('Edge');
        });

        it('should return Unknown for unrecognised UA', async () => {
            expect(await getBrowser('Opera/9.0')).toBe('Unknown');
        });
    });
});
