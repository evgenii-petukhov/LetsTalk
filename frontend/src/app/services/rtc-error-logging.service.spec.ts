import { TestBed } from '@angular/core/testing';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi, type MockedObject } from 'vitest';
import { RtcErrorLoggingService } from './rtc-error-logging.service';
import { RtcPeerConnectionManager } from './rtc-peer-connection-manager';
import { ApiService } from './api.service';
import { DebugService } from './debug.service';
import { RtcErrorType } from '../api-client/api-client';

describe('RtcErrorLoggingService', () => {
    let service: RtcErrorLoggingService;
    let connectionManager: Partial<MockedObject<RtcPeerConnectionManager>>;
    let mockStore: MockedObject<Store>;
    let apiService: MockedObject<ApiService>;
    let debugService: MockedObject<DebugService>;

    const mockVideoCallState = {
        callId: 'test-call-id',
        chatId: 'test-chat-id',
    };

    const mockDiagnostics = {
        connectionState: 'connected',
        localCandidateTypes: {},
        remoteCandidateTypes: {},
        browser: 'Chrome',
        platform: 'Win32',
    };

    beforeEach(() => {
        connectionManager = {
            getDiagnostics: vi
                .fn()
                .mockName('RtcPeerConnectionManager.getDiagnostics')
                .mockResolvedValue(mockDiagnostics),
        };

        mockStore = {
            dispatch: vi.fn().mockName('Store.dispatch'),
            select: vi
                .fn()
                .mockName('Store.select')
                .mockReturnValue(of(mockVideoCallState)),
        } as MockedObject<Store>;

        apiService = {
            logWebRtcError: vi
                .fn()
                .mockName('ApiService.logWebRtcError')
                .mockResolvedValue(undefined),
        } as MockedObject<ApiService>;

        debugService = {
            getStackTrace: vi
                .fn()
                .mockName('DebugService.getStackTrace')
                .mockReturnValue('mock-stack-trace'),
        } as MockedObject<DebugService>;

        TestBed.configureTestingModule({
            providers: [
                RtcErrorLoggingService,
                {
                    provide: RtcPeerConnectionManager,
                    useValue: connectionManager,
                },
                { provide: Store, useValue: mockStore },
                { provide: ApiService, useValue: apiService },
                { provide: DebugService, useValue: debugService },
            ],
        });

        service = TestBed.inject(RtcErrorLoggingService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('logConnectionError', () => {
        it('should log connection error with error object and message', async () => {
            // Arrange
            const errorMessage = 'Connection failed';
            const error = new Error(errorMessage);

            // Act
            await service.logConnectionError(error, errorMessage);

            // Assert
            expect(apiService.logWebRtcError).toHaveBeenCalledWith(
                mockVideoCallState.callId,
                mockVideoCallState.chatId,
                mockDiagnostics,
                RtcErrorType.Connection,
                error,
                errorMessage,
                'mock-stack-trace',
            );
        });

        it('should log connection error without error message', async () => {
            // Arrange
            const error = new Error('Connection timeout');

            // Act
            await service.logConnectionError(error);

            // Assert
            expect(apiService.logWebRtcError).toHaveBeenCalledWith(
                mockVideoCallState.callId,
                mockVideoCallState.chatId,
                mockDiagnostics,
                RtcErrorType.Connection,
                error,
                undefined,
                'mock-stack-trace',
            );
        });
    });

    describe('logIceServerError', () => {
        it('should log ICE server error', async () => {
            // Arrange
            const errorMessage = 'STUN server unreachable';
            const error = new Error(errorMessage);

            // Act
            await service.logIceServerError(error, errorMessage);

            // Assert
            expect(apiService.logWebRtcError).toHaveBeenCalledWith(
                mockVideoCallState.callId,
                mockVideoCallState.chatId,
                mockDiagnostics,
                RtcErrorType.IceServer,
                error,
                errorMessage,
                'mock-stack-trace',
            );
        });
    });

    describe('error handling', () => {
        it('should get diagnostics from connection manager', async () => {
            // Act
            await service.logConnectionError(new Error('Test error'));

            // Assert
            expect(connectionManager.getDiagnostics).toHaveBeenCalled();
        });

        it('should get call settings from store', async () => {
            // Act
            await service.logConnectionError(new Error('Test error'));

            // Assert
            expect(mockStore.select).toHaveBeenCalled();
        });

        it('should get stack trace from debug service', async () => {
            // Arrange
            const error = new Error('Test error');

            // Act
            await service.logConnectionError(error);

            // Assert
            expect(debugService.getStackTrace).toHaveBeenCalledWith(error);
        });

        it('should not call logWebRtcError when callId is absent', async () => {
            // Arrange
            mockStore.select.mockReturnValue(of({ callId: null, chatId: null }));

            // Act
            await service.logConnectionError(new Error('Test error'));

            // Assert
            expect(apiService.logWebRtcError).not.toHaveBeenCalled();
        });
    });
});
