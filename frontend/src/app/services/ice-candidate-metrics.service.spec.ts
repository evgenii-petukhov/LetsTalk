import { TestBed } from '@angular/core/testing';
import { IceCandidateMetricsService } from './ice-candidate-metrics.service';
import { IceCandidateMetricsTestHelper } from './ice-candidate-metrics.service.test-helper';

describe('IceCandidateMetricsService', () => {
    let service: IceCandidateMetricsService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(IceCandidateMetricsService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('hasSufficientServers', () => {
        it('should return true when all requirements are met exactly', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidates)).toBe(true);
        });

        it('should return true when all requirements are exceeded', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 5,
                srflx: 3,
                relay: 4,
            });
            expect(service.hasSufficientServers(candidates)).toBe(true);
        });

        it('should return false when host requirement is not met', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidates)).toBe(false);
        });

        it('should return false when srflx requirement is not met', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidates)).toBe(false);
        });

        it('should return false when relay requirement is not met', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
            });
            expect(service.hasSufficientServers(candidates)).toBe(false);
        });

        it('should return false when multiple requirements are not met', () => {
            const candidates: RTCIceCandidate[] = [];
            expect(service.hasSufficientServers(candidates)).toBe(false);
        });

        it('should return true when prflx exceeds requirement (prflx requirement is 0)', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
                prflx: 5,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidates)).toBe(true);
        });

        it('should handle boundary values correctly', () => {
            // Test exactly at the boundary
            const candidatesAtBoundary = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidatesAtBoundary)).toBe(true);

            // Test just below the boundary
            const candidatesBelowBoundary = IceCandidateMetricsTestHelper.createMockCandidateArray({
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(candidatesBelowBoundary)).toBe(false);
        });
    });

    describe('hasMinimumCandidateCount', () => {
        it('should return true when candidate count meets minimum requirement', () => {
            // Required total is 3 (host: 1 + srflx: 1 + relay: 1)
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 1,
                    srflx: 1,
                    relay: 1,
                },
            );

            expect(service.hasMinimumCandidateCount(candidates)).toBe(true);
        });

        it('should return true when candidate count exceeds minimum requirement', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 2,
                    srflx: 2,
                    prflx: 1,
                    relay: 2,
                },
            );

            expect(service.hasMinimumCandidateCount(candidates)).toBe(true);
        });

        it('should return false when candidate count is below minimum requirement', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 1,
                    srflx: 1,
                },
            ); // Only 2 candidates, need 3

            expect(service.hasMinimumCandidateCount(candidates)).toBe(false);
        });

        it('should return false for empty candidate array', () => {
            expect(service.hasMinimumCandidateCount([])).toBe(false);
        });

        it('should return false for single candidate', () => {
            const candidates = [
                IceCandidateMetricsTestHelper.createMockCandidate('host'),
            ];

            expect(service.hasMinimumCandidateCount(candidates)).toBe(false);
        });

        it('should handle exactly minimum count edge case', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 1,
                    srflx: 1,
                    relay: 1,
                },
            ); // Exactly 3 candidates

            expect(service.hasMinimumCandidateCount(candidates)).toBe(true);
        });

        it('should work regardless of candidate types (only checks count)', () => {
            // 3 candidates of same type should pass preCheck
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    prflx: 3,
                },
            );

            expect(service.hasMinimumCandidateCount(candidates)).toBe(true);
        });
    });

    describe('Integration tests with test scenarios', () => {
        const scenarios = IceCandidateMetricsTestHelper.getTestScenarios();

        it('should handle empty scenario correctly', () => {
            const scenario = scenarios.empty;

            expect(service.hasMinimumCandidateCount(scenario.candidates)).toBe(
                scenario.shouldPassPreCheck,
            );

            expect(service.hasSufficientServers(scenario.candidates)).toBe(
                scenario.shouldBeSufficient,
            );
        });

        it('should handle minimal scenario correctly', () => {
            const scenario = scenarios.minimal;

            expect(service.hasMinimumCandidateCount(scenario.candidates)).toBe(
                scenario.shouldPassPreCheck,
            );

            expect(service.hasSufficientServers(scenario.candidates)).toBe(
                scenario.shouldBeSufficient,
            );
        });

        it('should handle optimal scenario correctly', () => {
            const scenario = scenarios.optimal;

            expect(service.hasMinimumCandidateCount(scenario.candidates)).toBe(
                scenario.shouldPassPreCheck,
            );

            expect(service.hasSufficientServers(scenario.candidates)).toBe(
                scenario.shouldBeSufficient,
            );
        });

        it('should handle insufficient no relay scenario correctly', () => {
            const scenario = scenarios.insufficientNoRelay;

            expect(service.hasMinimumCandidateCount(scenario.candidates)).toBe(
                scenario.shouldPassPreCheck,
            );

            expect(service.hasSufficientServers(scenario.candidates)).toBe(
                scenario.shouldBeSufficient,
            );
        });

        it('should handle insufficient count scenario correctly', () => {
            const scenario = scenarios.insufficientCount;

            expect(service.hasMinimumCandidateCount(scenario.candidates)).toBe(
                scenario.shouldPassPreCheck,
            );

            expect(service.hasSufficientServers(scenario.candidates)).toBe(
                scenario.shouldBeSufficient,
            );
        });
    });

    describe('Edge cases and error conditions', () => {
        it('should handle null/undefined candidate properties gracefully', () => {
            // This tests the robustness of the service
            const candidates = [
                IceCandidateMetricsTestHelper.createMockCandidate('host'),
                IceCandidateMetricsTestHelper.createMockCandidate('srflx'),
                IceCandidateMetricsTestHelper.createMockCandidate('relay'),
            ];

            // Should not throw errors
            expect(() => {
                service.hasSufficientServers(candidates);
                service.hasMinimumCandidateCount(candidates);
            }).not.toThrow();
        });

        it('should handle very large candidate arrays efficiently', () => {
            const startTime = performance.now();

            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 1000,
                    srflx: 1000,
                    prflx: 1000,
                    relay: 1000,
                },
            );

            service.hasSufficientServers(candidates);
            const endTime = performance.now();

            // Should complete within reasonable time (less than 100ms)
            expect(endTime - startTime).toBeLessThan(100);
        });

        it('should maintain consistency across multiple calls', () => {
            const candidates = IceCandidateMetricsTestHelper.createMockCandidateArray(
                {
                    host: 2,
                    srflx: 3,
                    relay: 1,
                },
            );

            // Multiple calls should return identical results
            const result1 = service.hasSufficientServers(candidates);
            const result2 = service.hasSufficientServers(candidates);
            const result3 = service.hasSufficientServers(candidates);

            expect(result1).toBe(result2);
            expect(result2).toBe(result3);

            expect(service.hasMinimumCandidateCount(candidates)).toBe(
                service.hasMinimumCandidateCount(candidates),
            );
        });
    });

    describe('Service requirements configuration', () => {
        it('should have correct default requirements', () => {
            // Test the requirements indirectly through hasSufficientServers
            const minimalCandidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
                relay: 1,
            });

            expect(service.hasSufficientServers(minimalCandidates)).toBe(true);

            // Test that reducing any required field fails
            const noHostCandidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(noHostCandidates)).toBe(false);

            const noSrflxCandidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(noSrflxCandidates)).toBe(false);

            const noRelayCandidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
            });
            expect(service.hasSufficientServers(noRelayCandidates)).toBe(false);

            // Test that prflx is not required (can be 0)
            const noPrflxCandidates = IceCandidateMetricsTestHelper.createMockCandidateArray({
                host: 1,
                srflx: 1,
                relay: 1,
            });
            expect(service.hasSufficientServers(noPrflxCandidates)).toBe(true);
        });
    });
});
