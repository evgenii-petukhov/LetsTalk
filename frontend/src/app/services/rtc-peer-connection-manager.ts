/* eslint-disable @typescript-eslint/no-explicit-any */
import { inject, Injectable } from '@angular/core';
import { IceCandidateMetricsService } from './ice-candidate-metrics.service';
import { mediaStreamConstraintFallbacks } from './media-stream-constraint-fallbacks';
import { RtcConnectionDiagnosticsService } from './rtc-connection-diagnostics.service';
import { VideoCall } from '../models/video-call';

@Injectable({
    providedIn: 'root',
})
export class RtcPeerConnectionManager {
    onCandidatesReceived: (data: string) => void;
    onGatheringCompleted: (timeElapsed: number, collectedAll: boolean) => void;
    onConnected: () => void;
    onConnectionError: (errorMessage?: string, error?: any) => void;
    onIceServerError: (errorMessage?: string, error?: any) => void;
    onDisconnected: () => void;
    isMediaCaptured = false;
    private connection = new RTCPeerConnection();
    private localCandidates: RTCIceCandidate[] = [];
    private readonly iceCandidateMetricsService = inject(
        IceCandidateMetricsService,
    );
    private readonly rtcConnectionDiagnosticsService = inject(
        RtcConnectionDiagnosticsService,
    );
    private isGathering = true;
    private localMediaStream: MediaStream | null = null;
    private remoteMediaStream: MediaStream | null = null;
    private iceCandidateGatheringStarted: number;

    constructor() {
        this.connection.onicecandidate = this.onIceCandidateReceived.bind(this);
        this.connection.onconnectionstatechange =
            this._onConnectionStateChange.bind(this);
        this.connection.oniceconnectionstatechange =
            this.onIceConnectionStateChange.bind(this);
        this.connection.onicecandidateerror =
            this.onIceCandidateError.bind(this);
    }

    async initiateOffer(config: RTCConfiguration): Promise<void> {
        this.isGathering = true;
        this.localCandidates = [];
        this.connection.setConfiguration(config);
        this.iceCandidateGatheringStarted = performance.now();

        const offer = await this.connection.createOffer();
        await this.connection.setLocalDescription(offer);
    }

    async handleOfferAndCreateAnswer(
        config: RTCConfiguration,
        desc: RTCSessionDescriptionInit,
        candidates: RTCIceCandidateInit[],
    ): Promise<void> {
        this.isGathering = true;
        this.localCandidates = [];
        this.connection.setConfiguration(config);
        this.iceCandidateGatheringStarted = performance.now();

        await this.connection.setRemoteDescription(desc);
        if (candidates) {
            for (const c of candidates)
                await this.connection.addIceCandidate(c);
        }

        const answer = await this.connection.createAnswer();
        await this.connection.setLocalDescription(answer);
    }

    requestCompleteGathering(): void {
        if (
            !this.iceCandidateMetricsService.hasMinimumCandidateCount(
                this.localCandidates,
            )
        )
            return;

        if (
            this.iceCandidateMetricsService.hasSufficientServers(
                this.localCandidates,
            )
        ) {
            this.finalizeIceGathering(false);
        }
    }

    async setRemoteAnswerAndCandidates(
        desc: RTCSessionDescriptionInit,
        candidates: RTCIceCandidateInit[],
    ): Promise<void> {
        if (this.connection.signalingState !== 'have-local-offer') {
            return;
        }

        await this.connection.setRemoteDescription(desc);
        if (candidates) {
            for (const c of candidates)
                await this.connection.addIceCandidate(c);
        }
    }

    async startMediaCapture(
        localVideo: HTMLVideoElement,
        remoteVideo: HTMLVideoElement,
        facingMode: VideoCall['facingMode'],
    ): Promise<void> {
        for (const constraints of mediaStreamConstraintFallbacks) {
            try {
                const videoConstraint =
                    typeof constraints.video === 'object'
                        ? { ...constraints.video, facingMode }
                        : { facingMode };
                this.localMediaStream =
                    await navigator.mediaDevices.getUserMedia({
                        ...constraints,
                        video: videoConstraint,
                    });
                this.connectLocalVideo(localVideo);
                this.localMediaStream
                    .getTracks()
                    .forEach((track) =>
                        this.connection.addTrack(track, this.localMediaStream),
                    );
                this.connection.ontrack = (e) => {
                    this.remoteMediaStream = e.streams[0];
                    this.connectRemoteVideo(remoteVideo);
                };
                this.isMediaCaptured = true;
                return;
            } catch (error) {
                this.onConnectionError?.(undefined, error);
                console.error(error);
            }
        }
    }

