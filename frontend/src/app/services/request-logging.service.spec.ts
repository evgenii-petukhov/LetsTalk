import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { RequestLoggingService } from './request-logging.service';

describe('RequestLoggingService', () => {
    let service: RequestLoggingService;
    let consoleSpy: Mock;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [RequestLoggingService],
        });
        service = TestBed.inject(RequestLoggingService);

        consoleSpy = vi.spyOn(console, 'log');
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('log', () => {
        it('should log request details with download and upload sizes', () => {
            // Arrange
            const protocol = 'HTTP';
            const url = 'https://example.com';
            const time = 123.456;
            const downloaded = 2048; // 2MB
            const uploaded = 1024; // 1MB

            // Act
            service.log(protocol, url, time, downloaded, uploaded);

            // Assert
            expect(consoleSpy).toHaveBeenCalledWith(
                `[${protocol}] ${url} ${time.toFixed()}ms ▼${Math.ceil(downloaded / 1024)}kB ▲${Math.ceil(uploaded / 1024)}kB`,
            );
        });

        it('should log request details with no download or upload sizes if values are zero', () => {
            // Arrange
            const protocol = 'HTTP';
            const url = 'https://example.com';
            const time = 123.456;
            const downloaded = 0;
            const uploaded = 0;

            // Act
            service.log(protocol, url, time, downloaded, uploaded);

            // Assert
            expect(consoleSpy).toHaveBeenCalledWith(
                `[${protocol}] ${url} ${time.toFixed()}ms`,
            );
        });
    });
});
