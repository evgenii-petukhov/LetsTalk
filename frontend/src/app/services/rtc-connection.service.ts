import { inject, Injectable } from '@angular/core';
import { firstValueFrom, lastValueFrom, Subject, takeUntil } from 'rxjs';
import { ApiService } from './api.service';
import { Timer } from '../utils/timer';
import { RtcPeerConnectionManager } from './rtc-peer-connection-manager';
import { StoreService } from './store.service';
import { Store } from '@ngrx/store';
import { selectVideoCall } from '../state/video-call/video-call.selectors';
import { RtcErrorLoggingService } from './rtc-error-logging.service';

@Injectable({
    providedIn: 'root',
})
export class RtcConnectionService {
    private readonly apiService = inject(ApiService);
    private readonly connectionManager = inject(RtcPeerConnectionManager);
    private readonly storeService = inject(StoreService);
    private readonly store = inject(Store);
    private readonly errorLoggingService = inject(RtcErrorLoggingService);
    private iceCandidateSubject = new Subject<string>();
    private iceGatheringComplete = new Subject<void>();
    private iceGatheringTimer: Timer;
    private iceGatheringTimeoutMs = 10000;
    private iceGatheringElapsedMs = 0;
    private iceGatheringCollectedAll = false;

    constructor() {
        this.connectionManager.onCandidatesReceived =
            this.onIceCandidateGenerated.bind(this);
        this.connectionManager.onGatheringCompleted =
            this.onIceGatheringComplete.bind(this);
        this.connectionManager.onConnected = this.onConnected.bind(this);
        this.connectionManager.onConnectionError =
            this.onConnectionError.bind(this);
        this.connectionManager.onIceServerError =
            this.onIceServerError.bind(this);
        this.connectionManager.onDisconnected = this.onDisconnected.bind(this);
    }

    async startOutgoingCall(chatId: string): Promise<void> {
        try {
            const callSettings = await this.apiService.getCallSettings();
            this.connectionManager.initiateOffer(
                JSON.parse(callSettings.iceServerConfiguration),
            );

            this.iceGatheringTimer = new Timer(() => {
                this.connectionManager.requestCompleteGathering();
            }, this.iceGatheringTimeoutMs);

            const finalOffer = await lastValueFrom(
                this.iceCandidateSubject.pipe(
                    takeUntil(this.iceGatheringComplete),
                ),
            );

            const diagnostics = await this.connectionManager.getDiagnostics();

            const { callId } = await this.apiService.startOutgoingCall(
                chatId,
                finalOffer,
                this.iceGatheringElapsedMs,
                this.iceGatheringCollectedAll,
                diagnostics,
            );

            this.storeService.setCallId(callId);
        } catch (error) {
            await this.errorLoggingService.logConnectionError(undefined, error);
            throw error;
        }
    }

    async handleIncomingCall(
        callId: string,
        chatId: string,
        offer: string,
    ): Promise<void> {
        try {
            const remote = JSON.parse(offer);
            const callSettings = await this.apiService.getCallSettings();

            await this.connectionManager.handleOfferAndCreateAnswer(
                JSON.parse(callSettings.iceServerConfiguration),
                remote.desc,
                remote.candidates,
            );

            this.iceGatheringTimer = new Timer(() => {
                this.connectionManager.requestCompleteGathering();
            }, this.iceGatheringTimeoutMs);

            const finalOffer = await lastValueFrom(
                this.iceCandidateSubject.pipe(
                    takeUntil(this.iceGatheringComplete),
                ),
            );

            const diagnostics = await this.connectionManager.getDiagnostics();

            return this.apiService.handleIncomingCall(
                callId,
                chatId,
                finalOffer,
                this.iceGatheringElapsedMs,
                this.iceGatheringCollectedAll,
                diagnostics,
            );
        } catch (error) {
            await this.errorLoggingService.logConnectionError(undefined, error);
            throw error;
        }
    }

    async establishConnection(answer: string): Promise<void> {
        const remote = JSON.parse(answer);

        if (remote.desc.type !== 'answer') return;

        await this.connectionManager.setRemoteAnswerAndCandidates(
            remote.desc,
            remote.candidates,
        );
    }

    endCall(): void {
        this.processEndCall();
    }

    private onIceCandidateGenerated(data: string): void {
        this.iceCandidateSubject.next(data);

        if (this.iceGatheringTimer.isExpired()) {
            this.connectionManager.requestCompleteGathering();
        }
    }

    private onIceGatheringComplete(
        timeElapsed: number,
        collectedAll: boolean,
    ): void {
        this.iceGatheringElapsedMs = timeElapsed;
        this.iceGatheringCollectedAll = collectedAll;
        this.iceGatheringComplete.next();
        this.iceGatheringTimer.clear();
    }

    private async onConnected(): Promise<void> {
        const diagnostics = await this.connectionManager.getDiagnostics();
        const callSettings = await firstValueFrom(
            this.store.select(selectVideoCall),
        );
        await this.apiService.logConnectionEstablished(
            callSettings.callId,
            callSettings.chatId,
            diagnostics,
        );
    }

    private async onDisconnected(): Promise<void> {
        this.processEndCall();
    }

    private processEndCall(): void {
        this.connectionManager.reinitialize();
        this.storeService.resetCall();
    }

    private onConnectionError(
        errorMessage?: string,
        error?: any,
    ): Promise<void> {
        return this.errorLoggingService.logConnectionError(errorMessage, error);
    }

    private onIceServerError(
        errorMessage?: string,
        error?: any,
    ): Promise<void> {
        return this.errorLoggingService.logIceServerError(errorMessage, error);
    }
}
