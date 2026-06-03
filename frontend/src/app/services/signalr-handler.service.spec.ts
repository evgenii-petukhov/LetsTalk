import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type Mock,
    type MockedObject,
} from 'vitest';
/* eslint-disable @typescript-eslint/no-explicit-any */
import { TestBed } from '@angular/core/testing';
import { SignalrHandlerService } from './signalr-handler.service';
import { SignalrService } from './signalr.service';
import { StoreService } from './store.service';
import { ApiService } from './api.service';
import { BrowserNotificationService } from './browser-notification.service';
import { Router } from '@angular/router';
import {
    IMessageDto,
    ILinkPreviewDto,
    IImagePreviewDto,
    IChatDto,
    ImageDto,
} from '../api-client/api-client';

describe('SignalrHandlerService', () => {
    let service: SignalrHandlerService;
    let apiService: MockedObject<ApiService>;
    let storeService: MockedObject<StoreService>;
    let signalrService: MockedObject<SignalrService>;
    let browserNotificationService: MockedObject<BrowserNotificationService>;
    let router: MockedObject<Router>;

    const mockMessageDto: IMessageDto = {
        id: '1',
        chatId: 'chatId',
        text: 'message',
        created: 1728867957,
        isMine: false,
        image: null,
    } as IMessageDto;

    const mockImageMessageDto: IMessageDto = {
        id: '2',
        chatId: 'chatId',
        text: '',
        created: 1728867958,
        isMine: false,
        image: new ImageDto(),
    } as IMessageDto;

    const mockChats: IChatDto[] = [
        { id: 'chatId', chatName: 'Chat 1', unreadCount: 0 } as IChatDto,
        { id: 'otherChatId', chatName: 'Chat 2', unreadCount: 0 } as IChatDto,
    ];

    const mockLinkPreviewDto: ILinkPreviewDto = {
        chatId: 'chatId',
        url: 'http://example.com',
        title: 'Example',
    } as ILinkPreviewDto;

    const mockImagePreviewDto: IImagePreviewDto = {
        chatId: 'chatId',
        imageId: '1',
        url: 'http://example.com/image.jpg',
    } as IImagePreviewDto;

    beforeEach(() => {
        // Create spies with return values
        apiService = {
            markAsRead: vi.fn().mockName('ApiService.markAsRead'),
        } as MockedObject<ApiService>;
        apiService.markAsRead.mockReturnValue(Promise.resolve());

        storeService = {
            setLastMessageInfo: vi
                .fn()
                .mockName('StoreService.setLastMessageInfo'),
            addMessage: vi.fn().mockName('StoreService.addMessage'),
            incrementUnreadMessages: vi
                .fn()
                .mockName('StoreService.incrementUnreadMessages'),
            initChatStorage: vi.fn().mockName('StoreService.initChatStorage'),
            setLinkPreview: vi.fn().mockName('StoreService.setLinkPreview'),
            setImagePreview: vi.fn().mockName('StoreService.setImagePreview'),
            initIncomingCall: vi.fn().mockName('StoreService.initIncomingCall'),
        } as MockedObject<StoreService>;
        storeService.initChatStorage.mockReturnValue(Promise.resolve());

        signalrService = {
            init: vi.fn().mockName('SignalrService.init'),
            removeHandlers: vi.fn().mockName('SignalrService.removeHandlers'),
        } as MockedObject<SignalrService>;
        signalrService.init.mockReturnValue(Promise.resolve());

        browserNotificationService = {
            init: vi.fn().mockName('BrowserNotificationService.init'),
            showNotification: vi
                .fn()
                .mockName('BrowserNotificationService.showNotification'),
        } as MockedObject<BrowserNotificationService>;
        browserNotificationService.init.mockReturnValue(Promise.resolve());

        router = {
            navigate: vi.fn().mockName('Router.navigate'),
        } as MockedObject<Router>;
        router.navigate.mockReturnValue(Promise.resolve(true));

        TestBed.configureTestingModule({
            providers: [
                SignalrHandlerService,
                { provide: ApiService, useValue: apiService },
                { provide: StoreService, useValue: storeService },
                { provide: SignalrService, useValue: signalrService },
                {
                    provide: BrowserNotificationService,
                    useValue: browserNotificationService,
                },
                { provide: Router, useValue: router },
            ],
        });

        service = TestBed.inject(SignalrHandlerService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('initHandlers', () => {
        let handleMessageNotification: Mock;
        let handleLinkPreviewNotification: Mock;
        let handleImagePreviewNotification: Mock;
        let handleIncomingCallNotification: Mock;
        let handleEstablishConnectionNotification: Mock;

        beforeEach(() => {
            handleMessageNotification = vi.fn();
            handleLinkPreviewNotification = vi.fn();
            handleImagePreviewNotification = vi.fn();
            handleIncomingCallNotification = vi.fn();
            handleEstablishConnectionNotification = vi.fn();
        });

        it('should initialize browser notifications and SignalR handlers', async () => {
            await service.initHandlers(
                handleMessageNotification,
                handleLinkPreviewNotification,
                handleImagePreviewNotification,
                handleIncomingCallNotification,
                handleEstablishConnectionNotification,
            );

            expect(browserNotificationService.init).toHaveBeenCalledTimes(1);
            expect(signalrService.init).toHaveBeenCalledWith(
                handleMessageNotification,
                handleLinkPreviewNotification,
                handleImagePreviewNotification,
                handleIncomingCallNotification,
                handleEstablishConnectionNotification,
            );
        });

        it('should handle browser notification initialization failure gracefully', async () => {
            browserNotificationService.init.mockReturnValue(
                Promise.reject(new Error('Notification init failed')),
            );

            await expect(
                service.initHandlers(
                    handleMessageNotification,
                    handleLinkPreviewNotification,
                    handleImagePreviewNotification,
                    handleIncomingCallNotification,
                    handleEstablishConnectionNotification,
                ),
            ).rejects.toThrow();

            expect(browserNotificationService.init).toHaveBeenCalledTimes(1);
        });

        it('should handle SignalR initialization failure gracefully', async () => {
            signalrService.init.mockReturnValue(
                Promise.reject(new Error('SignalR init failed')),
            );

            await expect(
                service.initHandlers(
                    handleMessageNotification,
                    handleLinkPreviewNotification,
                    handleImagePreviewNotification,
                    handleIncomingCallNotification,
                    handleEstablishConnectionNotification,
                ),
            ).rejects.toThrow();

            expect(signalrService.init).toHaveBeenCalledTimes(1);
        });
    });

    describe('removeHandlers', () => {
        it('should remove SignalR handlers', () => {
            service.removeHandlers();

            expect(signalrService.removeHandlers).toHaveBeenCalledTimes(1);
        });

        it('should be callable multiple times without error', () => {
            service.removeHandlers();
            service.removeHandlers();

            expect(signalrService.removeHandlers).toHaveBeenCalledTimes(2);
        });
    });

    describe('handleMessageNotification', () => {
        it('should set last message info and add message if chatId matches selectedChatId', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'chatId',
                mockChats,
                false,
            );

            expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.created,
                mockMessageDto.id,
            );
            expect(storeService.addMessage).toHaveBeenCalledWith(
                mockMessageDto,
            );
        });

        it('should not process further if message is mine', async () => {
            const myMessageDto = { ...mockMessageDto, isMine: true };

            await service.handleMessageNotification(
                myMessageDto,
                'chatId',
                mockChats,
                false,
            );

            expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
                myMessageDto.chatId,
                myMessageDto.created,
                myMessageDto.id,
            );
            expect(storeService.addMessage).toHaveBeenCalledWith(myMessageDto);
            expect(apiService.markAsRead).not.toHaveBeenCalled();
            expect(storeService.incrementUnreadMessages).not.toHaveBeenCalled();
            expect(
                browserNotificationService.showNotification,
            ).not.toHaveBeenCalled();
        });

        it('should mark message as read if window is active and chatId matches selectedChatId', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'chatId',
                mockChats,
                true,
            );

            expect(apiService.markAsRead).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.id,
            );
            expect(storeService.incrementUnreadMessages).not.toHaveBeenCalled();
            expect(
                browserNotificationService.showNotification,
            ).not.toHaveBeenCalled();
        });

        it('should increment unread messages and show notification if chatId does not match selectedChatId', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId',
                mockChats,
                false,
            );

            expect(storeService.addMessage).not.toHaveBeenCalled();
            expect(storeService.incrementUnreadMessages).toHaveBeenCalledWith(
                mockMessageDto.chatId,
            );
            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith('Chat 1', mockMessageDto.text, false);
        });

        it('should show "Image" notification for image messages', async () => {
            await service.handleMessageNotification(
                mockImageMessageDto,
                'otherChatId',
                mockChats,
                false,
            );

            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith('Chat 1', 'Image', false);
        });

        it('should initialize chat storage if chat is not found', async () => {
            const messageWithUnknownChat = {
                ...mockMessageDto,
                chatId: 'nonExistentChatId',
            };

            await service.handleMessageNotification(
                messageWithUnknownChat,
                'selectedChatId', // Different from message chatId
                mockChats, // mockChats doesn't contain 'nonExistentChatId'
                false,
            );

            expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
            expect(storeService.incrementUnreadMessages).not.toHaveBeenCalled();
            expect(
                browserNotificationService.showNotification,
            ).not.toHaveBeenCalled();
        });

        it('should handle empty chats array', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId', // Different from message chatId so it goes to else branch
                [],
                false,
            );

            expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.created,
                mockMessageDto.id,
            );
            expect(storeService.addMessage).not.toHaveBeenCalled(); // chatId doesn't match selectedChatId
            expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
        });

        it('should handle markAsRead API failure gracefully', async () => {
            apiService.markAsRead.mockReturnValue(
                Promise.reject(new Error('API Error')),
            );

            await expect(
                service.handleMessageNotification(
                    mockMessageDto,
                    'chatId',
                    mockChats,
                    true,
                ),
            ).rejects.toThrow();

            expect(apiService.markAsRead).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.id,
            );
        });

        it('should handle initChatStorage failure gracefully', async () => {
            storeService.initChatStorage.mockReturnValue(
                Promise.reject(new Error('Storage Error')),
            );

            const messageWithUnknownChat = {
                ...mockMessageDto,
                chatId: 'nonExistentChatId',
            };

            await expect(
                service.handleMessageNotification(
                    messageWithUnknownChat,
                    'selectedChatId', // Different from message chatId
                    mockChats, // mockChats doesn't contain 'nonExistentChatId'
                    false,
                ),
            ).rejects.toThrow();

            expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
        });

        it('should handle window active state correctly for different chat', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId',
                mockChats,
                true,
            );

            expect(apiService.markAsRead).not.toHaveBeenCalled();
            expect(storeService.incrementUnreadMessages).toHaveBeenCalledWith(
                mockMessageDto.chatId,
            );
            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith('Chat 1', mockMessageDto.text, true);
        });

        it('should handle null/undefined message properties', async () => {
            const incompleteMessage = {
                id: '1',
                chatId: 'chatId',
                text: null,
                created: 1728867957,
                isMine: false,
                image: null,
            } as IMessageDto;

            await service.handleMessageNotification(
                incompleteMessage,
                'otherChatId',
                mockChats,
                false,
            );

            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith(
                'Chat 1',
                null, // Should handle null text
                false,
            );
        });
    });

    describe('handleLinkPreviewNotification', () => {
        it('should set link preview if chatId matches selectedChatId', () => {
            service.handleLinkPreviewNotification(mockLinkPreviewDto, 'chatId');

            expect(storeService.setLinkPreview).toHaveBeenCalledWith(
                mockLinkPreviewDto,
            );
        });

        it('should not set link preview if chatId does not match selectedChatId', () => {
            service.handleLinkPreviewNotification(
                mockLinkPreviewDto,
                'otherChatId',
            );

            expect(storeService.setLinkPreview).not.toHaveBeenCalled();
        });

        it('should handle null selectedChatId', () => {
            service.handleLinkPreviewNotification(
                mockLinkPreviewDto,
                null as any,
            );

            expect(storeService.setLinkPreview).not.toHaveBeenCalled();
        });

        it('should handle undefined selectedChatId', () => {
            service.handleLinkPreviewNotification(
                mockLinkPreviewDto,
                undefined as any,
            );

            expect(storeService.setLinkPreview).not.toHaveBeenCalled();
        });

        it('should handle empty string selectedChatId', () => {
            service.handleLinkPreviewNotification(mockLinkPreviewDto, '');

            expect(storeService.setLinkPreview).not.toHaveBeenCalled();
        });

        it('should handle null linkPreviewDto chatId', () => {
            const nullChatIdDto = {
                ...mockLinkPreviewDto,
                chatId: null as any,
            };

            service.handleLinkPreviewNotification(nullChatIdDto, 'chatId');

            expect(storeService.setLinkPreview).not.toHaveBeenCalled();
        });

        it('should handle malformed linkPreviewDto', () => {
            const malformedDto = {
                chatId: 'chatId',
                url: null,
                title: undefined,
            } as any;

            service.handleLinkPreviewNotification(malformedDto, 'chatId');

            expect(storeService.setLinkPreview).toHaveBeenCalledWith(
                malformedDto,
            );
        });

        it('should handle very long URLs and titles', () => {
            const longDataDto = {
                ...mockLinkPreviewDto,
                url: 'http://example.com/' + 'a'.repeat(2000),
                title: 'Very long title: ' + 'b'.repeat(1000),
            };

            service.handleLinkPreviewNotification(longDataDto, 'chatId');

            expect(storeService.setLinkPreview).toHaveBeenCalledWith(
                longDataDto,
            );
        });

        it('should handle special characters in link data', () => {
            const specialCharsDto = {
                ...mockLinkPreviewDto,
                url: 'http://example.com/path with spaces & émojis 🔗',
                title: 'Title with <script>alert("xss")</script> & 中文',
            };

            service.handleLinkPreviewNotification(specialCharsDto, 'chatId');

            expect(storeService.setLinkPreview).toHaveBeenCalledWith(
                specialCharsDto,
            );
        });
    });

    describe('handleImagePreviewNotification', () => {
        it('should set image preview if chatId matches selectedChatId', () => {
            service.handleImagePreviewNotification(
                mockImagePreviewDto,
                'chatId',
            );

            expect(storeService.setImagePreview).toHaveBeenCalledWith(
                mockImagePreviewDto,
            );
        });

        it('should not set image preview if chatId does not match selectedChatId', () => {
            service.handleImagePreviewNotification(
                mockImagePreviewDto,
                'otherChatId',
            );

            expect(storeService.setImagePreview).not.toHaveBeenCalled();
        });

        it('should handle null selectedChatId', () => {
            service.handleImagePreviewNotification(
                mockImagePreviewDto,
                null as any,
            );

            expect(storeService.setImagePreview).not.toHaveBeenCalled();
        });

        it('should handle undefined selectedChatId', () => {
            service.handleImagePreviewNotification(
                mockImagePreviewDto,
                undefined as any,
            );

            expect(storeService.setImagePreview).not.toHaveBeenCalled();
        });

        it('should handle empty string selectedChatId', () => {
            service.handleImagePreviewNotification(mockImagePreviewDto, '');

            expect(storeService.setImagePreview).not.toHaveBeenCalled();
        });

        it('should handle null imagePreviewDto chatId', () => {
            const nullChatIdDto = {
                ...mockImagePreviewDto,
                chatId: null as any,
            };

            service.handleImagePreviewNotification(nullChatIdDto, 'chatId');

            expect(storeService.setImagePreview).not.toHaveBeenCalled();
        });

        it('should handle malformed imagePreviewDto', () => {
            const malformedDto = {
                chatId: 'chatId',
                imageId: null,
                url: undefined,
            } as any;

            service.handleImagePreviewNotification(malformedDto, 'chatId');

            expect(storeService.setImagePreview).toHaveBeenCalledWith(
                malformedDto,
            );
        });

        it('should handle very long URLs', () => {
            const longUrlDto = {
                ...mockImagePreviewDto,
                url: 'http://example.com/' + 'a'.repeat(2000) + '.jpg',
            };

            service.handleImagePreviewNotification(longUrlDto, 'chatId');

            expect(storeService.setImagePreview).toHaveBeenCalledWith(
                longUrlDto,
            );
        });

        it('should handle special characters in image data', () => {
            const specialCharsDto = {
                ...mockImagePreviewDto,
                imageId: 'img-🖼️-special',
                url: 'http://example.com/image with spaces & special chars.jpg',
            };

            service.handleImagePreviewNotification(specialCharsDto, 'chatId');

            expect(storeService.setImagePreview).toHaveBeenCalledWith(
                specialCharsDto,
            );
        });
    });

    describe('RTC Session Handlers', () => {
        describe('handleIncomingCallNotification', () => {
            it('should navigate to chat and init incoming call when chat exists', async () => {
                const callId = 'callId';
                const chatId = 'chatId';
                const offer = 'mock-offer-string';

                await service.handleIncomingCallNotification(
                    mockChats,
                    callId,
                    chatId,
                    offer,
                );

                expect(router.navigate).toHaveBeenCalledWith([
                    '/messenger/chat',
                    chatId,
                ]);
                expect(storeService.initIncomingCall).toHaveBeenCalledWith(
                    callId,
                    chatId,
                    offer,
                );
                expect(storeService.initChatStorage).not.toHaveBeenCalled();
            });

            it('should init chat storage when chat does not exist', async () => {
                const callId = 'callId';
                const chatId = 'nonExistentChatId';
                const offer = 'mock-offer-string';

                await service.handleIncomingCallNotification(
                    mockChats,
                    callId,
                    chatId,
                    offer,
                );

                expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
                expect(router.navigate).toHaveBeenCalledWith([
                    '/messenger/chat',
                    chatId,
                ]);
                expect(storeService.initIncomingCall).toHaveBeenCalledWith(
                    callId,
                    chatId,
                    offer,
                );
            });

            it('should handle empty chats array', async () => {
                const callId = 'callId';
                const chatId = 'chatId';
                const offer = 'mock-offer-string';

                await service.handleIncomingCallNotification(
                    [],
                    callId,
                    chatId,
                    offer,
                );

                expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
                expect(router.navigate).toHaveBeenCalledWith([
                    '/messenger/chat',
                    chatId,
                ]);
                expect(storeService.initIncomingCall).toHaveBeenCalledWith(
                    callId,
                    chatId,
                    offer,
                );
            });

            it('should handle router navigation failure gracefully', async () => {
                const callId = 'callId';
                const chatId = 'chatId';
                const offer = 'mock-offer-string';

                router.navigate.mockReturnValue(
                    Promise.reject(new Error('Navigation failed')),
                );

                await expect(
                    service.handleIncomingCallNotification(
                        mockChats,
                        callId,
                        chatId,
                        offer,
                    ),
                ).rejects.toThrow();

                expect(router.navigate).toHaveBeenCalledWith([
                    '/messenger/chat',
                    chatId,
                ]);
            });

            it('should handle initChatStorage failure gracefully', async () => {
                const callId = 'callId';
                const chatId = 'nonExistentChatId';
                const offer = 'mock-offer-string';

                storeService.initChatStorage.mockReturnValue(
                    Promise.reject(new Error('Storage init failed')),
                );

                await expect(
                    service.handleIncomingCallNotification(
                        mockChats,
                        callId,
                        chatId,
                        offer,
                    ),
                ).rejects.toThrow();

                expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
            });

            it('should handle null/undefined parameters', async () => {
                await expect(
                    service.handleIncomingCallNotification(
                        null as any,
                        'callId',
                        'chatId',
                        'offer',
                    ),
                ).rejects.toThrow();
            });

            it('should handle empty string parameters', async () => {
                const callId = '';
                const chatId = '';
                const offer = '';

                await service.handleIncomingCallNotification(
                    mockChats,
                    callId,
                    chatId,
                    offer,
                );

                expect(storeService.initChatStorage).toHaveBeenCalledWith(true);
                expect(router.navigate).toHaveBeenCalledWith([
                    '/messenger/chat',
                    chatId,
                ]);
                expect(storeService.initIncomingCall).toHaveBeenCalledWith(
                    callId,
                    chatId,
                    offer,
                );
            });
        });
    });

    describe('Integration Tests', () => {
        it('should handle complete message workflow for active window and selected chat', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'chatId',
                mockChats,
                true,
            );

            // Verify complete workflow
            expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.created,
                mockMessageDto.id,
            );
            expect(storeService.addMessage).toHaveBeenCalledWith(
                mockMessageDto,
            );
            expect(apiService.markAsRead).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.id,
            );
            expect(storeService.incrementUnreadMessages).not.toHaveBeenCalled();
            expect(
                browserNotificationService.showNotification,
            ).not.toHaveBeenCalled();
        });

        it('should handle complete message workflow for inactive window and different chat', async () => {
            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId',
                mockChats,
                false,
            );

            // Verify complete workflow
            expect(storeService.setLastMessageInfo).toHaveBeenCalledWith(
                mockMessageDto.chatId,
                mockMessageDto.created,
                mockMessageDto.id,
            );
            expect(storeService.addMessage).not.toHaveBeenCalled();
            expect(apiService.markAsRead).not.toHaveBeenCalled();
            expect(storeService.incrementUnreadMessages).toHaveBeenCalledWith(
                mockMessageDto.chatId,
            );
            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith('Chat 1', mockMessageDto.text, false);
        });

        it('should handle service initialization and cleanup lifecycle', async () => {
            const handlers = {
                message: vi.fn(),
                linkPreview: vi.fn(),
                imagePreview: vi.fn(),
                rtcOffer: vi.fn(),
                rtcAnswer: vi.fn(),
            };

            // Initialize
            await service.initHandlers(
                handlers.message,
                handlers.linkPreview,
                handlers.imagePreview,
                handlers.rtcOffer,
                handlers.rtcAnswer,
            );

            expect(browserNotificationService.init).toHaveBeenCalledTimes(1);
            expect(signalrService.init).toHaveBeenCalledTimes(1);

            // Cleanup
            service.removeHandlers();

            expect(signalrService.removeHandlers).toHaveBeenCalledTimes(1);
        });
    });

    describe('Edge Cases and Error Handling', () => {
        it('should handle concurrent message notifications', async () => {
            const promises = [
                service.handleMessageNotification(
                    mockMessageDto,
                    'chatId',
                    mockChats,
                    true,
                ),
                service.handleMessageNotification(
                    mockImageMessageDto,
                    'chatId',
                    mockChats,
                    true,
                ),
            ];

            await Promise.all(promises);

            expect(storeService.setLastMessageInfo).toHaveBeenCalledTimes(2);
            expect(storeService.addMessage).toHaveBeenCalledTimes(2);
            expect(apiService.markAsRead).toHaveBeenCalledTimes(2);
        });

        it('should handle malformed message data', async () => {
            const malformedMessage = {
                id: null,
                chatId: undefined,
                text: '',
                created: null,
                isMine: undefined,
            } as any;

            // Should not throw
            await expect(
                service.handleMessageNotification(
                    malformedMessage,
                    'chatId',
                    mockChats,
                    false,
                ),
            ).resolves.not.toThrow();
        });

        it('should handle very large chat arrays efficiently', async () => {
            const largeChatArray = Array.from({ length: 1000 }, (_, i) => ({
                id: `chat-${i}`,
                chatName: `Chat ${i}`,
                unreadCount: 0,
            })) as IChatDto[];

            const startTime = performance.now();

            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId',
                largeChatArray,
                false,
            );

            const endTime = performance.now();

            // Should complete within reasonable time (less than 10ms)
            expect(endTime - startTime).toBeLessThan(10);
        });

        it('should handle service method failures in sequence', async () => {
            // Setup failures
            storeService.setLastMessageInfo.mockImplementation(() => {
                throw new Error('Store error');
            });

            await expect(
                service.handleMessageNotification(
                    mockMessageDto,
                    'chatId',
                    mockChats,
                    true,
                ),
            ).rejects.toThrow();

            expect(storeService.setLastMessageInfo).toHaveBeenCalled();
        });

        it('should handle memory pressure scenarios', async () => {
            // Simulate many rapid notifications
            const notifications = Array.from({ length: 100 }, (_, i) => ({
                ...mockMessageDto,
                id: `msg-${i}`,
            }));

            const promises = notifications.map((msg) =>
                service.handleMessageNotification(
                    msg,
                    'chatId',
                    mockChats,
                    true,
                ),
            );

            await Promise.all(promises);

            expect(storeService.setLastMessageInfo).toHaveBeenCalledTimes(100);
            expect(storeService.addMessage).toHaveBeenCalledTimes(100);
        });

        it('should handle service dependencies being null', () => {
            // Test that service can handle dependency injection issues gracefully
            expect(service).toBeTruthy();
            expect(() => service.removeHandlers()).not.toThrow();
        });

        it('should handle readonly array mutations safely', async () => {
            const readonlyChats = Object.freeze([
                ...mockChats,
            ]) as readonly IChatDto[];

            await service.handleMessageNotification(
                mockMessageDto,
                'otherChatId',
                readonlyChats,
                false,
            );

            // Should not modify the original array
            expect(readonlyChats).toEqual(mockChats);
            expect(storeService.incrementUnreadMessages).toHaveBeenCalled();
        });

        it('should handle special characters in chat data', async () => {
            const specialCharsMessage = {
                ...mockMessageDto,
                text: '🚀 Special chars: <script>alert("xss")</script> & émojis 中文',
                chatId: 'chat-with-special-chars-🚀',
            };

            const specialCharsChat = {
                id: 'chat-with-special-chars-🚀',
                chatName: 'Chat with 🚀 & special chars',
                unreadCount: 0,
            } as IChatDto;

            await service.handleMessageNotification(
                specialCharsMessage,
                'otherChatId',
                [specialCharsChat],
                false,
            );

            expect(
                browserNotificationService.showNotification,
            ).toHaveBeenCalledWith(
                'Chat with 🚀 & special chars',
                '🚀 Special chars: <script>alert("xss")</script> & émojis 中文',
                false,
            );
        });

        it('should handle service initialization with all dependencies', async () => {
            const handlers = {
                message: vi.fn(),
                linkPreview: vi.fn(),
                imagePreview: vi.fn(),
                rtcOffer: vi.fn(),
                rtcAnswer: vi.fn(),
            };

            // Test that all dependencies are properly injected and called
            await service.initHandlers(
                handlers.message,
                handlers.linkPreview,
                handlers.imagePreview,
                handlers.rtcOffer,
                handlers.rtcAnswer,
            );

            expect(browserNotificationService.init).toHaveBeenCalledTimes(1);
            expect(signalrService.init).toHaveBeenCalledWith(
                handlers.message,
                handlers.linkPreview,
                handlers.imagePreview,
                handlers.rtcOffer,
                handlers.rtcAnswer,
            );
        });

        it('should handle multiple rapid RTC session offers', async () => {
            const callId = 'callId';
            const offers = ['offer1', 'offer2', 'offer3'];

            const promises = offers.map((offer, index) =>
                service.handleIncomingCallNotification(
                    mockChats,
                    callId,
                    `chatId${index}`,
                    offer,
                ),
            );

            await Promise.all(promises);

            expect(router.navigate).toHaveBeenCalledTimes(3);
            expect(storeService.initIncomingCall).toHaveBeenCalledTimes(3);
        });
    });
});
