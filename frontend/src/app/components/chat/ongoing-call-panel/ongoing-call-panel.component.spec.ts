import {
    afterEach,
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subject, of } from 'rxjs';
import { OngoingCallComponent } from './ongoing-call-panel.component';
import { RtcConnectionService } from '../../../services/rtc-connection.service';
import { RtcPeerConnectionManager } from '../../../services/rtc-peer-connection-manager';
import { StoreService } from '../../../services/store.service';
import { VideoCall } from '../../../models/video-call';
import {
    selectCaptureAudio,
    selectCaptureVideo,
    selectVideoCall,
    selectVideoCallChat,
} from '../../../state/video-call/video-call.selectors';
import {
    selectIsAwaitingResponseButtonVisible,
    selectIsOngoingCallControlSetVisible,
} from '../../../state/selected-chat/selected-chat.selector';

describe('OngoingCallComponent', () => {
    let component: OngoingCallComponent;
    let fixture: ComponentFixture<OngoingCallComponent>;
    let mockStore: MockedObject<Store>;
    let mockRouter: MockedObject<Router>;
    let mockStoreService: MockedObject<StoreService>;
    let mockRtcConnectionService: MockedObject<RtcConnectionService>;
    let mockConnectionManager: MockedObject<RtcPeerConnectionManager>;
    let videoCallSubject: Subject<VideoCall | null>;

    const mockChat = { id: 'chat-123', chatName: 'Test Chat' };

    const mockVideoCall: VideoCall = {
        callId: 'call-1',
        chatId: 'chat-123',
        status: 'outgoing',
        captureVideo: true,
        captureAudio: true,
        isMinimized: true,
    };

    beforeEach(async () => {
        videoCallSubject = new Subject<VideoCall | null>();

        const storeSpy = {
            select: vi.fn().mockImplementation((selector: unknown) => {
                if (selector === selectVideoCall)
                    return videoCallSubject.asObservable();
                if (selector === selectVideoCallChat) return of(mockChat);
                if (selector === selectCaptureVideo) return of(true);
                if (selector === selectCaptureAudio) return of(true);
                if (selector === selectIsAwaitingResponseButtonVisible)
                    return of(false);
                if (selector === selectIsOngoingCallControlSetVisible)
                    return of(true);
                return of(null);
            }),
        };

        const routerSpy = {
            navigate: vi.fn().mockResolvedValue(true),
        };

        const storeServiceSpy = {
            maximizeCall: vi.fn(),
            acceptIncomingCall: vi.fn(),
            resetCall: vi.fn(),
            toggleCaptureVideo: vi.fn(),
            toggleCaptureAudio: vi.fn(),
        };

        const rtcConnectionServiceSpy = {
            endCall: vi.fn(),
        };

        const connectionManagerSpy = {
            reconnectVideoElements: vi.fn(),
            setVideoEnabled: vi.fn(),
            setAudioEnabled: vi.fn(),
        };
        Object.defineProperty(connectionManagerSpy, 'isMediaCaptured', {
            get: vi.fn().mockReturnValue(false),
            configurable: true,
        });

        await TestBed.configureTestingModule({
            declarations: [OngoingCallComponent],
            providers: [
                { provide: Store, useValue: storeSpy },
                { provide: Router, useValue: routerSpy },
                { provide: StoreService, useValue: storeServiceSpy },
                {
                    provide: RtcConnectionService,
                    useValue: rtcConnectionServiceSpy,
                },
                {
                    provide: RtcPeerConnectionManager,
                    useValue: connectionManagerSpy,
                },
            ],
            schemas: [CUSTOM_ELEMENTS_SCHEMA],
        }).compileComponents();

        fixture = TestBed.createComponent(OngoingCallComponent);
        component = fixture.componentInstance;

        mockStore = TestBed.inject(Store) as MockedObject<Store>;
        mockRouter = TestBed.inject(Router) as MockedObject<Router>;
        mockStoreService = TestBed.inject(
            StoreService,
        ) as MockedObject<StoreService>;
        mockRtcConnectionService = TestBed.inject(
            RtcConnectionService,
        ) as MockedObject<RtcConnectionService>;
        mockConnectionManager = TestBed.inject(
            RtcPeerConnectionManager,
        ) as MockedObject<RtcPeerConnectionManager>;

        // Provide a mock remoteVideo ViewChild
        const mockRemoteVideo = document.createElement('video');
        component.remoteVideo = { nativeElement: mockRemoteVideo } as any;
    });

    afterEach(() => {
        videoCallSubject.complete();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    describe('signals', () => {
        it('should expose chat signal from store', () => {
            expect(component.chat()).toEqual(mockChat);
        });

        it('should expose captureVideo signal from store', () => {
            expect(component.captureVideo()).toBe(true);
        });

        it('should expose captureAudio signal from store', () => {
            expect(component.captureAudio()).toBe(true);
        });

        it('should expose isAwaitingResponseButtonVisible signal from store', () => {
            expect(component.isAwaitingResponseButtonVisible()).toBe(false);
        });

        it('should expose isCallControlSetVisible signal from store', () => {
            expect(component.isCallControlSetVisible()).toBe(true);
        });
    });

    describe('ngAfterViewInit', () => {
        it('should subscribe to selectVideoCall', () => {
            component.ngAfterViewInit();
            expect(mockStore.select).toHaveBeenCalled();
        });

        it('should reconnect video elements when media is already captured and videoCall emits', () => {
            Object.defineProperty(mockConnectionManager, 'isMediaCaptured', {
                get: () => true,
                configurable: true,
            });

            component.ngAfterViewInit();
            videoCallSubject.next(mockVideoCall);

            expect(
                mockConnectionManager.reconnectVideoElements,
            ).toHaveBeenCalledWith({
                remote: component.remoteVideo.nativeElement,
            });
        });

        it('should not reconnect video elements when media is not captured', () => {
            Object.defineProperty(mockConnectionManager, 'isMediaCaptured', {
                get: () => false,
                configurable: true,
            });

            component.ngAfterViewInit();
            videoCallSubject.next(mockVideoCall);

            expect(
                mockConnectionManager.reconnectVideoElements,
            ).not.toHaveBeenCalled();
        });

        it('should set video and audio enabled from videoCall state', () => {
            component.ngAfterViewInit();
            videoCallSubject.next(mockVideoCall);

            expect(mockConnectionManager.setVideoEnabled).toHaveBeenCalledWith(
                true,
            );
            expect(mockConnectionManager.setAudioEnabled).toHaveBeenCalledWith(
                true,
            );
        });

        it('should set video and audio enabled with false values', () => {
            const callWithDisabled: VideoCall = {
                ...mockVideoCall,
                captureVideo: false,
                captureAudio: false,
            };

            component.ngAfterViewInit();
            videoCallSubject.next(callWithDisabled);

            expect(mockConnectionManager.setVideoEnabled).toHaveBeenCalledWith(
                false,
            );
            expect(mockConnectionManager.setAudioEnabled).toHaveBeenCalledWith(
                false,
            );
        });

        it('should not react when videoCall is null (filtered out)', () => {
            component.ngAfterViewInit();
            videoCallSubject.next(null);

            expect(
                mockConnectionManager.setVideoEnabled,
            ).not.toHaveBeenCalled();
            expect(
                mockConnectionManager.setAudioEnabled,
            ).not.toHaveBeenCalled();
        });

        it('should stop reacting after ngOnDestroy', () => {
            component.ngAfterViewInit();
            component.ngOnDestroy();
            videoCallSubject.next(mockVideoCall);

            expect(
                mockConnectionManager.setVideoEnabled,
            ).not.toHaveBeenCalled();
        });
    });

    describe('ngOnDestroy', () => {
        it('should set remoteVideo srcObject to null', () => {
            component.remoteVideo.nativeElement.srcObject = {} as MediaStream;
            component.ngOnDestroy();
            expect(component.remoteVideo.nativeElement.srcObject).toBeNull();
        });

        it('should complete the unsubscribe subject', () => {
            const unsubscribe$ = component['unsubscribe$'];
            vi.spyOn(unsubscribe$, 'next');
            vi.spyOn(unsubscribe$, 'complete');

            component.ngOnDestroy();

            expect(unsubscribe$.next).toHaveBeenCalled();
            expect(unsubscribe$.complete).toHaveBeenCalled();
        });
    });

    describe('onPanelClick', () => {
        it('should call storeService.maximizeCall', async () => {
            await component.onPanelClick();
            expect(mockStoreService.maximizeCall).toHaveBeenCalled();
        });

        it('should navigate to chat when chat is available', async () => {
            await component.onPanelClick();
            expect(mockRouter.navigate).toHaveBeenCalledWith([
                '/messenger/chat',
                mockChat.id,
            ]);
        });

        it('should not navigate when chat is null', async () => {
            mockStore.select.mockImplementation((selector: unknown) => {
                if (selector === selectVideoCallChat) return of(null);
                return of(null);
            });
            // Re-create component with null chat
            fixture = TestBed.createComponent(OngoingCallComponent);
            component = fixture.componentInstance;
            component.remoteVideo = {
                nativeElement: document.createElement('video'),
            } as any;

            await component.onPanelClick();

            expect(mockRouter.navigate).not.toHaveBeenCalled();
        });
    });

    describe('onAcceptClicked', () => {
        it('should stop event propagation', async () => {
            const event = new MouseEvent('click');
            vi.spyOn(event, 'stopPropagation');

            await component.onAcceptClicked(event);

            expect(event.stopPropagation).toHaveBeenCalled();
        });

        it('should call storeService.acceptIncomingCall', async () => {
            const event = new MouseEvent('click');
            await component.onAcceptClicked(event);
            expect(mockStoreService.acceptIncomingCall).toHaveBeenCalled();
        });

        it('should navigate to chat', async () => {
            const event = new MouseEvent('click');
            await component.onAcceptClicked(event);
            expect(mockRouter.navigate).toHaveBeenCalledWith([
                '/messenger/chat',
                mockChat.id,
            ]);
        });
    });

    describe('onDeclineClicked', () => {
        it('should stop event propagation', () => {
            const event = new MouseEvent('click');
            vi.spyOn(event, 'stopPropagation');

            component.onDeclineClicked(event);

            expect(event.stopPropagation).toHaveBeenCalled();
        });

        it('should call storeService.resetCall', () => {
            const event = new MouseEvent('click');
            component.onDeclineClicked(event);
            expect(mockStoreService.resetCall).toHaveBeenCalled();
        });
    });

    describe('toggleCaptureVideo', () => {
        it('should stop event propagation', () => {
            const event = new MouseEvent('click');
            vi.spyOn(event, 'stopPropagation');

            component.toggleCaptureVideo(event);

            expect(event.stopPropagation).toHaveBeenCalled();
        });

        it('should call storeService.toggleCaptureVideo', () => {
            const event = new MouseEvent('click');
            component.toggleCaptureVideo(event);
            expect(mockStoreService.toggleCaptureVideo).toHaveBeenCalled();
        });
    });

    describe('toggleCaptureAudio', () => {
        it('should stop event propagation', () => {
            const event = new MouseEvent('click');
            vi.spyOn(event, 'stopPropagation');

            component.toggleCaptureAudio(event);

            expect(event.stopPropagation).toHaveBeenCalled();
        });

        it('should call storeService.toggleCaptureAudio', () => {
            const event = new MouseEvent('click');
            component.toggleCaptureAudio(event);
            expect(mockStoreService.toggleCaptureAudio).toHaveBeenCalled();
        });
    });

    describe('endCall', () => {
        it('should stop event propagation', () => {
            const event = new MouseEvent('click');
            vi.spyOn(event, 'stopPropagation');

            component.endCall(event);

            expect(event.stopPropagation).toHaveBeenCalled();
        });

        it('should call rtcConnectionService.endCall', () => {
            const event = new MouseEvent('click');
            component.endCall(event);
            expect(mockRtcConnectionService.endCall).toHaveBeenCalled();
        });
    });
});
