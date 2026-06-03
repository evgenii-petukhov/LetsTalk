import { MessageFetchStatus } from "./message-fetch-status";

export interface SelectedChat {
    chatId: string;
    messageFetchStatus: MessageFetchStatus;
}
