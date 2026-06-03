import { TestBed } from '@angular/core/testing';
import { DebugService } from './debug.service';
import { beforeEach, describe, expect, it } from 'vitest';

describe('DebugService', () => {
    let service: DebugService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(DebugService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('getStackTrace', () => {
        it('should return the stack from a provided error', () => {
            const error = new Error('test error');
            error.stack =
                'Error: test error\n    at someFunction (file.ts:1:1)';

            const result = service.getStackTrace(error);

            expect(result).toBe(error.stack);
        });

        it('should fall back to a new Error stack when provided error has no stack', () => {
            const error = new Error('test error');
            error.stack = undefined;

            const result = service.getStackTrace(error);

            expect(result).toMatch(/Error/);
        });

        it('should generate a stack trace when no error is provided', () => {
            const result = service.getStackTrace();

            expect(result).toBeTruthy();
            expect(typeof result).toBe('string');
        });

        it('should return "No stack trace available" when fallback Error has no stack', () => {
            const originalError = globalThis.Error;
            // Must use a real constructor function (not arrow) so `new Error()` works
            function MockError(this: { stack: undefined }) {
                this.stack = undefined;
            }
            globalThis.Error = MockError as unknown as ErrorConstructor;

            const result = service.getStackTrace();

            expect(result).toBe('No stack trace available');

            globalThis.Error = originalError;
        });
    });
});
