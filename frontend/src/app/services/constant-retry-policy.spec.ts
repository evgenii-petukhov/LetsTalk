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
});
