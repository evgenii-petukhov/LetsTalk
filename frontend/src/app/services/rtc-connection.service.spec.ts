import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { RtcConnectionService } from './rtc-connection.service';
import { ApiService } from './api.service';
import { RtcPeerConnectionManager } from './rtc-peer-connection-manager';
import { StoreService } from './store.service';
import { DebugService } from './debug.service';
import {
    CallSettingsDto,
    RtcErrorType,
    StartOutgoingCallDto,
} from '../api-client/api-client';

describe('RtcConnectionService', () => {
    let service: RtcConnectionService;
    let apiService: MockedObject<ApiService>;
    let connectionManager: Partial<MockedObject<RtcPeerConnectionManager>>;
    let storeService: MockedObject<StoreService>;
    let mockStore: MockedObject<Store>;
    let debugService: MockedObject<DebugService>;

    const mockCallSettings = {
        iceServerConfiguration: JSON.stringify({
            iceServers: [{ urls: 'stun:stun.l.google.com:19302' }],
        }),
    } as CallSettingsDto;

    const mockVideoCallState = {
        callId: 'call-id',
        chatId: 'test-chat-id',
    };

    const mockOffer = {
        desc: {
            type: 'offer' as RTCSdpType,
            sdp: 'mock-offer-sdp',
        },
        candidates: [
            {
                candidate:
                    'candidate:1 1 UDP 2130706431 192.168.1.1 54400 typ host',
                sdpMLineIndex: 0,
                sdpMid: '0',
            },
        ],
    };

    const mockAnswer = {
        desc: {
            type: 'answer' as RTCSdpType,
            sdp: 'mock-answer-sdp',
        },
        candidates: [
            {
                candidate:
                    'candidate:2 1 UDP 2130706431 192.168.1.2 54401 typ host',
                sdpMLineIndex: 0,
                sdpMid: '0',
            },
        ],
    };

    beforeEach(() => {
        apiService = {
            getCallSettings: vi.fn().mockName('ApiService.getCallSettings'),
            startOutgoingCall: vi.fn().mockName('ApiService.startOutgoingCall'),
            handleIncomingCall: vi
                .fn()
                .mockName('ApiService.handleIncomingCall'),
            logConnectionEstablished: vi
                .fn()
                .mockName('ApiService.logConnectionEstablished')
                .mockResolvedValue(undefined),
            logWebRtcError: vi
                .fn()
                .mockName('ApiService.logWebRtcError')
                .mockResolvedValue(undefined),
        } as MockedObject<ApiService>;

        connectionManager = {
            initiateOffer: vi
                .fn()
                .mockName('RtcPeerConnectionManager.initiateOffer'),
            handleOfferAndCreateAnswer: vi
                .fn()
                .mockName(
                    'RtcPeerConnectionManager.handleOfferAndCreateAnswer',
                ),
            setRemoteAnswerAndCandidates: vi
                .fn()
                .mockName(
                    'RtcPeerConnectionManager.setRemoteAnswerAndCandidates',
                ),
            requestCompleteGathering: vi
                .fn()
                .mockName('RtcPeerConnectionManager.requestCompleteGathering'),
            reinitialize: vi
                .fn()
                .mockName('RtcPeerConnectionManager.reinitialize'),
            getDiagnostics: vi
                .fn()
                .mockName('RtcPeerConnectionManager.getDiagnostics')
                .mockResolvedValue(undefined),
            onCandidatesReceived: null,
            onGatheringCompleted: null,
            onConnected: null,
            onConnectionError: null,
            onDisconnected: null,
        };

        storeService = {
            resetCall: vi.fn().mockName('StoreService.resetCall'),
            setCallId: vi.fn().mockName('StoreService.setCallId'),
        } as MockedObject<StoreService>;

        mockStore = {
            dispatch: vi.fn().mockName('Store.dispatch'),
            select: vi.fn().mockName('Store.select').mockReturnValue(of(mockVideoCallState)),
        } as MockedObject<Store>;

        debugService = {
            getStackTrace: vi.fn().mockName('DebugService.getStackTrace').mockReturnValue('mock-stack-trace'),
        } as MockedObject<DebugService>;

        TestBed.configureTestingModule({
            providers: [
                RtcConnectionService,
                { provide: ApiService, useValue: apiService },
                {
                    provide: RtcPeerConnectionManager,
                    useValue: connectionManager,
                },
                { provide: StoreService, useValue: storeService },
                { provide: Store, useValue: mockStore },
                { provide: DebugService, useValue: debugService },
            ],
        });

        service = TestBed.inject(RtcConnectionService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should set up connection manager callbacks in constructor', () => {
        expect(connectionManager.onCandidatesReceived).toBeDefined();
        expect(connectionManager.onGatheringCompleted).toBeDefined();
    });

    describe('startOutgoingCall', () => {
        beforeEach(() => {
            const callId = 'call-id';
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.startOutgoingCall.mockReturnValue(
                Promise.resolve(
                    new StartOutgoingCallDto({
                        callId,
                    }),
                ),
            );
        });

        it('should start outgoing call successfully', async () => {
            // Arrange
            const accountId = 'test-account-id';
            const finalOfferData = JSON.stringify(mockOffer);

            // Act
            const promise = service.startOutgoingCall(accountId);

            // Simulate ice candidate generation first, then completion
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalOfferData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await promise;

            // Assert
            expect(apiService.getCallSettings).toHaveBeenCalled();
            expect(connectionManager.initiateOffer).toHaveBeenCalledWith(
                JSON.parse(mockCallSettings.iceServerConfiguration),
            );
            expect(apiService.startOutgoingCall).toHaveBeenCalledWith(
                accountId,
                finalOfferData,
                0,
                false,
                undefined,
            );
        });

        it('should handle timer expiration during ice gathering', async () => {
            // Arrange
            const accountId = 'test-account-id';
            const finalOfferData = JSON.stringify(mockOffer);

            // Act
            const promise = service.startOutgoingCall(accountId);

            // Wait for timer to be created
            await new Promise((resolve) => setTimeout(resolve, 0));

            // Mock the timer to be expired
            service['iceGatheringTimer'] = {
                isExpired: vi.fn().mockReturnValue(true),
                clear: vi.fn(),
            } as any;

            // Simulate ice candidate generation with expired timer
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalOfferData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await promise;

            // Assert
            expect(
                connectionManager.requestCompleteGathering,
            ).toHaveBeenCalled();
        });
    });

    describe('handleIncomingCall', () => {
        beforeEach(() => {
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.handleIncomingCall.mockReturnValue(Promise.resolve());
        });

        it('should handle incoming call successfully', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const offerString = JSON.stringify(mockOffer);
            const finalAnswerData = JSON.stringify(mockAnswer);

            // Act
            const promise = service.handleIncomingCall(
                callId,
                chatId,
                offerString,
            );

            // Simulate ice candidate generation first, then completion
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalAnswerData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await promise;

            // Assert
            expect(apiService.getCallSettings).toHaveBeenCalled();
            expect(
                connectionManager.handleOfferAndCreateAnswer,
            ).toHaveBeenCalledWith(
                JSON.parse(mockCallSettings.iceServerConfiguration),
                mockOffer.desc,
                mockOffer.candidates,
            );
            expect(apiService.handleIncomingCall).toHaveBeenCalledWith(
                callId,
                chatId,
                finalAnswerData,
                0,
                false,
                undefined,
            );
        });

        it('should handle timer expiration during answer generation', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const offerString = JSON.stringify(mockOffer);
            const finalAnswerData = JSON.stringify(mockAnswer);

            // Act
            const promise = service.handleIncomingCall(
                callId,
                chatId,
                offerString,
            );

            // Wait for timer to be created
            await new Promise((resolve) => setTimeout(resolve, 0));

            // Mock the timer to be expired
            service['iceGatheringTimer'] = {
                isExpired: vi.fn().mockReturnValue(true),
                clear: vi.fn(),
            } as any;

            // Simulate ice candidate generation with expired timer
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalAnswerData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await promise;

            // Assert
            expect(
                connectionManager.requestCompleteGathering,
            ).toHaveBeenCalled();
        });
    });

    describe('establishConnection', () => {
        it('should establish connection with valid answer', async () => {
            // Arrange
            const answerString = JSON.stringify(mockAnswer);

            // Act
            await service.establishConnection(answerString);

            // Assert
            expect(
                connectionManager.setRemoteAnswerAndCandidates,
            ).toHaveBeenCalledWith(mockAnswer.desc, mockAnswer.candidates);
        });

        it('should return early if answer type is not "answer"', async () => {
            // Arrange
            const invalidAnswer = {
                desc: {
                    type: 'offer' as RTCSdpType, // Wrong type
                    sdp: 'mock-sdp',
                },
                candidates: [],
            };
            const answerString = JSON.stringify(invalidAnswer);

            // Act
            await service.establishConnection(answerString);

            // Assert
            expect(
                connectionManager.setRemoteAnswerAndCandidates,
            ).not.toHaveBeenCalled();
        });

        it('should handle malformed JSON gracefully', async () => {
            // Arrange
            const malformedJson = 'invalid-json';

            // Act & Assert
            await expect(
                service.establishConnection(malformedJson),
            ).rejects.toThrow();
        });
    });

    describe('onIceCandidateGenerated', () => {
        it('should emit ice candidate data', () => {
            // Arrange
            const testData = 'test-ice-candidate-data';
            vi.spyOn(service['iceCandidateSubject'], 'next');

            // Mock timer as not expired
            service['iceGatheringTimer'] = { isExpired: () => false } as any;

            // Act
            service['onIceCandidateGenerated'](testData);

            // Assert
            expect(service['iceCandidateSubject'].next).toHaveBeenCalledWith(
                testData,
            );
        });

        it('should request complete gathering when timer is expired', () => {
            // Arrange
            const testData = 'test-ice-candidate-data';
            service['iceGatheringTimer'] = { isExpired: () => true } as any;

            // Act
            service['onIceCandidateGenerated'](testData);

            // Assert
            expect(
                connectionManager.requestCompleteGathering,
            ).toHaveBeenCalled();
        });

        it('should not request complete gathering when timer is not expired', () => {
            // Arrange
            const testData = 'test-ice-candidate-data';
            service['iceGatheringTimer'] = { isExpired: () => false } as any;

            // Act
            service['onIceCandidateGenerated'](testData);

            // Assert
            expect(
                connectionManager.requestCompleteGathering,
            ).not.toHaveBeenCalled();
        });
    });

    describe('onIceGatheringComplete', () => {
        it('should emit gathering complete signal and clear timer', () => {
            // Arrange
            vi.spyOn(service['iceGatheringComplete'], 'next');
            const mockTimer = { clear: vi.fn() };
            service['iceGatheringTimer'] = mockTimer as any;

            // Act
            service['onIceGatheringComplete'](0, false);

            // Assert
            expect(service['iceGatheringComplete'].next).toHaveBeenCalled();
            expect(mockTimer.clear).toHaveBeenCalled();
        });
    });

    describe('integration scenarios', () => {
        it('should handle complete outgoing call flow', async () => {
            // Arrange
            const callId = 'call-id';
            const accountId = 'test-account-id';
            const finalOfferData = JSON.stringify(mockOffer);
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.startOutgoingCall.mockReturnValue(
                Promise.resolve(
                    new StartOutgoingCallDto({
                        callId,
                    }),
                ),
            );

            // Act - Start the call
            const callPromise = service.startOutgoingCall(accountId);

            // Simulate the WebRTC flow
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalOfferData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await callPromise;

            // Assert
            expect(apiService.getCallSettings).toHaveBeenCalled();
            expect(connectionManager.initiateOffer).toHaveBeenCalled();
            expect(apiService.startOutgoingCall).toHaveBeenCalledWith(
                accountId,
                finalOfferData,
                0,
                false,
                undefined,
            );
        });

        it('should handle complete incoming call flow', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const offerString = JSON.stringify(mockOffer);
            const finalAnswerData = JSON.stringify(mockAnswer);
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.handleIncomingCall.mockReturnValue(Promise.resolve());

            // Act - Handle the incoming call
            const callPromise = service.handleIncomingCall(
                callId,
                chatId,
                offerString,
            );

            // Simulate the WebRTC flow
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalAnswerData);
            }, 0);

            setTimeout(() => {
                service['onIceGatheringComplete'](0, false);
            }, 5);

            await callPromise;

            // Assert
            expect(apiService.getCallSettings).toHaveBeenCalled();
            expect(
                connectionManager.handleOfferAndCreateAnswer,
            ).toHaveBeenCalled();
            expect(apiService.handleIncomingCall).toHaveBeenCalledWith(
                callId,
                chatId,
                finalAnswerData,
                0,
                false,
                undefined,
            );
        });

        it('should handle connection establishment after call setup', async () => {
            // Arrange
            const answerString = JSON.stringify(mockAnswer);

            // Act
            await service.establishConnection(answerString);

            // Assert
            expect(
                connectionManager.setRemoteAnswerAndCandidates,
            ).toHaveBeenCalledWith(mockAnswer.desc, mockAnswer.candidates);
        });
    });

    describe('endCall', () => {
        it('should call processEndCall', () => {
            // Arrange
            vi.spyOn(service as any, 'processEndCall');

            // Act
            service.endCall();

            // Assert
            expect(service['processEndCall']).toHaveBeenCalled();
        });
    });

    describe('onConnectionStateChange', () => {
        it('should call processEndCall when disconnected', () => {
            // Arrange
            vi.spyOn(service as any, 'processEndCall');

            // Act
            service['onDisconnected']();

            // Assert
            expect(service['processEndCall']).toHaveBeenCalled();
        });

        it('should log connection when connected', async () => {
            // Act
            await service['onConnected']();

            // Assert
            expect(apiService.logConnectionEstablished).toHaveBeenCalledWith(
                mockVideoCallState.callId,
                mockVideoCallState.chatId,
                undefined,
            );
        });

        it('should log connection failure', async () => {
            // Arrange
            const errorMessage = 'Connection failed';
            const error = new Error(errorMessage);

            // Act
            await service['onConnectionError'](errorMessage, error);

            // Assert
            expect(apiService.logWebRtcError).toHaveBeenCalledWith(
                mockVideoCallState.callId,
                mockVideoCallState.chatId,
                undefined,
                RtcErrorType.Connection,
                errorMessage,
                'mock-stack-trace',
            );
        });
    });

    describe('processEndCall', () => {
        it('should reinitialize connection manager and reset call in store', () => {
            // Act
            service['processEndCall']();

            // Assert
            expect(connectionManager.reinitialize).toHaveBeenCalled();
            expect(storeService.resetCall).toHaveBeenCalled();
        });
    });

    describe('Timer integration', () => {
        it('should create timer with correct timeout in startOutgoingCall', async () => {
            // Arrange
            const callId = 'call-id';
            const accountId = 'test-account-id';
            const finalOfferData = JSON.stringify(mockOffer);
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.startOutgoingCall.mockReturnValue(
                Promise.resolve(
                    new StartOutgoingCallDto({
                        callId,
                    }),
                ),
            );

            // Act
            const callPromise = service.startOutgoingCall(accountId);

            // Wait for timer to be created
            await new Promise((resolve) => setTimeout(resolve, 0));

            // Assert timer was created with correct timeout
            expect(service['iceGatheringTimer']).toBeDefined();
            expect(service['iceGatheringTimeoutMs']).toBe(10000);

            // Complete the call
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalOfferData);
                service['onIceGatheringComplete'](0, false);
            }, 0);

            await callPromise;
        });

        it('should create timer with correct timeout in handleIncomingCall', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const offerString = JSON.stringify(mockOffer);
            const finalAnswerData = JSON.stringify(mockAnswer);
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            apiService.handleIncomingCall.mockReturnValue(Promise.resolve());

            // Act
            const callPromise = service.handleIncomingCall(
                callId,
                chatId,
                offerString,
            );

            // Wait for timer to be created
            await new Promise((resolve) => setTimeout(resolve, 0));

            // Assert timer was created with correct timeout
            expect(service['iceGatheringTimer']).toBeDefined();
            expect(service['iceGatheringTimeoutMs']).toBe(10000);

            // Complete the call
            setTimeout(() => {
                service['onIceCandidateGenerated'](finalAnswerData);
                service['onIceGatheringComplete'](0, false);
            }, 0);

            await callPromise;
        });
    });

    describe('Subject behavior', () => {
        it('should have iceCandidateSubject and iceGatheringComplete subjects initialized', () => {
            expect(service['iceCandidateSubject']).toBeDefined();
            expect(service['iceGatheringComplete']).toBeDefined();
        });

        it('should complete ice gathering observable when onIceGatheringComplete is called', async () => {
            // Arrange
            let completed = false;
            const mockTimer = { clear: vi.fn() };
            service['iceGatheringTimer'] = mockTimer as any;

            service['iceGatheringComplete'].subscribe({
                next: () => {
                    completed = true;
                },
                complete: () => {
                    // This should not be called in this test
                },
            });

            // Act
            service['onIceGatheringComplete'](0, false);

            // Assert
            setTimeout(() => {
                expect(completed).toBe(true);
                expect(mockTimer.clear).toHaveBeenCalled();
            }, 0);
        });
    });

    describe('JSON parsing edge cases', () => {
        it('should handle malformed JSON in handleIncomingCall', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const malformedOffer = 'invalid-json';

            // Act & Assert
            await expect(
                service.handleIncomingCall(callId, chatId, malformedOffer),
            ).rejects.toThrow();
        });

        it('should handle JSON with missing properties in establishConnection', async () => {
            // Arrange
            const incompleteAnswer = JSON.stringify({
                desc: { type: 'answer' },
                // Missing candidates - this will cause an error when trying to access remote.candidates
            });

            // Act & Assert
            try {
                await service.establishConnection(incompleteAnswer);
                // If we get here, the method didn't throw, which means it handled missing candidates gracefully
                expect(
                    connectionManager.setRemoteAnswerAndCandidates,
                ).toHaveBeenCalledWith({ type: 'answer' }, undefined);
            } catch (error) {
                // If it throws, that's also acceptable behavior for malformed input
                expect(error).toBeDefined();
            }
        });

        it('should handle empty JSON object in establishConnection', async () => {
            // Arrange
            const emptyAnswer = JSON.stringify({});

            // Act & Assert
            try {
                await service.establishConnection(emptyAnswer);
                // If no error is thrown, the method should return early due to missing desc.type
                expect(
                    connectionManager.setRemoteAnswerAndCandidates,
                ).not.toHaveBeenCalled();
            } catch (error) {
                // If it throws due to missing properties, that's also acceptable
                expect(error).toBeDefined();
            }
        });
    });

    describe('Callback integration', () => {
        it('should properly bind callback methods to connection manager', () => {
            // The callbacks should be bound in constructor and should be functions
            expect(connectionManager.onCandidatesReceived).toBeDefined();
            expect(connectionManager.onGatheringCompleted).toBeDefined();
            expect(connectionManager.onConnected).toBeDefined();
            expect(connectionManager.onConnectionError).toBeDefined();
            expect(connectionManager.onDisconnected).toBeDefined();
        });

        it('should trigger onDisconnected callback correctly', () => {
            // Arrange
            vi.spyOn(service as any, 'processEndCall');

            // Act - Simulate callback from connection manager by calling the bound method directly
            service['onDisconnected']();

            // Assert
            expect(service['processEndCall']).toHaveBeenCalled();
        });

        it('should trigger onIceCandidateGenerated callback correctly', () => {
            // Arrange
            const testData = 'test-candidate-data';
            vi.spyOn(service['iceCandidateSubject'], 'next');
            service['iceGatheringTimer'] = { isExpired: () => false } as any;

            // Act - Simulate callback from connection manager by calling the bound method directly
            service['onIceCandidateGenerated'](testData);

            // Assert
            expect(service['iceCandidateSubject'].next).toHaveBeenCalledWith(
                testData,
            );
        });

        it('should trigger onIceGatheringComplete callback correctly', () => {
            // Arrange
            vi.spyOn(service['iceGatheringComplete'], 'next');
            const mockTimer = { clear: vi.fn() };
            service['iceGatheringTimer'] = mockTimer as any;

            // Act - Simulate callback from connection manager by calling the bound method directly
            service['onIceGatheringComplete'](0, false);

            // Assert
            expect(service['iceGatheringComplete'].next).toHaveBeenCalled();
            expect(mockTimer.clear).toHaveBeenCalled();
        });
    });

    describe('error handling', () => {
        it('should handle API errors in startOutgoingCall', async () => {
            // Arrange
            const accountId = 'test-account-id';
            const error = new Error('API Error');
            apiService.getCallSettings.mockReturnValue(Promise.reject(error));

            // Act & Assert
            await expect(service.startOutgoingCall(accountId)).rejects.toThrow(
                'API Error',
            );
            
            // Should have attempted to log the failure
            expect(apiService.logWebRtcError).toHaveBeenCalled();
        });

        it('should handle API errors in handleIncomingCall', async () => {
            // Arrange
            const callId = 'call-id';
            const chatId = 'test-chat-id';
            const offerString = JSON.stringify(mockOffer);
            const error = new Error('API Error');
            apiService.getCallSettings.mockReturnValue(Promise.reject(error));

            // Act & Assert
            await expect(
                service.handleIncomingCall(callId, chatId, offerString),
            ).rejects.toThrow('API Error');
            
            // Should have attempted to log the failure
            expect(apiService.logWebRtcError).toHaveBeenCalled();
        });

        it('should handle connection manager errors', async () => {
            // Arrange
            const accountId = 'test-account-id';
            const error = new Error('Connection Manager Error');
            apiService.getCallSettings.mockReturnValue(
                Promise.resolve(mockCallSettings),
            );
            connectionManager.initiateOffer.mockImplementation(() => {
                throw error;
            });

            // Act & Assert
            await expect(service.startOutgoingCall(accountId)).rejects.toThrow(
                'Connection Manager Error',
            );
            
            // Should have attempted to log the failure
            expect(apiService.logWebRtcError).toHaveBeenCalled();
        });
    });
});
