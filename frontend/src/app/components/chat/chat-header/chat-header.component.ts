import { Component, computed, inject, Input } from '@angular/core';
import { Store } from '@ngrx/store';
import {
    selectSelectedChat,
} from '../../../state/selected-chat/selected-chat.selector';
import { IChatDto } from '../../../api-client/api-client';
import { BackButtonStatus } from '../../../models/back-button-status';
import { faPhone } from '@fortawesome/free-solid-svg-icons';
import { StoreService } from '../../../services/store.service';
import { IdGeneratorService } from '../../../services/id-generator.service';
import { ApiService } from '../../../services/api.service';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { selectIsAnyCallInProgress } from '../../../state/video-call/video-call.selectors';

@Component({
    selector: 'app-chat-header',
    templateUrl: './chat-header.component.html',
    styleUrls: ['./chat-header.component.scss'],
    standalone: false,
})
export class ChatHeaderComponent {
    faPhone = faPhone;
    @Input() backButton: BackButtonStatus;

    private readonly store = inject(Store);
    private readonly storeService = inject(StoreService);
    private readonly idGeneratorService = inject(IdGeneratorService);
    private readonly apiService = inject(ApiService);
    private readonly router = inject(Router);

    chat = toSignal(this.store.select(selectSelectedChat), {
        initialValue: null,
    });

    isAnyCallInProgress = toSignal(
        this.store.select(selectIsAnyCallInProgress),
    );

    urlOptions = computed(() => {
        const chat = this.chat();
        return chat && [chat.image, chat.photoUrl];
    });

    async onCallClicked(): Promise<void> {
        const chat = this.chat();
        if (!chat) return;

        let chatId: string;
        if (this.shouldCreateIndividualChat(chat)) {
            chatId = await this.handleIndividualChatCreation(
                chat.id,
                chat.accountIds[0],
            );
        }

        this.storeService.initOutgoingCall(chatId ?? chat.id);
    }

    private shouldCreateIndividualChat(chat: IChatDto): boolean {
        return chat.isIndividual && this.idGeneratorService.isFake(chat.id);
    }

    private async handleIndividualChatCreation(
        chatId: string,
        accountId: string,
    ): Promise<string> {
        const chatDto = await this.apiService.createIndividualChat(accountId);
        this.storeService.updateChatId(chatId, chatDto.id);
        await this.router.navigate(['/messenger/chat', chatDto.id]);
        return chatDto.id;
    }
}
