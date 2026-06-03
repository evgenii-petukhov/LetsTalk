import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ChatDto, IAccountDto, IChatDto } from '../../../../api-client/api-client';
import { combineLatest, Subject, takeUntil } from 'rxjs';
import { StoreService } from '../../../../services/store.service';
import { IdGeneratorService } from '../../../../services/id-generator.service';
import { Store } from '@ngrx/store';
import { selectChats } from '../../../../state/chats/chats.selector';
import { selectAccounts } from '../../../../state/accounts/accounts.selector';
import { Location } from '@angular/common';

@Component({
    selector: 'app-account-list',
    templateUrl: './account-list.component.html',
    styleUrls: ['./account-list.component.scss'],
    standalone: false,
})
export class AccountListComponent implements OnInit, OnDestroy {
    accounts = signal<readonly IAccountDto[]>([]);
    private chats = signal<readonly IChatDto[]>([]);

    private readonly unsubscribe$: Subject<void> = new Subject<void>();
    private readonly store = inject(Store);
    private readonly storeService = inject(StoreService);
    private readonly idGeneratorService = inject(IdGeneratorService);
    private readonly location = inject(Location);

    async ngOnInit(): Promise<void> {
        combineLatest([
            this.store.select(selectChats),
            this.store.select(selectAccounts),
        ])
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe(([chats, accounts]) => {
                this.chats.set(chats);
                this.accounts.set(accounts);
            });

        await this.storeService.initAccountStorage();
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    async onAccountSelected(account: IAccountDto): Promise<void> {
        const chat = this.chats().find(
            (chat) => chat.isIndividual && chat.accountIds[0] === account.id,
        );
        if (chat) {
            await this.storeService.markAllAsRead(chat);
        } else {
            const chatDto = new ChatDto({
                id: this.idGeneratorService.getNextFakeId().toString(),
                isIndividual: true,
                accountIds: [account.id],
                accountTypeId: account.accountTypeId,
                chatName: `${account.firstName} ${account.lastName}`,
                image: account.image,
                photoUrl: account.photoUrl,
                unreadCount: 0,
            });

            this.storeService.addChat(chatDto);
        }
        this.location.back();
    }
}
