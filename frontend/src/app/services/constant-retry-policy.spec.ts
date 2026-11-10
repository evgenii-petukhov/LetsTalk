import { ConstantRetryPolicy } from './constant-retry-policy';

describe('ConstantRetryPolicy', () => {
    let retryPolicy: ConstantRetryPolicy;
    const interval = 5000;

    beforeEach(() => {
        retryPolicy = new ConstantRetryPolicy(interval);
    });

    it('should be created', () => {
        expect(retryPolicy).toBeTruthy();
    });

    it('should initialize with the given interval', () => {
        expect(retryPolicy['interval']).toBe(interval);
    });

    it('should return the correct retry delay', () => {
        expect(retryPolicy.nextRetryDelayInMilliseconds()).toBe(interval);
    });

    it('should call the hook with the retry context before every retry', () => {
        const beforeRetry = vi.fn();
        const policy = new ConstantRetryPolicy(interval, beforeRetry);
        const first = { previousRetryCount: 0, elapsedMilliseconds: 0, retryReason: new Error() };
        const second = { previousRetryCount: 1, elapsedMilliseconds: interval, retryReason: new Error() };

        expect(policy.nextRetryDelayInMilliseconds(first)).toBe(interval);
        expect(policy.nextRetryDelayInMilliseconds(second)).toBe(interval);

        expect(beforeRetry).toHaveBeenNthCalledWith(1, first);
        expect(beforeRetry).toHaveBeenNthCalledWith(2, second);
    });
});
