import {
    Component,
    inject,
    OnDestroy,
    OnInit,
    signal,
    computed,
    ViewChild,
} from '@angular/core';
import { required, validate } from '../../../decorators/required.decorator';
import { IChatDto, IMessageDto } from '../../../api-client/api-client';
import { selectSelectedChat } from '../../../state/selected-chat/selected-chat.selector';
import { Subject, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { IdGeneratorService } from '../../../services/id-generator.service';
import { ApiService } from '../../../services/api.service';
import { StoreService } from '../../../services/store.service';
import { errorMessages } from '../../../constants/errors';
import { ErrorService } from '../../../services/error.service';
import { environment } from '../../../../environments/environment';
import { ImageRoles, UploadImageResponse } from '../../../protos/file_upload_pb';
import { ImageUploadService } from '../../../services/image-upload.service';
import { AutoResizeTextAreaComponent } from '../auto-resize-text-area/auto-resize-text-area.component';
import { Router } from '@angular/router';

@Component({
    selector: 'app-compose-area',
    templateUrl: './compose-area.component.html',
    styleUrl: './compose-area.component.scss',
    standalone: false,
})
export class ComposeAreaComponent implements OnInit, OnDestroy {
    message = signal('');
    isSending = signal(false);
    hasMessage = computed(() => !!this.message().trim());
    isDisabled = computed(() => this.isSending() || !this.hasMessage());

    @ViewChild(AutoResizeTextAreaComponent)
    textareaRef: AutoResizeTextAreaComponent;

    private chat: IChatDto | null = null;

    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly store = inject(Store);
    private readonly apiService = inject(ApiService);
    private readonly errorService = inject(ErrorService);
    private readonly storeService = inject(StoreService);
    private readonly idGeneratorService = inject(IdGeneratorService);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly router = inject(Router);

    ngOnInit(): void {
        this.store
            .select(selectSelectedChat)
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe((chat) => {
                this.chat = chat;
            });
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    @validate
    async onSendMessage(@required message: string): Promise<void> {
        this.message.set('');
        this.isSending.set(true);
        this.textareaRef?.focus();
        try {
            await this.processSendMessage(this.chat, message);
        } catch (e) {
            this.errorService.handleError(e, errorMessages.sendMessage);
        } finally {
            this.isSending.set(false);
        }
    }

    async onImageBlobReady(blob: Blob): Promise<void> {
        const chat = this.chat;
        const buffer = await blob.arrayBuffer();
        const base64 = URL.createObjectURL(new Blob([buffer]));
        const sizeLimits = environment.imageSettings.limits;
        try {
            const image = await this.imageUploadService.resizeAndUploadImage(
                base64 as string,
                sizeLimits.picture.width,
                sizeLimits.picture.height,
                ImageRoles.MESSAGE,
            );
            await this.processSendMessage(chat, undefined, image);
        } catch (e) {
            this.errorService.handleError(e, errorMessages.sendMessage);
        } finally {
            URL.revokeObjectURL(base64);
        }
    }

    private async processSendMessage(
        chat: IChatDto,
        message: string,
        image?: UploadImageResponse,
    ): Promise<void> {
        if (this.shouldCreateIndividualChat(chat)) {
            await this.handleIndividualChatCreation(
                chat.id,
                chat.accountIds[0],
                message,
                image,
            );
        } else {
            await this.handleMessageSending(chat.id, message, image);
        }
    }

    private shouldCreateIndividualChat(chat: IChatDto): boolean {
        return chat.isIndividual && this.idGeneratorService.isFake(chat.id);
    }

    private async handleIndividualChatCreation(
        chatId: string,
        accountId: string,
        message: string,
        image: UploadImageResponse,
    ): Promise<void> {
        const chatDto = await this.apiService.createIndividualChat(accountId);
        await this.apiService.sendMessage(chatDto.id, message, image);
        this.storeService.updateChatId(chatId, chatDto.id);
        await this.router.navigate(['/messenger/chat', chatDto.id]);
    }

    private async handleMessageSending(
        chatId: string,
        message: string,
        image: UploadImageResponse,
    ): Promise<void> {
        const messageDto = await this.apiService.sendMessage(
            chatId,
            message,
            image,
        );
        if (chatId === this.chat.id) {
            this.addMessageToStore(chatId, messageDto);
        }
    }

    private addMessageToStore(chatId: string, messageDto: IMessageDto): void {
        messageDto.isMine = true;
        this.storeService.addMessage(messageDto);
        this.storeService.setLastMessageInfo(
            chatId,
            messageDto.created,
            messageDto.id,
        );
    }
}
