import { createReducer, on } from '@ngrx/store';
import { ChatDto, IChatDto } from '../../api-client/api-client';
import { chatsActions } from './chats.actions';

export const initialState: readonly IChatDto[] | null = null as readonly IChatDto[] | null;

export const chatsReducer = createReducer(
    initialState,
    on(chatsActions.init, (_state, { chats }) => chats),
    on(chatsActions.setUnreadCount, (state, { chatId, unreadCount }) =>
        state ? state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, unreadCount: unreadCount })
                : chat,
        ) : null,
    ),
    on(chatsActions.incrementUnread, (state, { chatId }) =>
        state ? state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, unreadCount: (chat.unreadCount ?? 0) + 1 })
                : chat,
        ) : null,
    ),
    on(chatsActions.setLastMessageDate, (state, { chatId, date }) =>
        state ? state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, lastMessageDate: date })
                : chat,
        ) : null,
    ),
    on(chatsActions.setLastMessageId, (state, { chatId, id }) =>
        state ? state.map((chat) =>
            chat.id === chatId
                ? new ChatDto({ ...chat, lastMessageId: id })
                : chat,
        ) : null,
    ),
    on(chatsActions.updateChatId, (state, { chatId, newChatId }) =>
        state ? state.map((chat) =>
            chat.id === chatId ? new ChatDto({ ...chat, id: newChatId }) : chat,
        ) : null,
    ),
    on(chatsActions.add, (state, { chatDto }) => [...(state ?? []), chatDto]),
);
