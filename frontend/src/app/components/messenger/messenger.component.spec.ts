import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessengerComponent } from './messenger.component';
import { SignalrHandlerService } from '../../services/signalr-handler.service';
import { StoreService } from '../../services/store.service';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import {
    IChatDto,
    IMessageDto,
    ILinkPreviewDto,
    IImagePreviewDto,
} from '../../api-client/api-client';
import { selectChats } from '../../state/chats/chats.selector';
import { StubChatComponent } from '../chat/chat.component.stub';
import { StubImageViewerComponent } from '../image-viewer/image-viewer.component.stub';
import { StubSidebarComponent } from '../sidebar/sidebar.component.stub';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { RouterTestingModule } from '@angular/router/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { RtcConnectionService } from '../../services/rtc-connection.service';
import { IncomingCall } from '../../models/incoming-call';
import { EstablishConnection } from '../../models/establish-connection';

describe('MessengerComponent', () => {
    let component: MessengerComponent;
    let fixture: ComponentFixture<MessengerComponent>;
    let signalrHandlerService: MockedObject<SignalrHandlerService>;
    let storeService: MockedObject<StoreService>;
    let store: MockedObject<Store>;
    let rtcConnectionService: MockedObject<RtcConnectionService>;

    beforeEach(async () => {
        signalrHandlerService = {
            initHandlers: vi
                .fn()
                .mockName('SignalrHandlerService.initHandlers'),
            removeHandlers: vi
                .fn()
                .mockName('SignalrHandlerService.removeHandlers'),
            handleMessageNotification: vi
                .fn()
                .mockName('SignalrHandlerService.handleMessageNotification'),
            handleLinkPreviewNotification: vi
                .fn()
                .mockName(
                    'SignalrHandlerService.handleLinkPreviewNotification',
                ),
            handleImagePreviewNotification: vi
                .fn()
                .mockName(
                    'SignalrHandlerService.handleImagePreviewNotification',
                ),
            handleIncomingCallNotification: vi
                .fn()
                .mockName(
                    'SignalrHandlerService.handleIncomingCallNotification',
                ),
        } as MockedObject<SignalrHandlerService>;

        storeService = {
            markAllAsRead: vi.fn().mockName('StoreService.markAllAsRead'),
            initChatStorage: vi.fn().mockName('StoreService.initChatStorage'),
            setSelectedChatId: vi
                .fn()
                .mockName('StoreService.setSelectedChatId'),
        } as MockedObject<StoreService>;

        store = {
            select: vi.fn().mockImplementation((selector) => {
                if (selector === selectChats) {
                    return of([]);
                }
                return of(null);
            }),
        } as MockedObject<Store>;

        rtcConnectionService = {
            establishConnection: vi
                .fn()
                .mockName('RtcConnectionService.establishConnection'),
        } as MockedObject<RtcConnectionService>;

        await TestBed.configureTestingModule({
            declarations: [
                MessengerComponent,
                StubImageViewerComponent,
                StubSidebarComponent,
                StubChatComponent,
            ],
            imports: [CommonModule, RouterTestingModule],
            providers: [
                {
                    provide: SignalrHandlerService,
                    useValue: signalrHandlerService,
                },
                { provide: StoreService, useValue: storeService },
                { provide: Store, useValue: store },
                {
                    provide: ActivatedRoute,
                    useValue: { firstChild: null, params: of({}) },
                },
                {
                    provide: RtcConnectionService,
                    useValue: rtcConnectionService,
                },
            ],
            schemas: [NO_ERRORS_SCHEMA],
        }).compileComponents();

        fixture = TestBed.createComponent(MessengerComponent);
        component = fixture.componentInstance;
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize chat storage and SignalR handlers on ngOnInit', async () => {
        // Arrange

        // Act
        await component.ngOnInit();

        // Assert
        expect(storeService.initChatStorage).toHaveBeenCalledTimes(1);
        expect(signalrHandlerService.initHandlers).toHaveBeenCalledTimes(1);
    });

    it('should handle visibility change event', () => {
        // Arrange
        component['selectedChat'] = { id: 'chatId' } as IChatDto;

        // Act
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        component.onVisibilityChange({ target: document } as any);

        // Assert
        expect(storeService.markAllAsRead).toHaveBeenCalledTimes(1);

        // Assert
        expect(storeService.markAllAsRead).toHaveBeenCalledWith(
            component['selectedChat'],
        );
    });

    it('should call removeHandlers on ngOnDestroy', () => {
        // Arrange

        // Act
        component.ngOnDestroy();

        // Assert
        expect(signalrHandlerService.removeHandlers).toHaveBeenCalledTimes(1);
    });

    it('should handle message notification', () => {
        // Arrange
        const messageDto = { chatId: 'chatId', isMine: false } as IMessageDto;

        // Act
        component.handleMessageNotification(messageDto);

        // Assert
        expect(
            signalrHandlerService.handleMessageNotification,
        ).toHaveBeenCalledTimes(1);

        // Assert
        expect(
            signalrHandlerService.handleMessageNotification,
        ).toHaveBeenCalledWith(messageDto, undefined, [], true);
    });

    it('should handle link preview notification', () => {
        // Arrange
        const linkPreviewDto = { chatId: 'chatId' } as ILinkPreviewDto;

        // Act
        component.handleLinkPreviewNotification(linkPreviewDto);

        // Assert
        expect(
            signalrHandlerService.handleLinkPreviewNotification,
        ).toHaveBeenCalledTimes(1);

        // Assert
        expect(
            signalrHandlerService.handleLinkPreviewNotification,
        ).toHaveBeenCalledWith(linkPreviewDto, undefined);
    });

    it('should handle image preview notification', () => {
        // Arrange
        const imagePreviewDto = { chatId: 'chatId' } as IImagePreviewDto;

        // Act
        component.handleImagePreviewNotification(imagePreviewDto);

        // Assert
        expect(
            signalrHandlerService.handleImagePreviewNotification,
        ).toHaveBeenCalledTimes(1);

        // Assert
        expect(
            signalrHandlerService.handleImagePreviewNotification,
        ).toHaveBeenCalledWith(imagePreviewDto, undefined);
    });

    it('should handle incoming call notification', async () => {
        // Arrange
        const incomingCall: IncomingCall = {
            callId: 'call-1',
            chatId: 'chat-1',
            offer: 'sdp-offer',
        };
        component['chats'] = [{ id: 'chat-1' } as IChatDto];

        // Act
        await component.handleIncomingCallNotification(incomingCall);

        // Assert
        expect(
            signalrHandlerService.handleIncomingCallNotification,
        ).toHaveBeenCalledTimes(1);
        expect(
            signalrHandlerService.handleIncomingCallNotification,
        ).toHaveBeenCalledWith(
            component['chats'],
            incomingCall.callId,
            incomingCall.chatId,
            incomingCall.offer,
        );
    });

    it('should handle establish connection notification', () => {
        // Arrange
        const establishConnection: EstablishConnection = { answer: 'sdp-answer' };

        // Act
        component.handleEstablishConnectionNotification(establishConnection);

        // Assert
        expect(rtcConnectionService.establishConnection).toHaveBeenCalledTimes(1);
        expect(rtcConnectionService.establishConnection).toHaveBeenCalledWith(
            establishConnection.answer,
        );
    });

    it('should show sidebar when no chat id in route params', async () => {
        // Act
        await component.ngOnInit();

        // Assert
        expect(component.isSidebarShown()).toBe(true);
    });

    it('should unsubscribe on ngOnDestroy', () => {
        // Arrange
        const nextSpy = vi.spyOn(component['unsubscribe$'], 'next');
        const completeSpy = vi.spyOn(component['unsubscribe$'], 'complete');

        // Act
        component.ngOnDestroy();

        // Assert
        expect(nextSpy).toHaveBeenCalledTimes(1);
        expect(completeSpy).toHaveBeenCalledTimes(1);
    });
});
