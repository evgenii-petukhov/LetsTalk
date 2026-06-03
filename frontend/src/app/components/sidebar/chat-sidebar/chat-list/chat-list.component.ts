import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Store } from '@ngrx/store';
import { IChatDto } from '../../../../api-client/api-client';
import { selectSelectedChatId } from '../../../../state/selected-chat/selected-chat-info.selectors';
import { combineLatest, Subject, takeUntil } from 'rxjs';
import { selectChats } from '../../../../state/chats/chats.selector';

@Component({
    selector: 'app-chat-list',
    templateUrl: './chat-list.component.html',
    styleUrls: ['./chat-list.component.scss'],
    standalone: false,
})
export class ChatListComponent implements OnInit, OnDestroy {
    chats = signal<readonly IChatDto[]>([]);
    selectedChatId = signal<string>(null);

    private readonly store = inject(Store);
    private readonly unsubscribe$: Subject<void> = new Subject<void>();

    ngOnInit(): void {
        combineLatest([
            this.store.select(selectChats),
            this.store.select(selectSelectedChatId),
        ])
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe(([chats, selectedChatId]) => {
                this.chats.set(chats);
                this.selectedChatId.set(selectedChatId);
            });
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }
}
