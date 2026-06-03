import { createFeatureSelector, createSelector } from '@ngrx/store';
import { VideoCall } from '../../models/video-call';
import { selectChats } from '../chats/chats.selector';

export const selectVideoCall = createFeatureSelector<VideoCall>('videoCall');

export const selectVideoCallChatId = createSelector(
    selectVideoCall,
    (videoCall) => videoCall?.chatId,
);

export const selectVideoCallChat = createSelector(
    selectChats,
    selectVideoCall,
    (chats, videoCall) => chats?.find((chat) => chat.id === videoCall.chatId),
);

export const selectIsAnyCallInProgress = createSelector(
    selectVideoCall,
    (videoCall) => !!videoCall,
);

export const selectCaptureVideo = createSelector(
    selectVideoCall,
    (videoCall) => videoCall && videoCall.captureVideo,
);

export const selectCaptureAudio = createSelector(
    selectVideoCall,
    (videoCall) => videoCall && videoCall.captureAudio,
);

export const selectFacingMode = createSelector(
    selectVideoCall,
    (videoCall) => videoCall && videoCall.facingMode,
);

export const selectCaller = createSelector(
    selectVideoCall,
    selectChats,
    (videoCall, chats) =>
        !videoCall || !chats
            ? null
            : chats.find((chat) => chat.id === videoCall.chatId),
);
