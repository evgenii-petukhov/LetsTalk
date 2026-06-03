import {
    AfterViewInit,
    Component,
    ElementRef,
    inject,
    OnDestroy,
    ViewChild,
} from '@angular/core';
import { Store } from '@ngrx/store';
import { pairwise, startWith, Subject, takeUntil } from 'rxjs';
import { RtcConnectionService } from '../../../services/rtc-connection.service';
import { RtcPeerConnectionManager } from '../../../services/rtc-peer-connection-manager';
import { StoreService } from '../../../services/store.service';
import {
    selectCaptureAudio,
    selectCaptureVideo,
    selectFacingMode,
    selectVideoCall,
} from '../../../state/video-call/video-call.selectors';
import { toSignal } from '@angular/core/rxjs-interop';
import { VideoCall } from 'src/app/models/video-call';

@Component({
    selector: 'app-video-call',
    templateUrl: './video-call.component.html',
    styleUrl: './video-call.component.scss',
    standalone: false,
})
export class VideoCallComponent implements OnDestroy, AfterViewInit {
    @ViewChild('localVideo', { static: false })
    localVideo!: ElementRef<HTMLVideoElement>;
    @ViewChild('remoteVideo', { static: false })
    remoteVideo!: ElementRef<HTMLVideoElement>;

    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly store = inject(Store);
    private readonly rtcConnectionService = inject(RtcConnectionService);
    private readonly connectionManager = inject(RtcPeerConnectionManager);
    private readonly storeService = inject(StoreService);
    private facingMode = toSignal<VideoCall['facingMode']>(
        this.store.select(selectFacingMode),
    );

    captureVideo = toSignal(this.store.select(selectCaptureVideo));
    captureAudio = toSignal(this.store.select(selectCaptureAudio));

    ngAfterViewInit(): void {
        this.store
            .select(selectVideoCall)
            .pipe(startWith(null), pairwise(), takeUntil(this.unsubscribe$))
            .subscribe(async ([prevState, currentState]) => {
                if (currentState === null) {
                    if (prevState !== null) {
                        this.disconnectVideoElements();
                    }
                    return;
                }

                if (prevState === null) {
                    if (this.connectionManager.isMediaCaptured) {
                        this.connectionManager.reconnectVideoElements({
                            local: this.localVideo.nativeElement,
                            remote: this.remoteVideo.nativeElement,
                        });
                    } else {
                        try {
                            await this.connectionManager.startMediaCapture(
                                this.localVideo.nativeElement,
                                this.remoteVideo.nativeElement,
                                currentState.facingMode,
                            );
                            if (currentState.status === 'incoming-active') {
                                await this.rtcConnectionService.handleIncomingCall(
                                    currentState.callId,
                                    currentState.chatId,
                                    currentState.offer,
                                );
                            } else if (currentState.status === 'outgoing') {
                                await this.rtcConnectionService.startOutgoingCall(
                                    currentState.chatId,
                                );
                            }
                        } catch (error) {
                            console.error('Video call error:', error);
                        }
                    }
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

    endCall(): void {
        this.rtcConnectionService.endCall();
    }

    toggleCaptureVideo(): void {
        this.storeService.toggleCaptureVideo();
    }

    toggleCaptureAudio(): void {
        this.storeService.toggleCaptureAudio();
    }

    minimizeCall(): void {
        this.storeService.minimizeCall();
    }

    switchCamera(): Promise<void> {
        this.storeService.toggleFacingMode();
        return this.connectionManager.switchCamera(
            this.localVideo.nativeElement,
            this.remoteVideo.nativeElement,
            this.facingMode(),
        );
    }

    private disconnectVideoElements(): void {
        this.localVideo.nativeElement.srcObject = null;
        this.remoteVideo.nativeElement.srcObject = null;
    }
}
