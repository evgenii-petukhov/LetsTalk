/**
 * Test helper utilities for IceCandidateMetrics tests
 */

export class IceCandidateMetricsTestHelper {
    /**
     * Creates a mock RTCIceCandidate with the specified type
     */
    static createMockCandidate(
        type: RTCIceCandidateType,
        options: Partial<RTCIceCandidate> = {}
    ): RTCIceCandidate {
        const defaultCandidate: RTCIceCandidate = {
            type,
            candidate: `candidate:1 1 UDP 2113667326 192.168.1.100 54400 typ ${type}`,
            component: "rtcp",
            foundation: '1',
            port: 54400,
            priority: 2113667326,
            protocol: 'udp',
            relatedAddress: null,
            relatedPort: null,
            sdpMLineIndex: 0,
            sdpMid: '0',
            usernameFragment: 'test',
            address: '192.168.1.100',
            toJSON: function() { return this; },
            tcpType: "active"
        };

        return { ...defaultCandidate, ...options };
    }

    /**
     * Creates an array of mock candidates with specified counts for each type
     */
    static createMockCandidateArray(counts: {
        host?: number;
        srflx?: number;
        prflx?: number;
        relay?: number;
    }): RTCIceCandidate[] {
        const candidates: RTCIceCandidate[] = [];

        // Add host candidates
        for (let i = 0; i < (counts.host || 0); i++) {
            candidates.push(this.createMockCandidate('host', {
                foundation: `host-${i}`,
                port: 54400 + i
            }));
        }

        // Add srflx candidates
        for (let i = 0; i < (counts.srflx || 0); i++) {
            candidates.push(this.createMockCandidate('srflx', {
                foundation: `srflx-${i}`,
                port: 55400 + i
            }));
        }

        // Add prflx candidates
        for (let i = 0; i < (counts.prflx || 0); i++) {
            candidates.push(this.createMockCandidate('prflx', {
                foundation: `prflx-${i}`,
                port: 56400 + i
            }));
        }

        // Add relay candidates
        for (let i = 0; i < (counts.relay || 0); i++) {
            candidates.push(this.createMockCandidate('relay', {
                foundation: `relay-${i}`,
                port: 57400 + i
            }));
        }

        return candidates;
    }

    /**
     * Test data scenarios for common use cases
     */
    static getTestScenarios() {
        return {
            empty: {
                candidates: [],
                expectedStats: { host: 0, srflx: 0, prflx: 0, relay: 0 },
                shouldBeSufficient: false,
                shouldPassPreCheck: false
            },
            minimal: {
                candidates: this.createMockCandidateArray({ host: 1, srflx: 1, relay: 1 }),
                expectedStats: { host: 1, srflx: 1, prflx: 0, relay: 1 },
                shouldBeSufficient: true,
                shouldPassPreCheck: true
            },
            optimal: {
                candidates: this.createMockCandidateArray({ host: 2, srflx: 3, prflx: 1, relay: 2 }),
                expectedStats: { host: 2, srflx: 3, prflx: 1, relay: 2 },
                shouldBeSufficient: true,
                shouldPassPreCheck: true
            },
            insufficientNoRelay: {
                candidates: this.createMockCandidateArray({ host: 2, srflx: 3 }),
                expectedStats: { host: 2, srflx: 3, prflx: 0, relay: 0 },
                shouldBeSufficient: false,
                shouldPassPreCheck: true
            },
            insufficientCount: {
                candidates: this.createMockCandidateArray({ host: 1, srflx: 1 }),
                expectedStats: { host: 1, srflx: 1, prflx: 0, relay: 0 },
                shouldBeSufficient: false,
                shouldPassPreCheck: false
            }
        };
    }
}