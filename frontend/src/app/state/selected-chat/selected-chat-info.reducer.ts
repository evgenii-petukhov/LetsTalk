import { createReducer, on } from '@ngrx/store';
import { selectedChatInfoActions } from './selected-chat-info.actions';
import { SelectedChat } from 'src/app/models/selected-chat';
import { MessageFetchStatus } from '../../models/message-fetch-status';

export const initialState: SelectedChat = null;

export const selectedChatInfoReducer = createReducer(
    initialState,
    on(selectedChatInfoActions.init, (state, { chatId }) => ({
        chatId,
        messageFetchStatus:
            state && state.chatId === chatId
                ? state.messageFetchStatus
                : MessageFetchStatus.Unknown,
    })),
    on(selectedChatInfoActions.setMessageFetchStatus, (state, { status }) => ({
        ...state,
        messageFetchStatus: status,
    })),
);
