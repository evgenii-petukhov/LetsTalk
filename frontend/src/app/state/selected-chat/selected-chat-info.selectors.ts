import { createFeatureSelector, createSelector } from '@ngrx/store';
import { MessageFetchStatus } from '../../models/message-fetch-status';
import { SelectedChat } from '../../models/selected-chat';

const selectSelectedChatInfo =
    createFeatureSelector<SelectedChat>('selectedChatInfo');

export const selectSelectedChatId = createSelector(
    selectSelectedChatInfo,
    (info) => info?.chatId,
);

export const selectMessageFetchStatus = createSelector(
    selectSelectedChatInfo,
    (info) => info?.messageFetchStatus ?? MessageFetchStatus.Unknown,
);
