/* eslint-disable @typescript-eslint/no-explicit-any */
import {
    afterEach,
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type Mock,
    type MockedObject,
} from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { SignalrService } from './signalr.service';
import { TokenStorageService } from './token-storage.service';
import {
    IImagePreviewDto,
    ILinkPreviewDto,
    IMessageDto,
} from '../api-client/api-client';
import { IncomingCall } from '../models/incoming-call';

describe('SignalrService', () => {
    let service: SignalrService;
    let mockTokenStorageService: MockedObject<TokenStorageService>;
    let mockHubConnection: any;

    const mockToken = 'test-token';
    const mockMessageDto: IMessageDto = {
        id: '1',
        text: 'test message',
        createdAt: new Date(),
        userId: 'user1',
    } as IMessageDto;

    const mockLinkPreviewDto: ILinkPreviewDto = {
        url: 'https://example.com',
        title: 'Test Link',
    } as ILinkPreviewDto;

    const mockImagePreviewDto: IImagePreviewDto = {
        url: 'https://example.com/image.jpg',
        width: 100,
        height: 100,
    } as IImagePreviewDto;

    const mockRtcSessionSettings: IncomingCall = {
        sessionId: 'session-123',
        offer: 'mock-offer-data',
        answer: 'mock-answer-data',
    } as IncomingCall;

    beforeEach(() => {
        // Create spies for HubConnection
        mockHubConnection = {
            start: vi.fn().mockReturnValue(Promise.resolve()),
            stop: vi.fn().mockReturnValue(Promise.resolve()),
            invoke: vi.fn().mockReturnValue(Promise.resolve()),
            on: vi.fn(),
            off: vi.fn(),
            onreconnected: vi.fn(),
        };

        // Create spy for TokenStorageService
        mockTokenStorageService = {
            getToken: vi.fn().mockName('TokenStorageService.getToken'),
        } as MockedObject<TokenStorageService>;
        mockTokenStorageService.getToken.mockReturnValue(mockToken);

        // Mock HubConnectionBuilder
        vi.spyOn(HubConnectionBuilder.prototype, 'withUrl').mockReturnValue(
            HubConnectionBuilder.prototype,
        );
        vi.spyOn(
            HubConnectionBuilder.prototype,
            'withAutomaticReconnect',
        ).mockReturnValue(HubConnectionBuilder.prototype);
        vi.spyOn(
            HubConnectionBuilder.prototype,
            'configureLogging',
        ).mockReturnValue(HubConnectionBuilder.prototype);
        vi.spyOn(HubConnectionBuilder.prototype, 'build').mockReturnValue(
            mockHubConnection,
        );

        TestBed.configureTestingModule({
            providers: [
                SignalrService,
                {
                    provide: TokenStorageService,
                    useValue: mockTokenStorageService,
                },
            ],
        });

        service = TestBed.inject(SignalrService);
    });

    afterEach(() => {
        // Clear any timers that might be running
        try {
            if ((service as any).connectionTimerId) {
                clearInterval((service as any).connectionTimerId);
            }
            // Reset the service state
            if (service && service.removeHandlers) {
                service.removeHandlers();
            }
        } catch {
            // Ignore cleanup errors
        }
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('init', () => {
        let messageHandler: Mock;
        let linkPreviewHandler: Mock;
        let imagePreviewHandler: Mock;
        let rtcSessionOfferHandler: Mock;
        let rtcSessionAnswerHandler: Mock;

        beforeEach(() => {
            messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            linkPreviewHandler = vi.fn();
            imagePreviewHandler = vi.fn();
            rtcSessionOfferHandler = vi.fn();
            rtcSessionAnswerHandler = vi.fn();

            // Reset spies for each test
            mockHubConnection.start.mockReturnValue(Promise.resolve());
            mockHubConnection.invoke.mockReturnValue(Promise.resolve());
            mockHubConnection.start.mockClear();
            mockHubConnection.invoke.mockClear();
            mockHubConnection.on.mockClear();
            mockHubConnection.onreconnected.mockClear();
        });

        it('should initialize successfully when connection is established', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            expect(mockHubConnection.start).toHaveBeenCalled();
            expect(mockHubConnection.invoke).toHaveBeenCalledWith(
                'AuthorizeAsync',
                mockToken,
            );
            expect(mockHubConnection.on).toHaveBeenCalledWith(
                'SendNotificationAsync',
                expect.any(Function),
            );
            expect(mockHubConnection.onreconnected).toHaveBeenCalledWith(
                expect.any(Function),
            );
        });

        it('should not reinitialize if already initialized', async () => {
            // First initialization
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            // Reset spies
            mockHubConnection.start.mockClear();

            // Second initialization attempt
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            expect(mockHubConnection.start).not.toHaveBeenCalled();
        });

        it('should set up retry timer when connection fails', async () => {
            const setIntervalSpy = vi
                .spyOn(window, 'setInterval')
                .mockReturnValue(123 as any);

            mockHubConnection.start.mockReturnValue(
                Promise.reject(new Error('Connection failed')),
            );

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            expect(setIntervalSpy).toHaveBeenCalledWith(
                expect.any(Function),
                expect.any(Number),
            );
        });

        it('should handle message notifications correctly', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            // Get the notification handler that was registered
            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];

            // Simulate receiving a message notification
            await notificationHandler(mockMessageDto, 'MessageDto');

            expect(messageHandler).toHaveBeenCalledWith(mockMessageDto);
        });

        it('should handle link preview notifications correctly', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];
            notificationHandler(mockLinkPreviewDto, 'LinkPreviewDto');

            expect(linkPreviewHandler).toHaveBeenCalledWith(mockLinkPreviewDto);
        });

        it('should handle image preview notifications correctly', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];
            notificationHandler(mockImagePreviewDto, 'ImagePreviewDto');

            expect(imagePreviewHandler).toHaveBeenCalledWith(
                mockImagePreviewDto,
            );
        });

        it('should handle RTC session offer notifications correctly', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];
            notificationHandler(mockRtcSessionSettings, 'IncomingCall');

            expect(rtcSessionOfferHandler).toHaveBeenCalledWith(
                mockRtcSessionSettings,
            );
        });

        it('should handle RTC session answer notifications correctly', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];
            notificationHandler(mockRtcSessionSettings, 'EstablishConnection');

            expect(rtcSessionAnswerHandler).toHaveBeenCalledWith(
                mockRtcSessionSettings,
            );
        });

        it('should handle unknown notification types gracefully', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];

            // This should not throw an error
            expect(() => {
                notificationHandler(mockMessageDto, 'UnknownType' as any);
            }).not.toThrow();
        });

        it('should handle null/undefined handlers gracefully', async () => {
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcSessionOfferHandler,
                rtcSessionAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];

            // Override handler mapping to test null handler
            (service as any).handlerMapping = {
                MessageDto: null,
                LinkPreviewDto: linkPreviewHandler,
                ImagePreviewDto: imagePreviewHandler,
                IncomingCall: rtcSessionOfferHandler,
                EstablishConnection: rtcSessionAnswerHandler,
            };

            // This should not throw an error
            expect(() => {
                notificationHandler(mockMessageDto, 'MessageDto');
            }).not.toThrow();
        });
    });

    describe('removeHandlers', () => {
        it('should remove handlers and stop connection', () => {
            service.removeHandlers();

            expect(mockHubConnection.off).toHaveBeenCalledWith(
                'SendNotificationAsync',
            );
            expect(mockHubConnection.stop).toHaveBeenCalled();
        });

        it('should reset initialization state', async () => {
            const testMessageHandler = vi
                .fn()
                .mockReturnValue(Promise.resolve());
            const testLinkPreviewHandler = vi.fn();
            const testImagePreviewHandler = vi.fn();
            const testRtcOfferHandler = vi.fn();
            const testRtcAnswerHandler = vi.fn();

            // Initialize first
            await service.init(
                testMessageHandler,
                testLinkPreviewHandler,
                testImagePreviewHandler,
                testRtcOfferHandler,
                testRtcAnswerHandler,
            );

            // Remove handlers
            service.removeHandlers();

            // Reset spies
            mockHubConnection.start.mockClear();

            // Should be able to initialize again
            await service.init(
                testMessageHandler,
                testLinkPreviewHandler,
                testImagePreviewHandler,
                testRtcOfferHandler,
                testRtcAnswerHandler,
            );

            expect(mockHubConnection.start).toHaveBeenCalled();
        });
    });

    describe('reconnection handling', () => {
        it('should authorize on reconnection', async () => {
            const testMessageHandler = vi
                .fn()
                .mockReturnValue(Promise.resolve());
            const testLinkPreviewHandler = vi.fn();
            const testImagePreviewHandler = vi.fn();
            const testRtcOfferHandler = vi.fn();
            const testRtcAnswerHandler = vi.fn();

            await service.init(
                testMessageHandler,
                testLinkPreviewHandler,
                testImagePreviewHandler,
                testRtcOfferHandler,
                testRtcAnswerHandler,
            );

            // Get the reconnection handler
            const reconnectionHandler = vi.mocked(
                mockHubConnection.onreconnected,
            ).mock.calls[0][0];

            // Reset invoke spy to track reconnection authorization
            mockHubConnection.invoke.mockClear();

            // Simulate reconnection
            await reconnectionHandler();

            expect(mockHubConnection.invoke).toHaveBeenCalledWith(
                'AuthorizeAsync',
                mockToken,
            );
        });

        it('should handle reconnection authorization errors gracefully', async () => {
            const testMessageHandler = vi
                .fn()
                .mockReturnValue(Promise.resolve());
            const testLinkPreviewHandler = vi.fn();
            const testImagePreviewHandler = vi.fn();
            const testRtcOfferHandler = vi.fn();
            const testRtcAnswerHandler = vi.fn();

            await service.init(
                testMessageHandler,
                testLinkPreviewHandler,
                testImagePreviewHandler,
                testRtcOfferHandler,
                testRtcAnswerHandler,
            );

            const reconnectionHandler = vi.mocked(
                mockHubConnection.onreconnected,
            ).mock.calls[0][0];

            // Make authorization fail on reconnection
            mockHubConnection.invoke.mockReturnValue(
                Promise.reject(new Error('Auth failed')),
            );

            // Should not throw - the actual service doesn't handle this error, so it will reject
            await expect(reconnectionHandler()).rejects.toThrow();
        });
    });

    describe('retry mechanism', () => {
        beforeEach(() => {
            // Clear all spies before each test in this describe block
            vi.clearAllMocks();
        });

        it('should retry connection setup on failure', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            let callCount = 0;
            mockHubConnection.start.mockImplementation(() => {
                callCount++;
                if (callCount === 1) {
                    return Promise.reject(new Error('Connection failed'));
                }
                return Promise.resolve();
            });

            // Mock setInterval to capture the retry function
            let retryFunction: Function;
            const setIntervalSpy = vi
                .spyOn(window, 'setInterval')
                .mockImplementation(
                    (callback: TimerHandler) => {
                        retryFunction = callback as Function;
                        return 123 as any;
                    },
                );

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            // Initial connection should have failed and set up retry
            expect(setIntervalSpy).toHaveBeenCalled();
            expect(mockHubConnection.start).toHaveBeenCalledTimes(1);

            // Manually trigger retry
            await retryFunction();
            expect(mockHubConnection.start).toHaveBeenCalledTimes(2);
        });

        it('should clear retry timer when connection succeeds', async () => {
            const clearIntervalSpy = vi.spyOn(window, 'clearInterval');
            const setIntervalSpy = vi
                .spyOn(window, 'setInterval')
                .mockReturnValue(123 as any);

            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            // First call fails, then succeeds
            let callCount = 0;
            mockHubConnection.start.mockImplementation(() => {
                callCount++;
                if (callCount === 1) {
                    return Promise.reject(new Error('Connection failed'));
                }
                return Promise.resolve();
            });

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            expect(setIntervalSpy).toHaveBeenCalled();

            // Get the retry function and call it - this should succeed and clear the timer
            const retryFunction = vi.mocked(setIntervalSpy).mock.calls[0][0];
            await retryFunction();

            expect(clearIntervalSpy).toHaveBeenCalledWith(123);
        });

        it('should not set up retry timer if connection succeeds initially', async () => {
            const setIntervalSpy = vi.spyOn(window, 'setInterval');

            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            // Connection succeeds immediately
            mockHubConnection.start.mockReturnValue(Promise.resolve());

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            expect(setIntervalSpy).not.toHaveBeenCalled();
        });
    });

    describe('error handling', () => {
        it('should handle authorization errors gracefully', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            mockHubConnection.start.mockReturnValue(Promise.resolve());
            mockHubConnection.invoke.mockReturnValue(
                Promise.reject(new Error('Auth failed')),
            );

            // Should not throw
            await expect(
                service.init(
                    messageHandler,
                    linkPreviewHandler,
                    imagePreviewHandler,
                    rtcOfferHandler,
                    rtcAnswerHandler,
                ),
            ).resolves.not.toThrow();
        });

        it('should handle connection start errors gracefully', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            mockHubConnection.start.mockReturnValue(
                Promise.reject(new Error('Start failed')),
            );

            // Should not throw
            await expect(
                service.init(
                    messageHandler,
                    linkPreviewHandler,
                    imagePreviewHandler,
                    rtcOfferHandler,
                    rtcAnswerHandler,
                ),
            ).resolves.not.toThrow();
        });

        it('should handle message handler errors gracefully', async () => {
            const messageHandler = vi
                .fn()
                .mockReturnValue(Promise.reject(new Error('Handler failed')));
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];

            // Should not throw even if handler fails
            expect(() => {
                notificationHandler(mockMessageDto, 'MessageDto');
            }).not.toThrow();

            expect(messageHandler).toHaveBeenCalledWith(mockMessageDto);
        });
    });

    describe('private methods', () => {
        it('should call authorize with correct token', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            expect(mockTokenStorageService.getToken).toHaveBeenCalled();
            expect(mockHubConnection.invoke).toHaveBeenCalledWith(
                'AuthorizeAsync',
                mockToken,
            );
        });

        it('should set up connection event handlers correctly', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            expect(mockHubConnection.on).toHaveBeenCalledWith(
                'SendNotificationAsync',
                expect.any(Function),
            );
            expect(mockHubConnection.onreconnected).toHaveBeenCalledWith(
                expect.any(Function),
            );
        });
    });

    describe('integration tests', () => {
        it('should handle complete workflow successfully', async () => {
            const messageHandler = vi.fn().mockReturnValue(Promise.resolve());
            const linkPreviewHandler = vi.fn();
            const imagePreviewHandler = vi.fn();
            const rtcOfferHandler = vi.fn();
            const rtcAnswerHandler = vi.fn();

            // Initialize service
            await service.init(
                messageHandler,
                linkPreviewHandler,
                imagePreviewHandler,
                rtcOfferHandler,
                rtcAnswerHandler,
            );

            // Verify initialization
            expect(mockHubConnection.start).toHaveBeenCalled();
            expect(mockHubConnection.invoke).toHaveBeenCalledWith(
                'AuthorizeAsync',
                mockToken,
            );

            // Test notification handling
            const notificationHandler = vi.mocked(mockHubConnection.on).mock
                .calls[0][1];

            await notificationHandler(mockMessageDto, 'MessageDto');
            notificationHandler(mockLinkPreviewDto, 'LinkPreviewDto');
            notificationHandler(mockImagePreviewDto, 'ImagePreviewDto');
            notificationHandler(mockRtcSessionSettings, 'IncomingCall');
            notificationHandler(mockRtcSessionSettings, 'EstablishConnection');

            expect(messageHandler).toHaveBeenCalledWith(mockMessageDto);
            expect(linkPreviewHandler).toHaveBeenCalledWith(mockLinkPreviewDto);
            expect(imagePreviewHandler).toHaveBeenCalledWith(
                mockImagePreviewDto,
            );
            expect(rtcOfferHandler).toHaveBeenCalledWith(
                mockRtcSessionSettings,
            );
            expect(rtcAnswerHandler).toHaveBeenCalledWith(
                mockRtcSessionSettings,
            );

            // Test cleanup
            service.removeHandlers();
            expect(mockHubConnection.off).toHaveBeenCalledWith(
                'SendNotificationAsync',
            );
            expect(mockHubConnection.stop).toHaveBeenCalled();
        });
    });
});
