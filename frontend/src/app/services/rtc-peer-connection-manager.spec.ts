import { beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { IceCandidateMetricsService } from './ice-candidate-metrics.service';
import { RtcConnectionDiagnosticsService } from './rtc-connection-diagnostics.service';
import { RtcPeerConnectionManager } from './rtc-peer-connection-manager';
import { mediaStreamConstraintFallbacks } from './media-stream-constraint-fallbacks';

describe('RtcPeerConnectionManager', () => {
    let service: RtcPeerConnectionManager;
    let iceCandidateMetricsService: any;
    let rtcConnectionDiagnosticsService: any;
    let mockConnection: any;
    let mockMediaStream: any;
    let mockTrack: any;

    const mockConfig: RTCConfiguration = {
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }],
    };

    const mockOffer: RTCSessionDescriptionInit = {
        type: 'offer',
        sdp: 'mock-offer-sdp',
    };

    const mockAnswer: RTCSessionDescriptionInit = {
        type: 'answer',
        sdp: 'mock-answer-sdp',
    };

    const mockCandidate: RTCIceCandidateInit = {
        candidate: 'candidate:1 1 UDP 2130706431 192.168.1.1 54400 typ host',
        sdpMLineIndex: 0,
        sdpMid: '0',
    };

    beforeEach(() => {
        // Mock MediaStream and MediaStreamTrack
        mockTrack = {
            stop: vi.fn(),
            enabled: true,
            kind: 'video',
        };
        mockMediaStream = {
            getTracks: vi.fn(),
            getVideoTracks: vi.fn(),
            getAudioTracks: vi.fn(),
        };
        (mockMediaStream.getTracks as Mock).mockReturnValue([mockTrack]);
        (mockMediaStream.getVideoTracks as Mock).mockReturnValue([mockTrack]);
        (mockMediaStream.getAudioTracks as Mock).mockReturnValue([mockTrack]);

        // Mock navigator.mediaDevices.getUserMedia
        vi.spyOn(navigator.mediaDevices, 'getUserMedia').mockReturnValue(
            Promise.resolve(mockMediaStream),
        );

        iceCandidateMetricsService = {
            hasMinimumCandidateCount: vi.fn(),
            hasSufficientServers: vi.fn(),
        };

        rtcConnectionDiagnosticsService = {
            gatherConnectionDiagnostics: vi.fn().mockResolvedValue({
                connectionState: 'connected',
                localCandidateTypes: {},
                remoteCandidateTypes: {},
                browser: 'Chrome',
                platform: 'Win32',
            }),
        };

        TestBed.configureTestingModule({
            providers: [
                RtcPeerConnectionManager,
                {
                    provide: IceCandidateMetricsService,
                    useValue: iceCandidateMetricsService,
                },
                {
                    provide: RtcConnectionDiagnosticsService,
                    useValue: rtcConnectionDiagnosticsService,
                },
            ],
        });

        service = TestBed.inject(RtcPeerConnectionManager);

        // Get reference to the actual connection instance created by the service
        mockConnection = service['connection'];
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
        expect(mockConnection).toBeDefined();
        expect(mockConnection.onicecandidate).toBeDefined();
    });

    describe('initiateOffer', () => {
        it('should create and set local offer', async () => {
            // Act
            await service.initiateOffer(mockConfig);

            // Assert
            expect(mockConnection.setConfiguration).toHaveBeenCalledWith(
                mockConfig,
            );
            expect(mockConnection.createOffer).toHaveBeenCalled();
            expect(mockConnection.setLocalDescription).toHaveBeenCalled();
            expect(service['isGathering']).toBe(true);
            expect(service['localCandidates']).toEqual([]);
        });
    });

    describe('handleOfferAndCreateAnswer', () => {
        it('should handle offer and create answer', async () => {
            // Arrange
            const candidates = [mockCandidate];

            // Act
            await service.handleOfferAndCreateAnswer(
                mockConfig,
                mockOffer,
                candidates,
            );

            // Assert
            expect(mockConnection.setConfiguration).toHaveBeenCalledWith(
                mockConfig,
            );
            expect(mockConnection.setRemoteDescription).toHaveBeenCalled();
            expect(mockConnection.addIceCandidate).toHaveBeenCalled();
            expect(mockConnection.createAnswer).toHaveBeenCalled();
            expect(mockConnection.setLocalDescription).toHaveBeenCalled();
            expect(service['isGathering']).toBe(true);
            expect(service['localCandidates']).toEqual([]);
        });

        it('should handle offer without candidates', async () => {
            // Act
            await service.handleOfferAndCreateAnswer(
                mockConfig,
                mockOffer,
                null,
            );

            // Assert
            expect(mockConnection.setRemoteDescription).toHaveBeenCalled();
            expect(mockConnection.addIceCandidate).not.toHaveBeenCalled();
            expect(mockConnection.createAnswer).toHaveBeenCalled();
        });
    });

    describe('requestCompleteGathering', () => {
        it('should return early if minimum candidate count not met', () => {
            // Arrange
            iceCandidateMetricsService.hasMinimumCandidateCount.mockReturnValue(
                false,
            );

            // Act
            service.requestCompleteGathering();

            // Assert
            expect(
                iceCandidateMetricsService.hasMinimumCandidateCount,
            ).toHaveBeenCalledWith([]);
            expect(
                iceCandidateMetricsService.hasSufficientServers,
            ).not.toHaveBeenCalled();
        });

        it('should finalize gathering if sufficient servers available', () => {
            // Arrange
            iceCandidateMetricsService.hasMinimumCandidateCount.mockReturnValue(
                true,
            );
            iceCandidateMetricsService.hasSufficientServers.mockReturnValue(
                true,
            );
            service.onGatheringCompleted = vi.fn();

            // Act
            service.requestCompleteGathering();

            // Assert
            expect(
                iceCandidateMetricsService.hasSufficientServers,
            ).toHaveBeenCalledWith([]);
            expect(service['isGathering']).toBe(false);
            expect(service.onGatheringCompleted).toHaveBeenCalled();
        });

        it('should not finalize gathering if insufficient servers', () => {
            // Arrange
            iceCandidateMetricsService.hasMinimumCandidateCount.mockReturnValue(
                true,
            );
            iceCandidateMetricsService.hasSufficientServers.mockReturnValue(
                false,
            );
            service.onGatheringCompleted = vi.fn();

            // Act
            service.requestCompleteGathering();

            // Assert
            expect(service['isGathering']).toBe(true);
            expect(service.onGatheringCompleted).not.toHaveBeenCalled();
        });
    });

    describe('setRemoteAnswerAndCandidates', () => {
        it('should set remote answer and candidates when in correct state', async () => {
            // Arrange
            mockConnection.signalingState = 'have-local-offer';
            const candidates = [mockCandidate];

            // Act
            await service.setRemoteAnswerAndCandidates(mockAnswer, candidates);

            // Assert
            expect(mockConnection.setRemoteDescription).toHaveBeenCalled();
            expect(mockConnection.addIceCandidate).toHaveBeenCalled();
        });

        it('should return early if not in have-local-offer state', async () => {
            // Arrange
            mockConnection.signalingState = 'stable';

            // Act
            await service.setRemoteAnswerAndCandidates(mockAnswer, [
                mockCandidate,
            ]);

            // Assert
            expect(mockConnection.setRemoteDescription).not.toHaveBeenCalled();
            expect(mockConnection.addIceCandidate).not.toHaveBeenCalled();
        });

        it('should handle null candidates', async () => {
            // Arrange
            mockConnection.signalingState = 'have-local-offer';

            // Act
            await service.setRemoteAnswerAndCandidates(mockAnswer, null);

            // Assert
            expect(mockConnection.setRemoteDescription).toHaveBeenCalled();
            expect(mockConnection.addIceCandidate).not.toHaveBeenCalled();
        });
    });

    describe('startMediaCapture', () => {
        let mockLocalVideo: any;
        let mockRemoteVideo: any;

        beforeEach(() => {
            mockLocalVideo = { srcObject: null };
            mockRemoteVideo = { srcObject: null };
        });

        it('should start media capture successfully', async () => {
            // Arrange
            service.onCandidatesReceived = vi.fn();

            // Act
            await service.startMediaCapture(mockLocalVideo, mockRemoteVideo, 'user');

            // Assert
            expect(navigator.mediaDevices.getUserMedia).toHaveBeenCalled();
            expect(mockLocalVideo.srcObject).toBeInstanceOf(MediaStream);
            expect(mockConnection.addTrack).toHaveBeenCalledWith(
                mockTrack,
                mockMediaStream,
            );
            expect(service.isMediaCaptured).toBe(true);
        });

        it('should handle media capture error', async () => {
            // Arrange
            const error = new Error('Media access denied');
            (navigator.mediaDevices.getUserMedia as Mock).mockRejectedValue(
                error,
            );
            vi.spyOn(console, 'error');

            // Act
            await service.startMediaCapture(mockLocalVideo, mockRemoteVideo, 'user');

            // Assert
            expect(console.error).toHaveBeenCalledWith(error);
            expect(console.error).toHaveBeenCalledTimes(
                mediaStreamConstraintFallbacks.length,
            );
            expect(service.isMediaCaptured).toBe(false);
        });

        it('should handle ontrack event', async () => {
            // Arrange
            const mockRemoteStream = {
                getTracks: vi.fn().mockName('MediaStream.getTracks'),
            };
            const mockTrackEvent = {
                streams: [mockRemoteStream],
                track: mockTrack,
                receiver: {} as RTCRtpReceiver,
                transceiver: {} as RTCRtpTransceiver,
            } as unknown as RTCTrackEvent;

            // Act
            await service.startMediaCapture(mockLocalVideo, mockRemoteVideo, 'user');

            // Simulate the ontrack event by calling the handler directly
            if (mockConnection.ontrack) {
                mockConnection.ontrack(mockTrackEvent);
            }

            // Assert
            expect(mockRemoteVideo.srcObject).toBe(mockRemoteStream);
            expect(service['remoteMediaStream']).toBe(mockRemoteStream);
        });
    });

    describe('reconnectVideoElements', () => {
        let mockLocalVideo: any;
        let mockRemoteVideo: any;

        beforeEach(() => {
            mockLocalVideo = { srcObject: null };
            mockRemoteVideo = { srcObject: null };
        });

        it('should reconnect video elements', () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;
            service['remoteMediaStream'] = mockMediaStream;

            // Act
            service.reconnectVideoElements({
                local: mockLocalVideo,
                remote: mockRemoteVideo,
            });

            // Assert
            expect(mockLocalVideo.srcObject).toBeInstanceOf(MediaStream);
            expect(mockRemoteVideo.srcObject).toBe(mockMediaStream);
        });

        it('should handle null streams', () => {
            // Arrange
            service['localMediaStream'] = null;
            service['remoteMediaStream'] = null;

            // Act
            service.reconnectVideoElements({
                local: mockLocalVideo,
                remote: mockRemoteVideo,
            });

            // Assert
            expect(mockLocalVideo.srcObject).toBeNull();
            expect(mockRemoteVideo.srcObject).toBeNull();
        });
    });

    describe('endCall', () => {
        it('should end call and reset state', () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;
            service['remoteMediaStream'] = mockMediaStream;
            service['localCandidates'] = [mockCandidate as RTCIceCandidate];
            service['isGathering'] = false;
            service.isMediaCaptured = true;

            // Act
            service.reinitialize();

            // Assert
            expect(mockTrack.stop).toHaveBeenCalled();
            expect(mockConnection.close).toHaveBeenCalled();
            expect(service['remoteMediaStream']).toBeNull();
            expect(service['localCandidates']).toEqual([]);
            expect(service['isGathering']).toBe(true);
            expect(service.isMediaCaptured).toBe(false);
        });

        it('should handle null local media stream', () => {
            // Arrange
            service['localMediaStream'] = null;

            // Act
            service.reinitialize();

            // Assert
            expect(mockTrack.stop).not.toHaveBeenCalled();
            expect(mockConnection.close).toHaveBeenCalled();
        });
    });

    describe('onIceCandidateReceived', () => {
        it('should return early if not gathering', () => {
            // Arrange
            service['isGathering'] = false;
            const mockEvent = {
                candidate: mockCandidate,
            } as RTCPeerConnectionIceEvent;
            service.onCandidatesReceived = vi.fn();

            // Act
            service['onIceCandidateReceived'](mockEvent);

            // Assert
            expect(service.onCandidatesReceived).not.toHaveBeenCalled();
        });

        it('should finalize gathering when candidate is null', () => {
            // Arrange
            service['isGathering'] = true;
            const mockEvent = { candidate: null } as RTCPeerConnectionIceEvent;
            service.onGatheringCompleted = vi.fn();

            // Act
            service['onIceCandidateReceived'](mockEvent);

            // Assert
            expect(service['isGathering']).toBe(false);
            expect(service.onGatheringCompleted).toHaveBeenCalled();
        });

        it('should add candidate and call onCandidatesReceived', () => {
            // Arrange
            service['isGathering'] = true;
            const mockRTCCandidate = mockCandidate as RTCIceCandidate;
            const mockEvent = {
                candidate: mockRTCCandidate,
            } as RTCPeerConnectionIceEvent;

            // Set the localDescription on the mock connection
            mockConnection.localDescription =
                mockOffer as RTCSessionDescription;
            service.onCandidatesReceived = vi.fn();

            // Act
            service['onIceCandidateReceived'](mockEvent);

            // Assert
            expect(service['localCandidates']).toContain(mockRTCCandidate);
            expect(service.onCandidatesReceived).toHaveBeenCalledWith(
                JSON.stringify({
                    desc: mockOffer,
                    candidates: [mockRTCCandidate],
                }),
            );
        });
    });

    describe('private methods', () => {
        it('should connect local video when stream exists', () => {
            // Arrange
            const mockVideo = { srcObject: null };
            service['localMediaStream'] = mockMediaStream;

            // Act
            service['connectLocalVideo'](mockVideo as HTMLVideoElement);

            // Assert
            expect(mockVideo.srcObject).toBeInstanceOf(MediaStream);
        });

        it('should not connect local video when stream is null', () => {
            // Arrange
            const mockVideo = { srcObject: null };
            service['localMediaStream'] = null;

            // Act
            service['connectLocalVideo'](mockVideo as HTMLVideoElement);

            // Assert
            expect(mockVideo.srcObject).toBeNull();
        });

        it('should connect remote video when stream exists', () => {
            // Arrange
            const mockVideo = { srcObject: null };
            service['remoteMediaStream'] = mockMediaStream;

            // Act
            service['connectRemoteVideo'](mockVideo as HTMLVideoElement);

            // Assert
            expect(mockVideo.srcObject).toBe(mockMediaStream);
        });

        it('should not connect remote video when stream is null', () => {
            // Arrange
            const mockVideo = { srcObject: null };
            service['remoteMediaStream'] = null;

            // Act
            service['connectRemoteVideo'](mockVideo as HTMLVideoElement);

            // Assert
            expect(mockVideo.srcObject).toBeNull();
        });

        it('should finalize ice gathering', () => {
            // Arrange
            service['isGathering'] = true;
            service.onGatheringCompleted = vi.fn();

            // Act
            service['finalizeIceGathering'](true);

            // Assert
            expect(service['isGathering']).toBe(false);
            expect(service.onGatheringCompleted).toHaveBeenCalled();
        });
    });

    describe('setVideoEnabled', () => {
        it('should enable video tracks when enabled is true', () => {
            // Arrange
            const mockVideoTrack = { enabled: false, kind: 'video' };
            mockMediaStream.getVideoTracks = vi
                .fn()
                .mockReturnValue([mockVideoTrack]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setVideoEnabled(true);

            // Assert
            expect(mockMediaStream.getVideoTracks).toHaveBeenCalled();
            expect(mockVideoTrack.enabled).toBe(true);
        });

        it('should disable video tracks when enabled is false', () => {
            // Arrange
            const mockVideoTrack = { enabled: true, kind: 'video' };
            mockMediaStream.getVideoTracks = vi
                .fn()
                .mockReturnValue([mockVideoTrack]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setVideoEnabled(false);

            // Assert
            expect(mockMediaStream.getVideoTracks).toHaveBeenCalled();
            expect(mockVideoTrack.enabled).toBe(false);
        });

        it('should handle null local media stream', () => {
            // Arrange
            service['localMediaStream'] = null;

            // Act & Assert - should not throw
            expect(() => service.setVideoEnabled(true)).not.toThrow();
        });

        it('should handle multiple video tracks', () => {
            // Arrange
            const mockVideoTrack1 = { enabled: false, kind: 'video' };
            const mockVideoTrack2 = { enabled: false, kind: 'video' };
            mockMediaStream.getVideoTracks = vi
                .fn()
                .mockReturnValue([mockVideoTrack1, mockVideoTrack2]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setVideoEnabled(true);

            // Assert
            expect(mockVideoTrack1.enabled).toBe(true);
            expect(mockVideoTrack2.enabled).toBe(true);
        });
    });

    describe('setAudioEnabled', () => {
        it('should enable audio tracks when enabled is true', () => {
            // Arrange
            const mockAudioTrack = { enabled: false, kind: 'audio' };
            mockMediaStream.getAudioTracks = vi
                .fn()
                .mockReturnValue([mockAudioTrack]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setAudioEnabled(true);

            // Assert
            expect(mockMediaStream.getAudioTracks).toHaveBeenCalled();
            expect(mockAudioTrack.enabled).toBe(true);
        });

        it('should disable audio tracks when enabled is false', () => {
            // Arrange
            const mockAudioTrack = { enabled: true, kind: 'audio' };
            mockMediaStream.getAudioTracks = vi
                .fn()
                .mockReturnValue([mockAudioTrack]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setAudioEnabled(false);

            // Assert
            expect(mockMediaStream.getAudioTracks).toHaveBeenCalled();
            expect(mockAudioTrack.enabled).toBe(false);
        });

        it('should handle null local media stream', () => {
            // Arrange
            service['localMediaStream'] = null;

            // Act & Assert - should not throw
            expect(() => service.setAudioEnabled(true)).not.toThrow();
        });

        it('should handle multiple audio tracks', () => {
            // Arrange
            const mockAudioTrack1 = { enabled: false, kind: 'audio' };
            const mockAudioTrack2 = { enabled: false, kind: 'audio' };
            mockMediaStream.getAudioTracks = vi
                .fn()
                .mockReturnValue([mockAudioTrack1, mockAudioTrack2]);
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.setAudioEnabled(true);

            // Assert
            expect(mockAudioTrack1.enabled).toBe(true);
            expect(mockAudioTrack2.enabled).toBe(true);
        });
    });

    describe('edge cases and error handling', () => {
        it('should handle connection setup with null video elements', async () => {
            // Act & Assert - should not throw
            await expect(
                service.startMediaCapture(null as any, null as any, 'user'),
            ).resolves.not.toThrow();
        });

        it('should handle reconnection with null video elements', () => {
            // Act & Assert - should not throw
            expect(() =>
                service.reconnectVideoElements({
                local: null as any,
                remote: null as any,
            }),
            ).not.toThrow();
        });

        it('should handle ice candidate with null candidate gracefully', () => {
            // Arrange
            service['isGathering'] = true;
            service.onGatheringCompleted = vi.fn();
            const mockEvent = { candidate: null } as RTCPeerConnectionIceEvent;

            // Act
            service['onIceCandidateReceived'](mockEvent);

            // Assert
            expect(service['isGathering']).toBe(false);
            expect(service.onGatheringCompleted).toHaveBeenCalled();
        });

        it('should handle onCandidatesReceived callback being undefined', () => {
            // Arrange
            service['isGathering'] = true;
            service.onCandidatesReceived = undefined as any;
            const mockRTCCandidate = mockCandidate as RTCIceCandidate;
            const mockEvent = {
                candidate: mockRTCCandidate,
            } as RTCPeerConnectionIceEvent;
            Object.defineProperty(mockConnection, 'localDescription', {
                value: mockOffer as RTCSessionDescription,
                writable: true,
            });

            // Act & Assert - should not throw
            expect(() =>
                service['onIceCandidateReceived'](mockEvent),
            ).not.toThrow();
        });

        it('should handle reinitialize when connection is null', () => {
            // Arrange
            service['connection'] = null as any;

            // Act & Assert - should not throw
            expect(() => service.reinitialize()).not.toThrow();
        });

        it('should handle connectLocalVideo with null video element', () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;

            // Act & Assert - should not throw
            expect(() =>
                service['connectLocalVideo'](null as any),
            ).not.toThrow();
        });

        it('should handle connectRemoteVideo with null video element', () => {
            // Arrange
            service['remoteMediaStream'] = mockMediaStream;

            // Act & Assert - should not throw
            expect(() =>
                service['connectRemoteVideo'](null as any),
            ).not.toThrow();
        });
    });

    describe('integration scenarios', () => {
        it('should maintain gathering state through offer creation', async () => {
            // Arrange
            expect(service['isGathering']).toBe(true);

            // Act
            await service.initiateOffer(mockConfig);

            // Assert
            expect(service['isGathering']).toBe(true);
            expect(service['localCandidates']).toEqual([]);
        });

        it('should reset gathering state on reinitialize', () => {
            // Arrange
            service['isGathering'] = false;
            service['localCandidates'] = [mockCandidate as RTCIceCandidate];

            // Act
            service.reinitialize();

            // Assert
            expect(service['isGathering']).toBe(true);
            expect(service['localCandidates']).toEqual([]);
        });
    });

    describe('stopMediaCapture', () => {
        it('should stop all tracks on the local media stream', () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;

            // Act
            service.stopMediaCapture();

            // Assert
            expect(mockMediaStream.getTracks).toHaveBeenCalled();
            expect(mockTrack.stop).toHaveBeenCalled();
        });

        it('should do nothing when local media stream is null', () => {
            // Arrange
            service['localMediaStream'] = null;

            // Act & Assert - should not throw
            expect(() => service.stopMediaCapture()).not.toThrow();
            expect(mockTrack.stop).not.toHaveBeenCalled();
        });
    });

    describe('getDiagnostics', () => {
        it('should delegate to rtcConnectionDiagnosticsService', async () => {
            // Act
            const result = await service.getDiagnostics();

            // Assert
            expect(
                rtcConnectionDiagnosticsService.gatherConnectionDiagnostics,
            ).toHaveBeenCalledWith(service['connection']);
            expect(result).toMatchObject({ connectionState: 'connected' });
        });
    });

    describe('switchCamera', () => {
        let mockLocalVideo: any;
        let mockRemoteVideo: any;
        let mockSender: any;

        beforeEach(() => {
            mockLocalVideo = { srcObject: null };
            mockRemoteVideo = { srcObject: null };
            mockSender = {
                track: { kind: 'video' },
                replaceTrack: vi.fn().mockResolvedValue(undefined),
            };
        });

        it('should stop capture, restart with new facingMode, and replace video track', async () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;
            mockMediaStream.getVideoTracks = vi.fn().mockReturnValue([mockTrack]);
            mockConnection.getSenders = vi.fn().mockReturnValue([mockSender]);

            // Act
            await service.switchCamera(mockLocalVideo, mockRemoteVideo, 'environment');

            // Assert
            expect(mockTrack.stop).toHaveBeenCalled();
            expect(navigator.mediaDevices.getUserMedia).toHaveBeenCalled();
            expect(mockSender.replaceTrack).toHaveBeenCalledWith(mockTrack);
        });

        it('should not call replaceTrack when no video sender exists', async () => {
            // Arrange
            service['localMediaStream'] = mockMediaStream;
            mockMediaStream.getVideoTracks = vi.fn().mockReturnValue([mockTrack]);
            mockConnection.getSenders = vi.fn().mockReturnValue([
                { track: { kind: 'audio' }, replaceTrack: vi.fn() },
            ]);

            // Act
            await service.switchCamera(mockLocalVideo, mockRemoteVideo, 'user');

            // Assert
            expect(mockSender.replaceTrack).not.toHaveBeenCalled();
        });
    });

    describe('_onConnectionStateChange', () => {
        it('should call onConnected when state is connected', () => {
            // Arrange
            service.onConnected = vi.fn();
            mockConnection.connectionState = 'connected';

            // Act
            service['_onConnectionStateChange']();

            // Assert
            expect(service.onConnected).toHaveBeenCalledTimes(1);
        });

        it('should call onConnectionError when state is failed', () => {
            // Arrange
            service.onConnectionError = vi.fn();
            mockConnection.connectionState = 'failed';

            // Act
            service['_onConnectionStateChange']();

            // Assert
            expect(service.onConnectionError).toHaveBeenCalledTimes(1);
        });

        it('should call onDisconnected when state is disconnected', () => {
            // Arrange
            service.onDisconnected = vi.fn();
            mockConnection.connectionState = 'disconnected';

            // Act
            service['_onConnectionStateChange']();

            // Assert
            expect(service.onDisconnected).toHaveBeenCalledTimes(1);
        });

        it('should not throw when callbacks are undefined', () => {
            // Arrange
            mockConnection.connectionState = 'connected';

            // Act & Assert
            expect(() => service['_onConnectionStateChange']()).not.toThrow();
        });
    });

    describe('onIceConnectionStateChange', () => {
        it('should call onIceServerError when ice connection state is failed', () => {
            // Arrange
            service.onIceServerError = vi.fn();
            mockConnection.iceConnectionState = 'failed';

            // Act
            service['onIceConnectionStateChange']();

            // Assert
            expect(service.onIceServerError).toHaveBeenCalledWith(
                undefined,
                expect.any(Error),
            );
        });

        it('should not call onIceServerError for non-failed states', () => {
            // Arrange
            service.onIceServerError = vi.fn();
            mockConnection.iceConnectionState = 'connected';

            // Act
            service['onIceConnectionStateChange']();

            // Assert
            expect(service.onIceServerError).not.toHaveBeenCalled();
        });
    });

    describe('onIceCandidateError', () => {
        it('should call onIceServerError with error text', () => {
            // Arrange
            service.onIceServerError = vi.fn();
            const mockEvent = { errorText: 'STUN error' } as RTCPeerConnectionIceErrorEvent;

            // Act
            service['onIceCandidateError'](mockEvent);

            // Assert
            expect(service.onIceServerError).toHaveBeenCalledWith(
                'STUN error',
                expect.any(Error),
            );
        });

        it('should not throw when onIceServerError is undefined', () => {
            // Arrange
            service.onIceServerError = undefined as any;
            const mockEvent = { errorText: 'STUN error' } as RTCPeerConnectionIceErrorEvent;

            // Act & Assert
            expect(() => service['onIceCandidateError'](mockEvent)).not.toThrow();
        });
    });
});
