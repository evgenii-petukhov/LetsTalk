import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { MessageFetchStatus } from '../../models/message-fetch-status';
import { StoreService } from '../../services/store.service';
import { selectSelectedChatId } from '../../state/selected-chat/selected-chat-info.selectors';
import {
    selectIsAwaitingResponseScreenVisible,
    selectIsHeaderVisible,
    selectIsMessageListVisible,
    selectIsComposeAreaVisible,
    selectIsNotFoundVisible,
    selectIsErrorVisible,
    selectIsOngoingCallScreenVisible,
} from 'src/app/state/selected-chat/selected-chat.selector';

@Component({
    selector: 'app-chat',
    templateUrl: './chat.component.html',
    styleUrls: ['./chat.component.scss'],
    standalone: false,
})
export class ChatComponent implements OnInit, OnDestroy {
    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly store = inject(Store);
    private readonly storeService = inject(StoreService);

    isOngoingCallScreenVisible = toSignal(
        this.store.select(selectIsOngoingCallScreenVisible),
    );
    isAwaitingResponseScreenVisible = toSignal(
        this.store.select(selectIsAwaitingResponseScreenVisible),
    );
    isHeaderVisible = toSignal(this.store.select(selectIsHeaderVisible));
    isMessageListVisible = toSignal(
        this.store.select(selectIsMessageListVisible),
    );
    isComposeAreaVisible = toSignal(
        this.store.select(selectIsComposeAreaVisible),
    );
    isNotFoundVisible = toSignal(this.store.select(selectIsNotFoundVisible));
    isErrorVisible = toSignal(this.store.select(selectIsErrorVisible));

    ngOnInit(): void {
        this.store
            .select(selectSelectedChatId)
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe(() => {
                this.storeService.setSelectedChatMessageFetchStatus(
                    MessageFetchStatus.Unknown,
                );
            });
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }
}
