import { createReducer, on } from '@ngrx/store';
import { ChatDto, IChatDto } from '../../api-client/api-client';
import { chatsActions } from './chats.actions';

export const initialState: readonly IChatDto[] = null;

export const chatsReducer = createReducer(
    initialState,
    on(chatsActions.init, (_state, { chats }) => chats),
    on(chatsActions.setUnreadCount, (state, { chatId, unreadCount }) =>
        state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, unreadCount: unreadCount })
                : chat,
        ),
    ),
    on(chatsActions.incrementUnread, (state, { chatId }) =>
        state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, unreadCount: chat.unreadCount + 1 })
                : chat,
        ),
    ),
    on(chatsActions.setLastMessageDate, (state, { chatId, date }) =>
        state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, lastMessageDate: date })
                : chat,
        ),
    ),
    on(chatsActions.setLastMessageId, (state, { chatId, id }) =>
        state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, lastMessageId: id })
                : chat,
        ),
    ),
    on(chatsActions.updateChatId, (state, { chatId, newChatId }) =>
        state.map((chat) =>
            chat.id === chatId ? new ChatDto({ ...chat, id: newChatId }) : chat,
        ),
    ),
    on(chatsActions.add, (state, { chatDto }) => [...state, chatDto]),
);
