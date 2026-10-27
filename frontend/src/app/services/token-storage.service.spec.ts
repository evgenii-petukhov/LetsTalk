import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
    let service: TokenStorageService;

    beforeEach(() => {
        vi.clearAllMocks();

        TestBed.configureTestingModule({
            providers: [TokenStorageService],
        });
        service = TestBed.inject(TokenStorageService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('clearLegacyToken', () => {
        it('should remove the legacy auth-token entry from localStorage', () => {
            service.clearLegacyToken();

            expect(window.localStorage.removeItem).toHaveBeenCalledWith(
                'auth-token',
            );
        });

        it('should not throw when localStorage is unavailable', () => {
            (
                window.localStorage.removeItem as ReturnType<typeof vi.fn>
            ).mockImplementationOnce(() => {
                throw new Error('blocked');
            });

            expect(() => service.clearLegacyToken()).not.toThrow();
        });
    });
});