    reconnectVideoElements(elements: {
        local?: HTMLVideoElement;
        remote?: HTMLVideoElement;
    }): void {
        if (elements.local) {
            this.connectLocalVideo(elements.local);
        }

        if (elements.remote) {
            this.connectRemoteVideo(elements.remote);
        }
    }

    setVideoEnabled(enabled: boolean): void {
        if (this.localMediaStream) {
            const videoTracks = this.localMediaStream.getVideoTracks();
            videoTracks.forEach((track) => {
                track.enabled = enabled;
            });
        }
    }

    setAudioEnabled(enabled: boolean): void {
        if (this.localMediaStream) {
            const audioTracks = this.localMediaStream.getAudioTracks();
            audioTracks.forEach((track) => {
                track.enabled = enabled;
            });
        }
    }

    stopMediaCapture() {
        if (this.localMediaStream) {
            this.localMediaStream.getTracks().forEach((track) => track.stop());
        }
    }

    reinitialize(): void {
        if (this.connection) {
            this.stopMediaCapture();
            this.connection.close();
            this.connection = new RTCPeerConnection();
            this.connection.onicecandidate =
                this.onIceCandidateReceived.bind(this);
            this.connection.onconnectionstatechange =
                this._onConnectionStateChange.bind(this);
        }

        this.remoteMediaStream = null;
        this.localCandidates = [];
        this.isGathering = true;
        this.isMediaCaptured = false;
    }

    getDiagnostics() {
        return this.rtcConnectionDiagnosticsService.gatherConnectionDiagnostics(
            this.connection,
        );
    }

    async switchCamera(
        localVideo: HTMLVideoElement,
        remoteVideo: HTMLVideoElement,
        facingMode: VideoCall['facingMode'],
    ): Promise<void> {
        this.stopMediaCapture();
        await this.startMediaCapture(localVideo, remoteVideo, facingMode);

        const newVideoTrack = this.localMediaStream.getVideoTracks()[0];
        const sender = this.connection
            .getSenders()
            .find((s) => s.track?.kind === 'video');
        if (sender) await sender.replaceTrack(newVideoTrack);
    }

    private connectLocalVideo(localVideo: HTMLVideoElement): void {
        if (this.localMediaStream && localVideo) {
            const videoOnlyStream = new MediaStream(
                this.localMediaStream.getVideoTracks(),
            );
            localVideo.srcObject = videoOnlyStream;
        }
    }

    private connectRemoteVideo(remoteVideo: HTMLVideoElement): void {
        if (this.remoteMediaStream && remoteVideo) {
            remoteVideo.srcObject = this.remoteMediaStream;
        }
    }

    private onIceCandidateReceived(e: RTCPeerConnectionIceEvent): void {
        if (!this.isGathering) return;

        if (!e.candidate) {
            this.finalizeIceGathering(true);
            return;
        }

        this.localCandidates.push(e.candidate);

        const data = {
            desc: this.connection.localDescription,
            candidates: this.localCandidates,
        };

        this.onCandidatesReceived?.(JSON.stringify(data));

        this.requestCompleteGathering();
    }

    private finalizeIceGathering(collectedAll: boolean): void {
        this.isGathering = false;
        this.onGatheringCompleted?.(
            Math.round(performance.now() - this.iceCandidateGatheringStarted),
            collectedAll,
        );
    }

    private _onConnectionStateChange(): void {
        switch (this.connection.connectionState) {
            case 'connected':
                this.onConnected?.();
                break;
            case 'failed':
                this.onConnectionError?.();
                break;
            case 'disconnected':
                this.onDisconnected?.();
                break;
        }
    }

    private onIceConnectionStateChange(): void {
        if (this.connection.iceConnectionState === 'failed') {
            this.onIceServerError?.(undefined, new Error());
        }
    }

    private onIceCandidateError(event: RTCPeerConnectionIceErrorEvent): void {
        this.onIceServerError?.(event.errorText, new Error());
    }
}
