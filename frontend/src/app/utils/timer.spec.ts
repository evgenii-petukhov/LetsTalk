import { vi, type Mock } from "vitest";
/* eslint-disable @typescript-eslint/no-explicit-any */
import { Timer } from './timer';

describe('Timer', () => {
    let callback: Mock;
    let timer: Timer;

    beforeEach(() => {
        callback = vi.fn();
        vi.useFakeTimers();
    });

    afterEach(() => {
        if (timer) {
            timer.clear();
        }
        vi.useRealTimers();
    });

    describe('constructor', () => {
        it('should create timer with valid callback and delay', () => {
            timer = new Timer(callback, 1000);

            expect(timer).toBeTruthy();
            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);
        });

        it('should throw error if callback is not a function', () => {
            expect(() => {
                new Timer(null as any, 1000);
            }).toThrowError('Callback must be a function');

            expect(() => {
                new Timer(undefined as any, 1000);
            }).toThrowError('Callback must be a function');

            expect(() => {
                new Timer('not a function' as any, 1000);
            }).toThrowError('Callback must be a function');

            expect(() => {
                new Timer(123 as any, 1000);
            }).toThrowError('Callback must be a function');
        });

        it('should throw error if delay is negative', () => {
            expect(() => {
                new Timer(callback, -1);
            }).toThrowError('Delay must be non-negative');

            expect(() => {
                new Timer(callback, -100);
            }).toThrowError('Delay must be non-negative');
        });

        it('should accept zero delay', () => {
            expect(() => {
                timer = new Timer(callback, 0);
            }).not.toThrow();

            expect(timer.isActive()).toBe(true);
        });

        it('should start timer automatically on construction', () => {
            timer = new Timer(callback, 1000);

            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);
        });
    });

    describe('timer execution', () => {
        it('should execute callback after specified delay', () => {
            timer = new Timer(callback, 1000);

            expect(callback).not.toHaveBeenCalled();
            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            vi.advanceTimersByTime(999);
            expect(callback).not.toHaveBeenCalled();
            expect(timer.isActive()).toBe(true);

            vi.advanceTimersByTime(1);
            expect(callback).toHaveBeenCalledTimes(1);
            expect(timer.isActive()).toBe(false);
            expect(timer.isExpired()).toBe(true);
        });

        it('should execute callback immediately with zero delay', () => {
            timer = new Timer(callback, 0);

            expect(callback).not.toHaveBeenCalled();
            expect(timer.isActive()).toBe(true);

            vi.advanceTimersByTime(0);
            expect(callback).toHaveBeenCalledTimes(1);
            expect(timer.isActive()).toBe(false);
            expect(timer.isExpired()).toBe(true);
        });

        it('should execute callback only once', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(callback).toHaveBeenCalledTimes(1);

            vi.advanceTimersByTime(1000);
            expect(callback).toHaveBeenCalledTimes(1);
        });

        it('should handle callback errors gracefully', () => {
            const errorCallback = vi.fn().mockImplementation(() => {
                throw new Error('Test error');
            });
            vi.spyOn(console, 'error');

            timer = new Timer(errorCallback, 1000);

            vi.advanceTimersByTime(1000);

            expect(errorCallback).toHaveBeenCalledTimes(1);
            expect(console.error).toHaveBeenCalledWith(expect.any(Error));
            expect(timer.isExpired()).toBe(true);
            expect(timer.isActive()).toBe(false);
        });

        it('should clear timer after callback execution', () => {
            timer = new Timer(callback, 1000);
            const initialTimerId = (timer as any).timerId;

            expect(initialTimerId).not.toBeNull();

            vi.advanceTimersByTime(1000);

            expect((timer as any).timerId).toBeNull();
            expect(timer.isActive()).toBe(false);
        });
    });

    describe('clear()', () => {
        it('should clear active timer', () => {
            timer = new Timer(callback, 1000);

            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            timer.clear();

            expect(timer.isActive()).toBe(false);
            expect(timer.isExpired()).toBe(false);
            expect((timer as any).timerId).toBeNull();

            vi.advanceTimersByTime(1000);
            expect(callback).not.toHaveBeenCalled();
        });

        it('should be safe to call clear() multiple times', () => {
            timer = new Timer(callback, 1000);

            timer.clear();
            timer.clear();
            timer.clear();

            expect(timer.isActive()).toBe(false);
            expect((timer as any).timerId).toBeNull();
        });

        it('should be safe to call clear() on already expired timer', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(timer.isExpired()).toBe(true);

            expect(() => timer.clear()).not.toThrow();
            expect(timer.isActive()).toBe(false);
        });

        it('should prevent callback execution when cleared before expiration', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(500);
            timer.clear();
            vi.advanceTimersByTime(500);

            expect(callback).not.toHaveBeenCalled();
            expect(timer.isActive()).toBe(false);
            expect(timer.isExpired()).toBe(false);
        });
    });

    describe('isExpired()', () => {
        it('should return false for new timer', () => {
            timer = new Timer(callback, 1000);

            expect(timer.isExpired()).toBe(false);
        });

        it('should return false for active timer', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(500);
            expect(timer.isExpired()).toBe(false);
        });

        it('should return true after timer expires', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(timer.isExpired()).toBe(true);
        });

        it('should return false for cleared timer', () => {
            timer = new Timer(callback, 1000);

            timer.clear();
            expect(timer.isExpired()).toBe(false);
        });

        it('should remain true after timer has expired', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(timer.isExpired()).toBe(true);

            vi.advanceTimersByTime(1000);
            expect(timer.isExpired()).toBe(true);
        });
    });

    describe('isActive()', () => {
        it('should return true for new timer', () => {
            timer = new Timer(callback, 1000);

            expect(timer.isActive()).toBe(true);
        });

        it('should return true while timer is running', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(500);
            expect(timer.isActive()).toBe(true);

            vi.advanceTimersByTime(499);
            expect(timer.isActive()).toBe(true);
        });

        it('should return false after timer expires', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(timer.isActive()).toBe(false);
        });

        it('should return false after timer is cleared', () => {
            timer = new Timer(callback, 1000);

            timer.clear();
            expect(timer.isActive()).toBe(false);
        });

        it('should return false for expired timer even after clearing', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);
            expect(timer.isActive()).toBe(false);

            timer.clear();
            expect(timer.isActive()).toBe(false);
        });
    });

    describe('start() method behavior', () => {
        it('should not restart timer if already active', () => {
            timer = new Timer(callback, 1000);
            const initialTimerId = (timer as any).timerId;

            // Try to call start again (accessing private method for testing)
            (timer as any).start();

            expect((timer as any).timerId).toBe(initialTimerId);
            expect(timer.isActive()).toBe(true);
        });

        it('should restart timer after clearing when start() is called', () => {
            timer = new Timer(callback, 1000);

            timer.clear();
            expect(timer.isActive()).toBe(false);

            // Try to call start again (accessing private method for testing)
            (timer as any).start();

            expect(timer.isActive()).toBe(true);
            expect((timer as any).timerId).not.toBeNull();
        });
    });

    describe('edge cases and integration', () => {
        it('should handle very short delays correctly', () => {
            timer = new Timer(callback, 1);

            expect(timer.isActive()).toBe(true);

            vi.advanceTimersByTime(1);

            expect(callback).toHaveBeenCalledTimes(1);
            expect(timer.isExpired()).toBe(true);
            expect(timer.isActive()).toBe(false);
        });

        it('should handle very long delays correctly', () => {
            const longDelay = 2147483647; // Max safe timeout value
            timer = new Timer(callback, longDelay);

            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            vi.advanceTimersByTime(longDelay - 1);
            expect(callback).not.toHaveBeenCalled();
            expect(timer.isActive()).toBe(true);

            vi.advanceTimersByTime(1);
            expect(callback).toHaveBeenCalledTimes(1);
            expect(timer.isExpired()).toBe(true);
        });

        it('should maintain correct state throughout lifecycle', () => {
            timer = new Timer(callback, 1000);

            // Initial state
            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            // Mid-execution
            vi.advanceTimersByTime(500);
            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            // Just before expiration
            vi.advanceTimersByTime(499);
            expect(timer.isActive()).toBe(true);
            expect(timer.isExpired()).toBe(false);

            // After expiration
            vi.advanceTimersByTime(1);
            expect(timer.isActive()).toBe(false);
            expect(timer.isExpired()).toBe(true);
        });

        it('should handle callback that throws different types of errors', () => {
            vi.spyOn(console, 'error');

            // String error
            const stringErrorCallback = vi.fn().mockImplementation(() => {
                throw new Error('String error');
            });
            timer = new Timer(stringErrorCallback, 100);
            vi.advanceTimersByTime(100);

            expect(console.error).toHaveBeenCalledWith(expect.any(Error));
            expect(timer.isExpired()).toBe(true);

            timer.clear();

            // Object error
            const objectErrorCallback = vi.fn().mockImplementation(() => {
                throw { message: 'Object error' };
            });
            timer = new Timer(objectErrorCallback, 100);
            vi.advanceTimersByTime(100);

            expect(console.error).toHaveBeenCalledWith({ message: 'Object error' });
            expect(timer.isExpired()).toBe(true);
        });

        it('should work correctly with callback that modifies timer state', () => {
            let callbackExecuted = false;
            const selfModifyingCallback = vi.fn().mockImplementation(() => {
                callbackExecuted = true;
                // Callback tries to clear the timer (should be safe)
                timer.clear();
            });

            timer = new Timer(selfModifyingCallback, 1000);

            vi.advanceTimersByTime(1000);

            expect(selfModifyingCallback).toHaveBeenCalledTimes(1);
            expect(callbackExecuted).toBe(true);
            expect(timer.isExpired()).toBe(true);
            expect(timer.isActive()).toBe(false);
        });
    });

    describe('memory management', () => {
        it('should properly clean up timer references', () => {
            timer = new Timer(callback, 1000);
            const timerId = (timer as any).timerId;

            expect(timerId).not.toBeNull();
            expect(typeof timerId).toBe('number');

            timer.clear();

            expect((timer as any).timerId).toBeNull();
        });

        it('should clean up after natural expiration', () => {
            timer = new Timer(callback, 1000);

            vi.advanceTimersByTime(1000);

            expect((timer as any).timerId).toBeNull();
            expect(callback).toHaveBeenCalledTimes(1);
        });
    });
});
