import { createReducer, on } from '@ngrx/store';
import { Message } from '../../models/message';
import { messagesActions } from './messages.actions';

export const initialState: readonly Message[] = [];

export const messagesReducer = createReducer(
    initialState,
    on(messagesActions.init, (_state, { messageDtos }) =>
        messageDtos.map((messageDto) => new Message(messageDto)),
    ),
    on(messagesActions.addMessage, (state, { messageDto }) => {
        const existing = state.find((m) => m.id === messageDto.id);
        return !!existing?.text || !!existing?.image
            ? state
            : [
                  ...state.filter((m) => m.id !== messageDto.id),
                  new Message(existing, messageDto),
              ];
    }),
    on(messagesActions.addMessages, (state, { messageDtos }) => {
        const messages = messageDtos
            .map((messageDto) => new Message(messageDto))
            .filter(
                (message) => !state.some((prev) => prev.id === message.id),
            );
        return [...state, ...messages];
    }),
    on(messagesActions.setLinkPreview, (state, { linkPreviewDto }) => {
        const existing = state.find((m) => m.id === linkPreviewDto.messageId);
        return existing?.linkPreview
            ? state
            : [
                  ...state.filter((m) => m.id !== linkPreviewDto.messageId),
                  new Message(existing, { linkPreview: linkPreviewDto }),
              ];
    }),
    on(messagesActions.setImagePreview, (state, { imagePreviewDto }) => {
        const existing = state.find((m) => m.id === imagePreviewDto.messageId);
        return existing?.imagePreview
            ? state
            : [
                  ...state.filter((m) => m.id !== imagePreviewDto.messageId),
                  new Message(existing, { imagePreview: imagePreviewDto }),
              ];
    }),
);
