import { createReducer, on } from '@ngrx/store';
import { VideoCall } from '../../models/video-call';
import { videoCallActions } from './video-call.actions';

export const initialState: VideoCall | null = null;

export const videoCallReducer = createReducer(
    initialState,
    on(videoCallActions.initOutgoingCall, (_state, { chatId }) => ({
        chatId,
        status: 'outgoing',
        captureVideo: true,
        captureAudio: true,
        isMinimized: false,
        facingMode: 'user',
    })),
    on(
        videoCallActions.initIncomingCall,
        (_state, { callId, chatId, offer }) => ({
            callId,
            chatId,
            offer,
            status: 'incoming-awaiting',
            captureVideo: true,
            captureAudio: true,
            isMinimized: false,
            facingMode: 'user',
        }),
    ),
    on(videoCallActions.acceptIncomingCall, (state) => {
        if (!state) return state;
        return {
            ...state,
            status: 'incoming-active',
        };
    }),
    on(videoCallActions.setCallId, (state, { callId }) => ({
        ...state,
        callId,
    })),
    on(videoCallActions.toggleCaptureVideo, (state) => {
        if (!state) return state;
        return {
            ...state,
            captureVideo: !state.captureVideo,
        };
    }),
    on(videoCallActions.toggleCaptureAudio, (state) => {
        if (!state) return state;
        return {
            ...state,
            captureAudio: !state.captureAudio,
        };
    }),
    on(videoCallActions.reset, () => null),
    on(videoCallActions.minimize, (state) => {
        if (!state) return state;
        return {
            ...state,
            isMinimized: true,
        };
    }),
    on(videoCallActions.maximize, (state) => {
        if (!state) return state;
        return {
            ...state,
            isMinimized: false,
        };
    }),
    on(videoCallActions.toggleFacingMode, (state) => {
        if (!state) return state;
        return {
            ...state,
            facingMode: state.facingMode === 'user' ? 'environment' : 'user',
        };
    }),
);
