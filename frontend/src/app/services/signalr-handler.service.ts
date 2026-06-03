import { inject, Injectable } from '@angular/core';
import { SignalrService } from './signalr.service';
import {
    IMessageDto,
    ILinkPreviewDto,
    IImagePreviewDto,
    IChatDto,
} from '../api-client/api-client';
import { StoreService } from './store.service';
import { ApiService } from './api.service';
import { BrowserNotificationService } from './browser-notification.service';
import { IncomingCall } from '../models/incoming-call';
import { Router } from '@angular/router';
import { EstablishConnection } from '../models/establish-connection';

@Injectable({
    providedIn: 'root',
})
export class SignalrHandlerService {
    private readonly apiService = inject(ApiService);
    private readonly storeService = inject(StoreService);
    private readonly signalrService = inject(SignalrService);
    private readonly browserNotificationService = inject(
        BrowserNotificationService,
    );
    private readonly router = inject(Router);

    async initHandlers(
        handleMessageNotification: (messageDto: IMessageDto) => Promise<void>,
        handleLinkPreviewNotification: (
            linkPreviewDto: ILinkPreviewDto,
        ) => void,
        handleImagePreviewNotification: (
            imagePreviewDto: IImagePreviewDto,
        ) => void,
        handleIncomingCallNotification: (
            data: IncomingCall,
        ) => void,
        handleEstablishConnectionNotification: (
            data: EstablishConnection,
        ) => void,
    ): Promise<void> {
        await this.browserNotificationService.init();
        await this.signalrService.init(
            handleMessageNotification,
            handleLinkPreviewNotification,
            handleImagePreviewNotification,
            handleIncomingCallNotification,
            handleEstablishConnectionNotification,
        );
    }

    removeHandlers() {
        this.signalrService.removeHandlers();
    }

    async handleMessageNotification(
        messageDto: IMessageDto,
        selectedChatId: string,
        chats: readonly IChatDto[],
        isWindowActive: boolean,
    ): Promise<void> {
        this.storeService.setLastMessageInfo(
            messageDto.chatId,
            messageDto.created,
            messageDto.id,
        );
        if (messageDto.chatId === selectedChatId) {
            this.storeService.addMessage(messageDto);
        }

        if (messageDto.isMine) {
            return;
        }

        if (isWindowActive && messageDto.chatId === selectedChatId) {
            await this.apiService.markAsRead(messageDto.chatId, messageDto.id);
        } else {
            const chat = chats.find((chat) => chat.id === messageDto.chatId);
            if (chat) {
                this.storeService.incrementUnreadMessages(messageDto.chatId);
                this.browserNotificationService.showNotification(
                    chat.chatName,
                    messageDto.image ? 'Image' : messageDto.text,
                    isWindowActive,
                );
            } else {
                await this.storeService.initChatStorage(true);
            }
        }
    }

    handleLinkPreviewNotification(
        linkPreviewDto: ILinkPreviewDto,
        selectedChatId: string,
    ): void {
        if (linkPreviewDto.chatId !== selectedChatId) {
            return;
        }
        this.storeService.setLinkPreview(linkPreviewDto);
    }

    handleImagePreviewNotification(
        imagePreviewDto: IImagePreviewDto,
        selectedChatId: string,
    ): void {
        if (imagePreviewDto.chatId !== selectedChatId) {
            return;
        }
        this.storeService.setImagePreview(imagePreviewDto);
    }

    async handleIncomingCallNotification(
        chats: readonly IChatDto[],
        callId: string,
        chatId: string,
        offer: string,
    ): Promise<void> {
        const chat = chats.find((chat) => chat.id === chatId);
        if (!chat) {
            await this.storeService.initChatStorage(true);
        }

        await this.router.navigate(['/messenger/chat', chatId]);

        this.storeService.initIncomingCall(callId, chatId, offer);
    }
}
