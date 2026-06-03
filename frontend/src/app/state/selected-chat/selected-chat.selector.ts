import { createSelector } from '@ngrx/store';
import { selectChats } from '../chats/chats.selector';
import {
    selectMessageFetchStatus,
    selectSelectedChatId,
} from './selected-chat-info.selectors';
import { selectVideoCall } from '../video-call/video-call.selectors';
import { MessageFetchStatus } from '../../models/message-fetch-status';

export const selectSelectedChat = createSelector(
    selectChats,
    selectSelectedChatId,
    (chats, chatId) => chats?.find((chat) => chat.id === chatId),
);

export const selectIsOngoingCallScreenVisible = createSelector(
    selectVideoCall,
    selectSelectedChatId,
    (videoCall, chatId) =>
        videoCall != null &&
        !videoCall.isMinimized &&
        videoCall.chatId === chatId &&
        videoCall.status !== 'incoming-awaiting',
);

export const selectIsOngoingCallPanelVisible = createSelector(
    selectVideoCall,
    selectSelectedChatId,
    (videoCall, chatId) =>
        videoCall != null &&
        (videoCall.isMinimized || videoCall.chatId !== chatId),
);

export const selectIsAwaitingResponseScreenVisible = createSelector(
    selectVideoCall,
    selectSelectedChatId,
    (videoCall, chatId) =>
        videoCall != null &&
        videoCall.chatId === chatId &&
        videoCall.status === 'incoming-awaiting',
);

export const selectIsOngoingCallControlSetVisible = createSelector(
    selectIsOngoingCallPanelVisible,
    selectVideoCall,
    (isOngoingCallPanelVisible, videoCall) =>
        isOngoingCallPanelVisible &&
        videoCall != null &&
        videoCall.status !== 'incoming-awaiting',
);

export const selectIsAwaitingResponseButtonVisible = createSelector(
    selectIsOngoingCallPanelVisible,
    selectVideoCall,
    (isOngoingCallPanelVisible, videoCall) =>
        isOngoingCallPanelVisible &&
        videoCall != null &&
        videoCall.status === 'incoming-awaiting',
);

export const selectIsHeaderVisible = createSelector(
    selectIsOngoingCallScreenVisible,
    selectIsAwaitingResponseScreenVisible,
    (isCallInProgress, awaitingResponse) =>
        !isCallInProgress && !awaitingResponse,
);

export const selectIsMessageListVisible = createSelector(
    selectIsOngoingCallScreenVisible,
    selectIsAwaitingResponseScreenVisible,
    selectMessageFetchStatus,
    (isCallInProgress, awaitingResponse, status) =>
        !isCallInProgress &&
        !awaitingResponse &&
        [MessageFetchStatus.Unknown, MessageFetchStatus.Success].includes(
            status,
        ),
);

export const selectIsComposeAreaVisible = createSelector(
    selectIsOngoingCallScreenVisible,
    selectIsAwaitingResponseScreenVisible,
    selectMessageFetchStatus,
    (isCallInProgress, awaitingResponse, status) =>
        !isCallInProgress &&
        !awaitingResponse &&
        status === MessageFetchStatus.Success,
);

export const selectIsNotFoundVisible = createSelector(
    selectIsOngoingCallScreenVisible,
    selectMessageFetchStatus,
    (isCallInProgress, status) =>
        !isCallInProgress && status === MessageFetchStatus.NotFound,
);

export const selectIsErrorVisible = createSelector(
    selectIsOngoingCallScreenVisible,
    selectMessageFetchStatus,
    (isCallInProgress, status) =>
        !isCallInProgress && status === MessageFetchStatus.Error,
);
