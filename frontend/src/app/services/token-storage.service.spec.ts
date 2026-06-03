/* eslint-disable @typescript-eslint/no-explicit-any */
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
    let service: TokenStorageService;

    beforeEach(() => {
        // Clear all mocks before each test
        vi.clearAllMocks();

        TestBed.configureTestingModule({
            providers: [TokenStorageService],
        });
        service = TestBed.inject(TokenStorageService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('saveToken', () => {
        it('should save the token to localStorage', () => {
            // Arrange
            const token = 'sample-token';

            // Act
            service.saveToken(token);

            // Assert
            expect(window.localStorage.setItem).toHaveBeenCalledWith(
                'auth-token',
                token,
            );
        });
    });

    describe('getToken', () => {
        it('should return the token from localStorage', () => {
            // Arrange
            const token = 'sample-token';
            (window.localStorage.getItem as any).mockReturnValue(token);

            // Act
            const result = service.getToken();

            // Assert
            expect(window.localStorage.getItem).toHaveBeenCalledWith(
                'auth-token',
            );
            expect(result).toBe(token);
        });
    });

    describe('isLoggedIn', () => {
        it('should return true if a token exists', () => {
            // Arrange
            (window.localStorage.getItem as any).mockReturnValue(
                'sample-token',
            );

            // Act
            const result = service.isLoggedIn();

            // Assert
            expect(result).toBe(true);
        });

        it('should return false if no token exists', () => {
            // Arrange
            (window.localStorage.getItem as any).mockReturnValue(null);

            // Act
            const result = service.isLoggedIn();

            // Assert
            expect(result).toBe(false);
        });
    });
});
