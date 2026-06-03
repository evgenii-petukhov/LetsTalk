import {
    AfterViewInit,
    Component,
    ElementRef,
    HostListener,
    inject,
    OnDestroy,
    ViewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { faPhoneVolume } from '@fortawesome/free-solid-svg-icons';
import { Store } from '@ngrx/store';
import { filter, Subject, takeUntil } from 'rxjs';
import { RtcConnectionService } from '../../../services/rtc-connection.service';
import { RtcPeerConnectionManager } from '../../../services/rtc-peer-connection-manager';
import { StoreService } from '../../../services/store.service';
import {
    selectIsAwaitingResponseButtonVisible,
    selectIsOngoingCallControlSetVisible,
} from '../../../state/selected-chat/selected-chat.selector';
import {
    selectCaptureAudio,
    selectCaptureVideo,
    selectVideoCall,
    selectVideoCallChat,
} from '../../../state/video-call/video-call.selectors';

@Component({
    selector: 'app-ongoing-call-panel',
    templateUrl: './ongoing-call-panel.component.html',
    styleUrl: './ongoing-call-panel.component.scss',
    standalone: false,
})
export class OngoingCallComponent implements OnDestroy, AfterViewInit {
    private readonly router = inject(Router);
    private readonly store = inject(Store);
    private readonly storeService = inject(StoreService);
    private readonly rtcConnectionService = inject(RtcConnectionService);
    private readonly connectionManager = inject(RtcPeerConnectionManager);
    private readonly unsubscribe$: Subject<void> = new Subject<void>();

    chat = toSignal(this.store.select(selectVideoCallChat));
    isAwaitingResponseButtonVisible = toSignal(
        this.store.select(selectIsAwaitingResponseButtonVisible),
    );
    isCallControlSetVisible = toSignal(
        this.store.select(selectIsOngoingCallControlSetVisible),
    );
    captureVideo = toSignal(this.store.select(selectCaptureVideo));
    captureAudio = toSignal(this.store.select(selectCaptureAudio));

    faPhoneVolume = faPhoneVolume;

    @ViewChild('remoteVideo', { static: false })
    remoteVideo!: ElementRef<HTMLVideoElement>;

    ngAfterViewInit(): void {
        this.store
            .select(selectVideoCall)
            .pipe(
                filter((videoCall) => !!videoCall),
                takeUntil(this.unsubscribe$),
            )
            .subscribe((currentState) => {
                if (this.connectionManager.isMediaCaptured) {
                    this.connectionManager.reconnectVideoElements({
                        remote: this.remoteVideo.nativeElement,
                    });
                }

                this.connectionManager.setVideoEnabled(
                    currentState.captureVideo,
                );
                this.connectionManager.setAudioEnabled(
                    currentState.captureAudio,
                );
            });
    }

    ngOnDestroy(): void {
        this.disconnectVideoElements();

        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    @HostListener('click')
    async onPanelClick(): Promise<void> {
        this.storeService.maximizeCall();
        this.navigateToChat();
    }

    async onAcceptClicked(e: MouseEvent): Promise<void> {
        e.stopPropagation();
        this.storeService.acceptIncomingCall();
        await this.navigateToChat();
    }

    onDeclineClicked(e: MouseEvent): void {
        e.stopPropagation();
        this.storeService.resetCall();
    }

    toggleCaptureVideo(e: MouseEvent) {
        e.stopPropagation();
        this.storeService.toggleCaptureVideo();
    }

    toggleCaptureAudio(e: MouseEvent) {
        e.stopPropagation();
        this.storeService.toggleCaptureAudio();
    }

    endCall(e: MouseEvent): void {
        e.stopPropagation();
        this.rtcConnectionService.endCall();
    }

    private async navigateToChat(): Promise<void> {
        const chat = this.chat();
        if (chat) {
            await this.router.navigate(['/messenger/chat', chat.id]);
        }
    }

    private disconnectVideoElements(): void {
        this.remoteVideo.nativeElement.srcObject = null;
    }
}
