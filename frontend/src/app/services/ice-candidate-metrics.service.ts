import { Injectable } from '@angular/core';
import { IceCandidateMetrics } from '../models/ice-candidate-metrics';

@Injectable({
    providedIn: 'root',
})
export class IceCandidateMetricsService {
    private readonly required: IceCandidateMetrics = {
        host: 1,
        srflx: 1,
        prflx: 0,
        relay: 1,
    };

    hasSufficientServers(candidates: RTCIceCandidate[]): boolean {
        const metrics = this.getIceCandidateMetrics(candidates);

        return (
            metrics.host >= this.required.host &&
            metrics.srflx >= this.required.srflx &&
            metrics.relay >= this.required.relay
        );
    }

    hasMinimumCandidateCount(candidates: RTCIceCandidate[]) {
        return candidates.length >= this.getRequiredTotal();
    }

    private getIceCandidateMetrics(candidates: RTCIceCandidate[]): IceCandidateMetrics {
        const result: IceCandidateMetrics = {
            host: 0,
            srflx: 0,
            prflx: 0,
            relay: 0,
        };

        for (const candidate of candidates) {
            result[candidate.type] = result[candidate.type] + 1;
        }
        return result;
    }

    private getRequiredTotal(): number {
        return this.required.host + this.required.srflx + this.required.relay;
    }
}
