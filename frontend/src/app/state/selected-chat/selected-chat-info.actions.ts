import { createActionGroup, props } from '@ngrx/store';
import { MessageFetchStatus } from '../../models/message-fetch-status';

export const selectedChatInfoActions = createActionGroup({
    source: 'selectedChatInfo',
    events: {
        init: props<{ chatId: string }>(),
        setMessageFetchStatus: props<{ status: MessageFetchStatus }>(),
    },
});
