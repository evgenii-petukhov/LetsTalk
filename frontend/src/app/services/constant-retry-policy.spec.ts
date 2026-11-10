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

    it('should call the hook before every retry', () => {
        const beforeRetry = vi.fn();
        const policy = new ConstantRetryPolicy(interval, beforeRetry);

        expect(policy.nextRetryDelayInMilliseconds()).toBe(interval);
        expect(policy.nextRetryDelayInMilliseconds()).toBe(interval);

        expect(beforeRetry).toHaveBeenCalledTimes(2);
    });
});
